using Microsoft.Extensions.Configuration; // Fixes IConfiguration
using Microsoft.AspNetCore.Http;          // Fixes IFormFile
using Azure.Storage.Blobs;                // Fixes BlobServiceClient
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;


namespace BusinessLayer.Services
{
    public class BlobService
    {
        private readonly string _connectionString;

        public BlobService(IConfiguration configuration)
        {
            // We only need the connection string here now
            _connectionString = configuration.GetConnectionString("AzureBlobStorage");
        }



        public async Task<string> UploadAndResizeImageAsync(IFormFile file, string containerName, int width, int height)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // 1. Create a unique name but force .jpg extension
            string fileName = $"{Guid.NewGuid()}.jpg";
            var blobClient = containerClient.GetBlobClient(fileName);

            // 2. Load the image from the uploaded file stream
            using var inputStream = file.OpenReadStream();
            using var image = await Image.LoadAsync(inputStream);

            // 3. Resize the image (this maintains aspect ratio if you use specific modes)
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Max // Shrinks to fit within 400x400 without distorting
            }));

            // 4. Save the resized image to a MemoryStream
            using var outputStream = new MemoryStream();
            await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 80 });

            // Reset stream position to the beginning before uploading!
            outputStream.Position = 0;

            // 5. Upload to Azure
            await blobClient.UploadAsync(outputStream, true);

            return blobClient.Uri.ToString();
        }

        /// <summary>
        /// Deletes a blob by its full URL. Used for cleanup when a database transaction
        /// fails after a successful upload, preventing orphaned blobs.
        /// </summary>
        public async Task DeleteBlobAsync(string blobUrl, string containerName)
        {
            if (string.IsNullOrWhiteSpace(blobUrl)) return;

            try
            {
                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                // Extract just the file name from the full URL
                string fileName = Path.GetFileName(new Uri(blobUrl).LocalPath);
                var blobClient = containerClient.GetBlobClient(fileName);

                await blobClient.DeleteIfExistsAsync();
            }
            catch
            {
                // Swallow silently — cleanup failure should never mask the original error
            }
        }

        // Now we pass the 'containerName' as a variable
        //public async Task<string> UploadFileAsync(IFormFile file, string containerName)
        //{
        //    var blobServiceClient = new BlobServiceClient(_connectionString);
        //    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        //    string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        //    var blobClient = containerClient.GetBlobClient(fileName);

        //    using var stream = file.OpenReadStream();
        //    await blobClient.UploadAsync(stream, true);

        //    return blobClient.Uri.ToString();
        //}
    }
}

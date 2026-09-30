using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataAccessLayer.DTOs.Category
{
    public class CategoryEditRequestDTO
    {
        [Required(ErrorMessage = "Category ID is required.")]
        [JsonPropertyName("id")]
        public int ID { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 255 characters.")]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Minimum age is required.")]
        [Range(0, 100, ErrorMessage = "Minimum age must be between 0 and 100.")]
        [JsonPropertyName("ageMin")]
        public int MinAge { get; set; }

        [Required(ErrorMessage = "Maximum age is required.")]
        [Range(1, 100, ErrorMessage = "Maximum age must be between 1 and 100.")]
        [JsonPropertyName("ageMax")]
        public int MaxAge { get; set; }

        [Required(ErrorMessage = "Capacity is required.")]
        [Range(1, 10000, ErrorMessage = "Capacity must be between 1 and 10,000.")]
        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Registration fee is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Registration fee must be a non-negative value.")]
        [JsonPropertyName("registrationFee")]
        public decimal RegistrationFee { get; set; }
    }
}

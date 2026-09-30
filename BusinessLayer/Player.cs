using BusinessLayer.Exceptions;
using BusinessLayer.Helpers;
using BusinessLayer.Services;
using BusinessLayer.Services.Azure;
using DataAccessLayer.DTOs.AdultPlayerDetails;
using DataAccessLayer.DTOs.MinorPlayerDetails;
using DataAccessLayer.DTOs.Person;
using DataAccessLayer.DTOs.Player;
using DataAccessLayer.DTOs.PlayerMedicalDossier;
using DataAccessLayer.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Transactions;

namespace BusinessLayer
{
    public class Player
    {
        public int ID { get; set; }
        public IFormFile Photo { get; set; }
        public int PrimaryPositionID { get; set; }
        public int? SecondaryPositionID { get; set; }
        public int PreferredFootID { get; set; }
        public string JerseyNumber { get; set; }
        public string Address { get; set; }
        public int CategoryID { get; set; }

        private readonly PlayerRegistrationRequestDTO _request;
        private PlayerUpdateRequestDTO _updateRequest;
        private Person _person;
        private PlayerMedicalDossier _medicalDossier;
        private PlayerRegistrationSaveDTO _saveDTO;
        private string _uploadedImageUrl;

        public PlayerRegistrationResponseDTO playerRegistrationResponseDTO = new PlayerRegistrationResponseDTO();
        private static IMemoryCache GetCache() => AppServicesHelper.ServiceProvider.GetRequiredService<IMemoryCache>();
        private readonly static ConcurrentDictionary<int, HashSet<string>> _PlayersByCategoryCacheKeys = new();
        private static readonly object _cacheLock = new();


        public enum enMode
        {
            addNew = 0,
            update = 1
        }

        public enMode mode;

        public Player(PlayerRegistrationRequestDTO request)
        {
            _request = request;
            _InitializeFromRequestDTO();
            mode = enMode.addNew;
        }

        public Player(PlayerUpdateRequestDTO request)
        {
            _updateRequest = request;
            this.ID = request.ID;
            this.Photo = request.Photo;
            this.PrimaryPositionID = request.PrimaryPositionID;
            this.SecondaryPositionID = request.SecondaryPositionID;
            this.PreferredFootID = request.PreferredFootID;
            this.JerseyNumber = request.JerseyNumber;
            this.Address = request.Address;
            this.CategoryID = request.CategoryID;
            mode = enMode.update;
        }

        private void _InitializeFromRequestDTO()
        {
            this.Photo = _request.Photo;
            this.PrimaryPositionID = _request.PrimaryPositionID;
            this.SecondaryPositionID = _request.SecondaryPositionID;
            this.PreferredFootID = _request.PreferredFootID;
            this.JerseyNumber = _request.JerseyNumber;
            this.Address = _request.Address;
            this.CategoryID = _request.CategoryID;
        }

        private void _InitializePersonDetails()
        {
            PersonRegistrationRequestDTO personDTO = new PersonRegistrationRequestDTO();
            personDTO.FirstName = _request.FirstName;
            personDTO.SecondName = _request.SecondName;
            personDTO.LastName = _request.LastName;
            personDTO.Gender = _request.Gender;
            personDTO.BirthDate = _request.BirthDate;
            personDTO.ClubID = GeneralSettings.ClubID;

            _person = new Person(personDTO);
        }

        private void _InitializeSaveDTO()
        {
            _saveDTO = new PlayerRegistrationSaveDTO();
            _saveDTO.Photo = _uploadedImageUrl;
            _saveDTO.PrimaryPositionID = this.PrimaryPositionID;
            _saveDTO.SecondaryPositionID = this.SecondaryPositionID;
            _saveDTO.PreferredFootID = this.PreferredFootID;
            _saveDTO.JerseyNumber = this.JerseyNumber;
            _saveDTO.PersonID = _person.ID;
            _saveDTO.Address = this.Address;
        }

        private void _InitializeMedicalDossier()
        {
            PlayerMedicalDossierRegistrationRequestDTO medicalDTO = new PlayerMedicalDossierRegistrationRequestDTO();
            medicalDTO.BloodTypeID = _request.BloodTypeID;
            medicalDTO.Allergies = _request.Allergies;
            medicalDTO.MedicalNotes = _request.MedicalNotes;
            medicalDTO.PlayerID = this.ID;

            _medicalDossier = new PlayerMedicalDossier(medicalDTO);
        }

        private void _ValidatePrerequisites()
        {
            CategoryProvider.CheckCategoriesExistence(new[] { this.CategoryID }, GeneralSettings.ClubID).GetAwaiter().GetResult();
        }

        private bool _IsMinor()
        {
            var birthDate = _updateRequest?.BirthDate ?? _request.BirthDate;
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age < 18;
        }

        private void _AddAdultDetails()
        {
            AdultPlayerDetailsRegistrationSaveDTO dto = new AdultPlayerDetailsRegistrationSaveDTO();
            dto.Phone = _request.Phone!;
            dto.Email = _request.Email!;
            dto.PlayerID = this.ID;

            AdultPlayerDetailsProvider.AddNew(dto);
        }

        private void _AddMinorDetails()
        {
            MinorPlayerDetailsRegistrationSaveDTO dto = new MinorPlayerDetailsRegistrationSaveDTO();
            dto.CategoryID = this.CategoryID;
            dto.PaymentStatusID = 1; // Default: Pending
            dto.GuardianFullName = _request.GuardianFullName!;
            dto.GuardianPhone = _request.GuardianPhone!;
            dto.PlayerID = this.ID;

            MinorPlayerDetailsProvider.AddNew(dto);
        }

        private async Task<bool> _addNew(BlobService blobService)
        {
            _ValidatePrerequisites();

            // Upload the image BEFORE the transaction starts.
            // If the transaction later fails, we delete the blob in the catch block.
            _uploadedImageUrl = await blobService.UploadAndResizeImageAsync(
                this.Photo, AzureContainers.Players, 800, 800);

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    // Step 1: Create the Person record
                    _InitializePersonDetails();
                    if (!_person.Save()) return false;

                    // Step 2: Build save DTO and insert into players table
                    _InitializeSaveDTO();
                    this.ID = PlayerProvider.AddNew(_saveDTO, playerRegistrationResponseDTO);
                    if (this.ID == -1) return false;

                    // Step 2.5: Link the player to its category (players_categories)
                    await PlayerProvider.AddPlayerCategory(this.ID, this.CategoryID);

                    // Step 3: Create the medical dossier linked to the player
                    _InitializeMedicalDossier();
                    if (! await _medicalDossier.Save()) return false;

                    // Step 4: Create adult or minor details based on age
                    if (_IsMinor())
                    {
                        _AddMinorDetails();
                    }
                    else
                    {
                        _AddAdultDetails();
                    }

                    // The new player invalidates the cached players-by-category
                    // lists for this club, so drop them before committing.
                    _RemoveAllCache();

                    scope.Complete();
                    return true;
                }
                catch (SqlException ex) when (ex.Number == 547)
                {
                    // SQL rolls back automatically — clean up the orphaned blob
                    await blobService.DeleteBlobAsync(_uploadedImageUrl, AzureContainers.Players);
                    throw new Exception("The selected position, foot preference, or category is no longer available.");
                }
                catch
                {
                    // SQL rolls back automatically — clean up the orphaned blob
                    await blobService.DeleteBlobAsync(_uploadedImageUrl, AzureContainers.Players);
                    throw;
                }
            }
        }

        private async Task<bool> _update(BlobService blobService)
        {
            _ValidatePrerequisites();

            // Upload a replacement photo BEFORE the transaction starts. If the DB
            // work later fails, we delete the freshly uploaded blob in the catch.
            if (this.Photo != null)
            {
                _uploadedImageUrl = await blobService.UploadAndResizeImageAsync(
                    this.Photo, AzureContainers.Players, 800, 800);
            }

            try
            {
                var saveDTO = new PlayerUpdateSaveDTO
                {
                    ID = this.ID,
                    FirstName = _updateRequest.FirstName,
                    SecondName = _updateRequest.SecondName,
                    LastName = _updateRequest.LastName,
                    Gender = _updateRequest.Gender,
                    BirthDate = _updateRequest.BirthDate,
                    Photo = _uploadedImageUrl, // null => keep the current photo
                    PrimaryPositionID = this.PrimaryPositionID,
                    SecondaryPositionID = this.SecondaryPositionID,
                    PreferredFootID = this.PreferredFootID,
                    JerseyNumber = this.JerseyNumber,
                    Address = this.Address,
                    CategoryID = this.CategoryID,
                    BloodTypeID = _updateRequest.BloodTypeID,
                    Allergies = _updateRequest.Allergies,
                    MedicalNotes = _updateRequest.MedicalNotes,
                    Phone = _updateRequest.Phone,
                    Email = _updateRequest.Email,
                    GuardianFullName = _updateRequest.GuardianFullName,
                    GuardianPhone = _updateRequest.GuardianPhone,
                    IsMinor = _IsMinor()
                };

                bool success = await PlayerProvider.Update(saveDTO, GeneralSettings.ClubID);

                if (success)
                {
                    _RemoveAllCache();
                }
                else if (_uploadedImageUrl != null)
                {
                    // Player was not found for this club — remove the new blob.
                    await blobService.DeleteBlobAsync(_uploadedImageUrl, AzureContainers.Players);
                }

                return success;
            }
            catch
            {
                if (_uploadedImageUrl != null)
                {
                    await blobService.DeleteBlobAsync(_uploadedImageUrl, AzureContainers.Players);
                }
                throw;
            }
        }

        private static string _getPlayersByCategory_CacheKey(int categoryID)
        {
            return $"playersByCategory_{GeneralSettings.ClubID}_{categoryID}";
        }

        // Evicts every cached players-by-category list tracked for the current
        // club, so the next read re-queries the database with the new player.
        private static void _RemovePlayersByCategoryCacheKeys()
        {
            lock (_cacheLock)
            {
                if (!_PlayersByCategoryCacheKeys.TryGetValue(GeneralSettings.ClubID, out var keysHashset))
                {
                    return;
                }

                foreach (var key in keysHashset)
                {
                    GetCache().Remove(key);
                }

                _PlayersByCategoryCacheKeys.TryRemove(GeneralSettings.ClubID, out _);
            }
        }

        private static void _RemoveAllCache()
        {
            _RemovePlayersByCategoryCacheKeys();
            Dashboard.ClearCache();
        }

        // Returns all players in the given category for the current club.
        // Club scoping comes from the auth token via GeneralSettings.ClubID.
        public static async Task<List<PlayerByCategoryResponseDTO>> GetPlayersByCategory(int categoryId)
        {
            string PlayersByCategoryCacheKey = _getPlayersByCategory_CacheKey(categoryId);

            if (!GetCache().TryGetValue(PlayersByCategoryCacheKey, out List<PlayerByCategoryResponseDTO> list))
            {
                list = await PlayerProvider.GetPlayersByCategory(categoryId, GeneralSettings.ClubID);
                GetCache().Set(PlayersByCategoryCacheKey, list);

                lock (_cacheLock)
                {
                    var keys = _PlayersByCategoryCacheKeys.GetOrAdd(GeneralSettings.ClubID, _=> new HashSet<string>());
                    keys.Add(PlayersByCategoryCacheKey);
                }

            }

            return list;
        }



        // Returns every player in the given category that currently has no
        // active injury (used for building a call-up / lineup). Club scoping
        // comes from the auth token via GeneralSettings.ClubID.
        public static async Task<List<MatchCallUpPlayerByCategoryResponseDTO>> GetMatchCallUpPlayersByCategory(int categoryId, int matchID)
        {
            if (categoryId <= 0)
                throw new ArgumentOutOfRangeException(nameof(categoryId), "Category ID must be greater than 0.");

            if (matchID <= 0)
                throw new ArgumentOutOfRangeException(nameof(matchID), "Match ID must be greater than 0.");

            return await PlayerProvider.GetMatchCallUpPlayersByCategory(categoryId, GeneralSettings.ClubID, matchID);
        }

        // Returns the full detail of a single player (used to prefill the update
        // form). Returns null when the player does not exist for the current club.
        public static async Task<PlayerGetByIdResponseDTO> GetById(int playerId)
        {
            return await PlayerProvider.GetById(playerId, GeneralSettings.ClubID);
        }

        // Soft-deletes a player for the current club. Deactivated players vanish
        // from every squad list while their match/training history is preserved.
        public static async Task<bool> Delete(int playerId)
        {
            bool result = await PlayerProvider.SoftDelete(playerId, GeneralSettings.ClubID);

            if (result)
            {
                _RemoveAllCache();
            }

            return result;
        }

        public async Task<bool> Save(BlobService blobService)
        {
            switch (mode)
            {
                case enMode.addNew:
                    try
                    {
                        return await _addNew(blobService);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                case enMode.update:
                    return await _update(blobService);

                default:
                    return false;
            }
        }
    }
}

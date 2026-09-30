using Azure.Core;
using BusinessLayer.Services;
using BusinessLayer.Services.Azure;
using DataAccessLayer.DTOs.Club;
using DataAccessLayer.DTOs.Person;
using DataAccessLayer.DTOs.Staff;
using DataAccessLayer.DTOs.StaffCategory;
using DataAccessLayer.DTOs.StaffMedicalDossier;
using DataAccessLayer.DTOs.User;
using DataAccessLayer.Providers;
using Microsoft.Data.SqlClient;
using System.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using BusinessLayer.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace BusinessLayer
{
    public class Staff
    {
        public int ID { get; set; }
        public IFormFile Photo { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public int StaffMedicalDossierID { get; set; }
        public int PrimaryRoleID { get; set; }
        public int RoleClassificationID { get; set; }
        public int UserID { get; set; }
        public int[] Categories { get; set; }

        private StaffRegistrationRequestDTO _staffRegistrationRequestDTO = new StaffRegistrationRequestDTO();
        private Person person;
        private User user;
        private StaffMedicalDossier medicalDossier;
        private StaffRegistrationSaveDTO _staffRegistrationSaveDTO;
        private StaffCategoryRegistrationSaveDTO _staffCategoryRegistrationSaveDTO;
        public StaffRegistrationResponseDTO staffRegistrationResponseDTO = new StaffRegistrationResponseDTO();
        private string _uploadedImageUrl;
        public enum enMode
        {
            addNew = 0,
            update = 1
        }

        public enMode mode;

        // 1. Helper to get the cache safely
        private static IMemoryCache GetCache() => AppServicesHelper.ServiceProvider.GetRequiredService<IMemoryCache>();

        private readonly static ConcurrentDictionary<int, HashSet<string>> _clubStaffCacheKeys = new();

        private readonly static ConcurrentDictionary<int, HashSet<string>> _clubStaffByFilterCacheKeys = new();

        private static readonly object _cacheLock = new();


        private void _InitializeFromRegistrationRequestDTO()
        {
            this.Photo = _staffRegistrationRequestDTO.Photo;
            this.PhoneNumber = _staffRegistrationRequestDTO.PhoneNumber;
            this.Address = _staffRegistrationRequestDTO.Address;
            this.PrimaryRoleID = _staffRegistrationRequestDTO.PrimaryRoleID;
            this.RoleClassificationID = _staffRegistrationRequestDTO.RoleClassificationID;
            this.Categories = _staffRegistrationRequestDTO.CategoriesIDs;
        }


        private void _InitializeSaveDTO()
        {
            _staffRegistrationSaveDTO = new StaffRegistrationSaveDTO();
            _staffRegistrationSaveDTO.Photo = _uploadedImageUrl;
            _staffRegistrationSaveDTO.PhoneNumber = this.PhoneNumber;
            _staffRegistrationSaveDTO.Address = this.Address;
            _staffRegistrationSaveDTO.StaffMedicalDossierID = medicalDossier.ID;
            _staffRegistrationSaveDTO.PrimaryRoleID = this.PrimaryRoleID;
            _staffRegistrationSaveDTO.RoleClassificationID = this.RoleClassificationID;
            _staffRegistrationSaveDTO.UserID = user.ID;
        }
        public Staff(StaffRegistrationRequestDTO staffRegistrationRequestDTO)
        {
            _staffRegistrationRequestDTO = staffRegistrationRequestDTO;
            _InitializeFromRegistrationRequestDTO();
            mode = enMode.addNew;
        }


        private void _InitializePersonDetails()
        {
            PersonRegistrationRequestDTO personRegistrationRequestDTO = new PersonRegistrationRequestDTO();
            personRegistrationRequestDTO.FirstName = _staffRegistrationRequestDTO.FirstName;
            personRegistrationRequestDTO.SecondName = _staffRegistrationRequestDTO.SecondName;
            personRegistrationRequestDTO.LastName = _staffRegistrationRequestDTO.LastName;
            personRegistrationRequestDTO.Gender = _staffRegistrationRequestDTO.Gender;
            personRegistrationRequestDTO.BirthDate = _staffRegistrationRequestDTO.BirthDate;
            personRegistrationRequestDTO.ClubID = GeneralSettings.ClubID;

            person = new Person(personRegistrationRequestDTO);
        }

        private void _InitializeUserDetails()
        {
            UserRegistrationRequestDTO userRegistrationRequestDTO = new UserRegistrationRequestDTO();

            userRegistrationRequestDTO.Email = _staffRegistrationRequestDTO.Email;
            userRegistrationRequestDTO.Password = _staffRegistrationRequestDTO.Password;
            userRegistrationRequestDTO.PersonID = person.ID;
            userRegistrationRequestDTO.RoleID = RoleProvider.GetRoleIDByName("Staff");

            user = new User(userRegistrationRequestDTO);
        }

        private void _InitializeStaffCategoryDTO()
        {
            _staffCategoryRegistrationSaveDTO = new StaffCategoryRegistrationSaveDTO();
            _staffCategoryRegistrationSaveDTO.StaffID = this.ID;
            _staffCategoryRegistrationSaveDTO.Categories = this.Categories;
        }

        private void _InitializeMedicalDossierDetailsDetails()
        {
            StaffMedicalDossierRegistrationRequestDTO staffMedicalDossierRegistrationRequestDTO = new StaffMedicalDossierRegistrationRequestDTO();

            staffMedicalDossierRegistrationRequestDTO.BloodTypeID = _staffRegistrationRequestDTO.BloodTypeID;
            staffMedicalDossierRegistrationRequestDTO.Allergies = _staffRegistrationRequestDTO.Allergies;
            staffMedicalDossierRegistrationRequestDTO.MedicalNotes = _staffRegistrationRequestDTO.MedicalNotes;

            medicalDossier = new StaffMedicalDossier(staffMedicalDossierRegistrationRequestDTO);
        }

        private void _CheckCategoriesExistence()
        {
            CategoryProvider.CheckCategoriesExistence(Categories, GeneralSettings.ClubID);
        }

        private void _ValidatePrerequisites()
        {
            if (!PrimaryRoleProvider.DoesPrimaryRoleExist(PrimaryRoleID))
                throw new PrimaryRoleNotFoundException(PrimaryRoleID);

            if (!RoleClassificationProvider.DoesRoleClassificationExist(RoleClassificationID))
                throw new RoleClassificationNotFoundException(RoleClassificationID);

            _CheckCategoriesExistence();
        }

        private async Task<bool> _addNew(BlobService blobService)
        {
            _ValidatePrerequisites();

            // Upload the image BEFORE the transaction starts.
            // If the transaction later fails, we delete the blob in the catch block.
            if (this.Photo != null)
            {
                _uploadedImageUrl = await blobService.UploadAndResizeImageAsync(this.Photo, AzureContainers.Staff, 800, 800);
            }

            // allowing the transaction to use await and async
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    _InitializePersonDetails();
                    if (!person.Save()) return false;

                    _InitializeUserDetails();
                    if (await user.Save() != true) return false;

                    _InitializeMedicalDossierDetailsDetails();
                    if (! await medicalDossier.Save()) return false;

                    _InitializeSaveDTO();
                    this.ID = StaffProvider.AddNew(_staffRegistrationSaveDTO, staffRegistrationResponseDTO);

                    _InitializeStaffCategoryDTO();
                    StaffCategoryProvider.AddStaffCategories(_staffCategoryRegistrationSaveDTO);

                    _RemoveAllCache();

                    scope.Complete();
                    return true;
                }
                catch (SqlException ex) when (ex.Number == 547) // 547 is the SQL code for Foreign Key violation
                {
                    // SQL rolls back automatically — clean up the orphaned blob
                    await blobService.DeleteBlobAsync(_uploadedImageUrl, AzureContainers.Staff);
                    throw new Exception("The selected role or category is no longer available.");
                }
                catch
                {
                    // SQL rolls back automatically — clean up the orphaned blob
                    await blobService.DeleteBlobAsync(_uploadedImageUrl, AzureContainers.Staff);
                    throw;
                }
            }
        }

        private bool _update()
        {
            return false;
        }

        public async Task<bool> Save(BlobService blobService)
        {
            switch (mode)
            {
                case enMode.addNew:
                    try
                    {
                        // You must use 'await' here because _addNew is an async Task
                        return await _addNew(blobService);
                    }
                    catch (Exception)
                    {
                        // If you have image cleanup logic, put it here
                        throw;
                    }
                case enMode.update:
                    return _update();

                default:
                    return false;
            }
        }

        private static string _getStaffDashboardCount_CacheKey()
        {
            return $"getStaffDashboardCount_{GeneralSettings.ClubID}";
        }

        private static void _RemoveStaffDashboardCount_Cache()
        {
            GetCache().Remove(_getStaffDashboardCount_CacheKey());
        }

        public async static Task<StaffDashboardCountsResponseDTO> GetStaffDashboardCount()
        {
            string GetStaffDashboardCountCacheKey = _getStaffDashboardCount_CacheKey();

            // 1. Try to get the data from cache
            if (!GetCache().TryGetValue(GetStaffDashboardCountCacheKey, out StaffDashboardCountsResponseDTO counts))
            {
                // 2. If NOT in cache (Cache Miss), get it from the Provider (SQL)
                counts = await StaffProvider.GetDashboardCounts(GeneralSettings.ClubID);

                // 4. Save data to cache
                GetCache().Set(GetStaffDashboardCountCacheKey, counts);
            }

            return counts;
        }

        private static string _getAllStaff_CacheKey(int pageNumber, int rowsPerPage)
        {
            return $"getAllStaffListByClub_{GeneralSettings.ClubID}_p{pageNumber}_s{rowsPerPage}";
        }

        public async static Task<List<StaffGetAllResponse>> GetAllStaff(int pageNumber, int rowsPerPage)
        {
            // Create a unique cache key for this club/page/pageSize.
            // Example: staff_1_2_10
            string cacheKey = _getAllStaff_CacheKey(pageNumber, rowsPerPage);

            // Try to get the data from IMemoryCache.
            if (!GetCache().TryGetValue(cacheKey, out List<StaffGetAllResponse> list))
            {
                // Cache Miss:
                // The data isn't cached, so retrieve it from SQL Server.
                list = await StaffProvider.GetAll(
                    pageNumber,
                    rowsPerPage,
                    GeneralSettings.ClubID);

                // Store the retrieved data in IMemoryCache.
                GetCache().Set(cacheKey, list);

                // We now need to remember that this cache key belongs to this club.
                // This allows us to remove all cached pages for this club later.
                lock (_cacheLock)
                {
                    // Get the HashSet for this club.
                    // If it doesn't exist yet, create a new one.
                    var keys = _clubStaffCacheKeys.GetOrAdd(
                        GeneralSettings.ClubID,
                        _ => new HashSet<string>());

                    // Store the cache key.
                    // HashSet automatically ignores duplicates.
                    keys.Add(cacheKey);
                }
            }

            // Either the cached data or the freshly loaded data.
            return list;
        }

        private static string _getStaffByFilters_CacheKey(int pageNumber, int rowsPerPage, int? primaryRoleID, int? roleClassificationID, int[] categoryIDs)
        {

            var sortedCategories = categoryIDs?
            .OrderBy(x => x)
            .ToArray();

            string categoriesKey = sortedCategories != null && sortedCategories.Length > 0
                ? string.Join("_", sortedCategories)
                : "none";
            return $"getStaffByFilters_{GeneralSettings.ClubID}_p{pageNumber}_s{rowsPerPage}_pr{primaryRoleID}_rc{roleClassificationID}_cat{categoriesKey}";
        }

        public async static Task<List<StaffGetAllResponse>> GetStaffByFilters(int pageNumber, int rowsPerPage, int? primaryRoleID, int? roleClassificationID, int[] categoryIDs)
        {
            string GetStaffByFiltersCacheKey = _getStaffByFilters_CacheKey(pageNumber, rowsPerPage, primaryRoleID, roleClassificationID, categoryIDs);

            if (!GetCache().TryGetValue(GetStaffByFiltersCacheKey, out List<StaffGetAllResponse> list))
            {
                list = await StaffProvider.GetStaffByFilters(pageNumber, rowsPerPage, GeneralSettings.ClubID, primaryRoleID, roleClassificationID, categoryIDs);

                GetCache().Set(GetStaffByFiltersCacheKey, list);

                lock (_cacheLock)
                {
                    var keys = _clubStaffByFilterCacheKeys.GetOrAdd(GeneralSettings.ClubID, 
                    _ => new HashSet<string>() );
                    keys.Add(GetStaffByFiltersCacheKey);
                }

            }
            return list;
        }

        public async static Task<StaffGetByIdResponse> GetById(int staffId)
        {
            return await StaffProvider.GetById(staffId, GeneralSettings.ClubID);
        }

        public async static Task<bool> Update(StaffEditSaveDTO saveDTO)
        {
            if (await StaffProvider.Update(saveDTO, GeneralSettings.ClubID))
            {
                _RemoveAllCache();
                return true;
            }

            return false;
        }

        /// <summary>
        /// True when the supplied email is already used by a different account.
        /// Delegates the existence check to the shared UserProvider.IsEmailExists and
        /// excludes this staff member's own current email, so an unchanged email does
        /// not trip the duplicate check.
        /// </summary>
        public static async Task<bool> IsEmailTakenByOtherAsync(int staffId, string email)
        {
            // Email not used by anyone -> no conflict.
            if (!UserProvider.IsEmailExists(email))
                return false;

            // Email exists; treat it as a conflict only if it does not already belong
            // to this staff member (i.e. the email was actually changed).
            var existing = await GetById(staffId);
            if (existing == null)
                return false; // staff not found for this club; the update will report 404.

            return !string.Equals(existing.Email, email, StringComparison.OrdinalIgnoreCase);
        }

        private static void _RemoveClubStaffCacheKeys()
        {
            lock (_cacheLock)
            {
                if (!_clubStaffCacheKeys.TryGetValue(GeneralSettings.ClubID, out var keysHashset))
                {
                    return;
                }

                foreach (var key in keysHashset)
                {
                    GetCache().Remove(key);
                }

                _clubStaffCacheKeys.TryRemove(GeneralSettings.ClubID, out _);
            }
        }

        private static void _RemoveClubStaffByFilterCacheKeys()
        {
            lock (_cacheLock)
            {
                if (!_clubStaffByFilterCacheKeys.TryGetValue(GeneralSettings.ClubID, out var keysHashset))
                {
                    return;
                }

                foreach (var key in keysHashset)
                {
                    GetCache().Remove(key);
                }

                _clubStaffByFilterCacheKeys.TryRemove(GeneralSettings.ClubID, out _);
            }
        }


        private static void _RemoveAllCache()
        {
            _RemoveStaffDashboardCount_Cache();
            _RemoveClubStaffCacheKeys();
            _RemoveClubStaffByFilterCacheKeys();
            Dashboard.ClearCache();
        }

        public static async Task<bool> Delete(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Staff ID must be greater than zero.");
            }

            if (await StaffProvider.SoftDelete(id, GeneralSettings.ClubID))
            {
                _RemoveAllCache();
                return true;
            }

            return false;
        }

    }
}

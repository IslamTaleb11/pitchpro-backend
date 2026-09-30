using BusinessLayer.Exceptions;
using BusinessLayer.Helpers;
using DataAccessLayer.DTOs.Category;
using DataAccessLayer.DTOs.StaffCategory;
using DataAccessLayer.Providers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLayer
{
    public class Category
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public int Capacity { get; set; }
        public decimal RegistrationFee { get; set; }
        public int ClubID { get; set; }

        public enum enMode
        {
            addNew = 0,
            update = 1
        }

        public enMode mode;

        private CategoryAddRequestDTO _categoryAddRequestDTO;
        private CategoryAddSaveDTO _categoryAddSaveDTO;
        public CategoryAddResponseDTO categoryAddResponseDTO = new CategoryAddResponseDTO();

        private CategoryEditRequestDTO _categoryEditRequestDTO;
        private CategoryEditSaveDTO _categoryEditSaveDTO;

        private static IMemoryCache GetCache() => AppServicesHelper.ServiceProvider.GetRequiredService<IMemoryCache>();
        private static string _GetCacheKey;

        private void _InitializeFromRequestDTO()
        {
            this.Name = _categoryAddRequestDTO.Name;
            this.MinAge = _categoryAddRequestDTO.MinAge;
            this.MaxAge = _categoryAddRequestDTO.MaxAge;
            this.Capacity = _categoryAddRequestDTO.Capacity;
            this.RegistrationFee = _categoryAddRequestDTO.RegistrationFee;
            this.ClubID = GeneralSettings.ClubID;
        }

        private void _InitializeSaveDTO()
        {
            _categoryAddSaveDTO = new CategoryAddSaveDTO();
            _categoryAddSaveDTO.Name = this.Name;
            _categoryAddSaveDTO.MinAge = this.MinAge;
            _categoryAddSaveDTO.MaxAge = this.MaxAge;
            _categoryAddSaveDTO.Capacity = this.Capacity;
            _categoryAddSaveDTO.RegistrationFee = this.RegistrationFee;
            _categoryAddSaveDTO.ClubID = this.ClubID;
        }

        private void _InitializeEditSaveDTO()
        {
            _categoryEditSaveDTO = new CategoryEditSaveDTO();
            _categoryEditSaveDTO.ID = this.ID;
            _categoryEditSaveDTO.Name = this.Name;
            _categoryEditSaveDTO.MinAge = this.MinAge;
            _categoryEditSaveDTO.MaxAge = this.MaxAge;
            _categoryEditSaveDTO.Capacity = this.Capacity;
            _categoryEditSaveDTO.RegistrationFee = this.RegistrationFee;
            _categoryEditSaveDTO.ClubID = this.ClubID;
        }

        public Category(CategoryAddRequestDTO categoryAddRequestDTO)
        {
            _categoryAddRequestDTO = categoryAddRequestDTO;
            _InitializeFromRequestDTO();
            _InitializeSaveDTO();
            mode = enMode.addNew;
        }

        public Category(CategoryEditRequestDTO categoryEditRequestDTO)
        {
            _categoryEditRequestDTO = categoryEditRequestDTO;
            this.ID = categoryEditRequestDTO.ID;
            this.Name = categoryEditRequestDTO.Name;
            this.MinAge = categoryEditRequestDTO.MinAge;
            this.MaxAge = categoryEditRequestDTO.MaxAge;
            this.Capacity = categoryEditRequestDTO.Capacity;
            this.RegistrationFee = categoryEditRequestDTO.RegistrationFee;
            this.ClubID = GeneralSettings.ClubID;
            _InitializeEditSaveDTO();
            mode = enMode.update;
        }

        private void _Validate()
        {
            if (MinAge > MaxAge)
                throw new BaseException("Minimum age cannot be greater than maximum age.");
        }

        private static readonly ConcurrentDictionary<string, bool> _categoryCacheKeys = new ConcurrentDictionary<string, bool>();

        private static string _categoryCachePrefix()
        {
            return $"getAllCategoriesByClub_{GeneralSettings.ClubID}";
        }

        private static string _getAllCategories_CacheKey()
        {
            return _categoryCachePrefix();
        }

        private static string _getPagedCategories_CacheKey(int pageNumber, int rowsPerPage)
        {
            return $"{_categoryCachePrefix()}_p{pageNumber}_s{rowsPerPage}";
        }

        private static void _trackCategoryCacheKey(string key)
        {
            _categoryCacheKeys.TryAdd(key, true);
        }

        private static void _clearAllCategoryCache()
        {
            var cache = GetCache();

            foreach (var key in _categoryCacheKeys.Keys)
            {
                cache.Remove(key);
            }

            _categoryCacheKeys.Clear();
            Dashboard.ClearCache();
        }

        private static void _setCache<T>(string key, T value)
        {
            _trackCategoryCacheKey(key);
            GetCache().Set(key, value);
        }

        public static async Task<List<CategoryGetAllLookupResponse>> GetAllCategories()
        {
            _GetCacheKey = _getAllCategories_CacheKey();

            if (!GetCache().TryGetValue(_GetCacheKey, out List<CategoryGetAllLookupResponse> list))
            {
                list = await CategoryProvider.GetAllCategories(GeneralSettings.ClubID);
                _setCache(_GetCacheKey, list);
            }

            return list;
        }

        public static async Task<List<CategoryGetAllResponseDTO>> GetAllCategories(int pageNumber, int rowsPerPage)
        {
            _GetCacheKey = _getPagedCategories_CacheKey(pageNumber, rowsPerPage);

            if (!GetCache().TryGetValue(_GetCacheKey, out List<CategoryGetAllResponseDTO> list))
            {
                list = await CategoryProvider.GetAllCategories(GeneralSettings.ClubID, pageNumber, rowsPerPage);
                _setCache(_GetCacheKey, list);
            }

            return list;
        }

        private async Task<bool> _addNew()
        {
            _Validate();

                try
                {
                    int newId = await CategoryProvider.AddNew(_categoryAddSaveDTO, categoryAddResponseDTO);

                    if (newId > 0)
                    {
                        this.ID = newId;
                        _clearAllCategoryCache();
                        return true;
                    }

                    return false;
                }
                catch (SqlException ex) when (ex.Number == 547)
                {
                    throw new Exception("The selected club is no longer available.");
                }
                catch(Exception)
                {
                    throw;
                }
            
        }

        private async Task<bool> _update()
        {
            _Validate();

            try
            {
                bool result = await CategoryProvider.Update(_categoryEditSaveDTO);

                if (result)
                {
                    _clearAllCategoryCache();
                }

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static string _getFilteredCategories_CacheKey(
            int minAge, int maxAge, int? capacity,
            int registrationFeeMin, int registrationFeeMax,
            int minPlayers, int maxPlayers,
            int pageNumber, int pageSize)
        {
            string capacityKey = capacity.HasValue ? capacity.Value.ToString() : "null";
            return $"getFilteredCategories_{GeneralSettings.ClubID}_minA{minAge}_maxA{maxAge}_cap{capacityKey}_fee{registrationFeeMin}_{registrationFeeMax}_pl{minPlayers}_{maxPlayers}_p{pageNumber}_s{pageSize}";
        }

        public static async Task<List<CategoryFilterResponseDTO>> GetCategoriesWithFilter(
            int minAge, int maxAge, int? capacity,
            int registrationFeeMin, int registrationFeeMax,
            int minPlayers, int maxPlayers,
            int pageNumber, int pageSize)
        {
            _GetCacheKey = _getFilteredCategories_CacheKey(
                minAge, maxAge, capacity,
                registrationFeeMin, registrationFeeMax,
                minPlayers, maxPlayers,
                pageNumber, pageSize);

            if (!GetCache().TryGetValue(_GetCacheKey, out List<CategoryFilterResponseDTO> list))
            {
                list = await CategoryProvider.GetCategoriesWithFilter(
                    GeneralSettings.ClubID,
                    minAge, maxAge, capacity,
                    registrationFeeMin, registrationFeeMax,
                    minPlayers, maxPlayers,
                    pageNumber, pageSize);

                _setCache(_GetCacheKey, list);
            }

            return list;
        }

        public static async Task<bool> Delete(int categoryID)
        {
            bool result = await CategoryProvider.Delete(categoryID, GeneralSettings.ClubID);

            if (result)
            {
                _clearAllCategoryCache();
            }

            return result;
        }

        public async Task<bool> Save()
        {
            switch (mode)
            {
                case enMode.addNew:
                    try
                    {
                        return await _addNew();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                case enMode.update:
                    return await _update();
                default:
                    return false;
            }
        }

    }
}

using BusinessLayer.Exceptions;
using BusinessLayer.Helpers;
using DataAccessLayer.DTOs.PlayerInjury;
using DataAccessLayer.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace BusinessLayer
{
    public class PlayerInjury
    {
        public int ID { get; set; }
        public int PlayerMedicalDossierID { get; set; }
        public int BodyPart { get; set; }
        public int Severity { get; set; }
        public int Status { get; set; }
        public DateTime InjuryDate { get; set; }
        public DateTime? EstimatedReturnDate { get; set; }

        private readonly PlayerInjuryRegistrationRequestDTO _request;
        private PlayerInjuryRegistrationSaveDTO _saveDTO;

        public PlayerInjuryRegistrationResponseDTO playerInjuryRegistrationResponseDTO = new PlayerInjuryRegistrationResponseDTO();

        private static IMemoryCache GetCache() => AppServicesHelper.ServiceProvider.GetRequiredService<IMemoryCache>();
        private readonly static ConcurrentDictionary<int, HashSet<string>> _injuriesByCategoryCacheKeys = new();
        private static readonly object _cacheLock = new();

        public enum enMode
        {
            addNew = 0,
            update = 1
        }

        public enMode mode;

        public PlayerInjury(PlayerInjuryRegistrationRequestDTO request)
        {
            _request = request;
            _InitializeFromRequestDTO();
            mode = enMode.addNew;
        }

        private void _InitializeFromRequestDTO()
        {
            this.PlayerMedicalDossierID = _request.PlayerMedicalDossierID;
            this.BodyPart = _request.BodyPart;
            this.Severity = _request.Severity;
            this.Status = _request.Status;
            this.InjuryDate = _request.InjuryDate;
            this.EstimatedReturnDate = _request.EstimatedReturnDate;
        }

        private void _InitializeSaveDTO()
        {
            _saveDTO = new PlayerInjuryRegistrationSaveDTO();
            _saveDTO.PlayerMedicalDossierID = this.PlayerMedicalDossierID;
            _saveDTO.BodyPart = this.BodyPart;
            _saveDTO.Severity = this.Severity;
            _saveDTO.Status = this.Status;
            _saveDTO.InjuryDate = this.InjuryDate;
            _saveDTO.EstimatedReturnDate = this.EstimatedReturnDate;
        }

        // Guards the numeric codes against the canonical enums so we never persist
        // a body part / severity / status that the rest of the system can't map.
        private void _ValidatePrerequisites()
        {
            if (!Enum.IsDefined(typeof(BodyParts.enBodyPart), this.BodyPart))
                throw new InvalidInjuryDataException($"'{this.BodyPart}' is not a valid body part.");

            if (!Enum.IsDefined(typeof(Severities.enSeverity), this.Severity))
                throw new InvalidInjuryDataException($"'{this.Severity}' is not a valid severity.");

            if (!Enum.IsDefined(typeof(PlayerStatuses.enPlayerStatus), this.Status))
                throw new InvalidInjuryDataException($"'{this.Status}' is not a valid player status.");

            if (this.EstimatedReturnDate.HasValue && this.EstimatedReturnDate.Value.Date < this.InjuryDate.Date)
                throw new InvalidInjuryDataException("The estimated return date cannot be earlier than the injury date.");
        }

        private async Task<bool> _addNew()
        {
            _ValidatePrerequisites();

            _InitializeSaveDTO();
            this.ID = await PlayerInjuryProvider.AddNew(_saveDTO);

            if (this.ID == -1)
                return false;

            playerInjuryRegistrationResponseDTO.ID = this.ID;
            playerInjuryRegistrationResponseDTO.PlayerMedicalDossierID = this.PlayerMedicalDossierID;
            playerInjuryRegistrationResponseDTO.BodyPart = this.BodyPart;
            playerInjuryRegistrationResponseDTO.Severity = this.Severity;
            playerInjuryRegistrationResponseDTO.Status = this.Status;
            playerInjuryRegistrationResponseDTO.InjuryDate = this.InjuryDate;
            playerInjuryRegistrationResponseDTO.EstimatedReturnDate = this.EstimatedReturnDate;

            // The new injury invalidates the cached injuries-by-category lists
            // for this club, so drop them.
            _RemoveAllCache();

            return true;
        }

        private bool _update()
        {
            return false;
        }

        private static string _getInjuriesByCategory_CacheKey(int categoryID)
        {
            return $"injuriesByCategory_{GeneralSettings.ClubID}_{categoryID}";
        }

        // Returns all injuries for players in the given category for the current
        // club. Club scoping comes from the auth token via GeneralSettings.ClubID.
        // Numeric body-part/severity codes are mapped to display names here so the
        // DataAccessLayer stays free of the BusinessLayer enums.
        public static async Task<List<InjuryByCategoryResponseDTO>> GetInjuriesByCategory(int categoryId)
        {
            string InjuriesByCategoryCacheKey = _getInjuriesByCategory_CacheKey(categoryId);

            if (!GetCache().TryGetValue(InjuriesByCategoryCacheKey, out List<InjuryByCategoryResponseDTO> list))
            {
                list = await PlayerInjuryProvider.GetInjuriesByCategory(categoryId, GeneralSettings.ClubID);

                foreach (var injury in list)
                {
                    injury.BodyPartName = Enum.IsDefined(typeof(BodyParts.enBodyPart), injury.BodyPart)
                        ? ((BodyParts.enBodyPart)injury.BodyPart).ToString()
                        : string.Empty;

                    injury.SeverityName = Enum.IsDefined(typeof(Severities.enSeverity), injury.Severity)
                        ? ((Severities.enSeverity)injury.Severity).ToString()
                        : string.Empty;
                }

                GetCache().Set(InjuriesByCategoryCacheKey, list);

                lock (_cacheLock)
                {
                    var keys = _injuriesByCategoryCacheKeys.GetOrAdd(GeneralSettings.ClubID, _ => new HashSet<string>());
                    keys.Add(InjuriesByCategoryCacheKey);
                }
            }

            return list;
        }

        // Marks an injury as recovered (is_active = 0) for the current club.
        // Returns false when the injury doesn't exist or belongs to another club.
        public static async Task<bool> MarkAsRecovered(int injuryId)
        {
            int rowsAffected = await PlayerInjuryProvider.SetInjuryInactive(injuryId, GeneralSettings.ClubID);

            if (rowsAffected == 0)
                return false;

            // The player is no longer injured, so the cached injuries-by-category
            // lists for this club are stale — drop them.
            _RemoveAllCache();

            return true;
        }

        // Evicts every cached injuries-by-category list tracked for the current
        // club, so the next read re-queries the database with the new injury.
        private static void _RemoveInjuriesByCategoryCacheKeys()
        {
            lock (_cacheLock)
            {
                if (!_injuriesByCategoryCacheKeys.TryGetValue(GeneralSettings.ClubID, out var keysHashset))
                {
                    return;
                }

                foreach (var key in keysHashset)
                {
                    GetCache().Remove(key);
                }

                _injuriesByCategoryCacheKeys.TryRemove(GeneralSettings.ClubID, out _);
            }
        }

        private static void _RemoveAllCache()
        {
            _RemoveInjuriesByCategoryCacheKeys();
        }

        public async Task<bool> Save()
        {
            switch (mode)
            {
                case enMode.addNew:
                    if (await _addNew())
                    {
                        mode = enMode.update;
                        return true;
                    }
                    return false;

                case enMode.update:
                    return _update();

                default:
                    return false;
            }
        }
    }
}

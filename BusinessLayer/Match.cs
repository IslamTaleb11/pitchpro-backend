using BusinessLayer.Exceptions;
using DataAccessLayer.DTOs.Match;
using DataAccessLayer.Providers;

namespace BusinessLayer
{
    public class Match
    {
        public int ID { get; set; }
        public int ClubID { get; set; }
        public int CategoryID { get; set; }
        public string OpponentName { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan KickoffTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsHome { get; set; }
        public string StadiumName { get; set; }

        private readonly MatchRegistrationRequestDTO _request;
        private MatchRegistrationSaveDTO _saveDTO;

        public MatchRegistrationResponseDTO matchRegistrationResponseDTO = new MatchRegistrationResponseDTO();

        private readonly MatchEditRequestDTO _editRequest;
        private MatchEditSaveDTO _editSaveDTO;

        public enum enMode
        {
            addNew = 0,
            update = 1
        }

        public enMode mode;

        public Match(MatchRegistrationRequestDTO request)
        {
            _request = request;
            _InitializeFromRequestDTO();
            mode = enMode.addNew;
        }

        public Match(MatchEditRequestDTO editRequest)
        {
            _editRequest = editRequest;
            this.ID = editRequest.ID;
            this.ClubID = GeneralSettings.ClubID;
            this.CategoryID = editRequest.CategoryID;
            this.OpponentName = editRequest.OpponentName;
            this.Date = editRequest.Date;
            this.KickoffTime = editRequest.KickoffTime;
            this.EndTime = editRequest.EndTime;
            this.IsCompleted = false;
            this.IsHome = editRequest.IsHome;
            this.StadiumName = editRequest.StadiumName;
            _InitializeEditSaveDTO();
            mode = enMode.update;
        }

        private void _InitializeFromRequestDTO()
        {
            this.ClubID       = GeneralSettings.ClubID;
            this.CategoryID   = _request.CategoryID;
            this.OpponentName = _request.OpponentName;
            this.Date         = _request.Date;
            this.KickoffTime  = _request.KickoffTime;
            this.EndTime      = _request.EndTime;
            this.IsCompleted  = false;
            this.IsHome       = _request.IsHome;
            this.StadiumName  = _request.StadiumName;
        }

        private void _InitializeSaveDTO()
        {
            _saveDTO = new MatchRegistrationSaveDTO();
            _saveDTO.ClubID       = this.ClubID;
            _saveDTO.CategoryID   = this.CategoryID;
            _saveDTO.OpponentName = this.OpponentName;
            _saveDTO.Date         = this.Date;
            _saveDTO.KickoffTime  = this.KickoffTime;
            _saveDTO.EndTime      = this.EndTime;
            _saveDTO.IsCompleted  = this.IsCompleted;
            _saveDTO.IsHome       = this.IsHome;
            _saveDTO.StadiumName  = this.StadiumName;
        }

        private void _InitializeEditSaveDTO()
        {
            _editSaveDTO = new MatchEditSaveDTO();
            _editSaveDTO.ID = this.ID;
            _editSaveDTO.ClubID = this.ClubID;
            _editSaveDTO.CategoryID = this.CategoryID;
            _editSaveDTO.OpponentName = this.OpponentName;
            _editSaveDTO.Date = this.Date;
            _editSaveDTO.KickoffTime = this.KickoffTime;
            _editSaveDTO.EndTime = this.EndTime;
            _editSaveDTO.IsCompleted = this.IsCompleted;
            _editSaveDTO.IsHome = this.IsHome;
            _editSaveDTO.StadiumName = this.StadiumName;
        }

        private void _ValidatePrerequisites()
        {
            CategoryProvider.CheckCategoriesExistence(new[] { this.CategoryID }, GeneralSettings.ClubID).GetAwaiter().GetResult();
        }

        private bool _addNew()
        {
            _ValidatePrerequisites();
            _InitializeSaveDTO();

            this.ID = MatchProvider.AddNew(_saveDTO, matchRegistrationResponseDTO);

            if (this.ID != -1)
                Dashboard.ClearCache();

            return this.ID != -1;
        }

        private async Task<bool> _update()
        {
            _ValidatePrerequisites();
            bool result = await MatchProvider.Update(_editSaveDTO);
            if (result)
                Dashboard.ClearCache();
            return result;
        }

        // Sets the result (club and opponent scores) for a match. Club scoping
        // comes from the auth token. Throws KeyNotFoundException when the match
        // doesn't exist or doesn't belong to the current club.
        public static async Task SetResultAsync(int matchId, MatchResultSaveDTO dto)
        {
            int clubId = GeneralSettings.ClubID;

            bool exists = await MatchProvider.Exists(matchId, clubId);
            if (!exists)
                throw new KeyNotFoundException("Match not found or you don't have permission to set its result.");

            await MatchProvider.SetResult(matchId, clubId, dto);
            ClearDashboardCache();
        }

        // Marks a match as completed. Club scoping comes from the auth token.
        // Throws KeyNotFoundException when the match doesn't exist or doesn't
        // belong to the current club.
        public static async Task SetCompletedAsync(int matchId)
        {
            int clubId = GeneralSettings.ClubID;

            bool exists = await MatchProvider.Exists(matchId, clubId);
            if (!exists)
                throw new KeyNotFoundException("Match not found or you don't have permission to complete it.");

            await MatchProvider.SetCompleted(matchId, clubId);
            ClearDashboardCache();
        }

        public async Task<bool> SaveAsync()
        {
            switch (mode)
            {
                case enMode.addNew:
                    if (_addNew())
                    {
                        mode = enMode.update;
                        return true;
                    }
                    return false;

                case enMode.update:
                    return await _update();

                default:
                    return false;
            }
        }

        private static void ClearDashboardCache()
        {
            Dashboard.ClearCache();
        }
    }
}

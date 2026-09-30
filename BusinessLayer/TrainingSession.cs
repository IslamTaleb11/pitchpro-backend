using BusinessLayer.Exceptions;
using DataAccessLayer.DTOs.TrainingSession;
using DataAccessLayer.Providers;

namespace BusinessLayer
{
    public class TrainingSession
    {
        public int ID { get; set; }
        public int ClubID { get; set; }
        public int CategoryID { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Location { get; set; }
        public int SessionTypeID { get; set; }

        private readonly TrainingSessionRegistrationRequestDTO _request;
        private TrainingSessionRegistrationSaveDTO _saveDTO;

        public TrainingSessionRegistrationResponseDTO trainingSessionRegistrationResponseDTO = new TrainingSessionRegistrationResponseDTO();

        private readonly TrainingSessionEditRequestDTO _editRequest;
        private TrainingSessionEditSaveDTO _editSaveDTO;

        public enum enMode
        {
            addNew = 0,
            update = 1
        }

        public enMode mode;

        public TrainingSession(TrainingSessionRegistrationRequestDTO request)
        {
            _request = request;
            _InitializeFromRequestDTO();
            mode = enMode.addNew;
        }

        public TrainingSession(TrainingSessionEditRequestDTO editRequest)
        {
            _editRequest = editRequest;
            this.ID = editRequest.ID;
            this.ClubID = GeneralSettings.ClubID;
            this.CategoryID = editRequest.CategoryID;
            this.Date = editRequest.Date;
            this.StartTime = editRequest.StartTime;
            this.EndTime = editRequest.EndTime;
            this.Location = editRequest.Location;
            this.SessionTypeID = editRequest.SessionTypeID;
            _InitializeEditSaveDTO();
            mode = enMode.update;
        }

        private void _InitializeFromRequestDTO()
        {
            this.ClubID        = GeneralSettings.ClubID;
            this.CategoryID    = _request.CategoryID;
            this.Date          = _request.Date;
            this.StartTime     = _request.StartTime;
            this.EndTime       = _request.EndTime;
            this.Location      = _request.Location;
            this.SessionTypeID = _request.SessionTypeID;
        }

        private void _InitializeSaveDTO()
        {
            _saveDTO = new TrainingSessionRegistrationSaveDTO();
            _saveDTO.ClubID        = this.ClubID;
            _saveDTO.CategoryID    = this.CategoryID;
            _saveDTO.Date          = this.Date;
            _saveDTO.StartTime     = this.StartTime;
            _saveDTO.EndTime       = this.EndTime;
            _saveDTO.Location      = this.Location;
            _saveDTO.SessionTypeID = this.SessionTypeID;
        }

        private void _InitializeEditSaveDTO()
        {
            _editSaveDTO = new TrainingSessionEditSaveDTO();
            _editSaveDTO.ID = this.ID;
            _editSaveDTO.ClubID = this.ClubID;
            _editSaveDTO.CategoryID = this.CategoryID;
            _editSaveDTO.Date = this.Date;
            _editSaveDTO.StartTime = this.StartTime;
            _editSaveDTO.EndTime = this.EndTime;
            _editSaveDTO.Location = this.Location;
            _editSaveDTO.SessionTypeID = this.SessionTypeID;
        }

        private void _ValidatePrerequisites()
        {
            CategoryProvider.CheckCategoriesExistence(new[] { this.CategoryID }, GeneralSettings.ClubID).GetAwaiter().GetResult();
        }

        private bool _addNew()
        {
            _ValidatePrerequisites();

            _InitializeSaveDTO();

            this.ID = TrainingSessionProvider.AddNew(_saveDTO, trainingSessionRegistrationResponseDTO);

            if (this.ID != -1)
                Dashboard.ClearCache();

            return this.ID != -1;
        }

        private async Task<bool> _update()
        {
            _ValidatePrerequisites();
            bool result = await TrainingSessionProvider.Update(_editSaveDTO);
            if (result)
                Dashboard.ClearCache();
            return result;
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
        // Returns all players in a category who have no active injury, with an
        // isAttended flag for the given training session. Throws
        // CategoryNotFoundException if the category doesn't belong to the club.
        public static async Task<List<TrainingSessionPlayerByCategoryResponseDTO>> GetPlayersByCategoryAsync(
            int categoryId, int trainingSessionId)
        {
            int clubId = GeneralSettings.ClubID;

            await CategoryProvider.CheckCategoriesExistence(new[] { categoryId }, clubId);

            return await TrainingSessionProvider.GetPlayersByCategory(categoryId, clubId, trainingSessionId);
        }

        // Returns paginated completed training sessions for the given category
        // within the current club. Throws CategoryNotFoundException if the
        // category doesn't belong to the club.
        public static async Task<(List<TrainingSessionByCategoryResponseDTO> Data, int TotalCount)> GetCompletedByCategoryAsync(
            int categoryId, int page, int pageSize)
        {
            int clubId = GeneralSettings.ClubID;

            await CategoryProvider.CheckCategoriesExistence(new[] { categoryId }, clubId);

            return await TrainingSessionProvider.GetCompletedByCategory(categoryId, clubId, page, pageSize);
        }

        // Marks a training session as completed. Throws InvalidCallUpDataException
        // if the session doesn't belong to the club or doesn't exist.
        public static async Task CompleteAsync(int trainingSessionId)
        {
            int clubId = GeneralSettings.ClubID;

            bool success = await TrainingSessionProvider.SetCompleted(trainingSessionId, clubId);
            if (!success)
                throw new InvalidCallUpDataException(
                    "Training session not found or you don't have permission to complete it.");

            Dashboard.ClearCache();
        }

        // Returns all non-completed training sessions for the given category within
        // the current club. Throws CategoryNotFoundException if the category doesn't
        // belong to the club.
        public static async Task<List<TrainingSessionByCategoryResponseDTO>> GetByCategoryAsync(int categoryId)
        {
            int clubId = GeneralSettings.ClubID;

            await CategoryProvider.CheckCategoriesExistence(new[] { categoryId }, clubId);

            return await TrainingSessionProvider.GetByCategory(categoryId, clubId);
        }
    }
}

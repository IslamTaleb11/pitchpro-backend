using DataAccessLayer.DTOs.Schedule;
using DataAccessLayer.Providers;

namespace BusinessLayer
{
    public class Schedule
    {
        public static async Task<List<UpcomingScheduleItemDTO>> GetUpcomingSchedule(
            int pageNumber, 
            int rowsPerPage,
            string eventClassification = null,
            string category = null)
        {
            return await ScheduleProvider.GetUpcomingSchedule(GeneralSettings.ClubID, pageNumber, rowsPerPage, eventClassification, category);
        }
    }
}

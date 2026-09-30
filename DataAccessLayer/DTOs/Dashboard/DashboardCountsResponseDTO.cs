namespace DataAccessLayer.DTOs.Dashboard
{
    public class DashboardCountsResponseDTO
    {
        public int TotalCategories { get; set; }
        public int TotalPlayers { get; set; }
        public int TotalActiveStaff { get; set; }
        public int TotalMatches { get; set; }
        public int TotalCompletedMatches { get; set; }
        public int TotalUpcomingMatches { get; set; }
        public int TotalTrainingSessions { get; set; }
        public int TotalCompletedTrainingSessions { get; set; }
        public int TotalUpcomingTrainingSessions { get; set; }
        public int TotalCoachingStaff { get; set; }
        public int TotalMedicalStaff { get; set; }
        public int TotalFitnessStaff { get; set; }
    }
}

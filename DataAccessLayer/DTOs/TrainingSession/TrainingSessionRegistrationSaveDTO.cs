namespace DataAccessLayer.DTOs.TrainingSession
{
    public class TrainingSessionRegistrationSaveDTO
    {
        public int ClubID { get; set; }
        public int CategoryID { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Location { get; set; }
        public int SessionTypeID { get; set; }
    }
}

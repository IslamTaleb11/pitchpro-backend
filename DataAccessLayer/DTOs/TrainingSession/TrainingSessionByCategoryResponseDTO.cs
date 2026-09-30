namespace DataAccessLayer.DTOs.TrainingSession
{
    public class TrainingSessionByCategoryResponseDTO
    {
        public int ID { get; set; }
        public int ClubID { get; set; }
        public int CategoryID { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Location { get; set; }
        public string SessionTypeName { get; set; }
    }
}

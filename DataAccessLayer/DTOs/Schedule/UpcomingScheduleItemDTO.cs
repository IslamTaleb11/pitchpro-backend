namespace DataAccessLayer.DTOs.Schedule
{
    public class UpcomingScheduleItemDTO
    {
        public string EventType { get; set; }   // "Match" | "Training"
        public int EventId { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
        public string Title { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public bool? IsHome { get; set; }
    }
}

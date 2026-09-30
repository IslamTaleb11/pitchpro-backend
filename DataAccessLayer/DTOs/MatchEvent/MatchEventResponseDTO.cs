namespace DataAccessLayer.DTOs.MatchEvent
{
    public class MatchEventResponseDTO
    {
        public int ID { get; set; }
        public int MatchAttendanceID { get; set; }
        public int EventTypeID { get; set; }
        public byte EventAt { get; set; }
    }
}
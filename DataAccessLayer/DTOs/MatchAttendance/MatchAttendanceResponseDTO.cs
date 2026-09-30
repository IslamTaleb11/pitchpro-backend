namespace DataAccessLayer.DTOs.MatchAttendance
{
    public class MatchAttendanceResponseDTO
    {
        public int ID { get; set; }
        public int MatchCallUpPlayerID { get; set; }
        public bool Status { get; set; }
        public System.DateTime RecordedAt { get; set; }
    }
}

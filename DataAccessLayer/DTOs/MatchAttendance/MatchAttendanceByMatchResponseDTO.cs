namespace DataAccessLayer.DTOs.MatchAttendance
{
    public class MatchAttendanceByMatchResponseDTO
    {
        public int PlayerID { get; set; }
        public string PlayerName { get; set; }
        public string JerseyNumber { get; set; }
        public string PositionName { get; set; }
        public string PlayerImage { get; set; }
        public bool? Attended { get; set; }
    }
}

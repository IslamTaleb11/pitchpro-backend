namespace DataAccessLayer.DTOs.Player
{
    // Read model for players that belong to a category and currently have no
    // active injury, used when building a call-up / lineup. The person name is
    // flattened from the persons table, and the primary position name is pulled
    // from primary_positions.
    public class MatchCallUpPlayerByCategoryResponseDTO
    {
        public int PlayerID { get; set; }
        public string PlayerName { get; set; }
        public string PlayerImage { get; set; } 
        public string JerseyNumber { get; set; }
        public string PositionName { get; set; }
        public bool IsAlreadyAttended { get; set; }
        public bool IsAbsent { get; set; }
        public int? MatchAttendanceID { get; set; }
    }
}

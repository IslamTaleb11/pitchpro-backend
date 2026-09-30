namespace DataAccessLayer.DTOs.MatchAttendance
{
    // Flattened, validated values handed to the provider for the INSERT/UPDATE.
    // Only match_callup_players_id, status and recorded_at are persisted; club
    // scoping is guaranteed upstream by the ownership check in the business layer.
    public class MatchAttendanceSaveDTO
    {
        public int? MatchCallUpPlayerID { get; set; }
        public bool Status { get; set; }
        public System.DateTime RecordedAt { get; set; }
    }
}

namespace DataAccessLayer.DTOs.MatchCallUpPlayer
{
    // Flattened, validated values handed to the provider for the INSERT.
    // Only match_id and player_id are persisted; club scoping is guaranteed
    // upstream by the ownership checks in the business layer.
    public class MatchCallUpPlayerSaveDTO
    {
        public int MatchID { get; set; }
        public int PlayerID { get; set; }
    }
}

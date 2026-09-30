using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.DTOs.MatchCallUpPlayerId
{
    // Request payload to get a MatchCallUpPlayerID by player_id and match_id.
    // The club is derived server-side from the auth token (GeneralSettings.ClubID); only
    // the match id and player id are supplied. Club scoping is enforced in the provider,
    // so a player_id that belongs to a different club produces no result rather than
    // leaking another club's call-up data.
    public class MatchCallUpPlayerIdRequestDTO
    {
        [Required(ErrorMessage = "Match ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please provide a valid match id.")]
        public int MatchID { get; set; }

        [Required(ErrorMessage = "Player ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please provide a valid player id.")]
        public int PlayerID { get; set; }
    }
}
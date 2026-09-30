using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.DTOs.MatchCallUpPlayer
{
    // Payload sent by the client when adding one or more players to a match
    // call-up. PlayerIDs is the list of players to enrol. The club is derived
    // server-side from the auth token (GeneralSettings.ClubID); CategoryID is
    // supplied so the server can verify the match and every player belong to it.
    public class MatchCallUpPlayerRegistrationRequestDTO
    {
        [Required(ErrorMessage = "At least one player is required.")]
        [MinLength(1, ErrorMessage = "At least one player is required.")]
        public List<int> PlayerIDs { get; set; }

        [Required(ErrorMessage = "Match is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid match.")]
        public int MatchID { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid category.")]
        public int CategoryID { get; set; }
    }
}

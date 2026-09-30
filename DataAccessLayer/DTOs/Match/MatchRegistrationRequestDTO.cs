using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.DTOs.Match
{
    public class MatchRegistrationRequestDTO
    {
        // Club is derived server-side from the auth token (GeneralSettings.ClubID),
        // never from the client, so it is intentionally not part of this DTO.

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid Category.")]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Opponent name must be between 2 and 255 characters.")]
        public string OpponentName { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [CustomValidation(typeof(MatchRegistrationRequestDTO), nameof(ValidateFutureDate))]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan KickoffTime { get; set; }

        [Required]
        [CustomValidation(typeof(MatchRegistrationRequestDTO), nameof(ValidateEndTimeAfterKickoff))]
        public TimeSpan EndTime { get; set; }

        [Required]
        public bool IsHome { get; set; }

        [StringLength(255)]
        public string StadiumName { get; set; }

        public static ValidationResult ValidateFutureDate(DateTime date, ValidationContext context)
        {
            return date.Date >= DateTime.Today
                ? ValidationResult.Success
                : new ValidationResult("Match date must be today or in the future.");
        }

        public static ValidationResult ValidateEndTimeAfterKickoff(TimeSpan endTime, ValidationContext context)
        {
            var instance = (MatchRegistrationRequestDTO)context.ObjectInstance;
            return endTime > instance.KickoffTime
                ? ValidationResult.Success
                : new ValidationResult("End time must be after kickoff time.");
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.DTOs.Match
{
    public class MatchEditRequestDTO
    {
        [Required(ErrorMessage = "Match ID is required.")]
        public int ID { get; set; }

        [Required(ErrorMessage = "Club is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid Club.")]
        public int ClubID { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid Category.")]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Opponent name is required.")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Opponent name must be between 2 and 255 characters.")]
        public string OpponentName { get; set; }

        [Required(ErrorMessage = "Match date is required.")]
        [DataType(DataType.Date)]
        [CustomValidation(typeof(MatchEditRequestDTO), nameof(ValidateFutureDate))]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Kickoff time is required.")]
        public TimeSpan KickoffTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        [CustomValidation(typeof(MatchEditRequestDTO), nameof(ValidateEndTimeAfterKickoff))]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Please specify if this is a home or away match.")]
        public bool IsHome { get; set; }

        [StringLength(255, ErrorMessage = "Stadium name cannot exceed 255 characters.")]
        public string StadiumName { get; set; }

        public static ValidationResult ValidateFutureDate(DateTime date, ValidationContext context)
        {
            return date.Date >= DateTime.Today
                ? ValidationResult.Success
                : new ValidationResult("Match date must be today or in the future.");
        }

        public static ValidationResult ValidateEndTimeAfterKickoff(TimeSpan endTime, ValidationContext context)
        {
            var instance = (MatchEditRequestDTO)context.ObjectInstance;
            return endTime > instance.KickoffTime
                ? ValidationResult.Success
                : new ValidationResult("End time must be after kickoff time.");
        }
    }
}
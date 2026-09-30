using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.DTOs.TrainingSession
{
    public class TrainingSessionRegistrationRequestDTO
    {

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid Category.")]
        public int CategoryID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [CustomValidation(typeof(TrainingSessionRegistrationRequestDTO), nameof(ValidateFutureDate))]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        [CustomValidation(typeof(TrainingSessionRegistrationRequestDTO), nameof(ValidateEndTimeAfterStartTime))]
        public TimeSpan EndTime { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 2)]
        public string Location { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid Session Type.")]
        public int SessionTypeID { get; set; }

        public static ValidationResult ValidateFutureDate(DateTime date, ValidationContext context)
        {
            return date.Date >= DateTime.Today
                ? ValidationResult.Success
                : new ValidationResult("Training date must be today or in the future.");
        }

        public static ValidationResult ValidateEndTimeAfterStartTime(TimeSpan endTime, ValidationContext context)
        {
            var instance = (TrainingSessionRegistrationRequestDTO)context.ObjectInstance;
            return endTime > instance.StartTime
                ? ValidationResult.Success
                : new ValidationResult("End time must be after start time.");
        }
    }
}

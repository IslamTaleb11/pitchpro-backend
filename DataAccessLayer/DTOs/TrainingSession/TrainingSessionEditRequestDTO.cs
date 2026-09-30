using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.DTOs.TrainingSession
{
    public class TrainingSessionEditRequestDTO
    {
        [Required]
        public int ID { get; set; }

        [Required(ErrorMessage = "Club ID is required.")]
        public int ClubID { get; set; }

        [Required(ErrorMessage = "Category ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid category.")]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Session Type ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid session type.")]
        public int SessionTypeID { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        [DataType(DataType.Date)]
        [CustomValidation(typeof(TrainingSessionEditRequestDTO), nameof(ValidateFutureDate))]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Start time is required.")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        [CustomValidation(typeof(TrainingSessionEditRequestDTO), nameof(ValidateEndTimeAfterStartTime))]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Location must be between 2 and 255 characters.")]
        public string Location { get; set; }

        public static ValidationResult ValidateFutureDate(DateTime date, ValidationContext context)
        {
            return date.Date >= DateTime.Today
                ? ValidationResult.Success
                : new ValidationResult("Training date must be today or in the future.");
        }

        public static ValidationResult ValidateEndTimeAfterStartTime(TimeSpan endTime, ValidationContext context)
        {
            var instance = (TrainingSessionEditRequestDTO)context.ObjectInstance;
            return endTime > instance.StartTime
                ? ValidationResult.Success
                : new ValidationResult("End time must be after start time.");
        }
    }
}

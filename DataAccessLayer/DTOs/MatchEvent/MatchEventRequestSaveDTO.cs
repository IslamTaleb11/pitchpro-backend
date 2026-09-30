using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.DTOs.MatchEvent
{
    public class MatchEventRequestSaveDTO
    {
        [Required(ErrorMessage = "Match attendance ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Match attendance ID must be a positive number.")]
        public int MatchAttendanceID { get; set; }

        [Required(ErrorMessage = "Event type ID is required.")]
        [Range(1, 255, ErrorMessage = "Event type ID must be between 1 and 255.")]
        public int EventTypeID { get; set; }

        [Required(ErrorMessage = "Event at is required.")]
        [Range(0, 150, ErrorMessage = "Event at must be between 0 and 150.")]
        public byte EventAt { get; set; }
    }
}
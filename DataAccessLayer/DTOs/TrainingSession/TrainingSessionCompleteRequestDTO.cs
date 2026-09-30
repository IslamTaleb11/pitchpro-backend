using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.DTOs.TrainingSession
{
    public class TrainingSessionCompleteRequestDTO
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid training session ID.")]
        public int TrainingSessionID { get; set; }
    }
}

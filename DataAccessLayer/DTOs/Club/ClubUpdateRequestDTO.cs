using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.DTOs.Club
{
    public class ClubUpdateRequestDTO
    {
        [Required(ErrorMessage = "The Club ID must be provided for an update.")]
        [Range(1, int.MaxValue, ErrorMessage = "A valid Club ID must be greater than 0.")]
        public int ID { get; set; }


        [Required(ErrorMessage = "The club name is mandatory.")]
        [StringLength(255, ErrorMessage = "The club name cannot exceed 255 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "A club crest or logo is required.")]
        public IFormFile Crest { get; set; }

        [Required(ErrorMessage = "Please provide the club's primary identity color.")]
        [StringLength(255, ErrorMessage = "The color name/code must be under 255 characters.")]
        public string PrimaryIdentityColor { get; set; }

        [Required(ErrorMessage = "A contact phone number is required.")]
        [StringLength(255, ErrorMessage = "The contact number is too long.")]
        [Phone(ErrorMessage = "Please enter a valid phone number format.")]
        public string ContactNumber { get; set; }

    }
}

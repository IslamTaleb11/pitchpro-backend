using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;

namespace DataAccessLayer.DTOs.Club
{
    public class ClubRegistrationRequest
    {
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

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(255, ErrorMessage = "First name cannot exceed 255 characters.")]
        public string FirstName { get; set; }

        // Optional: No [Required] attribute here
        [DefaultValue(null)]
        [StringLength(255, ErrorMessage = "Second name cannot exceed 255 characters.")]
        public string? SecondName { get; set; } = null;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(255, ErrorMessage = "Last name cannot exceed 255 characters.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Gender selection is mandatory.")]
        public bool Gender { get; set; } // true = Male, false = Female (or however your logic is set)

        [Required(ErrorMessage = "Birth date is required.")]
        [DataType(DataType.Date)]
        [Range(typeof(DateTime), "1900-01-01", "2026-04-28", ErrorMessage = "Please enter a valid birth date between 1900 and today.")]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(450)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(255, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        // Optional: Add regex for strong passwords (uppercase, number, special char)
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Password must have an uppercase letter, a lowercase letter, and a number.")]
        public string Password { get; set; }

    }
}

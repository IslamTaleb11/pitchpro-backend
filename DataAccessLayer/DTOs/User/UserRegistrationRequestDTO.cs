using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.User
{
    public class UserRegistrationRequestDTO
    {
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

        [Required(ErrorMessage = "Role ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid Role.")]
        public int RoleID { get; set; } // Changed from bool to int

        [Required(ErrorMessage = "Person ID is required.")]
        public int PersonID { get; set; }
    }
}

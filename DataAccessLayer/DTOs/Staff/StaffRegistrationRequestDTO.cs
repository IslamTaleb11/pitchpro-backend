using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
namespace DataAccessLayer.DTOs.Staff
{
    public class StaffRegistrationRequestDTO
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(255, MinimumLength = 2)]
        public string FirstName { get; set; }

        [StringLength(255)]
        public string? SecondName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(255, MinimumLength = 2)]
        public string LastName { get; set; }

        [Required]
        public bool Gender { get; set; }

        [Required]
        [DataType(DataType.Date)]
        // You might want a custom attribute to check if they are at least 18
        public DateTime BirthDate { get; set; }


        [Required]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(450)]
        public string Email { get; set; }

        [Required]
        [StringLength(255, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Password must have an uppercase letter, a lowercase letter, and a number.")]
        public string Password { get; set; }
        
        [Required]
        public IFormFile Photo { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        // Regex to ensure it contains only digits, +, -, and spaces
        [RegularExpression(@"^(\+?\d{1,4}[\s-]?)?\(?\d{3}\)?[\s-]?\d{3}[\s-]?\d{4}$", ErrorMessage = "Phone number must be a valid international or local format")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")] // 255 is way too long for a phone number
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Address must be between 10 and 500 characters")]
        public string Address { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid Primary Role must be selected")]
        public int PrimaryRoleID { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid Role Classification must be selected")]
        public int RoleClassificationID { get; set; }

        [Required(ErrorMessage = "You must assign at least one category")]
        [MinLength(1, ErrorMessage = "At least one category must be assigned")]
        // Prevents sending an array with null or 0 values
        public int[] CategoriesIDs { get; set; }

        [Required]
        [Range(1, 8, ErrorMessage = "Please select a valid Blood Type (1-8)")]
        public int BloodTypeID { get; set; }

        [StringLength(1000, ErrorMessage = "Allergies description is too long")]
        public string? Allergies { get; set; }

        [StringLength(2000, ErrorMessage = "Medical notes cannot exceed 2000 characters")]
        public string? MedicalNotes { get; set; }
    }
}
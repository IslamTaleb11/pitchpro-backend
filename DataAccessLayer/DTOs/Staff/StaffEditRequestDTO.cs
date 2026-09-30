using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
namespace DataAccessLayer.DTOs.Staff
{
    public class StaffEditRequestDTO : IValidatableObject
    {
        [Required]
        public int ID { get; set; }

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
        public DateTime BirthDate { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(450)]
        public string Email { get; set; }

        public IFormFile? Photo { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [RegularExpression(@"^(\+?\d{1,4}[\s-]?)?\(?\d{3}\)?[\s-]?\d{3}[\s-]?\d{4}$", ErrorMessage = "Phone number must be a valid international or local format")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
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
        public int[] CategoriesIDs { get; set; }

        [Required]
        [Range(1, 8, ErrorMessage = "Please select a valid Blood Type (1-8)")]
        public int BloodTypeID { get; set; }

        [StringLength(1000, ErrorMessage = "Allergies description is too long")]
        public string? Allergies { get; set; }

        [StringLength(2000, ErrorMessage = "Medical notes cannot exceed 2000 characters")]
        public string? MedicalNotes { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Reject birth dates that lie in the future.
            if (BirthDate.Date > DateTime.Today)
            {
                yield return new ValidationResult(
                    "Birth date cannot be in the future.",
                    new[] { nameof(BirthDate) });
            }
        }
    }
}

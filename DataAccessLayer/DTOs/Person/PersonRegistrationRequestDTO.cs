using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.Person
{
    public class PersonRegistrationRequestDTO
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(255, ErrorMessage = "First name cannot exceed 255 characters.")]
        public string FirstName { get; set; }

        public int ClubID { get; set; }


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

    }
}

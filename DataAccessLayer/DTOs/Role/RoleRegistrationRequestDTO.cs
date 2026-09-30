using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.Role
{
    public class RoleRegistrationRequestDTO
    {

        [Required(ErrorMessage = "Role name is required.")]
        [StringLength(255, MinimumLength = 3, ErrorMessage = "Role name must be between 3 and 255 characters.")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Role name can only contain letters and spaces.")]
        public string Name { get; set; }
    }
}

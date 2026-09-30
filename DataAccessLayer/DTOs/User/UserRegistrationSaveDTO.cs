using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.User
{
    public class UserRegistrationSaveDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public int RoleID { get; set; }
        public int PersonID { get; set; }
        public string EmailVerificationTokenHash { get; set; }
        public DateTime? EmailVerificationTokenExpiresAt { get; set; }
    }
}

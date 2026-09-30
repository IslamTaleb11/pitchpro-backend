using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.User
{
    public class UserFindResponseDTO
    {
        public int ID { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int RoleID { get; set; }
        public int PersonID { get; set; }
        public string Role { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }
    }
}

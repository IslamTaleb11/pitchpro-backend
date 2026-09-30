using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.Staff
{
    public class StaffRegistrationSaveDTO
    {
        public string Photo { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public int StaffMedicalDossierID { get; set; }
        public int PrimaryRoleID { get; set; }
        public int RoleClassificationID { get; set; }
        public int UserID { get; set; }

    }
}

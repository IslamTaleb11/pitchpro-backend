using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.Staff
{
    public class StaffEditSaveDTO
    {
        public int ID { get; set; }

        // Person details
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string LastName { get; set; }
        public bool Gender { get; set; }
        public DateTime BirthDate { get; set; }

        // User details
        public string Email { get; set; }

        // Staff details
        // Photo holds the already-uploaded blob URL, or null when the photo is unchanged.
        public string Photo { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public int PrimaryRoleID { get; set; }
        public int RoleClassificationID { get; set; }
        public int[] CategoriesIDs { get; set; }

        // Medical dossier
        public int BloodTypeID { get; set; }
        public string Allergies { get; set; }
        public string MedicalNotes { get; set; }
    }
}

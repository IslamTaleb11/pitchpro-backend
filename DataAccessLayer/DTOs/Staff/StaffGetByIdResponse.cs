using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.Staff
{
    public class StaffGetByIdResponse
    {
        public int ID { get; set; }
        public string Photo { get; set; }

        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string LastName { get; set; }

        public bool? Gender { get; set; }
        public DateTime? BirthDate { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }
        public string Address { get; set; }

        public int PrimaryRoleID { get; set; }
        public int RoleClassificationID { get; set; }

        // Categories attached to the staff (IDs)
        public int[] CategoriesIDs { get; set; }

        // Medical dossier
        public int? StaffMedicalDossierID { get; set; }
        public int? BloodTypeID { get; set; }
        public string Allergies { get; set; }
        public string MedicalNotes { get; set; }
    }
}

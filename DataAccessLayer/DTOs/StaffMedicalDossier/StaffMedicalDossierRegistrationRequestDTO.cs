using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.StaffMedicalDossier
{
    public class StaffMedicalDossierRegistrationRequestDTO
    {
        public int BloodTypeID { get; set; }
        public string? Allergies { get; set; }
        public string? MedicalNotes { get; set; }
    }
}

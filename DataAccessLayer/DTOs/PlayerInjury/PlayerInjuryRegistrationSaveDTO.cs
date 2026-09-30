using System;

namespace DataAccessLayer.DTOs.PlayerInjury
{
    // Flattened, validated values handed to the provider for the INSERT.
    public class PlayerInjuryRegistrationSaveDTO
    {
        public int PlayerMedicalDossierID { get; set; }
        public int BodyPart { get; set; }
        public int Severity { get; set; }
        public int Status { get; set; }
        public DateTime InjuryDate { get; set; }
        public DateTime? EstimatedReturnDate { get; set; }
    }
}

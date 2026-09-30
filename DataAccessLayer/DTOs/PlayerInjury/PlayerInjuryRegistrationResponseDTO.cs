using System;

namespace DataAccessLayer.DTOs.PlayerInjury
{
    public class PlayerInjuryRegistrationResponseDTO
    {
        public int ID { get; set; }
        public int PlayerMedicalDossierID { get; set; }
        public int BodyPart { get; set; }
        public int Severity { get; set; }
        public int Status { get; set; }
        public DateTime InjuryDate { get; set; }
        public DateTime? EstimatedReturnDate { get; set; }
    }
}

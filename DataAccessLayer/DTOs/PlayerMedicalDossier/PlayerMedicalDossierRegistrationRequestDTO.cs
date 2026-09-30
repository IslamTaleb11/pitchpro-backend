namespace DataAccessLayer.DTOs.PlayerMedicalDossier
{
    public class PlayerMedicalDossierRegistrationRequestDTO
    {
        public int BloodTypeID { get; set; }
        public string? Allergies { get; set; }
        public string? MedicalNotes { get; set; }
        public int PlayerID { get; set; }
    }
}

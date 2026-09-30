namespace DataAccessLayer.DTOs.Player
{
    // Data handed from the Business Layer to PlayerProvider.Update. Photo is the
    // new image URL when a replacement was uploaded, or null to keep the existing one.
    public class PlayerUpdateSaveDTO
    {
        public int ID { get; set; }

        // Identity (persons)
        public string FirstName { get; set; }
        public string? SecondName { get; set; }
        public string LastName { get; set; }
        public bool Gender { get; set; }
        public DateTime BirthDate { get; set; }

        // Technical profile (players)
        public string? Photo { get; set; }
        public int PrimaryPositionID { get; set; }
        public int? SecondaryPositionID { get; set; }
        public int PreferredFootID { get; set; }
        public string JerseyNumber { get; set; }
        public string Address { get; set; }
        public int CategoryID { get; set; }

        public int BloodTypeID { get; set; }
        public string? Allergies { get; set; }
        public string? MedicalNotes { get; set; }

        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? GuardianFullName { get; set; }
        public string? GuardianPhone { get; set; }

        public bool IsMinor { get; set; }
    }
}

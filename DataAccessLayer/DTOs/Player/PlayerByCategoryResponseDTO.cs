namespace DataAccessLayer.DTOs.Player
{
    // Lightweight read model for listing players that belong to a category.
    public class PlayerByCategoryResponseDTO
    {
        public int PlayerID { get; set; }
        public string FullName { get; set; }
        public string JerseyNumber { get; set; }
        public int MedicalDossierID { get; set; }
    }
}

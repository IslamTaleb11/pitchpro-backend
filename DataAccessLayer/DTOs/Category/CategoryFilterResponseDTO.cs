namespace DataAccessLayer.DTOs.Category
{
    public class CategoryFilterResponseDTO
    {
        public string Name { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public decimal RegistrationFee { get; set; }
        public int Capacity { get; set; }
        public int TotalPlayers { get; set; }
    }
}

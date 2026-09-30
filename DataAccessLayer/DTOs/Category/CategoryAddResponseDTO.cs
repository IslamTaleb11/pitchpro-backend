using System;

namespace DataAccessLayer.DTOs.Category
{
    public class CategoryAddResponseDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public int Capacity { get; set; }
        public decimal RegistrationFee { get; set; }
        public int ClubID { get; set; }
    }
}

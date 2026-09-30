using Microsoft.AspNetCore.Http;

namespace DataAccessLayer.DTOs.Club
{
    public class ClubRegistrationResponseDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Crest { get; set; }
        public string PrimaryIdentityColor { get; set; }
        public string ContactNumber { get; set; }
    }
}

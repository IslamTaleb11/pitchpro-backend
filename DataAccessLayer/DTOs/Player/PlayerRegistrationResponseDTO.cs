namespace DataAccessLayer.DTOs.Player
{
    public class PlayerRegistrationResponseDTO
    {
        public int ID { get; set; }
        public string Photo { get; set; }
        public int PrimaryPositionID { get; set; }
        public int SecondaryPositionID { get; set; }
        public int PreferredFootID { get; set; }
        public string JerseyNumber { get; set; }
        public int PersonID { get; set; }
    }
}

namespace DataAccessLayer.DTOs.Player
{
    public class PlayerByCategorySimpleResponseDTO
    {
        public int PlayerID { get; set; }
        public string FullName { get; set; }
        public string JerseyNumber { get; set; }
        public string PositionName { get; set; }
        public string PlayerImage { get; set; }
    }
}

namespace DataAccessLayer.DTOs.Player
{
    public class AvailablePlayerByCategoryResponseDTO
    {
        public int PlayerID { get; set; }
        public string PlayerName { get; set; }
        public string JerseyNumber { get; set; }
        public string PositionName { get; set; }
        public bool IsCalled { get; set; }
    }
}

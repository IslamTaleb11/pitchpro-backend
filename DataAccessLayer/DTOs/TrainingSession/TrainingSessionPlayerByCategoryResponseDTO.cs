namespace DataAccessLayer.DTOs.TrainingSession
{
    public class TrainingSessionPlayerByCategoryResponseDTO
    {
        public int PlayerID { get; set; }
        public string PlayerName { get; set; }
        public string JerseyNumber { get; set; }
        public string PositionName { get; set; }
        public string PlayerImage { get; set; }
        public bool IsAttended { get; set; }
    }
}

namespace DataAccessLayer.DTOs.Match
{
    public class MatchRegistrationSaveDTO
    {
        public int ClubID { get; set; }
        public int CategoryID { get; set; }
        public string OpponentName { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan KickoffTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsHome { get; set; }
        public string StadiumName { get; set; }
    }
}

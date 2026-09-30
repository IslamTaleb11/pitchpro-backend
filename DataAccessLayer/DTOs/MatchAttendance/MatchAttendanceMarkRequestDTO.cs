using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.DTOs.MatchAttendance
{
    public class MatchAttendanceMarkRequestDTO
    {
        public int MatchID { get; set; }

        public Dictionary<int, bool> PlayersAttendance { get; set; } = new Dictionary<int, bool>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.DTOs.MatchAttendance
{
    public class MatchAttendanceResetRequestDTO
    {
        public int MatchID { get; set; }

        [Required, MinLength(1)]
        public List<int> PlayerIDs { get; set; }
    }
}

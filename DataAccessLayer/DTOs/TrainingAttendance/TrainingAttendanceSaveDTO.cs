using System.Collections.Generic;

namespace DataAccessLayer.DTOs.TrainingAttendance
{
    public class TrainingAttendanceSaveRequestDTO
    {
        public int TrainingSessionID { get; set; }
        public Dictionary<int, bool> PlayersAttendance { get; set; } = new Dictionary<int, bool>();
    }
}

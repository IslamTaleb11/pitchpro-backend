using System;

namespace DataAccessLayer.DTOs.TrainingAttendance
{
    public class TrainingAttendanceInsertDTO
    {
        public int PlayerID { get; set; }
        public int TrainingSessionID { get; set; }
        public bool Status { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}

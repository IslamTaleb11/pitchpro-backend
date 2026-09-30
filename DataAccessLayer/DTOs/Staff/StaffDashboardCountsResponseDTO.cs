using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.Staff
{
    public class StaffDashboardCountsResponseDTO
    {
        public int TotalActiveStaff { get; set; }
        public int TotalCoachingStaff { get; set; }
        public int TotalMedicalStaff { get; set; }
        public int TotalFitnessStaff { get; set; }

    }
}

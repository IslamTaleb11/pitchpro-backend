using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.StaffCategory
{
    public class StaffCategoryRegistrationSaveDTO
    {
        public int StaffID { get; set; }
        public int[] Categories { get; set; }


    }
}

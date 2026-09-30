using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.RoleClassification
{
    public class RoleClassificationGetAllResponseDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int StaffPrimaryRoleID { get; set; }
    }
}

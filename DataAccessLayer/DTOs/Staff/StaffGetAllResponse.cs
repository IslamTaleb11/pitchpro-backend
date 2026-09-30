using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.Staff
{
    public class StaffGetAllResponse
    {
        public int ID { get; set; }
        public string Photo { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PrimaryRoleName { get; set; }
        public string RoleClassificationName { get; set; }
        public string[] CategoriesNames { get; set; }

    }
}

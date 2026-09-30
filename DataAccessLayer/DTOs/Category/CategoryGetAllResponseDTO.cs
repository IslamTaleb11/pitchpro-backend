using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.StaffCategory
{
    public class CategoryGetAllResponseDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public int Capacity { get; set; }
        public decimal RegistrationFee { get; set; }
        public int ClubID { get; set; }
    }
}

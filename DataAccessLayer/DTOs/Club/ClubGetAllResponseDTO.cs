using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.Club
{
    public class ClubGetAllResponseDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Crest { get; set; }
        public string PrimaryIdentityColor { get; set; }
        public string ContactNumber { get; set; }
    }
}

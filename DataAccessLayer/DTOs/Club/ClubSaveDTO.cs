using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.Club
{
    public class ClubSaveDTO
    {
        public string Name { get; set; }
        public string Crest { get; set; }
        public string PrimaryIdentityColor { get; set; }
        public string ContactNumber { get; set; }
    }
}

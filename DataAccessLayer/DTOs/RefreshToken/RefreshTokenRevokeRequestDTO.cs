using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.RefreshToken
{
    public class RefreshTokenRevokeRequestDTO
    {
        public string TokenHash { get; set; }
        public DateTime RevokedAt { get; set; }
        public int? ReplacedByTokenID { get; set; }
        public string RevokedByIP { get; set; }
        public string DeviceName { get; set; }
        public int RevokedReason { get; set; }


    }
}

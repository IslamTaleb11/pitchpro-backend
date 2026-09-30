using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs.RefreshToken
{
    public class RefreshTokenRegisterDTO
    {
        public int UserID { get; set; }

        public string TokenHash { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public int? ReplacedByTokenID { get; set; }

        public string? CreatedByIP { get; set; }

        public string? RevokedByIP { get; set; }

        public string? DeviceName { get; set; }

        public int? RevokedReason { get; set; }
    }
}

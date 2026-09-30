using BusinessLayer.Helpers;
using BusinessLayer;
using DataAccessLayer.DTOs.RefreshToken;
using PitchProAPI.Helpers;


namespace PitchProAPI.Services
{
    public class RefreshTokenService
    {
        public static async Task<RefreshToken> CreateRefreshTokenObject(int UserID, string RefreshToken, 
            string? CreatedByIP = null, 
            string? DeviceName = null, DateTime? RevokedAt = null, int? ReplacedByTokenID = null, string? RevokedByIP = null
            , int? RevokedReason = null)
        {
            RefreshTokenRegisterDTO dto = new RefreshTokenRegisterDTO();
            dto.UserID = UserID;
            dto.TokenHash = GeneralHelper.ComputeSha256Hash(RefreshToken);
            dto.CreatedAt = DateTime.UtcNow;
            dto.ExpiresAt = DateTime.UtcNow.AddDays(7);
            dto.CreatedByIP = CreatedByIP;
            dto.DeviceName = DeviceName;

            RefreshToken refreshTokenObject = new RefreshToken(dto);

            return refreshTokenObject;

        }
    }
}

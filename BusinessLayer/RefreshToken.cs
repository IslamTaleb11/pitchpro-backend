using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer;
using DataAccessLayer.DTOs.RefreshToken;
using DataAccessLayer.Providers;

namespace BusinessLayer
{
    public class RefreshToken
    {
        public int ID { get; set; }

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

        private RefreshTokenRegisterSaveDTO _refreshTokenRegisterSaveDTO;

        public enum enMode
        {
            addNew = 0,
            update = 1
        }
        public enMode mode = enMode.update;

        private void _initializeRefreTokenRegisterDTO(RefreshTokenRegisterDTO dto)
        {
            this.UserID = dto.UserID;
            this.TokenHash = dto.TokenHash;
            this.CreatedAt = dto.CreatedAt;
            this.ExpiresAt = dto.ExpiresAt;
            this.RevokedAt = dto.RevokedAt;
            this.ReplacedByTokenID = dto.ReplacedByTokenID;
            this.CreatedByIP = dto.CreatedByIP;
            this.RevokedByIP = dto.RevokedByIP;
            this.DeviceName = dto.DeviceName;
            this.RevokedReason = dto.RevokedReason;
        }

        private void _initializeRefreTokenRegisterSaveDTO()
        {
            _refreshTokenRegisterSaveDTO = new RefreshTokenRegisterSaveDTO();
            _refreshTokenRegisterSaveDTO.UserID = this.UserID;
            _refreshTokenRegisterSaveDTO.TokenHash = this.TokenHash;
            _refreshTokenRegisterSaveDTO.CreatedAt = this.CreatedAt;
            _refreshTokenRegisterSaveDTO.ExpiresAt = this.ExpiresAt;
            _refreshTokenRegisterSaveDTO.RevokedAt = this.RevokedAt;
            _refreshTokenRegisterSaveDTO.ReplacedByTokenID = this.ReplacedByTokenID;
            _refreshTokenRegisterSaveDTO.CreatedByIP = this.CreatedByIP;
            _refreshTokenRegisterSaveDTO.RevokedByIP = this.RevokedByIP;
            _refreshTokenRegisterSaveDTO.DeviceName = this.DeviceName;
            _refreshTokenRegisterSaveDTO.RevokedReason = this.RevokedReason;

        }

        public RefreshToken(RefreshTokenRegisterDTO refreshTokenRegisterDTO)
        {
            _initializeRefreTokenRegisterDTO(refreshTokenRegisterDTO);
            _initializeRefreTokenRegisterSaveDTO();
            mode = enMode.addNew;
        }

        private async Task<bool> _addNew()
        {
            this.ID = await RefreshTokenProvider.AddNew(_refreshTokenRegisterSaveDTO);

            return this.ID != -1;
        }


        public async Task<bool> save()
        {
            switch(mode)
            {
                case enMode.addNew:
                    return await _addNew();

                case enMode.update:

                    break;
            }
            return false;
        }


        public static async Task<RefreshTokenFindResponseDTO> FindByHashedToken(string hashedToken)
        {
            return await RefreshTokenProvider.GetByRefreshedHashedToken(hashedToken);
        }

        public static async Task<bool> RevokeRefreshToken(RefreshTokenRevokeRequestDTO dto)
        {
            return await RefreshTokenProvider.RevokeRefreshToken(dto);
        }

    }
}

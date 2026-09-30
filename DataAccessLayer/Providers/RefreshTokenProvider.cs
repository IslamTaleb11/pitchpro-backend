using DataAccessLayer.DTOs.RefreshToken;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class RefreshTokenProvider
    {
        public static async Task<int> AddNew(RefreshTokenRegisterSaveDTO dto)
        {
            const string query = @"
        INSERT INTO RefreshTokens
        (
            UserID,
            TokenHash,
            CreatedAt,
            ExpiresAt,
            RevokedAt,
            ReplacedByTokenID,
            CreatedByIP,
            RevokedByIP,
            RevokedReason,
            DeviceName
        )
        VALUES
        (
            @UserID,
            @TokenHash,
            @CreatedAt,
            @ExpiresAt,
            @RevokedAt,
            @ReplacedByTokenID,
            @CreatedByIP,
            @RevokedByIP,
            @RevokedReason,
            @DeviceName
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);
    ";

            await using SqlConnection conn =
                new SqlConnection(DataAccessSettings.connectionString);

            await using SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@UserID", dto.UserID);
            cmd.Parameters.AddWithValue("@TokenHash", dto.TokenHash);
            cmd.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);
            cmd.Parameters.AddWithValue("@ExpiresAt", dto.ExpiresAt);

            cmd.Parameters.AddWithValue("@RevokedAt",
                (object?)dto.RevokedAt ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@ReplacedByTokenID",
                (object?)dto.ReplacedByTokenID ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@CreatedByIP",
                (object?)dto.CreatedByIP ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@RevokedByIP",
                (object?)dto.RevokedByIP ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@RevokedReason",
                (object?)dto.RevokedReason ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@DeviceName",
                (object?)dto.DeviceName ?? DBNull.Value);

            try
            {
                await conn.OpenAsync();

                object? result = await cmd.ExecuteScalarAsync();

                return result != null
                    ? Convert.ToInt32(result)
                    : -1;
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public static async Task<RefreshTokenFindResponseDTO?> GetByRefreshedHashedToken(string hashedToken)
        {
            const string query = @"
        SELECT
            ID,
            UserID,
            TokenHash,
            CreatedAt,
            ExpiresAt,
            RevokedAt,
            ReplacedByTokenID,
            CreatedByIP,
            RevokedByIP,
            RevokedReason,
            DeviceName
        FROM RefreshTokens
        WHERE TokenHash = @TokenHash;
    ";

            await using SqlConnection conn =
                new SqlConnection(DataAccessSettings.connectionString);

            await using SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@TokenHash", hashedToken);

            try
            {
                await conn.OpenAsync();

                await using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new RefreshTokenFindResponseDTO
                    {
                        ID = reader.GetInt32(reader.GetOrdinal("ID")),
                        UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                        TokenHash = reader.GetString(reader.GetOrdinal("TokenHash")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        ExpiresAt = reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
                        RevokedAt = reader.IsDBNull(reader.GetOrdinal("RevokedAt"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("RevokedAt")),
                        ReplacedByTokenID = reader.IsDBNull(reader.GetOrdinal("ReplacedByTokenID"))
                            ? null
                            : reader.GetInt32(reader.GetOrdinal("ReplacedByTokenID")),
                        CreatedByIP = reader.IsDBNull(reader.GetOrdinal("CreatedByIP"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("CreatedByIP")),
                        RevokedByIP = reader.IsDBNull(reader.GetOrdinal("RevokedByIP"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("RevokedByIP")),
                        RevokedReason = reader.IsDBNull(reader.GetOrdinal("RevokedReason"))
                            ? null
                            : reader.GetInt16(reader.GetOrdinal("RevokedReason")),
                        DeviceName = reader.IsDBNull(reader.GetOrdinal("DeviceName"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("DeviceName"))
                    };
                }

                return null;
            }
            catch
            {
                throw;
            }
        }

        public static async Task<bool> RevokeRefreshToken(RefreshTokenRevokeRequestDTO dto)
        {
            const string query = @"
                UPDATE RefreshTokens
                SET
                    RevokedAt = @RevokedAt,
                    ReplacedByTokenID = @ReplacedByTokenID,
                    RevokedByIP = @RevokedByIP,
                    RevokedReason = @RevokedReason
                WHERE
                    TokenHash = @TokenHash
                    AND RevokedAt IS NULL;
            ";

            await using SqlConnection conn =
                new SqlConnection(DataAccessSettings.connectionString);

            await using SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@TokenHash", dto.TokenHash);
            cmd.Parameters.AddWithValue("@RevokedAt", dto.RevokedAt);
            cmd.Parameters.AddWithValue("@ReplacedByTokenID",
                (object?)dto.ReplacedByTokenID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RevokedByIP", dto.RevokedByIP);
            cmd.Parameters.AddWithValue("@RevokedReason", dto.RevokedReason);

            try
            {
                await conn.OpenAsync();

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                return rowsAffected > 0;
            }
            catch
            {
                throw;
            }
        }
    }
}

using DataAccessLayer.DTOs.PlayerInjury;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class PlayerInjuryProvider
    {
        // Inserts a new row into player_injuries and returns the new identity.
        // body_part / severity / status are TINYINT enum codes; estimated_return_date
        // is optional (TBD at intake) and written as NULL when not provided.
        public static async Task<int> AddNew(PlayerInjuryRegistrationSaveDTO saveDTO)
        {
            int newId = -1;

            string sql = @"INSERT INTO player_injuries
                               (player_medical_dossier_id, body_part, severity, status, InjuryDate, estimated_return_date)
                           VALUES
                               (@PlayerMedicalDossierID, @BodyPart, @Severity, @Status, @InjuryDate, @EstimatedReturnDate);
                           SELECT CAST(scope_identity() AS int);";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@PlayerMedicalDossierID", SqlDbType.Int).Value = saveDTO.PlayerMedicalDossierID;
                    command.Parameters.Add("@BodyPart", SqlDbType.TinyInt).Value = saveDTO.BodyPart;
                    command.Parameters.Add("@Severity", SqlDbType.TinyInt).Value = saveDTO.Severity;
                    command.Parameters.Add("@Status", SqlDbType.TinyInt).Value = saveDTO.Status;
                    command.Parameters.Add("@InjuryDate", SqlDbType.Date).Value = saveDTO.InjuryDate;
                    command.Parameters.Add("@EstimatedReturnDate", SqlDbType.Date).Value =
                        (object?)saveDTO.EstimatedReturnDate ?? DBNull.Value;

                    try
                    {
                        await connection.OpenAsync();

                        var result = await command.ExecuteScalarAsync();
                        if (result != null)
                        {
                            newId = Convert.ToInt32(result);
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while adding a new player injury.");
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }

            return newId;
        }

        public static async Task<int> CountActiveInjuriesByDossier(int playerMedicalDossierID)
        {
            const string sql = @"SELECT COUNT(*)
                                 FROM player_injuries
                                 WHERE player_medical_dossier_id = @PlayerMedicalDossierID
                                   AND is_active = 1;";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@PlayerMedicalDossierID", SqlDbType.Int).Value = playerMedicalDossierID;

                    await connection.OpenAsync();

                    var result = await command.ExecuteScalarAsync();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        // Returns every injury for players in the given category within a club.
        // Walks player_injuries -> players_medical_dossiers -> players -> persons
        // and joins players_categories/categories for the category filter + name.
        // body_part / severity are returned as numeric codes; the business layer
        // maps them to display names. Club scoping comes from the auth token.
        public static async Task<List<InjuryByCategoryResponseDTO>> GetInjuriesByCategory(int categoryId, int clubId)
        {
            var list = new List<InjuryByCategoryResponseDTO>();

            string sql = @"SELECT pi.id AS injuryId,
                                  CONCAT(pr.first_name, ' ', pr.second_name, ' ', pr.last_name) AS fullName,
                                  pl.photo AS playerImage,
                                  c.name AS categoryName,
                                  pi.body_part,
                                  pi.severity,
                                  pi.InjuryDate,
                                  pi.estimated_return_date
                           FROM player_injuries pi
                           INNER JOIN players_medical_dossiers md ON pi.player_medical_dossier_id = md.id
                           INNER JOIN players pl ON md.player_id = pl.id
                           INNER JOIN persons pr ON pl.person_id = pr.id
                           INNER JOIN players_categories pc ON pl.id = pc.player_id
                           INNER JOIN categories c ON pc.category_id = c.id
                           WHERE pc.category_id = @CategoryID
                             AND pr.club_id = @ClubID
                             AND pi.is_active = 1;";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryId;
                    command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                    try
                    {
                        await connection.OpenAsync();

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            int injuryIdColumn = reader.GetOrdinal("injuryId");
                            int fullNameColumn = reader.GetOrdinal("fullName");
                            int playerImageColumn = reader.GetOrdinal("playerImage");
                            int categoryNameColumn = reader.GetOrdinal("categoryName");
                            int bodyPartColumn = reader.GetOrdinal("body_part");
                            int severityColumn = reader.GetOrdinal("severity");
                            int injuryDateColumn = reader.GetOrdinal("InjuryDate");
                            int estimatedReturnColumn = reader.GetOrdinal("estimated_return_date");

                            while (await reader.ReadAsync())
                            {
                                list.Add(new InjuryByCategoryResponseDTO
                                {
                                    ID = reader.GetInt32(injuryIdColumn),
                                    PlayerName = reader.GetString(fullNameColumn),
                                    PlayerImage = reader.IsDBNull(playerImageColumn) ? string.Empty : reader.GetString(playerImageColumn),
                                    CategoryName = reader.GetString(categoryNameColumn),
                                    BodyPart = reader.GetByte(bodyPartColumn),
                                    Severity = reader.GetByte(severityColumn),
                                    InjuryDate = reader.GetDateTime(injuryDateColumn),
                                    EstimatedReturnDate = reader.IsDBNull(estimatedReturnColumn)
                                        ? (DateTime?)null
                                        : reader.GetDateTime(estimatedReturnColumn)
                                });
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while loading injuries by category.", ex);
                    }
                }
            }

            return list;
        }

        // Marks an injury as no longer active (is_active = 0) — i.e. the player
        // has recovered. Club scoping is enforced through the person link so a
        // club can only deactivate its own injuries. Returns the rows affected
        // (0 when the injury doesn't exist or belongs to another club).
        public static async Task<int> SetInjuryInactive(int injuryId, int clubId)
        {
            int rowsAffected = 0;

            string sql = @"UPDATE pi
                           SET pi.is_active = 0
                           FROM player_injuries pi
                           INNER JOIN players_medical_dossiers md ON pi.player_medical_dossier_id = md.id
                           INNER JOIN players pl ON md.player_id = pl.id
                           INNER JOIN persons pr ON pl.person_id = pr.id
                           WHERE pi.id = @InjuryID
                             AND pr.club_id = @ClubID
                             AND pi.is_active = 1;";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@InjuryID", SqlDbType.Int).Value = injuryId;
                    command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                    try
                    {
                        await connection.OpenAsync();
                        rowsAffected = await command.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while updating the injury status.", ex);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }

            return rowsAffected;
        }
    }
}

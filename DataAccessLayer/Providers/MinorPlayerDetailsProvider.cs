using DataAccessLayer.DTOs.MinorPlayerDetails;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataAccessLayer.Providers
{
    public class MinorPlayerDetailsProvider
    {
        public static int AddNew(MinorPlayerDetailsRegistrationSaveDTO dto)
        {
            int newId = -1;

            string sql = @"INSERT INTO minor_players_details (guardian_full_name, guardian_phone, player_id)
                           VALUES (@GuardianFullName, @GuardianPhone, @PlayerID);
                           SELECT CAST(scope_identity() AS int);";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@GuardianFullName", SqlDbType.NVarChar, 500).Value = dto.GuardianFullName;
                    command.Parameters.Add("@GuardianPhone", SqlDbType.NVarChar, 255).Value = dto.GuardianPhone;
                    command.Parameters.Add("@PlayerID", SqlDbType.Int).Value = dto.PlayerID;

                    try
                    {
                        connection.Open();

                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            newId = Convert.ToInt32(result);
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while adding minor player details.", ex);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }

            return newId;
        }
    }
}

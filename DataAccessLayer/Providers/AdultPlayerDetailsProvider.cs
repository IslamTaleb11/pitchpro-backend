using DataAccessLayer.DTOs.AdultPlayerDetails;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataAccessLayer.Providers
{
    public class AdultPlayerDetailsProvider
    {
        public static int AddNew(AdultPlayerDetailsRegistrationSaveDTO dto)
        {
            int newId = -1;

            string sql = @"INSERT INTO adult_players_details (phone, email, player_id)
                           VALUES (@Phone, @Email, @PlayerID);
                           SELECT CAST(scope_identity() AS int);";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@Phone", SqlDbType.NVarChar, 255).Value = dto.Phone;
                    command.Parameters.Add("@Email", SqlDbType.NVarChar, 1000).Value = dto.Email;
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
                        throw new Exception("Database error occurred while adding adult player details.", ex);
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

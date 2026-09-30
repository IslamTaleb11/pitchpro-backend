using DataAccessLayer.DTOs.BloodType;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class BloodTypeProvider
    {
        public static async Task<bool> DoesBloodTypeExist(int id)
        {
            // Inline SQL query instead of a stored procedure
            string query = "SELECT TOP 1 1 FROM blood_types WHERE id = @ID";

            using (SqlConnection conn = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // We only need one parameter now
                    cmd.Parameters.AddWithValue("@ID", id);

                    try
                    {
                        await conn.OpenAsync();

                        // ExecuteScalarAsync returns the '1' if found, or null if not found
                        object result = await cmd.ExecuteScalarAsync();

                        return result != null;
                    }
                    catch (Exception ex)
                    {
                        // For a startup, good logging here is a lifesaver
                        Console.WriteLine("Database Error: " + ex.Message);
                        return false;
                    }
                }
            }
        }

        public static async Task<List<BloodTypeGetAllResponseDTO>> GetAllBloodTypes()
        {
            var list = new List<BloodTypeGetAllResponseDTO>();

            try
            {
                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                {
                    const string sql = "SELECT ID, name FROM Blood_types";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        await conn.OpenAsync();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            // Performance Tip: Get column positions once before the loop
                            int idColumn = reader.GetOrdinal("ID");
                            int nameColumn = reader.GetOrdinal("name");

                            while (await reader.ReadAsync())
                            {
                                list.Add(new BloodTypeGetAllResponseDTO
                                {
                                    // Strict casting since we know the DB won't return null
                                    ID = reader.GetInt32(idColumn),
                                    Name = reader.GetString(nameColumn)
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // Log the technical details, but throw a clean message
                throw new Exception("Database connectivity issue while loading blood types.", ex);
            }

            return list;
        }
    }
}

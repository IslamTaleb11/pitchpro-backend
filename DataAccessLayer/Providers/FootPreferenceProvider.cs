using DataAccessLayer.DTOs.FootPreference;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class FootPreferenceProvider
    {
        public static bool DoesFootPreferenceExist(int id)
        {
            string query = "SELECT TOP 1 1 FROM foot_preferences WHERE id = @ID";

            using (SqlConnection conn = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);

                    try
                    {
                        conn.Open();
                        object result = cmd.ExecuteScalar();
                        return result != null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Database Error: " + ex.Message);
                        return false;
                    }
                }
            }
        }

        public static async Task<List<FootPreferenceGetAllResponseDTO>> GetAllFootPreferences()
        {
            var list = new List<FootPreferenceGetAllResponseDTO>();

            try
            {
                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                {
                    const string sql = "SELECT id, name FROM foot_preferences";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        await conn.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            int idColumn = reader.GetOrdinal("id");
                            int nameColumn = reader.GetOrdinal("name");

                            while (await reader.ReadAsync())
                            {
                                list.Add(new FootPreferenceGetAllResponseDTO
                                {
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
                throw new Exception("Database connectivity issue while loading foot preferences.", ex);
            }

            return list;
        }
    }
}

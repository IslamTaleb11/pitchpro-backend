using DataAccessLayer.DTOs.PrimaryPosition;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class PrimaryPositionProvider
    {
        public static bool DoesPrimaryPositionExist(int id)
        {
            string query = "SELECT TOP 1 1 FROM primary_positions WHERE id = @ID";

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

        public static async Task<List<PrimaryPositionGetAllResponseDTO>> GetAllPrimaryPositions()
        {
            var list = new List<PrimaryPositionGetAllResponseDTO>();

            try
            {
                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                {
                    const string sql = "SELECT id, code, name FROM primary_positions";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        await conn.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            int idColumn = reader.GetOrdinal("id");
                            int codeColumn = reader.GetOrdinal("code");
                            int nameColumn = reader.GetOrdinal("name");

                            while (await reader.ReadAsync())
                            {
                                list.Add(new PrimaryPositionGetAllResponseDTO
                                {
                                    ID = reader.GetInt32(idColumn),
                                    Code = reader.GetString(codeColumn),
                                    Name = reader.GetString(nameColumn)
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Database connectivity issue while loading primary positions.", ex);
            }

            return list;
        }
    }
}

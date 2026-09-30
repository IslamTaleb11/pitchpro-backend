using DataAccessLayer.DTOs.PrimaryRole;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class PrimaryRoleProvider
    {
        public static bool DoesPrimaryRoleExist(int id)
        {
            using (SqlConnection conn = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_DoesPrimaryRoleExist", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID", id);

                    conn.Open();
                    // ExecuteScalar returns the first column of the first row
                    object result = cmd.ExecuteScalar();

                    return result != null; // If we got a 1, it exists!
                }
            }
        }


        public static async Task<List<PrimaryRoleGetAllResponseDTO>> GetAllPrimaryRoles()
        {
            var list = new List<PrimaryRoleGetAllResponseDTO>();

            try
            {
                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                {
                    // Using the table name and columns from your SQL string
                    const string sql = "SELECT id, name FROM staff_primary_roles";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        await conn.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            // High-performance Ordinal lookup
                            int idColumn = reader.GetOrdinal("id");
                            int nameColumn = reader.GetOrdinal("name");

                            while (await reader.ReadAsync())
                            {
                                list.Add(new PrimaryRoleGetAllResponseDTO
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
                // Log locally and throw a clean message for the API
                throw new Exception("Database error while fetching primary roles.", ex);
            }

            return list;
        }





    }
}

using DataAccessLayer.DTOs.RoleClassification;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class RoleClassificationProvider
    {
        public static bool DoesRoleClassificationExist(int id)
        {
            using (SqlConnection conn = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_DoesRoleClassificationExist", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID", id);

                    conn.Open();

                    // ExecuteScalar returns the first column of the first row (the '1' from our SQL)
                    object result = cmd.ExecuteScalar();

                    // If result is null, the ID wasn't found
                    return result != null;
                }
            }
        }



        public static async Task<List<RoleClassificationGetAllResponseDTO>> GetAllRoleClassifications()
        {
            var list = new List<RoleClassificationGetAllResponseDTO>();

            try
            {
                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                {
                    const string sql = "SELECT id, name, staff_primary_role_id FROM staff_roles_classifications";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        await conn.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            // Performance: Map ordinals once before looping
                            int idCol = reader.GetOrdinal("id");
                            int nameCol = reader.GetOrdinal("name");
                            int primaryRoleCol = reader.GetOrdinal("staff_primary_role_id");

                            while (await reader.ReadAsync())
                            {
                                list.Add(new RoleClassificationGetAllResponseDTO
                                {
                                    ID = reader.GetInt32(idCol),
                                    Name = reader.GetString(nameCol),
                                    // Including the foreign key for frontend filtering
                                    StaffPrimaryRoleID = reader.GetInt32(primaryRoleCol)
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // Log ex.Message internally
                throw new Exception("Database error while fetching classifications.", ex);
            }

            return list;
        }
    }
}

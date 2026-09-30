using DataAccessLayer.DTOs.Club;
using DataAccessLayer.DTOs.Role;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class RoleProvider
    {
        public static int AddNew(RoleRegistrationSaveDTO roleSaveDTO, RoleRegistrationResponseDTO roleRegistrationResponseDTO)
        {
            // We use 'int' as the return type to capture the newly created ID (Identity)
            int newId = -1;

            // 1. T-SQL Query with Output clause to get the new ID
            string sql = @"INSERT INTO Roles (Name) 
                           VALUES (@Name);
                           SELECT CAST(scope_identity() AS int);";

            // 2. Wrap in 'using' blocks to ensure connections are closed automatically
            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    // 3. Define parameters with explicit SqlDbTypes to prevent SQL Injection
                    command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = roleSaveDTO.Name;

                    try
                    {
                        connection.Open();

                        // 4. Execute and get the new Identity ID
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            newId = Convert.ToInt32(result);

                            roleRegistrationResponseDTO.ID = newId;
                            roleRegistrationResponseDTO.Name = roleSaveDTO.Name;
                        }
                    }
                    catch (SqlException ex)
                    {
                        // In a 3-tier app, we log the error and re-throw it 
                        // so the Business Layer can decide how to tell the user.
                        throw new Exception("Database error occurred while adding a new club.", ex);
                    }
                    finally
                    {
                        // The 'using' block handles this, but closing manually is extra safe.
                        connection.Close();
                    }
                }
            }

            return newId;
        }


        public static bool DoesRoleExist(int id)
        {
            bool exists = false;

            using (SqlConnection conn = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_DoesRoleExist", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID", id);

                    // Setup the Output Parameter
                    SqlParameter outputParam = new SqlParameter("@Exists", SqlDbType.Bit)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    exists = (bool)outputParam.Value;
                }
            }
            return exists;
        }

        public static int GetRoleIDByName(string roleName)
        {
            // The plain SQL query
            string query = "SELECT id FROM roles WHERE name = @RoleName";
            using (SqlConnection conn = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Still using parameters for security!
                    cmd.Parameters.AddWithValue("@RoleName", roleName);

                    try
                    {
                        conn.Open();
                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            return Convert.ToInt32(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log your exception here (helpful for a startup to track bugs)
                        Console.WriteLine("Database Error: " + ex.Message);
                    }
                }
            }
            return -1; 
        }


        public static async Task<string?> GetRoleNameByID(int roleID)
        {
            // The plain SQL query
            string query = "SELECT name FROM roles WHERE id = @RoleID";

            using (SqlConnection conn = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Parameterized query for security
                    cmd.Parameters.AddWithValue("@RoleID", roleID);

                    try
                    {
                        await conn.OpenAsync();

                        object? result = await cmd.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                        {
                            return result.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the exception
                        throw;
                    }
                }
            }

            return null;
        }

    }
}

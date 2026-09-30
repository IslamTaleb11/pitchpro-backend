using DataAccessLayer.DTOs.Club;
using DataAccessLayer.DTOs.Person;
using DataAccessLayer.DTOs.User;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class UserProvider
    {
        public static int AddNew(UserRegistrationSaveDTO userRegistrationSaveDTO, UserRegistrationResponseDTO userRegistrationResponseDTO)
        {
            // We use 'int' as the return type to capture the newly created ID (Identity)
            int newId = -1;

            // 1. T-SQL Query with Output clause to get the new ID
            string sql = @"INSERT INTO Users (email, password, role_id, person_id, email_verification_token_hash, email_verification_expires_at) 
                           VALUES (@Email, @Password, @RoleID, @PersonID, @EmailVerificationTokenHash, @EmailVerificationTokenExpiresAt);
                           SELECT CAST(scope_identity() AS int);";

            // 2. Wrap in 'using' blocks to ensure connections are closed automatically
            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    // 3. Define parameters with explicit SqlDbTypes to prevent SQL Injection
                    command.Parameters.Add("@Email", SqlDbType.NVarChar, 450).Value = userRegistrationSaveDTO.Email;
                    command.Parameters.Add("@Password", SqlDbType.NVarChar, 255).Value = userRegistrationSaveDTO.Password;
                    command.Parameters.Add("@RoleID", SqlDbType.Int).Value = userRegistrationSaveDTO.RoleID;
                    command.Parameters.Add("@PersonID", SqlDbType.Int).Value = userRegistrationSaveDTO.PersonID;
                    command.Parameters.Add("@EmailVerificationTokenHash", SqlDbType.NVarChar, 128).Value = (object)userRegistrationSaveDTO.EmailVerificationTokenHash ?? DBNull.Value;
                    command.Parameters.Add("@EmailVerificationTokenExpiresAt", SqlDbType.DateTime2).Value = (object)userRegistrationSaveDTO.EmailVerificationTokenExpiresAt ?? DBNull.Value;

                    try
                    {
                        connection.Open();

                        // 4. Execute and get the new Identity ID
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            newId = Convert.ToInt32(result);

                            userRegistrationResponseDTO.ID = newId;
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



        public static bool IsEmailExists(string email)
        {
            bool exists = false;

            using (SqlConnection conn = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_IsEmailExists", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Email", email);

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



        public static UserFindResponseDTO GetByID(int id)
        {
            UserFindResponseDTO user = null;

            // 1. T-SQL Query with a WHERE clause
            string sql = @"SELECT 
                Users.ID, 
                Users.email, 
                Users.password, 
                Users.role_id, 
                Users.person_id, 
                Users.email_verified_at, 
                roles.name AS Role_name
            FROM Users
            INNER JOIN roles ON Users.role_id = roles.id
            WHERE Users.ID = @ID;";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    // 2. Always use parameters for the ID to prevent SQL Injection
                    command.Parameters.Add("@ID", SqlDbType.Int).Value = id;

                    try
                    {
                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // 3. Use 'if' instead of 'while' because we only expect ONE record
                            if (reader.Read())
                            {
                                user = new UserFindResponseDTO
                                {
                                    ID = (int)reader["ID"],
                                    Email = (string)reader["Email"],
                                    PersonID = (int)reader["Person_id"],
                                    RoleID = (int)reader["Role_id"],
                                    Role = (string)reader["Role_name"],
                                    EmailVerifiedAt = reader["Email_verified_at"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["Email_verified_at"])
                                        : (DateTime?)null
                                };
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        // Rethrow for the Business Layer to handle
                        throw new Exception("Database error occurred while finding the club.", ex);
                    }
                }
            }

            return user; // Returns null if no record was found
        }


        // Retrieve a user's club ID by user ID. Returns null if not found.
        public static async Task<int?> GetClubIdByUserId(int userId)
        {
            int? clubId = null;
            string sql = @"SELECT c.id
                       FROM Users u
                       INNER JOIN Persons p ON u.person_id = p.id
                       INNER JOIN Clubs c ON p.club_id = c.id
                       WHERE u.id = @UserId";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    try
                    {
                        connection.Open();
                        var result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            clubId = Convert.ToInt32(result);
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while retrieving club ID.", ex);
                    }
                }
            }
            return clubId; // null if not found
        }

        public static UserFindResponseDTO GetByEmail(string email)
        {
            UserFindResponseDTO user = null;

            string sql = @"select users.id, email, password, roles.name as Role, person_id, users.role_id, users.email_verified_at from users
                            inner join roles on users.role_id = roles.id
                            where email = @Email;";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@Email", SqlDbType.NVarChar, 450).Value = email;

                    try
                    {
                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user = new UserFindResponseDTO
                                {
                                    ID = (int)reader["ID"],
                                    Email = (string)reader["Email"],
                                    Password = (string)reader["Password"],
                                    PersonID = (int)reader["Person_id"],
                                    RoleID = (int)reader["Role_id"],
                                    Role = (string)reader["Role"],
                                    EmailVerifiedAt = reader["email_verified_at"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["email_verified_at"])
                                        : (DateTime?)null
                                };
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while finding the user by email.", ex);
                    }
                }
            }

            return user; // Returns null when email not found
        }

        // Marks the user matching the given token hash as verified,
        // provided the token is still valid and not expired.
        public static bool VerifyEmail(string tokenHash)
        {
            try
            {
                bool verified = false;

                using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
                {
                    string selectSql = @"SELECT id, email_verification_expires_at 
                                     FROM Users
                                     WHERE email_verification_token_hash = @TokenHash
                                       AND email_verified_at IS NULL;";

                    using (SqlCommand command = new SqlCommand(selectSql, connection))
                    {
                        command.Parameters.Add("@TokenHash", SqlDbType.NVarChar, 128).Value = tokenHash;

                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                DateTime expiresAt = reader["email_verification_expires_at"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["email_verification_expires_at"])
                                    : DateTime.MinValue;

                                if (expiresAt > DateTime.UtcNow)
                                {
                                    verified = true;
                                }
                            }
                        }
                    }

                    if (verified)
                    {
                        string updateSql = @"UPDATE Users
                                         SET email_verified_at = GETUTCDATE(),
                                             email_verification_token_hash = NULL,
                                             email_verification_expires_at = NULL
                                         WHERE email_verification_token_hash = @TokenHash;";

                        using (SqlCommand command = new SqlCommand(updateSql, connection))
                        {
                            command.Parameters.Add("@TokenHash", SqlDbType.NVarChar, 128).Value = tokenHash;
                            command.ExecuteNonQuery();
                        }
                    }
                }

                return verified;
            }
            catch (Exception ex)
            {
                throw;
            }

            
        }


        }


    
}

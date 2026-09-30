using DataAccessLayer.DTOs.Club;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;


namespace DataAccessLayer.Providers
{
    public class ClubProvider
    {
        public static async Task<int> AddNew(ClubSaveDTO clubSaveDTO, ClubRegistrationResponseDTO clubRegistrationResponseDTO)
        {
            int newId = -1;

            string sql = @"INSERT INTO Clubs (Name, Crest, Primary_Identity_Color, Contact_Number) 
                   VALUES (@Name, @Crest, @Color, @Phone);
                   SELECT CAST(scope_identity() AS int);";

            // 1. Establish ONE single connection for the entire operation
            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = clubSaveDTO.Name;
                    command.Parameters.Add("@Crest", SqlDbType.NVarChar, -1).Value = (object)clubSaveDTO.Crest ?? DBNull.Value;
                    command.Parameters.Add("@Color", SqlDbType.NVarChar, 255).Value = clubSaveDTO.PrimaryIdentityColor;
                    command.Parameters.Add("@Phone", SqlDbType.NVarChar, 255).Value = clubSaveDTO.ContactNumber;

                    try
                    {
                        await connection.OpenAsync();

                        var result = await command.ExecuteScalarAsync();
                        if (result != null)
                        {
                            newId = Convert.ToInt32(result);

                            clubRegistrationResponseDTO.ID = newId;
                            clubRegistrationResponseDTO.Name = clubSaveDTO.Name;
                            clubRegistrationResponseDTO.Crest = clubSaveDTO.Crest;
                            clubRegistrationResponseDTO.PrimaryIdentityColor = clubSaveDTO.PrimaryIdentityColor;
                            clubRegistrationResponseDTO.ContactNumber = clubSaveDTO.ContactNumber;
                        }

                        // 2. If the club was built, pass the shared connection to the helper functions
                        if (newId > 0)
                        {
                            int durationDays = await GetSubscriptionDurationDays(1, connection);
                            DateTime startDate = DateTime.UtcNow; // Better to use UtcNow for databases
                            DateTime expiryDate = startDate.AddDays(durationDays);

                            await AddClubSubscription(newId, 1, startDate, expiryDate, connection);
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while adding a new club.", ex);
                    }
                }
            }

            return newId;
        }

        // 3. Modified helper accepts the shared connection instead of initializing a new one
        private static async Task<int> GetSubscriptionDurationDays(int subscriptionTypeId, SqlConnection connection)
        {
            const string sql = @"SELECT duration_in_days FROM subscriptionTypes WHERE id = @SubscriptionTypeId";

            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@SubscriptionTypeId", SqlDbType.Int).Value = subscriptionTypeId;

                try
                {
                    // Connection is already opened by the caller! Just run it.
                    var result = command.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                    {
                        throw new Exception($"Subscription type with ID {subscriptionTypeId} was not found.");
                    }

                    return Convert.ToInt32(result);
                }
                catch (SqlException ex)
                {
                    throw new Exception("Database error occurred while retrieving subscription duration.", ex);
                }
            }
        }

        // 4. Modified helper accepts the shared connection instead of initializing a new one
        private static async Task AddClubSubscription(int clubId, int subscriptionTypeId, DateTime startDate, DateTime expiryDate, SqlConnection connection)
        {
            const string sql = @"INSERT INTO clubSubscriptions (club_id, subscription_type_id, start_date, expiry_date)
                         VALUES (@ClubId, @SubscriptionTypeId, @StartDate, @ExpiryDate);";

            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ClubId", SqlDbType.Int).Value = clubId;
                command.Parameters.Add("@SubscriptionTypeId", SqlDbType.Int).Value = subscriptionTypeId;
                command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate;
                command.Parameters.Add("@ExpiryDate", SqlDbType.DateTime).Value = expiryDate;

                try
                {
                    // Connection is already opened by the caller! Just run it.
                    command.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    throw new Exception("Database error occurred while adding the club subscription.", ex);
                }
            }
        }

        public static List<ClubGetAllResponseDTO> GetAll(int pageNumber, int rowsPerPage)
        {
            List<ClubGetAllResponseDTO> clubsList = new List<ClubGetAllResponseDTO>();

            int offset = (pageNumber - 1) * rowsPerPage;

            // 1. T-SQL Query to fetch all clubs
            // Note: Use the actual column names from your database
            string sql = @"SELECT ID, Name, Crest, Primary_Identity_Color, Contact_Number FROM Clubs
            order by ID
            OFFSET @offset ROWS
            FETCH NEXT @rowsPerPage ROWS ONLY;";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@offset", offset);
                        command.Parameters.AddWithValue("@rowsPerPage", rowsPerPage);
                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // 2. Loop through the rows returned by the database
                            while (reader.Read())
                            {
                                // 3. Map each row to your specific DTO
                                clubsList.Add(new ClubGetAllResponseDTO
                                {
                                    ID = (int)reader["ID"],
                                    Name = (string)reader["Name"],
                                    Crest = (string)reader["Crest"],
                                    PrimaryIdentityColor = (string)reader["Primary_Identity_Color"],
                                    ContactNumber = (string)reader["Contact_Number"],
                                });
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        // In production, log 'ex' here
                        throw new Exception("Database error occurred while retrieving clubs.", ex);
                    }
                }
            }

            return clubsList;
        }


        public static ClubFindByIDResponseDTO GetByID(int id)
        {
            ClubFindByIDResponseDTO club = null;

            // 1. T-SQL Query with a WHERE clause
            string sql = @"SELECT ID, Name, Crest, Primary_Identity_Color, Contact_Number 
                   FROM Clubs 
                   WHERE ID = @ID";

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
                                club = new ClubFindByIDResponseDTO
                                {
                                    ID = (int)reader["ID"],
                                    Name = (string)reader["Name"],
                                    // Handle NULL for optional fields
                                    Crest = (string)reader["Crest"],
                                    PrimaryIdentityColor = (string)reader["Primary_Identity_Color"],
                                    ContactNumber = (string)reader["Contact_Number"],
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

            return club; // Returns null if no record was found
        }



        public static bool Update(ClubSaveUpdateDTO saveUpdateDTO, ClubUpdateResponseDTO updateResponseDTO)
        {
            int rowsAffected = 0;

            // 1. The SQL Update Statement
            // We use a WHERE clause to make sure we only update the specific record
            string sql = @"UPDATE Clubs 
                   SET Name = @Name, 
                       Crest = @Crest, 
                       Primary_Identity_Color = @Color, 
                       Contact_Number = @Phone
                   WHERE ID = @ID";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    // 2. Mapping parameters (The "Secure" part)
                    command.Parameters.Add("@ID", SqlDbType.Int).Value = saveUpdateDTO.ID;
                    command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = saveUpdateDTO.Name;

                    command.Parameters.Add("@Crest", SqlDbType.NVarChar, -1).Value = (object)saveUpdateDTO.Crest;

                    command.Parameters.Add("@Color", SqlDbType.NVarChar, 255).Value = saveUpdateDTO.PrimaryIdentityColor;
                    command.Parameters.Add("@Phone", SqlDbType.NVarChar, 255).Value = saveUpdateDTO.ContactNumber;

                    try
                    {
                        connection.Open();

                        // 3. ExecuteNonQuery returns the number of rows updated
                        rowsAffected = command.ExecuteNonQuery();

                        updateResponseDTO.ID = saveUpdateDTO.ID;
                        updateResponseDTO.Name = saveUpdateDTO.Name;
                        updateResponseDTO.Crest = saveUpdateDTO.Crest;
                        updateResponseDTO.PrimaryIdentityColor = saveUpdateDTO.PrimaryIdentityColor;
                        updateResponseDTO.ContactNumber = saveUpdateDTO.ContactNumber;

                    }
                    catch (SqlException ex)
                    {
                        // Bubbling the error up to your Business Layer
                        throw new Exception("Database error: Could not update the club record.", ex);
                    }
                }
            }

            // 4. If rowsAffected is 0, it means the ID wasn't found in the database
            return rowsAffected > 0;
        }


        public static bool Delete(int id)
        {
            int rowsAffected = 0;

            string sql = @"Delete from Clubs 
                   WHERE ID = @ID";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@ID", SqlDbType.Int).Value = id;
                    try
                    {
                        connection.Open();

                        // 3. ExecuteNonQuery returns the number of rows updated
                        rowsAffected = command.ExecuteNonQuery();

                    }
                    catch (SqlException ex)
                    {
                        // Bubbling the error up to your Business Layer
                        throw new Exception("Database error: Could not update the club record.", ex);
                    }
                }
            }

            // 4. If rowsAffected is 0, it means the ID wasn't found in the database
            return rowsAffected > 0;
        }

        public static bool DoesClubExist(int id)
        {
            bool exists = false;

            // 1. T-SQL Query with a WHERE clause
            string sql = @"SELECT 1 from clubs WHERE id = @ClubID";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    // 2. Always use parameters for the ID to prevent SQL Injection
                    command.Parameters.Add("@ClubID", SqlDbType.Int).Value = id;

                    try
                    {
                        connection.Open();

                        object result = command.ExecuteScalar();
                        exists = (result != null);

                    }
                    catch (SqlException ex)
                    {
                        // Rethrow for the Business Layer to handle
                        throw new Exception("Database error", ex);
                    }
                }
            }

            return exists; // Returns null if no record was found
        }


    }
}

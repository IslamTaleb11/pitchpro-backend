using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class ClubSubscriptionProvider
    {
        /// <summary>
        /// Gets the subscription type ID by name (e.g., "free")
        /// </summary>
        public static int GetSubscriptionTypeIdByName(string subscriptionTypeName)
        {
            int subscriptionTypeId = -1;

            string sql = @"SELECT id FROM subscriptionTypes WHERE name = @Name";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = subscriptionTypeName;

                    try
                    {
                        connection.Open();
                        object result = command.ExecuteScalar();
                        if (result != null)
                        {
                            subscriptionTypeId = Convert.ToInt32(result);
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while retrieving subscription type.", ex);
                    }
                }
            }

            return subscriptionTypeId;
        }

        /// <summary>
        /// Gets the number of days for a subscription type
        /// </summary>
        public static int GetSubscriptionDays(int subscriptionTypeId)
        {
            int days = 0;

            string sql = @"SELECT days FROM subscriptionTypes WHERE id = @SubscriptionTypeId";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@SubscriptionTypeId", SqlDbType.Int).Value = subscriptionTypeId;

                    try
                    {
                        connection.Open();
                        object result = command.ExecuteScalar();
                        if (result != null)
                        {
                            days = Convert.ToInt32(result);
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while retrieving subscription days.", ex);
                    }
                }
            }

            return days;
        }

        /// <summary>
        /// Creates a new club subscription record
        /// </summary>
        public static bool AddNew(int clubId, int subscriptionTypeId, DateTime startDate, DateTime expiryDate)
        {
            bool success = false;

            string sql = @"INSERT INTO clubSubscriptions (club_id, subscription_type_id, start_date, expiry_date) 
                           VALUES (@ClubID, @SubscriptionTypeID, @StartDate, @ExpiryDate)";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;
                    command.Parameters.Add("@SubscriptionTypeID", SqlDbType.Int).Value = subscriptionTypeId;
                    command.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate;
                    command.Parameters.Add("@ExpiryDate", SqlDbType.Date).Value = expiryDate;

                    try
                    {
                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();
                        success = rowsAffected > 0;
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while adding club subscription.", ex);
                    }
                }
            }

            return success;
        }



        public static async Task<string> GetCurrentSubscriptionName(int clubID)
        {
            // 1. Fixed validation: int can't be null, so check for invalid IDs (e.g., <= 0)
            if (clubID <= 0)
                throw new ArgumentException("Invalid Club ID provided.", nameof(clubID));

            const string sql = @"
            SELECT subscriptionTypes.type_name 
            FROM clubSubscriptions
            INNER JOIN subscriptionTypes ON clubSubscriptions.subscription_type_Id = subscriptionTypes.id
            WHERE clubSubscriptions.club_id = @ClubID";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    // 2. Fixed parameter name and mapped it to the actual method argument
                    command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubID;

                    try
                    {
                        await connection.OpenAsync();
                        var result = await command.ExecuteScalarAsync();

                        // 3. Handled missing records cleanly by returning string.Empty (or throw if preferred)
                        if (result == null || result == DBNull.Value)
                        {
                            return string.Empty;
                        }

                        // 4. Fixed return type conversion to string
                        return Convert.ToString(result);
                    }
                    catch (SqlException ex)
                    {
                        // In production, consider logging 'ex' here
                        throw new Exception("Database error occurred while retrieving the subscription name for the club.", ex);
                    }
                }
            }
        }



        public static async Task<DateTime?> GetClubSubscriptionExpiryDate(int clubID)
        {
            if (clubID <= 0)
                throw new ArgumentException("Invalid Club ID provided.", nameof(clubID));

            const string sql = "SELECT expiry_date FROM clubSubscriptions WHERE club_id = @ClubID";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubID;

                try
                {
                    await connection.OpenAsync();
                    var result = await command.ExecuteScalarAsync();

                    // Handle missing records or NULL database values cleanly
                    if (result == null || result == DBNull.Value)
                    {
                        return null;
                    }

                    // Explicitly cast or convert the DB value to DateTime
                    return Convert.ToDateTime(result);
                }
                catch (SqlException ex)
                {
                    throw new Exception("Database error occurred while retrieving the subscription expiry date.", ex);
                }
            }
        }


        public static async Task<DateTime?> GetClubSubscriptionStartDate(int clubID)
        {
            if (clubID <= 0)
                throw new ArgumentException("Invalid Club ID provided.", nameof(clubID));

            const string sql = "SELECT start_date FROM clubSubscriptions WHERE club_id = @ClubID";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubID;

                try
                {
                    await connection.OpenAsync();
                    var result = await command.ExecuteScalarAsync();

                    // Handle missing records or NULL database values cleanly
                    if (result == null || result == DBNull.Value)
                    {
                        return null;
                    }

                    // Explicitly cast or convert the DB value to DateTime
                    return Convert.ToDateTime(result);
                }
                catch (SqlException ex)
                {
                    throw new Exception("Database error occurred while retrieving the subscription expiry date.", ex);
                }
            }
        }


        /// <summary>
        /// Downgrades a club's current subscription to the Free plan by switching its
        /// subscription type. The Free plan id is resolved from its name (case-insensitive).
        /// </summary>
        public static async Task<bool> DowngradeToFreePlan(int clubID, string freePlanName)
        {
            if (clubID <= 0)
                throw new ArgumentException("Invalid Club ID provided.", nameof(clubID));

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // 1. Resolve the Free plan's subscription type id.
                    int freePlanId;
                    using (SqlCommand idCommand = new SqlCommand("SELECT id FROM subscriptionTypes WHERE type_name = @FreePlanName", connection))
                    {
                        idCommand.Parameters.Add("@FreePlanName", SqlDbType.NVarChar, 255).Value = freePlanName;

                        var idResult = await idCommand.ExecuteScalarAsync();
                        if (idResult == null || idResult == DBNull.Value)
                        {
                            throw new Exception($"Subscription type '{freePlanName}' was not found.");
                        }

                        freePlanId = Convert.ToInt32(idResult);
                    }

                    // 2. Switch the club's current subscription over to the Free plan.
                    using (SqlCommand updateCommand = new SqlCommand("UPDATE clubSubscriptions SET subscription_type_id = @FreePlanId WHERE club_id = @ClubID", connection))
                    {
                        updateCommand.Parameters.Add("@FreePlanId", SqlDbType.Int).Value = freePlanId;
                        updateCommand.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubID;

                        int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
                catch (SqlException ex)
                {
                    throw new Exception("Database error occurred while downgrading the club subscription to Free.", ex);
                }
            }
        }

    }
}

using DataAccessLayer.DTOs.Payment;
using DataAccessLayer.DTOs.Subscription;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;


namespace DataAccessLayer.Providers
{
    public class PaymentProvider
    {
        /// <summary>
        /// Get subscription duration days from subscriptionTypes table
        /// </summary>
        public static int GetSubscriptionDurationDays(int subscriptionTypeId)
        {
            const string sql = @"SELECT duration_in_days FROM subscriptionTypes WHERE id = @SubscriptionTypeId";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@SubscriptionTypeId", SqlDbType.Int).Value = subscriptionTypeId;

                    try
                    {
                        connection.Open();
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
        }

        /// <summary>
        /// Get subscription type by name (e.g., "Premium")
        /// </summary>
        public static SubscriptionTypeDTO GetSubscriptionTypeByName(string subscriptionTypeName)
        {
            SubscriptionTypeDTO subscriptionType = null;

            string sql = @"SELECT id, type_name, price, duration_in_days FROM subscriptionTypes WHERE type_name = @Name";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = subscriptionTypeName;

                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                subscriptionType = new SubscriptionTypeDTO
                                {
                                    Id = (int)reader["id"],
                                    Name = (string)reader["type_name"],
                                    Price = (decimal)reader["price"],
                                    DurationInDays = (int)reader["duration_in_days"]
                                };
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while retrieving subscription type.", ex);
                    }
                }
            }

            return subscriptionType;
        }

        /// <summary>
        /// Update clubSubscriptions after successful payment
        /// Sets subscription_type_id to 2, start_date to today, and calculates expiry_date
        /// </summary>
        public static void UpdateClubSubscriptionAfterPayment(int clubId)
        {
            // Get duration from subscriptionTypes table
            SubscriptionTypeDTO subscriptionType = GetSubscriptionTypeByName("Premium");
            int durationDays = GetSubscriptionDurationDays(subscriptionType.Id);

            DateTime startDate = DateTime.UtcNow;
            DateTime expiryDate = startDate.AddDays(durationDays);

            const string sql = @"UPDATE clubSubscriptions 
                              SET subscription_type_id = @SubscriptionTypeId, 
                                  start_date = @StartDate, 
                                  expiry_date = @ExpiryDate
                              WHERE club_id = @ClubId";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@SubscriptionTypeId", SqlDbType.Int).Value = subscriptionType.Id;
                    command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate;
                    command.Parameters.Add("@ExpiryDate", SqlDbType.DateTime).Value = expiryDate;
                    command.Parameters.Add("@ClubId", SqlDbType.Int).Value = clubId;

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while updating club subscription.", ex);
                    }
                }
            }
        }


        public static async Task RenewClubSubscriptionAfterPayment(int clubId)
        {
            // Get duration from subscriptionTypes table
            SubscriptionTypeDTO subscriptionType = GetSubscriptionTypeByName("Premium");
            int durationDays = GetSubscriptionDurationDays(subscriptionType.Id);
            DateTime? oldExpiryDate = await GetClubSubscriptionExpiryDate(clubId);

            DateTime startDate = oldExpiryDate ?? DateTime.Now;
            DateTime expiryDate = startDate.AddDays(durationDays);

            const string sql = @"UPDATE clubSubscriptions 
                              SET subscription_type_id = @SubscriptionTypeId, 
                                  start_date = @StartDate, 
                                  expiry_date = @ExpiryDate
                              WHERE club_id = @ClubId";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@SubscriptionTypeId", SqlDbType.Int).Value = subscriptionType.Id;
                    command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate;
                    command.Parameters.Add("@ExpiryDate", SqlDbType.DateTime).Value = expiryDate;
                    command.Parameters.Add("@ClubId", SqlDbType.Int).Value = clubId;

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while updating club subscription.", ex);
                    }
                }
            }
        }





        /// <summary>
        /// Get payment method ID by method name
        /// </summary>
        public static int GetPaymentMethodId(string methodName)
        {
            const string sql = @"SELECT id FROM refPaymentMethods WHERE type_name = @MethodName";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@MethodName", SqlDbType.NVarChar, 50).Value = methodName;

                    try
                    {
                        connection.Open();
                        var result = command.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            // If payment method doesn't exist, create it with a default description
                            return CreatePaymentMethod(methodName, $"Payment via {methodName}");
                        }

                        return Convert.ToInt32(result);
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while retrieving payment method.", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Create a new payment method
        /// </summary>
        private static int CreatePaymentMethod(string methodName, string description)
        {
            const string sql = @"INSERT INTO refPaymentMethods (methodName, description) 
                              VALUES (@MethodName, @Description);
                              SELECT CAST(scope_identity() AS int);";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@MethodName", SqlDbType.NVarChar, 50).Value = methodName;
                    command.Parameters.Add("@Description", SqlDbType.NVarChar, 255).Value = description ?? (object)DBNull.Value;

                    try
                    {
                        connection.Open();
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            return Convert.ToInt32(result);
                        }
                        throw new Exception("Failed to create payment method.");
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while creating payment method.", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Get current club subscription ID for a club
        /// </summary>
        public static int GetClubSubscriptionId(int clubId)
        {
            const string sql = @"SELECT id FROM clubSubscriptions WHERE club_id = @ClubId";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@ClubId", SqlDbType.Int).Value = clubId;

                    try
                    {
                        connection.Open();
                        var result = command.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            throw new Exception($"Club subscription not found for club ID {clubId}.");
                        }

                        return Convert.ToInt32(result);
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while retrieving club subscription.", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Create a new payment subscription record
        /// </summary>
        public static int CreatePaymentSubscription(PaymentSubscriptionCreateDTO paymentDTO)
        {
            int newId = -1;

            const string sql = @"INSERT INTO paymentSubscriptions 
                              (plan_name, amount, currency, payment_method_id, chargily_invoice_id, is_active, club_subscription_id, payment_date_time)
                              VALUES (@PlanName, @Amount, @Currency, @PaymentMethodId, @ChargilyInvoiceId, @IsActive, @ClubSubscriptionId, @PaymentDateTime);
                              SELECT CAST(scope_identity() AS int);";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@PlanName", SqlDbType.NVarChar, 50).Value = paymentDTO.PlanName;
                    command.Parameters.Add("@Amount", SqlDbType.Decimal).Value = paymentDTO.Amount;
                    command.Parameters.Add("@Currency", SqlDbType.NVarChar, 3).Value = paymentDTO.Currency;
                    command.Parameters.Add("@PaymentMethodId", SqlDbType.Int).Value = paymentDTO.PaymentMethodId;
                    command.Parameters.Add("@ChargilyInvoiceId", SqlDbType.NVarChar, 100).Value = paymentDTO.ChargilyInvoiceId;
                    command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = paymentDTO.IsActive;
                    command.Parameters.Add("@ClubSubscriptionId", SqlDbType.Int).Value = paymentDTO.ClubSubscriptionId;
                    command.Parameters.Add("@PaymentDateTime", SqlDbType.DateTime).Value = DateTime.UtcNow;

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
                        throw new Exception("Database error occurred while creating payment subscription.", ex);
                    }
                }
            }

            return newId;
        }

        /// <summary>
        /// Get payment subscription by Chargily invoice ID
        /// </summary>
        public static PaymentSubscriptionResponseDTO GetPaymentByChargilyInvoiceId(string chargilyInvoiceId)
        {
            PaymentSubscriptionResponseDTO payment = null;

            const string sql = @"SELECT id, plan_name, amount, currency, payment_method_id, 
                                       chargily_invoice_id, is_active, club_subscription_id, payment_date_time
                                FROM paymentSubscriptions 
                                WHERE chargily_invoice_id = @ChargilyInvoiceId";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@ChargilyInvoiceId", SqlDbType.NVarChar, 100).Value = chargilyInvoiceId;

                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                payment = new PaymentSubscriptionResponseDTO
                                {
                                    Id = (int)reader["id"],
                                    PlanName = (string)reader["plan_name"],
                                    Amount = (decimal)reader["amount"],
                                    Currency = (string)reader["currency"],
                                    PaymentMethodId = (int)reader["payment_method_id"],
                                    ChargilyInvoiceId = (string)reader["chargily_invoice_id"],
                                    IsActive = (bool)reader["is_active"],
                                    ClubSubscriptionId = (int)reader["club_subscription_id"],
                                    CreatedDate = DateTime.UtcNow, // Fallback since no created_at column
                                    PaymentDate = reader["payment_date_time"] != DBNull.Value ? (DateTime)reader["payment_date_time"] : (DateTime?)null
                                };
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while retrieving payment subscription.", ex);
                    }
                }
            }

            return payment;
        }

        /// <summary>
        /// Get all payments for a club
        /// </summary>
        public static List<PaymentSubscriptionResponseDTO> GetPaymentsByClubId(int clubId)
        {
            List<PaymentSubscriptionResponseDTO> payments = new List<PaymentSubscriptionResponseDTO>();

            const string sql = @"SELECT ps.id, ps.plan_name, ps.amount, ps.currency, ps.payment_method_id, 
                                       ps.chargily_invoice_id, ps.is_active, ps.club_subscription_id, ps.payment_date_time
                                FROM paymentSubscriptions ps
                                INNER JOIN clubSubscriptions cs ON ps.club_subscription_id = cs.id
                                WHERE cs.club_id = @ClubId
                                ORDER BY ps.id DESC"; // Order by id since no created_at column

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@ClubId", SqlDbType.Int).Value = clubId;

                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                payments.Add(new PaymentSubscriptionResponseDTO
                                {
                                    Id = (int)reader["id"],
                                    PlanName = (string)reader["plan_name"],
                                    Amount = (decimal)reader["amount"],
                                    Currency = (string)reader["currency"],
                                    PaymentMethodId = (int)reader["payment_method_id"],
                                    ChargilyInvoiceId = (string)reader["chargily_invoice_id"],
                                    IsActive = (bool)reader["is_active"],
                                    ClubSubscriptionId = (int)reader["club_subscription_id"],
                                    CreatedDate = DateTime.UtcNow, // Fallback since no created_at column
                                    PaymentDate = reader["payment_date_time"] != DBNull.Value ? (DateTime)reader["payment_date_time"] : (DateTime?)null
                                });
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while retrieving payments.", ex);
                    }
                }
            }

            return payments;
        }


        public static async Task<decimal> GetSubscriptionPriceByName(string subscriptionTypeName)
        {
            if (string.IsNullOrWhiteSpace(subscriptionTypeName))
                throw new ArgumentException("Subscription type name cannot be empty.", nameof(subscriptionTypeName));

            const string sql = @"SELECT price FROM subscriptionTypes WHERE type_name = @Name";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = subscriptionTypeName;

                    try
                    {
                        await connection.OpenAsync();
                        var result = await command.ExecuteScalarAsync();

                        if (result == null || result == DBNull.Value)
                        {
                            throw new Exception($"Subscription type named '{subscriptionTypeName}' was not found.");
                        }

                        return Convert.ToDecimal(result);
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while retrieving subscription price by name.");
                    }
                }
            }
        }



        private static async Task<DateTime?> GetClubSubscriptionExpiryDate(int clubId)
        {
            if (clubId <= 0)
            {
                throw new KeyNotFoundException("ClubId not found");
            }


            // Querying the latest active expiration date for this club
            string query = @"SELECT TOP 1 expiry_date 
                             FROM ClubSubscriptions 
                             WHERE club_id = @ClubID;";

            try
            {
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                        await connection.OpenAsync();

                        object result = await command.ExecuteScalarAsync();

                        // If the database returns null or DBNull, return null
                        if (result == null || result == DBNull.Value)
                        {
                            return null;
                        }

                        return (DateTime)result;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public static async Task<DateTime?> GetPreviousPaymentDateTime(int clubId)
        {
            if (clubId <= 0) return null;


            string query = @"SELECT payment_date_time 
                             FROM paymentSubscriptions
                             INNER JOIN clubSubscriptions ON paymentSubscriptions.club_subscription_id = clubSubscriptions.id
                             INNER JOIN clubs ON clubSubscriptions.club_id = clubs.id
                             WHERE clubs.id = @ClubID
                             ORDER BY paymentSubscriptions.payment_date_time DESC
                             OFFSET 1 ROWS
                             FETCH NEXT 1 ROWS ONLY;";

            try
            {
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                        await connection.OpenAsync();

                        object result = await command.ExecuteScalarAsync();

                        if (result == null || result == DBNull.Value)
                        {
                            return null;
                        }

                        return (DateTime)result;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQL ERROR] GetPreviousPaymentDateTime failed for Club {clubId}: {ex.Message}");
                throw;
            }
        }
    }



}




using DataAccessLayer.DTOs.Payment;
using DataAccessLayer.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Services
{
    public class PaymentService
    {
        /// <summary>
        /// Process a successful payment webhook from Chargily
        /// This method:
        /// 1. Updates clubSubscriptions with subscription_type_id = 2, start_date = today, expiry_date calculated
        /// 2. Creates a paymentSubscriptions record
        /// </summary>
        public static async Task ProcessPaymentWebhook(int clubId, string planName, decimal amount, string currency, 
                                                 string paymentMethod, string chargilyInvoiceId)
        {
            try
            {
                // first we check maybe the user want to renew his plan while his premium plan still valid
                if (await ClubSubscription.GetCurrentSubscriptionName(clubId) == "Premium")
                {
                    await PaymentProvider.RenewClubSubscriptionAfterPayment(clubId);
                }
                else
                {
                    // Step 1: Update club subscription with new subscription_type_id (2) and dates
                    PaymentProvider.UpdateClubSubscriptionAfterPayment(clubId);
                }



                // Step 2: Get payment method ID
                int paymentMethodId = PaymentProvider.GetPaymentMethodId(paymentMethod);

                // Step 3: Get club subscription ID
                int clubSubscriptionId = PaymentProvider.GetClubSubscriptionId(clubId);

                // Step 4: Create payment subscription record
                var paymentDTO = new PaymentSubscriptionCreateDTO
                {
                    PlanName = planName,
                    Amount = amount,
                    Currency = currency,
                    PaymentMethodId = paymentMethodId,
                    ChargilyInvoiceId = chargilyInvoiceId,
                    IsActive = true,
                    ClubSubscriptionId = clubSubscriptionId
                };

                int paymentId = PaymentProvider.CreatePaymentSubscription(paymentDTO);

                if (paymentId <= 0)
                {
                    throw new Exception("Failed to create payment subscription record.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Payment processing failed for club {clubId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Verify if a payment already exists for a given Chargily invoice ID
        /// </summary>
        public static bool PaymentExists(string chargilyInvoiceId)
        {
            var payment = PaymentProvider.GetPaymentByChargilyInvoiceId(chargilyInvoiceId);
            return payment != null;
        }

        /// <summary>
        /// Get payment subscription by invoice ID
        /// </summary>
        public static PaymentSubscriptionResponseDTO GetPaymentByInvoiceId(string chargilyInvoiceId)
        {
            return PaymentProvider.GetPaymentByChargilyInvoiceId(chargilyInvoiceId);
        }

        /// <summary>
        /// Get all payments for a club
        /// </summary>
        public static List<PaymentSubscriptionResponseDTO> GetClubPayments(int clubId)
        {
            return PaymentProvider.GetPaymentsByClubId(clubId);
        }
    }
}

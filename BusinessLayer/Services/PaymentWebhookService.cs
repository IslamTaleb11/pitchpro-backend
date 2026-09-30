using DataAccessLayer.DTOs.Payment;
using DataAccessLayer.Providers;
using Chargily.Pay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.DTOs.Chargily;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;


namespace BusinessLayer.Services
{
    public class PaymentWebhookService
    {
        /// <summary>
        /// Process webhook data from Chargily Pay
        /// Extracts invoice information, validates, and processes successful payments
        /// </summary>

    public static async Task ProcessChargilyWebhook(ChargilyWebhookDTO webhookRequest)
    {
        if (webhookRequest == null)
        {
            throw new ArgumentNullException(nameof(webhookRequest), "Inbound webhook request payload is null.");
        }

        try
        {
            // ?? FIX 1: Declare and assign the 'checkout' variable first!
            var checkout = webhookRequest.Data;
            if (checkout == null)
            {
                throw new InvalidOperationException("Webhook inner checkout payload data block is missing.");
            }

            // ?? FIX 2: Safely read mapping fields directly from your DTO
            string invoiceId = checkout.InvoiceId ?? checkout.Id ?? "";
            string status = checkout.Status ?? "";

            // Since DTO.Amount is a regular decimal type, you don't need '?? 0' here
            decimal finalAmount = checkout.Amount;

            string currency = checkout.Currency ?? "DZD";
            string paymentMethod = checkout.PaymentMethod ?? "EDAHABIA";

                // Extract clubId from metadata array
                int clubId = 0;
                if (checkout.Metadata != null && checkout.Metadata.Count > 0)
                {
                    foreach (var meta in checkout.Metadata)
                    {
                        if (meta.StartsWith("clubId:"))
                        {
                            string clubIdStr = meta.Substring("clubId:".Length);
                            int.TryParse(clubIdStr, out clubId);
                            break;
                        }
                    }
                }

                // Enforce validation on vital fields
                if (string.IsNullOrEmpty(invoiceId))
            {
                throw new KeyNotFoundException("Chargily invoice identifier could not be resolved from webhook data.");
            }

            // ?? FIX 3: Compare string statuses cleanly using StringComparison (ignores casing bugs)
            if (status.Equals("paid", StringComparison.OrdinalIgnoreCase))
            {
                // Check for duplicate transaction processing attempts
                if (PaymentService.PaymentExists(invoiceId))
                {
                    throw new InvalidOperationException($"Duplicate payment execution rejected. Invoice: '{invoiceId}' has already been processed.");
                }

                    // Ensure our internal operational identifier exists
                    if (clubId <= 0)
                    {
                        throw new KeyNotFoundException($"Missing or invalid custom metadata mapping property 'clubId' for invoice ID: '{invoiceId}'.");
                    }

                    // Process the business logic update routine
                    await PaymentService.ProcessPaymentWebhook(
                    clubId,
                    planName: "Premium",
                    amount: finalAmount,
                    currency: currency,
                    paymentMethod: paymentMethod,
                    chargilyInvoiceId: invoiceId
                    );

                    string cacheKey = $"club_expiry_{clubId}";

                    var cache = BusinessLayer.ClubSubscription.GetCacheInstance();
                    // Use a clean pattern to remove it
                    cache.Remove(cacheKey);

                }
            else if (status.Equals("failed", StringComparison.OrdinalIgnoreCase) || status.Equals("expired", StringComparison.OrdinalIgnoreCase))
            {
                // Throw a custom trace exception for monitoring logging handlers
                throw new Exception($"Chargily payment gateway reported a failed or expired transaction status outcome ('{status}') for Invoice: '{invoiceId}'.");
            }
        }
        catch (Exception ex)
        {
                // Re-throw the exception so it bubbles out of this method and hits the Controller wrapper's 500 block
                throw;
        }
    }
}
}

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization; // 🚀 Added for JsonPropertyName

namespace DataAccessLayer.DTOs.Chargily
{
    public class ChargilyWebhookDTO
    {
        public string Id { get; set; }
        public string Type { get; set; }

        [JsonPropertyName("data")] // 🚀 Forces System.Text.Json to locate the nested payload block
        public WebhookData Data { get; set; }

        public class WebhookData
        {
            public string Id { get; set; }
            public string Status { get; set; }

            [JsonPropertyName("amount")] // 🚀 Forces mapping to the raw JSON "amount" property number value
            public decimal Amount { get; set; }

            public string Currency { get; set; }

            [JsonPropertyName("invoice_id")]
            public string InvoiceId { get; set; }

            [JsonPropertyName("payment_method")]
            public string PaymentMethod { get; set; }

            public List<string> Metadata { get; set; }
        }
    }
}
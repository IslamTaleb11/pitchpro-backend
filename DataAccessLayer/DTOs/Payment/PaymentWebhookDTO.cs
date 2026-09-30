namespace DataAccessLayer.DTOs.Payment
{
    public class PaymentWebhookDTO
    {
        public string InvoiceNumber { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string ClientReference { get; set; }  // Can contain club_id or other metadata
        public string PaymentMethod { get; set; }
    }
}

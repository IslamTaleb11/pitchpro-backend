namespace DataAccessLayer.DTOs.Payment
{
    public class PaymentSubscriptionResponseDTO
    {
        public int Id { get; set; }
        public string PlanName { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public int PaymentMethodId { get; set; }
        public string ChargilyInvoiceId { get; set; }
        public bool IsActive { get; set; }
        public int ClubSubscriptionId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}

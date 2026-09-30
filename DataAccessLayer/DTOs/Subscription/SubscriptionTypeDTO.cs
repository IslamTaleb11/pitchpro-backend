namespace DataAccessLayer.DTOs.Subscription
{
    public class SubscriptionTypeDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public int DurationInDays { get; set; }
    }
}
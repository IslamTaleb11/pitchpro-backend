using DataAccessLayer.Providers;


namespace BusinessLayer
{
    public class Payment
    {
        public static async Task<decimal> GetSubscriptionPriceByName(string typeName)
        {
            return await PaymentProvider.GetSubscriptionPriceByName(typeName);
        }
    }
}

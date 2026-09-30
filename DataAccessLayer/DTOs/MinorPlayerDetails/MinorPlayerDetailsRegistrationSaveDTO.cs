namespace DataAccessLayer.DTOs.MinorPlayerDetails
{
    public class MinorPlayerDetailsRegistrationSaveDTO
    {
        public int CategoryID { get; set; }
        public int PaymentStatusID { get; set; }
        public string GuardianFullName { get; set; }
        public string GuardianPhone { get; set; }
        public int PlayerID { get; set; }
    }
}

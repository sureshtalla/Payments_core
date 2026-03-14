namespace Payments_core.Models
{
    public class WalletLoadSuccessDto
    {
        public int UserId { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public int ProviderId { get; set; }
        public int ProductTypeId { get; set; }
        public int PaymentModeId { get; set; }
    }

 
}

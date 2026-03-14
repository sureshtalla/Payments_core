namespace Payments_core.Models
{
    public class PayinCreateRequest
    {
        public long UserId { get; set; }
        public long MerchantId { get; set; }
        public decimal Amount { get; set; }
    }
}
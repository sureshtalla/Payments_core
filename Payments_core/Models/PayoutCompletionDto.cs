namespace Payments_core.Models
{
    public class PayoutCompletionDto
    {
        public long UserId { get; set; }
        public string TransactionId { get; set; }
        public string HoldTxnId { get; set; }
        public decimal Amount { get; set; }
        public decimal FeeAmount { get; set; }
        public bool Success { get; set; }
    }
}

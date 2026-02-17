namespace Payments_core.Models.BBPS
{
    public class BbpsPendingTxnDto
    {
        public string TxnRefId { get; set; }
        public string BillRequestId { get; set; }

        // Optional (if needed later)
        public long UserId { get; set; }
        public decimal Amount { get; set; }

        public string BillerId { get; set; }
        public string BillerName { get; set; }
        public string PaymentMode { get; set; }

        public int SmsSent { get; set; }
    }
}

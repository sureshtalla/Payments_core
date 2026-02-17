namespace Payments_core.Models.BBPS
{
    public class BbpsReceiptDto
    {
        public string TxnReferenceId { get; set; }
        public string BillerId { get; set; }
        public string BillerName { get; set; }
        public string CustomerName { get; set; }
        public string MobileNumber { get; set; }
        public decimal BillAmount { get; set; }
        public decimal CCF { get; set; }
        public decimal TotalAmount { get; set; }
        public string BillDate { get; set; }
        public string BillPeriod { get; set; }
        public string BillNumber { get; set; }
        public string DueDate { get; set; }
        public string PaymentMode { get; set; }
        public string TransactionStatus { get; set; }
        public DateTime TxnDate { get; set; }
        public string ApprovalNumber { get; set; }
    }
}
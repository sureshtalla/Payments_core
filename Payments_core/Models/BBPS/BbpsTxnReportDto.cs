namespace Payments_core.Models.BBPS
{
    public class BbpsTxnReportDto
    {
        public string? BillerId { get; set; }
        public string? BillerName { get; set; }
        public string? BillerCategory { get; set; }
        public decimal Amount { get; set; }
        public DateTime? TxnDate { get; set; }
        public string? TxnReferenceId { get; set; }
        public string? TxnStatus { get; set; }
        public string? ResponseCode { get; set; }
        public string? PaymentMode { get; set; }
        public string? RequestId { get; set; }
        public string? AgentName { get; set; }
        public string? AgentMobile { get; set; }
    }
}
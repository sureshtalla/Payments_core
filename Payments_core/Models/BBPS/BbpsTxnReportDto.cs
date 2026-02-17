namespace Payments_core.Models.BBPS
{
    public class BbpsTxnReportDto
    {
        public string AgentId { get; set; }
        public decimal Amount { get; set; }
        public string BillerName { get; set; }
        public DateTime TxnDate { get; set; }
        public string TxnReferenceId { get; set; }
        public string TxnStatus { get; set; }
    }
}
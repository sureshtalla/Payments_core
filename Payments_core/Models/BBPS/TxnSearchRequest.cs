namespace Payments_core.Models.BBPS
{
    public class TxnSearchRequest
    {
        public string? TxnRefId { get; set; }
        public string? Mobile { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
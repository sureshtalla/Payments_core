namespace Payments_core.Models.BBPS
{
    public class BbpsStatusResponseDto
    {
        public string? ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
        public string? TxnRefId { get; set; }
        public string? Status { get; set; }

        // ✅ ADD THIS
        public string RawXml { get; set; }

        public string CustomerName { get; set; }
        public string PaidAmount { get; set; }
        public string ApprovalRefNumber { get; set; }
        public string BillNumber { get; set; }
        public string DueDate { get; set; }
    }
}

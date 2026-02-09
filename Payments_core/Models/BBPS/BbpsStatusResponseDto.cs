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
    }
}

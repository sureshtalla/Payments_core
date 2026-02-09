namespace Payments_core.Models.BBPS
{
    public class BbpsPayResponseDto
    {
        public string? ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
        public string? TxnRefId { get; set; }
        public string? Status { get; set; }
    }
}

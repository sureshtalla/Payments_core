namespace Payments_core.Models.BBPS
{
    public class BbpsFetchResponseDto
    {
        public string? ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
        public string? BillRequestId { get; set; }
        public string? CustomerName { get; set; }
        public decimal BillAmount { get; set; }
        public DateTime DueDate { get; set; }
    }
}

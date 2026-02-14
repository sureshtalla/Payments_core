namespace Payments_core.Models.BBPS
{
    public class BbpsFetchResponseDto
    {
        public string? ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
        public string? BillRequestId { get; set; }
        public string? RequestId { get; set; }

        public string? CustomerName { get; set; }
        public decimal BillAmount { get; set; }
        public DateTime DueDate { get; set; }

        public List<InputParamDto>? InputParams { get; set; }
        public BillerResponseDto? BillerResponse { get; set; }
        public List<AdditionalInfoDto>? AdditionalInfo { get; set; }
    }

    public class InputParamDto
    {
        public string? ParamName { get; set; }
        public string? ParamValue { get; set; }
    }

    public class AdditionalInfoDto
    {
        public string? InfoName { get; set; }
        public string? InfoValue { get; set; }
    }

    public class AmountOptionDto
    {
        public string? AmountName { get; set; }
        public string? AmountValue { get; set; }
    }

    public class BillerResponseDto
    {
        public string? BillAmount { get; set; }
        public string? BillDate { get; set; }
        public string? BillNumber { get; set; }
        public string? BillPeriod { get; set; }
        public string? CustomerName { get; set; }
        public string? DueDate { get; set; }
        public List<AmountOptionDto>? AmountOptions { get; set; }
    }
}

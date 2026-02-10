namespace Payments_core.Models.BBPS
{
    public class BbpsBillValidationResponseDto
    {
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public bool IsValid =>
            ResponseCode == "000";
    }
}

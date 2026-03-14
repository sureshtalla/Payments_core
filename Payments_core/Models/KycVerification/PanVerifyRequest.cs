namespace Payments_core.Models.KycVerification
{
    public class PanVerifyRequest
    {
        public long UserId { get; set; }

        public string Pan { get; set; }
    }
}

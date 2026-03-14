namespace Payments_core.Models.KycVerification
{
    public class AadhaarVerifyRequest
    {
        public long UserId { get; set; }

        public string Aadhaar { get; set; }
    }
}

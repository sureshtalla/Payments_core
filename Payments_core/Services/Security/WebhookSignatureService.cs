using System.Security.Cryptography;
using System.Text;

namespace Payments_core.Services.Security
{
    public class WebhookSignatureService
    {
        public bool VerifyHmac(string payload, string receivedSignature, string secret)
        {
            var key = Encoding.UTF8.GetBytes(secret);
            var message = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(key);

            var hash = hmac.ComputeHash(message);

            var computedSignature = BitConverter
                .ToString(hash)
                .Replace("-", "")
                .ToLower();

            return computedSignature == receivedSignature.ToLower();
        }
    }
}
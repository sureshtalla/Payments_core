using System.Security.Cryptography;
using System.Text;

namespace Payments_core.Services.Security
{
    public class WebhookSignatureService
    {
        private readonly IConfiguration _config;

        public WebhookSignatureService(IConfiguration config)
        {
            _config = config;
        }

        // ── Get real secrets from config (never hardcoded) ─────────────────
        public string GetCashfreeSecret()
            => _config["Webhook:CashfreeSecret"]
               ?? throw new InvalidOperationException(
                    "Cashfree webhook secret not configured. Set CASHFREE_WEBHOOK_SECRET env var.");

        public string GetRazorpaySecret()
            => _config["Webhook:RazorpaySecret"]
               ?? throw new InvalidOperationException(
                    "Razorpay webhook secret not configured. Set RAZORPAY_WEBHOOK_SECRET env var.");

        // ── Constant-time HMAC verification (prevents timing attacks) ───────
        public bool VerifyHmac(string payload, string receivedSignature, string secret)
        {
            if (string.IsNullOrEmpty(payload)
             || string.IsNullOrEmpty(receivedSignature)
             || string.IsNullOrEmpty(secret))
                return false;

            var key = Encoding.UTF8.GetBytes(secret);
            var message = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(message);

            var computedHex = Convert.ToHexString(hash).ToLowerInvariant();
            var receivedHex = receivedSignature.ToLowerInvariant();

            var computedBytes = Encoding.UTF8.GetBytes(computedHex);
            var receivedBytes = Encoding.UTF8.GetBytes(receivedHex);

            // Length must match first — otherwise FixedTimeEquals throws
            if (computedBytes.Length != receivedBytes.Length)
                return false;

            // Constant-time comparison — prevents timing side-channel attacks
            return CryptographicOperations.FixedTimeEquals(computedBytes, receivedBytes);
        }
    }
}
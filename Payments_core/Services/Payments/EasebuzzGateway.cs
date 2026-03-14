using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Payments_core.Models.Settings;

namespace Payments_core.Services.Payments
{
    public class EasebuzzGateway : IPaymentGateway
    {
        private readonly HttpClient _http;
        private readonly PaymentSettings _settings;

        public string Code => "PG1_Easebuzz";

        public EasebuzzGateway(
            HttpClient http,
            IOptions<PaymentSettings> settings)
        {
            _http = http;
            _settings = settings.Value;
        }

        public async Task<string> CreatePayin(
            string requestId,
            decimal amount,
            string callbackUrl,
            dynamic provider)
        {
            var payload = new
            {
                txnid = requestId,
                amount = amount,
                productinfo = "FINX PAYIN",
                firstname = "Customer",
                phone = "9999999999",
                email = "test@test.com",

                // Redirect after payment
                surl = $"{provider.config_json.return_url}/admin/payment-callback/{requestId}",
                furl = $"{provider.config_json.return_url}/admin/payment-callback/{requestId}"
            };

            var req = new HttpRequestMessage(
                HttpMethod.Post,
                provider.config_json.api_url);

            req.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            req.Headers.Add("key", provider.api_key);

            var res = await _http.SendAsync(req);

            return await res.Content.ReadAsStringAsync();
        }

        public Task<string> CreatePayout(
            string txnId,
            decimal amount,
            string account,
            string ifsc,
            dynamic provider)
        {
            throw new NotImplementedException();
        }
    }
}
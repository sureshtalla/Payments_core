using System.Text;
using System.Text.Json;

namespace Payments_core.Services.Payments
{
    public class CashfreeGateway : IPaymentGateway
    {
        private readonly HttpClient _http;

        public string Code => "CashFree";

        public CashfreeGateway(HttpClient http)
        {
            _http = http;
        }

        public async Task<string> CreatePayin(
           string requestId,
           decimal amount,
           string callbackUrl,
           dynamic provider)
        {
            var payload = new
            {
                order_id = requestId,
                order_amount = amount,
                order_currency = "INR",
                customer_details = new
                {
                    customer_id = "FINX_USER",
                    customer_email = "customer@test.com",
                    customer_phone = "9999999999"
                },
                order_meta = new
                {
                    return_url = callbackUrl
                }
            };

            var req = new HttpRequestMessage(
                HttpMethod.Post,
                "https://sandbox.cashfree.com/pg/orders");

            req.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            req.Headers.Add("x-client-id", provider.api_key);
            req.Headers.Add("x-client-secret", provider.api_secret);

            var res = await _http.SendAsync(req);

            var json = await res.Content.ReadAsStringAsync();

            dynamic obj = JsonSerializer.Deserialize<dynamic>(json);

            return obj.GetProperty("payment_link").GetString();
        }

        public async Task<string> CreatePayout(
            string txnId,
            decimal amount,
            string account,
            string ifsc,
            dynamic provider)
        {
            var payload = new
            {
                beneId = account,
                amount = amount,
                transferId = txnId,
                transferMode = "IMPS"
            };

            var req = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.cashfree.com/payout/v1/requestTransfer");

            req.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            req.Headers.Add("X-Client-Id", provider.api_key);
            req.Headers.Add("X-Client-Secret", provider.api_secret);

            var res = await _http.SendAsync(req);

            var json = await res.Content.ReadAsStringAsync();

            dynamic obj = JsonSerializer.Deserialize<dynamic>(json);

            return obj.GetProperty("data").GetProperty("payment_url").GetString();
        }
    }
}
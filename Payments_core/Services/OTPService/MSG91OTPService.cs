using System.Text;
using System.Text.Json;
using Payments_core.Models;
using Payments_core.Services.DataLayer;
using static System.Net.WebRequestMethods;

namespace Payments_core.Services.OTPService
{
    public class MSG91OTPService : IMSG91OTPService
    {
        private readonly IDapperContext _dbContext;

        public MSG91OTPService(IDapperContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> MSG91SendOTPAsync(string otp, string mobileNumber, string authKey, string templateId, string msgURL)
        {
            using var client = new HttpClient();

            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("authkey", authKey);

            var payload = new
            {
                template_id = templateId,
                recipients = new[]
                 {
                    new
                    {
                        mobiles = mobileNumber,
                        OTP = otp
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(msgURL, content);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                return true;
            }

            return false;
        }

        public async Task<MSGOTPConfig> GetMSGOTPConfigAsync()
        {
            return await _dbContext.GetSingleData<MSGOTPConfig>("sp_GetMSGConfig", null);
        }

        public async Task<bool> SendPaymentFlowSmsAsync(
        string mobile,
        string txnId,
        decimal amount,
        string billerName,
        string billerId,
        string mode,
        string status,
        string authKey,
        string flowId,
        string msgUrl)
        {
            try
            {
                using var client = new HttpClient();

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("authkey", authKey);

                var payload = new
                {
                    flow_id = flowId,
                    sender = "FINX",
                    mobiles = mobile,
                    VAR1 = amount.ToString("0.00"),
                    VAR2 = billerName,
                    VAR3 = billerId,
                    VAR4 = txnId,
                    VAR5 = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
                    VAR6 = mode
                };

                var response = await client.PostAsJsonAsync(msgUrl, payload);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine("MSG91 Payment SMS Error: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> SendComplaintFlowSmsAsync(
        string mobileNumber,
        string txnId,
        string complaintId,
        string authKey,
        string flowId,
        string msgUrl)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("authkey", authKey);

                var payload = new
                {
                    flow_id = flowId,
                    mobiles = mobileNumber,
                    txnId = txnId,
                    complaintId = complaintId
                };

                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(msgUrl, content);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}

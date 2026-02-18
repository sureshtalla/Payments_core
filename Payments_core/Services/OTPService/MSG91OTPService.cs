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
     decimal amount,
     string billerName,
     string consumerNo,
     string txnId,
     string mode,
     string authKey,
     string templateId,
     string msgUrl)
        {
            try
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
                    mobiles = mobile,

                    // 🔥 EXACT MATCH WITH TEMPLATE
                    var1 = amount.ToString("0.00"),
                    var2 = billerName,
                    var3 = consumerNo,
                    var4 = txnId,
                    var5 = DateTime.Now.ToString("dd-MM-yyyy hh:mm tt"),
                    var6 = mode
                }
            }
                };

                var json = JsonSerializer.Serialize(payload);

                Console.WriteLine("MSG91 PAYMENT PAYLOAD:");
                Console.WriteLine(json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(msgUrl, content);

                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine("MSG91 RESPONSE: " + result);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine("MSG91 Payment Error: " + ex.Message);
                return false;
            }
        }

        // ================================
        // ✅ COMPLAINT SMS (FLOW)
        // ================================
        public async Task<bool> SendComplaintFlowSmsAsync(
         string txnId,
         string complaintId,
         string mobile,
         string authKey,
         string templateId,
         string msgUrl)
        {
            try
            {
                using var client = new HttpClient();

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("accept", "application/json");
                client.DefaultRequestHeaders.Add("authkey", authKey);

                var payload = new
                {
                    template_id = templateId,   // ✅ TEMPLATE ID (not flow_id)

                    recipients = new[]
                    {
                new
                {
                    mobiles = mobile,

                    // 🔥 MUST MATCH ##var1## ##var2##
                    var1 = txnId,
                    var2 = complaintId
                }
            }
                };

                var json = JsonSerializer.Serialize(payload);

                Console.WriteLine("MSG91 COMPLAINT PAYLOAD:");
                Console.WriteLine(json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(msgUrl, content);

                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine("MSG91 COMPLAINT RESPONSE: " + result);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine("MSG91 Complaint SMS Error: " + ex.Message);
                return false;
            }
        }
    }
}

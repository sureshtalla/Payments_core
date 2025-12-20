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
    }
}

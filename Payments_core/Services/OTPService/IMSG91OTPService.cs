using Payments_core.Models;

namespace Payments_core.Services.OTPService
{
    public interface IMSG91OTPService
    {
        Task<MSGOTPConfig> GetMSGOTPConfigAsync();
        Task<bool> MSG91SendOTPAsync(string otp, string mobileNumber, string authKey, string templateId, string msgURL);
    }
}

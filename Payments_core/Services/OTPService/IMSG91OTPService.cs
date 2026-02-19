using Payments_core.Models;

namespace Payments_core.Services.OTPService
{
    public interface IMSG91OTPService
    {
        Task<MSGOTPConfig> GetMSGOTPConfigAsync();
        Task<bool> MSG91SendOTPAsync(string otp, string mobileNumber, string authKey, string templateId, string msgURL);

        Task<bool> SendPaymentTemplateSmsAsync(
        string mobile,
        decimal amount,
        string billerName,
        string consumerNo,
        string txnId,
        string mode,
        string authKey,
        string templateId,
        string msgUrl
    );

        Task<bool> SendComplaintTemplateSmsAsync(
            string mobile,
            string txnId,
            string complaintId,
            string authKey,
            string templateId,
            string msgUrl
        );
    }
}

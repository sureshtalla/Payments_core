using Payments_core.Models;

namespace Payments_core.Services.OTPService
{
    public interface IMSG91OTPService
    {
        Task<MSGOTPConfig> GetMSGOTPConfigAsync();
        Task<bool> MSG91SendOTPAsync(string otp, string mobileNumber, string authKey, string templateId, string msgURL);

        // ✅ PAYMENT TEMPLATE SMS
        Task<bool> SendPaymentFlowSmsAsync(
             string mobile,
            decimal amount,
            string billerName,
            string billerId,
            string txnId,
            string mode,
            string authKey,
            string templateId,
            string msgUrl
        );

        // ✅ COMPLAINT FLOW SMS
        Task<bool> SendComplaintFlowSmsAsync(
            string txnId,
            string complaintId,
              string mobile,
            string authKey,
            string templateId,
            string msgUrl
        );
    }
}

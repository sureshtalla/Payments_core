using Payments_core.Models;

namespace Payments_core.Services.OTPService
{
    public interface IMSG91OTPService
    {
        Task<MSGOTPConfig> GetMSGOTPConfigAsync();
        Task<bool> MSG91SendOTPAsync(string otp, string mobileNumber, string authKey, string templateId, string msgURL);

        Task<bool> SendPaymentFlowSmsAsync(
        string mobile,
        string txnId,
        decimal amount,
        string billerName,
        string billerId,
        string mode,
        string status,
        string authKey,
        string flowId,
        string msgUrl
    );
    Task<bool> SendComplaintFlowSmsAsync(
    string mobileNumber,
    string txnId,
    string complaintId,
    string authKey,
    string flowId,
    string msgUrl
);
    }
}

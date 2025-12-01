namespace Payments_core.Services.UserDataService
{
    public interface IOtpService
    {
        Task<string> GenerateOtpAsync(long userId, string mobile);
        Task<bool> VerifyOtpAsync(long userId, string otp);
    }
}

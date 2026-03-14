namespace Payments_core.Services.Payments
{
    public interface IPaymentGateway
    {
        string Code { get; }

        Task<string> CreatePayin(
            string requestId,
            decimal amount,
            string callbackUrl,
            dynamic provider);

        Task<string> CreatePayout(
            string txnId,
            decimal amount,
            string account,
            string ifsc,
            dynamic provider);
    }
}
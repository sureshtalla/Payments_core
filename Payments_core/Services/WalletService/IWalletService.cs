using Payments_core.Models;
using System.Threading.Tasks;

namespace Payments_core.Services.WalletService
{
    public interface IWalletService
    {
        // PAYIN
        Task<int> WalletLoad(WalletLoadInit req);
        Task<int> WalletLoadCommissionPercent(WalletLoadInit req);
        Task<int> UpdateWalletLoadStatus(long userId, string txnId, int statusId, string remarks);

        // PAYOUT
        Task<int> PayoutInitAsync(PayoutRequest req);
        Task<int> PayoutAsync(PayoutRequest req);

        // LIMIT CONTROL
        Task CheckDailyPayoutLimit(long userId, decimal amount);

        // WALLET TRANSFER
        Task<int> WalletTransfer(WalletTransferInit req);
        // ============================
        // PAYIN (MULTI PG)
        // ============================
        Task<string> CreatePayinTransaction(
            long userId,
            long merchantId,
            decimal amount,
            string callbackUrl);


        // ============================
        // PAYOUT (MULTI PG)
        // ============================
        Task<string> CreatePayoutOrder(
            long userId,
            int beneficiaryId,
            decimal amount,
            decimal fee,
            string mode,
            string tpin);


        // ============================
        // PROVIDER ROUTING
        // ============================
        Task<IEnumerable<dynamic>> GetProviders(string type);

        // HOLD SYSTEM
        Task<string> HoldAsync(long userId, decimal amount, string sourceType, string sourceId, string narration);
        Task FinalizeAsync(long userId, decimal amount, string sourceType, string sourceId, string txnId, string narration);
        Task ReleaseAsync(long userId, decimal amount, string sourceType, string sourceId, string txnId, string narration);

        // BENEFICIARY
        Task<int> CreateBeneficiary(Beneficiary req);
        Task<int> VerifyBeneficiary(int id);
        Task<IEnumerable<BeneficiaryDto>> GetBeneficiaries(int userId);

        // REPORT
        Task<IEnumerable<LedgerReport>> GetLedgerReport(DateTime from, DateTime to, int type, int userId);

        // 🔥 BACKWARD COMPATIBILITY (KEEP FOR UAT)
        //Task<string> HoldAmount(long userId, decimal amount, string narration);
        //Task DebitFromHold(long userId, decimal amount, string refId, string narration);
        //Task ReleaseHold(long userId, decimal amount, string refId, string narration);
        //Task FinalizeDebit(long userId, decimal amount, string referenceId, string narration);
        Task ReverseHold(long userId, decimal amount, string referenceId, string narration);
        Task FinalizeIfPending(string txnRefId);
        Task RefundIfPending(string txnRefId);


        Task<long> InsertWebhookLog(
        int providerId,
        string eventType,
        string headers,
        string payload);

        Task UpdateWebhookStatus(
            long logId,
            string status);

        Task<dynamic?> GetPgTransaction(string requestId);

        Task UpdatePgTransactionStatus(
            string requestId,
            string status,
            string payload);

        Task<bool> IsWalletCredited(string requestId);

        Task ProcessPayinWalletCredit(string requestId);
        Task<IEnumerable<dynamic>> GetPendingReconTransactions();

        Task<dynamic?> GetWalletBalance(long userId);
        Task<dynamic?> GetPaymentStatus(string requestId);
        Task UpdateWebhookTxnLink(
        long logId,
        long? txnId);
    }
}
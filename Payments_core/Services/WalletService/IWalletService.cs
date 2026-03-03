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
    }
}
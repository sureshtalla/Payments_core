using System.Threading.Tasks;

namespace Payments_core.Services.WalletService
{
    public interface IWalletService
    {
        Task<string> HoldAmount(      // ✅ changed long → string
            long userId,
            decimal amount,
            string narration
        );

        Task DebitFromHold(
            long userId,
            decimal amount,
            string refId,
            string narration
        );

        Task ReleaseHold(
            long userId,
            decimal amount,
            string refId,
            string narration
        );

        Task FinalizeIfPending(string txnRefId);

        Task RefundIfPending(string txnRefId);

        Task FinalizeDebit(
            long userId,
            decimal amount,
            string referenceId,
            string narration
        );

        Task ReverseHold(
            long userId,
            decimal amount,
            string referenceId,
            string narration
        );
    }
}
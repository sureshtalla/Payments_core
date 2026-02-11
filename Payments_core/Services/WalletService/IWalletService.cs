namespace Payments_core.Services.WalletService
{
    public interface IWalletService
    {
        /// <summary>
        /// Hold amount before BBPS payment
        /// </summary>
        //Task<long> HoldAmount(
        //    long userId,
        //    decimal amount,
        //    string narration
        //);

        Task<string> HoldAmount(long userId, decimal amount, string narration);

        /// <summary>
        /// Final debit after BBPS success
        /// </summary>
        Task DebitFromHold(
            long userId,
            decimal amount,
            string refId,
            string narration
        );

        /// <summary>
        /// Release hold if BBPS failed or reversed
        /// </summary>
        Task ReleaseHold(
         long userId,
         decimal amount,
         string refId,
         string narration
 );

        /// <summary>
        /// Used by requery job when status becomes SUCCESS
        /// </summary>
        Task FinalizeIfPending(string txnRefId);

        /// <summary>
        /// Used by requery job when status becomes FAILED
        /// </summary>
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
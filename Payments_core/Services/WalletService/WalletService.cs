using Payments_core.Services.DataLayer;
using System;
using System.Threading.Tasks;

namespace Payments_core.Services.WalletService
{
    public class WalletService : IWalletService
    {
        private readonly IDapperContext _db;

        public WalletService(IDapperContext db)
        {
            _db = db;
        }

        public async Task<string> HoldAmount(   // ✅ changed return type
            long userId,
            decimal amount,
            string narration)
        {
            var txnId = Guid.NewGuid().ToString("N");

            Console.WriteLine($"[WALLET][HOLD] TxnId={txnId}, UserId={userId}, Amount={amount}");

            await _db.ExecuteStoredAsync("sp_wallet_hold_amount", new
            {
                p_user_id = userId,
                p_amount = amount,
                p_txn_id = txnId,
                p_narration = narration
            });

            return txnId; // ✅ safe return
        }

        public Task DebitFromHold(
            long userId,
            decimal amount,
            string refId,
            string narration)
            => _db.ExecuteStoredAsync("sp_wallet_debit_from_hold", new
            {
                p_user_id = userId,
                p_amount = amount,
                p_txn_id = refId,
                p_narration = narration
            });

        public Task ReleaseHold(
            long userId,
            decimal amount,
            string refId,
            string narration)
            => _db.ExecuteStoredAsync("sp_wallet_release_hold", new
            {
                p_user_id = userId,
                p_amount = amount,
                p_txn_id = refId,
                p_narration = narration
            });

        public Task FinalizeDebit(
            long userId,
            decimal amount,
            string referenceId,
            string narration)
            => _db.ExecuteStoredAsync("sp_wallet_finalize_if_pending", new
            {
                p_user_id = userId,
                p_amount = amount,
                p_txn_id = referenceId,
                p_narration = narration
            });

        public Task ReverseHold(
            long userId,
            decimal amount,
            string referenceId,
            string narration)
            => _db.ExecuteStoredAsync("sp_wallet_release_if_pending", new
            {
                p_user_id = userId,
                p_amount = amount,
                p_txn_id = referenceId,
                p_narration = narration
            });

        public Task FinalizeIfPending(string txnRefId)
            => Task.CompletedTask;

        public Task RefundIfPending(string txnRefId)
            => Task.CompletedTask;
    }
}
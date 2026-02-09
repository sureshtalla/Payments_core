using Payments_core.Services.DataLayer;

namespace Payments_core.Services.WalletService
{
    public class WalletService : IWalletService
    {
        private readonly IDapperContext _db;

        public WalletService(IDapperContext db)
        {
            _db = db;
        }

        public async Task<long> HoldAmount(long userId, decimal amount, string narration)
        {
            var txnId = Guid.NewGuid().ToString("N");

            await _db.ExecuteStoredAsync("sp_wallet_hold_amount", new
            {
                p_user_id = userId,
                p_amount = amount,
                p_txn_id = txnId,
                p_narration = narration
            });

            // Return numeric reference usable in BBPS tables
            return long.Parse(txnId.Substring(0, 18));
        }

        public Task DebitFromHold(long userId, decimal amount, string refId, string narration)
            => _db.ExecuteStoredAsync("sp_wallet_debit_from_hold", new
            {
                p_user_id = userId,
                p_amount = amount,
                p_txn_id = refId,
                p_narration = narration
            });

        public Task ReleaseHold(long userId, decimal amount, long refId, string narration)
            => _db.ExecuteStoredAsync("sp_wallet_release_hold", new
            {
                p_user_id = userId,
                p_amount = amount,
                p_txn_id = refId.ToString(),
                p_narration = narration
            });

        public Task FinalizeIfPending(string txnRefId)
        {
            // Implement only if you want auto-settlement from requery
            return Task.CompletedTask;
        }

        public Task RefundIfPending(string txnRefId)
        {
            // Implement only if you want auto-refund from requery
            return Task.CompletedTask;
        }
    }
}
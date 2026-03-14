using Payments_core.Services.DataLayer;
using Payments_core.Services.WalletService;

namespace Payments_core.Services.Reconciliation
{
    public class ReconciliationService
    {
        private readonly IWalletService _wallet;
        private readonly IDapperContext _db;

        public ReconciliationService(
            IWalletService wallet,
            IDapperContext db)
        {
            _wallet = wallet;
            _db = db;
        }

        // PAYIN RECON (existing)
        public async Task RunPayinReconciliation()
        {
            var txns = await _wallet.GetPendingReconTransactions();

            foreach (var txn in txns)
            {
                try
                {
                    var credited =
                        await _wallet.IsWalletCredited(txn.request_id);

                    if (!credited)
                    {
                        await _wallet.ProcessPayinWalletCredit(
                            txn.request_id);
                    }
                }
                catch
                {
                    // log error
                }
            }
        }

        // NEW BANK RECON
        public async Task RunBankWalletReconciliation()
        {
            await _db.ExecuteStoredAsync(
                "sp_reconcile_bank_wallet",
                null);
        }

        // =======================================
        // PG TIMEOUT RECOVERY
        // =======================================
        public async Task RunPgTimeoutRecovery()
        {
            var txns = await _db.GetData<dynamic>(
                "sp_pg_recovery_pending",
                null);

            foreach (var txn in txns)
            {
                try
                {
                    var credited =
                        await _wallet.IsWalletCredited(
                            txn.request_id);

                    if (!credited)
                    {
                        await _wallet.ProcessPayinWalletCredit(
                            txn.request_id);
                    }
                }
                catch
                {
                    // ignore individual txn failure
                }
            }
        }

        public async Task RunWebhookRecovery()
        {
            var rows = await _db.GetData<dynamic>(
                "sp_webhook_failed",
                null);

            foreach (var w in rows)
            {
                try
                {
                    var txn =
                        await _wallet.GetPgTransaction(
                            w.request_id);

                    if (txn?.status == "SUCCESS")
                    {
                        await _wallet.ProcessPayinWalletCredit(
                            w.request_id);

                        await _wallet.UpdateWebhookStatus(
                            w.id,
                            "PROCESSED");
                    }
                }
                catch
                {
                }
            }
        }
        public async Task RunWalletVsBankReconciliation()
        {
            await _db.ExecuteStoredAsync(
                "sp_reconcile_wallet_vs_bank",
                null);
        }
    }
}
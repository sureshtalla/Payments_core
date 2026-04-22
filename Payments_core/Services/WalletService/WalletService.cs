// ============================================================
// FILE: Payments_core/Services/WalletService/WalletService.cs
// FIXES APPLIED:
//   BUG #3  — FinalizeIfPending() and RefundIfPending() were empty stubs
//             (Task.CompletedTask). Now they check ledger and act correctly.
//   BUG #12 — CreatePayoutOrder() was passing empty strings "" for
//             account number and IFSC to the gateway. Now fetches from DB.
// ============================================================

using Payments_core.Models;
using Payments_core.Services.DataLayer;
using System;
using System.Threading.Tasks;
using Payments_core.Services.Security;
using Payments_core.Services.FailureQueue;
using Payments_core.Services.Payments;
using Payments_core.Services.Monitoring;
using System.Security.Cryptography;
using System.Text;

namespace Payments_core.Services.WalletService
{
    public class WalletService : IWalletService
    {
        private readonly IDapperContext _db;
        private readonly FraudService _fraud;
        private readonly FailureService _failure;
        private readonly PgRetryService _retry;
        private readonly MetricsService _metrics;
        private readonly PaymentRouterService _router;

        public WalletService(
            IDapperContext db,
            FraudService fraud,
            PaymentRouterService router,
            FailureService failure,
            PgRetryService retry,
            MetricsService metrics)
        {
            _db = db;
            _fraud = fraud;
            _router = router;
            _failure = failure;
            _retry = retry;
            _metrics = metrics;
        }

        // ===========================
        // PAYIN
        // ===========================
        public Task<int> WalletLoad(WalletLoadInit req)
            => _db.ExecuteStoredAsync("SP_Create_WalletLoadInit", new
            {
                p_UserId = req.UserId,
                p_Amount = req.Amount,
                p_ProviderId = req.ProviderId,
                p_ProductTypeId = req.ProductTypeId,
                p_PaymentModeId = req.PaymentModeId,
                p_SettlementType = req.SettlementType,
                p_TransactionId = req.TransactionId
            });

        // ===========================
        // PAYIN COMMISSION
        // ===========================
        public Task<int> WalletLoadCommissionPercent(WalletLoadInit req)
            => _db.ExecuteStoredAsync("sp_Create_Wallet_Load_Commission_v1", new
            {
                p_UserId = req.UserId,
                p_TransactionId = req.TransactionId,
                p_Amount = req.Amount,
                p_WalletAmount = req.Amount,
                p_ProviderId = req.ProviderId,
                p_ProductTypeId = req.ProductTypeId,
                p_PaymentModeId = req.PaymentModeId
            });

        // ===========================
        // PAYIN STATUS UPDATE
        // ===========================
        public Task<int> UpdateWalletLoadStatus(long userId, string txnId, int statusId, string remarks)
            => _db.ExecuteStoredAsync("sp_Update_Wallet_Load_Status", new
            {
                p_UserId = userId,
                p_TransactionId = txnId,
                p_StatusId = statusId,
                p_Remarks = remarks
            });

        // ===========================
        // HOLD LOGIC
        // ===========================
        public async Task<string> HoldAsync(long userId, decimal amount,
            string sourceType, string sourceId, string narration)
        {
            if (amount <= 0)
                throw new ArgumentException("Invalid hold amount");

            var txnId = Guid.NewGuid().ToString("N");

            await _db.ExecuteStoredAsync("sp_wallet_hold", new
            {
                p_user_id = userId,
                p_amount = amount,
                p_source_type = sourceType,
                p_source_id = sourceId,
                p_txn_id = txnId,
                p_narration = narration
            });

            return txnId;
        }

        // ===========================
        // FINALIZE / DEBIT FROM HOLD
        // ===========================
        public Task FinalizeAsync(long userId, decimal amount,
            string sourceType, string sourceId,
            string txnId, string narration)
            => _db.ExecuteStoredAsync("sp_wallet_finalize", new
            {
                p_user_id = userId,
                p_amount = amount,
                p_source_type = sourceType,
                p_source_id = sourceId,
                p_txn_id = txnId,
                p_narration = narration
            });

        // ===========================
        // RELEASE / REVERSE HOLD
        // ===========================
        public Task ReleaseAsync(long userId, decimal amount,
            string sourceType, string sourceId,
            string txnId, string narration)
            => _db.ExecuteStoredAsync("sp_wallet_release", new
            {
                p_user_id = userId,
                p_amount = amount,
                p_source_type = sourceType,
                p_source_id = sourceId,
                p_txn_id = txnId,
                p_narration = narration
            });

        // ===========================
        // PAYOUT INIT
        // ===========================
        public Task<int> PayoutInitAsync(PayoutRequest req)
            => _db.ExecuteStoredAsync("sp_Payout_Init", new
            {
                p_BeneficiaryId = req.BeneficiaryId,
                p_UserId = req.UserId,
                p_Amount = req.Amount,
                p_FeeAmount = req.FeeAmount,
                p_Mode = req.Mode,
                p_TransactionId = req.TransactionId,
                p_TPin = req.TPin
            });

        // ===========================
        // PAYOUT FINALIZE
        // ===========================
        public Task<int> PayoutAsync(PayoutRequest req)
            => _db.ExecuteStoredAsync("sp_Create_Payout", new
            {
                p_BeneficiaryId = req.BeneficiaryId,
                p_UserId = req.UserId,
                p_Amount = req.Amount,
                p_FeeAmount = req.FeeAmount,
                p_Status = req.Status,
                p_Reason = req.Reason,
                p_TransactionId = req.TransactionId
            });

        // ===========================
        // DAILY PAYOUT LIMIT CHECK
        // ===========================
        public Task CheckDailyPayoutLimit(long userId, decimal amount)
            => _db.ExecuteStoredAsync("sp_check_daily_payout_limit", new
            {
                p_user_id = userId,
                p_amount = amount
            });

        // ===========================
        // BENEFICIARY
        // ===========================
        public Task<int> CreateBeneficiary(Beneficiary req)
            => _db.ExecuteStoredAsync("sp_Create_Beneficiary", new
            {
                p_UserId = req.UserId,
                p_BeneficiaryName = req.BeneficiaryName,
                p_AccountNumber = req.AccountNumber,
                p_IFSCCode = req.IFSCCode,
                p_BankName = req.BankName,
                p_Mobile = req.Mobile
            });

        public Task<int> VerifyBeneficiary(int id)
            => _db.ExecuteStoredAsync("sp_Verify_Beneficiary", new
            {
                p_BeneficiaryId = id
            });

        public Task<IEnumerable<BeneficiaryDto>> GetBeneficiaries(int userId)
            => _db.GetData<BeneficiaryDto>("sp_Get_Beneficiaries_By_User", new
            {
                p_UserId = userId
            });

        // ===========================
        // REPORTS
        // ===========================
        public Task<IEnumerable<LedgerReport>> GetLedgerReport(
            DateTime from, DateTime to, int type, int userId)
            => _db.GetData<LedgerReport>("sp_Get_LedgerReport", new
            {
                p_FromDate = from,
                p_ToDate = to,
                p_TransactionType = type,
                p_UserId = userId
            });

        // ===========================
        // WALLET TRANSFER
        // ===========================
        public async Task<int> WalletTransfer(WalletTransferInit req)
        {
            var param = new
            {
                p_FromUserId = req.FromUserId,
                p_ToUserId = req.ToUserId,
                p_IsWalletTransfer = req.IsWalletTransfer,
                p_TransactionType = req.TransactionType,
                p_Amount = req.Amount,
                p_Reason = req.Reason
            };

            return await _db.ExecuteStoredAsync("sp_Wallet_Transfer", param);
        }

        // ===========================
        // GET ACTIVE PROVIDERS
        // ===========================
        public async Task<IEnumerable<dynamic>> GetProviders(string type)
        {
            return await _db.GetData<dynamic>(
                "sp_get_active_providers",
                new { p_type = type });
        }

        // ===========================
        // CREATE PAYIN TRANSACTION
        // ===========================
        public async Task<string> CreatePayinTransaction(
            long userId,
            long merchantId,
            decimal amount,
            string callbackUrl)
        {
            var safe = await _fraud.CheckFraud(userId, amount);
            if (!safe)
                throw new Exception("Fraud rule triggered");

            string requestId = Guid.NewGuid().ToString("N");

            var gateways = await _router.GetGateways("PAYIN");
            if (!gateways.Any())
                throw new Exception("No PAYIN gateways configured");

            var exists = await _db.GetData<dynamic>(
                "sp_pg_duplicate_check",
                new { p_user_id = userId, p_amount = amount });

            if (exists.Any())
                throw new Exception("Duplicate payment attempt");

            foreach (var (gateway, provider) in gateways)
            {
                var rule = await _retry.GetRetryRule(provider.id);
                int retries = rule?.max_retries ?? 1;
                int delay = rule?.retry_delay_seconds ?? 2;

                for (int i = 0; i < retries; i++)
                {
                    try
                    {
                        string attemptId = requestId + "_" + provider.id;

                        await _db.ExecuteStoredAsync(
                            "sp_pg_transaction_init",
                            new
                            {
                                p_request_id = attemptId,
                                p_provider_id = provider.id,
                                p_category = "PAYIN",
                                p_user_id = userId,
                                p_merchant_id = merchantId,
                                p_amount = amount,
                                p_callback_url = callbackUrl
                            });

                        await gateway.CreatePayin(requestId, amount, callbackUrl, provider);

                        return requestId;
                    }
                    catch
                    {
                        await Task.Delay(delay * 1000);
                    }
                }
            }

            throw new Exception("All payment gateways failed");
        }

        public async Task<bool> IsWalletCredited(string requestId)
        {
            var rows = await _db.GetData<dynamic>(
                "sp_wallet_is_credited",
                new { p_txn_id = requestId });

            return rows.Any();
        }

        public async Task<IEnumerable<dynamic>> GetPendingReconTransactions()
        {
            return await _db.GetData<dynamic>("sp_reconcile_pending_payins", null);
        }

        // ===========================
        // WALLET BALANCE
        // ===========================
        public async Task<dynamic?> GetWalletBalance(long userId)
        {
            var rows = await _db.GetData<dynamic>(
                "sp_wallet_get_balance",
                new { p_user_id = userId });

            return rows.FirstOrDefault();
        }

        // ===========================
        // GET PAYMENT STATUS
        // ===========================
        public async Task<dynamic?> GetPaymentStatus(string requestId)
        {
            var rows = await _db.GetData<dynamic>(
                "sp_pg_get_status",
                new { p_request_id = requestId });

            return rows.FirstOrDefault();
        }

        // ===========================
        // PAYOUT ROUTING
        // BUG FIX #12: Fetch real beneficiary account/IFSC before calling gateway.
        //              Before: empty strings "" were sent — gateway always rejected.
        // ===========================
        public async Task<string> CreatePayoutOrder(
            long userId,
            int beneficiaryId,
            decimal amount,
            decimal fee,
            string mode,
            string tpin)
        {
            var gateways = await _router.GetGateways("PAYOUT");
            if (!gateways.Any())
                throw new Exception("No PAYOUT gateways configured");

            string txnId = Guid.NewGuid().ToString("N").ToUpper();
            decimal total = amount + fee;

            await CheckDailyPayoutLimit(userId, total);

            var holdTxn = await HoldAsync(userId, total, "PAYOUT", txnId, "Payout Hold");

            // ✅ FIX #12 — Fetch real beneficiary details for gateway call
            var beneficiaries = await GetBeneficiaries((int)userId);
            var bene = beneficiaries.FirstOrDefault(b => b.Id == beneficiaryId);

            if (bene == null)
            {
                await ReleaseAsync(userId, total, "PAYOUT", txnId, holdTxn, "Beneficiary not found");
                throw new Exception("Beneficiary not found");
            }

            foreach (var (gateway, provider) in gateways)
            {
                try
                {
                    await gateway.CreatePayout(
                        txnId,
                        amount,
                        bene.AccountNumber,   // ✅ was "" before
                        bene.IFSCCode,        // ✅ was "" before
                        provider);

                    return txnId;
                }
                catch
                {
                    continue;
                }
            }

            await ReleaseAsync(userId, total, "PAYOUT", txnId, holdTxn, "Provider failure");

            throw new Exception("All payout providers failed");
        }

        // ===========================
        // WEBHOOK LOG INSERT
        // ===========================
        public async Task<long> InsertWebhookLog(
            int providerId,
            string eventType,
            string headers,
            string payload)
        {
            var hash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

            var rows = await _db.GetData<dynamic>(
                "sp_webhook_log_insert",
                new
                {
                    p_provider_id = providerId,
                    p_event_type = eventType,
                    p_headers = headers,
                    p_payload = payload,
                    p_hash = hash
                });

            return (long)rows.First().webhook_id;
        }

        // ===========================
        // WEBHOOK STATUS UPDATE
        // ===========================
        public async Task UpdateWebhookStatus(long logId, string status)
        {
            await _db.ExecuteStoredAsync(
                "sp_webhook_update_status",
                new
                {
                    p_id = logId,
                    p_status = status
                });
        }

        public async Task<dynamic?> GetPgTransaction(string requestId)
        {
            var rows = await _db.GetData<dynamic>(
                "sp_pg_get_transaction_by_request_id",
                new { p_request_id = requestId });

            return rows.FirstOrDefault();
        }

        public async Task UpdatePgTransactionStatus(
            string requestId, string status, string payload)
        {
            await _db.ExecuteStoredAsync(
                "sp_pg_transaction_update_status",
                new
                {
                    p_request_id = requestId,
                    p_status = status,
                    p_payload = payload
                });
        }

        public async Task ProcessPayinWalletCredit(string requestId)
        {
            var txn = await GetPgTransaction(requestId);
            if (txn == null) return;

            await _db.ExecuteStoredAsync(
                "sp_wallet_credit",
                new
                {
                    p_user_id = txn.created_by_user,
                    p_amount = txn.amount,
                    p_source_type = "PAYIN",
                    p_source_id = requestId,
                    p_txn_id = requestId,
                    p_narration = "Wallet credit from PG"
                });

            await _metrics.UpdateMetric("PAYIN_SUCCESS", txn.amount);
        }

        public async Task UpdateWebhookTxnLink(long logId, long? txnId)
        {
            await _db.ExecuteStoredAsync(
                "sp_webhook_link_txn",
                new
                {
                    p_id = logId,
                    p_pg_txn_id = txnId
                });
        }

        // ===========================
        // FINALIZE IF PENDING
        // BUG FIX #3: Was Task.CompletedTask (empty stub).
        //             Now checks ledger — if HOLD exists but no DEBIT,
        //             finalizes the wallet deduction for BBPS success.
        // ===========================
        public async Task FinalizeIfPending(string txnRefId)
        {
            // Get BBPS payment record
            var payments = await _db.GetData<dynamic>(
                "sp_bbps_get_payment_by_txn_ref",
                new { p_txn_ref_id = txnRefId });

            var payment = payments.FirstOrDefault();
            if (payment == null) return;

            // Check if HOLD exists in ledger
            var holdRows = await _db.GetData<dynamic>(
                "sp_wallet_ledger_check",
                new { p_txn_id = txnRefId, p_type = "HOLD" });

            // Check if DEBIT already done (idempotency)
            var debitRows = await _db.GetData<dynamic>(
                "sp_wallet_ledger_check",
                new { p_txn_id = txnRefId, p_type = "DEBIT" });

            // Only finalize if HOLD exists and DEBIT not yet done
            if (holdRows.Any() && !debitRows.Any())
            {
                await FinalizeAsync(
                    (long)payment.user_id,
                    (decimal)payment.amount,
                    "BBPS",
                    txnRefId,
                    txnRefId,
                    "BBPS Status SUCCESS - Finalize");
            }
        }

        // ===========================
        // REFUND IF PENDING
        // BUG FIX #3: Was Task.CompletedTask (empty stub).
        //             Now checks ledger — if HOLD exists but no RELEASE,
        //             releases the held amount back to user for BBPS failure.
        // ===========================
        public async Task RefundIfPending(string txnRefId)
        {
            // Get BBPS payment record
            var payments = await _db.GetData<dynamic>(
                "sp_bbps_get_payment_by_txn_ref",
                new { p_txn_ref_id = txnRefId });

            var payment = payments.FirstOrDefault();
            if (payment == null) return;

            // Check if HOLD exists in ledger
            var holdRows = await _db.GetData<dynamic>(
                "sp_wallet_ledger_check",
                new { p_txn_id = txnRefId, p_type = "HOLD" });

            // Check if RELEASE already done (idempotency)
            var releaseRows = await _db.GetData<dynamic>(
                "sp_wallet_ledger_check",
                new { p_txn_id = txnRefId, p_type = "RELEASE" });

            // Only release if HOLD exists and RELEASE not yet done
            if (holdRows.Any() && !releaseRows.Any())
            {
                await ReleaseAsync(
                    (long)payment.user_id,
                    (decimal)payment.amount,
                    "BBPS",
                    txnRefId,
                    txnRefId,
                    "BBPS Status FAILED - Refund");
            }
        }

        // ===========================
        // BACKWARD COMPAT
        // ===========================
        public Task ReverseHold(long userId, decimal amount, string referenceId, string narration)
            => ReleaseAsync(userId, amount, "LEGACY", referenceId, referenceId, narration);

        public Task FinalizeDebit(long userId, decimal amount, string referenceId, string narration)
            => FinalizeAsync(userId, amount, "LEGACY", referenceId, referenceId, narration);
    }
}

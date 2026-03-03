using Payments_core.Models;
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
        // PAYIN COMMISSION (CRITICAL - MUST BE CALLED ONLY ONCE AFTER SUCCESS)
        // ===========================
        public Task<int> WalletLoadCommissionPercent(WalletLoadInit req)
            => _db.ExecuteStoredAsync("sp_Create_Wallet_Load_Commission", new
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
        // PAYIN STATUS UPDATE (CRITICAL - MUST BE CALLED ONLY ONCE)
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
        // HOLD LOGIC (CRITICAL)
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
        // FINALIZE / DEBIT FROM HOLD (CRITICAL)
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
        // RELEASE / REVERSE HOLD (CRITICAL)
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
        // PAYOUT
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
        // PAYOUT FINALIZE (CRITICAL - MUST BE CALLED ONLY ONCE AFTER HOLD)
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
        // Check Daily Payout Limit (CRITICAL - MUST BE CALLED BEFORE PAYOUT INIT AND PAYOUT FINALIZE)
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
                p_IFSCCode = req.IFSCCode
            });

        // ===========================
        // VERIFY BENEFICIARY (CRITICAL - MUST BE CALLED ONLY ONCE PER BENEFICIARY)
        // ===========================
        public Task<int> VerifyBeneficiary(int id)
            => _db.ExecuteStoredAsync("sp_Verify_Beneficiary", new
            {
                p_BeneficiaryId = id
            });

        // ===========================
        // BENEFICIARY LIST
        // ===========================
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
        // Wallet TRANSFER (CRITICAL - MUST BE CALLED ONLY ONCE)
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

        // =========================================
        // BACKWARD COMPATIBILITY
        // =========================================

        //public Task<string> HoldAmount(long userId, decimal amount, string narration)
        //    => HoldAsync(userId, amount, "LEGACY", Guid.NewGuid().ToString("N"), narration);

        //public Task DebitFromHold(long userId, decimal amount, string refId, string narration)
        //    => FinalizeAsync(userId, amount, "LEGACY", refId, refId, narration);

        //public Task ReleaseHold(long userId, decimal amount, string refId, string narration)
        //    => ReleaseAsync(userId, amount, "LEGACY", refId, refId, narration);

        //public Task FinalizeDebit(long userId, decimal amount, string referenceId, string narration)
        //    => FinalizeAsync(userId, amount, "LEGACY", referenceId, referenceId, narration);

        public Task ReverseHold(long userId, decimal amount, string referenceId, string narration)
            => ReleaseAsync(userId, amount, "LEGACY", referenceId, referenceId, narration);

        public Task FinalizeIfPending(string txnRefId)
            => Task.CompletedTask;

        public Task RefundIfPending(string txnRefId)
            => Task.CompletedTask;
    }
}
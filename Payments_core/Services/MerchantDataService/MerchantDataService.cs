using Payments_core.Models;
using Payments_core.Services.DataLayer;
using Payments_core.Services.SuperDistributorService;

namespace Payments_core.Services.MerchantDataService
{
    public class MerchantDataService : IMerchantDataService
    {
        private readonly IDapperContext _dbContext;

        public MerchantDataService(IDapperContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<MerchantListItemDto>> GetAllMerchantsAsync()
        {
            return await _dbContext.GetData<MerchantListItemDto>("sp_merchants_get_all", null);
        }
        public async Task<int> UpdateMerchantApprovalAsync(MerchantApprovalRequest req)
        {
            var param = new
            {
                p_merchant_id = req.MerchantId,
                p_action = req.Action,
                p_remarks = req.Remarks
            };

            return await _dbContext.ExecuteStoredAsync("sp_merchant_approval_update", param);
        }

        public async Task<int> UpdateMerchantKycStatusAsync(MerchantKycUpdateRequest req)
        {
            var param = new
            {
                p_merchant_id = req.MerchantId,
                p_status = req.KycStatus
            };

            return await _dbContext.ExecuteStoredAsync("sp_merchant_kyc_update", param);
        }

        public async Task<int> WalletLoad(WalletLoadInit req)
        {
            var param = new
            {
                p_UserId = req.UserId,
                p_Amount = req.Amount,
                p_ProviderId = req.ProviderId,
                p_ProductTypeId = req.ProductTypeId,
                p_PaymentModeId = req.PaymentModeId,
                p_SettlementType = req.SettlementType,
                p_TransactionId = req.TransactionId
            };

            return await _dbContext.ExecuteStoredAsync("SP_Create_WalletLoadInit", param);
        }

        public async Task<int> WalletLoadCommissionPercent(WalletLoadInit req)
        {
            // To Be done the p_WalletAmount
            var param = new
            {
                p_UserId = req.UserId,                
                p_TransactionId = req.TransactionId,
                p_Amount = req.Amount,
                p_WalletAmount = req.Amount, 
                p_ProviderId = req.ProviderId,
                p_ProductTypeId = req.ProductTypeId,
                p_PaymentModeId = req.PaymentModeId
            };

            return await _dbContext.ExecuteStoredAsync("sp_Create_Wallet_Load_Commission", param);
        }

        public async Task<int> CreateBeneficiary(Beneficiary req)
        {
            var param = new
            {
                p_UserId = req.UserId,
                p_BeneficiaryName = req.BeneficiaryName,
                p_AccountNumber = req.AccountNumber,
                p_IFSCCode = req.IFSCCode
            };

            return await _dbContext.ExecuteStoredAsync("sp_Create_Beneficiary", param);
        }

        public async Task<int> VerifyBeneficiary(int Id)
        {
            var param = new
            {
                p_BeneficiaryId = Id
            };

            return await _dbContext.ExecuteStoredAsync("sp_Verify_Beneficiary", param);
        }

        public async Task<IEnumerable<BeneficiaryDto>> GetBeneficiaries(int UserId)
        {
            var param = new
            {
                p_UserId = UserId
            };

            return await _dbContext.GetData<BeneficiaryDto>("sp_Get_Beneficiaries_By_User", param);
        }

        public async Task<int> PayoutInitAsync(PayoutRequest req)
        {
            var param = new
            {
                p_BeneficiaryId = req.BeneficiaryId,
                p_UserId = req.UserId,
                p_Amount = req.Amount,
                p_FeeAmount = req.FeeAmount,
                p_Mode = req.Mode,
                p_TransactionId = req.TransactionId,
                p_TPin = req.TPin
            };

            return await _dbContext.ExecuteStoredAsync("sp_Payout_Init", param);
        }

        public async Task<int> PayoutAsync(PayoutRequest req)
        {
            var param = new
            {
                p_BeneficiaryId = req.BeneficiaryId,
                p_UserId = req.UserId,
                p_Amount = req.Amount,
                p_FeeAmount = req.FeeAmount,
                p_Status = req.Status,
                p_Reason = req.Reason,
                p_TransactionId = req.TransactionId
            };

            return await _dbContext.ExecuteStoredAsync("sp_Create_Payout", param);
        }

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

            return await _dbContext.ExecuteStoredAsync("sp_Wallet_Transfer", param);
        }

    }
}

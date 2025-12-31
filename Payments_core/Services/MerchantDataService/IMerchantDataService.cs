using Microsoft.AspNetCore.Mvc;
using Payments_core.Models;

namespace Payments_core.Services.MerchantDataService
{
    public interface IMerchantDataService
    {
        Task<IEnumerable<MerchantListItemDto>> GetAllMerchantsAsync();
        Task<int> UpdateMerchantApprovalAsync(MerchantApprovalRequest req);
        Task<int> UpdateMerchantKycStatusAsync(MerchantKycUpdateRequest req);
        Task<int> WalletLoad(WalletLoadInit req);
        Task<int> WalletLoadCommissionPercent(WalletLoadInit req);
        Task<int> CreateBeneficiary(Beneficiary req);
        Task<int> VerifyBeneficiary(int Id);
        Task<IEnumerable<BeneficiaryDto>> GetBeneficiaries(int UserId);
        Task<int> PayoutInitAsync(PayoutRequest req);
        Task<int> PayoutAsync(PayoutRequest req);
        Task<int> WalletTransfer(WalletTransferInit req);
        Task<IEnumerable<LedgerReport>> GetLedgerReport(DateTime FromDate, DateTime ToDate, int TransactionType, int UserId);
    }
}

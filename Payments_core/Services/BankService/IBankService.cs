using Payments_core.Models;

namespace Payments_core.Services.BankService
{
    public interface IBankService
    {
        Task<IEnumerable<BankDto>> GetAllBanksAsync();
        Task<IEnumerable<BankDto>> GetBanksByTypeAsync(string bankType);
        Task<IEnumerable<BankDto>> GetPayoutBanksAsync();
        Task<IEnumerable<BankDto>> SearchBanksAsync(string searchTerm);
        Task<BankDto?> GetBankByIFSCPrefixAsync(string ifscPrefix);
    }
}

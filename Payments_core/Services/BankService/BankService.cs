using Dapper;
using Payments_core.Models;
using Payments_core.Services.DataLayer;

namespace Payments_core.Services.BankService
{
    public class BankService : IBankService
    {
        private readonly IDapperContext _db;

        public BankService(IDapperContext db)
        {
            _db = db;
        }

        // ============================================================
        // GET ALL ACTIVE BANKS
        // ============================================================
        public async Task<IEnumerable<BankDto>> GetAllBanksAsync()
        {
            return await _db.GetData<BankDto>("sp_Get_All_Banks", null);
        }

        // ============================================================
        // GET BANKS BY TYPE (PUBLIC / PRIVATE / SMALL_FINANCE etc.)
        // ============================================================
        public async Task<IEnumerable<BankDto>> GetBanksByTypeAsync(string bankType)
        {
            var param = new DynamicParameters();
            param.Add("p_BankType", bankType);
            return await _db.GetData<BankDto>("sp_Get_Banks_By_Type", param);
        }

        // ============================================================
        // GET PAYOUT BANKS (PUBLIC + PRIVATE + SMALL_FINANCE + PAYMENTS)
        // ============================================================
        public async Task<IEnumerable<BankDto>> GetPayoutBanksAsync()
        {
            return await _db.GetData<BankDto>("sp_Get_Payout_Banks", null);
        }

        // ============================================================
        // SEARCH BANKS BY NAME / SHORT NAME (typeahead)
        // ============================================================
        public async Task<IEnumerable<BankDto>> SearchBanksAsync(string searchTerm)
        {
            var param = new DynamicParameters();
            param.Add("p_SearchTerm", searchTerm);
            return await _db.GetData<BankDto>("sp_Search_Banks", param);
        }

        // ============================================================
        // AUTO-DETECT BANK FROM IFSC CODE (first 4 chars)
        // ============================================================
        public async Task<BankDto?> GetBankByIFSCPrefixAsync(string ifscPrefix)
        {
            var prefix = ifscPrefix.Length >= 4
                ? ifscPrefix.Substring(0, 4).ToUpper()
                : ifscPrefix.ToUpper();

            var param = new DynamicParameters();
            param.Add("p_IFSCPrefix", prefix);

            var result = await _db.GetData<BankDto>("sp_Get_Bank_By_IFSC_Prefix", param);
            return result.FirstOrDefault();
        }
    }
}

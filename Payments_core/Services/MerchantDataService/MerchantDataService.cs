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


    }
}

using Dapper;
using Payments_core.Models;
using Payments_core.Services.DataLayer;
using System.Diagnostics.Metrics;

namespace Payments_core.Services.MasterDataService
{
    public class MasterDataService : IMasterDataService
    {
        IDapperContext dbContext;
        public MasterDataService(IDapperContext _dbContext)
        {
            dbContext = _dbContext;
        }

        public async Task<IEnumerable<Roles>> GetAllRoles()
        {
            return await dbContext.GetData<Roles>("sp_roles_get_all", null);
        }

        public async Task<IEnumerable<Provider>>GetProvidersAsync()
        {
            return await dbContext.GetData<Provider>("sp_master_get_providers",null);
        }

        public async Task<IEnumerable<MdrPricing>> GetMdrPricingAsync()
        {
            return await dbContext.GetData<MdrPricing>("sp_master_get_mdr_pricing", null);
        }
        public async Task<IEnumerable<Biller>> GetBillerAsync(string Category)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("p_category", Category);
            return await dbContext.GetData<Biller>("sp_master_get_billers", parameters);
        }

        public async Task<IEnumerable<BusineessRoles>> RolebasedBusineessName(int RoleId)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("p_role_id", RoleId);
            return await dbContext.GetData<BusineessRoles>("sp_GetBusinessNamesByRole", parameters);
        }
    }
}

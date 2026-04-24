using Dapper;
using Payments_core.Models.BBPS;
using Payments_core.Services.DataLayer;

namespace Payments_core.Services.BBPSService.Repository
{
    public class BbpsCatalogRepository : IBbpsCatalogRepository
    {
        private readonly IDapperContext _db;

        public BbpsCatalogRepository(IDapperContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<BbpsBillerCatalogDto>> GetAll(
            string? category,
            string? environment,
            string? search,
            bool? isActive)
        {
            return await _db.GetData<BbpsBillerCatalogDto>(
                "sp_bbps_catalog_get_all",
                new
                {
                    p_category    = string.IsNullOrWhiteSpace(category) ? null : category,
                    p_environment = string.IsNullOrWhiteSpace(environment) ? null : environment,
                    p_search      = string.IsNullOrWhiteSpace(search) ? null : search,
                    p_is_active   = isActive.HasValue ? (object)Convert.ToInt32(isActive.Value) : DBNull.Value
                });
        }

        public async Task<BbpsBillerCatalogDto?> GetById(string billerId)
        {
            var rows = await _db.GetData<BbpsBillerCatalogDto>(
                "sp_bbps_catalog_get_by_id",
                new { p_biller_id = billerId });
            return rows.FirstOrDefault();
        }

        public async Task<bool> Exists(string billerId)
        {
            var row = await GetById(billerId);
            return row != null;
        }

        public Task Add(BbpsBillerCatalog catalog)
            => _db.ExecuteStoredAsync("sp_bbps_catalog_add", new
            {
                p_biller_id       = catalog.BillerId,
                p_biller_name     = catalog.BillerName,
                p_service_category = catalog.ServiceCategory,
                p_environment     = catalog.Environment,
                p_is_active       = catalog.IsActive ? 1 : 0,
                p_mdm_supported   = catalog.MdmSupported ? 1 : 0
            });

        public Task Update(string billerId, UpdateBillerCatalogRequest req)
            => _db.ExecuteStoredAsync("sp_bbps_catalog_update", new
            {
                p_biller_id       = billerId,
                p_biller_name     = req.BillerName,
                p_service_category = req.ServiceCategory,
                p_environment     = req.Environment,
                p_mdm_supported   = req.MdmSupported.HasValue
                                        ? (object)Convert.ToInt32(req.MdmSupported.Value)
                                        : DBNull.Value
            });

        public Task SetActive(string billerId, bool isActive)
            => _db.ExecuteStoredAsync("sp_bbps_catalog_set_active", new
            {
                p_biller_id = billerId,
                p_is_active = isActive ? 1 : 0
            });

        public async Task BulkSetActive(List<string> billerIds, bool isActive)
        {
            // Call individually — stored proc handles one at a time
            // For large batches this is fine (typically <500 billers)
            foreach (var id in billerIds)
                await SetActive(id, isActive);
        }

        public Task Delete(string billerId)
            => _db.ExecuteStoredAsync("sp_bbps_catalog_delete",
               new { p_biller_id = billerId });

        public async Task<IEnumerable<string>> GetDistinctCategories()
        {
            var rows = await _db.GetData<string>("sp_bbps_catalog_distinct_categories");
            return rows;
        }

        public async Task<CatalogStatsDto> GetStats()
        {
            var rows = await _db.GetData<CatalogStatsDto>("sp_bbps_catalog_stats");
            return rows.FirstOrDefault() ?? new CatalogStatsDto();
        }
    }
}

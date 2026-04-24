using Payments_core.Models.BBPS;

namespace Payments_core.Services.BBPSService.Repository
{
    // ??????????????????????????????????????????????????????
    // DTO used by Update() — defined here so BOTH
    // BbpsCatalogRepository.cs and IBbpsCatalogRepository.cs
    // can see it without needing a using from Controllers/
    // ??????????????????????????????????????????????????????
    public class UpdateBillerCatalogRequest
    {
        public string? BillerName { get; set; }
        public string? ServiceCategory { get; set; }
        public string? Environment { get; set; }
        public bool? MdmSupported { get; set; }
    }

    public interface IBbpsCatalogRepository
    {
        Task<IEnumerable<BbpsBillerCatalogDto>> GetAll(
            string? category,
            string? environment,
            string? search,
            bool? isActive);

        Task<BbpsBillerCatalogDto?> GetById(string billerId);

        Task<bool> Exists(string billerId);

        Task Add(BbpsBillerCatalog catalog);

        Task Update(string billerId, UpdateBillerCatalogRequest req);

        Task SetActive(string billerId, bool isActive);

        Task BulkSetActive(List<string> billerIds, bool isActive);

        Task Delete(string billerId);

        Task<IEnumerable<string>> GetDistinctCategories();

        Task<CatalogStatsDto> GetStats();
    }
}
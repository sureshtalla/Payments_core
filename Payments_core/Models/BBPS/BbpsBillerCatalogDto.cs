namespace Payments_core.Models.BBPS
{
    /// <summary>
    /// Read DTO — returned to the admin frontend from the catalog table.
    /// </summary>
    public class BbpsBillerCatalogDto
    {
        public string BillerId { get; set; } = "";
        public string BillerName { get; set; } = "";
        public string ServiceCategory { get; set; } = "";
        public string Environment { get; set; } = "PROD";
        public bool IsActive { get; set; }
        public bool MdmSupported { get; set; }
        public DateTime CreatedOn { get; set; }

        // Joined from bbps_billers — shows last MDM sync result
        public string? LastSyncedName { get; set; }
        public string? FetchRequirement { get; set; }
        public bool? SupportsAdhoc { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
    }

    /// <summary>
    /// Summary stats for the admin dashboard cards.
    /// </summary>
    public class CatalogStatsDto
    {
        public int TotalBillers { get; set; }
        public int ActiveBillers { get; set; }
        public int InactiveBillers { get; set; }
        public int MdmSupportedBillers { get; set; }
        public int SyncedBillers { get; set; }   // count in bbps_billers
        public int ProdBillers { get; set; }
        public int StgBillers { get; set; }
    }
}

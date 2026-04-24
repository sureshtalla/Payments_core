using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments_core.Models.BBPS;
using Payments_core.Services.BBPSService;
using Payments_core.Services.BBPSService.Repository; // ← UpdateBillerCatalogRequest lives here now

namespace Payments_core.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/admin/bbps-catalog")]
    public class BbpsCatalogController : ControllerBase
    {
        private readonly IBbpsCatalogRepository _catalog;
        private readonly IBbpsService _bbps;
        private readonly ILogger<BbpsCatalogController> _logger;

        public BbpsCatalogController(
            IBbpsCatalogRepository catalog,
            IBbpsService bbps,
            ILogger<BbpsCatalogController> logger)
        {
            _catalog = catalog;
            _bbps = bbps;
            _logger = logger;
        }

        // GET /api/admin/bbps-catalog
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? category,
            [FromQuery] string? environment,
            [FromQuery] string? search,
            [FromQuery] bool? isActive)
        {
            try
            {
                var result = await _catalog.GetAll(category, environment, search, isActive);
                return Ok(new { success = true, data = result, count = result.Count() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAll catalog failed");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET /api/admin/bbps-catalog/categories
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var cats = await _catalog.GetDistinctCategories();
            return Ok(new { success = true, data = cats });
        }

        // GET /api/admin/bbps-catalog/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _catalog.GetStats();
            return Ok(new { success = true, data = stats });
        }

        // POST /api/admin/bbps-catalog
        [HttpPost]
        public async Task<IActionResult> AddBiller([FromBody] AddBillerCatalogRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.BillerId) || req.BillerId.Length != 14)
                return BadRequest(new { success = false, message = "BillerId must be exactly 14 characters." });

            if (string.IsNullOrWhiteSpace(req.BillerName))
                return BadRequest(new { success = false, message = "BillerName is required." });

            if (string.IsNullOrWhiteSpace(req.ServiceCategory))
                return BadRequest(new { success = false, message = "ServiceCategory is required." });

            try
            {
                var exists = await _catalog.Exists(req.BillerId);
                if (exists)
                    return Conflict(new { success = false, message = $"BillerId '{req.BillerId}' already exists in catalog." });

                await _catalog.Add(new BbpsBillerCatalog
                {
                    BillerId = req.BillerId.Trim().ToUpper(),
                    BillerName = req.BillerName.Trim(),
                    ServiceCategory = req.ServiceCategory.Trim(),
                    Environment = req.Environment?.Trim().ToUpper() ?? "PROD",
                    IsActive = true,
                    MdmSupported = req.MdmSupported
                });

                _logger.LogInformation("Biller {BillerId} added to catalog", req.BillerId);
                return Ok(new { success = true, message = $"Biller '{req.BillerName}' added successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddBiller failed for {BillerId}", req.BillerId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST /api/admin/bbps-catalog/bulk-import
        [HttpPost("bulk-import")]
        public async Task<IActionResult> BulkImport([FromBody] List<BulkImportRow> rows)
        {
            if (rows == null || !rows.Any())
                return BadRequest(new { success = false, message = "No rows provided." });

            if (rows.Count > 500)
                return BadRequest(new { success = false, message = "Maximum 500 rows per import." });

            int added = 0, skipped = 0, failed = 0;
            var errors = new List<string>();

            foreach (var row in rows)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(row.BillerId) || row.BillerId.Length != 14)
                    {
                        errors.Add($"Skipped '{row.BillerId}': invalid BillerId length");
                        skipped++;
                        continue;
                    }

                    var exists = await _catalog.Exists(row.BillerId.Trim().ToUpper());
                    if (exists) { skipped++; continue; }

                    await _catalog.Add(new BbpsBillerCatalog
                    {
                        BillerId = row.BillerId.Trim().ToUpper(),
                        BillerName = (row.BillerName ?? "").Trim(),
                        ServiceCategory = (row.ServiceCategory ?? "").Trim(),
                        Environment = "PROD",
                        IsActive = true,
                        MdmSupported = true
                    });
                    added++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed '{row.BillerId}': {ex.Message}");
                    failed++;
                }
            }

            return Ok(new
            {
                success = true,
                added,
                skipped,
                failed,
                errors,
                message = $"Import complete: {added} added, {skipped} skipped, {failed} failed."
            });
        }

        // PUT /api/admin/bbps-catalog/{billerId}
        [HttpPut("{billerId}")]
        public async Task<IActionResult> Update(string billerId, [FromBody] UpdateBillerCatalogRequest req)
        {
            try
            {
                var exists = await _catalog.Exists(billerId);
                if (!exists)
                    return NotFound(new { success = false, message = $"BillerId '{billerId}' not found." });

                await _catalog.Update(billerId, req);
                _logger.LogInformation("Biller {BillerId} updated", billerId);
                return Ok(new { success = true, message = "Biller updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // PATCH /api/admin/bbps-catalog/{billerId}/toggle
        [HttpPatch("{billerId}/toggle")]
        public async Task<IActionResult> Toggle(string billerId)
        {
            try
            {
                var current = await _catalog.GetById(billerId);
                if (current == null)
                    return NotFound(new { success = false, message = "Biller not found." });

                bool newStatus = !current.IsActive;
                await _catalog.SetActive(billerId, newStatus);

                string label = newStatus ? "activated" : "deactivated";
                _logger.LogInformation("Biller {BillerId} {Label}", billerId, label);
                return Ok(new { success = true, isActive = newStatus, message = $"Biller {label}." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // PATCH /api/admin/bbps-catalog/bulk-toggle
        [HttpPatch("bulk-toggle")]
        public async Task<IActionResult> BulkToggle([FromBody] BulkToggleRequest req)
        {
            if (req.BillerIds == null || !req.BillerIds.Any())
                return BadRequest(new { success = false, message = "No billerIds provided." });

            try
            {
                await _catalog.BulkSetActive(req.BillerIds, req.IsActive);
                string label = req.IsActive ? "activated" : "deactivated";
                _logger.LogInformation("{Count} billers {Label}", req.BillerIds.Count, label);
                return Ok(new { success = true, message = $"{req.BillerIds.Count} billers {label}." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // DELETE /api/admin/bbps-catalog/{billerId}
        [HttpDelete("{billerId}")]
        public async Task<IActionResult> Delete(string billerId)
        {
            try
            {
                await _catalog.Delete(billerId);
                _logger.LogInformation("Biller {BillerId} deleted from catalog", billerId);
                return Ok(new { success = true, message = "Biller removed from catalog." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST /api/admin/bbps-catalog/sync
        [HttpPost("sync")]
        public async Task<IActionResult> TriggerSync()
        {
            try
            {
                await _bbps.SyncBillers();
                return Ok(new { success = true, message = "MDM sync completed. bbps_billers table updated." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MDM sync triggered from admin failed");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    // ──────── Request DTOs (Controller-level only) ────────
    public class AddBillerCatalogRequest
    {
        public string BillerId { get; set; } = "";
        public string BillerName { get; set; } = "";
        public string ServiceCategory { get; set; } = "";
        public string Environment { get; set; } = "PROD";
        public bool MdmSupported { get; set; } = true;
    }

    public class BulkImportRow
    {
        public string BillerId { get; set; } = "";
        public string BillerName { get; set; } = "";
        public string ServiceCategory { get; set; } = "";
    }

    public class BulkToggleRequest
    {
        public List<string> BillerIds { get; set; } = new();
        public bool IsActive { get; set; }
    }
}
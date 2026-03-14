using Payments_core.Services.DataLayer;

namespace Payments_core.Services.Monitoring
{
    public class MetricsService
    {
        private readonly IDapperContext _db;

        public MetricsService(IDapperContext db)
        {
            _db = db;
        }

        public async Task UpdateMetric(
            string name,
            decimal value)
        {
            await _db.ExecuteAsync(
            @"INSERT INTO system_metrics
            (metric_name,metric_value)
            VALUES(@name,@value)
            ON DUPLICATE KEY UPDATE
            metric_value=@value,
            updated_at=NOW()",
            new { name, value });
        }
    }
}
using System.Text.Json;
using Payments_core.Services.DataLayer;

namespace Payments_core.Services.FailureQueue
{
    public class FailureService
    {
        private readonly IDapperContext _db;

        public FailureService(IDapperContext db)
        {
            _db = db;
        }

        public async Task LogFailure(
            string requestId,
            string serviceType,
            object payload,
            string reason)
        {
            await _db.ExecuteAsync(
            @"INSERT INTO failed_transactions
            (request_id,service_type,payload,reason)
            VALUES(@req,@svc,@pay,@reason)",
            new
            {
                req = requestId,
                svc = serviceType,
                pay = JsonSerializer.Serialize(payload),
                reason
            });
        }
    }
}
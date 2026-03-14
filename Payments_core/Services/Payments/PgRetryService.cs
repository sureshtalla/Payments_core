using Payments_core.Services.DataLayer;

namespace Payments_core.Services.Payments
{
    public class PgRetryService
    {
        private readonly IDapperContext _db;

        public PgRetryService(IDapperContext db)
        {
            _db = db;
        }

        public async Task<dynamic?> GetRetryRule(int providerId)
        {
            var rows = await _db.GetData<dynamic>(
            @"SELECT *
            FROM pg_retry_rules
            WHERE provider_id=@id
            AND status=1",
            new { id = providerId });

            return rows.FirstOrDefault();
        }

        public async Task LogAttempt(
          string requestId,
          int providerId,
          int attempt,
          string status,
          string error)
        {
            await _db.ExecuteStoredAsync(
                "sp_pg_insert_attempt",
                new
                {
                    p_request_id = requestId,
                    p_provider_id = providerId,
                    p_attempt_no = attempt,
                    p_status = status,
                    p_error = error
                });
        }
    }
}
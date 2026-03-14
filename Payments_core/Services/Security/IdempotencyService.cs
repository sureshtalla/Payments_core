using Payments_core.Services.DataLayer;

namespace Payments_core.Services.Security
{
    public class IdempotencyService
    {
        private readonly IDapperContext _db;

        public IdempotencyService(IDapperContext db)
        {
            _db = db;
        }

        public async Task<bool> ValidateRequest(
         string requestKey,
         string endpoint)
        {
            try
            {
                await _db.ExecuteAsync(
                @"INSERT INTO api_idempotency
          (request_key,endpoint)
          VALUES(@key,@ep)",
                new { key = requestKey, ep = endpoint });

                return true;
            }
            catch (Exception ex)
            {
                // duplicate key means request already processed
                if (ex.Message.Contains("Duplicate"))
                    return false;

                throw; // other DB errors should not be hidden
            }
        }
    }
}
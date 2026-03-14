using Payments_core.Services.DataLayer;

namespace Payments_core.Services.Security
{
    public class FraudService
    {
        private readonly IDapperContext _db;

        public FraudService(IDapperContext db)
        {
            _db = db;
        }

        public async Task<bool> CheckFraud(
            long userId,
            decimal amount)
        {
            var rows = await _db.GetData<dynamic>(
                "SELECT * FROM fraud_rules WHERE status=1");

            var rule = rows.FirstOrDefault();

            if (rule == null)
                return true;

            if (amount > rule.max_amount)
                return false;

            return true;
        }
    }
}
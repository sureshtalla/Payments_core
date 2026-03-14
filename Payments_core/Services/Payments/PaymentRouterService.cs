using Payments_core.Services.DataLayer;

namespace Payments_core.Services.Payments
{
    public class PaymentRouterService
    {
        private readonly IDapperContext _db;
        private readonly IEnumerable<IPaymentGateway> _gateways;

        public PaymentRouterService(
            IDapperContext db,
            IEnumerable<IPaymentGateway> gateways)
        {
            _db = db;
            _gateways = gateways;
        }

        public async Task<List<(IPaymentGateway gateway, dynamic provider)>>
            GetGateways(string category)
        {
            var routes = await _db.GetData<dynamic>(
                "sp_pg_get_gateways",
                new { p_category = category });

            var result = new List<(IPaymentGateway, dynamic)>();

            foreach (var r in routes)
            {
                //var gateway =
                //    _gateways.First(g => g.Code == r.code);

                var gateway =
                     _gateways.FirstOrDefault(g => g.Code == r.code);

                if (gateway == null)
                    throw new Exception($"Gateway {r.code} not registered");

                result.Add((gateway, r));
            }

            return result;
        }
    }
}
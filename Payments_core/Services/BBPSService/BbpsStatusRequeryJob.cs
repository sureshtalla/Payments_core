using Payments_core.Services.BBPSService.Repository;

namespace Payments_core.Services.BBPSService
{
    public class BbpsStatusRequeryJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public BbpsStatusRequeryJob(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IBbpsRepository>();
                var svc = scope.ServiceProvider.GetRequiredService<IBbpsService>();

                foreach (var txn in await repo.GetPendingTxns())
                {
                    await svc.CheckStatus(txn.TxnRefId, txn.BillRequestId);
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
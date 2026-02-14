using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

                var repo = scope.ServiceProvider
                                .GetRequiredService<IBbpsRepository>();

                var svc = scope.ServiceProvider
                               .GetRequiredService<IBbpsService>();

                var pendingTxns = await repo.GetPendingTxns();

                foreach (var txn in pendingTxns)
                {
                    // 🔥 Get requestId from DB
                    var requestId = await repo
                        .GetRequestIdByTxnRef(txn.TxnRefId);

                    if (string.IsNullOrEmpty(requestId))
                    {
                        Console.WriteLine(
                            $"[REQUERY] RequestId not found for {txn.TxnRefId}"
                        );
                        continue;
                    }

                    await svc.CheckStatus(
                        requestId,
                        txn.TxnRefId,
                        txn.BillRequestId
                    );
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(5),
                    stoppingToken
                );
            }
        }
    }
}
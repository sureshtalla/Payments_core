using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payments_core.Services.Reconciliation;

namespace Payments_core.Services.Jobs
{
    public class ReconciliationJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReconciliationJob> _logger;

        public ReconciliationJob(
            IServiceScopeFactory scopeFactory,
            ILogger<ReconciliationJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "FINX Reconciliation Job Started");

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope =
                    _scopeFactory.CreateScope();

                var service =
                    scope.ServiceProvider
                    .GetRequiredService<ReconciliationService>();

                try
                {
                    _logger.LogInformation(
                        "Running Payin reconciliation");

                    await service.RunPayinReconciliation();

                    _logger.LogInformation(
                        "Running Bank wallet reconciliation");

                    await service.RunBankWalletReconciliation();

                    _logger.LogInformation(
                        "Running PG timeout recovery");

                    await service.RunPgTimeoutRecovery();
                    _logger.LogInformation(
                           "Running webhook recovery");

                    await service.RunWebhookRecovery();

                    _logger.LogInformation(
                          "Running wallet vs bank reconciliation");

                    await service.RunWalletVsBankReconciliation();
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Reconciliation job failed");
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(5),
                    stoppingToken);
            }
        }
    }
}
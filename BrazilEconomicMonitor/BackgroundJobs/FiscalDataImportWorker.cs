using BrazilEconomicMonitor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BrazilEconomicMonitor.BackgroundJobs
{
    public class FiscalDataImportWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FiscalDataImportWorker> _logger;

        public FiscalDataImportWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<FiscalDataImportWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
        {
            using PeriodicTimer timer =
                new PeriodicTimer(TimeSpan.FromMinutes(1));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var scope =
                        _scopeFactory.CreateAsyncScope();

                    var importService =
                        scope.ServiceProvider
                            .GetRequiredService<TreasuryImportService>();

                    await importService.AddLatestTreasuryData(
                        stoppingToken);

                    _logger.LogInformation(
                                        "Fiscal import of data completed at {Time}",
                                        DateTimeOffset.Now);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,     
                        "Fiscal import failed.");

                }
            }
        }
    }
}

using BrazilEconomicMonitor.Services.InternalServices;
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
                new PeriodicTimer(TimeSpan.FromMinutes(5));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {

                _logger.LogInformation(
                                        "Import and transformation cycle started at {Time}",
                                        DateTimeOffset.Now);
                try
                {
                    await using var scope =
                        _scopeFactory.CreateAsyncScope();

                    var treasuryImportService =
                        scope.ServiceProvider
                            .GetRequiredService<TreasuryImportService>();

                    var centralBankImportService =
                        scope.ServiceProvider
                            .GetRequiredService<CentralBankImportService>();

                    var ttmTransformationService =
                        scope.ServiceProvider
                            .GetRequiredService<TtmTransformationService>();

                    var primaryBalanceOverGdpTransformationService =
                        scope.ServiceProvider
                            .GetRequiredService<PrimaryBalanceOverGdpTransformationService>();

                    var forecastError12MonthsTransformationService =
                        scope.ServiceProvider
                            .GetRequiredService<ForecastError12MonthsTransformationService>();

                    var yoyTransformationService =
                        scope.ServiceProvider
                            .GetRequiredService<YoyTransformationService>();


                    await treasuryImportService.UpdateTreasuryDataAsync(
                        stoppingToken);

                    await centralBankImportService.UpdateCentralBankDataAsync(
                        stoppingToken);

                    await ttmTransformationService.UpdateTtmAsync(
                        stoppingToken);

                    await primaryBalanceOverGdpTransformationService.UpdatePrimaryBalanceOverGdpAsync(
                        stoppingToken);
                    
                    await forecastError12MonthsTransformationService.UpdateForecastError12MonthsAsync(
                        stoppingToken);

                    await yoyTransformationService.UpdateYoyAsync(
                        stoppingToken);

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
                        "Program stopped.");
                }
            }
        }
    }
}

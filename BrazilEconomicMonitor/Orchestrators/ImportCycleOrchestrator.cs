using BrazilEconomicMonitor.CustomExceptions;
using BrazilEconomicMonitor.Services.InternalServices;
using BrazilEconomicMonitor.Services.TransformationServices;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace BrazilEconomicMonitor.Orchestrators
{
    public class ImportCycleOrchestrator
    {
        private readonly TreasuryImportService _treasuryImportService;
        private readonly CentralBankImportService _centralBankImportService;
        private readonly CbOlindaImportService _cbOlindaImportService;
        private readonly TtmTransformationService _ttmTransformationService;
        private readonly PrimaryBalanceOverGdpTransformationService _primaryBalanceOverGdpTransformationService;
        private readonly ForecastError12MonthsTransformationService _forecastError12MonthsTransformationService;
        private readonly YoyTransformationService _yoyTransformationService;
        private readonly ILogger<ImportCycleOrchestrator> _logger;

        public ImportCycleOrchestrator(
            TreasuryImportService treasuryImportService,
            CentralBankImportService centralBankImportService,
            CbOlindaImportService cbOlindaImportService,
            TtmTransformationService ttmTransformationService,
            PrimaryBalanceOverGdpTransformationService primaryBalanceOverGdpTransformationService,
            ForecastError12MonthsTransformationService forecastError12MonthsTransformationService,
            YoyTransformationService yoyTransformationService,
            ILogger<ImportCycleOrchestrator> logger)
        {
            _treasuryImportService = treasuryImportService;
            _centralBankImportService = centralBankImportService;
            _cbOlindaImportService = cbOlindaImportService;
            _ttmTransformationService = ttmTransformationService;
            _primaryBalanceOverGdpTransformationService =
                primaryBalanceOverGdpTransformationService;
            _forecastError12MonthsTransformationService =
                forecastError12MonthsTransformationService;
            _yoyTransformationService = yoyTransformationService;
            _logger = logger;
        }

        public record ImportCycleResult(
        DateTimeOffset StartedAt,
        DateTimeOffset FinishedAt,
        List<StepResult> Steps);

        public record StepResult(
        string StepName,
        bool Success,
        string? ErrorMessage = null);


        private async Task<StepResult> ExecutemportStepAsync(
        string stepName,
        Func<Task> operation,
        CancellationToken cancellationToken)
        {
            try
            {
                await operation();

                return new StepResult(stepName, true);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{StepName} failed.",
                    stepName);

                return new StepResult(
                stepName,
                false,
                ex.Message);
            }
        }

        public async Task<ImportCycleResult> RunImportCycleAsync(CancellationToken cancellationToken)
        {

            DateTimeOffset StartedAt = DateTimeOffset.Now;

            _logger.LogInformation(
            "Import and transformation cycle started at {StartedAt}",
            StartedAt);

            var steps = new List<StepResult>();

            steps.Add(await ExecutemportStepAsync(
            "Treasury import",
            () => _treasuryImportService.UpdateTreasuryDataAsync(
            cancellationToken),
            cancellationToken));

            steps.Add(await ExecutemportStepAsync(
            "Central Bank import",
            () => _centralBankImportService.UpdateCentralBankDataAsync(
            cancellationToken),
            cancellationToken));

            steps.Add(await ExecutemportStepAsync(
            "Olinda Central Bank import expected Interest Rates",
            () => _cbOlindaImportService.ImportIntrestRatesExpectationsAsync(
            12,
            cancellationToken),
            cancellationToken));

            steps.Add(await ExecutemportStepAsync(
            "Olinda Central Bank import expected Inflation",
            () => _cbOlindaImportService.ImportInflationExpectationsAsync(
            12,
            cancellationToken),
            cancellationToken));

            var failedStepNames = steps
            .Where(x => !x.Success)
            .Select(x => x.StepName)
            .ToList();


            DateTimeOffset FinishedAt = DateTimeOffset.Now;

            TimeSpan duration = FinishedAt - StartedAt;

            _logger.LogInformation(
            "Import cycle completed in {DurationMs} ms. TotalSteps: {TotalSteps}, SuccessfulSteps: {SuccessfulSteps}, FailedSteps: {FailedSteps}, FailedStepNames: {FailedStepNames}",
            duration.TotalMilliseconds,
            steps.Count,
            steps.Count(x => x.Success),
            failedStepNames.Count,
            failedStepNames.ToArray());

            return new ImportCycleResult(StartedAt, FinishedAt, steps);




            await _treasuryImportService.UpdateTreasuryDataAsync(
                cancellationToken);

            await _centralBankImportService.UpdateCentralBankDataAsync(
                cancellationToken);

            int interestRatesObsCount = 12;

            await _cbOlindaImportService
                .ImportIntrestRatesExpectationsAsync(
                interestRatesObsCount,
                cancellationToken);

            int inflationObsCount = 12;

            await _cbOlindaImportService
                .ImportInflationExpectationsAsync(
                inflationObsCount,
                cancellationToken);

            await _ttmTransformationService.UpdateTtmAsync(
                cancellationToken);

            await _primaryBalanceOverGdpTransformationService
                .UpdatePrimaryBalanceOverGdpAsync(
                cancellationToken);

            await _forecastError12MonthsTransformationService
                .UpdateForecastError12MonthsAsync(
                cancellationToken);

            await _yoyTransformationService.UpdateYoyAsync(
                cancellationToken);

            _logger.LogInformation(
                "Import and transformation cycle completed.");
        }
    }
}

using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BrazilEconomicMonitor.Settings;

namespace BrazilEconomicMonitor.Services.InternalServices
{
    public class ForecastError12MonthsTransformationService
    {
        private readonly BrazilEconomicMonitorDbContext _db;

        private readonly ILogger<ForecastError12MonthsTransformationService> _logger;

        private readonly HelperServices _helperServices;

        private readonly int _LookbackMonths;

        private readonly HashSet<string> _ForecastError12Months =
        [
            "10.03.1"
        ];

        public ForecastError12MonthsTransformationService(BrazilEconomicMonitorDbContext db, ILogger<ForecastError12MonthsTransformationService> logger,
            HelperServices helperServices, IOptions<ImportSettings> options)

        {
            _db = db;
            _logger = logger;
            _helperServices = helperServices;
            _LookbackMonths = options.Value.LookbackMonths;
        }

        public async Task CalculateForecastError12MonthsAsync(DateTime startDate, CancellationToken cancellationToken)
        {
            foreach (string code in _ForecastError12Months)
            {
                Series? inputSeries = await _db.Series.SingleOrDefaultAsync(s => s.Code == code, cancellationToken);

                if (inputSeries == null)
                    continue;
                List<Observation> observations =
                    await _db.Observations
                    .Where(o => o.SeriesId == inputSeries.Id)
                    .OrderByDescending(o => o.ObservationDate >= startDate)
                    .ToListAsync(cancellationToken);

                string derivedSeriesCode = code + "_error12Months";
                string derivedSeriesName = inputSeries.Name + " error12Months";

                Series error12MonthsSerie = await _helperServices.FindOrCreateNewDerivedSeries(derivedSeriesCode, derivedSeriesName, cancellationToken);

                for (int i = 0; i < observations.Count; i++)
                {
                    Observation current = observations[i];
                    Observation previousYear = observations[i + 12];

                    decimal error12MonthsValue = current.Value - previousYear.Value;

                    if (current.ObservationDate != previousYear.ObservationDate.AddYears(1))
                    {
                        _logger.LogWarning(
                        "YoY calculation skipped for {Code} at {Date}: previous-year month is missing.",
                        code,
                        current.ObservationDate);
                    }

                    await _helperServices.UpsertDerivedObservationAsync(error12MonthsSerie.Id, current.ObservationDate, error12MonthsValue, cancellationToken);
                }

                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("ForecastError12Months series saved to db successfully! " +
                    "Series transformed: {series}", string.Join(",", _ForecastError12Months));
            }
        }

        public async Task SeedForecastError12MonthsASync(CancellationToken cancellationToken)
        {
            DateTime startDate = new DateTime(2010, 1, 1).AddMonths(-12);

            _logger.LogInformation("Started seeding the database with Ttm transformations." +
                "Series transformed: {series}", string.Join(",", _ForecastError12Months));

            await CalculateForecastError12MonthsAsync(startDate, cancellationToken);
        }

        public async Task UpdateForecastError12MonthsAsync(CancellationToken cancellationToken)
        {
            foreach (string code in _ForecastError12Months)
            {
                Series? inputSeries = await _db.Series.SingleOrDefaultAsync(s => s.Code == code, cancellationToken);

                if (inputSeries == null)
                    continue;

                DateTime latestDate =
                    await _db.Observations
                    .Where(o => o.SeriesId == inputSeries.Id)
                    .OrderByDescending(o => o.ObservationDate)
                    .Select(o => o.ObservationDate)
                    .FirstOrDefaultAsync(cancellationToken);

                DateTime startDate = latestDate.AddMonths(-(_LookbackMonths + 12)); // calculate the ttm for the latest "Lookback" months, IOptions

                await CalculateForecastError12MonthsAsync(startDate, cancellationToken);
            }
        }
    }
}

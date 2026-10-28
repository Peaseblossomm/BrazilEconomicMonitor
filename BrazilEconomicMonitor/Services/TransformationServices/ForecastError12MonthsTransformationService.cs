using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BrazilEconomicMonitor.Settings;
using System.Diagnostics.Eventing.Reader;

namespace BrazilEconomicMonitor.Services.TransformationServices
{
    public class ForecastErrorInflation12MonthsTransformationService
    {
        private readonly BrazilEconomicMonitorDbContext _db;

        private readonly ILogger<ForecastErrorInflation12MonthsTransformationService> _logger;

        private readonly HelperServices _helperServices;

        private readonly int _LookbackMonths;
        public ForecastErrorInflation12MonthsTransformationService(BrazilEconomicMonitorDbContext db, ILogger<ForecastErrorInflation12MonthsTransformationService> logger,
            HelperServices helperServices, IOptions<ImportSettings> options)

        {
            _db = db;
            _logger = logger;
            _helperServices = helperServices;
            _LookbackMonths = options.Value.LookbackMonths;

        }
        public async Task CalculateForecastErrorInflation12MonthsAsync(DateTime earliestDate, CancellationToken cancellationToken)
        {

            string inflationExpectationCode = "ExpectativasMercadoInflacao12Meses";
            string actualInflationMonthlyCode = "13522";

            Series? forecastSeries = await _db.Series.SingleOrDefaultAsync(s => s.Code == inflationExpectationCode, cancellationToken);

            Series? actualSeries = await _db.Series.SingleOrDefaultAsync(s => s.Code == actualInflationMonthlyCode, cancellationToken);

            if (forecastSeries == null)
            {
                throw new InvalidOperationException($"No series containing {inflationExpectationCode} where found");
            }
            else if (actualSeries == null)
            {
                throw new InvalidOperationException($"No series containing {actualInflationMonthlyCode} where found");
            }

            DateTime earliestForecastDate = earliestDate.AddYears(-1);  // The date of question is compared to the forecast exactly 12 months prior

            Dictionary<DateTime, decimal> forecastInflationObservationsMonthly =
                await _db.Observations
                .Where(o => o.SeriesId == forecastSeries.Id)
                .OrderBy(o => o.ObservationDate >= earliestForecastDate && o.ObservationDate.Day == 1)
                .ToDictionaryAsync(a => a.ObservationDate,
                a => a.Value,
                cancellationToken);

            Dictionary<DateTime, decimal> actualInflationObservationsMonthly =
                await _db.Observations
                .Where(o => o.SeriesId == actualSeries.Id)
                .OrderBy(o => o.ObservationDate >= earliestDate)
                .ToDictionaryAsync(a => a.ObservationDate,
                a => a.Value,
                cancellationToken);

            DateTime latestObservationDate = await _db.Observations.Where(o => o.SeriesId == actualSeries.Id)
                .OrderByDescending(o => o.ObservationDate >= earliestDate).Select(o => o.ObservationDate).SingleOrDefaultAsync(cancellationToken);

            string derivedSeriesCode = actualSeries.Code + "_error12Months";
            string derivedSeriesName = actualSeries.Name + " error12Months";

            Series? error12MonthsSerie = await _db.Series.SingleOrDefaultAsync(s => s.Code == derivedSeriesCode, cancellationToken);

            if (error12MonthsSerie == null)
            {
                error12MonthsSerie = await _helperServices.FindOrCreateNewDerivedSeries(derivedSeriesCode, derivedSeriesName, cancellationToken);
            }

            DateTime rollingDate = earliestDate;

            while (rollingDate <= latestObservationDate)
            {

                if (!actualInflationObservationsMonthly.ContainsKey(rollingDate))
                {
                    _logger.LogWarning("No observations from {series} for {date} recorded in db", actualSeries, rollingDate);
                    rollingDate = rollingDate.AddMonths(1);
                    continue;
                }
                if (!forecastInflationObservationsMonthly.ContainsKey(rollingDate))
                {
                    _logger.LogWarning("No observations from {series} for {date} recorded in db", forecastSeries, rollingDate);
                    rollingDate = rollingDate.AddMonths(1);
                    continue;
                }

                decimal error12MonthsValue = actualInflationObservationsMonthly[rollingDate] - forecastInflationObservationsMonthly[rollingDate];

                await _helperServices.UpsertDerivedObservationAsync(error12MonthsSerie.Id, rollingDate, error12MonthsValue, cancellationToken);

                rollingDate = rollingDate.AddMonths(1);
            }
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Series with code {code} from {startdate} to {enddate} saved to db successfully!", derivedSeriesCode, earliestDate, latestObservationDate);
        }

    public async Task SeedForecastError12MonthsAsync(CancellationToken cancellationToken)
    {
        DateTime startDate = new DateTime(2018, 1, 1).AddMonths(-12);

        _logger.LogInformation("Started seeding derived series with code 13522_error12Months.");

        await CalculateForecastErrorInflation12MonthsAsync(startDate, cancellationToken);

        _logger.LogInformation("Seeded ForecastError12Months observations since 1 jan 2018 successfully!");
        }

        public async Task UpdateForecastError12MonthsAsync(CancellationToken cancellationToken)
        {

                Series? Inputseries = await _db.Series.SingleOrDefaultAsync(s => s.Code == "13522", cancellationToken);

            if (Inputseries == null)
            {
                throw new InvalidOperationException("Please seed the input series with Code '13522'");
            }

            DateTime latestDate =
                    await _db.Observations
                    .Where(o => o.SeriesId == Inputseries.Id)
                    .OrderByDescending(o => o.ObservationDate)
                    .Select(o => o.ObservationDate)
                    .FirstOrDefaultAsync(cancellationToken);

                DateTime startDate = latestDate.AddMonths(-(_LookbackMonths + 12)); // calculate the ttm for the latest "Lookback" months, IOptions

                await CalculateForecastErrorInflation12MonthsAsync(startDate, cancellationToken);
            
        }
    }
}

using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using BrazilEconomicMonitor.Services;

namespace BrazilEconomicMonitor.Services
{
    public class ForecastError12MonthsTransformationService
    {
        private readonly BrazilEconomicMonitorDbContext _db;

        private readonly ILogger<ForecastError12MonthsTransformationService> _logger;

        private readonly HelperServices _helperServices;

        private readonly HashSet<string> _ForecastError12Months =
        [
            "10.03.1"
        ];

        public ForecastError12MonthsTransformationService(BrazilEconomicMonitorDbContext db, ILogger<ForecastError12MonthsTransformationService> logger, HelperServices helperServices)

        {
            _db = db;
            _logger = logger;
            _helperServices = helperServices;
        }

        public async Task ForecastError12Months(CancellationToken cancellationToken)
        {
            foreach (string code in _ForecastError12Months)
            {
                Series? inputSeries = await _db.Series.SingleOrDefaultAsync(s => s.Code == code, cancellationToken);

                if (inputSeries == null)
                    continue;
                List<Observation> observations =
                    await _db.Observations
                    .Where(o => o.SeriesId == inputSeries.Id)
                    .OrderByDescending(o => o.ObservationDate)
                    .Take(18)
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
            }
        }
    }

}

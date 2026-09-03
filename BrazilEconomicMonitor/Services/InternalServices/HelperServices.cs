using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BrazilEconomicMonitor.Services.InternalServices
{
    public class HelperServices
    {
        private readonly BrazilEconomicMonitorDbContext _db;

        private readonly ILogger _logger;

        public HelperServices(BrazilEconomicMonitorDbContext db, ILogger logger)

            {
                _db = db;
                _logger = logger;
            }

        public async Task UpsertDerivedObservationAsync( // ***** PERFORM _db.SaveChangesAsync AFTERWARDS ******
            int derivedSeriesId,
            DateTime observationDate,
            decimal value,
            CancellationToken cancellationToken)
        {
            Observation? existing =
                await _db.Observations
                    .SingleOrDefaultAsync(
                        o =>
                            o.SeriesId == derivedSeriesId &&
                            o.ObservationDate == observationDate,
                            cancellationToken);

            if (existing == null)
            {
                Observation observation = new Observation
                {
                    SeriesId = derivedSeriesId,
                    ObservationDate = observationDate,
                    Value = value
                };

                _db.Observations.Add(observation);

            }
            else if (existing.Value != value)
            {
                existing.Value = value;
            }
        }

        public async Task<Series> FindOrCreateNewDerivedSeries(string derivedSeriesCode, string derivedSeriesName, CancellationToken cancellationToken)
        {

            Series? derivedSeries = await _db.Series
                .SingleOrDefaultAsync(
                    s => s.Code == derivedSeriesCode &&
                            !s.IsRaw,
                    cancellationToken);

            if (derivedSeries == null)
            {
                Sources? source = await _db.Sources.FirstOrDefaultAsync(s => s.Name == "Derived Value", cancellationToken);
                if (source == null)
                {
                    throw new InvalidOperationException("Derived value source not found");
                }

                derivedSeries = new Series
                {
                    Name = derivedSeriesName,
                    Code = derivedSeriesCode,
                    SourceId = source.Id,
                    IsRaw = false
                };

                _db.Series.Add(derivedSeries);
                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("New derived series {DerivedSeries}(code:{code}) created!", derivedSeries.Name, derivedSeriesCode);
            }

            return derivedSeries;
        }
    }
}

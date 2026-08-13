using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using BrazilEconomicMonitor.DTOs;

namespace BrazilEconomicMonitor.Services;

public class DataTransformationService
{
    private readonly BrazilEconomicMonitorDbContext _db;

    private readonly ILogger<DataTransformationService> _logger;

    private readonly HashSet<string> _ttmSeriesCodes =
        [
            "10.07.1",
            "10.09.1"
        ];
    private readonly HashSet<string> _YoYSeriesCodes =
        [
            "10.07.1",
            "10.09.1"
        ];

    public DataTransformationService(BrazilEconomicMonitorDbContext db, ILogger<DataTransformationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task CalculateTtmAsync(CancellationToken cancellationToken)
    {
        foreach (string code in _ttmSeriesCodes)
        {
            Series? rawSeries = await _db.Series.SingleOrDefaultAsync(s => s.Code == code && s.IsRaw, cancellationToken);

            if (rawSeries == null)
                continue;

            List<Observation> observations =
                await _db.Observations
                .Where(o => o.SeriesId == rawSeries.Id)
                .OrderByDescending(o => o.ObservationDate)
                .Take(17)
                .ToListAsync(cancellationToken);

            observations.Reverse();

            string ttmCode = code + "_TTM";

            Series? ttmSeries = await _db.Series
                .SingleOrDefaultAsync(
                    s => s.Code == ttmCode &&
                            !s.IsRaw,
                    cancellationToken);

            for (int i = 11; i < observations.Count; i++)
            {
                Observation first = observations[i - 11];
                Observation last = observations[i];

                if (last.ObservationDate !=
                    first.ObservationDate.AddMonths(11))
                {
                    _logger.LogWarning(
                    "TTM calculation skipped for series {Code}. " +
                    "Expected 12 consecutive months between {FirstDate} and {LastDate}.",
                    code,
                    first.ObservationDate,
                    last.ObservationDate);

                    continue;
                }

                decimal sum = 0;

                for (int j = i - 11; j <= i; j++)
                {
                    sum += observations[j].Value;
                }
                await UpsertDerivedObservationAsync(ttmSeries.Id, last.ObservationDate, sum, cancellationToken);
            }
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task UpsertDerivedObservationAsync(
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
}
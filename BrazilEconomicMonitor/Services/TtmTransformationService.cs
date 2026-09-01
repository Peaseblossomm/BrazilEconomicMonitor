using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.DTOs;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;
using BrazilEconomicMonitor.Services;


namespace BrazilEconomicMonitor.Services;

public class TtmTransformationService
{
    private readonly BrazilEconomicMonitorDbContext _db;

    private readonly ILogger<TtmTransformationService> _logger;

    private readonly HelperServices _helperServices;


    private readonly HashSet<string> _ttmSeriesCodes =  // Raw series for which we apply ttm transformation 
        [
            "10.07.1",
            "10.09.1"
        ];
    public TtmTransformationService( BrazilEconomicMonitorDbContext db, ILogger<TtmTransformationService> logger, HelperServices helperServices)

    {
        _db = db;
        _logger = logger;
        _helperServices = helperServices;
    }

    public async Task CalculateTtmAsync(DateTime startDate, CancellationToken cancellationToken)
    {
        foreach (string code in _ttmSeriesCodes)
        {
            Series? inputSeries = await _db.Series.SingleOrDefaultAsync(s => s.Code == code, cancellationToken);

            if (inputSeries == null)
                continue;

            List<Observation> observations =
                await _db.Observations
                .Where(o => o.SeriesId == inputSeries.Id && o.ObservationDate >= startDate)
                .OrderBy(o => o.ObservationDate)
                .ToListAsync(cancellationToken);

            string derivedSeriesCode = code + "_TTM";
            string derivedSeriesName = inputSeries.Name + " TTM";

            Series ttmSeries = await _helperServices.FindOrCreateNewDerivedSeries(derivedSeriesCode, derivedSeriesName, cancellationToken);

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

                decimal ttmSum = 0;

                for (int j = i - 11; j <= i; j++)
                {
                    ttmSum += observations[j].Value;
                }
                await _helperServices.UpsertDerivedObservationAsync(ttmSeries.Id, last.ObservationDate, ttmSum, cancellationToken);
            }
        await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SeedTtmAsync(CancellationToken cancellationToken)
    {
        DateTime startDate = new DateTime(2010, 1, 1).AddMonths(-12);

        await CalculateTtmAsync(startDate, cancellationToken);
    }

    public async Task UpdateTtmAsync(CancellationToken cancellationToken)
    {
        foreach (string code in _ttmSeriesCodes)
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

            DateTime startDate = latestDate.AddMonths(-18); // calculate the ttm for the latest 6 months

            await CalculateTtmAsync(startDate, cancellationToken);
        }
    }
}
using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.DTOs;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace BrazilEconomicMonitor.Services;

public class DataTransformationService
{
    private readonly BrazilEconomicMonitorDbContext _db;

    private readonly ILogger<DataTransformationService> _logger;


    private readonly HashSet<string> _ttmSeriesCodes =  // Raw series for which we apply ttm transformation 
        [
            "10.07.1",
            "10.09.1"
        ];
    private readonly HashSet<string> _YoYSeriesCodes =   // Raw series for which we apply YoY transformationh
        [
            "10.07.1",
            "10.09.1"
        ];
    private readonly HashSet<string> _PrincipalBalanceOverGDP =
        [
            "10.03.1"
        ];

    private readonly HashSet<string> _ForecastError12Months =
        [
            "10.03.1"
        ];
   

    public DataTransformationService(BrazilEconomicMonitorDbContext db, ILogger<DataTransformationService> logger)

    {
        _db = db;
        _logger = logger;
    }

    public async Task UpdateTtmAsync(CancellationToken cancellationToken)
    {
        foreach (string code in _ttmSeriesCodes)
        {
            Series? inputSeries = await _db.Series.SingleOrDefaultAsync(s => s.Code == code, cancellationToken);

            if (inputSeries == null)
                continue;

            List<Observation> observations =
                await _db.Observations
                .Where(o => o.SeriesId == inputSeries.Id)
                .OrderByDescending(o => o.ObservationDate)
                .Take(17)
                .ToListAsync(cancellationToken);

            observations.Reverse();

            string derivedSeriesCode = code + "_TTM";
            string derivedSeriesName = inputSeries.Name + " TTM";

            Series ttmSeries = await FindOrCreateNewDerivedSeries(derivedSeriesCode, derivedSeriesName, cancellationToken);

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
                await UpsertDerivedObservationAsync(ttmSeries.Id, last.ObservationDate, ttmSum, cancellationToken);
            }
        await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateYoYAsync(CancellationToken cancellationToken)
    {
        foreach (string code in _YoYSeriesCodes)
        {
            Series? inputSeries = await _db.Series.SingleOrDefaultAsync(s => s.Code == code, cancellationToken);

            if (inputSeries == null)
                continue;

            List<Observation> observations =
                await _db.Observations
                .Where(o => o.SeriesId == inputSeries.Id)
                .OrderByDescending(o => o.ObservationDate)
                .Take(24)
                .ToListAsync(cancellationToken);

            string derivedSeriesCode = code + "_YoY";
            string derivedSeriesName = inputSeries.Name + " YoY";

            Series YoYSeries = await FindOrCreateNewDerivedSeries(derivedSeriesCode, derivedSeriesName, cancellationToken);

            for (int i = 0; i < observations.Count - 12; i++)
            {

                Observation current = observations[i];
                Observation previousYear = observations[i - 12];

                if (observations[i - 12].Value == 0)
                {
                    throw new DivideByZeroException($"Cannot calculate YoY for {current.ObservationDate:MM/yyyy}" +
                    $"because the value for {previousYear.ObservationDate:MM/yyyy} is zero");
                }

                if (current.ObservationDate != previousYear.ObservationDate.AddYears(1))
                {
                    _logger.LogWarning(
                    "YoY calculation skipped for {Code} at {Date}: previous-year month is missing.",
                    code,
                    current.ObservationDate);
                }
                decimal YoYValue = ((current.Value / previousYear.Value) - 1) * 100;

                await UpsertDerivedObservationAsync(YoYSeries.Id, current.ObservationDate, YoYValue, cancellationToken);
            }
            await _db.SaveChangesAsync(cancellationToken);
        }
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

            Series error12MonthsSerie = await FindOrCreateNewDerivedSeries(derivedSeriesCode, derivedSeriesName, cancellationToken);

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

                await UpsertDerivedObservationAsync(error12MonthsSerie.Id, current.ObservationDate, error12MonthsValue, cancellationToken);
            }    

            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task PrimaryBalanceOverGdp(CancellationToken cancellationToken)
    {
        string derivedSeriesCode = "10.07.1_PrimaryBalanceOverGdp";
        string derivedSeriesName = "Primary Balance Over Gdp";

        Series primaryBalanceOverGdp = await FindOrCreateNewDerivedSeries(derivedSeriesCode, derivedSeriesName, cancellationToken);

        Observation? latestPrimaryBalanceObservation = await _db.Observations.Include(o => o.Series).Where(o => o.Series.Name == "Primary Balance")
            .OrderByDescending(o => o.ObservationDate).FirstOrDefaultAsync();

        Observation? latestNominalGdpObservation = await _db.Observations.Include(o => o.Series).Where(o => o.Series.Name == "Nominal GDP")
            .OrderByDescending(o => o.ObservationDate).FirstOrDefaultAsync();

        if (latestPrimaryBalanceObservation == null)
        {
            throw new Exception("No Principal Balance observations are written in db");
        }

        if (latestNominalGdpObservation == null)
        {
            throw new Exception("No Nominal GDP observations are written in db");
        }

        DateTime commonLatestDate = latestPrimaryBalanceObservation.ObservationDate < latestNominalGdpObservation.ObservationDate
                                ? latestPrimaryBalanceObservation.ObservationDate : latestNominalGdpObservation.ObservationDate;

        for (int i = 0; i <= 5; i++)
        {
            DateTime date = commonLatestDate.AddMonths(-i);

            decimal primBalance = await _db.Observations.Where(o => o.ObservationDate == date && o.SeriesId == latestPrimaryBalanceObservation.SeriesId).Select(s => s.Value).SingleOrDefaultAsync();
            decimal nomGdp = await _db.Observations.Where(o => o.ObservationDate == date && o.SeriesId == latestNominalGdpObservation.SeriesId).Select(s => s.Value).SingleOrDefaultAsync();

            if (primBalance == null || nomGdp == null)
            {
                continue;
            }
            if (nomGdp == 0m)
            {
                continue;
            }
            
            decimal primBalanceGdpRatio = primBalance * 100 / nomGdp;

            await UpsertDerivedObservationAsync(primaryBalanceOverGdp.Id, date, primBalanceGdpRatio, cancellationToken);
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
        await _db.SaveChangesAsync(cancellationToken);
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
        }
        return derivedSeries;
    }
}
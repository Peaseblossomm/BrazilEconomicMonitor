using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BrazilEconomicMonitor.Settings;

namespace BrazilEconomicMonitor.Services.TransformationServices
{
    public class YoyTransformationService
    {
        private readonly BrazilEconomicMonitorDbContext _db;

        private readonly ILogger<YoyTransformationService> _logger;

        private readonly HelperServices _helperServices;

        private readonly int _LookbackMonths;

        private readonly HashSet<string> _YoYSeriesCodes =   // Raw series for which we apply YoY transformationh
           [
               "10.07.1",
                "10.09.1",
                "4382"
           ];
        public YoyTransformationService(BrazilEconomicMonitorDbContext db, ILogger<YoyTransformationService> logger,
            HelperServices helperServices, IOptions<ImportSettings> options)

        {
            _db = db;
            _logger = logger;
            _helperServices = helperServices;
            _LookbackMonths = options.Value.LookbackMonths;
        }

        public async Task CalculateYoyAsync(DateTime startDate, CancellationToken cancellationToken)
        {

            List<string> successfullyTransformedSeries = new List<string>();

            foreach (string code in _YoYSeriesCodes)
            {
                Series? inputSeries = await _db.Series.SingleOrDefaultAsync(s => s.Code == code, cancellationToken);

                if (inputSeries == null)
                    continue;

                List<Observation> observations =
                    await _db.Observations
                    .Where(o => o.SeriesId == inputSeries.Id)
                    .OrderByDescending(o => o.ObservationDate >= startDate)
                    .ToListAsync(cancellationToken);

                string derivedSeriesCode = code + "_YoY";
                string derivedSeriesName = inputSeries.Name + " YoY";

                Series YoYSeries = await _helperServices.FindOrCreateNewDerivedSeries(derivedSeriesCode, derivedSeriesName, cancellationToken);

                for (int i = 0; i < observations.Count - 12; i++)
                {

                    Observation current = observations[i];
                    Observation previousYear = observations[i + 12];

                    if (previousYear.Value == 0)
                    {
                        throw new DivideByZeroException($"Cannot calculate YoY for series {code} for {current.ObservationDate:MM/yyyy}" +
                        $"because the value for {previousYear.ObservationDate:MM/yyyy} is zero");
                    }

                    if (current.ObservationDate != previousYear.ObservationDate.AddYears(-1))
                    {
                        _logger.LogWarning(
                        "YoY calculation skipped for {Code} at {Date}: previous-year month is missing.",
                        code,
                        current.ObservationDate);

                        continue;
                    }

                    decimal YoYValue = ((current.Value / previousYear.Value) - 1) * 100;

                    await _helperServices.UpsertDerivedObservationAsync(YoYSeries.Id, current.ObservationDate, YoYValue, cancellationToken);

                    successfullyTransformedSeries.Add(code);

                }
                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Saved YoY observations successfully! Series transformed: {series}", successfullyTransformedSeries);
            }
        }

        public async Task SeedYoyAsync(CancellationToken cancellationToken)
        {
            DateTime startDate = new DateTime(2015, 1, 1).AddMonths(-12);

            await CalculateYoyAsync(startDate, cancellationToken);

            _logger.LogInformation("Seeded YoY observations since 1 jan 2015 successfully!");

        }

        public async Task UpdateYoyAsync(CancellationToken cancellationToken)
        {
            foreach (string code in _YoYSeriesCodes)
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

                DateTime startDate = latestDate.AddMonths(-(_LookbackMonths + 12));      // calculate the ttm for the latest "Lookback" months, IOptions

                await CalculateYoyAsync(startDate, cancellationToken);
            }
        }
    }    
}

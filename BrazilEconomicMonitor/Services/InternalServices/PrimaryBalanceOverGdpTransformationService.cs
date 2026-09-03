using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BrazilEconomicMonitor.Services.InternalServices
{
    public class PrimaryBalanceOverGdpTransformationService
    {
        private readonly BrazilEconomicMonitorDbContext _db;

        private readonly ILogger<PrimaryBalanceOverGdpTransformationService> _logger;

        private readonly HelperServices _helperServices;

        private readonly int _LookbackMonths;

        public PrimaryBalanceOverGdpTransformationService(BrazilEconomicMonitorDbContext db, ILogger<PrimaryBalanceOverGdpTransformationService> logger,
            HelperServices helperServices, IOptions<ImportSettings> options)

        {
            _db = db;
            _logger = logger;
            _helperServices = helperServices;
            _LookbackMonths = options.Value.LookbackMonths;
        }

        public async Task CalculatePrimaryBalanceOverGdpAsync(DateTime startDate, CancellationToken cancellationToken)
        {

            string derivedSeriesCode = "10.07.1_PrimaryBalanceOverGdp";
            string derivedSeriesName = "Primary Balance Over Gdp";

            Series primaryBalanceOverGdp = await _helperServices.FindOrCreateNewDerivedSeries(derivedSeriesCode, derivedSeriesName, cancellationToken);

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

            _logger.LogInformation("Started calculating from {date} to {date}(latest) ", startDate, commonLatestDate);

            int months = (commonLatestDate.Year - startDate.Year) * 12 + commonLatestDate.Month - startDate.Month;



            for (int i = 0; i <= months; i++)
            {
                DateTime date = startDate.AddMonths(+i);

                Observation? primBalance = await _db.Observations.Where(o => o.ObservationDate == date && o.SeriesId == latestPrimaryBalanceObservation.SeriesId).SingleOrDefaultAsync();
                Observation? nomGdp = await _db.Observations.Where(o => o.ObservationDate == date && o.SeriesId == latestNominalGdpObservation.SeriesId).SingleOrDefaultAsync();


                if (primBalance == null)
                {
                    _logger.LogWarning("PrimBalanceOverGdp calculation skipped, observation for Primary Balance for {date} is missing", date);
                    continue;
                }
                if (nomGdp == null)
                {
                    _logger.LogWarning("PrimBalanceOverGdp calculation skipped, observation for Nominal Gdp for {date} is missing", date);
                    continue;
                }

                decimal primBalanceGdpRatio = primBalance.Value * 100m / nomGdp.Value;

                await _helperServices.UpsertDerivedObservationAsync(primaryBalanceOverGdp.Id, date, primBalanceGdpRatio, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Saved PrimaryBalance/NominalGdp observations successfully!" +
                " Derived from series: 10.07.1, 4382");
        }

        public async Task SeedPrimaryBalanceOverGdpAsync(CancellationToken cancellationToken)
        {
            DateTime startDate = new DateTime(2010, 1, 1);

            _logger.LogInformation("Started seeding the database with PrimaryBalance/NominalGdp transformations." +
                " Derived from series: 10.07.1, 4382");

            await CalculatePrimaryBalanceOverGdpAsync(startDate, cancellationToken);

            _logger.LogInformation("Seeded PrimaryBalance/NominalGdp observations successfully!");
        }

        public async Task UpdatePrimaryBalanceOverGdpAsync(CancellationToken cancellationToken)
        {
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

            DateTime startDate = commonLatestDate.AddMonths(-_LookbackMonths);  // calculate the PrimBalance/NomGdp for the latest "Lookback" months, IOptions

            await CalculatePrimaryBalanceOverGdpAsync(startDate, cancellationToken);
        }
    }
}

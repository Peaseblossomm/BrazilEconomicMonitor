using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using BrazilEconomicMonitor.Services;

namespace BrazilEconomicMonitor.Services
{
    public class PrimaryBalanceOverGdpTransformationService
    {
        private readonly BrazilEconomicMonitorDbContext _db;

        private readonly ILogger<PrimaryBalanceOverGdpTransformationService> _logger;

        private readonly HelperServices _helperServices;

        public PrimaryBalanceOverGdpTransformationService(BrazilEconomicMonitorDbContext db, ILogger<PrimaryBalanceOverGdpTransformationService> logger, HelperServices helperServices)

        {
            _db = db;
            _logger = logger;
            _helperServices = helperServices;
        }

        public async Task PrimaryBalanceOverGdp(CancellationToken cancellationToken)
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

            for (int i = 0; i <= 5; i++)
            {
                DateTime date = commonLatestDate.AddMonths(-i);

                Observation? primBalance = await _db.Observations.Where(o => o.ObservationDate == date && o.SeriesId == latestPrimaryBalanceObservation.SeriesId).SingleOrDefaultAsync();
                Observation? nomGdp = await _db.Observations.Where(o => o.ObservationDate == date && o.SeriesId == latestNominalGdpObservation.SeriesId).SingleOrDefaultAsync();

                if (primBalance == null || nomGdp == null)
                {
                    continue;
                }

                decimal primBalanceGdpRatio = primBalance.Value * 100m / nomGdp.Value;

                await _helperServices.UpsertDerivedObservationAsync(primaryBalanceOverGdp.Id, date, primBalanceGdpRatio, cancellationToken);
            }
        }
    }
}

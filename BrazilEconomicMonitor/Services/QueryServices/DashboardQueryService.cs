using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.DTOs;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BrazilEconomicMonitor.Services.QueryServices
{
    public class DashboardQueryService
    {
        private readonly BrazilEconomicMonitorDbContext _db;

        public DashboardQueryService(BrazilEconomicMonitorDbContext db)
        {
            _db = db;
        }

        public async Task<ObservationResponseDto?> GetObservationsByDateAsync(
            string seriesCode,
            CancellationToken cancellationToken)
        {
            return await _db.Observations.Where(o => o.Series.Code == seriesCode)
                 .OrderByDescending(o => o.ObservationDate)
                 .Select(o => new ObservationResponseDto
                 {
                     Date = o.ObservationDate,
                     Value = o.Value
                 })
                 .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<ObservationResponseDto>>GetLatestObservationsByCountAsync(
            string seriesCode,
            int count,
            CancellationToken cancellation)
        {
            var seriesExist = await _db.Series.Where(o => o.Code == seriesCode).SingleOrDefaultAsync();

            if (seriesExist == null)
            {
                return new List<ObservationResponseDto>();
            }

            List<ObservationResponseDto> observations =
                await _db.Observations
                .Where(o => o.Series.Code == seriesCode)
                .OrderByDescending(o => o.ObservationDate)
                .Take(count)
                .Select(o => new ObservationResponseDto
                {
                    Date = o.ObservationDate,
                    Value = o.Value
                })
                .ToListAsync();

            observations.Reverse();

            return observations;
        }
    }
}

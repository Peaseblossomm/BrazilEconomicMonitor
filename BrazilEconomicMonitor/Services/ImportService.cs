using BrazilEconomicMonitor.Infrastructure;

namespace BrazilEconomicMonitor.Services
{
    public class ImportService
    {
        private readonly TreasuryApiClient _client;
        private readonly BrazilEconomicMonitorDbContext _db;

        public ImportService(
        TreasuryApiClient client,
        BrazilEconomicMonitorDbContext db)
        {
            _client = client;
            _db = db;
        }

        public async Task ImportFiscalAsync(
        string code,
        string startDate,
        string endDate)
        {
            var json = await _client.GetFiscalResultAsync(
            code,
            startDate,
            endDate);

            // deserialize json

            // map to Observation

            // _db.Observations.Add(...)

            await _db.SaveChangesAsync();
        }
    }
}

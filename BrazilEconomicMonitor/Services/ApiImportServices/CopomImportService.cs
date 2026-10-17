using BrazilEconomicMonitor.Infrastructure;

namespace BrazilEconomicMonitor.Services.ApiImportServices
{
    public class CopomImportService
    {
        private readonly CopomApiClient _client;
        private readonly BrazilEconomicMonitorDbContext _db;
        private readonly ILogger<CopomImportService> _logger;

        CopomImportService(CopomApiClient client,
            BrazilEconomicMonitorDbContext db,
            ILogger<CopomApiClient> logger
            )
        {

        }


    }
}

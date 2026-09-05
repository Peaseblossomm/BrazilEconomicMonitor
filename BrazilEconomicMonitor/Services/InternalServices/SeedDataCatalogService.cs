using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.DTOs;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Xml.Linq;

namespace BrazilEconomicMonitor.Services.InternalServices
{
    public class SeedDataCatalogService
    {
        private readonly TreasuryApiClient _client;

        private readonly BrazilEconomicMonitorDbContext _db;

        private readonly ILogger<SeedDataCatalogService> _logger;

        private const string Source =
        "https://sisweb.tesouro.gov.br/apex/f?p=10250:7:101490171757515::NO:7:P7_ID_PROJETO:1766";

        private const string SeriesType = "Raw";

        private static readonly HashSet<string> WantedSeriesCodes =
    [
        "10.03.1",
        "10.03.1.4",
        "10.07.1",
        "10.09.1"
    ];

        public SeedDataCatalogService(
            TreasuryApiClient client,
            BrazilEconomicMonitorDbContext db, ILogger<SeedDataCatalogService> logger)
        {
            _client = client;
            _db = db;
            _logger = logger;
        }

        public async Task<int> SeedSourcesAsync(string Name, string SourceDocLink, 
        CancellationToken cancellationToken = default)
        {
            var source = new Sources
            {
                Name = Name,
                DocLink = SourceDocLink
            };
            _db.Sources.Add(source);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully seeded {Name} Source entity", Name);

            return source.Id;
        }
        public async Task SeedSeriesAsync(string Name, string Code, int SourceId, CancellationToken cancellationToken = default)
        {
            var series = new Series
            {
                Name = Name,
                Code = Code,
                SourceId = SourceId,
                IsRaw = true
            };

            _db.Series.Add(series);

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully seeded {Name} Series entity", Name);
        }
    }
}

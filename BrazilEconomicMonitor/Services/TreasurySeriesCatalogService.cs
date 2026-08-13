using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.DTOs;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Xml.Linq;

namespace BrazilEconomicMonitor.Services
{
    public class DataSeriesCatalogService
    {
        private readonly TreasuryApiClient _client;

        private readonly BrazilEconomicMonitorDbContext _db;

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

        public DataSeriesCatalogService(
            TreasuryApiClient client,
            BrazilEconomicMonitorDbContext db)
        {
            _client = client;
            _db = db;
        }

        public async Task ImportSelectedSeriesAsync(
        CancellationToken cancellationToken = default)
        {
            var json = await _client.GetSeriesCatalogAsync(
                page: 1,
                pageSize: 1000,
                cancellationToken);

            var response =
            JsonSerializer.Deserialize<TreasurySeriesCatalogResponseDto>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            if (response == null)
                return;

            var selectedRecords = response.Registros
            .Where(r => WantedSeriesCodes.Contains(r.CodigoSerie))
            .ToList();

            foreach (var record in selectedRecords)
            {
                var exists = await _db.Series
                    .AnyAsync(
                        s =>
                            s.Code == record.CodigoSerie &&
                            s.Source == Source,
                        cancellationToken);

                if (exists)
                    continue;

                var series = new Series
                {
                    Code = record.CodigoSerie,
                    Name = record.NomeSerie,
                    Source = Source
                };
                _db.Series.Add(series);
            }
                await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task ImportDerivedSeriesManually (string Name, string Code, CancellationToken cancellationToken = default)
        {
            var series = new Series
            {
                Name = Name,
                Code = Code,
                Source = "Calculated",
                IsRaw = false
            };
            _db.Series.Add(series);

            await _db.SaveChangesAsync(cancellationToken);

            Console.WriteLine("Series imported successfully.");
        }

    }
}

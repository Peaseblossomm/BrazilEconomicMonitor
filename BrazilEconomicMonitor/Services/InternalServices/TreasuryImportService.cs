using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.DTOs;
using BrazilEconomicMonitor.Infrastructure;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BrazilEconomicMonitor.Settings;
using System.Globalization;

namespace BrazilEconomicMonitor.Services.InternalServices
{
    public class TreasuryImportService
    {
        private readonly TreasuryApiClient _client;
        private readonly BrazilEconomicMonitorDbContext _db;
        private readonly int _LookbackMonths;
        private readonly ILogger<TreasuryImportService> _logger;

        public TreasuryImportService(
        TreasuryApiClient client,
        BrazilEconomicMonitorDbContext db,
        IOptions<ImportSettings> options,
        ILogger<TreasuryImportService> logger)
        {
            _client = client;
            _db = db;
            _LookbackMonths = options.Value.LookbackMonths;
            _logger = logger;
        }

        public async Task UpdateTreasuryDataAsync(CancellationToken cancellationToken)
        {
            List<Series> series = await _db.Series.Where(s => s.Sources.Name == "Treasury").ToListAsync(cancellationToken);

            foreach (Series serie in series)
            {
                string code = serie.Code;

                DateTime? latestDate = await _db.Observations.Where(s => s.Series.Id == serie.Id).Select(o => (DateTime?)o.ObservationDate).MaxAsync(cancellationToken);

                DateTime startDate =
                   latestDate?.AddMonths(-_LookbackMonths)
                   ?? new DateTime(2010, 1, 1);

                string apiStartDate =
                    startDate.ToString("MM/yyyy", CultureInfo.InvariantCulture);

                await ImportFiscalAsync(
                    code,
                    apiStartDate,
                    "",
                    cancellationToken);

                _logger.LogInformation("Updated latest data for Treasury series {serie} up to {latestDate}", serie.Name, latestDate);
            }
        }

        public async Task ImportFiscalAsync(
        string code,
        string startDate,
        string? endDate,
            CancellationToken cancellationToken)
        {
            string json = await _client.GetFiscalResultAsync(
            code,
            startDate,
            endDate,
            cancellationToken);

            TreasuryResponseDto? response =
                JsonSerializer.Deserialize<TreasuryResponseDto>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (response == null)
                return;

            Series? series = await _db.Series
                .SingleOrDefaultAsync(s => s.Code == code &&
                s.Sources.Name == "Treasury", cancellationToken);

            if (series == null)
            {
                throw new Exception($"Series {code} not found.");
            }

            foreach (TreasuryRecordDto record in response.Registros)
            {
                DateTime observationDate =
                    new DateTime(
                        record.Data.Year,
                        record.Data.Month,
                        1);

                Observation? existing =
                   await _db.Observations
                       .SingleOrDefaultAsync(
                           o =>
                               o.SeriesId == series.Id &&
                               o.ObservationDate == observationDate,
                           cancellationToken);

                if (existing == null)
                {
                    Observation? observation = new Observation
                    {
                        SeriesId = series.Id,
                        ObservationDate = observationDate,
                        Value = record.Valor
                    };

                    _db.Observations.Add(observation);
                }
            }
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

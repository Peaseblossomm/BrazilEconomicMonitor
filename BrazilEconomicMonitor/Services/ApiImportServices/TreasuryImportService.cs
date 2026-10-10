using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.DTOs;
using BrazilEconomicMonitor.Infrastructure;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BrazilEconomicMonitor.Settings;
using System.Globalization;
using BrazilEconomicMonitor.CustomExceptions;

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

        public async Task UpdateTreasuryDataAsync(CancellationToken cancellationToken)      // Forms the query parameters for the scheduled calling of ImportFiscalAsync by the background worker orchestrator based on the latest ObservationDate.
        {                                                                                   // Fetches x LookbackMonths from the latest observation date found in the db for each serie
                                                                                            
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
        string seriesCode,
        string startDate,
        string? endDate,
            CancellationToken cancellationToken)
        {

               string json = await _client.GetFiscalResultAsync(
                seriesCode,
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
            {
                throw new InvalidOperationException($"Api response {seriesCode} is not found.");
            }

            Series? series = await _db.Series
                .SingleOrDefaultAsync(s => s.Code == seriesCode &&
                s.Sources.Name == "Treasury", cancellationToken);

            if (series == null)
            {
                throw new InvalidOperationException($"Series {seriesCode} not found.");
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

                if (existing != null) //update branch
                {
                    if (existing.Value != record.Valor)
                    {
                        existing.Value = record.Valor;

                        _logger.LogInformation("Revision of observation {observation} from source Treasury API for date {date}", existing.Series.Name, existing.ObservationDate);
                    }

                    continue;
                }

                Observation? observation = new Observation   // insert branch
                {
                    SeriesId = series.Id,
                    ObservationDate = observationDate,
                    Value = record.Valor
                };

                _db.Observations.Add(observation);

            }
                await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

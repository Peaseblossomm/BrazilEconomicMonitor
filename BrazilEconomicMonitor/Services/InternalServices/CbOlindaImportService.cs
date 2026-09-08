using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.DTOs;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
using BrazilEconomicMonitor.Services;
using Microsoft.Extensions.Options;
using BrazilEconomicMonitor.Settings;

namespace BrazilEconomicMonitor.Services.InternalServices
{
    public class CbOlindaImportService

    {
        private readonly CbOlindaApiClient _client;
        private readonly BrazilEconomicMonitorDbContext _db;
        private readonly ILogger<CbOlindaImportService> _logger;
        private readonly int _LookbackMonths;


        public CbOlindaImportService(CbOlindaApiClient client,
            BrazilEconomicMonitorDbContext db,
            ILogger<CbOlindaImportService> logger,
            IOptions<ImportSettings> options)
        {
            _client = client;
            _db = db;
            _logger = logger;
            _LookbackMonths = options.Value.LookbackMonths;

        }

        public async Task ImportIntrestRatesExpectationsAsync(
            int count,
            CancellationToken cancellationToken)
        {
            string json = await _client.GetInterestRatesExpectationsAsync(
            count,
            cancellationToken);

            CbOlindaRatesResponseDto? response =
                JsonSerializer.Deserialize<CbOlindaRatesResponseDto>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (response == null)
                return;


            Sources? source = await _db.Sources
                   .Where(o => o.Name == "Central Bank Olinda").SingleOrDefaultAsync();

            if (source == null)
            {
                throw new Exception("Source could not be found");
            }

            foreach (CbOlindaRatesRecordDto record in response.value)
            {
                Series? existing =
                    await _db.Series.
                    SingleOrDefaultAsync(
                    o =>
                    o.Name == record.Reuniao,
                    cancellationToken);

                if (existing != null)
                {
                    break;
                }

                Series seriesPerMeeting = new Series
                {
                    Name = "Selic Rate " + record.Reuniao,
                    Code = "ExpectativasMercadoSelic_" + record.Reuniao,
                    SourceId = source.Id
                };

                _db.Series.Add(seriesPerMeeting);
            }

            await _db.SaveChangesAsync(cancellationToken);

            foreach (CbOlindaRatesRecordDto record in response.value)
            {
                Series? seriesPerMeeting = await _db.Series.Where(
                    o => o.Name == "Selic Rate " + record.Reuniao)
                    .SingleOrDefaultAsync(cancellationToken);

                if (seriesPerMeeting == null)
                {
                    throw new Exception("Series is not written in the db");
                }

                Observation? existing =
                   await _db.Observations
                       .SingleOrDefaultAsync(
                           o =>
                               o.Series.Name == "Selic Rate " + record.Reuniao &&
                               o.ObservationDate == record.Data,
                           cancellationToken);

                if (existing != null)
                {
                    existing.Value = record.Mediana;
                }

                Observation observation = new Observation
                {
                    SeriesId = seriesPerMeeting.Id,
                    ObservationDate = record.Data,
                    Value = record.Mediana
                };
                _db.Observations.Add(observation);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task ImportInflationExpectationsAsync(
            int count,
            CancellationToken cancellationToken)
        {
            string json = await _client.GetInflationExpectationsAsync(
            count,
            cancellationToken);

            CbOlindaInflationResponseDto? response =
                JsonSerializer.Deserialize<CbOlindaInflationResponseDto>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (response == null)
                return;

            Series? series = await _db.Series
               .SingleOrDefaultAsync(s => s.Code == "ExpectativasMercadoInflacao12Meses" &&
               s.Sources.Name == "Central Bank Olinda",
               cancellationToken);

            if (series == null)
            {
                throw new Exception($"Series for inflation expectation not found.");
            }

            foreach (var record in response.value)
            {
                DateTime observationDate = record.Data;

                Observation? existing =
                   await _db.Observations
                       .SingleOrDefaultAsync(
                           o =>
                               o.SeriesId == series.Id &&
                               o.ObservationDate == observationDate,
                           cancellationToken);

                if (existing != null)
                {
                    existing.Value = record.Mediana;
                    continue;
                }

                Observation observation = new Observation
                {
                    SeriesId = series.Id,
                    ObservationDate = observationDate,
                    Value = record.Mediana
                };

                _db.Observations.Add(observation);
            }
            await _db.SaveChangesAsync();
        }

    }
}

using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.DTOs;
using BrazilEconomicMonitor.Infrastructure;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace BrazilEconomicMonitor.Services
{
    public class TreasuryImportService
    {
        private readonly TreasuryApiClient _client;
        private readonly BrazilEconomicMonitorDbContext _db;

        private const string Source =
        "https://sisweb.tesouro.gov.br/apex/f?p=10250:7:101490171757515::NO:7:P7_ID_PROJETO:1766";

        public TreasuryImportService(
        TreasuryApiClient client,
        BrazilEconomicMonitorDbContext db)
        {
            _client = client;
            _db = db;
        }

        public async Task ImportFiscalAsync(
        string code,
        string startDate,
        string? endDate,
            CancellationToken cancellationToken)
        {
            var json = await _client.GetFiscalResultAsync(
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
                s.Source == Source, cancellationToken);

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
                        ObservationDate = record.Data,
                        Value = record.Valor
                    };

                    _db.Observations.Add(observation);
                }

                await _db.SaveChangesAsync(cancellationToken);

            }
        }

        public async Task AddLatestTreasuryData(CancellationToken cancellationToken)
        {
            List<Series> series = await _db.Series.Where(s => s.Source == Source).ToListAsync(cancellationToken);

            foreach (Series serie in series)
            {
                string code = serie.Code;

                DateTime? latestDate = await _db.Observations.Where(s => s.Series.Id == serie.Id).Select(o => (DateTime?)o.ObservationDate).MaxAsync(cancellationToken);

                DateTime startDate =
                   latestDate?.AddMonths(-3)
                   ?? new DateTime(2015, 1, 1);

                string apiStartDate =
                    startDate.ToString("MM/yyyy");


                await ImportFiscalAsync(
                    code,
                    apiStartDate,
                    null,
                    cancellationToken);
            }


        }


    }
}

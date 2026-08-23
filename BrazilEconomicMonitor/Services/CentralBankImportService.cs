using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.DTOs;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Options;
using BrazilEconomicMonitor.Settings;
using System.Globalization;

namespace BrazilEconomicMonitor.Services
{
    public class CentralBankImportService
    {
        private readonly CentralBankApiClient _client;
        private readonly BrazilEconomicMonitorDbContext _db;
        private readonly int _LookbackMonths;

        public CentralBankImportService(
            CentralBankApiClient client,
            BrazilEconomicMonitorDbContext db,
            IOptions<ImportSettings> options)
        {
            _client = client;
            _db = db;
            _LookbackMonths = options.Value.CentralBankLookbackMonths;
        }

        public async Task UpdateCentralBankDataAsync(CancellationToken cancellationToken)
        {
            List<Series> centralBankSeries = await _db.Series.Where(s => s.Sources.Name == "Central Bank").ToListAsync(cancellationToken);

            foreach (Series serie in centralBankSeries)
            {
                DateTime? latestDate = await _db.Observations.Where(s => s.SeriesId == serie.Id).Select(o => (DateTime?)o.ObservationDate).MaxAsync(cancellationToken);

                DateTime startDate = latestDate?.AddMonths(-_LookbackMonths)
                    ?? new DateTime(2015, 1, 1);

                string apiStartDate = startDate.ToString("MM/YYYY");

                await ImportDataAsync(
                    serie.Code,
                    apiStartDate,
                    "",
                    cancellationToken
                    );
            }

        }
        public async Task ImportDataAsync(
            string seriesCode,
            string startDate,
            string endDate,
            CancellationToken cancellationToken)
        {
            string json = await _client.GetFiscalResultsAsync(
                seriesCode,
                startDate,
                endDate);

            List<CentralBankRecordDto>? response =
                JsonSerializer.Deserialize<List<CentralBankRecordDto>>(
                    json,
                    new JsonSerializerOptions
                    { PropertyNameCaseInsensitive = true });

            if (response == null)
                return;

            Series? series = await _db.Series.SingleOrDefaultAsync(s =>
            s.Code == seriesCode && s.Sources.Name == "Central Bank", cancellationToken);

            if (series == null)
                throw new Exception("No series were found referencing Central Bank as the source and the given series code");

                foreach (CentralBankRecordDto dto in response)
                {
                    DateTime observationDate =
                        new DateTime(
                            dto.Data.Year,
                            dto.Data.Month,
                            1
                        );

                    Observation? existingObservation = await _db.Observations.SingleOrDefaultAsync( o => o.ObservationDate == observationDate && o.SeriesId == series.Id );

                    if (existingObservation == null)
                    {
                        Observation? observation = new Observation
                        {
                            SeriesId = series.Id,
                            ObservationDate = observationDate,
                            Value = dto.Valor
                        };

                        _db.Observations.Add(observation);
                    }

                    else if (dto.Valor != existingObservation.Value)
                    {
                        existingObservation.Value = dto.Valor;
                    }
                }
                await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

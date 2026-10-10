using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.DTOs;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Options;
using BrazilEconomicMonitor.Settings;
using System.Globalization;
using System.Linq.Expressions;
using BrazilEconomicMonitor.CustomExceptions;

namespace BrazilEconomicMonitor.Services.InternalServices
{
    public class CentralBankImportService
    {
        private readonly CentralBankApiClient _client;
        private readonly BrazilEconomicMonitorDbContext _db;
        private readonly int _LookbackMonths;
        private readonly ILogger<CentralBankImportService> _logger;

        public CentralBankImportService(
            CentralBankApiClient client,
            BrazilEconomicMonitorDbContext db,
            IOptions<ImportSettings> options,
            ILogger<CentralBankImportService> logger)
        {
            _client = client;
            _db = db;
            _LookbackMonths = options.Value.LookbackMonths;
            _logger = logger;
        }

        public async Task UpdateCentralBankDataAsync(CancellationToken cancellationToken) // Forms the query parameters for the scheduled calling of ImportFiscalAsync by the background worker orchestrator based on the latest ObservationDate.
                                                                                          // Fetches x LookbackMonths from the latest observation date found in the db for each serie

        {
            List<Series> centralBankSeries = await _db.Series.Where(s => s.Sources.Name == "Central Bank").ToListAsync(cancellationToken);  // All series fetched from CentralBank

            foreach (Series serie in centralBankSeries)
            {
                DateTime? latestDate = await _db.Observations.Where(s => s.SeriesId == serie.Id).Select(o => (DateTime?)o.ObservationDate).MaxAsync(cancellationToken);

                DateTime startDate = latestDate?.AddMonths(-_LookbackMonths)
                    ?? new DateTime(2010, 1, 1);

                string apiStartDate = startDate.ToString("01/MM/yyyy",CultureInfo.InvariantCulture);

                string seriesCode = serie.Code;

                await ImportFiscalAsync(
                    seriesCode,
                    apiStartDate,
                    "",               // left out empty means up to the latest data
                    cancellationToken
                    );

                _logger.LogInformation("Updated latest data for Central Bank series {serie} up to {latestDate}", serie.Name, latestDate);
            }
        }






        public async Task ImportFiscalAsync(         // Calls the ApiClient method 
            string seriesCode,
            string startDate,
            string endDate,
            CancellationToken cancellationToken)
        {
            string json;

            string source = "Central Bank";

            try
            {
                json = await _client.GetFiscalResultsAsync(
                seriesCode,
                startDate,
                endDate);
            }
            catch (HttpRequestException ex)
            {
                throw new SeriesImportException
                    (
                    seriesCode,
                    source,
                    ex
                    );
            }
            
            List<CentralBankRecordDto>? response =
                JsonSerializer.Deserialize<List<CentralBankRecordDto>>(
                    json,
                    new JsonSerializerOptions
                    { PropertyNameCaseInsensitive = true });

            if (response == null)
            {
                throw new InvalidOperationException($"Api response for series {seriesCode} from source {source} is not found.");
            }
                


            Series? series = await _db.Series.SingleOrDefaultAsync(s =>
            s.Code == seriesCode && s.Sources.Name == "Central Bank", cancellationToken);

            if (series == null)
                throw new Exception($"No series were found referencing {source} the source and the given series code");

                foreach (CentralBankRecordDto dto in response)
                {

                    if (seriesCode == "432" && dto.Data.Day != 1)   // Central Bank Code 432 logic branch - Selic rate expectations are published on daily basis. 
                    {                                               // We'll normalize this data to the first of each month and skip pserting the rest.
                        continue;
                    }

                        DateTime observationDate =   // Normalizing the Date to the first of each month.
                            new DateTime(
                                dto.Data.Year,
                                dto.Data.Month,
                                1
                            );

                        Observation? existing = await _db.Observations.SingleOrDefaultAsync( o => o.ObservationDate == observationDate && o.SeriesId == series.Id );

                    if (existing != null)   //Insert
                    {
                        if (existing.Value != dto.Valor)
                        {
                            existing.Value = dto.Valor;

                            _logger.LogInformation("Revision of observation {observation} from source Treasury API for date {date}", existing.Series.Name, existing.ObservationDate);
                        }

                        continue;
                    }

                    Observation? observation = new Observation
                    {
                        SeriesId = series.Id,
                        ObservationDate = observationDate,
                        Value = dto.Valor
                    };

                    _db.Observations.Add(observation);

                }

                await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

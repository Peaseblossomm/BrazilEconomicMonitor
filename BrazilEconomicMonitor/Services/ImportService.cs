using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.DTOs;
using BrazilEconomicMonitor.Infrastructure;
using System.Text.Json;

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

        TreasuryResponseDto? response =
            JsonSerializer.Deserialize<TreasuryResponseDto>(
                json,
                new JsonSerializerOptions
                {
                PropertyNameCaseInsensitive = true
                });
        if (response == null)
            return;

        foreach (TreasuryRecordDto record in response.Registros)
        {
            Observation observation = new Observation
            {
                ObservationDate = record.Data,
                Value = record.Valor
            };

            _db.Observations.Add(observation);
            }

        await _db.SaveChangesAsync();

        }

    }
}

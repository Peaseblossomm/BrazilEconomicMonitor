namespace BrazilEconomicMonitor.Infrastructure
{
    public class TreasuryApiClient
    {
        private readonly HttpClient _httpClient;

        public TreasuryApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<string> GetFiscalResultAsync(
            string seriesCode,
            string startDate,
            string endDate)
        {
            var url =
            $"v1/series-temporais/custom/resultado-fiscal" +
            $"?data_inicio={startDate}" +
            $"&data_fim={endDate}" +
            $"&tema=10" +
            $"&codigo_da_serie={seriesCode}";

            return await _httpClient.GetStringAsync(url);
        }
    }
}
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
            string? endDate,
            CancellationToken cancellationToken = default)
        {
            var url =
            $"v1/series-temporais/custom/resultado-fiscal" +
            $"?data_inicio={startDate}" +
            $"&data_fim={endDate}" +
            $"&tema=10" +
            $"&codigo_da_serie={seriesCode}";

            return await _httpClient.GetStringAsync(
            url,
            cancellationToken);
        }

        public async Task<string> GetSeriesCatalogAsync(
            int page = 1,
            int pageSize = 1000,
            CancellationToken cancellationToken = default)
        {
            var url =
                $"v1/series-temporais/custom/series" +
                $"?page={page}" +
                $"&pageSize={pageSize}";

            return await _httpClient.GetStringAsync(
                url,
                cancellationToken);
        }
    }
}

namespace BrazilEconomicMonitor.Infrastructure
{
    public class CentralBankApiClient
    {
        private readonly HttpClient _httpClient;

        public CentralBankApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetFiscalResultsAsync(
                string seriesCode,
                string startDate,
                string endDate,
                CancellationToken cancellationToken = default)
        {
            var url =
                $".sgs.{seriesCode}/dados"+
                $"?formato=json" +
                $"&dataInicial={startDate}" +
                $"dataFinal={endDate}";

            return await _httpClient.GetStringAsync(
                url,
                cancellationToken);
        }
    }
}

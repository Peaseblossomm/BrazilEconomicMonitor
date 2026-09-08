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
                string startDate, // format dd/MM/yyyy
                string endDate, // format dd/MM/yyyy
                CancellationToken cancellationToken = default)
        {
            var url =
                $"bcdata.sgs.{seriesCode}/dados" +
                    $"?formato=json" +
                    $"&dataInicial={startDate}" +
                    $"&dataFinal={endDate}";

            return await _httpClient.GetStringAsync(
                url,
                cancellationToken);
        }
    }
}

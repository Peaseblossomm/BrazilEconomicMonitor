using System.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;


namespace BrazilEconomicMonitor.Infrastructure
{
    public class CentralBankApiClient
    {
        private readonly HttpClient _httpClient;

        private readonly ILogger<CentralBankApiClient> _logger;

        public CentralBankApiClient(HttpClient httpClient, ILogger<CentralBankApiClient> logger)
        {
            _httpClient = httpClient;

            _logger = logger;
        }

        public async Task<string> GetFiscalResultsAsync(
                string seriesCode,
                string startDate, // format dd/MM/yyyy
                string endDate, // format dd/MM/yyyy
                CancellationToken cancellationToken = default)
        {
            string endpoint = $"bcdata.sgs.{seriesCode}/dados";

            var queryParameters = new Dictionary<string, string?>
            {
                ["formato"] = "json",
                ["dataInicial"] = startDate,
                ["dataFinal"] = endDate
            };

            string url = QueryHelpers.AddQueryString(endpoint, queryParameters);


            /* var url =
                $"bcdata.sgs.{seriesCode}/dados" +
                    $"?formato=json" +
                    $"&dataInicial={startDate}" +
                    $"&dataFinal={endDate}"; */

            string json =
                await _httpClient.GetStringAsync(
                    url,
                    cancellationToken);

            _logger.LogInformation("Request URL: {Url}", url);

            return json;
        }
    }
}

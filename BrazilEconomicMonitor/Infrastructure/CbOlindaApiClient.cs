using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace BrazilEconomicMonitor.Infrastructure

{
    public class CbOlindaApiClient
    {
        private readonly HttpClient _httpClient;

        private readonly ILogger<CbOlindaApiClient> _logger;

        public CbOlindaApiClient(HttpClient httpClient, ILogger<CbOlindaApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        public async Task<string> GetInflationExpectationsAsync(
            int count,
            CancellationToken cancellationToken)
        {
            string endpoint = "ExpectativasMercadoInflacao12Meses";

            var queryParameters = new Dictionary<string, string?>
            {
            ["format"] = "json",
            ["orderby"] = "Data desc",
            ["filter"] = "Suavizada eq 'S' and baseCalculo eq 0 and Indicador eq 'IPCA Serviços'",
            ["top"] = count.ToString()
            };
            
            string url = QueryHelpers.AddQueryString(endpoint, queryParameters);

            /* var url =
            $"ExpectativasMercadoInflacao12Meses" +
            $"?$format=json" +
            $"&$orderby=Data desc" +
            $"&$filter=Suavizada eq 'S' and baseCalculo eq 0 and Indicador eq 'IPCA Serviços'" +
            $"&$top={count}"; */

            return await _httpClient.GetStringAsync(
                    url,
                    cancellationToken);
        }

        public async Task<string> GetInterestRatesExpectationsAsync(
            int count,
            CancellationToken cancellationToken)
        {
            var url =
            $"ExpectativasMercadoSelic" +
            $"?$format=json" +
            $"&$orderby=Data desc" +
            $"&$filter=baseCalculo eq 0" +
            $"&$top={count}";

            return await _httpClient.GetStringAsync(
                    url,
                    cancellationToken);

        }
    }
}

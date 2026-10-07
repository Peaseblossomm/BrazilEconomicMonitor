using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using BrazilEconomicMonitor.CustomExceptions;
using System.Runtime.CompilerServices;

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

            string json;

            string seriesCode = "ExpectativasMercadoInflacao12Meses";

            string source = "Central Bank Olinda";

            try
            {
                var response = await _httpClient.GetAsync(
                    url,
                    cancellationToken);

                    json = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new SeriesImportException(
                        seriesCode,
                        source,
                        response.StatusCode,
                        response.ReasonPhrase);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new SeriesImportException(
                    seriesCode,
                    source,
                    ex);
            }

            return json;
        }

        public async Task<string> GetInterestRatesExpectationsAsync(
            int count,
            CancellationToken cancellationToken)
        {

            string endpoint = "ExpectativasMercadoSelic";

            var queryParameters = new Dictionary<string, string?>
            {
                ["format"] = "json",
                ["orderby"] = "Data desc",
                ["filter"] = "baseCalculo eq 0",
                ["top"] = count.ToString()
            };

            var url = QueryHelpers.AddQueryString(endpoint, queryParameters);

            string source = "Central Bank Olinda";

            string seriesCode = "ExpectativasMercadoSelic";

            string json;

            try
            {
                var response = await _httpClient.GetAsync(
                    url,
                    cancellationToken);

                json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new SeriesImportException(
                        seriesCode,
                        source,
                        response.StatusCode,
                        response.ReasonPhrase);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new SeriesImportException(
                    seriesCode,
                    source,
                    ex);
            }

            return json;

        }
    }
}

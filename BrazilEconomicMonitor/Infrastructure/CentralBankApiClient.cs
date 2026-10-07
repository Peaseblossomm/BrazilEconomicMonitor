using System.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;
using BrazilEconomicMonitor.CustomExceptions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;


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

            string source = "Central Bank";

            string json;

            try
            {
                using var response =
                    await _httpClient.GetAsync(
                    url,
                    cancellationToken);

                json = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {

                    /* _logger.LogError(
                        "Central Bank unsuccessful Http response" + "for Series:{SeriesCode}, Source: {source}, Status: {StatusCode}, Reason: {Reason}, Body: {Body}",
                        seriesCode,
                        source,
                        response.StatusCode,
                        response.ReasonPhrase,
                        json);             "variant 1 - Logged locally, doesn't propagate upwards"   */

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
                    ex
                    );
            }

            _logger.LogInformation("Request URL: {Url}", url);

            return json;
        }
    }
}

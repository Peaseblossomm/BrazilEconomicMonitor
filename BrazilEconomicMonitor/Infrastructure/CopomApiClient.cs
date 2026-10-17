using System.Diagnostics.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using BrazilEconomicMonitor.CustomExceptions;
using System.Runtime.CompilerServices;

namespace BrazilEconomicMonitor.Infrastructure
{
    public class CopomApiClient
    {
        private readonly HttpClient _httpClient;

        private readonly ILogger<CopomApiClient> _logger;

        public CopomApiClient(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> GetLatestCopomMeetingAsync(CancellationToken cancellationToken)
        {
            string endpoint = "comunicados";

            var queryParameters = new Dictionary<string, string?>
            {
                ["quantidade"] = "1"
            };

            string url = QueryHelpers.AddQueryString(endpoint, queryParameters);

            string json;

            string seriesCode = "CopomComunicados";

            string source = "Copom";

            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);

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
            catch (HttpIOException ex)
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

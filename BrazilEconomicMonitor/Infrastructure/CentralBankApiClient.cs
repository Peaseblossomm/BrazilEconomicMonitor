using System.Net.Http.Headers;


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
            var url =
                $"bcdata.sgs.{seriesCode}/dados" +
                    $"?formato=json" +
                    $"&dataInicial={startDate}" +
                    $"&dataFinal={endDate}";

            HttpRequestMessage request =
            new HttpRequestMessage(
            HttpMethod.Get,
            url);

            request.Headers.UserAgent.ParseAdd("Mozilla/5.0");

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            request.Headers.Referrer =
                new Uri("https://www3.bcb.gov.br/");

            HttpResponseMessage httpResponse =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            string json =
                await httpResponse.Content.ReadAsStringAsync(
                    cancellationToken);

            _logger.LogInformation("{status code}", httpResponse.StatusCode);

            _logger.LogInformation("{headers content}", httpResponse.Content.Headers.ContentType);

            _logger.LogInformation("{json response}", json);

            _logger.LogInformation("Request URL: {Url}", url);

            return json;
        }
    }
}

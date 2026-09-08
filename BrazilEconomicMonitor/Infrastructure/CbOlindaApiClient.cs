namespace BrazilEconomicMonitor.Infrastructure
{
    public class CbOlindaApiClient
    {
        private readonly HttpClient _httpClient;

        public CbOlindaApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<string> GetInflationExpectationsAsync(
            int count,
            CancellationToken cancellationToken)
        {
            var url =
            $"ExpectativasMercadoInflacao12Meses" +
            $"?$format=json" +
            $"&$orderby=Data desc" +
            $"&$filter=Suavizada eq 'S' and baseCalculo eq 0 and Indicador eq 'IPCA Serviços'" +
            $"&$top={count}";

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

namespace BrazilEconomicMonitor.Infrastructure
{
    public class TreasuryApiClient
    {
        private readonly HttpClient _httpClient;

        public TreasuryApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<string> GetGoogleAsync()
        {
            return await _httpClient.GetStringAsync(
            "https://www.google.com");
        }
    }
}

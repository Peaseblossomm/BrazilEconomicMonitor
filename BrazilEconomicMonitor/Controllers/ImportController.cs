using BrazilEconomicMonitor.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace BrazilEconomicMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly TreasuryApiClient _client;

        public ImportController(
        TreasuryApiClient client)
        {
            _client = client;
        }

        [HttpGet("test")]
        public async Task<string> Test()
        {
            return await _client.GetGoogleAsync();
        }
    }
}

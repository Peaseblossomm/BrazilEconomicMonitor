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

        [HttpGet("fiscal")]
        public async Task<string> Fiscal(
            string code,
            string startDate,
            string endDate)
        {
            return await _client.GetFiscalResultAsync(
            code,
            startDate,
            endDate);
        }
    }
}

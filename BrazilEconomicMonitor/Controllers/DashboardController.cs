using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.Services.QueryServices;
using Microsoft.AspNetCore.Mvc;
using BrazilEconomicMonitor.DTOs;

namespace BrazilEconomicMonitor.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardQueryService _dashboardQueryService;

    public DashboardController(
        DashboardQueryService dashboardQueryService)
        {
            _dashboardQueryService = dashboardQueryService;
        }

        [HttpGet("series/{code}/latest")]
        public async Task<ActionResult<ObservationResponseDto>> GetLatestObservation(
            string code,
            CancellationToken cancellationToken)
        {
            ObservationResponseDto? response =
            await _dashboardQueryService.GetLatestObservationAsync(
                    code,
                    cancellationToken);

                if (response == null)
                {
                return NotFound();
                }
            return Ok(response);
        }
    }
}

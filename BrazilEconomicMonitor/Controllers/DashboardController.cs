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

        [HttpGet("series/{seriesCode}/{count}")]
        public async Task<ActionResult<List<ObservationResponseDto>>> GetLatestGroupOfObservation(
        string seriesCode,
        int count,
        CancellationToken cancellationToken)

        {
            List<ObservationResponseDto> response =
            await _dashboardQueryService.GetLatestObservationsByCountAsync( // if series don't exist yet, it will return an empty list
                    seriesCode,
                    count,
                    cancellationToken);

            return Ok(response);
        }
    }
}

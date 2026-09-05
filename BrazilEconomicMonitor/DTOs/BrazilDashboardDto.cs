namespace BrazilEconomicMonitor.DTOs
{
    public class BrazilDashboardDto
    {
            public ObservationResponseDto? PrimaryBalanceOverGdp { get; set; }

            public ObservationResponseDto? InflationExpectation { get; set; }

            public ObservationResponseDto? SelicExpectation { get; set; }

            public ObservationResponseDto? BrlExchangeRate { get; set; }
        }
}


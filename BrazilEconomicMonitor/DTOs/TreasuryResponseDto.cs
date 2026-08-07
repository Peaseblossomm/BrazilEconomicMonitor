namespace BrazilEconomicMonitor.DTOs
{
    public class TreasuryResponseDto
    {
        public List<TreasuryRecordDto> Registros { get; set; }
        = new();
    }
}

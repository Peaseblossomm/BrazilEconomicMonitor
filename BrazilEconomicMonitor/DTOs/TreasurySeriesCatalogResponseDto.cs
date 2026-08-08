namespace BrazilEconomicMonitor.DTOs
{
    public class TreasurySeriesCatalogResponseDto
    {
        public string? Next { get; set; }

        public int PageSize { get; set; }

        public List<TreasurySeriesCatalogRecordDto> Registros { get; set; }
            = new();
    }
}

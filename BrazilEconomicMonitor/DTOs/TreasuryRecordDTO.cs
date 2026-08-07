namespace BrazilEconomicMonitor.DTOs
{
    public class TreasuryRecordDto
    {
        public DateTime Data { get; set; }

        public decimal Valor { get; set; }

        public string CodigoSerie { get; set; } = "";
    }
}

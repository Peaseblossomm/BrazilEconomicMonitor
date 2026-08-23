using System.Text.Json.Serialization;

namespace BrazilEconomicMonitor.DTOs
{
    public class CentralBankRecordDto
    {
        public DateTime Data { get; set; }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal Valor { get; set; }
        public string CodigoSerie { get; set; } = "";


    }
}

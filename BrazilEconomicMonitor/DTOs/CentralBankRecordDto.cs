using BrazilEconomicMonitor.Infrastructure;
using System.Text.Json.Serialization;

namespace BrazilEconomicMonitor.DTOs
{
    public class CentralBankRecordDto
    {
        [JsonConverter(typeof(DdMmYyyyDateTimeConverter))]
        public DateTime Data { get; set; }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal Valor { get; set; }
        public string CodigoSerie { get; set; } = "";


    }
}

using BrazilEconomicMonitor.Infrastructure;
using System.Text.Json.Serialization;

namespace BrazilEconomicMonitor.DTOs
{
    public class CbOlindaRatesRecordDto
    {

        [JsonConverter(typeof(yyyyMMddDateTimeConverter))]
        public DateTime Data { get; set; }
        public decimal Mediana { get; set; }
        public string Reuniao { get; set; } = "";
    }
}

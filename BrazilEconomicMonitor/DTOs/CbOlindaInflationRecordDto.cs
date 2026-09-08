using BrazilEconomicMonitor.Infrastructure;
using System.Text.Json.Serialization;

namespace BrazilEconomicMonitor.DTOs
{
    public class CbOlindaInflationRecordDto
    {
        [JsonConverter(typeof(yyyyMMddDateTimeConverter))]
        public DateTime Data { get; set; }
        public decimal Mediana { get; set; }

    }
}

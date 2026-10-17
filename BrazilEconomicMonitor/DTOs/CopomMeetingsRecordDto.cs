using BrazilEconomicMonitor.Infrastructure;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json.Serialization;

namespace BrazilEconomicMonitor.DTOs
{
    public class CopomMeetingsRecordDto
    {
        public int nro_reuniao { get; set; }

        [JsonConverter(typeof(yyyyMMddDateTimeConverter))]
        public DateTime dataReferencia { get; set; }
    }
}

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrazilEconomicMonitor.Infrastructure
{
    public class DdMmYyyyDateTimeConverter:JsonConverter<DateTime>
    {
        public override DateTime Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            string? value = reader.GetString();

            return DateTime.ParseExact(
                value!, 
                "dd/mm/yyyy",
                CultureInfo.InvariantCulture);
        }
    }
}

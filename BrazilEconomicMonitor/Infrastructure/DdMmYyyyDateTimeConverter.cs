using System.Globalization;
using System.Security.Cryptography.X509Certificates;
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
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture);
        }

        public override void Write(
            Utf8JsonWriter writer,
            DateTime value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(
                value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        }
    }
}

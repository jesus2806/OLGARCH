using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Helpers
{
    public class Decimal128StringJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // "1.5"
            if (reader.TokenType == JsonTokenType.String)
                return reader.GetString() ?? "";

            // 1.5
            if (reader.TokenType == JsonTokenType.Number)
                return reader.GetDecimal().ToString(System.Globalization.CultureInfo.InvariantCulture);

            // { "$numberDecimal": "1.5" }
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                if (doc.RootElement.TryGetProperty("$numberDecimal", out var el))
                    return el.GetString() ?? "";
                return "";
            }

            return "";
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value ?? "");
        }
    }
}

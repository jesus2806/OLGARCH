using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    public class ApiRespuesta<T>
    {
        [JsonPropertyName("success")]
        public bool bSuccess { get; set; }

        [JsonPropertyName("message")]
        public string sMessage { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        [JsonConverter(typeof(SingleOrArrayConverterFactory))]
        public List<T>? lData { get; set; }

        [JsonPropertyName("error")]
        public ErrorDetail? Error { get; set; }
    }

    public class ErrorDetail
    {
        [JsonPropertyName("code")]
        public int iCode { get; set; }

        [JsonPropertyName("details")]
        public string sDetails { get; set; } = string.Empty;
    }



    public class SingleOrArrayConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            // Verifica si el tipo es una lista genérica
            return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(List<>);
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            // Obtiene el tipo del elemento dentro de la lista
            Type itemType = typeToConvert.GetGenericArguments()[0];

            // Crea una instancia del convertidor genérico
            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(SingleOrArrayConverter<>).MakeGenericType(itemType))!;

            return converter;
        }
    }

    // Convertidor Genérico para List<T>
    public class SingleOrArrayConverter<T> : JsonConverter<List<T>>
    {
        public override List<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                // Si ya es un array, lo deserializa directamente
                return JsonSerializer.Deserialize<List<T>>(ref reader, options) ?? new List<T>();
            }
            else
            {
                // Si es un solo objeto, lo envuelve en una lista
                var singleItem = JsonSerializer.Deserialize<T>(ref reader, options);
                return singleItem != null ? new List<T> { singleItem } : new List<T>();
            }
        }

        public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }



}

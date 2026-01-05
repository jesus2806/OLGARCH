using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    public class ProductoIngrediente
    {
        // Id del ingrediente en Mongo
        [JsonPropertyName("sIdIngrediente")]
        public string sIdIngrediente { get; set; } = string.Empty;

        [JsonPropertyName("iCantidadUso")]
        public decimal iCantidadUso { get; set; }
    }
}

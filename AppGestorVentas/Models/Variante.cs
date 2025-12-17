using SQLite;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    [Table("tb_Variante")]
    public class Variante
    {
        [JsonPropertyName("_id")]
        public string sIdMongo { get; set; } = string.Empty;
        public string sIdMongoDBProducto { get; set; }

        [JsonPropertyName("sVariante")]
        public string sVariante { get; set; } = string.Empty;
    }
}

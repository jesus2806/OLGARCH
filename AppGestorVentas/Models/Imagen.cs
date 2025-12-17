using SQLite;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    [Table("tb_Imagen")]
    public class Imagen
    {
        [JsonPropertyName("_id")]
        public string sIdMongo { get; set; } = string.Empty;
        public string sIdMongoDBProducto { get; set; }

        [JsonPropertyName("sURLImagen")]
        public string sURLImagen { get; set; } = string.Empty;
    }
}

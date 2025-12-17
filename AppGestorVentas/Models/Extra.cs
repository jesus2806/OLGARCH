using SQLite;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    /// <summary>
    /// Representa un extra del catálogo (Pantalla 3)
    /// </summary>
    [Table("tb_Extra")]
    public class Extra
    {
        [PrimaryKey, AutoIncrement]
        public int iIdExtra { get; set; }
        
        [JsonPropertyName("_id")]
        public string sIdMongo { get; set; } = string.Empty;
        
        [JsonPropertyName("sNombre")]
        public string sNombre { get; set; } = string.Empty;
        
        [JsonPropertyName("iCostoReal")]
        public decimal iCostoReal { get; set; }
        
        [JsonPropertyName("iCostoPublico")]
        public decimal iCostoPublico { get; set; }
        
        [Ignore]
        [JsonPropertyName("imagenes")]
        public List<Imagen> Imagenes { get; set; } = new();
        
        [JsonPropertyName("bActivo")]
        public bool bActivo { get; set; } = true;
        
        // Propiedad calculada para mostrar precio formateado
        public string sPrecioFormateado => $"$ {iCostoPublico:N2} MXN";
        
        // Primera imagen o vacío
        public string sURLImagen => Imagenes?.FirstOrDefault()?.sURLImagen ?? string.Empty;
    }
}

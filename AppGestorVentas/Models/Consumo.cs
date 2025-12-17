using SQLite;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    /// <summary>
    /// Representa un consumo individual de un producto (ej: cada enchilada de una orden de 4)
    /// </summary>
    [Table("tb_Consumo")]
    public class Consumo
    {
        [PrimaryKey, AutoIncrement]
        public int iIdConsumo { get; set; }
        
        [JsonPropertyName("_id")]
        public string sIdMongo { get; set; } = string.Empty;
        
        public string sIdOrdenProducto { get; set; } = string.Empty; // FK a OrdenProducto
        
        [JsonPropertyName("iIndex")]
        public int iIndex { get; set; } // Índice del consumo (1, 2, 3, 4...)
        
        [Ignore]
        [JsonPropertyName("aExtras")]
        public List<ExtraConsumo> aExtras { get; set; } = new();
        
        // Calculado localmente
        public decimal iTotalExtrasConsumo => aExtras?.Sum(e => e.iCostoPublico) ?? 0;
    }
    
    /// <summary>
    /// Representa un extra aplicado a un consumo específico
    /// </summary>
    [Table("tb_ExtraConsumo")]
    public class ExtraConsumo
    {
        [PrimaryKey, AutoIncrement]
        public int iIdExtraConsumo { get; set; }
        
        public int iIdConsumo { get; set; } // FK a Consumo
        
        [JsonPropertyName("_id")]
        public string sIdMongo { get; set; } = string.Empty;
        
        public string sIdExtra { get; set; } = string.Empty; // ID del extra del catálogo
        
        [JsonPropertyName("sNombre")]
        public string sNombre { get; set; } = string.Empty;
        
        [JsonPropertyName("iCostoReal")]
        public decimal iCostoReal { get; set; }
        
        [JsonPropertyName("iCostoPublico")]
        public decimal iCostoPublico { get; set; }
        
        [JsonPropertyName("sURLImagen")]
        public string sURLImagen { get; set; } = string.Empty;
    }
}

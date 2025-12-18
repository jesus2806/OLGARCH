using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    [Table("tb_OrdenProducto")]
    public partial class OrdenProducto : ObservableObject
    {
        // Clave primaria local (SQLite), opcional si usas persistencia local
        [PrimaryKey, AutoIncrement]
        public string siIdOrdenProducto { get; set; }

        [JsonPropertyName("_id")]
        public string sIdMongo { get; set; } = string.Empty;
        public string sIdOrdenMongoDB { get; set; } = string.Empty;
        public string sNombre { get; set; } = string.Empty;
        public decimal iCostoReal { get; set; }
        public decimal iCostoPublico { get; set; }
        public string sURLImagen { get; set; } = string.Empty;
        public string sIndicaciones { get; set; } = string.Empty;
        public int iIndexVarianteSeleccionada { get; set; }

        [JsonPropertyName("aVariantes")]
        [Ignore]
        public List<Variante> aVariantes { get; set; } = new();

        [Ignore]
        public List<ExtraOrdenProducto> aExtras { get; set; } = [];
        
        // Nueva propiedad para consumos individuales (Pantalla 2)
        [Ignore]
        [JsonPropertyName("aConsumos")]
        public List<Consumo> aConsumos { get; set; } = [];
        
        // Cantidad del producto
        [JsonPropertyName("iCantidad")]
        public int iCantidad { get; set; } = 1;
        
        // Indica si el producto tiene extras asociados
        [JsonPropertyName("bTieneExtras")]
        public bool bTieneExtras { get; set; } = false;

        public decimal iTotalRealExtrasOrden {  get; set; }
        public decimal iTotalPublicoExtrasOrden {  get; set; }
        public decimal iTotalGeneralRealOrdenProducto { get; set; }
        public decimal iTotalGeneralPublicoOrdenProducto { get; set; }
        public int iTipoProducto { get; set; }

        [ObservableProperty]
        private bool isExpanded;
    }

    [Table("tb_ExtraOrdenProducto")]
    public class ExtraOrdenProducto
    {
        [PrimaryKey, AutoIncrement]
        public int iIdExtraOrdenProducto { get; set; }
        public int iIdMongoOrdenProducto { get; set; }
        [JsonPropertyName("_id")]
        public string sIdExtra { get; set; } = string.Empty;
        public string sNombre { get; set; } = string.Empty;
        public decimal iCostoReal { get; set; } 
        public decimal iCostoPublico { get; set; }
        public string sURLImagen { get; set; } = string.Empty;

    }

}

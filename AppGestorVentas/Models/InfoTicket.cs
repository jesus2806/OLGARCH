using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    public class InfoTicket
    {
        [JsonPropertyName("mesa")]
        public int iMesa { get; set; }
        [JsonPropertyName("mesero")]
        public string sMesero { get; set; }
        [JsonPropertyName("fechaAlta")]
        public DateTime dFechaAlta { get; set; }
        [JsonPropertyName("totalPublico")]
        public decimal dTotalPublico { get; set; }
        [JsonPropertyName("elementos")]
        public List<ProductosInfoTicket> Productos { get; set; }
    }

    public class ProductosInfoTicket
    {
        [JsonPropertyName("nombre")]
        public string sNombre { get; set; }
        [JsonPropertyName("costoPublico")]
        public decimal iCostoPublico { get; set; }
        [JsonPropertyName("cantidad")]
        public int iCantidad { get; set; }
        [JsonPropertyName("esExtra")]
        public bool bEsExtra { get; set; }
    }

}

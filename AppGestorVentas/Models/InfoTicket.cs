using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    public class InfoTicket
    {
        [JsonPropertyName("iMesa")]
        public int iMesa { get; set; }

        [JsonPropertyName("dTotalPublico")]
        public decimal dTotalPublico { get; set; }

        // OJO: tu backend manda "Productos"
        [JsonPropertyName("Productos")]
        public List<ProductosInfoTicket> Productos { get; set; } = new();
    }

    public class ProductosInfoTicket
    {
        [JsonPropertyName("sNombre")]
        public string sNombre { get; set; } = string.Empty;

        [JsonPropertyName("iCostoPublico")]
        public decimal iCostoPublico { get; set; }

        [JsonPropertyName("iCantidad")]
        public int iCantidad { get; set; }

        [JsonPropertyName("bEsExtra")]
        public bool bEsExtra { get; set; }
    }
}

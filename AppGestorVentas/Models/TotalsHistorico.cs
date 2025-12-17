using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    // Modelo para mapear la sección de totales
    public class TotalsHistorico
    {
        // Este campo puede ser null, de acuerdo a la respuesta del endpoint
        [JsonPropertyName("_id")]
        public string Id { get; set; } = null;

        [JsonPropertyName("totalCostoPublico")]
        public decimal TotalCostoPublico { get; set; }

        [JsonPropertyName("totalCostoReal")]
        public decimal TotalCostoReal { get; set; }

        [JsonPropertyName("totalEfectivo")]
        public decimal TotalEfectivo { get; set; }

        [JsonPropertyName("totalBanco")]
        public decimal TotalBanco { get; set; }
    }
}

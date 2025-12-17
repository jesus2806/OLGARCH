using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    // Modelo para mapear la respuesta completa del endpoint
    public class Historico
    {
        [JsonPropertyName("totals")]
        public TotalsHistorico Totals { get; set; }

        [JsonPropertyName("registros")]
        public List<RegistroHistorico> Registros { get; set; }
    }
}

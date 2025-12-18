using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    public class Consumo
    {
        [JsonPropertyName("sIdLocal")]
        public string sIdLocal { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("_id")]
        public string sIdMongo { get; set; } = string.Empty;

        [JsonPropertyName("sIdOrdenProductoLocal")]
        public string sIdOrdenProductoLocal { get; set; } = string.Empty;

        [JsonPropertyName("sIdOrdenProductoMongo")]
        public string sIdOrdenProductoMongo { get; set; } = string.Empty;

        [JsonPropertyName("iIndex")]
        public int iIndex { get; set; }

        [JsonPropertyName("aExtras")]
        public List<ExtraConsumo> aExtras { get; set; } = new List<ExtraConsumo>();

        [JsonIgnore]
        public decimal iTotalExtrasConsumo => aExtras?.Sum(e => e.iCostoPublico) ?? 0;
    }

    public class ExtraConsumo
    {
        [JsonPropertyName("sIdLocal")]
        public string sIdLocal { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("sIdConsumoLocal")]
        public string sIdConsumoLocal { get; set; } = string.Empty;

        [JsonPropertyName("_id")]
        public string sIdMongo { get; set; } = string.Empty;

        [JsonPropertyName("sIdExtra")]
        public string sIdExtra { get; set; } = string.Empty;

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
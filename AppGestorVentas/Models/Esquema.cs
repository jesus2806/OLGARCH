using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    public class Esquema
    {
        [JsonPropertyName("_id")]
        public string sIdMongo { get; set; } = string.Empty;

        [JsonPropertyName("sNombre")]
        public string sNombre { get; set; } = string.Empty;

        [JsonPropertyName("aDia")]
        public ObservableCollection<DiaEsquema> aDia { get; set; } = new();
    }
}

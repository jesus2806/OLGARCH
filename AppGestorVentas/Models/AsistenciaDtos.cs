using System.Text.Json.Serialization;

namespace AppGestorVentas.Models.Asistencia
{
    public class AsistenciaRosterDto
    {
        [JsonPropertyName("sDia")]
        public string sDia { get; set; } = "";

        [JsonPropertyName("roster")]
        public List<RosterItemDto> roster { get; set; } = new();
    }

    public class RosterItemDto
    {
        [JsonPropertyName("usuario")]
        public UsuarioDto usuario { get; set; } = new();

        [JsonPropertyName("asistencia")]
        public AsistenciaDto asistencia { get; set; } = new();
    }

    public class UsuarioDto
    {
        [JsonPropertyName("_id")]
        public string _id { get; set; } = "";

        [JsonPropertyName("sNombre")]
        public string sNombre { get; set; } = "";

        [JsonPropertyName("sApellidoPaterno")]
        public string sApellidoPaterno { get; set; } = "";

        [JsonPropertyName("sApellidoMaterno")]
        public string sApellidoMaterno { get; set; } = "";

        [JsonPropertyName("sUsuario")]
        public string sUsuario { get; set; } = "";

        [JsonIgnore]
        public string NombreCompleto => $"{sNombre} {sApellidoPaterno} {sApellidoMaterno}".Trim();
    }

    public class AsistenciaDto
    {
        [JsonPropertyName("sEstatus")]
        public string sEstatus { get; set; } = "sin_marcar";

        [JsonPropertyName("sNotas")]
        public string sNotas { get; set; } = "";
    }

    public class AsistenciaBulkRequest
    {
        [JsonPropertyName("sDia")]
        public string sDia { get; set; } = "";

        [JsonPropertyName("items")]
        public List<AsistenciaBulkItem> items { get; set; } = new();
    }

    public class AsistenciaBulkItem
    {
        [JsonPropertyName("oUsuario")]
        public string oUsuario { get; set; } = "";

        [JsonPropertyName("sEstatus")]
        public string sEstatus { get; set; } = "sin_marcar";

        [JsonPropertyName("sNotas")]
        public string sNotas { get; set; } = "";
    }
}

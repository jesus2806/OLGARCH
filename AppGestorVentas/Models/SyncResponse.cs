using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    /// <summary>
    /// Respuesta del backend para la sincronización
    /// </summary>
    public class SyncResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public SyncResponseData? Data { get; set; }
    }

    /// <summary>
    /// Datos de la respuesta de sincronización
    /// </summary>
    public class SyncResponseData
    {
        [JsonPropertyName("syncLogId")]
        public string SyncLogId { get; set; } = string.Empty;

        [JsonPropertyName("resumen")]
        public SyncResumen? Resumen { get; set; }

        [JsonPropertyName("estadoGeneral")]
        public string EstadoGeneral { get; set; } = string.Empty;

        [JsonPropertyName("resultados")]
        public List<SyncResultadoOperacion> Resultados { get; set; } = new();

        [JsonPropertyName("idMapping")]
        public Dictionary<string, string> IdMapping { get; set; } = new();
    }

    /// <summary>
    /// Resumen de la sincronización
    /// </summary>
    public class SyncResumen
    {
        [JsonPropertyName("totalOperaciones")]
        public int TotalOperaciones { get; set; }

        [JsonPropertyName("exitosas")]
        public int Exitosas { get; set; }

        [JsonPropertyName("fallidas")]
        public int Fallidas { get; set; }
    }

    /// <summary>
    /// Resultado de una operación individual
    /// </summary>
    public class SyncResultadoOperacion
    {
        [JsonPropertyName("idLocal")]
        public string IdLocal { get; set; } = string.Empty;

        [JsonPropertyName("tipoOperacion")]
        public string TipoOperacion { get; set; } = string.Empty;

        [JsonPropertyName("resultado")]
        public string Resultado { get; set; } = string.Empty;

        [JsonPropertyName("idMongoDB")]
        public string? IdMongoDB { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("datos")]
        public object? Datos { get; set; }
    }

    /// <summary>
    /// Respuesta del estado del servicio de sincronización
    /// </summary>
    public class SyncStatusResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public SyncStatusData? Data { get; set; }
    }

    /// <summary>
    /// Datos del estado del servicio
    /// </summary>
    public class SyncStatusData
    {
        [JsonPropertyName("servicioActivo")]
        public bool ServicioActivo { get; set; }

        [JsonPropertyName("mongoDBStatus")]
        public string MongoDBStatus { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }
}

using SQLite;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    /// <summary>
    /// Tipos de operaciones soportadas por el sistema de sincronización
    /// </summary>
    public enum TipoOperacionSync
    {
        CREAR_ORDEN,
        ACTUALIZAR_ORDEN,
        ELIMINAR_ORDEN,
        ACTUALIZAR_INDICACIONES_ORDEN,
        CREAR_PRODUCTO,
        ACTUALIZAR_PRODUCTO,
        ELIMINAR_PRODUCTO,
        ACTUALIZAR_CANTIDAD_PRODUCTO,
        AGREGAR_EXTRA_CONSUMOS,
        ELIMINAR_EXTRA_CONSUMO,
        ELIMINAR_CONSUMO
    }

    /// <summary>
    /// Estado de una operación de sincronización
    /// </summary>
    public enum EstadoOperacionSync
    {
        PENDIENTE,
        SINCRONIZANDO,
        EXITOSO,
        ERROR
    }

    /// <summary>
    /// Representa una operación pendiente de sincronización almacenada en SQLite
    /// </summary>
    [Table("tb_SyncOperation")]
    public class SyncOperation
    {
        [PrimaryKey]
        public string sIdLocal { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Tipo de operación a realizar
        /// </summary>
        public string sTipoOperacion { get; set; } = string.Empty;

        /// <summary>
        /// ID local de la entidad afectada (puede ser orden o producto)
        /// </summary>
        public string sIdEntidadLocal { get; set; } = string.Empty;

        /// <summary>
        /// ID de MongoDB si ya fue sincronizado previamente
        /// </summary>
        public string sIdEntidadMongoDB { get; set; } = string.Empty;

        /// <summary>
        /// Datos de la operación serializados como JSON
        /// </summary>
        public string sDatosJson { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp de cuando se creó la operación (para ordenamiento)
        /// </summary>
        public DateTime dtTimestampLocal { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Estado actual de la operación
        /// </summary>
        public string sEstado { get; set; } = EstadoOperacionSync.PENDIENTE.ToString();

        /// <summary>
        /// Mensaje de error si la operación falló
        /// </summary>
        public string sErrorMensaje { get; set; } = string.Empty;

        /// <summary>
        /// Número de intentos de sincronización
        /// </summary>
        public int iIntentos { get; set; } = 0;

        /// <summary>
        /// Orden de ejecución (para operaciones que dependen de otras)
        /// </summary>
        public int iOrdenEjecucion { get; set; } = 0;

        // Propiedades de conveniencia (no almacenadas)
        [Ignore]
        public TipoOperacionSync TipoOperacion
        {
            get => Enum.TryParse<TipoOperacionSync>(sTipoOperacion, out var tipo) ? tipo : TipoOperacionSync.CREAR_ORDEN;
            set => sTipoOperacion = value.ToString();
        }

        [Ignore]
        public EstadoOperacionSync Estado
        {
            get => Enum.TryParse<EstadoOperacionSync>(sEstado, out var estado) ? estado : EstadoOperacionSync.PENDIENTE;
            set => sEstado = value.ToString();
        }

        /// <summary>
        /// Deserializa los datos JSON al tipo especificado
        /// </summary>
        public T? ObtenerDatos<T>() where T : class
        {
            if (string.IsNullOrEmpty(sDatosJson)) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(sDatosJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Serializa los datos al formato JSON
        /// </summary>
        public void EstablecerDatos<T>(T datos) where T : class
        {
            sDatosJson = JsonSerializer.Serialize(datos, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }

    /// <summary>
    /// Modelo para enviar al backend en la sincronización
    /// </summary>
    public class SyncOperationRequest
    {
        [JsonPropertyName("tipoOperacion")]
        public string TipoOperacion { get; set; } = string.Empty;

        [JsonPropertyName("idLocal")]
        public string IdLocal { get; set; } = string.Empty;

        [JsonPropertyName("datos")]
        public object? Datos { get; set; }

        [JsonPropertyName("timestampLocal")]
        public DateTime TimestampLocal { get; set; }
    }

    /// <summary>
    /// Payload completo de sincronización
    /// </summary>
    public class SyncPayload
    {
        [JsonPropertyName("operaciones")]
        public List<SyncOperationRequest> Operaciones { get; set; } = new();
    }
}

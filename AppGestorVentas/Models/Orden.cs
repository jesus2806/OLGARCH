using SQLite;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    [Table("tb_Orden")]
    public class Orden
    {
        /// <summary>
        /// ID local único (UUID) para sincronización offline-first
        /// </summary>
        [PrimaryKey]
        public string sIdLocal { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// ID de MongoDB (se asigna después de sincronizar)
        /// </summary>
        [JsonPropertyName("_id")]
        public string sIdMongoDB { get; set; } = string.Empty;

        /// <summary>
        /// Indica si la orden ha sido sincronizada con el backend
        /// </summary>
        public bool bSincronizado { get; set; } = false;

        /// <summary>
        /// Indica si hay cambios pendientes de sincronizar
        /// </summary>
        public bool bTieneCambiosPendientes { get; set; } = false;

        public string sIdentificadorOrden { get; set; } = string.Empty;
        public int iMesa { get; set; }
        public int iTipoOrden { get; set; }
        public int iNumeroOrden { get; set; }
        public string sUsuarioMesero { get; set; } = string.Empty;
        public string sIdMongoDBMesero { get; set; } = string.Empty;
        
        [Ignore]
        public List<OrdenProducto> aProductos { get; set; } = new();
        
        public int iEstatus { get; set; } = 0; // 0 = dada de alta, 1 = tomada, 2 = en preparación, etc.
        public int iTipoPago { get; set; }  // 1 = Efectivo, 2 = Transferencia
        public DateTime dtFechaAlta { get; set; }
        
        [Ignore]
        public DateTime DtFechaAltaMexico
        {
            get
            {
                if (dtFechaAlta == default)
                    return default;

                TimeZoneInfo tzMexico = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
                return TimeZoneInfo.ConvertTimeFromUtc(dtFechaAlta, tzMexico);
            }
        }
        
        public DateTime? dtFechaFin { get; set; }
        public decimal iTotalOrden { get; set; } = 0;
        
        [Ignore]
        public decimal iTotalOrdenCostoReal { get; set; } = 0;
        
        public decimal iTotalExtrasOrden { get; set; } = 0;
        public bool bOrdenModificada { get; set; } = false;
        public string sIndicaciones { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene el ID efectivo (MongoDB si existe, sino el local)
        /// </summary>
        [Ignore]
        public string IdEfectivo => !string.IsNullOrEmpty(sIdMongoDB) ? sIdMongoDB : sIdLocal;
    }
}

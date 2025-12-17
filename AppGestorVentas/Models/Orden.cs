using SQLite;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    [Table("tb_Orden")]
    public class Orden
    {
        [JsonPropertyName("_id")]
        public string sIdMongoDB { get; set; } = string.Empty;
        public string sIdentificadorOrden { get; set; }
        public int iMesa { get; set; }
        public int iTipoOrden { get; set; }
        public int iNumeroOrden { get; set; }
        public string sUsuarioMesero { get; set; } = string.Empty;
        public string sIdMongoDBMesero { get; set; } = string.Empty;
        [Ignore]
        public List<OrdenProducto> aProductos { get; set; } = new();  // Lista de ObjectIds en forma de string
        public int iEstatus { get; set; } = 0; // 0 = dada de alta, 1 = tomada, 2 = en preparación, etc.
        public int iTipoPago { get; set; }  // 1 = Efectivo, 2 = Transferencia
        public DateTime dtFechaAlta { get; set; }
        public DateTime DtFechaAltaMexico
        {
            get
            {
                if (dtFechaAlta == default)
                    return default;

                // "America/Mexico_City" es reconocido en la mayoría de sistemas basados en IANA.
                // En Windows, tal vez necesites "Central Standard Time (Mexico)" según la versión.
                TimeZoneInfo tzMexico = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

                // Convertir desde UTC a la zona horaria de México:
                return TimeZoneInfo.ConvertTimeFromUtc(dtFechaAlta, tzMexico);
            }
        }
        public DateTime? dtFechaFin { get; set; }
        public decimal iTotalOrden { get; set; } = 0;
        [Ignore]
        public decimal iTotalOrdenCostoReal { get; set; } = 0;
        public decimal iTotalExtrasOrden { get; set; } = 0;
        public bool bOrdenModificada { get; set; } = false;
        public string sIndicaciones { get; set; } = string.Empty; // Indicaciones generales de la orden (Pantalla 6)
    }
}

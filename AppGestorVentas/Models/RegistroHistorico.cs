// Modelo para cada registro del histórico (ya existente)
using SQLite;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    [Table("tb_Historico")]
    public class RegistroHistorico
    {
        [PrimaryKey, AutoIncrement]
        public int iIdHistorico { get; set; }

        [JsonPropertyName("_id")]
        public string sIdMongo { get; set; } = string.Empty;

        [JsonPropertyName("sIdOrdenPrimaria")]
        public string sIdMongoOrdenPrimaria { get; set; } = string.Empty;

        [JsonPropertyName("sIdentificadorOrdenPrimaria")]
        public string sIdentificadorOrdenPrimaria { get; set; } = string.Empty;

        [JsonPropertyName("iMesa")]
        public int iMesa { get; set; }

        [JsonPropertyName("iNumeroOrden")]
        public int iNumeroOrden { get; set; }

        [JsonPropertyName("sUsuarioMesero")]
        public string sUsuarioMesero { get; set; } = string.Empty;

        [JsonPropertyName("dtFechaAlta")]
        public DateTime dtFechaAlta { get; set; } = DateTime.Now;
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

        [JsonPropertyName("dtFechaFin")]
        public DateTime dtFechaFin { get; set; } = DateTime.Now;
        public DateTime DtFechaFinMexico
        {
            get
            {
                if (dtFechaFin == default)
                    return default;

                // "America/Mexico_City" es reconocido en la mayoría de sistemas basados en IANA.
                // En Windows, tal vez necesites "Central Standard Time (Mexico)" según la versión.
                TimeZoneInfo tzMexico = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

                // Convertir desde UTC a la zona horaria de México:
                return TimeZoneInfo.ConvertTimeFromUtc(dtFechaFin, tzMexico);
            }
        }

        [JsonPropertyName("iTotalCostoPublico")]
        public decimal iTotalCostoPublico { get; set; } = 0;

        [JsonPropertyName("iTotalCostoReal")]
        public decimal iTotalCostoReal { get; set; } = 0;

        [JsonPropertyName("iTipoPago")]
        public int iTipoPago { get; set; } = 0;

        [JsonPropertyName("iEstatus")]
        public int iEstatus { get; set; } = 5;
    }
}






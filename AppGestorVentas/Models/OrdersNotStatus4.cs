using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AppGestorVentas.Models
{
    public class OrdersNotStatus4
    {
        [JsonPropertyName("ordenNumber")]
        public int iOrdenNumber { get; set; }

        [JsonPropertyName("type")]
        public string sTipo { get; set; }

        [JsonPropertyName("status")]
        public int iEstatus {get; set;}

    }
}

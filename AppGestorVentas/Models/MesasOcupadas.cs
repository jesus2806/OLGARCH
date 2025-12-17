using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AppGestorVentas.Models
{
    public class MesasOcupadas
    {
        [JsonPropertyName("data")]
        public List<int> lstMesasOcupadas { get; set; }
    }
}

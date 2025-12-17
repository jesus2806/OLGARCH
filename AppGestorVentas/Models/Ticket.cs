using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppGestorVentas.Models
{
    // Modelo para el ticket completo
    public class Ticket
    {
        public string sEncabezado { get; set; }
        public int iMesa { get; set; }
        public DateTime dFechaActual { get; set; } = DateTime.Now;
        public List<TicketItem> Items { get; set; } = new List<TicketItem>();
        public decimal iTotal { get; set; }
        public string sPie { get; set; }
    }
}

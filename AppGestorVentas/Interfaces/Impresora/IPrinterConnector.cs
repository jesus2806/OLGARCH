using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppGestorVentas.Interfaces.Impresora
{
    // Interfaz para abstraer el envío de datos a la impresora
    public interface IPrinterConnector
    {
        Task<bool> ConectarImpresoraAsync(string nombreImpresora);
        Task EnviarAsync(byte[] datos);
        Task DesconectarAsync();
    }
}

using AppGestorVentas.Extensions;
using AppGestorVentas.Interfaces.Impresora;
using AppGestorVentas.Models;
using ESCPOS_NET.Emitters;

namespace AppGestorVentas.Services
{
    public class EscPosPrinterService
    {
        private readonly IPrinterConnector _printerConnector;
        private readonly ICommandEmitter _emitter;

        public EscPosPrinterService(IPrinterConnector printerConnector)
        {
            _printerConnector = printerConnector;
            _emitter = new EPSON();
        }

        // Método principal para imprimir un ticket
        public async Task<bool> ImprimirTicketAsync(Ticket ticket)
        {
            try
            {
                // Construir la secuencia de comandos ESC/POS
                var comandos = ConstruirComandosTicket(ticket);
                byte[] bytes = comandos.ToArray();

                // Enviar los comandos a la impresora
                await _printerConnector.EnviarAsync(bytes);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al imprimir ticket: " + ex.Message);
                return false;
            }
        }

        private List<byte> ConstruirComandosTicket(Ticket ticket)
        {
            var comandos = new List<byte>();
            comandos.AddRange(_emitter.Initialize());
            comandos.AddRange(new byte[] { 0x1B, 0x74, 16 }); // CP1252

            const int W = 40;

            if (ticket.LogoBytes != null && ticket.LogoBytes.Length > 0)
            {
                comandos.AddRange(_emitter.CenterAlign());

                // PrintImage recibe bytes del archivo (PNG/JPG)
                comandos.AddRange(_emitter.PrintImage(ticket.LogoBytes, true));
                comandos.AddRange(_emitter.PrintAndFeedLines(1));
            }

            // ==== ENCABEZADO (centrado) ====
            comandos.AddRange(_emitter.CenterAlign());
            comandos.AddRange(_emitter.PrintLine(ticket.sEncabezado));
            comandos.AddRange(_emitter.PrintAndFeedLines(1));
            comandos.AddRange(_emitter.PrintLine($"No. Mesa: {ticket.iMesa}"));
            comandos.AddRange(_emitter.PrintLine($"Fecha y hora: {ticket.dFechaActual:dd/MM/yyyy HH:mm:ss}"));
            comandos.AddRange(_emitter.PrintLine("Tel: +52 418 122 0998"));

            // Separador (centrado para que no se “recorra”)
            comandos.AddRange(_emitter.CenterAlign());
            comandos.AddRange(_emitter.PrintLine(new string('-', W)));

            // ==== TABLA (centrada) ====
            // W=40 => 4 + 1 + 16 + 1 + 9 + 1 + 8 = 40
            const int QTY_W = 4;
            const int DESC_W = 16;
            const int UNIT_W = 9;
            const int SUB_W = 8;

            var header = string.Format(
                "{0,-4} {1,-16} {2,9} {3,8}",
                "Cant", "Descripción", "P. Unidad", "Subtot"
            );
            comandos.AddRange(_emitter.PrintLine(header));

            foreach (var item in ticket.Items)
            {
                var cantidadStr = (item.Cantidad <= 0 ? 1 : item.Cantidad).ToString();
                var desc = (item.Descripcion ?? "").Trim(); // ✅ sin espacios al inicio

                var unit = FitRight(item.PrecioUnitario.ToString("C"), UNIT_W);
                var subtotal = FitRight((item.PrecioUnitario * item.Cantidad).ToString("C"), SUB_W);

                var descLines = WrapText(desc, DESC_W);

                for (int i = 0; i < descLines.Count; i++)
                {
                    if (i == 0)
                    {
                        var line = string.Format(
                            "{0,-4} {1,-16} {2,9} {3,8}",
                            FitLeft(cantidadStr, QTY_W),
                            FitLeft(descLines[i], DESC_W),
                            unit,
                            subtotal
                        );
                        comandos.AddRange(_emitter.PrintLine(line));
                    }
                    else
                    {
                        var line = string.Format(
                            "{0,-4} {1,-16} {2,9} {3,8}",
                            "",
                            FitLeft(descLines[i], DESC_W),
                            "",
                            ""
                        );
                        comandos.AddRange(_emitter.PrintLine(line));
                    }
                }
            }

            // Separador inferior (centrado e igual al superior)
            comandos.AddRange(_emitter.PrintLine(new string('-', W)));

            // ==== TOTAL ====
            comandos.AddRange(_emitter.RightAlign());
            comandos.AddRange(_emitter.PrintLine("TOTAL: " + ticket.iTotal.ToString("C")));

            // Pie centrado
            comandos.AddRange(_emitter.CenterAlign());
            if (!string.IsNullOrEmpty(ticket.sPie))
                comandos.AddRange(_emitter.PrintLine(ticket.sPie));

            comandos.AddRange(_emitter.PrintAndFeedLines(3));
            comandos.AddRange(_emitter.CutPaper());
            return comandos;
        }

        private static string FitLeft(string s, int w)
        {
            s ??= "";
            if (s.Length > w) return s.Substring(0, w);
            return s.PadRight(w);
        }

        private static string FitRight(string s, int w)
        {
            s ??= "";
            if (s.Length > w) return s.Substring(s.Length - w, w);
            return s.PadLeft(w);
        }


        // Método principal para imprimir el corte
        public async Task<bool> ImprimirCorteAsync(Corte oCorte)
        {
            try
            {
                // Construir la secuencia de comandos ESC/POS
                var comandos = ConstruirComandosCorte(oCorte);
                byte[] bytes = comandos.ToArray();

                // Enviar los comandos a la impresora
                await _printerConnector.EnviarAsync(bytes);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al imprimir corte: " + ex.Message);
                return false;
            }
        }

        private List<byte> ConstruirComandosCorte(Corte oCorte)
        {
            var comandos = new List<byte>();

            // 1) Inicializar impresora y code page
            comandos.AddRange(_emitter.Initialize());
            comandos.AddRange(new byte[] { 0x1B, 0x74, 16 }); // Selección de code page, p.ej. CP1252

            // 2) Encabezado centrado
            comandos.AddRange(_emitter.CenterAlign());
            comandos.AddRange(_emitter.PrintLine("Resumen Corte"));
            comandos.AddRange(_emitter.PrintLine(new string('-', 40)));

            // 3) Cuerpo alineado a la izquierda
            comandos.AddRange(_emitter.LeftAlign());

            // Para imprimir las líneas de forma más limpia
            var lineas = new[]
            {
                $"Fecha corte: {oCorte.dFechaCorte:dd/MM/yyyy}",
                $"",
                $"Ingresos totales: {oCorte.dTotalCostoPublico:C2} MXM",
                $"Costo de ventas:  {oCorte.dTotalCostoReal:C2} MXM",
                $"Total ganancia:   {oCorte.dTotalGanancia:C2} MXM",
                $"Total fondos en efectivo:      {oCorte.dTotalEfectivo:C2} MXM",
                $"Total fondos en transferencia: {oCorte.dTotalTransferencia:C2} MXM"
            };

            foreach (var linea in lineas)
            {
                comandos.AddRange(_emitter.PrintLine(linea));
            }

            comandos.AddRange(_emitter.PrintAndFeedLines(4));
            // 4) Corte de papel
            comandos.AddRange(_emitter.CutPaper());

            return comandos;
        }



        /// <summary>
        /// Recibe un string largo y lo corta en múltiples líneas de longitud máxima `maxLength`.
        /// </summary>
        private List<string> WrapText(string text, int maxLength)
        {
            var lines = new List<string>();

            if (string.IsNullOrEmpty(text))
            {
                lines.Add("");
                return lines;
            }

            int startIndex = 0;
            while (startIndex < text.Length)
            {
                // Tomar el tramo que quepa en maxLength
                int length = Math.Min(maxLength, text.Length - startIndex);
                lines.Add(text.Substring(startIndex, length));
                startIndex += length;
            }

            return lines;
        }

    }
}

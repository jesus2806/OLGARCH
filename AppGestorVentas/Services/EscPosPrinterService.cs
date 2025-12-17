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

            // Inicializar la impresora
            comandos.AddRange(_emitter.Initialize());

            // 2) Seleccionar la code page (ej. CP1252)
            comandos.AddRange(new byte[] { 0x1B, 0x74, 16 });

            // === Encabezado centrado ===
            comandos.AddRange(_emitter.CenterAlign());
            comandos.AddRange(_emitter.PrintLine(ticket.sEncabezado));
            comandos.AddRange(_emitter.PrintAndFeedLines(1));
            comandos.AddRange(_emitter.PrintLine($"No. Mesa: {ticket.iMesa}"));
            comandos.AddRange(_emitter.PrintLine($"Fecha y hora: {ticket.dFechaActual:dd/MM/yyyy HH:mm:ss}"));
            comandos.AddRange(_emitter.PrintLine("Tel: +52 418 122 0998"));
            comandos.AddRange(_emitter.PrintLine(new string('-', 40)));

            // === Encabezados de columna ===
            //  Vamos a definir un formato que ocupe el ancho de 40 caracteres, 
            //  con columnas para Cantidad (4), Descripción (~20), PrecioUnit (7), Subtotal (8).
            //  Ajusta los valores según el ancho real de tu impresora y tus necesidades.
            //  El separador mínimo entre columnas suele ser 1 espacio.

            // Modo justificado a la izquierda para imprimir nuestras columnas
            //comandos.AddRange(_emitter.LeftAlign());

            comandos.AddRange(_emitter.CenterAlign());

            // Encabezado de columnas. Ejemplo:
            // "Cant  Descripción           P.U.   Subtotal"
            comandos.AddRange(_emitter.PrintLine("Cant  Descripción           P.Unit   Subtot"));

            // === Impresión de los items (productos) ===
            foreach (var item in ticket.Items)
            {
                // Cálculo de subtotal
                decimal subtotal = item.PrecioUnitario * item.Cantidad;
                string cantidadStr = item.Cantidad.ToString();
                string precioStr = item.PrecioUnitario.ToString("C");
                string subTotStr = subtotal.ToString("C");

                // Definimos una anchura máxima para la descripción del producto.
                // Por ejemplo, 20 caracteres. (O el valor que te convenga.)
                int descMaxLength = 20;

                // Podríamos dividir el nombre en múltiples líneas si excede descMaxLength
                // para no truncarlo abruptamente.
                List<string> lineasDescripcion = WrapText(item.Descripcion, descMaxLength);

                // Vamos a imprimir la primera línea mostrando Cantidad, Descripción (o parte de ella),
                // Precio Unitario y Subtotal. 
                // Si la descripción requiere más líneas, se imprimirán en bucle sin la cantidad repetida.
                for (int i = 0; i < lineasDescripcion.Count; i++)
                {
                    // En la primera línea se muestra cantidad, precio y subtotal
                    // En las subsecuentes, esos campos se dejan en blanco (o se omiten).
                    if (i == 0)
                    {
                        // Creamos la línea usando string.Format con anchos fijos
                        // Notación: {0,-4} => Columna 1 (Cantidad) de 4 chars, justificado izq.
                        //            {1,-20} => Columna 2 (Descripción) de 20 chars, just. izq.
                        //            {2,7} => Columna 3 (P.Unit) de 7 chars, justificado der.
                        //            {3,8} => Columna 4 (Subtotal) de 8 chars, justificado der.
                        string linea = string.Format(
                            "{0,-4} {1,-20} {2,7} {3,8}",
                            cantidadStr,
                            lineasDescripcion[i],
                            precioStr,
                            subTotStr
                        );
                        comandos.AddRange(_emitter.PrintLine(linea));
                    }
                    else
                    {
                        // Subsecuentes líneas de descripción (sin repetir cantidad/precio/subtotal)
                        string linea = string.Format(
                            "{0,-4} {1,-20} {2,7} {3,8}",
                            "", // vacío para no repetir cantidad
                            lineasDescripcion[i],
                            "", // vacío para no repetir precio
                            ""  // vacío para no repetir subtotal
                        );
                        comandos.AddRange(_emitter.PrintLine(linea));
                    }
                }
            }

            
            // Línea separadora
            comandos.AddRange(_emitter.PrintLine(new string('-', 40)));

            // === Total a la derecha ===
            comandos.AddRange(_emitter.RightAlign());
            comandos.AddRange(_emitter.PrintLine("TOTAL: " + ticket.iTotal.ToString("C")));

            // Regresar a la izquierda
            comandos.AddRange(_emitter.LeftAlign());

            // === Pie de ticket (mensaje final) ===
            if (!string.IsNullOrEmpty(ticket.sPie))
            {
                comandos.AddRange(_emitter.PrintLine(ticket.sPie));
            }


            // Alimentar y cortar
            comandos.AddRange(_emitter.PrintAndFeedLines(3));
            comandos.AddRange(_emitter.CutPaper());

            return comandos;
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

            comandos.AddRange(_emitter.PrintAndFeedLines(2));
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

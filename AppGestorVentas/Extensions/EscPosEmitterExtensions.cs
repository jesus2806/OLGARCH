using ESCPOS_NET.Emitters;

namespace AppGestorVentas.Extensions
{
    public static class EscPosEmitterExtensions
    {
        /// <summary>
        /// Envía comandos para alimentar un número específico de líneas.
        /// </summary>
        /// <param name="emitter">Instancia de ICommandEmitter</param>
        /// <param name="lines">Cantidad de líneas a alimentar</param>
        /// <returns>Secuencia de bytes para alimentar líneas</returns>
        public static IEnumerable<byte> PrintAndFeedLines(this ICommandEmitter emitter, int lines)
        {
            var comandos = new List<byte>();
            for (int i = 0; i < lines; i++)
            {
                // Se usa el carácter de salto de línea (LF, 0x0A)
                comandos.Add(0x0A);
            }
            return comandos;
        }

        /// <summary>
        /// Envía el comando para cortar el papel.
        /// </summary>
        /// <param name="emitter">Instancia de ICommandEmitter</param>
        /// <returns>Secuencia de bytes para cortar el papel</returns>
        public static IEnumerable<byte> CutPaper(this ICommandEmitter emitter)
        {
            // Comando para corte completo: GS V 66 0 (0x1D, 0x56, 0x42, 0x00)
            return new byte[] { 0x1D, 0x56, 0x42, 0x00 };
        }
    }
}

using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppGestorVentas.Services
{
    /// <summary>
    /// Contiene constantes y configuraciones para la base de datos local en la aplicación.
    /// </summary>
    public static class Constants
    {
        #region PROPIEDADES

        /// <summary>
        /// Nombre del archivo de la base de datos SQLite.
        /// </summary>
        public const string DatabaseFilename = "AppGestorVentas.db3";

        /// <summary>
        /// Configuraciones de apertura de SQLite, como lectura/escritura, creación y caché compartido.
        /// </summary>
        public const SQLiteOpenFlags Flags =
            // Abrir la base de datos en modo lectura/escritura
            SQLiteOpenFlags.ReadWrite |
            // Crear la base de datos si no existe
            SQLiteOpenFlags.Create |
            // Habilitar el acceso multi-hilo a la base de datos
            SQLiteOpenFlags.SharedCache;

        /// <summary>
        /// Ruta completa del archivo de la base de datos, ubicada en el directorio de datos de la aplicación.
        /// </summary>
        public static string DatabasePath =>
            Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);

        #endregion
    }
}

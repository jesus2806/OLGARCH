using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppGestorVentas.Classes
{
    class AdministradorSesion
    {

        #region IsUserLoggedInAsync

        /// <summary>
        /// Verifica si el usuario tiene datos de sesión almacenados, 
        /// lo cual indicaría que ha iniciado sesión.
        /// Por ejemplo, se verifica si la clave 'sUsuario' no está vacía.
        /// </summary>
        /// <returns>true si hay sesión iniciada; en caso contrario, false.</returns>
        public static async Task<bool> IsUserLoggedInAsync()
        {
            // Obtenemos el valor de la clave sUsuario.
            var usuario = await GetAsync(KeysSesion.sUsuario);

            // Consideramos que si 'usuario' no está vacío, el usuario inició sesión
            // Ajusta esta lógica según los requisitos reales
            return !string.IsNullOrWhiteSpace(usuario);
        }

        #endregion

        #region GetAsync

        /// <summary>
        /// Recupera el valor asociado a una clave específica de la sesión desde SecureStorage.
        /// </summary>
        /// <param name="key">Clave de tipo <see cref="KeysSesion"/> cuyo valor se desea obtener.</param>
        /// <returns>El valor asociado a la clave como una cadena. Si la clave no existe, devuelve una cadena vacía.</returns>
        public static async Task<string> GetAsync(KeysSesion key)
        {
            return await SecureStorage.GetAsync(key.ToString()) ?? "";
        }

        #endregion

        #region SetAsync

        /// <summary>
        /// Guarda un valor en SecureStorage asociado a una clave específica de la sesión.
        /// </summary>
        /// <param name="key">Clave de tipo <see cref="KeysSesion"/> a la que se asociará el valor.</param>
        /// <param name="sValue">Valor de tipo cadena que se almacenará en SecureStorage.</param>
        /// <returns>Una tarea que representa la operación asíncrona.</returns>
        public static async Task SetAsync(KeysSesion key, string sValue)
        {
            await SecureStorage.SetAsync(key.ToString(), sValue);
        }

        #endregion

        #region ClearSessionKeys

        /// <summary>
        /// Elimina todas las claves de sesión definidas en el enumerador <see cref="KeysSesion"/> de SecureStorage.
        /// </summary>
        /// <remarks>
        /// Este método recorre todas las claves enumeradas en <see cref="KeysSesion"/> y las elimina utilizando <see cref="SecureStorage.Remove"/>.
        /// Es útil para limpiar completamente los datos de sesión cuando el usuario cierra sesión o reinicia el contexto.
        /// </remarks>
        public static void ClearSessionKeys()
        {
            foreach (var key in Enum.GetValues(typeof(KeysSesion)))
            {
                SecureStorage.Remove(key.ToString()!);
            }
        }

        #endregion
    }
}

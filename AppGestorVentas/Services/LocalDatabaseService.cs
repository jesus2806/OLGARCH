using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppGestorVentas.Services
{
    /// <summary>
    /// Servicio para gestionar la base de datos local usando SQLite, proporcionando métodos para
    /// inicializar la conexión, crear tablas y manipular datos.
    /// </summary>
    public class LocalDatabaseService
    {
        #region PROPIEDADES

        private SQLiteAsyncConnection Database;

        #endregion

        #region Init

        /// <summary>
        /// Inicializa la conexión a la base de datos SQLite si no ha sido creada previamente.
        /// </summary>
        private async Task Init()
        {
            if (Database is not null)
                return;

            Database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
        }

        #endregion

        #region CreateTableAsync

        /// <summary>
        /// Crea una tabla en la base de datos local para el tipo especificado si aún no existe.
        /// </summary>
        /// <typeparam name="T">El tipo de datos para el que se creará la tabla.</typeparam>
        /// <returns>Retorna true si la tabla fue creada o ya existía; false en caso contrario.</returns>
        /// <exception cref="Exception">Lanza una excepción si ocurre un error durante la creación de la tabla, incluyendo problemas de conexión o de permisos.</exception>
        public async Task<bool> CreateTableAsync<T>() where T : new()
        {
            try
            {
                await Init();

                // Verificar si la tabla ya existe usando TableExistsAsync<T>
                if (await TableExistsAsync<T>() == false)
                {
                    var resultado = await Database.CreateTableAsync<T>();
                    return resultado == CreateTableResult.Created;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR CreateTableAsync: {ex.Message}");
            }

        }

        #endregion

        #region TableExistsAsync

        /// <summary>
        /// Verifica si una tabla específica existe en la base de datos.
        /// </summary>
        /// <typeparam name="T">El tipo de datos que representa la tabla a verificar.</typeparam>
        /// <returns>Retorna true si la tabla existe; de lo contrario, false.</returns>
        /// <exception cref="Exception">Lanza una excepción si ocurre un error al consultar la existencia de la tabla, como problemas de conexión o errores de SQL.</exception>
        public async Task<bool> TableExistsAsync<T>() where T : new()
        {
            try
            {
                await Init();

                // Obtener el nombre de la tabla usando reflexión
                var tableAttribute = typeof(T).GetCustomAttributes(typeof(TableAttribute), true)
                                              .FirstOrDefault() as TableAttribute;

                // Usa el nombre del atributo [Table] o el nombre de la clase si el atributo no existe
                var tableName = tableAttribute != null ? tableAttribute.Name : typeof(T).Name;

                // Consulta en sqlite_master para verificar si la tabla existe con ese nombre
                var result = await Database.ExecuteScalarAsync<int>(
                    $"SELECT count(name) FROM sqlite_master WHERE type='table' AND name='{tableName}'");

                // Devuelve true si la tabla existe
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR TableExistsAsync: {ex.Message}");
            }

        }

        #endregion

        #region GetItemsAsync

        /// <summary>
        /// Obtiene todos los elementos de una tabla específica en la base de datos.
        /// </summary>
        /// <typeparam name="T">El tipo de datos que representa la tabla.</typeparam>
        /// <returns>Una lista de elementos del tipo especificado.</returns>
        /// <exception cref="Exception">Lanza una excepción si ocurre un error al obtener los elementos de la tabla, como problemas de conexión o errores en la consulta.</exception>
        public async Task<List<T>> GetItemsAsync<T>() where T : new()
        {
            try
            {
                await Init();
                return await Database.Table<T>().ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR GetItemsAsync: {ex.Message}");
            }
        }

        #endregion

        #region GetItemsAsync

        /// <summary>
        /// Ejecuta una consulta SQL personalizada y retorna los resultados como una lista de elementos.
        /// </summary>
        /// <typeparam name="T">El tipo de datos de los elementos en la tabla.</typeparam>
        /// <param name="sQuery">La consulta SQL personalizada.</param>
        /// <returns>Una lista de elementos del tipo especificado, o null en caso de error.</returns>
        /// <exception cref="Exception">Lanza una excepción si ocurre un error al ejecutar la consulta personalizada, como errores de sintaxis SQL o problemas de conexión.</exception>
        public async Task<List<T>> GetItemsAsync<T>(string sQuery) where T : new()
        {
            try
            {
                await Init();
                return await Database.QueryAsync<T>(sQuery);

            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR GetItemsAsync: {ex.Message}");
            }
        }

        #endregion

        #region GetItemsAsync

        /// <summary>
        /// Ejecuta una consulta SQL personalizada y devuelve los resultados como una lista de objetos del tipo especificado.
        /// </summary>
        /// <typeparam name="T">El tipo de datos que representa los elementos en la tabla.</typeparam>
        /// <param name="sQuery">La consulta SQL personalizada que se ejecutará.</param>
        /// <param name="parameters">Parámetros opcionales para la consulta SQL. Estos valores serán sustituidos en las posiciones correspondientes dentro de la consulta.</param>
        /// <returns>Una lista de elementos del tipo especificado que coinciden con los resultados de la consulta.</returns>
        /// <exception cref="Exception">
        /// Lanza una excepción si ocurre un error al ejecutar la consulta, como problemas de sintaxis SQL,
        /// errores de conexión a la base de datos, o si los parámetros proporcionados son incorrectos.
        /// </exception>
        public async Task<List<T>> GetItemsAsync<T>(string sQuery, params object[] parameters) where T : new()
        {
            try
            {
                await Init();
                return await Database.QueryAsync<T>(sQuery, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR GetItemsAsync: {ex.Message}", ex);
            }
        }

        #endregion

        #region SaveItemAsync

        /// <summary>
        /// Guarda un nuevo elemento en la tabla correspondiente en la base de datos.
        /// </summary>
        /// <typeparam name="T">El tipo de datos del elemento a guardar.</typeparam>
        /// <param name="registro">El registro que se guardará en la tabla.</param>
        /// <returns>El número de filas afectadas por la inserción; 0 en caso de error.</returns>
        /// <exception cref="Exception">Lanza una excepción si ocurre un error al guardar el elemento en la base de datos, como problemas de conexión o restricciones de integridad.</exception>
        public async Task<int> SaveItemAsync<T>(T registro) where T : new()
        {
            try
            {
                await Init();
                return await Database.InsertAsync(registro);
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR SaveItemAsync: {ex.Message}");
            }
        }

        #endregion

        #region SaveAllItemsAsync

        /// <summary>
        /// Guarda múltiples registros en la base de datos de forma asíncrona.
        /// </summary>
        /// <typeparam name="T">El tipo de los registros a insertar. Debe ser una clase con un constructor sin parámetros.</typeparam>
        /// <param name="oRegistros">Lista de registros a insertar en la base de datos.</param>
        /// <returns>
        /// El número de filas afectadas en la base de datos.
        /// </returns>
        /// <exception cref="Exception">
        /// Lanza una excepción si ocurre un error durante la operación de inserción, incluyendo detalles del error.
        /// </exception>
        public async Task<int> SaveAllItemsAsync<T>(List<T> oRegistros) where T : new()
        {
            try
            {
                await Init();
                return await Database.InsertAllAsync(oRegistros);
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR SaveAllItemsAsync: {ex.Message}");
            }
        }

        #endregion

        #region ExecuteAsync

        /// <summary>
        /// Ejecuta una sentencia SQL.
        /// </summary>
        /// <param name="sQuery">La consulta SQL.</param>
        /// <param name="parameters">Parámetros opcionales para la consulta.</param>
        /// <returns>El número de filas afectadas por la operación.</returns>
        /// <exception cref="Exception">Lanza una excepción si ocurre un error durante la operación.</exception>
        public async Task<int> ExecuteAsync(string sQuery, params object[] parameters)
        {
            try
            {
                await Init();
                return await Database.ExecuteAsync(sQuery, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR ExecuteAsync: {ex.Message}", ex);
            }
        }

        #endregion

        #region CountItemsAsync

        /// <summary>
        /// Cuenta el número de filas en una tabla o el número de filas que cumplen una condición específica.
        /// </summary>
        /// <typeparam name="T">El tipo de datos que representa la tabla.</typeparam>
        /// <param name="sWhereClause">La cláusula WHERE opcional para filtrar los resultados. Por ejemplo: "column = ?".</param>
        /// <param name="aParameters">Los parámetros opcionales para la cláusula WHERE.</param>
        /// <returns>El número de filas que cumplen con los criterios especificados.</returns>
        /// <exception cref="Exception">Lanza una excepción si ocurre un error al ejecutar la consulta COUNT.</exception>
        public async Task<int> CountItemsAsync<T>(string sWhereClause = null, params object[] aParameters) where T : new()
        {
            try
            {
                await Init();

                // Obtener el nombre de la tabla usando reflexión

                var sTableName = typeof(T).GetCustomAttributes(typeof(TableAttribute), true)
                                              .FirstOrDefault() is TableAttribute oTableAttribute ? oTableAttribute.Name : typeof(T).Name;

                // Construir la consulta COUNT
                var query = $"SELECT COUNT(*) FROM {sTableName}";
                if (!string.IsNullOrEmpty(sWhereClause))
                {
                    query += $" WHERE {sWhereClause}";
                }

                // Ejecutar la consulta y retornar el resultado
                return await Database.ExecuteScalarAsync<int>(query, aParameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR CountItemsAsync: {ex.Message}", ex);
            }
        }

        #endregion

        #region SaveOrReplaceItemAsync

        /// <summary>
        /// Inserta o reemplaza un elemento en la base de datos.
        /// </summary>
        /// <typeparam name="T">El tipo del elemento a insertar o reemplazar.</typeparam>
        /// <param name="registro">El elemento a insertar o reemplazar.</param>
        /// <returns>El número de filas afectadas.</returns>
        /// <exception cref="Exception">Lanza una excepción si ocurre un error durante la operación.</exception>
        public async Task<int> SaveOrReplaceItemAsync<T>(T registro) where T : new()
        {
            try
            {
                await Init();
                return await Database.InsertOrReplaceAsync(registro);
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR SaveOrReplaceItemAsync: {ex.Message}", ex);
            }
        }

        #endregion

        #region DeleteAllRecordsAsync

        /// <summary>
        /// Elimina todos los registros de una tabla en la base de datos.
        /// </summary>
        /// <typeparam name="T">El tipo de datos que representa la tabla a limpiar.</typeparam>
        /// <returns>Una tarea que representa la operación de eliminación.</returns>
        /// <exception cref="Exception">Lanza una excepción si ocurre un error durante la operación.</exception>
        public async Task DeleteAllRecordsAsync<T>() where T : new()
        {
            try
            {
                await Init();

                // Obtener el nombre de la tabla usando reflexión
                var tableName = typeof(T).GetCustomAttributes(typeof(TableAttribute), true)
                                         .FirstOrDefault() is TableAttribute tableAttribute ? tableAttribute.Name : typeof(T).Name;

                // Ejecutar la sentencia SQL para eliminar todos los registros de la tabla
                await Database.ExecuteAsync($"DELETE FROM {tableName}");
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR DeleteAllRecordsAsync: {ex.Message}", ex);
            }
        }

        #endregion

        #region DeleteItemAsync
        /// <summary>
        /// Elimina un registro específico de la base de datos.
        /// </summary>
        /// <typeparam name="T">El tipo de datos del registro a eliminar.</typeparam>
        /// <param name="item">El registro que se desea eliminar.</param>
        /// <returns>El número de filas afectadas (1 si se eliminó correctamente, 0 si no se encontró el registro).</returns>
        /// <exception cref="Exception">Lanza una excepción si ocurre un error durante la operación.</exception>
        public async Task<int> DeleteItemAsync<T>(T item) where T : new()
        {
            try
            {
                await Init();
                return await Database.DeleteAsync(item);
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR DeleteItemAsync: {ex.Message}", ex);
            }
        }
        #endregion

        #region DeleteRecordsAsync
        /// <summary>
        /// Elimina registros de la tabla que cumplan con la condición especificada en la cláusula WHERE.
        /// </summary>
        /// <typeparam name="T">El tipo de datos que representa la tabla en la que se eliminarán los registros.</typeparam>
        /// <param name="sWhereClause">La cláusula WHERE que define la condición para eliminar registros. 
        /// Por ejemplo: "column = ?" o "column1 = ? AND column2 = ?".</param>
        /// <param name="aParameters">Los valores correspondientes a los parámetros incluidos en la cláusula WHERE.</param>
        /// <returns>El número de filas afectadas en la base de datos.</returns>
        /// <exception cref="Exception">Lanza una excepción si ocurre un error durante la operación.</exception>
        public async Task<int> DeleteRecordsAsync<T>(string sWhereClause, params object[] aParameters) where T : new()
        {
            try
            {
                await Init();

                // Obtener el nombre de la tabla usando reflexión
                var tableName = typeof(T).GetCustomAttributes(typeof(TableAttribute), true)
                                         .FirstOrDefault() is TableAttribute tableAttribute
                                             ? tableAttribute.Name
                                             : typeof(T).Name;

                // Construir la consulta DELETE
                var query = $"DELETE FROM {tableName} WHERE {sWhereClause}";

                // Ejecuta la consulta y retornar el número de filas afectadas
                return await Database.ExecuteAsync(query, aParameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR DeleteRecordsAsync: {ex.Message}", ex);
            }
        }
        #endregion


        #region UpdateItemAsync

        /// <summary>
        /// Actualiza un registro existente en la base de datos.
        /// </summary>
        /// <typeparam name="T">El tipo de datos del registro a actualizar.</typeparam>
        /// <param name="registro">El registro con los datos actualizados.</param>
        /// <returns>El número de filas afectadas por la operación de actualización.</returns>
        /// <exception cref="Exception">Lanza una excepción si ocurre un error durante la actualización.</exception>
        public async Task<int> UpdateItemAsync<T>(T registro) where T : new()
        {
            try
            {
                await Init();
                return await Database.UpdateAsync(registro);
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR UpdateItemAsync: {ex.Message}", ex);
            }
        }

        #endregion

        #region DropTableAsync

        /// <summary>
        /// Elimina la tabla asociada al tipo <typeparamref name="T"/>. 
        /// Esto borra todos sus datos y la estructura de la tabla.
        /// </summary>
        /// <typeparam name="T">Clase con atributo [Table], representa la tabla a eliminar.</typeparam>
        /// <returns>Retorna true si se eliminó la tabla, false si no existía.</returns>
        /// <exception cref="Exception">Lanza excepción ante errores de conexión o de permisos.</exception>
        public async Task<bool> DropTableAsync<T>() where T : new()
        {
            try
            {
                await Init();

                // Obtener el nombre de la tabla mediante reflexión
                var tableAttribute = typeof(T).GetCustomAttributes(typeof(TableAttribute), true)
                                              .FirstOrDefault() as TableAttribute;
                var tableName = tableAttribute != null ? tableAttribute.Name : typeof(T).Name;

                // Verificar si existe:
                if (await TableExistsAsync<T>())
                {
                    // Ejecuta la sentencia DROP TABLE ...
                    await Database.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR DropTableAsync: {ex.Message}", ex);
            }
        }

        #endregion

        #region DeleteDatabaseAsync

        /// <summary>
        /// Elimina físicamente el archivo de la base de datos local.
        /// Después de llamar a este método, la conexión actual se cierra
        /// (Database queda en null) y cualquier operación posterior requerirá
        /// crear nuevamente la base de datos.
        /// </summary>
        public async Task DeleteDatabaseAsync()
        {
            try
            {
                // Asegúrate de 'cerrar' la conexión para que no haya locks
                // Normalmente, SQLiteAsyncConnection no expone un .CloseAsync(), 
                // pero poner Database = null evita llamadas posteriores.
                if (Database != null)
                {
                    await Database.CloseAsync();
                    Database = null;
                }

                if (File.Exists(Constants.DatabasePath))
                {
                    File.Delete(Constants.DatabasePath);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR DeleteDatabaseAsync: {ex.Message}", ex);
            }
        }

        #endregion
    }
}

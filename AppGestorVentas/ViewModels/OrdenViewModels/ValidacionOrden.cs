using AppGestorVentas.Classes;
using AppGestorVentas.Models;
using AppGestorVentas.Services;
using System.Net.Http.Json;

namespace AppGestorVentas.ViewModels.OrdenViewModels
{
    public class ValidacionOrden
    {
        private readonly HttpApiService _httpApiService;

        public ValidacionOrden(HttpApiService httpApiService)
        {
            _httpApiService = httpApiService;
        }

        /// <summary>
        /// Obtiene una orden por su identificador de MongoDB.
        /// </summary>
        public async Task<Orden> ObtenerOrdenById(string sIdMongoDB)
        {
            try
            {
                var response = await _httpApiService.GetAsync($"api/orden/{sIdMongoDB}");
                if (response == null)
                {
                    // Se podría registrar un error aquí.
                    return null;
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<Orden>>();
                if (apiResponse?.bSuccess == true && apiResponse.lData?.Any() == true)
                {
                    return apiResponse.lData.First();
                }
                else
                {
                    // Se podría registrar una advertencia si la respuesta no contiene datos.
                    return null;
                }
            }
            catch (Exception ex)
            {
                // Logger.LogError(ex, "Error al obtener la orden.");
                return null;
            }
        }

        /// <summary>
        /// Convierte el código de estatus a su descripción.
        /// </summary>
        private string GetEstatusDescripcion(int codigo) =>
            codigo switch
            {
                0 => "Pendiente",
                1 => "Confirmada",
                2 => "En preparación",
                3 => "Preparada",
                4 => "Entregada",
                5 => "Pagada",
                _ => "Desconocido"
            };

        /// <summary>
        /// Mapea el estatus deseado (input) con el estatus actual esperado para permitir la actualización.
        /// Por ejemplo, para actualizar a "Confirmada" (input = 1), la orden debe estar en "Pendiente" (código 0).
        /// </summary>
        private (int ExpectedCurrentStatus, string Descripcion) GetStatusMapping(int nuevoEstatus) =>
            nuevoEstatus switch
            {
                1 => (0, "Confirmada"),
                2 => (1, "En preparación"),
                3 => (2, "Preparada"),
                4 => (3, "Entregada"),
                5 => (4, "Pagada"),
                _ => (-1, "Desconocido")
            };

        /// <summary>
        /// Actualiza el estatus de una orden, validando primero que la transición de estado sea permitida.
        /// Retorna un tuple que indica éxito y un mensaje descriptivo.
        /// </summary>
        public async Task<(bool, string)> ActualizarEstatusOrden(string sIdMongoDB, int iEstatus)
        {
            try
            {
                // Se obtiene la orden.
                var orden = await ObtenerOrdenById(sIdMongoDB);
                if (orden == null)
                {
                    return (false, $"No se encontró la orden con ID '{sIdMongoDB}'.");
                }

                // Se obtiene el mapeo entre el estatus deseado y el estatus actual esperado.
                var mapping = GetStatusMapping(iEstatus);
                if (mapping.ExpectedCurrentStatus == -1)
                {
                    return (false, $"Estatus solicitado no es válido: {iEstatus}.");
                }

                string estatusActual = GetEstatusDescripcion(orden.iEstatus);
                // Valida que la orden se encuentre en el estado previo esperado.
                if (orden.iEstatus != mapping.ExpectedCurrentStatus)
                {
                    return (false, $"No se puede actualizar la orden a '{mapping.Descripcion}' porque su estado actual es '{estatusActual}'.");
                }

                // Se prepara el payload y se llama al endpoint para actualizar.
                var payload = new { iEstatus };
                string route = $"api/orden/{sIdMongoDB}";
                var response = await _httpApiService.PutAsync(route, payload);
                if (response == null)
                {
                    return (false, $"No se recibió respuesta del servidor al intentar actualizar la orden a '{mapping.Descripcion}'.");
                }

                if (response.IsSuccessStatusCode)
                {
                    return (true, $"La orden se actualizó a '{mapping.Descripcion}' correctamente.");
                }
                else
                {
                    string detalleError = await response.Content.ReadAsStringAsync();
                    return (false, $"Error al actualizar la orden a '{mapping.Descripcion}'. Detalle: {detalleError}");
                }
            }
            catch (Exception ex)
            {
                // Logger.LogError(ex, "Error al actualizar el estatus de la orden.");
                return (false, $"ERROR al actualizar el estatus de la orden: {ex.Message} {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Obtiene el estatus actual de la orden.
        /// </summary>
        public async Task<(int, string)> ObtenerEstatusActual(string sIdMongoDB)
        {
            try
            {
                var orden = await ObtenerOrdenById(sIdMongoDB);
                if (orden == null)
                {
                    // Código -404 para indicar que no se encontró la orden.
                    return (-404, "Orden no encontrada.");
                }

                string descripcion = GetEstatusDescripcion(orden.iEstatus);
                return (orden.iEstatus, descripcion);
            }
            catch (Exception ex)
            {
                // Código -500 para errores internos.
                return (-500, $"Error al obtener el estatus actual de la orden: {ex.Message}");
            }
        }

        public async Task<(bool, string)> VerifyOrdenStatusForImprimir(string sIdMongoDB)
        {
            try
            {
                string sRoute = $"api/ordenes/verifyOrdenStatus/{sIdMongoDB}";
                var response = await _httpApiService.GetAsync(sRoute);

                if (response == null)
                {
                    return (false, $"No se recibió respuesta del servidor al intentar verificar la orden.");
                }

                var apiResponse = await response!.Content.ReadFromJsonAsync<ApiRespuesta<OrdersNotStatus4>>();

                if (response.IsSuccessStatusCode)
                {
                    if (apiResponse.bSuccess == false) // Existen ordenes que no estan entregadas
                    {
                        string sMensaje = "Las siguientes ordenes aun no se encuentran entregadas:\n\n";
                        foreach (OrdersNotStatus4 orden in apiResponse.lData)
                        {
                            sMensaje += $"\t* No. Orden: {orden.iOrdenNumber}\n";
                        }
                        return (true, sMensaje);
                    }
                    else
                    {
                        // Todas las ordenes tienen estatus entregada
                        return (true, "");
                    }
                }
                else
                {
                    string detalleError = apiResponse.Error.sDetails;
                    return (false, $"Error al verificar el estatus de la orden. Detalle: {detalleError}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"ERROR al verificar el estatus de la orden: {ex.Message} {ex.StackTrace}");
            }

        }
    }
}

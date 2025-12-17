using AppGestorVentas.Classes;
using AppGestorVentas.Models;
using AppGestorVentas.ViewModels.Popup;
using AppGestorVentas.Views.LoginViews;
using CommunityToolkit.Maui.Core;
using SkiaSharp;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AppGestorVentas.Services
{
    public class HttpApiService
    {
        #region PROPIEDADES

        private readonly HttpClient _httpClient;
        private readonly IPopupService _oPopupService;
        private readonly SocketIoService _socketIoService;

        #endregion

        #region CONSTRUCTORES

        public HttpApiService(HttpClient httpClient, IPopupService popupService, SocketIoService socketIoService)
        {
            _httpClient = httpClient;
            _oPopupService = popupService;
            _socketIoService = socketIoService;
        }

        #endregion

        #region PostAsync

        public async Task<HttpResponseMessage?> PostAsync(string route, object jsonBody, bool bRequiereToken = true)
        {
            try
            {
                // 1. Obtener Token si se requiere
                if (bRequiereToken)
                {
                    string tokenAcceso = await AdministradorSesion.GetAsync(KeysSesion.sTokenAcceso);

                    if (string.IsNullOrWhiteSpace(tokenAcceso))
                    {
                        return null;
                    }

                    // Asigna el token en la cabecera
                    _httpClient.DefaultRequestHeaders.Accept.Clear();
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", tokenAcceso);
                }
                else
                {
                    // No se requiere token para esta petición
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                }


                var content = JsonContent.Create(jsonBody);
                var requestUri = new Uri(_httpClient.BaseAddress!, route);
                HttpResponseMessage oHttpResponseMessege = await _httpClient.PostAsync(requestUri, content);
                if (bRequiereToken == true && oHttpResponseMessege.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await RedireccionarLogin(oHttpResponseMessege);
                    return null;
                }
                else
                {
                    return oHttpResponseMessege;
                }

            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR PostAsync: {ex.Message}", ex);
            }
        }

        #endregion

        #region PutAsync

        /// <summary>
        /// Realiza una solicitud HTTP PUT a la API.
        /// </summary>
        /// <param name="route">La ruta del endpoint.</param>
        /// <param name="jsonBody">El objeto a enviar en el cuerpo de la solicitud.</param>
        /// <param name="bRequiereToken">Indica si la solicitud requiere un token de autenticación.</param>
        /// <param name="bReutilizarToken">Indica si se debe reutilizar el token existente.</param>
        /// <returns>Retorna un HttpResponseMessage o null en caso de error.</returns>
        public async Task<HttpResponseMessage?> PutAsync(string route, object jsonBody, bool bRequiereToken = true, bool bReutilizarToken = false)
        {
            try
            {

                // 1. Obtener Token si se requiere
                if (bRequiereToken)
                {
                    string tokenAcceso = await AdministradorSesion.GetAsync(KeysSesion.sTokenAcceso);

                    if (string.IsNullOrWhiteSpace(tokenAcceso))
                    {
                        return null;
                    }

                    // Asigna el token en la cabecera
                    _httpClient.DefaultRequestHeaders.Accept.Clear();
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", tokenAcceso);
                }
                else
                {
                    // No se requiere token para esta petición
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                }

                var content = JsonContent.Create(jsonBody);
                var requestUri = new Uri(_httpClient.BaseAddress!, route);
                HttpResponseMessage oHttpResponseMessege = await _httpClient.PutAsync(requestUri, content);
                if (oHttpResponseMessege.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await RedireccionarLogin(oHttpResponseMessege);
                    return null;
                }
                else
                {
                    return oHttpResponseMessege;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR PutAsync: {ex.Message}", ex);
            }
        }

        #endregion

        #region PatchAsync

        /// <summary>
        /// Realiza una solicitud HTTP PATCH a la API.
        /// </summary>
        /// <param name="route">La ruta del endpoint.</param>
        /// <param name="jsonBody">El objeto a enviar en el cuerpo de la solicitud.</param>
        /// <param name="bRequiereToken">Indica si la solicitud requiere un token de autenticación.</param>
        /// <returns>Retorna un HttpResponseMessage o null en caso de error.</returns>
        public async Task<HttpResponseMessage?> PatchAsync(string route, object jsonBody, bool bRequiereToken = true)
        {
            try
            {
                // 1. Obtener Token si se requiere
                if (bRequiereToken)
                {
                    string tokenAcceso = await AdministradorSesion.GetAsync(KeysSesion.sTokenAcceso);

                    if (string.IsNullOrWhiteSpace(tokenAcceso))
                    {
                        return null;
                    }

                    // Asigna el token en la cabecera
                    _httpClient.DefaultRequestHeaders.Accept.Clear();
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", tokenAcceso);
                }
                else
                {
                    // No se requiere token para esta petición
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                }

                var content = JsonContent.Create(jsonBody);
                var requestUri = new Uri(_httpClient.BaseAddress!, route);
                
                var request = new HttpRequestMessage(HttpMethod.Patch, requestUri)
                {
                    Content = content
                };
                
                HttpResponseMessage oHttpResponseMessege = await _httpClient.SendAsync(request);
                
                if (oHttpResponseMessege.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await RedireccionarLogin(oHttpResponseMessege);
                    return null;
                }
                else
                {
                    return oHttpResponseMessege;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR PatchAsync: {ex.Message}", ex);
            }
        }

        #endregion

        #region DeleteAsync

        /// <summary>
        /// Realiza una solicitud HTTP DELETE a la API.
        /// </summary>
        /// <param name="route">La ruta del endpoint.</param>
        /// <param name="bRequiereToken">Indica si la solicitud requiere un token de autenticación.</param>
        /// <param name="bReutilizarToken">Indica si se debe reutilizar el token existente.</param>
        /// <returns>Retorna un HttpResponseMessage o null en caso de error.</returns>
        public async Task<HttpResponseMessage?> DeleteAsync(string route, bool bRequiereToken = true, bool bReutilizarToken = false)
        {
            try
            {
                // 1. Obtener Token si se requiere
                if (bRequiereToken)
                {
                    string tokenAcceso = await AdministradorSesion.GetAsync(KeysSesion.sTokenAcceso);

                    if (string.IsNullOrWhiteSpace(tokenAcceso))
                    {
                        return null;
                    }

                    // Asigna el token en la cabecera
                    _httpClient.DefaultRequestHeaders.Accept.Clear();
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", tokenAcceso);
                }
                else
                {
                    // No se requiere token para esta petición
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                }


                var requestUri = new Uri(_httpClient.BaseAddress!, route);
                HttpResponseMessage oHttpResponseMessege = await _httpClient.DeleteAsync(requestUri);
                if (oHttpResponseMessege.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await RedireccionarLogin(oHttpResponseMessege);
                    return null;
                }
                else
                {
                    return oHttpResponseMessege;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR DeleteAsync: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Realiza una solicitud HTTP DELETE a la API con soporte para cancelación.
        /// </summary>
        /// <param name="route">La ruta del endpoint.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <param name="bRequiereToken">Indica si la solicitud requiere un token de autenticación.</param>
        /// <param name="bReutilizarToken">Indica si se debe reutilizar el token existente.</param>
        /// <returns>Retorna un HttpResponseMessage o null en caso de error o cancelación.</returns>
        public async Task<HttpResponseMessage?> DeleteAsync(string route, CancellationToken cancellationToken, bool bRequiereToken = true, bool bReutilizarToken = false)
        {
            try
            {
                // 1. Obtener Token si se requiere
                if (bRequiereToken)
                {
                    string tokenAcceso = await AdministradorSesion.GetAsync(KeysSesion.sTokenAcceso);

                    if (string.IsNullOrWhiteSpace(tokenAcceso))
                    {
                        return null;
                    }

                    // Asigna el token en la cabecera
                    _httpClient.DefaultRequestHeaders.Accept.Clear();
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", tokenAcceso);
                }
                else
                {
                    // No se requiere token para esta petición
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                }


                var requestUri = new Uri(_httpClient.BaseAddress!, route);
                HttpResponseMessage oHttpResponseMessege = await _httpClient.DeleteAsync(requestUri, cancellationToken);
                if (oHttpResponseMessege.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await RedireccionarLogin(oHttpResponseMessege);
                    return null;
                }
                else
                {
                    return oHttpResponseMessege;
                }
            }
            catch (OperationCanceledException)
            {
                // La operación fue cancelada por el usuario
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR DeleteAsync: {ex.Message}", ex);
            }
        }

        #endregion

        #region GetAsync

        public async Task<HttpResponseMessage?> GetAsync(string sRoute, bool bRequiereToken = true, bool bReutilizarToken = false)
        {
            try
            {
                // 1. Obtener Token si se requiere
                if (bRequiereToken)
                {
                    string tokenAcceso = await AdministradorSesion.GetAsync(KeysSesion.sTokenAcceso);

                    if (string.IsNullOrWhiteSpace(tokenAcceso))
                    {
                        return null;
                    }

                    // Asigna el token en la cabecera
                    _httpClient.DefaultRequestHeaders.Accept.Clear();
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", tokenAcceso);
                }
                else
                {
                    // No se requiere token para esta petición
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                }

                // Realizar la solicitud HTTP GET
                HttpResponseMessage oHttpResponseMessege = await _httpClient.GetAsync(_httpClient.BaseAddress + sRoute);
                if (oHttpResponseMessege.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await RedireccionarLogin(oHttpResponseMessege);
                    return null;
                }
                else
                {
                    return oHttpResponseMessege;
                }


            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR GetAsync: {ex.Message}");
            }
        }

        public async Task<HttpResponseMessage?> GetAsync(string sRoute, CancellationToken oCancellationToken, bool bRequiereToken = true, bool bReutilizarToken = false)
        {
            try
            {
                // 1. Obtener Token si se requiere
                if (bRequiereToken)
                {
                    string tokenAcceso = await AdministradorSesion.GetAsync(KeysSesion.sTokenAcceso);

                    if (string.IsNullOrWhiteSpace(tokenAcceso))
                    {
                        return null;
                    }

                    // Asigna el token en la cabecera
                    _httpClient.DefaultRequestHeaders.Accept.Clear();
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", tokenAcceso);
                }
                else
                {
                    // No se requiere token para esta petición
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                }


                // Realizar la solicitud HTTP GET con soporte para cancelación
                HttpResponseMessage oHttpResponseMessege = await _httpClient.GetAsync(new Uri(_httpClient.BaseAddress!, sRoute), oCancellationToken);
                if (oHttpResponseMessege.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await RedireccionarLogin(oHttpResponseMessege);
                    return null;
                }
                else
                {
                    return oHttpResponseMessege;
                }
            }
            catch (OperationCanceledException)
            {
                // La operación fue cancelada por el usuario
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR GetAsync: {ex.Message}", ex);
            }
        }

        public async Task<HttpResponseMessage?> PostMultipartAsync(
            string route,
            Stream fileStream,
            string fileName,
            string fieldName = "image",  // Nombre del campo, por defecto "image"
            bool bRequiereToken = true,
            bool bReutilizarToken = false)
        {
            try
            {
                // 1. Obtener Token si se requiere
                if (bRequiereToken)
                {
                    string tokenAcceso = await AdministradorSesion.GetAsync(KeysSesion.sTokenAcceso);

                    if (string.IsNullOrWhiteSpace(tokenAcceso))
                    {
                        return null;
                    }

                    // Asigna el token en la cabecera
                    _httpClient.DefaultRequestHeaders.Accept.Clear();
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", tokenAcceso);
                }
                else
                {
                    // No se requiere token para esta petición
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                }

                // 2. Convertir el stream original a un arreglo de bytes
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await fileStream.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                // 2.1. Comprimir la imagen con SkiaSharp
                //      (Aquí puedes ajustar la calidad o, si quieres, también redimensionar la imagen)
                byte[] compressedBytes = CompressImageToJpeg(fileBytes, 50);

                // 3. Crear el contenido MultipartFormDataContent
                using var multipartContent = new MultipartFormDataContent();

                // Crear el ByteArrayContent con la imagen comprimida
                var imageContent = new ByteArrayContent(compressedBytes);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                // Nombre del campo y nombre de archivo
                multipartContent.Add(imageContent, fieldName, fileName);

                // 4. Construir la URI con baseAddress + route
                var requestUri = new Uri(_httpClient.BaseAddress!, route);

                // 5. Enviar la petición POST con el contenido multipart
                HttpResponseMessage oHttpResponseMessege = await _httpClient.PostAsync(requestUri, multipartContent);
                if (oHttpResponseMessege.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await RedireccionarLogin(oHttpResponseMessege);
                    return null;
                }
                else
                {
                    return oHttpResponseMessege;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR PostMultipartAsync: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// Método auxiliar para comprimir una imagen a formato JPEG con cierta calidad.
        /// </summary>
        /// <param name="sourceBytes">Arreglo de bytes de la imagen original.</param>
        /// <param name="quality">Calidad deseada (0-100), donde 100 es la máxima calidad.</param>
        /// <returns>Arreglo de bytes con la imagen comprimida en JPEG.</returns>
        private byte[] CompressImageToJpeg(byte[] sourceBytes, int quality)
        {
            using var inputStream = new MemoryStream(sourceBytes);

            // Decodificar la imagen original en un SKBitmap
            using var originalBitmap = SKBitmap.Decode(inputStream);
            if (originalBitmap == null)
                return sourceBytes; // Si falla la decodificación, retorna la imagen original

            // (Opcional) Redimensionar antes de recodificar
            // Por ejemplo, para limitar el ancho a 800px:
            // var width = 800;
            // var ratio = (double)width / originalBitmap.Width;
            // var height = (int)(originalBitmap.Height * ratio);
            // using var resizedBitmap = originalBitmap.Resize(
            //     new SKImageInfo(width, height),
            //     SKFilterQuality.High);

            // Decodificar en SKImage (si no redimensionas, usar originalBitmap directamente)
            // usando var finalBitmap = resizedBitmap ?? originalBitmap;
            using var image = SKImage.FromBitmap(originalBitmap);

            // Codificar a JPEG con la calidad solicitada
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);

            // Guardar en un MemoryStream y retornar bytes
            if (data == null)
                return sourceBytes;

            using var outputStream = new MemoryStream();
            data.SaveTo(outputStream);
            return outputStream.ToArray();
        }

        #endregion

        #region RedireccionarLogin

        public async Task RedireccionarLogin(HttpResponseMessage oHttpResponseMessege)
        {
            string mensajeError = string.Empty;
            try
            {
                var apiResponse = await oHttpResponseMessege.Content.ReadFromJsonAsync<ApiRespuesta<Object>>();

                if (apiResponse != null)
                {
                    mensajeError = apiResponse.Error != null && !string.IsNullOrWhiteSpace(apiResponse.Error.sDetails)
                        ? apiResponse.Error.sDetails
                        : apiResponse.sMessage;
                }
                else
                {
                    mensajeError = "Se ha producido un error de autenticación. Por favor, vuelva a iniciar sesión.";
                }

                await MostrarAlerta("Error de Autenticación", mensajeError);
                AdministradorSesion.ClearSessionKeys();
                if (Application.Current is App app)
                {
                    var page = app.Services.GetRequiredService<LoginView>();
                    app.MainPage = page; // O, si deseas usar la pila de navegación:
                }

            }
            catch (Exception ex)
            {
                await MostrarAlerta("Error", $"Ocurrió un error inesperado: {ex.Message}");
            }
        }

        #endregion

        #region MostrarAlerta

        /// <summary>
        /// Muestra una alerta en la pantalla principal.
        /// </summary>
        /// <param name="titulo">Título de la alerta.</param>
        /// <param name="mensaje">Mensaje de la alerta.</param>
        private async Task MostrarAlerta(string titulo, string mensaje)
        {
            if (mensaje.Contains("expirado"))
            {
                mensaje = "Sesión expirada. Por favor, inicia sesión nuevamente.";
            }
            var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (mainPage != null)
            {
                await _oPopupService.ShowPopupAsync<AlertaGeneralPopupViewModel>(vm =>
                {
                    vm.SMensaje = mensaje;
                });
            }
        }

        #endregion
    }
}

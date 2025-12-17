using AppGestorVentas.Classes;
using AppGestorVentas.Models;
using AppGestorVentas.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net.Http.Json;

namespace AppGestorVentas.ViewModels.LoginViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly HttpApiService _httpApiService;
        private readonly SocketIoService _socketIoService;

        [ObservableProperty]
        private string sUsuario = string.Empty;

        [ObservableProperty]
        private string sPassword = string.Empty;

        public LoginViewModel(HttpApiService httpApiService, SocketIoService socketIo)
        {
            _httpApiService = httpApiService;
            _socketIoService = socketIo;

        }


        public async Task ComprobarSesion()
        {
            try
            {
                string? sNombreUsuario = await AdministradorSesion.GetAsync(KeysSesion.sNombreUsuario);

                // Obtener el rol del usuario de la sesión.
                string? sRol = await AdministradorSesion.GetAsync(KeysSesion.iRol);
                if (!string.IsNullOrWhiteSpace(sNombreUsuario) && int.TryParse(sRol, out int rolUsuario))
                {
                    Application.Current.MainPage = new AppShell(_socketIoService, rolUsuario, sNombreUsuario);
                }
            }
            catch (Exception)
            {

            }
        }


        [RelayCommand]
        public async Task Ingresar()
        {
            try
            {
                // Validar que se hayan ingresado ambos campos.
                if (string.IsNullOrWhiteSpace(SUsuario) || string.IsNullOrWhiteSpace(SPassword))
                {
                    await MostrarAlerta("Error", "Los campos de usuario y contraseña son requeridos.");
                    return;
                }

                // Construir el payload para la autenticación.
                var payload = new
                {
                    sUsuario = SUsuario.Replace(" ",""),
                    sPassword = SPassword.Trim()
                };

                // Realizar la solicitud POST al endpoint de autenticación.
                // bRequiereToken: false indica que esta llamada no necesita un token.
                HttpResponseMessage? response = await _httpApiService.PostAsync("api/login", payload, bRequiereToken: false);

                if (response == null)
                {
                    await MostrarAlerta("Error", "No se recibió respuesta del servidor. Por favor, inténtelo nuevamente más tarde.");
                    return;
                }

                // Intentar deserializar la respuesta.
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<UsuarioToken>>();
                if (apiResponse == null)
                {
                    await MostrarAlerta("Error", "Respuesta inválida del servidor.");
                    return;
                }

                // Manejar el caso de error según la respuesta de la API.
                if (!apiResponse.bSuccess)
                {
                    // Si el backend envía detalles en la propiedad error, se muestran.
                    string mensajeError = apiResponse.Error != null && !string.IsNullOrWhiteSpace(apiResponse.Error.sDetails)
                        ? apiResponse.Error.sDetails
                        : apiResponse.sMessage;

                    await MostrarAlerta("Error", mensajeError);
                    return;
                }

                // Validar que se haya recibido el token de autenticación.
                if (apiResponse.lData == null || !apiResponse.lData.Any())
                {
                    await MostrarAlerta("Error", "No se recibió el token de autenticación. Por favor, intente nuevamente.");
                    return;
                }

                // Caso de éxito: se recibió el token correctamente.
                UsuarioToken oUsuarioToken = apiResponse.lData.First();

                if (await EstablecerSesion(oUsuarioToken))
                {
                    string? sNombreUsuario = await AdministradorSesion.GetAsync(KeysSesion.sNombreUsuario);
                    Application.Current.MainPage = new AppShell(_socketIoService, oUsuarioToken.iRol, sNombreUsuario);
                }

            }
            catch (Exception ex)
            {
                await MostrarAlerta("Error", $"Ocurrió un error inesperado: {ex.Message}");
            }
        }


        private async Task<bool> EstablecerSesion(UsuarioToken oUsuarioToken)
        {
            bool bRespuesta = false;
            try
            {
                await AdministradorSesion.SetAsync(KeysSesion.sNombreUsuario, oUsuarioToken.sNombreUsuario);
                await AdministradorSesion.SetAsync(KeysSesion.sUsuario, oUsuarioToken.sUsuario);
                await AdministradorSesion.SetAsync(KeysSesion.sIdUsuarioMongoDB, oUsuarioToken.sIdUsuarioMongoDB);
                await AdministradorSesion.SetAsync(KeysSesion.iRol, oUsuarioToken.iRol.ToString());
                await AdministradorSesion.SetAsync(KeysSesion.sTokenAcceso, oUsuarioToken.sTokenAcceso);
                bRespuesta = true;
            }
            catch (Exception ex)
            {
                await MostrarAlerta("Error", $"Ocurrió un error inesperado: {ex.Message}");
            }

            return bRespuesta;
        }


        /// <summary>
        /// Muestra una alerta en la pantalla principal.
        /// </summary>
        /// <param name="titulo">Título de la alerta.</param>
        /// <param name="mensaje">Mensaje de la alerta.</param>
        private async Task MostrarAlerta(string titulo, string mensaje)
        {
            var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (mainPage != null)
            {
                await mainPage.DisplayAlert(titulo, mensaje, "OK");
            }
        }
    }
}

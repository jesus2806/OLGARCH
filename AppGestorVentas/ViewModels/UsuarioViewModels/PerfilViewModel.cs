using AppGestorVentas.Classes;
using AppGestorVentas.Models;
using AppGestorVentas.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net.Http.Json;

namespace AppGestorVentas.ViewModels.UsuarioViewModels
{
    public partial class PerfilViewModel : ObservableObject
    {

        private HttpApiService _httpApiService;
        private string _iIdMongoDB;

        [ObservableProperty]
        public string sNombreUsuario;

        [ObservableProperty]
        public string sPassword;

        public PerfilViewModel(HttpApiService httpApiService)
        {
            _httpApiService = httpApiService;
        }

        public async Task InicializarDatos()
        {
            SNombreUsuario = await AdministradorSesion.GetAsync(KeysSesion.sNombreUsuario);
            _iIdMongoDB = await AdministradorSesion.GetAsync(KeysSesion.sIdUsuarioMongoDB);
        }

        [RelayCommand]
        private async Task ActualizarPass()
        {
            string sMensajeErrorProceso = string.Empty;

            try
            {
                // Validar que se hayan ingresado ambos campos.
                if (string.IsNullOrWhiteSpace(SPassword))
                {
                    await MostrarAlerta("Error", "El campo contraseña es requerido.");
                    return;
                }

                if (SPassword.Length <= 7)
                {
                    await MostrarAlerta("Error", "El campo debe contener al menos 8 caracteres.");
                    return;
                }

                Usuario oDatosUsuarioActualizados = new Usuario
                {
                    sPassword = SPassword
                };

                HttpResponseMessage? oRespuestaHTTP = await _httpApiService.PutAsync($"api/usuarios/{_iIdMongoDB}", oDatosUsuarioActualizados, bRequiereToken: true);
                if (oRespuestaHTTP != null)
                {
                    var oRespuestaApi = await oRespuestaHTTP.Content.ReadFromJsonAsync<ApiRespuesta<Usuario>>();
                    if (oRespuestaHTTP.IsSuccessStatusCode)
                    {
                        if (oRespuestaApi != null && oRespuestaApi.bSuccess)
                        {
                            var mainPage = Application.Current?.Windows[0].Page;
                            if (mainPage != null)
                            {
                                await mainPage.DisplayAlert("Actualizado", "Contraseña actualizada con éxito.", "OK");
                                SPassword = string.Empty;
                                await Shell.Current.GoToAsync("..");
                            }
                        }
                    }
                    else
                    {
                        sMensajeErrorProceso = oRespuestaApi?.Error?.sDetails ?? "Ha ocurrido un error inesperado.";
                    }
                }
            }
            catch (Exception ex)
            {
                sMensajeErrorProceso = $"{ex.Message} {ex.StackTrace}";
            }

            if (!string.IsNullOrWhiteSpace(sMensajeErrorProceso))
            {
                await MostrarAlerta("Error", sMensajeErrorProceso);
            }
        }


        [RelayCommand]
        private async Task Regresar()
        {
            try
            {
                SPassword = string.Empty;
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
               await MostrarAlerta("Error", $"{ex.Message} {ex.StackTrace}");
            }

        }


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

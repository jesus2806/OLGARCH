using AppGestorVentas.Models;
using AppGestorVentas.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace AppGestorVentas.ViewModels.EsquemaViewModels
{
    public partial class EsquemasViewModel : ObservableObject
    {
        private readonly HttpApiService _http;

        [ObservableProperty] private bool bLoading;
        [ObservableProperty] private ObservableCollection<Esquema> lstEsquemas = new();

        public EsquemasViewModel(HttpApiService httpApiService)
        {
            _http = httpApiService;
        }

        [RelayCommand]
        private async Task Cargar()
        {
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    await MostrarError("No tienes acceso a Internet.");
                    return;
                }

                BLoading = true;

                var resp = await _http.GetAsync("api/esquemas", bRequiereToken: true);
                if (resp == null) return;

                // Con tu ApiRespuesta<T> => lData es List<T>
                var api = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Esquema>>();

                if (resp.IsSuccessStatusCode && api != null && api.bSuccess && api.lData != null)
                    LstEsquemas = new ObservableCollection<Esquema>(api.lData);
                else
                    await MostrarError(api?.Error?.sDetails ?? "No se pudieron cargar los esquemas.");
            }
            catch (Exception ex)
            {
                await MostrarError($"Cargar: {ex.Message}");
            }
            finally
            {
                BLoading = false;
            }
        }

        [RelayCommand]
        private async Task Nuevo()
        {
            await Shell.Current.GoToAsync("datosEsquema");
        }

        // ✅ Editar = abrir DatosEsquema con el esquema seleccionado
        [RelayCommand]
        private async Task Editar(Esquema esquema)
        {
            if (esquema == null) return;

            await Shell.Current.GoToAsync("datosEsquema", new Dictionary<string, object>
            {
                { "oEsquema", esquema }
            });
        }

        // ✅ Eliminar = DELETE api/esquemas/{id}
        [RelayCommand]
        private async Task Eliminar(Esquema esquema)
        {
            try
            {
                if (esquema == null || string.IsNullOrWhiteSpace(esquema.sIdMongo))
                {
                    await MostrarError("Esquema inválido.");
                    return;
                }

                var main = Application.Current?.Windows[0].Page;
                if (main == null) return;

                bool confirm = await main.DisplayAlert(
                    "Eliminar esquema",
                    $"¿Eliminar el esquema \"{esquema.sNombre}\"?",
                    "Sí",
                    "No"
                );

                if (!confirm) return;

                BLoading = true;

                var resp = await _http.DeleteAsync($"api/esquemas/{esquema.sIdMongo}", bRequiereToken: true);
                if (resp == null) return;

                var api = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Esquema>>();

                if (resp.IsSuccessStatusCode && api != null && api.bSuccess)
                {
                    // quita de la lista local para que se vea inmediato
                    var item = LstEsquemas.FirstOrDefault(x => x.sIdMongo == esquema.sIdMongo);
                    if (item != null) LstEsquemas.Remove(item);

                    await MostrarOk("Esquema eliminado.");
                }
                else
                {
                    await MostrarError(api?.Error?.sDetails ?? "No se pudo eliminar el esquema.");
                }
            }
            catch (Exception ex)
            {
                await MostrarError($"Eliminar: {ex.Message}");
            }
            finally
            {
                BLoading = false;
            }
        }

        private static async Task MostrarError(string msg)
        {
            var mainPage = Application.Current?.Windows[0].Page;
            if (mainPage != null)
                await mainPage.DisplayAlert("Error", msg, "OK");
        }

        private static async Task MostrarOk(string msg)
        {
            var mainPage = Application.Current?.Windows[0].Page;
            if (mainPage != null)
                await mainPage.DisplayAlert("OK", msg, "OK");
        }
    }
}

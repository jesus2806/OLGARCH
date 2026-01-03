using AppGestorVentas.Models;
using AppGestorVentas.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace AppGestorVentas.ViewModels.IngredienteViewModels
{
    public partial class ListaIngredientesViewModel : ObservableObject
    {
        private readonly HttpApiService _httpApiService;

        [ObservableProperty]
        private ObservableCollection<Ingrediente> lstIngredientes = new();

        [ObservableProperty]
        private string sTextoBusqueda = string.Empty;

        [ObservableProperty]
        private bool bIsBusy;

        public ListaIngredientesViewModel(HttpApiService httpApiService)
        {
            _httpApiService = httpApiService;
        }

        [RelayCommand]
        public async Task CargarIngredientesAsync()
        {
            string sMensajeError = string.Empty;
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    await MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }

                BIsBusy = true;

                HttpResponseMessage? resp = await _httpApiService.GetAsync("api/ingredientes", bRequiereToken: true);
                if (resp != null)
                {
                    var api = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Ingrediente>>();
                    if (resp.IsSuccessStatusCode && api != null && api.bSuccess)
                    {
                        LstIngredientes = new ObservableCollection<Ingrediente>(api.lData ?? new List<Ingrediente>());
                    }
                    else
                    {
                        sMensajeError = api?.Error?.sDetails ?? "Error al cargar ingredientes.";
                    }
                }
            }
            catch (Exception ex)
            {
                sMensajeError = $"{ex.Message}\n{ex.StackTrace}";
            }
            finally
            {
                BIsBusy = false;
            }

            if (!string.IsNullOrWhiteSpace(sMensajeError))
                await MostrarError($"ERROR: {sMensajeError}");
        }

        [RelayCommand]
        public async Task BuscarAsync()
        {
            string sMensajeError = string.Empty;
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    await MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }

                BIsBusy = true;

                var body = new
                {
                    texto = STextoBusqueda,
                    // unidad = "",       // si luego lo quieres
                    // bajoStock = false  // si luego lo quieres
                };

                HttpResponseMessage? resp = await _httpApiService.PostAsync("api/ingredientes/search", body, bRequiereToken: true);
                if (resp != null)
                {
                    var api = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Ingrediente>>();
                    if (resp.IsSuccessStatusCode && api != null && api.bSuccess)
                    {
                        LstIngredientes = new ObservableCollection<Ingrediente>(api.lData ?? new List<Ingrediente>());
                    }
                    else
                    {
                        sMensajeError = api?.Error?.sDetails ?? "Error al buscar ingredientes.";
                    }
                }
            }
            catch (Exception ex)
            {
                sMensajeError = $"{ex.Message}\n{ex.StackTrace}";
            }
            finally
            {
                BIsBusy = false;
            }

            if (!string.IsNullOrWhiteSpace(sMensajeError))
                await MostrarError($"ERROR: {sMensajeError}");
        }

        [RelayCommand]
        public async Task NuevoAsync()
        {
            await Shell.Current.GoToAsync("datosIngredientes");
        }

        [RelayCommand]
        public async Task EditarAsync(Ingrediente? ingrediente)
        {
            if (ingrediente == null) return;

            var nav = new Dictionary<string, object>
        {
            { "oIngrediente", ingrediente }
        };

            await Shell.Current.GoToAsync("datosIngredientes", nav);
        }


        [RelayCommand]
        public async Task EliminarAsync(Ingrediente? ingrediente)
        {
            if (ingrediente == null || string.IsNullOrWhiteSpace(ingrediente.sIdMongo)) return;

            bool confirmar = await (Application.Current?.Windows[0].Page?.DisplayAlert(
                "Eliminar",
                $"¿Eliminar ingrediente: {ingrediente.sNombre}?",
                "Sí", "No") ?? Task.FromResult(false));

            if (!confirmar) return;

            string sMensajeError = string.Empty;

            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    await MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }

                BIsBusy = true;

                HttpResponseMessage? resp = await _httpApiService.DeleteAsync($"api/ingredientes/{ingrediente.sIdMongo}", bRequiereToken: true);
                if (resp != null)
                {
                    var api = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Ingrediente>>();
                    if (resp.IsSuccessStatusCode && api != null && api.bSuccess)
                    {
                        // Quitar de la lista
                        var item = LstIngredientes.FirstOrDefault(x => x.sIdMongo == ingrediente.sIdMongo);
                        if (item != null) LstIngredientes.Remove(item);
                    }
                    else
                    {
                        sMensajeError = api?.Error?.sDetails ?? "Error al eliminar ingrediente.";
                    }
                }
            }
            catch (Exception ex)
            {
                sMensajeError = $"{ex.Message}\n{ex.StackTrace}";
            }
            finally
            {
                BIsBusy = false;
            }

            if (!string.IsNullOrWhiteSpace(sMensajeError))
                await MostrarError($"ERROR: {sMensajeError}");
        }

        private static async Task MostrarError(string sMensaje)
        {
            var mainPage = Application.Current?.Windows[0].Page;
            if (mainPage != null)
                await mainPage.DisplayAlert("Error", sMensaje, "OK");
        }
    }
}

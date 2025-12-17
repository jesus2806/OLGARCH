using AppGestorVentas.Models;
using AppGestorVentas.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace AppGestorVentas.ViewModels.OrdenViewModels
{
    /// <summary>
    /// ViewModel para la Pantalla 3: Búsqueda y selección de extras
    /// </summary>
    public partial class BuscarExtrasViewModel : ObservableObject, IQueryAttributable
    {
        #region PROPIEDADES

        private readonly HttpApiService _httpApiService;

        private string _sIdOrdenProducto = string.Empty;
        private string _sIdOrden = string.Empty;
        private int _iCantidadConsumos;

        [ObservableProperty]
        private string sBusqueda = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Extra> lstExtras = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool noHayResultados;

        // Para el modal de selección de consumos (Pantalla 4)
        [ObservableProperty]
        private bool mostrarModalConsumos;

        [ObservableProperty]
        private Extra extraSeleccionado;

        [ObservableProperty]
        private ObservableCollection<ConsumoSeleccion> lstConsumosSeleccion = new();

        #endregion

        #region CONSTRUCTOR

        public BuscarExtrasViewModel(HttpApiService httpApiService)
        {
            _httpApiService = httpApiService;
        }

        #endregion

        #region NAVEGACIÓN

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("sIdOrdenProducto", out var idProducto) && idProducto != null)
            {
                _sIdOrdenProducto = idProducto.ToString() ?? string.Empty;
            }

            if (query.TryGetValue("sIdOrden", out var idOrden) && idOrden != null)
            {
                _sIdOrden = idOrden.ToString() ?? string.Empty;
            }

            if (query.TryGetValue("iCantidad", out var cantidad) && cantidad != null)
            {
                _iCantidadConsumos = Convert.ToInt32(cantidad);
                
                // Inicializar lista de consumos para selección
                LstConsumosSeleccion.Clear();
                for (int i = 1; i <= _iCantidadConsumos; i++)
                {
                    LstConsumosSeleccion.Add(new ConsumoSeleccion
                    {
                        iIndex = i,
                        sNombre = $"Consumo {i}",
                        IsSelected = true // Por defecto todos seleccionados
                    });
                }
            }
        }

        #endregion

        #region BÚSQUEDA

        partial void OnSBusquedaChanged(string value)
        {
            // Búsqueda automática con debounce
            _ = BuscarExtrasAsync();
        }

        private async Task BuscarExtrasAsync()
        {
            if (string.IsNullOrWhiteSpace(SBusqueda) || SBusqueda.Length < 2)
            {
                LstExtras.Clear();
                NoHayResultados = false;
                return;
            }

            IsLoading = true;
            NoHayResultados = false;

            try
            {
                var response = await _httpApiService.PostAsync($"api/extras/search",
                    new { texto = SBusqueda });

                if (response != null && response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<Extra>>();

                    LstExtras.Clear();

                    if (apiResponse != null && apiResponse.bSuccess && apiResponse.lData != null)
                    {
                        foreach (var extra in apiResponse.lData)
                        {
                            LstExtras.Add(extra);
                        }
                    }

                    NoHayResultados = LstExtras.Count == 0;
                }
            }
            catch (Exception ex)
            {
                await MostrarError($"Error en la búsqueda: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region COMANDOS

        [RelayCommand]
        private async Task SeleccionarExtra(Extra extra)
        {
            if (extra == null) return;

            ExtraSeleccionado = extra;

            // Si solo hay 1 consumo, agregar directamente sin mostrar modal
            if (_iCantidadConsumos == 1)
            {
                await AgregarExtraAConsumos(new List<int> { 1 });
            }
            else
            {
                // Mostrar modal para seleccionar consumos (Pantalla 4)
                // Reiniciar selección
                foreach (var consumo in LstConsumosSeleccion)
                {
                    consumo.IsSelected = true;
                }
                MostrarModalConsumos = true;
            }
        }

        [RelayCommand]
        private void CerrarModal()
        {
            MostrarModalConsumos = false;
            ExtraSeleccionado = null;
        }

        [RelayCommand]
        private async Task ConfirmarAgregarExtra()
        {
            var consumosSeleccionados = LstConsumosSeleccion
                .Where(c => c.IsSelected)
                .Select(c => c.iIndex)
                .ToList();

            if (consumosSeleccionados.Count == 0)
            {
                await MostrarError("Debes seleccionar al menos un consumo.");
                return;
            }

            await AgregarExtraAConsumos(consumosSeleccionados);
        }

        private async Task AgregarExtraAConsumos(List<int> indices)
        {
            if (ExtraSeleccionado == null) return;

            IsLoading = true;
            MostrarModalConsumos = false;

            try
            {
                var requestBody = new
                {
                    extra = new
                    {
                        sIdExtra = ExtraSeleccionado.sIdMongo,
                        sNombre = ExtraSeleccionado.sNombre,
                        iCostoReal = ExtraSeleccionado.iCostoReal,
                        iCostoPublico = ExtraSeleccionado.iCostoPublico,
                        sURLImagen = ExtraSeleccionado.sURLImagen
                    },
                    aIndexConsumos = indices
                };

                var response = await _httpApiService.PostAsync(
                    $"api/orden-productos/{_sIdOrdenProducto}/consumos/extras",
                    requestBody);

                if (response != null && response.IsSuccessStatusCode)
                {
                    await Shell.Current.DisplayAlert("Éxito", 
                        $"Extra '{ExtraSeleccionado.sNombre}' agregado correctamente.", "OK");
                    
                    // Regresar a la pantalla de consumos
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    var content = response != null ? await response.Content.ReadAsStringAsync() : "Sin respuesta";
                    await MostrarError($"Error al agregar el extra: {content}");
                }
            }
            catch (Exception ex)
            {
                await MostrarError($"Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                ExtraSeleccionado = null;
            }
        }

        [RelayCommand]
        private async Task Regresar()
        {
            await Shell.Current.GoToAsync("..");
        }

        #endregion

        #region HELPERS

        private async Task MostrarError(string mensaje)
        {
            await Shell.Current.DisplayAlert("Error", mensaje, "OK");
        }

        #endregion
    }

    #region CLASES AUXILIARES

    /// <summary>
    /// Modelo para selección de consumos en el modal
    /// </summary>
    public partial class ConsumoSeleccion : ObservableObject
    {
        public int iIndex { get; set; }
        public string sNombre { get; set; } = string.Empty;

        [ObservableProperty]
        private bool isSelected;
    }

    #endregion
}

using AppGestorVentas.Models;
using AppGestorVentas.Services;
using AppGestorVentas.ViewModels.Popup;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace AppGestorVentas.ViewModels.OrdenViewModels
{
    /// <summary>
    /// ViewModel para la Pantalla 2: Administración de consumos individuales y sus extras.
    /// Cada consumo representa UNA unidad del producto (ej: si hay 3 chilaquiles, se muestran 3 consumos).
    /// </summary>
    public partial class ConsumosProductoViewModel : ObservableObject, IQueryAttributable
    {
        #region PROPIEDADES

        private readonly HttpApiService _httpApiService;
        private readonly IPopupService _popupService;

        private string _sIdOrdenProducto = string.Empty;
        private string _sIdOrden = string.Empty;

        [ObservableProperty]
        private string sNombreProducto = string.Empty;

        [ObservableProperty]
        private int iCantidad;

        [ObservableProperty]
        private decimal iTotalExtras;

        [ObservableProperty]
        private ObservableCollection<ConsumoDisplay> lstConsumos = new();

        [ObservableProperty]
        private bool isLoading;

        // Propiedad para acceder al ID del producto desde la vista
        public string IdOrdenProducto => _sIdOrdenProducto;

        #endregion

        #region CONSTRUCTOR

        public ConsumosProductoViewModel(HttpApiService httpApiService, IPopupService popupService)
        {
            _httpApiService = httpApiService;
            _popupService = popupService;
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

            if (query.TryGetValue("sNombreProducto", out var nombre) && nombre != null)
            {
                SNombreProducto = nombre.ToString() ?? string.Empty;
            }

            if (query.TryGetValue("iCantidad", out var cantidad) && cantidad != null)
            {
                ICantidad = Convert.ToInt32(cantidad);
            }
        }

        #endregion

        #region CARGAR DATOS

        public async Task LoadDataAsync()
        {
            if (string.IsNullOrEmpty(_sIdOrdenProducto))
            {
                await MostrarError("No se especificó el producto.");
                return;
            }

            IsLoading = true;

            try
            {
                // Obtener datos del producto desde la API
                var response = await _httpApiService.GetAsync($"api/orden-productos/{_sIdOrdenProducto}");

                if (response != null && response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<OrdenProducto>>();

                    if (apiResponse != null && apiResponse.bSuccess && apiResponse.lData?.Count > 0)
                    {
                        var producto = apiResponse.lData[0];
                        
                        // Actualizar cantidad si viene del API
                        if (producto.iCantidad > 0)
                        {
                            ICantidad = producto.iCantidad;
                        }

                        // Generar la lista de consumos basándose en la cantidad
                        GenerarConsumos(producto.aConsumos);
                        
                        // Calcular total de extras
                        CalcularTotalExtras();
                    }
                    else
                    {
                        // Si no hay datos del API, generar consumos vacíos basados en la cantidad
                        GenerarConsumosVacios();
                    }
                }
                else
                {
                    // Si falla el API, generar consumos vacíos basados en la cantidad recibida
                    GenerarConsumosVacios();
                }
            }
            catch (Exception ex)
            {
                await MostrarError($"Error: {ex.Message}");
                // En caso de error, mostrar consumos vacíos
                GenerarConsumosVacios();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Genera los consumos basándose en la cantidad del producto.
        /// Si existen consumos con extras del backend, los mapea; si no, crea consumos vacíos.
        /// </summary>
        private void GenerarConsumos(List<Consumo> consumosBackend)
        {
            LstConsumos.Clear();

            // Crear un diccionario para acceso rápido a los consumos del backend por índice
            var consumosDict = new Dictionary<int, Consumo>();
            if (consumosBackend != null)
            {
                foreach (var c in consumosBackend)
                {
                    consumosDict[c.iIndex] = c;
                }
            }

            // Generar un consumo por cada unidad del producto
            for (int i = 1; i <= ICantidad; i++)
            {
                var consumoDisplay = new ConsumoDisplay
                {
                    iIndex = i,
                    sDisplayName = $"Consumo {i}",
                    Extras = new ObservableCollection<ExtraConsumoDisplay>()
                };

                // Si existe información del backend para este consumo, cargar sus extras
                if (consumosDict.TryGetValue(i, out var consumoBackend) && consumoBackend.aExtras != null)
                {
                    foreach (var extra in consumoBackend.aExtras)
                    {
                        consumoDisplay.Extras.Add(new ExtraConsumoDisplay
                        {
                            sIdExtra = extra.sIdExtra ?? extra.sIdMongo ?? string.Empty,
                            sNombre = extra.sNombre,
                            iCostoPublico = extra.iCostoPublico,
                            sURLImagen = extra.sURLImagen
                        });
                    }
                }

                consumoDisplay.ITotalExtrasConsumo = consumoDisplay.Extras.Sum(e => e.iCostoPublico);
                LstConsumos.Add(consumoDisplay);
            }
        }

        /// <summary>
        /// Genera consumos vacíos cuando no hay datos del backend.
        /// </summary>
        private void GenerarConsumosVacios()
        {
            LstConsumos.Clear();

            for (int i = 1; i <= ICantidad; i++)
            {
                LstConsumos.Add(new ConsumoDisplay
                {
                    iIndex = i,
                    sDisplayName = $"Consumo {i}",
                    Extras = new ObservableCollection<ExtraConsumoDisplay>(),
                    ITotalExtrasConsumo = 0
                });
            }
        }

        /// <summary>
        /// Calcula el total de extras de todos los consumos.
        /// </summary>
        private void CalcularTotalExtras()
        {
            ITotalExtras = LstConsumos.Sum(c => c.ITotalExtrasConsumo);
        }

        #endregion

        #region COMANDOS

        [RelayCommand]
        private async Task IrAgregarExtra()
        {
            await Shell.Current.GoToAsync("buscarExtras", new Dictionary<string, object>
            {
                { "sIdOrdenProducto", _sIdOrdenProducto },
                { "sIdOrden", _sIdOrden },
                { "iCantidad", ICantidad }
            });
        }

        [RelayCommand]
        private async Task EliminarExtra(ExtraConsumoParams param)
        {
            if (param == null) return;

            bool confirmar = await Shell.Current.DisplayAlert(
                "Confirmar eliminación",
                $"¿Deseas eliminar el extra '{param.sNombreExtra}' del Consumo {param.iIndexConsumo}?",
                "Sí", "No");

            if (!confirmar) return;

            IsLoading = true;

            try
            {
                var response = await _httpApiService.DeleteAsync(
                    $"api/orden-productos/{_sIdOrdenProducto}/consumos/{param.iIndexConsumo}/extras/{param.sIdExtra}");

                if (response != null && response.IsSuccessStatusCode)
                {
                    // Recargar datos para reflejar los cambios
                    await LoadDataAsync();
                }
                else
                {
                    await MostrarError("Error al eliminar el extra.");
                }
            }
            catch (Exception ex)
            {
                await MostrarError($"Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
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
    /// Modelo para mostrar un consumo en la UI.
    /// Representa UNA unidad del producto.
    /// </summary>
    public partial class ConsumoDisplay : ObservableObject
    {
        public int iIndex { get; set; }
        public string sDisplayName { get; set; } = string.Empty;
        
        [ObservableProperty]
        private ObservableCollection<ExtraConsumoDisplay> extras = new();
        
        [ObservableProperty]
        private decimal iTotalExtrasConsumo;
        
        public bool TieneExtras => Extras?.Count > 0;
    }

    /// <summary>
    /// Modelo para mostrar un extra en la UI.
    /// </summary>
    public partial class ExtraConsumoDisplay : ObservableObject
    {
        public string sIdExtra { get; set; } = string.Empty;
        public string sNombre { get; set; } = string.Empty;
        public decimal iCostoPublico { get; set; }
        public string sURLImagen { get; set; } = string.Empty;
        public string sPrecioFormateado => $"${iCostoPublico:N2} MXN";
    }

    /// <summary>
    /// Parámetros para eliminar un extra de un consumo específico.
    /// </summary>
    public class ExtraConsumoParams
    {
        public int iIndexConsumo { get; set; }
        public string sIdExtra { get; set; } = string.Empty;
        public string sNombreExtra { get; set; } = string.Empty;
    }

    #endregion
}

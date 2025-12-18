using AppGestorVentas.Models;
using AppGestorVentas.Services;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace AppGestorVentas.ViewModels.OrdenViewModels
{
    /// <summary>
    /// ViewModel para administrar consumos y extras de un producto.
    /// Los cambios se guardan LOCALMENTE hasta confirmar la orden.
    /// </summary>
    public partial class ConsumosProductoViewModel : ObservableObject, IQueryAttributable
    {
        #region SERVICIOS

        private readonly OrdenDraftService _ordenDraftService;
        private readonly IPopupService _popupService;
        private readonly HttpApiService _httpApiService;
        #endregion

        #region PROPIEDADES

        private string _sIdProducto = string.Empty;
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

        public string IdProducto => _sIdProducto;

        #endregion

        #region CONSTRUCTOR

        public ConsumosProductoViewModel(
            OrdenDraftService ordenDraftService,
            IPopupService popupService,
            HttpApiService httpApiService)
        {
            _ordenDraftService = ordenDraftService;
            _popupService = popupService;
            _httpApiService = httpApiService;
        }

        #endregion

        #region NAVEGACIÓN

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("sIdOrdenProducto", out var idProducto) && idProducto != null)
            {
                _sIdProducto = idProducto.ToString() ?? string.Empty;
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
            if (string.IsNullOrEmpty(_sIdProducto))
            {
                await MostrarError("No se especificó el producto.");
                return;
            }

            IsLoading = true;

            try
            {
                // 1. Primero intentar obtener del borrador local (orden en edición)
                var producto = _ordenDraftService.ObtenerProductoPorIdMongo(_sIdProducto)
                            ?? _ordenDraftService.ObtenerProducto(_sIdProducto);

                if (producto != null)
                {
                    // Usar datos del borrador local
                    ICantidad = producto.iCantidad;
                    SNombreProducto = producto.sNombre;
                    GenerarConsumos(producto.aConsumos);
                    CalcularTotalExtras();
                }
                else
                {
                    // 2. Si no está en borrador, consultar al backend (orden ya tomada)
                    await CargarDesdeBackendAsync();
                }
            }
            catch (Exception ex)
            {
                await MostrarError($"Error: {ex.Message}");
                GenerarConsumosVacios();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Carga los datos del producto desde el backend (MongoDB)
        /// </summary>
        private async Task CargarDesdeBackendAsync()
        {
            try
            {
                // Obtener el producto desde el backend
                var response = await _httpApiService.GetAsync($"api/orden-productos/{_sIdProducto}");

                if (response != null && response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<OrdenProducto>>();

                    if (apiResponse?.bSuccess == true && apiResponse.lData?.Count > 0)
                    {
                        var producto = apiResponse.lData[0];

                        ICantidad = producto.iCantidad;
                        SNombreProducto = producto.sNombre;

                        // Generar consumos desde los datos del backend
                        GenerarConsumos(producto.aConsumos);
                        CalcularTotalExtras();
                        return;
                    }
                }

                // Si no se encontró, generar vacíos
                GenerarConsumosVacios();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error al cargar desde backend: {ex.Message}");
                GenerarConsumosVacios();
            }
        }

        private void GenerarConsumos(List<Consumo>? consumosBackend)
        {
            LstConsumos.Clear();

            var consumosDict = new Dictionary<int, Consumo>();
            if (consumosBackend != null)
            {
                foreach (var c in consumosBackend)
                {
                    consumosDict[c.iIndex] = c;
                }
            }

            for (int i = 1; i <= ICantidad; i++)
            {
                var consumoDisplay = new ConsumoDisplay
                {
                    iIndex = i,
                    sDisplayName = $"Consumo {i}",
                    Extras = new ObservableCollection<ExtraConsumoDisplay>()
                };

                if (consumosDict.TryGetValue(i, out var consumo) && consumo.aExtras != null)
                {
                    foreach (var extra in consumo.aExtras)
                    {
                        consumoDisplay.Extras.Add(new ExtraConsumoDisplay
                        {
                            iIndexConsumo = i,
                            sIdExtraSubdoc = extra.sIdMongo ?? extra.sIdLocal,
                            sIdExtra = extra.sIdExtra,
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
                { "sIdOrdenProducto", _sIdProducto },
                { "sIdOrden", _sIdOrden },
                { "iCantidad", ICantidad }
            });
        }

        [RelayCommand]
        private async Task EliminarExtra(ExtraConsumoDisplay extra)
        {
            if (extra == null) return;

            bool confirmar = await Shell.Current.DisplayAlert(
                "Confirmar eliminación",
                $"¿Deseas eliminar el extra '{extra.sNombre}' del Consumo {extra.iIndexConsumo}?",
                "Sí", "No");

            if (!confirmar) return;

            IsLoading = true;
            try
            {
                // Eliminar localmente usando el servicio
                await _ordenDraftService.EliminarExtraDeConsumoAsync(
                    _sIdProducto,
                    extra.iIndexConsumo,
                    extra.sIdExtraSubdoc);

                // Recargar datos
                await LoadDataAsync();

                await Shell.Current.DisplayAlert("OK", "Extra eliminado. Recuerda guardar los cambios.", "OK");
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

        [ObservableProperty]
        private bool bModoDecremento;

        [RelayCommand]
        private async Task EliminarConsumo(ConsumoDisplay consumo)
        {
            if (consumo == null) return;

            string msg = consumo.TieneExtras
                ? $"El Consumo {consumo.iIndex} tiene extras. Al eliminar el consumo se eliminarán también sus extras. ¿Continuar?"
                : $"¿Deseas eliminar el Consumo {consumo.iIndex}?";

            bool confirmar = await Shell.Current.DisplayAlert("Eliminar consumo", msg, "Sí", "No");
            if (!confirmar) return;

            IsLoading = true;
            try
            {
                // Eliminar localmente usando el servicio
                await _ordenDraftService.EliminarConsumoAsync(_sIdProducto, consumo.iIndex);

                // Actualizar cantidad local
                var producto = _ordenDraftService.ObtenerProductoPorIdMongo(_sIdProducto) 
                            ?? _ordenDraftService.ObtenerProducto(_sIdProducto);
                
                if (producto != null)
                {
                    ICantidad = producto.iCantidad;
                }

                // Recargar datos
                await LoadDataAsync();

                await Shell.Current.DisplayAlert("OK", "Consumo eliminado. Recuerda guardar los cambios.", "OK");

                if (BModoDecremento || ICantidad == 0)
                {
                    await Shell.Current.GoToAsync("..");
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

        #endregion

        #region HELPERS

        private async Task MostrarError(string mensaje)
        {
            await Shell.Current.DisplayAlert("Error", mensaje, "OK");
        }

        #endregion
    }

    #region CLASES AUXILIARES

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

    public partial class ExtraConsumoDisplay : ObservableObject
    {
        public int iIndexConsumo { get; set; }
        public string sIdExtraSubdoc { get; set; } = string.Empty;
        public string sIdExtra { get; set; } = string.Empty;
        public string sNombre { get; set; } = string.Empty;
        public decimal iCostoPublico { get; set; }
        public string sURLImagen { get; set; } = string.Empty;
        public string sPrecioFormateado => $"${iCostoPublico:N2} MXN";
    }

    public class ExtraConsumoParams
    {
        public int iIndexConsumo { get; set; }
        public string sIdExtra { get; set; } = string.Empty;
        public string sNombreExtra { get; set; } = string.Empty;
    }

    #endregion
}

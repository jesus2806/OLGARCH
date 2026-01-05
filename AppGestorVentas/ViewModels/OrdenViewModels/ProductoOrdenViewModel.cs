using AppGestorVentas.Helpers;
using AppGestorVentas.Models;
using AppGestorVentas.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace AppGestorVentas.ViewModels.OrdenViewModels
{
    /// <summary>
    /// ViewModel para agregar/editar productos de una orden.
    /// Los cambios se guardan LOCALMENTE hasta confirmar la orden.
    /// </summary>
    public partial class ProductoOrdenViewModel : ObservableObject, IQueryAttributable
    {
        #region SERVICIOS

        private readonly HttpApiService _httpApiService;
        private readonly OrdenDraftService _ordenDraftService;

        #endregion

        #region PROPIEDADES

        private string _sIdOrden = string.Empty;

        // Para saber si se trata de una actualización (edición)
        [ObservableProperty]
        private bool esEdicion = false;

        // ID local del producto cuando se edita
        [ObservableProperty]
        private string sIdProductoLocal = string.Empty;

        // 1. Buscador principal (platillos/bebidas)
        [ObservableProperty]
        private string sBusquedaPrincipal = string.Empty;

        // Resultados de la búsqueda principal
        [ObservableProperty]
        private ObservableCollection<Producto> lstResultadosProductos = new();

        // 2. Producto seleccionado (solo 1)
        [ObservableProperty]
        private Producto? oProductoSeleccionado;

        // La variante seleccionada
        [ObservableProperty]
        private Variante? varianteSeleccionada;

        // 3. Buscador de extras
        [ObservableProperty]
        private string sBusquedaExtras = string.Empty;

        // Resultados de búsqueda de extras
        [ObservableProperty]
        private ObservableCollection<Producto> lstResultadosExtras = new();

        // 4. Extras seleccionados
        [ObservableProperty]
        private ObservableCollection<Producto> lstExtrasSeleccionados = new();

        // 5. Indicaciones
        [ObservableProperty]
        private string sIndicaciones = string.Empty;

        [ObservableProperty]
        private bool bNoHayUnoProductoSeleccionado = true;

        // Propiedades para el título de la página y el texto del botón de confirmación
        public string PageTitle => EsEdicion ? "Actualizar Producto" : "Agregar Producto";
        public string ConfirmButtonText => EsEdicion ? "Actualizar" : "Agregar";

        #endregion

        #region CONSTRUCTOR

        public ProductoOrdenViewModel(HttpApiService apiService, OrdenDraftService ordenDraftService)
        {
            _httpApiService = apiService;
            _ordenDraftService = ordenDraftService;
        }

        #endregion

        #region NAVEGACIÓN

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            // ID de la orden (puede ser local o MongoDB)
            if (query.TryGetValue("sIdOrden", out var sIdOrden) && sIdOrden != null)
            {
                _sIdOrden = sIdOrden.ToString() ?? string.Empty;
            }

            // Si se pasa un parámetro "ordenProducto" significa que se quiere editar
            if (query.TryGetValue("ordenProducto", out var ordenProductoParam) && ordenProductoParam != null)
            {
                try
                {
                    OrdenProducto? ordenProducto = null;

                    if (ordenProductoParam is string jsonString)
                    {
                        ordenProducto = JsonSerializer.Deserialize<OrdenProducto>(jsonString);
                    }
                    else if (ordenProductoParam is OrdenProducto op)
                    {
                        ordenProducto = op;
                    }

                    if (ordenProducto == null)
                    {
                        MostrarError("No se pudo interpretar el parámetro de edición.");
                        return;
                    }

                    // Activamos el modo edición
                    EsEdicion = true;
                    SIdProductoLocal = ordenProducto.sIdLocal;

                    // Convertir el OrdenProducto a un Producto para reutilizar la vista
                    OProductoSeleccionado = new Producto
                    {
                        sIdMongo = ordenProducto.sIdProductoMongoDB,
                        sNombre = ordenProducto.sNombre,
                        iCostoReal = ordenProducto.iCostoReal,
                        iCostoPublico = ordenProducto.iCostoPublico,
                        iTipoProducto = ordenProducto.iTipoProducto,
                        aImagenes = new List<Imagen>
                        {
                            new Imagen { sURLImagen = ordenProducto.sURLImagen }
                        },
                        aVariantes = ordenProducto.aVariantes
                    };

                    SIndicaciones = ordenProducto.sIndicaciones;

                    // Seleccionar la variante actual
                    if (OProductoSeleccionado.aVariantes?.Count > ordenProducto.iIndexVarianteSeleccionada)
                    {
                        VarianteSeleccionada = OProductoSeleccionado.aVariantes[ordenProducto.iIndexVarianteSeleccionada];
                    }

                    // Cargar extras
                    LstExtrasSeleccionados.Clear();
                    if (ordenProducto.aExtras != null)
                    {
                        foreach (var extra in ordenProducto.aExtras)
                        {
                            LstExtrasSeleccionados.Add(new Producto
                            {
                                sIdMongo = extra.sIdExtra,
                                sNombre = extra.sNombre,
                                iCostoReal = extra.iCostoReal,
                                iCostoPublico = extra.iCostoPublico,
                                aImagenes = new List<Imagen>
                                {
                                    new Imagen { sURLImagen = extra.sURLImagen }
                                }
                            });
                        }
                    }

                    BNoHayUnoProductoSeleccionado = false;
                    LstResultadosProductos.Clear();

                    OnPropertyChanged(nameof(PageTitle));
                    OnPropertyChanged(nameof(ConfirmButtonText));
                }
                catch (Exception ex)
                {
                    MostrarError($"Error al procesar parámetros de edición: {ex.Message}");
                }
            }
        }

        #endregion

        #region BÚSQUEDA DE PRODUCTOS

        partial void OnSBusquedaPrincipalChanged(string value)
        {
            _ = BuscarProductosAsync();
        }

        [RelayCommand]
        public async Task BuscarProductosAsync()
        {
            if (string.IsNullOrWhiteSpace(SBusquedaPrincipal) || SBusquedaPrincipal.Length < 2)
            {
                LstResultadosProductos.Clear();
                return;
            }

            try
            {
                var response = await _httpApiService.PostAsync("api/productos/search",
                    new { texto = SBusquedaPrincipal });

                if (response != null && response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<Producto>>();

                    LstResultadosProductos.Clear();

                    if (apiResponse?.bSuccess == true && apiResponse.lData != null)
                    {
                        foreach (var producto in apiResponse.lData)
                        {
                            LstResultadosProductos.Add(producto);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error en búsqueda: {ex.Message}");
            }
        }

        [RelayCommand]
        public void SeleccionarProducto(Producto producto)
        {
            if (producto == null) return;

            OProductoSeleccionado = producto;
            BNoHayUnoProductoSeleccionado = false;
            LstResultadosProductos.Clear();
            SBusquedaPrincipal = string.Empty;

            // Seleccionar primera variante por defecto
            if (producto.aVariantes?.Count > 0)
            {
                VarianteSeleccionada = producto.aVariantes[0];
            }
        }

        [RelayCommand]
        public void LimpiarSeleccionProducto()
        {
            OProductoSeleccionado = null;
            VarianteSeleccionada = null;
            BNoHayUnoProductoSeleccionado = true;
            LstExtrasSeleccionados.Clear();
            SIndicaciones = string.Empty;
        }

        [RelayCommand]
        public void QuitarProducto()
        {
            try
            {
                if (OProductoSeleccionado != null)
                {
                    var producto = LstResultadosProductos.FirstOrDefault(p => p.sIdMongo == OProductoSeleccionado.sIdMongo);
                    if (producto != null)
                    {
                        producto.isSeleccionado = false;
                    }
                    OProductoSeleccionado = null;
                    VarianteSeleccionada = null;
                    SIndicaciones = string.Empty;
                    LstExtrasSeleccionados.Clear();
                    BNoHayUnoProductoSeleccionado = true;
                    SBusquedaExtras = string.Empty;
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Ocurrió un error al quitar el producto: {ex.Message}");
            }
        }

        #endregion

        #region BÚSQUEDA DE EXTRAS

        partial void OnSBusquedaExtrasChanged(string value)
        {
            _ = BuscarExtrasAsync();
        }

        [RelayCommand]
        public async Task BuscarExtrasAsync()
        {
            if (string.IsNullOrWhiteSpace(SBusquedaExtras) || SBusquedaExtras.Length < 2)
            {
                LstResultadosExtras.Clear();
                return;
            }

            try
            {
                var response = await _httpApiService.PostAsync("api/productos/search",
                    new { texto = SBusquedaExtras, iTipoProducto = 3 }); // Tipo 3 = extras

                if (response != null && response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<Producto>>();

                    LstResultadosExtras.Clear();

                    if (apiResponse?.bSuccess == true && apiResponse.lData != null)
                    {
                        foreach (var extra in apiResponse.lData)
                        {
                            // No mostrar extras ya seleccionados
                            if (!LstExtrasSeleccionados.Any(e => e.sIdMongo == extra.sIdMongo))
                            {
                                LstResultadosExtras.Add(extra);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error en búsqueda de extras: {ex.Message}");
            }
        }

        [RelayCommand]
        public void AgregarExtra(Producto extra)
        {
            if (extra == null) return;

            if (!LstExtrasSeleccionados.Any(e => e.sIdMongo == extra.sIdMongo))
            {
                LstExtrasSeleccionados.Add(extra);
            }

            LstResultadosExtras.Clear();
            SBusquedaExtras = string.Empty;
        }

        [RelayCommand]
        public void EliminarExtra(Producto extra)
        {
            if (extra == null) return;
            LstExtrasSeleccionados.Remove(extra);
        }

        #endregion

        #region CONFIRMAR - GUARDAR LOCALMENTE

        [RelayCommand]
        public async Task ConfirmarAsync()
        {
            try
            {
                // Validaciones
                if (OProductoSeleccionado == null)
                {
                    await MostrarAlertaAsync("Validación", "Debes seleccionar un platillo o bebida.");
                    return;
                }

                if (VarianteSeleccionada == null)
                {
                    await MostrarAlertaAsync("Validación", "Debes elegir la variante del producto.");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(SIndicaciones) && !EntryValidations.IsValidText(SIndicaciones))
                {
                    await MostrarAlertaAsync("Validación", "El campo 'Indicaciones' no admite caracteres especiales.");
                    return;
                }

                // Construir lista de extras
                var lstExtras = LstExtrasSeleccionados.Select(e => new ExtraOrdenProducto
                {
                    sIdLocal = Guid.NewGuid().ToString(),
                    sIdExtra = e.sIdMongo,
                    sNombre = e.sNombre,
                    iCostoReal = e.iCostoReal,
                    iCostoPublico = e.iCostoPublico,
                    sURLImagen = e.aImagenes?.FirstOrDefault()?.sURLImagen ?? string.Empty
                }).ToList();

                if (EsEdicion)
                {
                    // ACTUALIZAR producto existente localmente
                    await _ordenDraftService.ActualizarProductoAsync(
                        SIdProductoLocal,
                        VarianteSeleccionada,
                        SIndicaciones,
                        lstExtras);

                    await MostrarAlertaAsync("OK", "Producto actualizado. Recuerda guardar los cambios.");
                }
                else
                {
                    // AGREGAR nuevo producto localmente
                    await _ordenDraftService.AgregarProductoAsync(
                        OProductoSeleccionado,
                        VarianteSeleccionada,
                        SIndicaciones,
                        lstExtras);

                    await MostrarAlertaAsync("OK", "Producto agregado. Recuerda guardar los cambios.");
                }

                // Regresar a la pantalla anterior
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                MostrarError($"Error: {ex.Message}");
            }
        }

        #endregion

        #region HELPERS

        private async void MostrarError(string mensaje)
        {
            var mainPage = Application.Current?.Windows?.FirstOrDefault()?.Page;
            if (mainPage != null)
            {
                await mainPage.DisplayAlert("Error", mensaje, "OK");
            }
        }

        private async Task MostrarAlertaAsync(string titulo, string mensaje)
        {
            var mainPage = Application.Current?.Windows?.FirstOrDefault()?.Page;
            if (mainPage != null)
            {
                await mainPage.DisplayAlert(titulo, mensaje, "OK");
            }
        }

        #endregion
    }
}

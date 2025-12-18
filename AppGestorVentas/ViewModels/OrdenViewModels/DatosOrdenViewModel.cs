using AppGestorVentas.Classes;
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
    /// ViewModel para la vista de detalles de una orden.
    /// Los cambios se guardan LOCALMENTE hasta que se confirmen.
    /// </summary>
    public partial class DatosOrdenViewModel : ObservableObject, IQueryAttributable
    {
        #region SERVICIOS

        private readonly HttpApiService _oHttpApiService;
        private readonly IPopupService _oIPopupService;
        private readonly SocketIoService _oSocketIoService;
        private readonly OrdenDraftService _ordenDraftService;

        #endregion

        #region PROPIEDADES

        [ObservableProperty]
        private ObservableCollection<OrdenProducto> lstOrdenProducto = new();

        [ObservableProperty]
        private Orden? oOrden;

        [ObservableProperty]
        private string sNombreMesaro = string.Empty;

        private string _sIdOrden = string.Empty;
        public string IdOrden => _sIdOrden;

        [ObservableProperty]
        private bool bHabilitarAccionesEdicion;

        [ObservableProperty]
        private bool bHabilitarBotonTomarOrden;

        [ObservableProperty]
        private bool bHabilitarBotonPrepararOrden;

        [ObservableProperty]
        private bool bHabilitarBotonOrdenPreparada;

        [ObservableProperty]
        private bool bHabilitarBotonGuardarCambios;

        [ObservableProperty]
        private bool bTieneCambiosPendientes;

        [ObservableProperty]
        private decimal totalExtrasOrden;

        [ObservableProperty]
        private decimal totalOrden;

        [ObservableProperty]
        private bool bEsOrdenNueva;

        #endregion

        #region CONSTRUCTOR

        public DatosOrdenViewModel(
            HttpApiService httpApiService,
            IPopupService popupService,
            SocketIoService socketIoService,
            OrdenDraftService ordenDraftService)
        {
            _oHttpApiService = httpApiService;
            _oIPopupService = popupService;
            _oSocketIoService = socketIoService;
            _ordenDraftService = ordenDraftService;

            // Suscribirse a cambios en el servicio de borrador
            _ordenDraftService.OnProductosChanged += OnProductosChanged;
            _ordenDraftService.OnCambiosPendientesChanged += OnCambiosPendientesChanged;
        }

        #endregion

        #region EVENTOS

        private void OnProductosChanged(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Solo actualizar totales, la lista se actualiza manualmente cuando es necesario
                ActualizarTotales();
            });
        }

        private void OnCambiosPendientesChanged(object? sender, bool tieneCambios)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                BTieneCambiosPendientes = tieneCambios;
                BHabilitarBotonGuardarCambios = tieneCambios && !BEsOrdenNueva;
            });
        }

        #endregion

        #region NAVEGACIÓN

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("bSoloLectura", out var bValorSoloLectura) && bValorSoloLectura != null)
            {
                BHabilitarAccionesEdicion = !(bool)bValorSoloLectura;
            }
            else
            {
                BHabilitarAccionesEdicion = true;
            }

            if (query.TryGetValue("sIdOrden", out var sIdOrden) && sIdOrden != null)
            {
                _sIdOrden = sIdOrden.ToString() ?? string.Empty;
            }

            // Verificar si es una orden nueva (viene del OrdenDraftService)
            if (query.TryGetValue("bEsNueva", out var esNueva) && esNueva != null)
            {
                BEsOrdenNueva = (bool)esNueva;
            }
        }

        #endregion

        #region CARGAR DATOS

        public async Task LoadDataAsync()
        {
            int iRol = -111;
            try
            {
                iRol = int.Parse(await AdministradorSesion.GetAsync(KeysSesion.iRol));

                // Si ya hay una orden en el servicio de borrador con el mismo ID, usarla
                if (_ordenDraftService.OrdenActual != null &&
                    (_ordenDraftService.OrdenActual.sIdLocal == _sIdOrden ||
                     _ordenDraftService.OrdenActual.sIdMongoDB == _sIdOrden))
                {
                    // Usar datos del servicio local
                    OOrden = _ordenDraftService.OrdenActual;
                    SNombreMesaro = OOrden.sUsuarioMesero;
                    BEsOrdenNueva = _ordenDraftService.EsOrdenNueva;
                    RefrescarProductos();
                }
                else if (!string.IsNullOrEmpty(_sIdOrden))
                {
                    // Cargar orden desde el backend al servicio local
                    await _ordenDraftService.CargarOrdenExistenteAsync(_sIdOrden);

                    if (_ordenDraftService.OrdenActual != null)
                    {
                        OOrden = _ordenDraftService.OrdenActual;
                        SNombreMesaro = OOrden.sUsuarioMesero;
                        BEsOrdenNueva = false;
                        RefrescarProductos();
                    }
                }

                ActualizarEstadoBotones(iRol);
            }
            catch (Exception ex)
            {
                MostrarError($"Error al cargar datos: {ex.Message}");
            }
        }

        private void RefrescarProductos()
        {
            LstOrdenProducto.Clear();

            foreach (var producto in _ordenDraftService.Productos)
            {
                producto.IsExpanded = false;
                SuscribirToggle(producto);
                LstOrdenProducto.Add(producto);
            }

            // Calcular totales
            TotalOrden = _ordenDraftService.CalcularTotalOrden();
            TotalExtrasOrden = _ordenDraftService.Productos
                .SelectMany(p => p.aConsumos)
                .SelectMany(c => c.aExtras)
                .Sum(e => e.iCostoPublico);

            BTieneCambiosPendientes = _ordenDraftService.TieneCambiosPendientes;
            BHabilitarBotonGuardarCambios = BTieneCambiosPendientes && !BEsOrdenNueva;
        }

        private void ActualizarEstadoBotones(int iRol)
        {
            if (OOrden == null) return;

            // Para órdenes nuevas o en estatus 0, mostrar "Tomar Orden"
            if (BEsOrdenNueva || (LstOrdenProducto.Count > 0 && BHabilitarAccionesEdicion && OOrden.iEstatus == 0))
            {
                BHabilitarBotonTomarOrden = true;
                BHabilitarBotonPrepararOrden = false;
                BHabilitarBotonOrdenPreparada = false;
            }
            else
            {
                BHabilitarBotonTomarOrden = false;

                if (OOrden.iEstatus == 1) // Tomada
                {
                    BHabilitarBotonPrepararOrden = true;
                }
                else if (OOrden.iEstatus == 2) // En preparación
                {
                    BHabilitarAccionesEdicion = false;
                    BHabilitarBotonOrdenPreparada = true;
                }
            }
        }

        private void SuscribirToggle(OrdenProducto prod)
        {
            prod.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(OrdenProducto.IsExpanded) && prod.IsExpanded)
                {
                    foreach (var other in LstOrdenProducto.Where(x => x != prod))
                        other.IsExpanded = false;
                }
            };
        }

        #endregion

        #region PRODUCTOS - ELIMINAR

        [RelayCommand]
        public async Task EliminarProductoOrden(string sIdProducto)
        {
            try
            {
                var mainPage = Application.Current?.Windows?.FirstOrDefault()?.Page;
                if (mainPage == null) return;

                bool confirmar = await mainPage.DisplayAlert(
                    "Confirmar Eliminación",
                    "¿Estás seguro de que deseas eliminar este producto?",
                    "Sí", "No");

                if (confirmar)
                {
                    // Buscar por ID local o MongoDB
                    var producto = _ordenDraftService.Productos
                        .FirstOrDefault(p => p.sIdLocal == sIdProducto || p.sIdMongo == sIdProducto);

                    if (producto != null)
                    {
                        await _ordenDraftService.EliminarProductoAsync(producto.sIdLocal);
                        await mainPage.DisplayAlert("OK", "Producto eliminado. Recuerda guardar los cambios.", "OK");
                        RefrescarProductos();
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error al eliminar producto: {ex.Message}");
            }
        }

        #endregion

        #region PRODUCTOS - INCREMENTAR/DECREMENTAR CANTIDAD

        [RelayCommand]
        public async Task IncrementarCantidad(OrdenProducto producto)
        {
            if (producto == null) return;

            try
            {
                await _ordenDraftService.ActualizarCantidadProductoAsync(
                    producto.sIdLocal,
                    producto.iCantidad + 1);

                // Actualizar totales sin recrear toda la lista
                ActualizarTotales();
            }
            catch (Exception ex)
            {
                MostrarError($"Error: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task DecrementarCantidad(OrdenProducto producto)
        {
            if (producto == null) return;

            try
            {
                if (producto.iCantidad <= 1)
                {
                    // Confirmar eliminación
                    var mainPage = Application.Current?.Windows?.FirstOrDefault()?.Page;
                    if (mainPage != null)
                    {
                        bool confirmar = await mainPage.DisplayAlert(
                            "Eliminar producto",
                            "Si decrementas a 0, el producto será eliminado. ¿Continuar?",
                            "Sí", "No");

                        if (!confirmar) return;
                    }

                    // Si confirma eliminar (cantidad = 0)
                    await _ordenDraftService.ActualizarCantidadProductoAsync(
                        producto.sIdLocal, 0);
                    RefrescarProductos(); // Necesario porque se elimina de la lista
                    return;
                }

                // Verificar si tiene extras y redirigir a consumos
                if (producto.bTieneExtras || producto.aConsumos.Any(c => c.aExtras.Count > 0))
                {
                    await Shell.Current.GoToAsync("consumosProducto", new Dictionary<string, object>
                    {
                        { "sIdOrdenProducto", producto.sIdLocal },
                        { "sIdOrden", _sIdOrden },
                        { "sNombreProducto", producto.sNombre },
                        { "iCantidad", producto.iCantidad },
                        { "bModoDecremento", true }
                    });
                    return;
                }

                await _ordenDraftService.ActualizarCantidadProductoAsync(
                    producto.sIdLocal,
                    producto.iCantidad - 1);

                // Actualizar totales sin recrear toda la lista
                ActualizarTotales();
            }
            catch (Exception ex)
            {
                MostrarError($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza solo los totales sin recrear la lista de productos
        /// </summary>
        private void ActualizarTotales()
        {
            TotalOrden = _ordenDraftService.CalcularTotalOrden();
            TotalExtrasOrden = _ordenDraftService.Productos
                .SelectMany(p => p.aConsumos)
                .SelectMany(c => c.aExtras)
                .Sum(e => e.iCostoPublico);

            BTieneCambiosPendientes = _ordenDraftService.TieneCambiosPendientes;
            BHabilitarBotonGuardarCambios = BTieneCambiosPendientes && !BEsOrdenNueva;
        }

        #endregion

        #region NAVEGACIÓN A OTRAS VISTAS

        [RelayCommand]
        public async Task IrAgregarProductoOrden()
        {
            try
            {
                await Shell.Current.GoToAsync("datosProductoOrden", new Dictionary<string, object>
                {
                    { "sIdOrden", _sIdOrden }
                });
            }
            catch (Exception ex)
            {
                MostrarError($"Error: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task IrEditarProductoOrden(OrdenProducto producto)
        {
            if (producto == null) return;

            await Shell.Current.GoToAsync("datosProductoOrden", new Dictionary<string, object>
            {
                { "sIdOrden", _sIdOrden },
                { "ordenProducto", producto }
            });
        }

        [RelayCommand]
        public async Task IrAdministrarConsumos(OrdenProducto producto)
        {
            if (producto == null) return;

            await Shell.Current.GoToAsync("consumosProducto", new Dictionary<string, object>
            {
                { "sIdOrdenProducto", producto.sIdLocal },
                { "sIdOrden", _sIdOrden },
                { "sNombreProducto", producto.sNombre },
                { "iCantidad", producto.iCantidad > 0 ? producto.iCantidad : 1 }
            });
        }

        [RelayCommand]
        public async Task MostrarIndicaciones()
        {
            await _oIPopupService.ShowPopupAsync<IndicacionesOrdenPopupViewModel>(vm =>
            {
                vm.SIndicaciones = OOrden?.sIndicaciones ?? string.Empty;
                vm.OnGuardar = async (indicaciones) =>
                {
                    await _ordenDraftService.ActualizarIndicacionesAsync(indicaciones);
                    if (OOrden != null)
                    {
                        OOrden.sIndicaciones = indicaciones;
                        OnPropertyChanged(nameof(OOrden));
                    }
                    await Shell.Current.DisplayAlert("OK", "Indicaciones actualizadas. Recuerda guardar los cambios.", "OK");
                };
            });
        }

        #endregion

        #region GUARDAR CAMBIOS (para órdenes existentes)

        [RelayCommand]
        public async Task GuardarCambios()
        {
            try
            {
                if (!BTieneCambiosPendientes)
                {
                    await Shell.Current.DisplayAlert("Info", "No hay cambios pendientes.", "OK");
                    return;
                }

                var mainPage = Application.Current?.Windows?.FirstOrDefault()?.Page;
                if (mainPage == null) return;

                bool confirmar = await mainPage.DisplayAlert(
                    "Guardar Cambios",
                    "¿Deseas guardar todos los cambios realizados a la orden?",
                    "Sí", "No");

                if (!confirmar) return;

                await _oIPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
                {
                    try
                    {
                        var (exito, mensaje) = await _ordenDraftService.GuardarEnBackendAsync();

                        await vm.Cerrar();

                        if (exito)
                        {
                            await mainPage.DisplayAlert("Éxito", mensaje, "OK");
                            RefrescarProductos();
                        }
                        else
                        {
                            await mainPage.DisplayAlert("Error", mensaje, "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        await vm.Cerrar();
                        MostrarError($"Error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                MostrarError($"Error: {ex.Message}");
            }
        }

        #endregion

        #region TOMAR ORDEN (guardar todo y cambiar estatus)

        [RelayCommand]
        public async Task TomarOrden()
        {
            try
            {
                var mainPage = Application.Current?.Windows?.FirstOrDefault()?.Page;
                if (mainPage == null) return;

                if (LstOrdenProducto.Count == 0)
                {
                    await mainPage.DisplayAlert("Validación", "Debes agregar al menos un producto a la orden.", "OK");
                    return;
                }

                bool confirmar = await mainPage.DisplayAlert(
                    "Confirmar Orden",
                    "¿Deseas confirmar y enviar esta orden a cocina?",
                    "Sí", "No");

                if (!confirmar) return;

                await _oIPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
                {
                    try
                    {
                        // 1. Guardar todos los cambios en el backend
                        var (exito, mensaje) = await _ordenDraftService.GuardarEnBackendAsync();

                        if (!exito)
                        {
                            await vm.Cerrar();
                            await mainPage.DisplayAlert("Error", $"No se pudo guardar la orden: {mensaje}", "OK");
                            return;
                        }

                        // 2. Cambiar estatus a "Tomada" (1)
                        var validacionOrden = new ValidacionOrden(_oHttpApiService);
                        var idOrden = _ordenDraftService.OrdenActual?.sIdMongoDB ?? string.Empty;

                        (bool bCodigoRespuesta, string sMensaje) = await validacionOrden.ActualizarEstatusOrden(idOrden, 1);

                        await vm.Cerrar();

                        if (bCodigoRespuesta)
                        {
                            // Notificar por WebSocket
                            await _oSocketIoService.SendMessageAsync("mensaje", "NuevaOrden");

                            await mainPage.DisplayAlert("Éxito", "¡Orden enviada a cocina!", "OK");

                            // Limpiar borrador y regresar
                            await _ordenDraftService.LimpiarBorradorAsync();
                            await Shell.Current.GoToAsync("..");
                        }
                        else
                        {
                            await mainPage.DisplayAlert("Error", sMensaje, "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        await vm.Cerrar();
                        MostrarError($"Error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                MostrarError($"Error: {ex.Message}");
            }
        }

        #endregion

        #region CAMBIAR ESTATUS (preparar, preparada)

        [RelayCommand]
        public async Task ActualizarAEnpreparacion()
        {
            var mainPage = Application.Current?.Windows?.FirstOrDefault()?.Page;
            if (mainPage == null || OOrden == null) return;

            bool confirmar = await mainPage.DisplayAlert(
                "Confirmar",
                "¿Estás seguro de iniciar esta orden?",
                "Sí", "No");

            if (!confirmar) return;

            try
            {
                var validacionOrden = new ValidacionOrden(_oHttpApiService);
                (bool exito, string mensaje) = await validacionOrden.ActualizarEstatusOrden(OOrden.sIdMongoDB, 2);

                if (exito)
                {
                    BHabilitarBotonPrepararOrden = false;
                    await LoadDataAsync();
                }
                else
                {
                    MostrarError(mensaje);
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task ActualizarAPreparada()
        {
            var mainPage = Application.Current?.Windows?.FirstOrDefault()?.Page;
            if (mainPage == null || OOrden == null) return;

            bool confirmar = await mainPage.DisplayAlert(
                "Confirmar",
                "¿Estás seguro de marcar la orden como Preparada?",
                "Sí", "No");

            if (!confirmar) return;

            try
            {
                var validacionOrden = new ValidacionOrden(_oHttpApiService);
                (bool exito, string mensaje) = await validacionOrden.ActualizarEstatusOrden(OOrden.sIdMongoDB, 3);

                if (exito)
                {
                    BHabilitarBotonOrdenPreparada = false;
                    await _oSocketIoService.SendMessageAsync("mensaje", "OrdenLista");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    MostrarError(mensaje);
                }
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

        #endregion
    }

    #region CLASES AUXILIARES

    public class TieneExtrasResponse
    {
        public bool bTieneExtras { get; set; }
    }

    public class CantidadResponse
    {
        public int iCantidad { get; set; }
        public bool requiereAdminConsumos { get; set; }
    }

    #endregion
}

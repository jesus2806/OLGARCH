using AppGestorVentas.Classes;
using AppGestorVentas.Models;
using AppGestorVentas.Services;
using AppGestorVentas.ViewModels.Popup;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace AppGestorVentas.ViewModels.OrdenViewModels
{
    public partial class AdministracionOrdenViewModel : ObservableObject
    {

        #region PROPIEDADES 

        private readonly HttpApiService _httpApiService;
        private readonly LocalDatabaseService _localDatabaseService;
        private readonly IPopupService _IPopupService;
        private readonly SocketIoService _socketIoService;
        private readonly NotificationService _oNotificationService;
        private bool _isAlertVisible = false;

        private const int PageSize = 5;
        private bool _isNoMoreData;
        private bool _isSearching;
        private int _currentPage;

        [ObservableProperty]
        private ObservableCollection<Orden> oOrdenes = new();

        [ObservableProperty]
        private string sTextoBusqueda = string.Empty;

        [ObservableProperty]
        private string sNumeroPaginaActual = string.Empty;

        [ObservableProperty]
        private bool bNoHayResultados = false;

        // PROPIEDADES PARA GESTIONAR LA VISIVILIDAD DE LAS ACCIONES (BOTONES)
        [ObservableProperty]
        private bool bMostrarAccionActualizar = false;

        [ObservableProperty]
        private bool bMostrarAccionEliminar = false;

        [ObservableProperty]
        private bool bMostrarAccionSubOrden = false;

        [ObservableProperty]
        private bool bMostrarAccionImprimir = false;

        [ObservableProperty]
        private bool bMostrarAccionConfirmar = false;

        [ObservableProperty]
        private bool bMostrarBotonAgregarOrden = false;

        [ObservableProperty]
        private bool bMostrarBotonAccionVer = false;

        [ObservableProperty]
        private bool bMostrarBotonPagarOrden = false;

        [ObservableProperty]
        private decimal iTotalBanco;

        [ObservableProperty]
        private decimal iTotalEfectivo;

        #endregion

        #region CONSTRUCTOR

        public AdministracionOrdenViewModel(HttpApiService httpApiService,
                                            LocalDatabaseService localDatabaseService,
                                            IPopupService popupService,
                                            SocketIoService socketIoService,
                                            NotificationService notificationService)
        {
            _httpApiService = httpApiService;
            _localDatabaseService = localDatabaseService;

            _IPopupService = popupService;
            _socketIoService = socketIoService;
            _oNotificationService = notificationService;

        }

        #endregion

        #region WEBSOCKET

        #region ConectarEvento

        public void ConectarEvento()
        {
            _socketIoService.OnMessageReceived += SocketIoService_OnMessageReceived;
        }

        #endregion

        #region DesconectarEvento

        public void DesconectarEvento()
        {
            _socketIoService.OnMessageReceived -= SocketIoService_OnMessageReceived;
        }

        #endregion

        #endregion

        #region EstablecerVisivilidadBotonPorRol

        public async void EstablecerVisivilidadBotonPorRol()
        {
            int iRol = int.Parse(await AdministradorSesion.GetAsync(KeysSesion.iRol));

            switch (iRol)
            {
                case 1: // Administrador
                case 2: // Mesero
                    BMostrarAccionActualizar = true;
                    BMostrarAccionEliminar = true;
                    BMostrarAccionImprimir = true;
                    BMostrarAccionSubOrden = true;
                    BMostrarBotonAgregarOrden = true;
                    BMostrarAccionConfirmar = true;
                    BMostrarBotonPagarOrden = true;
                    BMostrarBotonAccionVer = false;
                    break;
                case 3: // Cocina
                    BMostrarAccionActualizar = false;
                    BMostrarAccionEliminar = false;
                    BMostrarAccionImprimir = false;
                    BMostrarAccionSubOrden = false;
                    BMostrarBotonAgregarOrden = false;
                    BMostrarAccionConfirmar = false;
                    BMostrarBotonPagarOrden = false;
                    BMostrarBotonAccionVer = true;
                    break;
                default:
                    break;
            }

        }

        #endregion

        #region SocketIoService_OnMessageReceived

        // Evento invocado cuando se recibe un mensaje a través del socket
        private async void SocketIoService_OnMessageReceived(object sender, string message)
        {
            try
            {
                int iRol = int.Parse(await AdministradorSesion.GetAsync(KeysSesion.iRol));
                // Si el mensaje recibido es "NuevaOrden", se muestra el display
                if (message.Equals("NuevaOrden", StringComparison.OrdinalIgnoreCase) && iRol == 3)
                {
                    if (_isAlertVisible)
                        return; // Ya se está mostrando una alerta, evita mostrar otra

                    // Asegurarse de ejecutar en el hilo principal (UI)
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await _oNotificationService.PlayNotificationAsync();
                        try
                        {
                            _isAlertVisible = true;
                            await Shell.Current.DisplayAlert("Nueva Orden", "¡Se ha recibido una nueva orden!", "OK");
                            await ObtenerListadoOrdenesAPI();
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                        finally
                        {
                            _isAlertVisible = false;
                        }

                    });
                }
                else if (message.Equals("OrdenLista", StringComparison.OrdinalIgnoreCase) && iRol != 3)
                {
                    if (_isAlertVisible)
                        return; // Ya se está mostrando una alerta, evita mostrar otra

                    // Asegurarse de ejecutar en el hilo principal (UI)
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await _oNotificationService.PlayNotificationAsync();
                        try
                        {
                            _isAlertVisible = true;
                            await Shell.Current.DisplayAlert("Nueva Orden", "¡Una orden está lista para ser servida!", "OK");
                            await ObtenerListadoOrdenesAPI();
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                        finally
                        {
                            _isAlertVisible = false;
                        }

                    });
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Ocurrió un error inesperado al recibir el mensaje socket: {ex.Message} {ex.StackTrace}");
            }
        }

        #endregion

        #region CARGA DE DATOS

        /// <summary>
        /// Obtiene el listado de órdenes desde la API, lo almacena en la base local y carga la primera página.
        /// </summary>
        public async Task ObtenerListadoOrdenesAPI()
        {
            try
            {

                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }

                _isAlertVisible = false;
                //PopupService popupService = new PopupService();
                await _IPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
                {
                    try
                    {
                        OOrdenes.Clear();
                        _currentPage = 1;

                        // Si la tabla no existe, se crea; de lo contrario se limpia.
                        if (!await _localDatabaseService.TableExistsAsync<Orden>())
                        {
                            await _localDatabaseService.CreateTableAsync<Orden>();
                        }
                        else
                        {
                            await _localDatabaseService.DeleteAllRecordsAsync<Orden>();
                        }

                        var response = await _httpApiService.GetAsync("api/ordenes", bRequiereToken: true);
                        if (response != null && response.IsSuccessStatusCode)
                        {
                            var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<Orden>>();
                            if (apiResponse != null && apiResponse.bSuccess &&
                                apiResponse.lData != null && apiResponse.lData.Count > 0)
                            {
                                foreach (var orden in apiResponse.lData)
                                {
                                    try
                                    {
                                        await InsertarOrdenSQLite(orden);
                                    }
                                    catch (Exception ex)
                                    {
                                        MostrarError($"Ocurrió un error inesperado al insertar el producto: {ex.Message} {ex.StackTrace}");
                                        break;
                                    }
                                }
                                await LoadOrdenesFromDatabaseAsync();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MostrarError($"Ocurrió un error al obtener el listado de productos desde la API: {ex.Message} {ex.StackTrace}");
                    }
                    finally
                    {
                        SNumeroPaginaActual = $"Página 1";
                        await vm.Cerrar();
                    }
                });
            }
            catch (Exception ex)
            {
                MostrarError($"Ocurrió un error al obtener el listado de productos desde la API: {ex.Message} {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Inserta una orden en la base de datos local.
        /// </summary>
        public async Task InsertarOrdenSQLite(Orden orden)
        {
            try
            {
                await _localDatabaseService.SaveItemAsync(orden);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error InsertarOrdenSQLite: {ex.Message}", ex);
            }
        }

        #endregion

        #region SincronizarDatos

        [RelayCommand]
        public async Task SincronizarDatos()
        {
            await ObtenerListadoOrdenesAPI();
        }

        #endregion

        #region PAGINACIÓN

        [RelayCommand]
        public async Task OnPaginaSiguiente()
        {
            if (_isNoMoreData)
                return;

            int nextOffset = _currentPage * PageSize;
            var queryResult = _isSearching
                ? BuildSearchQuery(nextOffset)
                : BuildPaginationQuery(nextOffset);

            var nextPageData = await _localDatabaseService.GetItemsAsync<Orden>(queryResult.query, queryResult.parameters);
            if (nextPageData == null || nextPageData.Count == 0)
            {
                _isNoMoreData = true;
                return;
            }

            _currentPage++;
            OOrdenes.Clear();
            foreach (var orden in nextPageData)
                OOrdenes.Add(orden);

            if (nextPageData.Count < PageSize)
                _isNoMoreData = true;

            SNumeroPaginaActual = $"Página {_currentPage}";
        }

        [RelayCommand]
        public async Task OnPaginaAnterior()
        {
            if (_currentPage <= 1)
                return;

            _currentPage--;
            _isNoMoreData = false;
            await LoadOrdenesFromDatabaseAsync();
            SNumeroPaginaActual = $"Página {_currentPage}";
        }

        private async Task LoadOrdenesFromDatabaseAsync()
        {
            var queryResult = _isSearching
                ? BuildSearchQuery((_currentPage - 1) * PageSize)
                : BuildPaginationQuery((_currentPage - 1) * PageSize);

            var orders = await _localDatabaseService.GetItemsAsync<Orden>(queryResult.query, queryResult.parameters);
            OOrdenes.Clear();

            if (orders != null && orders.Count > 0)
            {
                foreach (var orden in orders)
                    OOrdenes.Add(orden);

                _isNoMoreData = orders.Count < PageSize;
            }
            else
            {
                _isNoMoreData = true;
            }

            BNoHayResultados = OOrdenes.Count == 0;
        }

        #endregion

        #region BÚSQUEDA

        [RelayCommand]
        public async Task OnBuscar()
        {
            _isSearching = !string.IsNullOrWhiteSpace(STextoBusqueda);
            _currentPage = 1;
            _isNoMoreData = false;
            await LoadOrdenesFromDatabaseAsync();
            SNumeroPaginaActual = $"Página {_currentPage}";
        }

        [RelayCommand]
        public async Task OnLimpiarBusqueda()
        {
            _isSearching = false;
            STextoBusqueda = string.Empty;
            _currentPage = 1;
            _isNoMoreData = false;
            await LoadOrdenesFromDatabaseAsync();
            SNumeroPaginaActual = $"Página {_currentPage}";
        }

        #endregion

        #region CONSULTAS SQL

        private (string query, object[] parameters) BuildPaginationQuery(int offset)
        {
            string query = @"
                SELECT * 
                FROM tb_Orden 
                ORDER BY dtFechaAlta ASC
                LIMIT ? 
                OFFSET ?;";

            object[] parameters = { PageSize, offset };
            return (query, parameters);
        }

        private (string query, object[] parameters) BuildSearchQuery(int offset)
        {
            string searchValue = $"%{STextoBusqueda}%";
            string query = @"
                SELECT * 
                FROM tb_Orden 
                WHERE sUsuarioMesero LIKE ? OR CAST(iMesa AS TEXT) LIKE ?
                ORDER BY dtFechaAlta ASC
                LIMIT ? 
                OFFSET ?;";

            object[] parameters = { searchValue, searchValue, PageSize, offset };
            return (query, parameters);
        }

        #endregion

        #region OPERACIONES CRUD

        #region AgregarOrden

        [RelayCommand]
        public async void AgregarOrden()
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                return;
            }
            await Shell.Current.GoToAsync("datosOrdenes");
        }

        #endregion

        #region VerOrden

        [RelayCommand]
        public async Task VerOrden(string sIdMongoDB)
        {
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }

                // Navega a la vista de detalle pasando la orden seleccionada.
                await Shell.Current.GoToAsync("datosOrdenesCocina", new Dictionary<string, object>
                                    {
                                        { "sIdOrden",  sIdMongoDB },
                                        { "bSoloLectura", true }
                                    });

                //// Navega a la vista de detalle pasando la orden seleccionada.
                //await Shell.Current.GoToAsync("datosOrdenes", new Dictionary<string, object>
                //                {
                //                    { "sIdOrden",  sIdMongoDB },
                //                    { "bSoloLectura", true }
                //                });
                
            }
            catch (Exception ex)
            {
                MostrarError($"Ocurrió un error inesperado al duplicar el producto: {ex.Message} {ex.StackTrace}");
            }
        }

        #endregion

        #region ActualizarOrden

        [RelayCommand]
        public async Task ActualizarOrden(string sIdMongoDB)
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                return;
            }
            // Navega a la vista de detalle pasando la orden seleccionada.
            await Shell.Current.GoToAsync("datosOrdenes", new Dictionary<string, object>
                                        {
                                            { "sIdOrden",  sIdMongoDB}
                                        });
        }

        #endregion

        #region EliminarOrden

        [RelayCommand]
        public async Task EliminarOrden(string sIdMongoDB)
        {
            bool confirmar = false;
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }

                var mainPage = Application.Current?.Windows[0].Page;
                if (mainPage != null)
                {
                    confirmar = await mainPage.DisplayAlert("Confirmar Eliminación", "¿Deseas eliminar esta orden?", "Sí", "No");
                }
                if (confirmar)
                {
                    await EliminarOrdenApi(sIdMongoDB);
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error en EliminarOrden: {ex.Message}");
            }
        }

        private async Task EliminarOrdenApi(string sIdMongoDB)
        {
            string sMensajeErrorProceso = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(sIdMongoDB))
                    return;

                await _IPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
                {
                    try
                    {
                        var response = await _httpApiService.DeleteAsync($"api/orden/{sIdMongoDB}", bRequiereToken: true);
                        var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<Orden>>();
                        if (apiResponse != null)
                        {
                            if (response.IsSuccessStatusCode && apiResponse != null)
                            {
                                if (apiResponse.bSuccess && apiResponse.lData != null)
                                {
                                    foreach (var orden in apiResponse.lData)
                                    {
                                        await _localDatabaseService.DeleteRecordsAsync<Orden>("sIdMongoDB = ?", sIdMongoDB);
                                        await SincronizarDatos();
                                        await OnLimpiarBusqueda();
                                    }
                                }
                                else
                                {
                                    if (apiResponse.Error != null)
                                    {
                                        sMensajeErrorProceso = apiResponse.Error.sDetails ?? "Error desconocido al eliminar.";
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MostrarError($"Ocurrió un error inesperado al eliminar la orden: {ex.Message} {ex.StackTrace}");
                    }
                    finally
                    {
                        await vm.Cerrar();
                    }
                });
            }
            catch (Exception ex)
            {
                sMensajeErrorProceso = $"{ex.Message} {ex.StackTrace}";
            }
            if (!string.IsNullOrWhiteSpace(sMensajeErrorProceso))
            {
                MostrarError(sMensajeErrorProceso);
            }
        }

        #endregion


        #region ImprimirTicketOrden

        [RelayCommand]
        public async Task ImprimirTicketOrden(string sIdentificadorOrden)
        {
            bool bConfirmar = false;
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }

                bConfirmar = await Shell.Current.DisplayAlert("Confirmar", "¿Deseas imprimir el ticket de esta orden?", "Sí", "No");

                if (bConfirmar)
                {
                    ValidacionOrden validacionOrden = new ValidacionOrden(_httpApiService);
                    (bool bRespuesta, string sMensaje) = await validacionOrden.VerifyOrdenStatusForImprimir(sIdentificadorOrden);

                    if (bRespuesta)
                    {
                        if (!string.IsNullOrWhiteSpace(sMensaje))
                        {
                            MostrarError(sMensaje);
                        }
                        else
                        {
                            await Shell.Current.GoToAsync("impresora", new Dictionary<string, object>
                                        {
                                            { "sIdOrden",  sIdentificadorOrden}
                                        });
                        }
                    }
                    else
                    {
                        MostrarError(sMensaje);
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error en ImprimirTicketOrden: {ex.Message} {ex.StackTrace}");
            }
        }

        #endregion

        #region CrearNuevaOrden

        public async Task CrearNuevaOrden()
        {

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                return;
            }

            await _IPopupService.ShowPopupAsync<CrearOrdenPopupViewModel>(async vw =>
            {
                await vw.InitializeAsync();
            });
        }

        #endregion

        #region CrearNuevaOrdenSecundaria

        [RelayCommand]
        public async Task CrearNuevaOrdenSecundaria(Orden oOrden)
        {
            // Verificar conectividad a Internet.
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                return;
            }

            // Solicitar confirmación al usuario para crear la suborden.
            bool confirmacion = await Shell.Current.DisplayAlert(
                "Confirmar Suborden",
                $"¿Estás seguro de que deseas crear una nueva suborden para la mesa {oOrden.iMesa}?",
                "Sí",
                "No");

            if (!confirmacion)
            {
                return;
            }

            // Obtener datos necesarios del usuario.
            string sIdMongoDBUsuario = await AdministradorSesion.GetAsync(KeysSesion.sIdUsuarioMongoDB);
            string sUsuarioMesero = await AdministradorSesion.GetAsync(KeysSesion.sNombreUsuario);

            // Construir el objeto que se enviará en el body de la petición.
            var nuevaOrden = new
            {
                sIdentificadorOrden = oOrden.sIdentificadorOrden,
                iMesa = oOrden.iMesa,
                iTipoOrden = 2, // Suborden
                sUsuarioMesero,
                sIdMongoDBMesero = sIdMongoDBUsuario
            };

            try
            {
                // Realiza la petición POST a la API.
                var response = await _httpApiService.PostAsync("api/nueva-orden", nuevaOrden);

                // Verificar si se recibió respuesta del servidor.
                if (response == null)
                {
                    MostrarError("No se recibió respuesta del servidor. Inténtalo de nuevo.");
                    return;
                }

                // Verificar el código de estado HTTP.
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    MostrarError($"Error en la solicitud ({response.StatusCode}): {errorContent}");
                    return;
                }

                // Intentar deserializar la respuesta.
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<Orden>>();
                if (apiResponse == null)
                {
                    MostrarError("La respuesta del servidor es inválida. Inténtalo de nuevo más tarde.");
                    return;
                }

                // Verificar el éxito de la operación en el API.
                if (!apiResponse.bSuccess)
                {
                    // Supongamos que ApiRespuesta incluye un campo 'sMensaje' para errores.
                    string mensajeError = string.IsNullOrWhiteSpace(apiResponse.sMessage)
                        ? "Error desconocido al crear la orden."
                        : apiResponse.sMessage;
                    MostrarError(mensajeError);
                    return;
                }

                // Verificar que se haya retornado al menos una orden.
                if (apiResponse.lData == null || apiResponse.lData.Count == 0)
                {
                    MostrarError("No se encontró información sobre la nueva orden en la respuesta del servidor.");
                    return;
                }

                // Navegar a la pantalla de detalles con la nueva orden.
                Orden oOrdenNueva = apiResponse.lData[0];
                await Shell.Current.GoToAsync("datosOrdenes", new Dictionary<string, object>
        {
            { "sIdOrden", oOrdenNueva.sIdMongoDB }
        });
            }
            catch (Exception ex)
            {
                // Capturar cualquier excepción y notificar al usuario con un mensaje claro.
                MostrarError($"Excepción al crear la orden: {ex.Message}. Por favor, inténtalo de nuevo.");
            }
        }

        #endregion

        #endregion

        #region ActualizarAPreparada

        [RelayCommand]
        public async Task ActualizarAEntregada(string sIdMongoDB)
        {
            bool bRespuesta = (bool)await Shell.Current.DisplayAlert("Confirmar actividad", $"¿Estás seguro de marcar la orden como Entregada?", "Si", "No");

            if (bRespuesta)
            {
                var validacionOrden = new ValidacionOrden(_httpApiService);

                (bool bCodigoRespuesta, string sMensaje) = await validacionOrden.ActualizarEstatusOrden(sIdMongoDB, 4);

                if (bCodigoRespuesta)
                {
                    await SincronizarDatos();
                }
                else
                {
                    MostrarError(sMensaje);
                }
            }
        }

        #endregion

        #region DefinirMetodoPago

        [RelayCommand]
        public async Task DefinirMetodoPago(Orden oOrden)
        {
            string sIdMongoDB;
            try
            {
                sIdMongoDB = oOrden.sIdMongoDB;

                string sRespuesta = "";
                CommunityToolkit.Maui.Views.Popup popup = null;

                // Botones de pago principales (Transferencia, Efectivo, Combinado)
                var transferenciaButton = new Button
                {
                    Text = "Transferencia",
                    BackgroundColor = Color.FromArgb("#007AFF"),
                    TextColor = Colors.White,
                    CornerRadius = 10,
                    HorizontalOptions = LayoutOptions.Fill
                };

                var efectivoButton = new Button
                {
                    Text = "Efectivo",
                    BackgroundColor = Color.FromArgb("#34C759"),
                    TextColor = Colors.White,
                    CornerRadius = 10,
                    HorizontalOptions = LayoutOptions.Fill
                };

                var combinadoButton = new Button
                {
                    Text = "Pago Combinado",
                    BackgroundColor = Color.FromArgb("#FFA500"),
                    TextColor = Colors.White,
                    CornerRadius = 10,
                    HorizontalOptions = LayoutOptions.Fill
                };

                // Entradas de texto para los montos de pago combinado
                var efectivoEntry = new Entry
                {
                    Placeholder = "Monto Efectivo",
                    Keyboard = Keyboard.Numeric,
                    IsVisible = false // Oculto inicialmente
                };

                var bancoEntry = new Entry
                {
                    Placeholder = "Monto Banco",
                    Keyboard = Keyboard.Numeric,
                    IsVisible = false // Oculto inicialmente
                };

                // Botón para confirmar los montos de pago combinado
                var confirmarButton = new Button
                {
                    Text = "Confirmar",
                    BackgroundColor = Color.FromArgb("#0A84FF"),
                    TextColor = Colors.White,
                    CornerRadius = 10,
                    IsVisible = false // Oculto inicialmente
                };

                // Acciones de los botones principales
                transferenciaButton.Command = new Command(() =>
                {
                    // Acción para Transferencia
                    sRespuesta = "t";
                    popup.Close();
                });

                efectivoButton.Command = new Command(() =>
                {
                    // Acción para Efectivo
                    sRespuesta = "e";
                    popup.Close();
                });

                combinadoButton.Command = new Command(() =>
                {
                    // Al presionar "Pago Combinado", ocultamos los botones de Transferencia y Efectivo
                    transferenciaButton.IsVisible = false;
                    efectivoButton.IsVisible = false;
                    combinadoButton.IsVisible = false;

                    // Mostramos las entradas y el botón de Confirmar
                    efectivoEntry.IsVisible = true;
                    bancoEntry.IsVisible = true;
                    confirmarButton.IsVisible = true;
                });

                // Botón "Confirmar" para Pago Combinado
                confirmarButton.Command = new Command(() =>
                {
                    // Intentamos parsear los valores numéricos
                    decimal.TryParse(efectivoEntry.Text, out iTotalEfectivo);
                    decimal.TryParse(bancoEntry.Text, out iTotalBanco);

                    // Aquí puedes asignar sRespuesta o llamar a otro método para procesar los montos
                    sRespuesta = "c"; // Ejemplo: 'c' representa "combinado"

                    // Cerrar el popup
                    popup.Close();
                });

                // Botón de Cancelar
                var cancelarButton = new Button
                {
                    Text = "Cancelar",
                    BackgroundColor = Color.FromArgb("#FF3B30"),
                    TextColor = Colors.White,
                    CornerRadius = 10,
                    Command = new Command(() =>
                    {
                        popup.Close();
                    })
                };

                // Construimos el popup con la nueva estructura
                popup = new CommunityToolkit.Maui.Views.Popup
                {
                    Content = new Border
                    {
                        WidthRequest = 350,
                        HeightRequest = 350, // Ajusta según el espacio que requieras
                        BackgroundColor = Colors.White,
                        Padding = new Thickness(20),
                        Content = new StackLayout
                        {
                            Spacing = 15,
                            Children =
                            {
                                new Label
                                {
                                    Text = "¿Cuál fue el método de pago utilizado para completar la orden?",
                                    FontSize = 18,
                                    TextColor = Colors.Black,
                                    HorizontalOptions = LayoutOptions.Center,
                                    HorizontalTextAlignment = TextAlignment.Center
                                },
                                // Contenedor horizontal para los 3 botones (Transferencia, Efectivo, Pago Combinado)
                                new StackLayout
                                {
                                    Orientation = StackOrientation.Vertical,
                                    Spacing = 10,
                                    Children =
                                    {
                                        transferenciaButton,
                                        efectivoButton,
                                        combinadoButton
                                    }
                                },
                                // Entradas de monto y botón de confirmar (visibles solo cuando se elige "Pago Combinado")
                                efectivoEntry,
                                bancoEntry,
                                confirmarButton,

                                // Botón de cancelar debajo de los otros controles
                                cancelarButton
                            }
                        }
                    },
                    CanBeDismissedByTappingOutsideOfPopup = false
                };

                await Shell.Current.ShowPopupAsync(popup);
                // Aquí puedes utilizar sRespuesta si es necesario
                int iTipoPago = -111;
                decimal iTotalDineroBanco = 0, iTotalDineroEfectivo = 0;
                if (sRespuesta == "e") // efectivo
                {
                    iTipoPago = 1;
                    iTotalDineroEfectivo = oOrden.iTotalOrden;
                }
                else if (sRespuesta == "t")
                {
                    iTipoPago = 2;
                    iTotalDineroBanco = oOrden.iTotalOrden;
                }
                else if(sRespuesta == "c")
                {
                    if (ITotalEfectivo <= 0)
                    {
                        MostrarError("Ingresa un monto válido para el campo \"Monto Efectivo\"");
                        return;
                    }
                    else if(ITotalBanco <= 0)
                    {
                        MostrarError("Ingresa un monto válido para el campo \"Monto Banco\"");
                        return;
                    }
                    else
                    {
                        iTipoPago = 3;
                        iTotalDineroEfectivo = ITotalEfectivo;
                        iTotalDineroBanco = ITotalBanco;
                    }
                }

                if (iTipoPago != -111)
                {
                    var payload = new { 
                        iTipoPago,
                        iTotalBanco = iTotalDineroBanco,
                        iTotalEfectivo = iTotalDineroEfectivo,
                        //iTotalCostoPublicoOrden = oOrden.iTotalOrden,
                        //iTotalCostoRealOrden = oOrden.iTotalOrdenCostoReal
                    };
                    string route = $"api/historico/crear/{sIdMongoDB}";
                    var response = await _httpApiService.PostAsync(route, payload);


                    if (response == null)
                    {
                        MostrarError("No se recibió respuesta del servidor al intentar actualizar la orden a Pagada.");
                    }
                    else
                    {
                        // Lee el contenido JSON de la respuesta
                        string jsonResponse = await response.Content.ReadAsStringAsync();

                        // Opciones para el deserializador
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };

                        ApiRespuesta<Orden> apiRespuesta = JsonSerializer.Deserialize<ApiRespuesta<Orden>>(jsonResponse, options);

                        if (apiRespuesta.bSuccess)
                        {
                            await SincronizarDatos();
                        }
                        else
                        {
                            MostrarError(apiRespuesta.Error.sDetails ?? "Ocurrió un error desconocido");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error general al actualizar la orden. Detalle: {ex.Message} {ex.StackTrace}");
            }
        }

        #endregion


        #region MostrarError

        private async void MostrarError(string sMensaje)
        {
            try
            {
                var mainPage = Application.Current?.Windows[0].Page;
                if (mainPage != null)
                {
                    await mainPage.DisplayAlert("Error", sMensaje, "OK");
                }
            }
            catch (Exception ex)
            {

            }
        }

        #endregion

    }
}

using AppGestorVentas.Clases;
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
    public partial class ImprimirTicketViewModel : ObservableObject, IQueryAttributable
    {
        private HttpApiService _httpApiService;
        private string _sIdOrdenMongoDB;
        private Corte _oCorte = null;


        [ObservableProperty]
        private ObservableCollection<string> availableDeviceNames = new();

        [ObservableProperty]
        private string selectedDeviceName;

        [ObservableProperty]
        private string connectedDeviceName;
        partial void OnConnectedDeviceNameChanged(string oldValue, string newValue)
        {
            // Si ConnectedDeviceName está vacío o nulo, entonces NO hay impresora conectada
            IsPrinterConnected = !string.IsNullOrEmpty(newValue);
        }

        [ObservableProperty]
        private bool canConnect;

        [ObservableProperty]
        private string statusMessage;

        [ObservableProperty]
        private bool isPrinterConnected;

#if ANDROID
        private readonly BluetoothClassicPrinterConnector _oBluetoothConnector;
#endif

#if WINDOWS
        private readonly BlePrinterConnector _oBluetoothConnector;
#endif

        //private readonly BlePrinterConnector _btClassicConnector;
        private readonly EscPosPrinterService _escPosPrinterService;
        private readonly IPopupService _oPopupService;

#if ANDROID
        public ImprimirTicketViewModel(
            BluetoothClassicPrinterConnector btClassicConnector,
            IPopupService popupService, HttpApiService httpApiService)
        {
            _oBluetoothConnector = btClassicConnector;
            _httpApiService = httpApiService;
            // El EscPosPrinterService recibe un IPrinterConnector, 
            // por lo que le puedes pasar _btClassicConnector directamente:
            _escPosPrinterService = new EscPosPrinterService(_oBluetoothConnector);
            _oPopupService = popupService;

            CanConnect = false;
        }
#endif


#if WINDOWS

        public ImprimirTicketViewModel(
            BlePrinterConnector btClassicConnector,
            IPopupService popupService, HttpApiService httpApiService)
        {
            _oBluetoothConnector = btClassicConnector;
            _httpApiService = httpApiService;
            //El EscPosPrinterService recibe un IPrinterConnector, 
            // por lo que le puedes pasar _btClassicConnector directamente:
            _escPosPrinterService = new EscPosPrinterService(_oBluetoothConnector);
            _oPopupService = popupService;

            CanConnect = false;
        }

#endif


        #region Manejo de Navegación

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            // Parámetro obligatorio: sIdOrden (el _id de la orden en Mongo)
            if (query.TryGetValue("sIdOrden", out var sIdOrden) && sIdOrden != null)
            {
                _sIdOrdenMongoDB = sIdOrden.ToString() ?? string.Empty;
            }

            if (query.TryGetValue("oCorte", out var oCorte) && oCorte != null)
            {
                _oCorte = (Corte)oCorte;
            }
        }

        #endregion

//#if ANDROID
        // --------------------------------------------------------------------
        // (A) Cargar dispositivos guardados al iniciar
        // --------------------------------------------------------------------
        public async Task LoadCachedDevicesAsync()
        {
            try
            {
                // Cargamos la lista anterior de nombres

                var devicesDict = await _oBluetoothConnector.CargarDispositivosEscaneadosAsync();

                AvailableDeviceNames.Clear();

                foreach (var kvp in devicesDict)
                    AvailableDeviceNames.Add(kvp.Key);

                if (AvailableDeviceNames.Any())
                {
                    SelectedDeviceName = AvailableDeviceNames.First();
                    StatusMessage = $"Se cargaron {devicesDict.Count} dispositivo(s) desde SecureStorage.";
                }
                else
                {
                    StatusMessage = "No hay dispositivos guardados en SecureStorage.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al cargar dispositivos guardados: " + ex.Message;
            }
        }

        // --------------------------------------------------------------------
        // (B) Al cambiar el dispositivo seleccionado
        // --------------------------------------------------------------------
        partial void OnSelectedDeviceNameChanged(string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(newValue))
            {
                CanConnect = false;
                return;
            }

            if (newValue.Equals(ConnectedDeviceName, StringComparison.OrdinalIgnoreCase))
                CanConnect = false;
            else
                CanConnect = true;
        }

        // --------------------------------------------------------------------
        // (C) "Escanear" para obtener dispositivos emparejados
        // --------------------------------------------------------------------
        [RelayCommand]
        private async Task ScanDevicesAsync()
        {
            try
            {
#if ANDROID
                if (!await CheckAndRequestBluetoothPermissions())
                {
                    StatusMessage = "No se otorgaron permisos de Bluetooth/Ubicación.";
                    return;
                }

                // Activar Bluetooth en Android si está apagado
                await ActivateBluetoothOnAndroidAsync();
#endif

                await _oPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
                {
                    try
                    {
                        // Obtenemos la lista de dispositivos emparejados
                        var devicesDict = await _oBluetoothConnector.ScanDevicesAsync();
                        AvailableDeviceNames.Clear();

                        if (devicesDict.Count == 0)
                        {
                            StatusMessage = "No se encontraron dispositivos Bluetooth emparejados.";
                            return;
                        }

                        foreach (var kvp in devicesDict)
                            AvailableDeviceNames.Add(kvp.Key);

                        StatusMessage = $"Se encontraron {devicesDict.Count} dispositivo(s) emparejados.";
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = "Error al listar dispositivos: " + ex.Message;
                    }
                    finally
                    {
                        await vm.Cerrar();
                    }
                });
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al obtener dispositivos: " + ex.Message;
            }
        }

        // --------------------------------------------------------------------
        // (D) Conectar a la impresora seleccionada (Bluetooth Clásico)
        // --------------------------------------------------------------------
        [RelayCommand]
        private async Task ConnectPrinterAsync()
        {
            if (string.Equals(SelectedDeviceName, ConnectedDeviceName, StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = $"Ya estás conectado a {SelectedDeviceName}.";
                return;
            }

            bool conectado = false;

            _oPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>();
            try
            {
                if (string.IsNullOrEmpty(SelectedDeviceName))
                {
                    StatusMessage = "No has seleccionado ningún dispositivo.";
                    return;
                }

                // Intentar conectar directamente
                conectado = await _oBluetoothConnector.ConectarImpresoraAsync(SelectedDeviceName);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                if (conectado)
                {
                    ConnectedDeviceName = SelectedDeviceName;
                    CanConnect = false;
                    StatusMessage = $"Conectado a {SelectedDeviceName}";
                }
                else
                {
                    StatusMessage = "No se pudo conectar a la impresora.";
                    ConnectedDeviceName = null;
                    CanConnect = !string.IsNullOrEmpty(SelectedDeviceName);
                }

            }
            await _oPopupService.ClosePopupAsync();
        }


        [RelayCommand]
        private async Task ImprimirAsync()
        {

            try
            {
                if (_oCorte != null)
                {
                    await PrintCorteAsync(_oCorte);
                }
                else
                {
                    await PrintTicketAsync();
                }
            }
            catch (Exception ex)
            {

            }
        }


        // --------------------------------------------------------------------
        // (E) Imprimir un ticket de prueba
        // --------------------------------------------------------------------
        private async Task PrintTicketAsync()
        {
            bool impreso = false;

            await _oPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
            {
                try
                {

                    var oResponse = await _httpApiService.GetAsync($"api/ordenes/getInfoTicket/{_sIdOrdenMongoDB}");
                    if (oResponse == null)
                    {
                        return;
                    }
                    var apiResponse = await oResponse.Content.ReadFromJsonAsync<ApiRespuesta<InfoTicket>>();

                    if (apiResponse.bSuccess)
                    {
                        InfoTicket oInfoTicket = apiResponse.lData[0];

                        var ticket = new Ticket
                        {
                            sEncabezado = $"BLOOMS BRUNCH RESTAURANT",
                            iMesa = oInfoTicket.iMesa,
                            sPie = "¡Gracias por su compra!",
                            iTotal = oInfoTicket.dTotalPublico
                        };

                        string sNombreProducto;
                        foreach (ProductosInfoTicket oProducto in oInfoTicket.Productos)
                        {
                            if (oProducto.bEsExtra)
                            {
                                sNombreProducto = $"{oProducto.sNombre.Trim()} (Extra)";
                            }
                            else
                            {
                                sNombreProducto = oProducto.sNombre;
                            }
                            ticket.Items.Add(new TicketItem { Cantidad = oProducto.iCantidad, Descripcion = sNombreProducto, PrecioUnitario = oProducto.iCostoPublico });
                        }

                        impreso = await _escPosPrinterService.ImprimirTicketAsync(ticket);
                    }


                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error: {ex.Message}";
                }
                finally
                {
                    StatusMessage = impreso
                        ? "Ticket impreso correctamente."
                        : "Error al imprimir el ticket.";
                    await vm.Cerrar();
                }
            });
        }


        // --------------------------------------------------------------------
        // (E) Imprimir un corte de prueba
        // --------------------------------------------------------------------
        [RelayCommand]
        private async Task PrintCorteAsync(Corte oCorte)
        {
            bool impreso = false;

            await _oPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
            {
                try
                {
                    impreso = await _escPosPrinterService.ImprimirCorteAsync(oCorte);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error: {ex.Message}";
                }
                finally
                {
                    StatusMessage = impreso
                        ? "Corte impreso correctamente."
                        : "Error al imprimir el corte.";
                    await vm.Cerrar();
                }
            });
        }




//#endif

        // --------------------------------------------------------------------
        // (F) (Opcional) Pedir permisos y habilitar Bluetooth en Android
        // --------------------------------------------------------------------
#if ANDROID
        private async Task<bool> CheckAndRequestBluetoothPermissions()
        {
            try
            {
                // Ajustar según tus necesidades/versión de Android
                var statusBlue = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
                if (statusBlue != PermissionStatus.Granted)
                    statusBlue = await Permissions.RequestAsync<Permissions.Bluetooth>();

                // En Android < 12 se requiere (a veces) Location
                var statusLocation = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (statusLocation != PermissionStatus.Granted)
                    statusLocation = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                return (statusBlue == PermissionStatus.Granted && statusLocation == PermissionStatus.Granted);
            }
            catch
            {
                return false;
            }
        }

        private async Task ActivateBluetoothOnAndroidAsync()
        {
            try
            {
                var adapter = Android.Bluetooth.BluetoothAdapter.DefaultAdapter;
                if (adapter == null)
                {
                    StatusMessage = "El dispositivo no soporta Bluetooth.";
                    return;
                }
                if (!adapter.IsEnabled)
                {
                    var intent = new Android.Content.Intent(Android.Bluetooth.BluetoothAdapter.ActionRequestEnable);
                    Platform.CurrentActivity?.StartActivity(intent);

                    // Esperamos un poco (o manejar OnActivityResult)
                    StatusMessage = "Por favor, activa el Bluetooth para continuar...";
                    await Task.Delay(2000);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "No se pudo activar Bluetooth: " + ex.Message;
            }
        }
#endif






    }
}

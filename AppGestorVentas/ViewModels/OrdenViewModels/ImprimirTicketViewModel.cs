using AppGestorVentas.Clases;
using AppGestorVentas.Models;
using AppGestorVentas.Services;
using AppGestorVentas.ViewModels.Popup;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

#if ANDROID
using Microsoft.Maui.ApplicationModel;
#endif

namespace AppGestorVentas.ViewModels.OrdenViewModels
{
    public partial class ImprimirTicketViewModel : ObservableObject, IQueryAttributable
    {
        private readonly HttpApiService _httpApiService;
        private string _sIdOrdenMongoDB = string.Empty;
        private Corte _oCorte = null;

        [ObservableProperty]
        private ObservableCollection<string> availableDeviceNames = new();

        [ObservableProperty]
        private string selectedDeviceName;

        [ObservableProperty]
        private string connectedDeviceName;

        partial void OnConnectedDeviceNameChanged(string oldValue, string newValue)
        {
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
#elif WINDOWS
        private readonly BlePrinterConnector _oBluetoothConnector;
#endif

        private readonly EscPosPrinterService _escPosPrinterService;
        private readonly IPopupService _oPopupService;

        // ==========================
        // CONSTRUCTORES POR PLATAFORMA
        // ==========================
#if ANDROID
        public ImprimirTicketViewModel(
            BluetoothClassicPrinterConnector btClassicConnector,
            IPopupService popupService,
            HttpApiService httpApiService)
        {
            _oBluetoothConnector = btClassicConnector;
            _httpApiService = httpApiService;
            _escPosPrinterService = new EscPosPrinterService(_oBluetoothConnector);
            _oPopupService = popupService;

            CanConnect = false;
        }
#elif WINDOWS
        public ImprimirTicketViewModel(
            BlePrinterConnector btClassicConnector,
            IPopupService popupService,
            HttpApiService httpApiService)
        {
            _oBluetoothConnector = btClassicConnector;
            _httpApiService = httpApiService;
            _escPosPrinterService = new EscPosPrinterService(_oBluetoothConnector);
            _oPopupService = popupService;

            CanConnect = false;
        }
#else
        // Fallback para iOS/MacCatalyst/u otras (evita errores de compilación/DI)
        public ImprimirTicketViewModel(
            IPopupService popupService,
            HttpApiService httpApiService)
        {
            _httpApiService = httpApiService;
            _oPopupService = popupService;

            // No hay conector en esta plataforma (solo evitamos que truene el ViewModel)
            _escPosPrinterService = null;

            CanConnect = false;
            StatusMessage = "Bluetooth no está disponible en esta plataforma.";
        }
#endif

        // ==========================
        // NAVEGACIÓN
        // ==========================
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("sIdOrden", out var sIdOrden) && sIdOrden != null)
                _sIdOrdenMongoDB = sIdOrden.ToString() ?? string.Empty;

            if (query.TryGetValue("oCorte", out var oCorte) && oCorte != null)
                _oCorte = (Corte)oCorte;
        }

        // ==========================
        // UI: cuando cambia el seleccionado
        // ==========================
        partial void OnSelectedDeviceNameChanged(string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(newValue))
            {
                CanConnect = false;
                return;
            }

            CanConnect = !newValue.Equals(ConnectedDeviceName, StringComparison.OrdinalIgnoreCase);
        }

#if ANDROID || WINDOWS
        // ==========================
        // DISPOSITIVOS / CONEXIÓN
        // ==========================
        public async Task LoadCachedDevicesAsync()
        {
            try
            {
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

                await ActivateBluetoothOnAndroidAsync();
#endif

                await _oPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
                {
                    try
                    {
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

        [RelayCommand]
        private async Task ConnectPrinterAsync()
        {
            if (string.Equals(SelectedDeviceName, ConnectedDeviceName, StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = $"Ya estás conectado a {SelectedDeviceName}.";
                return;
            }

            bool conectado = false;

            _ = _oPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>();
            try
            {
                if (string.IsNullOrEmpty(SelectedDeviceName))
                {
                    StatusMessage = "No has seleccionado ningún dispositivo.";
                    return;
                }

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

                await _oPopupService.ClosePopupAsync();
            }
        }
#else
        // Stubs para que compile en otras plataformas
        public Task LoadCachedDevicesAsync()
        {
            StatusMessage = "Bluetooth no está disponible en esta plataforma.";
            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task ScanDevicesAsync()
        {
            StatusMessage = "Bluetooth no está disponible en esta plataforma.";
            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task ConnectPrinterAsync()
        {
            StatusMessage = "Bluetooth no está disponible en esta plataforma.";
            return Task.CompletedTask;
        }
#endif

        // ==========================
        // IMPRIMIR
        // ==========================
        [RelayCommand]
        private async Task ImprimirAsync()
        {
            try
            {
                if (_oCorte != null)
                    await PrintCorteAsync(_oCorte);
                else
                    await PrintTicketAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error inesperado: " + ex.Message;
            }
        }

        private static async Task<byte[]?> LoadLogoBytesAsync(string fileName)
        {
            try
            {
                using var s = await FileSystem.OpenAppPackageFileAsync(fileName);
                using var ms = new MemoryStream();
                await s.CopyToAsync(ms);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        private async Task PrintTicketAsync()
        {
            bool impreso = false;

#if !(ANDROID || WINDOWS)
            StatusMessage = "Impresión no disponible en esta plataforma.";
            return;
#else
            await _oPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
            {
                try
                {
                    if (!IsPrinterConnected)
                    {
                        StatusMessage = "No hay impresora conectada.";
                        return;
                    }

                    var oResponse = await _httpApiService.GetAsync($"api/ordenes/getInfoTicket/{_sIdOrdenMongoDB}");
                    if (oResponse == null)
                    {
                        StatusMessage = "No se obtuvo respuesta del servidor.";
                        return;
                    }

                    var apiResponse = await oResponse.Content.ReadFromJsonAsync<ApiRespuesta<InfoTicket>>();
                    if (apiResponse == null || !apiResponse.bSuccess || apiResponse.lData == null || apiResponse.lData.Count == 0)
                    {
                        StatusMessage = "Respuesta inválida al obtener el ticket.";
                        return;
                    }

                    var oInfoTicket = apiResponse.lData[0];

                    var ticket = new Ticket
                    {
                        sEncabezado = "BLOOMS BRUNCH RESTAURANT",
                        iMesa = oInfoTicket.iMesa,
                        sPie = "¡Gracias por su compra!",
                        iTotal = oInfoTicket.dTotalPublico,
                        dFechaActual = DateTime.Now // IMPORTANTÍSIMO
                    };

                    // Logo opcional (debe estar en Resources/Raw y action correcta)
                    //ticket.LogoBytes = await LoadLogoBytesAsync("logoticket.png");

                    static string StripExtraSuffix(string s)
                    {
                        var n = (s ?? "").Trim();

                        if (n.EndsWith("(Extra)", StringComparison.OrdinalIgnoreCase))
                            n = n.Substring(0, n.Length - "(Extra)".Length).Trim();

                        if (n.EndsWith("Extra", StringComparison.OrdinalIgnoreCase))
                            n = n.Substring(0, n.Length - "Extra".Length).Trim().TrimEnd('-', ':').Trim();

                        return n.Trim();
                    }

                    foreach (var oProducto in oInfoTicket.Productos ?? new List<ProductosInfoTicket>())
                    {
                        var nombre = (oProducto.sNombre ?? "").Trim();

                        // ✅ solo "Ex. Nombre" y sin espacios extra
                        var desc = oProducto.bEsExtra
                            ? $"Ex. {StripExtraSuffix(nombre)}"
                            : nombre;

                        ticket.Items.Add(new TicketItem
                        {
                            Cantidad = oProducto.iCantidad,
                            Descripcion = desc,
                            PrecioUnitario = oProducto.iCostoPublico
                        });
                    }

                    impreso = await _escPosPrinterService.ImprimirTicketAsync(ticket);
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
#endif
        }

        private async Task PrintCorteAsync(Corte oCorte)
        {
            bool impreso = false;

#if !(ANDROID || WINDOWS)
            StatusMessage = "Impresión no disponible en esta plataforma.";
            return;
#else
            await _oPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
            {
                try
                {
                    if (!IsPrinterConnected)
                    {
                        StatusMessage = "No hay impresora conectada.";
                        return;
                    }

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
#endif
        }

        // ==========================
        // ANDROID: PERMISOS
        // ==========================
#if ANDROID
        private async Task<bool> CheckAndRequestBluetoothPermissions()
        {
            try
            {
                var statusBlue = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
                if (statusBlue != PermissionStatus.Granted)
                    statusBlue = await Permissions.RequestAsync<Permissions.Bluetooth>();

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
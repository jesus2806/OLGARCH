using AppGestorVentas.Classes;
using AppGestorVentas.Interfaces.Impresora;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Text.Json;
using System.Threading;

namespace AppGestorVentas.Services
{
    public class BlePrinterConnector : IPrinterConnector
    {
        private readonly IAdapter _adapter;
        private readonly IBluetoothLE _bluetoothLE;

        private Dictionary<string, IDevice> _dispositivosEscaneados = new(StringComparer.OrdinalIgnoreCase);

        private IDevice _dispositivoConectado;
        private IService _servicioImpresora;
        private ICharacteristic _caracteristicaEscritura;

        // Ajusta estos UUIDs a los de tu impresora (si es BLE con un servicio específico)
        private readonly Guid _servicioUUID = Guid.Parse("000018f0-0000-1000-8000-00805f9b34fb");
        private readonly Guid _caracteristicaUUID = Guid.Parse("00002af1-0000-1000-8000-00805f9b34fb");

        public BlePrinterConnector()
        {
            _bluetoothLE = CrossBluetoothLE.Current;
            _adapter = _bluetoothLE.Adapter;
        }

        // -------------------------------------------------------------
        // (A) Escanea dispositivos BLE + agrega dispositivos emparejados
        // -------------------------------------------------------------
        public async Task<Dictionary<string, IDevice>> ScanDevicesAsync(int scanSeconds = 5)
        {
            var encontrados = new Dictionary<string, IDevice>(StringComparer.OrdinalIgnoreCase);

            // Handler para dispositivos descubiertos vía BLE
            EventHandler<DeviceEventArgs> handler = (sender, args) =>
            {
                var device = args.Device;
                if (device != null && !string.IsNullOrEmpty(device.Name))
                {
                    encontrados[device.Name] = device;
                }
            };

            try
            {
                // Suscribir al evento de descubrimiento
                _adapter.DeviceDiscovered += handler;

                // Iniciar escaneo BLE
                await _adapter.StartScanningForDevicesAsync();

                // Esperar la duración configurada (scanSeconds)
                await Task.Delay(TimeSpan.FromSeconds(scanSeconds));

                // Detener escaneo
                await _adapter.StopScanningForDevicesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al escanear: " + ex.Message);
            }
            finally
            {
                _adapter.DeviceDiscovered -= handler;
            }

            // --- AGREGAR DISPOSITIVOS BONDED (EMAPREJADOS) ---
            // Muchas impresoras solo aparecen aquí (Bluetooth clásico)
            try
            {
                var bondedList = _adapter.BondedDevices;
                if (bondedList != null)
                {
                    foreach (var dev in bondedList)
                    {
                        if (!string.IsNullOrEmpty(dev.Name) &&
                            !encontrados.ContainsKey(dev.Name))
                        {
                            encontrados[dev.Name] = dev;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error obteniendo dispositivos emparejados: " + ex.Message);
            }

            // Cache en memoria
            _dispositivosEscaneados = encontrados;

            // Guardar en SecureStorage (solo nombres/IDs)
            await GuardarDispositivosEscaneadosAsync(_dispositivosEscaneados);

            return _dispositivosEscaneados;
        }

        // -------------------------------------------------------------
        // (B) Conectar a la impresora
        // -------------------------------------------------------------
        public async Task<bool> ConectarImpresoraAsync(string nombreImpresora)
        {
            // Desconectar si ya hay algo conectado
            if (_dispositivoConectado != null)
            {
                if (_dispositivoConectado.Name == nombreImpresora &&
                    _dispositivoConectado.State == DeviceState.Connected)
                {
                    return true; // Ya estamos conectados a la misma
                }
                else
                {
                    await DesconectarAsync();
                }
            }

            // Asegurar que tenemos dispositivos escaneados
            // o si no, forzar un escaneo rápido.
            if (!_dispositivosEscaneados.Any())
            {
                await ScanDevicesAsync(3);
            }

            // Verificar si existe el nombre en el diccionario
            if (!_dispositivosEscaneados.ContainsKey(nombreImpresora))
            {
                throw new Exception($"No se encontró la impresora: {nombreImpresora}");
            }

            var dispositivoEncontrado = _dispositivosEscaneados[nombreImpresora];
            if (dispositivoEncontrado == null)
            {
                throw new Exception($"El dispositivo {nombreImpresora} no es válido.");
            }

            try
            {
                // Conectamos con un timeout de 10 segundos
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _adapter.ConnectToDeviceAsync(dispositivoEncontrado, cancellationToken: cts.Token);

                _dispositivoConectado = dispositivoEncontrado;

                // Si tu impresora es verdaderamente BLE y expone servicios, obtén servicios/characteristics
                var servicios = await _dispositivoConectado.GetServicesAsync();
                _servicioImpresora = servicios.FirstOrDefault(s => s.Id == _servicioUUID);
                if (_servicioImpresora == null)
                {
                    // Si no se encuentra, pero tu impresora es Bluetooth clásico,
                    // es probable que no tenga servicios BLE. Puedes omitir este error
                    // o usar otra estrategia de impresión (por ejemplo SPP).
                    throw new Exception($"Servicio BLE no encontrado: {_servicioUUID}");
                }

                var caracteristicas = await _servicioImpresora.GetCharacteristicsAsync();
                // Buscar la característica exacta o la primera con WriteWithoutResponse
                _caracteristicaEscritura = caracteristicas.FirstOrDefault(c =>
                    c.Id == _caracteristicaUUID &&
                    c.Properties.HasFlag(CharacteristicPropertyType.WriteWithoutResponse));

                if (_caracteristicaEscritura == null)
                {
                    _caracteristicaEscritura = caracteristicas.FirstOrDefault(c =>
                        c.Properties.HasFlag(CharacteristicPropertyType.WriteWithoutResponse));
                }

                if (_caracteristicaEscritura == null)
                {
                    throw new Exception("No se encontró una característica con WriteWithoutResponse.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al conectar {nombreImpresora}: {ex.Message}");
                throw;
            }
        }

        // -------------------------------------------------------------
        // (C) Reintento si detectamos error 133 (Gatt error)
        // -------------------------------------------------------------
        public async Task<bool> ReconnectIfGattErrorAsync(string nombreImpresora, int maxReintentos = 1)
        {
            for (int intento = 0; intento < maxReintentos; intento++)
            {
                try
                {
                    bool ok = await ConectarImpresoraAsync(nombreImpresora);
                    if (ok) return true;
                }
                catch (Exception ex)
                {
                    // Solo reintentar si es error 133
                    if (!ex.Message.Contains("133"))
                        return false;
                }
            }
            return false;
        }

        // -------------------------------------------------------------
        // (D) Enviar datos (en bloques de 20 bytes para BLE)
        // -------------------------------------------------------------
        public async Task EnviarAsync(byte[] datos)
        {
            if (_dispositivoConectado == null || _caracteristicaEscritura == null)
                throw new Exception("Impresora no conectada o característica de escritura no disponible");

            // BLE a menudo impone paquetes <= 20 bytes
            const int packetSize = 20;
            for (int i = 0; i < datos.Length; i += packetSize)
            {
                int length = Math.Min(packetSize, datos.Length - i);
                byte[] chunk = new byte[length];
                Array.Copy(datos, i, chunk, 0, length);

                await _caracteristicaEscritura.WriteAsync(chunk);
                // Pequeña pausa para evitar saturar el buffer (ajusta si hace falta)
                await Task.Delay(40);
            }
        }

        // -------------------------------------------------------------
        // (E) Desconectar
        // -------------------------------------------------------------
        public async Task DesconectarAsync()
        {
            if (_dispositivoConectado != null)
            {
                try
                {
                    await _adapter.DisconnectDeviceAsync(_dispositivoConectado);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al desconectar: " + ex.Message);
                }
                finally
                {
                    _dispositivoConectado = null;
                    _servicioImpresora = null;
                    _caracteristicaEscritura = null;
                }
            }
        }

        // -------------------------------------------------------------
        // (F) Guardar / Cargar en SecureStorage
        // -------------------------------------------------------------
        private async Task GuardarDispositivosEscaneadosAsync(Dictionary<string, IDevice> dispositivos)
        {
            try
            {
                var dictToSerialize = dispositivos.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Id.ToString() ?? "");

                var json = JsonSerializer.Serialize(dictToSerialize);
                await AdministradorSesion.SetAsync(KeysSesion.sDispositivosEscaneados, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al guardar dispositivos: " + ex.Message);
            }
        }

        public async Task<Dictionary<string, IDevice>> CargarDispositivosEscaneadosAsync()
        {
            var dispositivos = new Dictionary<string, IDevice>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var json = await AdministradorSesion.GetAsync(KeysSesion.sDispositivosEscaneados);
                if (string.IsNullOrEmpty(json)) return dispositivos;

                var tempDict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (tempDict == null) return dispositivos;

                // Solo restauramos los nombres; el IDevice real se reconstruye en el próximo escaneo
                foreach (var kvp in tempDict)
                {
                    string nombre = kvp.Key;
                    dispositivos[nombre] = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al cargar dispositivos: " + ex.Message);
            }

            return dispositivos;
        }

        public void LimpiarDispositivosGuardados()
        {
            SecureStorage.Remove(KeysSesion.sDispositivosEscaneados.ToString());
            _dispositivosEscaneados.Clear();
        }
    }
}

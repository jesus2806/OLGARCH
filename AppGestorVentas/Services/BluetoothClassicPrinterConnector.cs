// Asegúrate de referenciar tu modelo e interfaces:
using AppGestorVentas.Classes;
using AppGestorVentas.Interfaces.Impresora;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Plugin.BluetoothClassic.Abstractions;
using System.Text.Json;

namespace AppGestorVentas.Clases
{
    public class BluetoothClassicPrinterConnector : IPrinterConnector
    {

#if ANDROID
        // UUID para RFCOMM SPP
        private static readonly Java.Util.UUID SPP_UUID = Java.Util.UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");

        // Adaptador y socket de Android
        private Android.Bluetooth.BluetoothAdapter _bluetoothAdapter;
        private Android.Bluetooth.BluetoothSocket _bluetoothSocket;
#endif

        // En memoria guardamos los dispositivos (clave = nombre, valor = modelo)
        private Dictionary<string, BluetoothDeviceModel> _dispositivos = new(StringComparer.OrdinalIgnoreCase);


        public BluetoothClassicPrinterConnector()
        {
#if ANDROID
            // Obtenemos el adaptador de Bluetooth local
            _bluetoothAdapter = Android.Bluetooth.BluetoothAdapter.DefaultAdapter;

            if (_bluetoothAdapter == null)
                throw new Exception("Este dispositivo no soporta Bluetooth o no se pudo obtener BluetoothAdapter.");
#endif
        }

        // ---------------------------------------------------------
        // (A) "Escanear" (realmente leer dispositivos emparejados)
        // ---------------------------------------------------------
        public async Task<Dictionary<string, BluetoothDeviceModel>> ScanDevicesAsync(int scanSeconds = 3)
        {
            var encontrados = new Dictionary<string, BluetoothDeviceModel>(StringComparer.OrdinalIgnoreCase);

            try
            {
#if ANDROID
                // Obtenemos la lista de dispositivos ya emparejados
                var bonded = _bluetoothAdapter?.BondedDevices;
                if (bonded != null)
                {
                    foreach (var dev in bonded)
                    {
                        if (!string.IsNullOrEmpty(dev.Name) && !encontrados.ContainsKey(dev.Name))
                        {
                            // Creamos nuestro propio modelo con la info necesaria
                            var model = new BluetoothDeviceModel(
                                Name: dev.Name,
                                Address: dev.Address);
                            encontrados[dev.Name] = model;
                        }
                    }
                }
#endif

                // Asignamos al campo privado
                _dispositivos = encontrados;

                // Guardar en almacenamiento seguro (por ejemplo SecureStorage)
                await GuardarDispositivosEscaneadosAsync(_dispositivos);

                return _dispositivos;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener dispositivos emparejados: " + ex.Message);
                return encontrados;
            }
        }

        // ---------------------------------------------------------
        // (B) Conectar (usando Sockets RFCOMM)
        // ---------------------------------------------------------
        public async Task<bool> ConectarImpresoraAsync(string nombreImpresora)
        {
            try
            {
#if ANDROID
                // Cerrar socket previo si existe
                if (_bluetoothSocket != null)
                {
                    _bluetoothSocket.Close();
                    _bluetoothSocket = null;
                }

                // Validar si existe en el diccionario
                if (!_dispositivos.ContainsKey(nombreImpresora))
                    throw new Exception($"No se encontró el dispositivo: {nombreImpresora}");

                var deviceModel = _dispositivos[nombreImpresora];
                if (deviceModel == null || string.IsNullOrEmpty(deviceModel.Address))
                    throw new Exception($"El dispositivo {nombreImpresora} no es válido (sin dirección).");

                // Obtenemos el dispositivo remoto por dirección
                var remoteDevice = _bluetoothAdapter.GetRemoteDevice(deviceModel.Address);
                if (remoteDevice == null)
                    throw new Exception($"No se pudo obtener el dispositivo remoto con dirección {deviceModel.Address}");

                // Creamos el socket RFCOMM
                _bluetoothSocket = remoteDevice.CreateRfcommSocketToServiceRecord(SPP_UUID);

                // Conectar (Bloquea hasta que se establezca la conexión o falle)
                await _bluetoothSocket.ConnectAsync();
#endif

                // Si llegó aquí sin excepciones, entonces la conexión fue exitosa
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al conectar con {nombreImpresora}: {ex.Message}");
                throw; // relanzamos la excepción para manejarla externamente si se desea
            }
        }

        // ---------------------------------------------------------
        // (C) Enviar datos
        // ---------------------------------------------------------
        public async Task EnviarAsync(byte[] datos)
        {
#if ANDROID
            if (_bluetoothSocket == null || !_bluetoothSocket.IsConnected)
                throw new Exception("No hay conexión activa con la impresora.");

            try
            {
                // Escribimos al OutputStream del socket
                var outputStream = _bluetoothSocket.OutputStream;
                await outputStream.WriteAsync(datos, 0, datos.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar datos: " + ex.Message);
                throw;
            }
#endif
        }

        // ---------------------------------------------------------
        // (D) Desconectar
        // ---------------------------------------------------------
        public async Task DesconectarAsync()
        {
#if ANDROID
            if (_bluetoothSocket != null)
            {
                try
                {
                    _bluetoothSocket.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al desconectar: " + ex.Message);
                }
                finally
                {
                    _bluetoothSocket = null;
                }
            }
            await Task.CompletedTask;
#endif
        }

        // ---------------------------------------------------------
        // (E) Cargar/Guardar en SecureStorage, similar a BLE
        // ---------------------------------------------------------
        private async Task GuardarDispositivosEscaneadosAsync(Dictionary<string, BluetoothDeviceModel> dispositivos)
        {
            try
            {
                // Guardamos solo Name=>Address en un diccionario
                var dictToSerialize = dispositivos.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Address ?? ""
                );

                var json = JsonSerializer.Serialize(dictToSerialize);
                await AdministradorSesion.SetAsync(KeysSesion.sDispositivosEscaneados, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al guardar dispositivos: " + ex.Message);
            }
        }

        public async Task<Dictionary<string, BluetoothDeviceModel>> CargarDispositivosEscaneadosAsync()
        {
            var dict = new Dictionary<string, BluetoothDeviceModel>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var json = await AdministradorSesion.GetAsync(KeysSesion.sDispositivosEscaneados);
                if (string.IsNullOrEmpty(json)) return dict;

                var temp = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (temp == null) return dict;

                foreach (var kvp in temp)
                {
                    dict[kvp.Key] = new BluetoothDeviceModel(
                        Name: kvp.Key,
                        Address: kvp.Value
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al cargar dispositivos: " + ex.Message);
            }
            return dict;
        }
    }
}

using SocketIOClient;

namespace AppGestorVentas.Services
{
    public class SocketIoService
    {
        private SocketIOClient.SocketIO _socket;
        private bool _wasDisconnected;

        // Eventos disponibles para que otros componentes se suscriban.
        public event EventHandler<string> OnMessageReceived;
        public event EventHandler<string> OnError;
        public event EventHandler OnDisconnected;
        public event EventHandler OnConnected;
        public event EventHandler OnReconnected;

        // Indica si el socket está conectado.
        public bool IsConnected => _socket?.Connected ?? false;

        /// <summary>
        /// Conecta al servidor Socket.IO solo si no existe ya una conexión activa.
        /// </summary>
        /// <param name="url">URL del servidor (por defecto "wss://prueba-api-gestorventas.click/")</param>
        //public async Task ConnectAsync(string url = "wss://prueba-api-gestorventas.click/")
        public async Task ConnectAsync(string url = "wss://ws-app-gestor-ventas-olgarch.click/")
        //public async Task ConnectAsync(string url = "ws://localhost:3000")
        {
            await Task.Delay(1500);
            // Si ya existe una conexión activa, no se crea una nueva.
            if (_socket != null && _socket.Connected)
            {
                // Conexión ya existente. No se crea una nueva.
                return;
            }

            try
            {
                _socket = new SocketIOClient.SocketIO(url, new SocketIOOptions
                {
                    Reconnection = true,
                    ReconnectionAttempts = int.MaxValue,
                    ReconnectionDelay = 1000,         // Retraso inicial
                    ReconnectionDelayMax = 5000,        // Retraso máximo
                });

                // Configurar el evento OnConnected.
                _socket.OnConnected += (sender, e) =>
                {
                    Console.WriteLine("Conectado al servidor Socket.IO");
                    if (_wasDisconnected)
                    {
                        OnReconnected?.Invoke(this, EventArgs.Empty);
                        _wasDisconnected = false;
                    }
                    else
                    {
                        OnConnected?.Invoke(this, EventArgs.Empty);
                    }
                };

                // Configurar el evento OnDisconnected.
                _socket.OnDisconnected += (sender, reason) =>
                {
                    _wasDisconnected = true;
                    OnDisconnected?.Invoke(this, EventArgs.Empty);
                };

                // Suscribirse al evento "mensaje".
                _socket.On("mensaje", response =>
                {
                    try
                    {
                        string message = response.GetValue<string>();
                        OnMessageReceived?.Invoke(this, message);
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke(this, "Error procesando evento 'mensaje': " + ex.Message);
                    }
                });

                await _socket.ConnectAsync();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, "Error en ConnectAsync: " + ex.Message);
            }
        }

        /// <summary>
        /// Envía un mensaje utilizando el evento especificado.
        /// </summary>
        /// <param name="eventName">Nombre del evento (por ejemplo, "mensaje")</param>
        /// <param name="data">Datos a enviar</param>
        public async Task SendMessageAsync(string eventName, object data)
        {
            if (IsConnected)
            {
                await _socket.EmitAsync(eventName, data);
            }
            else
            {
                OnError?.Invoke(this, "El socket no está conectado, no se puede enviar el mensaje.");
            }
        }

        /// <summary>
        /// Desconecta el socket, desactiva la reconexión y libera la instancia para evitar reconexiones no deseadas.
        /// </summary>
        //public async Task DisconnectAsync()
        //{
        //    if (_socket != null)
        //    {
        //        try
        //        {
        //            // Desactivar la reconexión para evitar que se intente reconectar automáticamente.
        //            // Dependiendo de la versión de SocketIOClient, Options.Reconnection puede ser modificable.
        //            _socket.Options.Reconnection = false;

        //            // Llamar a DisconnectAsync para cerrar la conexión actual.
        //            await _socket.DisconnectAsync( );

        //            // Remover todos los manejadores de eventos para evitar referencias residuales.
        //            _socket.Off("mensaje");

        //            // Liberar la instancia del socket.
        //            _socket.Dispose();
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("Error en DisconnectAsync: " + ex.Message);
        //        }
        //        finally
        //        {
        //            _socket = null;
        //        }
        //    }
        //}
    }
}

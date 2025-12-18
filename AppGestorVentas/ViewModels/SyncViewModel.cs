using AppGestorVentas.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppGestorVentas.ViewModels
{
    /// <summary>
    /// ViewModel para gestionar la sincronización de datos con el backend
    /// </summary>
    public partial class SyncViewModel : ObservableObject
    {
        #region PROPIEDADES

        private readonly SyncService _syncService;

        /// <summary>
        /// Indica si hay una sincronización en progreso
        /// </summary>
        [ObservableProperty]
        private bool bSincronizando;

        /// <summary>
        /// Número de operaciones pendientes de sincronizar
        /// </summary>
        [ObservableProperty]
        private int iOperacionesPendientes;

        /// <summary>
        /// Indica si hay operaciones pendientes
        /// </summary>
        [ObservableProperty]
        private bool bHayPendientes;

        /// <summary>
        /// Mensaje de estado actual
        /// </summary>
        [ObservableProperty]
        private string sMensajeEstado = string.Empty;

        /// <summary>
        /// Porcentaje de progreso (0-100)
        /// </summary>
        [ObservableProperty]
        private double dProgreso;

        /// <summary>
        /// Indica si la última sincronización fue exitosa
        /// </summary>
        [ObservableProperty]
        private bool? bUltimaSincExitosa;

        /// <summary>
        /// Fecha de la última sincronización
        /// </summary>
        [ObservableProperty]
        private DateTime? dtUltimaSync;

        /// <summary>
        /// Color del indicador según el estado
        /// </summary>
        [ObservableProperty]
        private Color colorIndicador = Colors.Gray;

        #endregion

        #region CONSTRUCTOR

        public SyncViewModel(SyncService syncService)
        {
            _syncService = syncService;
            
            // Suscribirse a cambios en operaciones pendientes
            _syncService.OnPendingOperationsChanged += OnPendingOperationsChanged;
            _syncService.OnSyncProgress += OnSyncProgress;

            // Cargar estado inicial
            _ = CargarEstadoInicialAsync();
        }

        #endregion

        #region MÉTODOS PÚBLICOS

        /// <summary>
        /// Carga el estado inicial del servicio de sincronización
        /// </summary>
        public async Task CargarEstadoInicialAsync()
        {
            try
            {
                IOperacionesPendientes = await _syncService.ObtenerCantidadPendientesAsync();
                BHayPendientes = IOperacionesPendientes > 0;
                ActualizarColorIndicador();
                ActualizarMensajeEstado();
            }
            catch (Exception ex)
            {
                SMensajeEstado = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Ejecuta la sincronización de todas las operaciones pendientes
        /// </summary>
        [RelayCommand]
        public async Task SincronizarAsync()
        {
            if (BSincronizando) return;

            BSincronizando = true;
            DProgreso = 0;
            SMensajeEstado = "Iniciando sincronización...";
            ColorIndicador = Colors.Orange;

            try
            {
                // Verificar conectividad
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    SMensajeEstado = "Sin conexión a Internet";
                    BUltimaSincExitosa = false;
                    ColorIndicador = Colors.Red;
                    await MostrarAlertaAsync("Sin Conexión", "No hay conexión a Internet. Intenta más tarde.");
                    return;
                }

                // Verificar estado del servicio
                var servicioActivo = await _syncService.VerificarEstadoServicioAsync();
                if (!servicioActivo)
                {
                    SMensajeEstado = "Servidor no disponible";
                    BUltimaSincExitosa = false;
                    ColorIndicador = Colors.Red;
                    await MostrarAlertaAsync("Servidor No Disponible", 
                        "El servidor de sincronización no está disponible. Intenta más tarde.");
                    return;
                }

                // Ejecutar sincronización
                var resultado = await _syncService.SincronizarAsync();

                DtUltimaSync = DateTime.Now;
                BUltimaSincExitosa = resultado.Exitoso;

                if (resultado.Exitoso)
                {
                    SMensajeEstado = $"✓ {resultado.Exitosas} operaciones sincronizadas";
                    ColorIndicador = Colors.Green;
                    
                    await MostrarAlertaAsync("Sincronización Exitosa", 
                        $"Se sincronizaron {resultado.Exitosas} operaciones correctamente.");
                }
                else
                {
                    SMensajeEstado = $"⚠ {resultado.Fallidas} errores";
                    ColorIndicador = Colors.Orange;
                    
                    await MostrarAlertaAsync("Sincronización Parcial", 
                        $"{resultado.Exitosas} exitosas, {resultado.Fallidas} fallidas.\n{resultado.Mensaje}");
                }

                // Actualizar contador
                IOperacionesPendientes = await _syncService.ObtenerCantidadPendientesAsync();
                BHayPendientes = IOperacionesPendientes > 0;
            }
            catch (Exception ex)
            {
                SMensajeEstado = $"Error: {ex.Message}";
                BUltimaSincExitosa = false;
                ColorIndicador = Colors.Red;
                
                await MostrarAlertaAsync("Error de Sincronización", 
                    $"Ocurrió un error durante la sincronización:\n{ex.Message}");
            }
            finally
            {
                BSincronizando = false;
                DProgreso = 0;
            }
        }

        /// <summary>
        /// Limpia todas las operaciones pendientes (para desarrollo/debug)
        /// </summary>
        [RelayCommand]
        public async Task LimpiarPendientesAsync()
        {
            var confirmar = await MostrarConfirmacionAsync(
                "Limpiar Pendientes",
                "¿Estás seguro de eliminar todas las operaciones pendientes? Esta acción no se puede deshacer.");

            if (confirmar)
            {
                await _syncService.LimpiarOperacionesAsync();
                IOperacionesPendientes = 0;
                BHayPendientes = false;
                SMensajeEstado = "Operaciones eliminadas";
                ColorIndicador = Colors.Gray;
            }
        }

        #endregion

        #region EVENTOS

        private void OnPendingOperationsChanged(object? sender, int cantidad)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IOperacionesPendientes = cantidad;
                BHayPendientes = cantidad > 0;
                ActualizarColorIndicador();
                ActualizarMensajeEstado();
            });
        }

        private void OnSyncProgress(object? sender, SyncProgressEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DProgreso = e.Porcentaje;
                SMensajeEstado = e.Estado;
            });
        }

        #endregion

        #region MÉTODOS AUXILIARES

        private void ActualizarColorIndicador()
        {
            if (BSincronizando)
            {
                ColorIndicador = Colors.Orange;
            }
            else if (BHayPendientes)
            {
                ColorIndicador = Colors.DodgerBlue;
            }
            else if (BUltimaSincExitosa == true)
            {
                ColorIndicador = Colors.Green;
            }
            else if (BUltimaSincExitosa == false)
            {
                ColorIndicador = Colors.Red;
            }
            else
            {
                ColorIndicador = Colors.Gray;
            }
        }

        private void ActualizarMensajeEstado()
        {
            if (BSincronizando)
            {
                // Ya se actualiza en el progreso
                return;
            }

            if (BHayPendientes)
            {
                SMensajeEstado = $"{IOperacionesPendientes} cambios pendientes";
            }
            else
            {
                SMensajeEstado = "Todo sincronizado";
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

        private async Task<bool> MostrarConfirmacionAsync(string titulo, string mensaje)
        {
            var mainPage = Application.Current?.Windows?.FirstOrDefault()?.Page;
            if (mainPage != null)
            {
                return await mainPage.DisplayAlert(titulo, mensaje, "Sí", "No");
            }
            return false;
        }

        #endregion
    }
}

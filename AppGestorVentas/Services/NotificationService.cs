using Plugin.Maui.Audio;

namespace AppGestorVentas.Services
{
    public class NotificationService
    {
        private readonly IAudioManager _audioManager;

        // Se inyecta el IAudioManager en el constructor
        public NotificationService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        /// <summary>
        /// Reproduce un tono y, si el dispositivo es Android, activa la vibración.
        /// </summary>
        public async Task PlayNotificationAsync()
        {
            try
            {
                // Cargar el archivo de audio desde los assets del paquete de la aplicación.
                // Asegúrate de que "notification.mp3" esté configurado como MauiAsset.
                using var stream = await FileSystem.OpenAppPackageFileAsync("bellding.mp3");

                // Crear el reproductor de audio.
                var player = _audioManager.CreatePlayer(stream);

                // Reproducir el tono.
                player.Play();

                // En dispositivos que soporten vibración (como Android) se vibra.
                // Vibration.Default.IsSupported devuelve false en plataformas de escritorio.
                if (Vibration.Default.IsSupported)
                {
                    // Vibrar durante 500 milisegundos.
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en PlayNotificationAsync: {ex.Message}");
            }
        }
    }
}

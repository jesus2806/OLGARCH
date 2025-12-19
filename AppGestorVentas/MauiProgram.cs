using AppGestorVentas.Clases;
using AppGestorVentas.Services;
using AppGestorVentas.ViewModels.HistoricoViewModels;
using AppGestorVentas.ViewModels.LoginViewModels;
using AppGestorVentas.ViewModels.OrdenViewModels;
using AppGestorVentas.ViewModels.Popup;
using AppGestorVentas.ViewModels.ProductoViewModels;
using AppGestorVentas.ViewModels.UsuarioViewModels;
using AppGestorVentas.Views.HistoricoViews;
using AppGestorVentas.Views.LoginViews;
using AppGestorVentas.Views.OrdenViews;
using AppGestorVentas.Views.Popup;
using AppGestorVentas.Views.ProductoVierws;
using AppGestorVentas.Views.UsuariosViews;
using AppGestorVentas.Views.UsuarioViews;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using Microsoft.Extensions.Logging;
using Plugin.BluetoothClassic.Abstractions;
using Plugin.Maui.Audio;

namespace AppGestorVentas
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMarkup()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont(filename: "materialdesignicons-webfont.ttf", alias: "MaterialDesignIcons");
                })
                .RegisterServices()
                .RegisterViewsViewModels()
                .RegisterPopup();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();

        }

        #region RegisterServices

        /// <summary>
        /// Registra los servicios necesarios en el contenedor de dependencias.
        /// </summary>
        /// <param name="mauiAppBuilder">El constructor de la aplicación MAUI.</param>
        /// <returns>El <see cref="MauiAppBuilder"/> actualizado con los servicios registrados.</returns>
        public static MauiAppBuilder RegisterServices(this MauiAppBuilder mauiAppBuilder)
        {
            mauiAppBuilder.Services.AddHttpClient<HttpApiService>(client =>
            {
                //client.BaseAddress = new Uri("https://prueba-api-gestorventas.click/"); // servidor preproducción
                client.BaseAddress = new Uri("https://ws-app-gestor-ventas-olgarch.click/"); // servidor producción
                //client.BaseAddress = new Uri("http://localhost:3000"); // servidor local
                client.DefaultRequestHeaders.Add("Accept", "application/json"); // Encabezado para aceptar JSON
                client.Timeout = TimeSpan.FromSeconds(30); // Configura el tiempo de espera a 30 segundos
            });
            mauiAppBuilder.Services.AddSingleton<LocalDatabaseService>();
            mauiAppBuilder.Services.AddSingleton<SocketIoService>();
            mauiAppBuilder.Services.AddSingleton<IAudioManager, AudioManager>();
            mauiAppBuilder.Services.AddSingleton<NotificationService>();
            
            // Servicio de sincronización offline-first
            mauiAppBuilder.Services.AddSingleton<SyncService>();
            
            // Servicio de borrador de orden (gestión local)
            mauiAppBuilder.Services.AddSingleton<OrdenDraftService>();
            
#if ANDROID
            mauiAppBuilder.Services.AddSingleton<BluetoothClassicPrinterConnector>();
#endif
#if WINDOWS
            mauiAppBuilder.Services.AddSingleton<BlePrinterConnector>();
#endif

            return mauiAppBuilder;
        }

#endregion

        #region RegisterPopup

        /// <summary>
        /// Registra los popup necesarios en el contenedor de dependencias.
        /// </summary>
        /// <param name="mauiAppBuilder">El constructor de la aplicación MAUI.</param>
        /// <returns>El <see cref="MauiAppBuilder"/> actualizado con los servicios registrados.</returns>
        public static MauiAppBuilder RegisterPopup(this MauiAppBuilder mauiAppBuilder)
        {
            mauiAppBuilder.Services.AddTransientPopup<CargaGeneralPopup, CargaGeneralPopupViewModel>();
            mauiAppBuilder.Services.AddTransientPopup<AlertaGeneralPopup, AlertaGeneralPopupViewModel>();
            mauiAppBuilder.Services.AddTransientPopup<CrearOrdenPopup, CrearOrdenPopupViewModel>();
            mauiAppBuilder.Services.AddTransientPopup<IndicacionesOrdenPopup, IndicacionesOrdenPopupViewModel>();
            // More popup registered here.

            return mauiAppBuilder;
        }

        #endregion

        #region RegisterViewsViewModels

        public static MauiAppBuilder RegisterViewsViewModels(this MauiAppBuilder mauiAppBuilder)
        {
            mauiAppBuilder.Services.AddTransientWithShellRoute<AdministracionUsuariosView, AdministracionUsuariosViewModel>("adminUsuarios");
            mauiAppBuilder.Services.AddTransientWithShellRoute<DatosUsuarioView, DatosUsuarioViewModel>("datosUsuarios");
            mauiAppBuilder.Services.AddTransientWithShellRoute<AdministracionProductosView, AdministracionProductosViewModel>("adminProductos");
            mauiAppBuilder.Services.AddTransientWithShellRoute<DatosProductoView, DatosProductoViewModel>("datosProductos");
            //mauiAppBuilder.Services.AddTransientWithShellRoute<LoginView, LoginViewModel>("login");
            mauiAppBuilder.Services.AddTransientWithShellRoute<AdministracionOrdenView, AdministracionOrdenViewModel>("adminOrdenes");

            //mauiAppBuilder.Services.AddSingleton<AdministracionOrdenViewModel>();
            //mauiAppBuilder.Services.AddTransient<AdministracionOrdenView>();


            mauiAppBuilder.Services.AddTransientWithShellRoute<DatosOrdenView, DatosOrdenViewModel>("datosOrdenes");
            mauiAppBuilder.Services.AddTransientWithShellRoute<DatosOrdenCocinaView, DatosOrdenViewModel>("datosOrdenesCocina");
            mauiAppBuilder.Services.AddTransientWithShellRoute<ProductoOrdenView, ProductoOrdenViewModel>("datosProductoOrden");
            mauiAppBuilder.Services.AddTransientWithShellRoute<ImprimirTicketView, ImprimirTicketViewModel>("impresora");
            mauiAppBuilder.Services.AddTransientWithShellRoute<HistoricoView, HistoricoViewModel>("historico");
            mauiAppBuilder.Services.AddTransientWithShellRoute<PerfilView, PerfilViewModel>("perfil");
            
            // Nuevas vistas para mockups de consumos y extras
            mauiAppBuilder.Services.AddTransientWithShellRoute<ConsumosProductoView, ConsumosProductoViewModel>("consumosProducto");
            mauiAppBuilder.Services.AddTransientWithShellRoute<BuscarExtrasView, BuscarExtrasViewModel>("buscarExtras");
            
            mauiAppBuilder.Services.AddTransient<LoginView>();
            mauiAppBuilder.Services.AddTransient<LoginViewModel>();
            
            // ViewModel de sincronización (singleton para compartir estado)
            mauiAppBuilder.Services.AddSingleton<AppGestorVentas.ViewModels.SyncViewModel>();
            
            // More views - view models registered here.

            return mauiAppBuilder;
        }

        #endregion

    }
}

using AppGestorVentas.Classes;
using AppGestorVentas.Services;
using AppGestorVentas.ViewModels.HistoricoViewModels;
using AppGestorVentas.ViewModels.OrdenViewModels;
using AppGestorVentas.Views.HistoricoViews;
using AppGestorVentas.Views.LoginViews;
using AppGestorVentas.Views.OrdenViews;
using AppGestorVentas.Views.ProductoVierws;
using AppGestorVentas.Views.UsuariosViews;
using AppGestorVentas.Views.UsuarioViews;
using AppGestorVentas.Views.IngredientesViews;
using AppGestorVentas.Views.EsquemasViews;

namespace AppGestorVentas
{
    public partial class AppShell : Shell
    {
        private SocketIoService _socketIoService;
        // El parámetro rolUsuario se utiliza para determinar qué pestañas se agregan.
        public AppShell(SocketIoService socketIoService, int rolUsuario, string sNombreUsuario)
        {
            _socketIoService = socketIoService;
            // Construir la Shell de forma dinámica según el rol.
            BuildShell(rolUsuario, sNombreUsuario);

            // Aquí puedes suscribirte a eventos del socket, etc.
            //_socketIoService.OnConnected += (s, e) =>
            //{
            //    Console.WriteLine("Conexión establecida.");
            //};

            _socketIoService.OnDisconnected += async (s, e) =>
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("Conexión perdida", "Se ha perdido la conexión con el servidor.", "Aceptar"));
            };

            _socketIoService.OnReconnected += async (s, e) =>
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("Reconectado", "La conexión se ha reestablecido.", "Aceptar"));
            };

            _socketIoService.OnError += async (s, e) =>
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("ERROR WS", e.ToString(), "Aceptar"));
            };

            //_socketIoService.OnMessageReceived += async (s, message) =>
            //{
            //    if (message == "MensajeEspecifico")
            //    {
            //        await MainThread.InvokeOnMainThreadAsync(() =>
            //            DisplayAlert("Mensaje recibido", "Se recibió el mensaje esperado.", "OK"));
            //    }
            //    else
            //    {
            //        Console.WriteLine($"Mensaje recibido: {message}");
            //    }
            //};
        }

        private void BuildShell(int rolUsuario, string sNombreUsuario)
        {
            // Configurar el FlyoutHeader dinámicamente.
            this.FlyoutHeader = CreateFlyoutHeader(sNombreUsuario);
            // Asegurarse de que el FlyoutBehavior esté configurado para mostrar el menú lateral.
            this.FlyoutBehavior = FlyoutBehavior.Flyout;

            // Creamos un TabBar para contener las pestañas.
            var tabBar = new TabBar();

            // Agregar la pestaña de Ordenes (siempre visible)
            var tabOrdenes = new Tab
            {
                Title = "Ordenes",
                Icon = "notebook.png"
            };
            tabOrdenes.Items.Add(new ShellContent
            {
                Title = "Monitor de Ordenes",
                ContentTemplate = new DataTemplate(typeof(AdministracionOrdenView))
            });

            tabBar.Items.Add(tabOrdenes);

            // Si el rol es administrador (1), agregamos las pestañas de Usuarios y Productos.
            if (rolUsuario == 1)
            {
                // Pestaña Usuarios
                var tabUsuarios = new Tab
                {
                    Title = "Usuarios",
                    Icon = "account.png"
                };
                tabUsuarios.Items.Add(new ShellContent
                {
                    Title = "Administración de Usuarios",
                    ContentTemplate = new DataTemplate(typeof(AdministracionUsuariosView))
                });
                tabBar.Items.Add(tabUsuarios);

                // Pestaña Productos
                var tabProductos = new Tab
                {
                    Title = "Productos",
                    Icon = "food.png"
                };
                tabProductos.Items.Add(new ShellContent
                {
                    Title = "Administración de Productos",
                    ContentTemplate = new DataTemplate(typeof(AdministracionProductosView))
                });

                tabBar.Items.Add(tabProductos);

                // Pestaña Ingredientes
                var tabIngredientes = new Tab
                {
                    Title = "Ingredientes",
                    Icon = "food.png"
                };

                tabIngredientes.Items.Add(new ShellContent
                {
                    Title = "Administración de Ingredientes",
                    ContentTemplate = new DataTemplate(typeof(ListaIngredientesView))
                });

                tabBar.Items.Add(tabIngredientes);

                var tabEsquemas = new Tab
                {
                    Title = "Esquemas",
                    Icon = "notebook.png"
                };

                tabEsquemas.Items.Add(new ShellContent
                {
                    Title = "Esquemas de Pago",
                    ContentTemplate = new DataTemplate(typeof(EsquemasView))
                });

                tabBar.Items.Add(tabEsquemas);

                // Pestaña Nómina
                var tabNomina = new Tab
                {
                    Title = "Nómina",
                    Icon = "cash.png"
                };
                tabNomina.Items.Add(new ShellContent
                {
                    Title = "Nómina",
                    ContentTemplate = new DataTemplate(typeof(AppGestorVentas.Views.NominaViews.NominaView))
                });
                tabBar.Items.Add(tabNomina);


                // Pestaña Historico
                var tabHistorico = new Tab
                {
                    Title = "Histórico",
                    Icon = "clock.png"
                };
                tabHistorico.Items.Add(new ShellContent
                {
                    Title = "Histórico",
                    ContentTemplate = new DataTemplate(typeof(HistoricoView))
                });

                tabBar.Items.Add(tabHistorico);
            }

            // Agregar el TabBar a la colección de Items de la Shell.
            Items.Add(tabBar);
        }


        private View CreateFlyoutHeader(string sNombreUsuario)
        {
            // Crear un Grid con 4 filas.
            var grid = new Grid
            {
                Padding = 20,
                RowDefinitions = new RowDefinitionCollection
        {
            new RowDefinition { Height = new GridLength(250) },
            new RowDefinition { Height = new GridLength(50) },
            new RowDefinition { Height = new GridLength(70) },
            new RowDefinition { Height = new GridLength(70) },
        }
                // Si es necesario, puedes definir ColumnDefinitions aquí.
            };

            // Imagen (logo).
            var logoImage = new Image
            {
                Source = "logo.png",
                HeightRequest = 200,
                WidthRequest = 200,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start
            };
            grid.Children.Add(logoImage);
            Grid.SetRow(logoImage, 0);
            // Grid.SetColumn(logoImage, 0); // Opcional, si tienes más de una columna

            // Label con el nombre.
            var nameLabel = new Label
            {
                Text = sNombreUsuario,
                HorizontalOptions = LayoutOptions.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            grid.Children.Add(nameLabel);
            Grid.SetRow(nameLabel, 1);

            // Botón Perfil.
            var perfilButton = new Button
            {
                Text = "Perfil",
                WidthRequest = 150,
                HeightRequest = 50,
            };
            perfilButton.Clicked += OnPerfilClicked; // Puedes asignar otro método si la lógica es distinta
            grid.Children.Add(perfilButton);
            Grid.SetRow(perfilButton, 2);

            // Botón Cerrar Sesión.
            var logoutButton = new Button
            {
                Text = "Cerrar Sesión",
                WidthRequest = 150,
                HeightRequest = 50,
                BackgroundColor = (Color)Application.Current.Resources["Secondary"]
            };
            logoutButton.Clicked += OnLogoutClicked;
            grid.Children.Add(logoutButton);
            Grid.SetRow(logoutButton, 3);

            return grid;
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Conectar si aún no lo está.
            if (!_socketIoService.IsConnected)
            {
                await _socketIoService.ConnectAsync();
            }
        }

        private void OnPerfilClicked(object sender, EventArgs e)
        {
            Shell.Current.GoToAsync("perfil");
            Shell.Current.FlyoutIsPresented = false;
        }


        private void OnLogoutClicked(object sender, EventArgs e)
        {
            AdministradorSesion.ClearSessionKeys();
            if (Application.Current is App app)
            {
                var page = app.Services.GetRequiredService<LoginView>();
                app.MainPage = page; // O, si deseas usar la pila de navegación:
            }
        }

    }
}

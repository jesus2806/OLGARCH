using AppGestorVentas.Views.LoginViews;

namespace AppGestorVentas
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; }
        public App(LoginView loginView, IServiceProvider services)
        {
            InitializeComponent();
            Services = services;
            Application.Current.UserAppTheme = AppTheme.Light;
            MainPage = loginView;
        }

        //protected override Window CreateWindow(IActivationState? activationState)
        //{
        //    try
        //    {
        //        var windows = new Window(new AppShell());
        //        //var windows = new Window(new LoginView());


        //        windows.Height = windows.MaximumHeight;
        //        windows.Width = windows.MaximumWidth;

        //        return windows;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"ERRR: {ex.Message}");
        //        return null;
        //    }

        //}
    }
}
using AppGestorVentas.ViewModels.ProductoViewModels;
using Microsoft.Maui.ApplicationModel;

namespace AppGestorVentas.Views.ProductoVierws
{
    public partial class DatosProductoView : ContentPage
    {
        public DatosProductoView(DatosProductoViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

            Shell.SetTabBarIsVisible(this, false);
            Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (OperatingSystem.IsAndroid())
            {
                VistaEscritorio.IsEnabled = false;
                VistaEscritorio.IsVisible = false;

                VistaAndroid.IsVisible = true;
                VistaAndroid.IsEnabled = true;
            }
            else
            {
                VistaAndroid.IsVisible = false;
                VistaAndroid.IsEnabled = false;

                VistaEscritorio.IsEnabled = true;
                VistaEscritorio.IsVisible = true;
            }

            // ✅ Cargar ingredientes al entrar
            if (BindingContext is DatosProductoViewModel vm)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await vm.InitIngredientes();
                });
            }
        }

        private void OnCambioTipoProducto(object? sender, EventArgs e)
        {
            if (BindingContext is DatosProductoViewModel vm)
                vm.CambioTipoProducto();
        }

        private async void OnBuscarIngredienteTextChanged(object sender, TextChangedEventArgs e)
        {
            if (BindingContext is DatosProductoViewModel vm)
            {
                vm.SBusquedaIngrediente = e.NewTextValue ?? string.Empty;

                // ✅ Asegura cache (solo la 1a vez hace llamada)
                await vm.InitIngredientes();

                // ✅ Ejecuta el filtro (insensible a may/min y acentos)
                vm.BuscarIngredientes();
            }
        }
    }
}
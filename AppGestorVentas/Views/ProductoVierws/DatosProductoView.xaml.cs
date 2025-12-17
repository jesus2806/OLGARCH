using AppGestorVentas.ViewModels.ProductoViewModels;
using System.Net.WebSockets;

namespace AppGestorVentas.Views.ProductoVierws;

public partial class DatosProductoView : ContentPage
{
	public DatosProductoView(DatosProductoViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
        Shell.SetTabBarIsVisible(this, false);
        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled); // Deshabilita el boton del menu lateral
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            if (OperatingSystem.IsAndroid())
            {
                VistaEscritorio.IsEnabled = false;
                VistaEscritorio.IsVisible = false;
                VistaAndroid.IsVisible = true;
                VistaAndroid.IsEnabled = true;
            }
            else if (OperatingSystem.IsWindows())
            {
                VistaAndroid.IsVisible = false;
                VistaAndroid.IsEnabled = false;
                VistaEscritorio.IsEnabled = true;
                VistaEscritorio.IsVisible = true;
            }

        } catch (Exception ex) { }
    }





    private void OnCambioTipoProducto(object? sender, EventArgs e)
    {
        if (BindingContext is DatosProductoViewModel vm)
        {
            vm.CambioTipoProducto();
        }
    }


}
using AppGestorVentas.ViewModels.OrdenViewModels;

namespace AppGestorVentas.Views.OrdenViews;

public partial class ProductoOrdenView : ContentPage
{
	public ProductoOrdenView(ProductoOrdenViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
        Shell.SetTabBarIsVisible(this, false);
        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled); // Deshabilita el boton del menu lateral
    }

    private async void BusquedaEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (BindingContext is ProductoOrdenViewModel vm)
        {
            //await vm.OnBusquedaProductosTextChanged(e.NewTextValue);
        }
    }

    private async void BusquedaEntry_ExtrasTextChanged(object sender, TextChangedEventArgs e)
    {
        if (BindingContext is ProductoOrdenViewModel vm)
        {
            //await vm.OnBusquedaExtrasTextChanged(e.NewTextValue);
        }
    }

}
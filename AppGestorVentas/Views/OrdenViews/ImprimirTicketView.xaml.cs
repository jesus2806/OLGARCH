using AppGestorVentas.ViewModels.OrdenViewModels;
using System.Net.WebSockets;

namespace AppGestorVentas.Views.OrdenViews;

public partial class ImprimirTicketView : ContentPage
{
	public ImprimirTicketView(ImprimirTicketViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
        Shell.SetTabBarIsVisible(this, false);
        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled); // Deshabilita el boton del menu lateral
    }


    // Cuando la página aparece, llamamos al método que carga los dispositivos guardados
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ImprimirTicketViewModel vm)
        {

#if ANDROID
            await vm.ScanDevicesCommand.ExecuteAsync(null);
#endif
            //#if WINDOWS
            //            await vm.LoadCachedDevicesAsync();
            //#endif
        }
    }


}
using AppGestorVentas.Classes;
using AppGestorVentas.ViewModels.OrdenViewModels;

namespace AppGestorVentas.Views.OrdenViews;

public partial class AdministracionOrdenView : ContentPage
{
    public AdministracionOrdenView(AdministracionOrdenViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void AgregarOrden(object sender, EventArgs e)
    {
        if (BindingContext is AdministracionOrdenViewModel vm)
        {
            await vm.CrearNuevaOrden();
        }
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (OperatingSystem.IsAndroid())
        {
            VistaEscritorio.IsEnabled = false;
            VistaEscritorio.IsVisible = false;
            VistaAndroid.IsVisible = true;
            VistaAndroid.IsEnabled = true;
            // Código específico para Android
        }
        else if (OperatingSystem.IsWindows())
        {
            VistaAndroid.IsVisible = false;
            VistaAndroid.IsEnabled = false;
            VistaEscritorio.IsEnabled = true;
            VistaEscritorio.IsVisible = true;
        }

        //
        

        if (BindingContext is AdministracionOrdenViewModel viewModel)
        {
            viewModel.ConectarEvento();
            viewModel.EstablecerVisivilidadBotonPorRol();
            await viewModel.ObtenerListadoOrdenesAPI();
        }

    }





    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Aquí colocas el código que se ejecuta cuando la página deja de estar activa

        if (BindingContext is AdministracionOrdenViewModel vm)
        {
            vm.DesconectarEvento();
        }
    }



}
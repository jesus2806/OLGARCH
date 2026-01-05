using AppGestorVentas.ViewModels.AsistenciaViewModels;

namespace AppGestorVentas.Views.AsistenciaViews;

public partial class PaseListaView : ContentPage
{
    public PaseListaView(PaseListaViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PaseListaViewModel vm)
            await vm.CargarAsync();
    }
}

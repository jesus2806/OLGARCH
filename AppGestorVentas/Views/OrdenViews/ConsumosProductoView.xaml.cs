using AppGestorVentas.ViewModels.OrdenViewModels;

namespace AppGestorVentas.Views.OrdenViews;

public partial class ConsumosProductoView : ContentPage
{
    private readonly ConsumosProductoViewModel _viewModel;

    public ConsumosProductoView(ConsumosProductoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        Shell.SetTabBarIsVisible(this,false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }
}

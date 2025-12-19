using AppGestorVentas.ViewModels.OrdenViewModels;

namespace AppGestorVentas.Views.OrdenViews;

public partial class BuscarExtrasView : ContentPage
{
    private readonly BuscarExtrasViewModel _viewModel;

    public BuscarExtrasView(BuscarExtrasViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        Shell.SetTabBarIsVisible(this, false);
    }
}

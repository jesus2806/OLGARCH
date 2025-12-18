using AppGestorVentas.ViewModels;

namespace AppGestorVentas.Views.Components;

public partial class SyncButtonView : ContentView
{
    public SyncButtonView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Establece el ViewModel desde fuera del componente
    /// </summary>
    public void SetViewModel(SyncViewModel viewModel)
    {
        BindingContext = viewModel;
    }
}

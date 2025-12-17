using AppGestorVentas.ViewModels.Popup;
using CommunityToolkit.Maui.Views;

namespace AppGestorVentas.Views.Popup;

public partial class IndicacionesOrdenPopup : CommunityToolkit.Maui.Views.Popup
{
    public IndicacionesOrdenPopup(IndicacionesOrdenPopupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

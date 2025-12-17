namespace AppGestorVentas.Views.Popup;

using AppGestorVentas.ViewModels.Popup;
using CommunityToolkit.Maui.Views;

public partial class CargaGeneralPopup : Popup
{
	public CargaGeneralPopup(CargaGeneralPopupViewModel cargaGeneralPopupViewModel)
	{
		InitializeComponent();
        BindingContext = cargaGeneralPopupViewModel;

    }
}
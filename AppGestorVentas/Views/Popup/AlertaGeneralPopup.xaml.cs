
namespace AppGestorVentas.Views.Popup;

using AppGestorVentas.ViewModels.Popup;
using CommunityToolkit.Maui.Views;

public partial class AlertaGeneralPopup : Popup

{
	public AlertaGeneralPopup(AlertaGeneralPopupViewModel alertaGeneralPopupViewModel)
	{
		InitializeComponent();
		BindingContext = alertaGeneralPopupViewModel;

    }
}

namespace AppGestorVentas.Views.Popup;

using AppGestorVentas.Classes;
using AppGestorVentas.ViewModels.Popup;
using CommunityToolkit.Maui.Views;

public partial class CrearOrdenPopup : Popup
{
	public CrearOrdenPopup(CrearOrdenPopupViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
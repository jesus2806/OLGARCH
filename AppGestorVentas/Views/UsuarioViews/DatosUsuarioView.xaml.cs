
using AppGestorVentas.ViewModels.UsuarioViewModels;

namespace AppGestorVentas.Views.UsuariosViews;

public partial class DatosUsuarioView : ContentPage
{
	public DatosUsuarioView(DatosUsuarioViewModel datosUsuarioViewModel)
	{
		InitializeComponent();
		BindingContext = datosUsuarioViewModel;
        Shell.SetTabBarIsVisible(this, false);
        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled); // Deshabilita el boton del menu lateral
    }








}
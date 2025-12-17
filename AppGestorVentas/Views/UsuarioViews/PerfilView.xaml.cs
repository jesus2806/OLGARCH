using AppGestorVentas.Helpers;
using AppGestorVentas.ViewModels.UsuarioViewModels;

namespace AppGestorVentas.Views.UsuarioViews;

public partial class PerfilView : ContentPage
{
	public PerfilView(PerfilViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
        Shell.SetTabBarIsVisible(this, false);
        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled); // Deshabilita el boton del menu lateral
    }


    #region OnAppearing

    /// <summary>
    /// Se ejecuta cuando la p�gina aparece, verificando la sesi�n y redirigiendo o expandiendo el contenedor seg�n el resultado.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var viewModel = BindingContext as PerfilViewModel;
        if (viewModel != null)
        {
            await viewModel.InicializarDatos();
        }

    }

    #endregion


    #region MostrarOcultarPass

    /// <summary>
    /// Alterna la visibilidad de la contrase�a, mostrando u ocultando el texto.
    /// </summary>
    /// <param name="sender">El control que activ� el evento.</param>
    /// <param name="e">Argumentos del evento.</param>
    public void MostrarOcultarPass(object sender, EventArgs e)
    {
        pass.IsPassword = !pass.IsPassword;

        if (sender is Button button)
        {
            button.Text = pass.IsPassword ? MaterialDesignIcons.Eye : MaterialDesignIcons.EyeOff;
        }

    }

    #endregion



}
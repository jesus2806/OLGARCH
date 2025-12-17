using AppGestorVentas.Helpers;
using AppGestorVentas.ViewModels.LoginViewModels;
using AppGestorVentas.ViewModels.Popup;
using CommunityToolkit.Maui.Core;

namespace AppGestorVentas.Views.LoginViews;

public partial class LoginView : ContentPage
{
    private IPopupService _oPopupService;

    public LoginView(LoginViewModel viewModel, IPopupService popupService)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _oPopupService = popupService;
    }


    #region OnAppearing

    /// <summary>
    /// Se ejecuta cuando la p�gina aparece, verificando la sesi�n y redirigiendo o expandiendo el contenedor seg�n el resultado.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var viewModel = BindingContext as LoginViewModel;
        if (viewModel != null) {
            await viewModel.ComprobarSesion();
        }

    }

    #endregion



    #region OnEntryCompleted

    /// <summary>
    /// Maneja el evento cuando se completa la entrada (Entry). Ejecuta el comando de ingreso si es posible.
    /// </summary>
    /// <param name="sender">El control de entrada que activ� el evento.</param>
    /// <param name="e">Argumentos del evento.</param>
    private void OnEntryCompleted(object sender, EventArgs e)
    {
        var viewModel = BindingContext as LoginViewModel;
        //if (viewModel?.IngresarCommand.CanExecute(null) == true)
        //{
        //    viewModel.IngresarCommand.Execute(null);
        //}
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


    #region OnBackgroundTapped

    /// <summary>
    /// Maneja el evento de toque en el fondo, colapsando el contenedor animado si est� expandido.
    /// </summary>
    /// <param name="sender">El origen del evento.</param>
    /// <param name="e">Argumentos del evento.</param>
    private void OnBackgroundTapped(object sender, EventArgs e)
    {
        if(!user.IsFocused && !pass.IsFocused)
        {
            ToggleControl(user);
            ToggleControl(pass);
        }
        // Alterna el estado de habilitaci�n de los controles user y pass
    }

    #endregion

    #region OnBackButtonPressed

    /// <summary>
    /// Maneja el evento de presionar el bot�n de retroceso.
    /// </summary>
    /// <returns>
    /// Un valor booleano que indica si se debe cancelar la navegaci�n hacia atr�s (true) 
    /// o permitirla (false).
    /// </returns>
    protected override bool OnBackButtonPressed()
    {
        // Alterna el estado de habilitaci�n de los controles user y pass
        ToggleControl(user);
        ToggleControl(pass);

        // Nota: Retorna true para cancelar la navegaci�n hacia atr�s, o false para permitirla.
        return false;
    }

    #endregion

    #region ToggleControl

    /// <summary>
    /// Alterna el estado de habilitaci�n de un control visual.
    /// </summary>
    /// <param name="control">El control visual cuyo estado de habilitaci�n se alternar�.</param>
    private void ToggleControl(VisualElement control)
    {
        if (control.IsEnabled)
        {
            // Deshabilita y vuelve a habilitar el control para refrescar su estado
            control.IsEnabled = false;
            control.IsEnabled = true;
        }
    }

    #endregion


}
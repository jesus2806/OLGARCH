using AppGestorVentas.Classes;
using AppGestorVentas.ViewModels.OrdenViewModels;

namespace AppGestorVentas.Views.OrdenViews;

public partial class DatosOrdenCocinaView : ContentPage
{
	public DatosOrdenCocinaView(DatosOrdenViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
        Shell.SetTabBarIsVisible(this, false);
        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled); // Deshabilita el boton del menu lateral
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("Test", "El botón fue clickeado", "OK");
    }



    #region OnAppearing

    /// <summary>
    /// Se ejecuta cuando la p�gina aparece en pantalla.
    /// Carga los datos del ViewModel.
    /// </summary>
    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            int iRol = int.Parse(await AdministradorSesion.GetAsync(KeysSesion.iRol));

            if (iRol == 1) // Admin
            {

            }

            // Llama al m�todo de carga de datos en el ViewModel
            if (BindingContext is DatosOrdenViewModel viewModel)
            {
                //await viewModel.LoadDataApi();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERRR: {ex.Message}");
        }
    }

    #endregion

}
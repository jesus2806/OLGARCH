using AppGestorVentas.ViewModels.UsuarioViewModels;
using System;

namespace AppGestorVentas.Views.UsuariosViews;

public partial class AdministracionUsuariosView : ContentPage
{
	public AdministracionUsuariosView(AdministracionUsuariosViewModel viewModel)
	{
        try
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERRR: {ex.Message}");
        }
	}

    #region OnAppearing

    /// <summary>
    /// Se ejecuta cuando la p�gina aparece en pantalla.
    /// Carga los datos del ViewModel y calcula las alturas iniciales del panel inferior.
    /// </summary>
    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            if (OperatingSystem.IsAndroid())
            {
                VistaEscritorio.IsEnabled = false;
                VistaEscritorio.IsVisible = false;
                VistaAndroid.IsVisible = true;
                VistaAndroid.IsEnabled = true;
                // Código específico para Android
            }
            else if (OperatingSystem.IsWindows())
            {
                VistaAndroid.IsVisible = false;
                VistaAndroid.IsEnabled = false;
                VistaEscritorio.IsEnabled = true;
                VistaEscritorio.IsVisible = true;
            }

            // Llama al m�todo de carga de datos en el ViewModel
            if (BindingContext is AdministracionUsuariosViewModel viewModel)
            {
                await viewModel.ObtenerListadoUsuariosAPI();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERRR: {ex.Message}");
        }
    }

    #endregion
}
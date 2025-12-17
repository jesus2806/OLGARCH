using AppGestorVentas.ViewModels.HistoricoViewModels;

namespace AppGestorVentas.Views.HistoricoViews;

public partial class HistoricoView : ContentPage
{
	public HistoricoView(HistoricoViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    #region OnAppearing

    /// <summary>
    /// Se ejecuta cuando la p�gina aparece, verificando la sesi�n y redirigiendo o expandiendo el contenedor seg�n el resultado.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var viewModel = BindingContext as HistoricoViewModel;
        if (viewModel != null)
        {
            await viewModel.InicializarDatos();
            await viewModel.ObtenerRegistroHistorico();
        }

    }

    #endregion


}
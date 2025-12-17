using AppGestorVentas.ViewModels.ProductoViewModels;

namespace AppGestorVentas.Views.ProductoVierws;

public partial class AdministracionProductosView : ContentPage
{
    Double iAlturaPantalla = 0;
	public AdministracionProductosView(AdministracionProductosViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
        SetTabSelected("Platillos");
        iAlturaPantalla = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            if (OperatingSystem.IsAndroid())
            {
                btnFlotante.InputTransparent = false;
                btnFlotante.IsEnabled = true;
                btnFlotante.IsVisible = true;
                ContenedorProductos.HeightRequest = iAlturaPantalla - 230;
                VistaEscritorio.IsEnabled = false;
                VistaEscritorio.IsVisible = false;
                VistaAndroid.IsVisible = true;
                VistaAndroid.IsEnabled = true;
                // Código específico para Android
            }
            else if (OperatingSystem.IsWindows())
            {
                btnFlotante.InputTransparent = true;
                btnFlotante.IsEnabled = false;
                btnFlotante.IsVisible = false;
                VistaAndroid.IsVisible = false;
                VistaAndroid.IsEnabled = false;
                VistaEscritorio.IsEnabled = true;
                VistaEscritorio.IsVisible = true;
            }

            if (BindingContext is AdministracionProductosViewModel vm)
            {
                await vm.ObtenerListadoProductosAPI();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERRR: {ex.Message}");
        }
    }


    private void SetTabSelected(string tab)
    {
        // Reiniciamos el estilo de los tres botones.
        btnPlatillos.BackgroundColor = (Color)Application.Current.Resources["Primary"];
        btnPlatillos.TextColor = Colors.White;

        btnBebidas.BackgroundColor = (Color)Application.Current.Resources["Primary"];
        btnBebidas.TextColor = Colors.White;

        btnExtras.BackgroundColor = (Color)Application.Current.Resources["Primary"];
        btnExtras.TextColor = Colors.White;

        // Resaltamos el botón seleccionado.
        switch (tab)
        {
            case "Platillos":
                btnPlatillos.BackgroundColor = (Color)Application.Current.Resources["PrimaryDark"];
                btnPlatillos.TextColor = Colors.White;
                break;
            case "Bebidas":
                btnBebidas.BackgroundColor = (Color)Application.Current.Resources["PrimaryDark"];
                btnBebidas.TextColor = Colors.White;
                break;
            case "Extras":
                btnExtras.BackgroundColor = (Color)Application.Current.Resources["PrimaryDark"];
                btnExtras.TextColor = Colors.White;
                break;
        }
    }

    private void OnPlatillosClicked(object sender, EventArgs e)
    {
        viewPlatillos.IsVisible = true;
        viewBebidas.IsVisible = false;
        viewExtras.IsVisible = false;
        SetTabSelected("Platillos");
    }

    private void OnBebidasClicked(object sender, EventArgs e)
    {
        viewPlatillos.IsVisible = false;
        viewBebidas.IsVisible = true;
        viewExtras.IsVisible = false;
        SetTabSelected("Bebidas");
    }

    private void OnExtrasClicked(object sender, EventArgs e)
    {
        viewPlatillos.IsVisible = false;
        viewBebidas.IsVisible = false;
        viewExtras.IsVisible = true;
        SetTabSelected("Extras");
    }



}
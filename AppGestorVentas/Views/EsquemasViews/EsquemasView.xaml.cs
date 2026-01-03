using AppGestorVentas.ViewModels.EsquemaViewModels;

namespace AppGestorVentas.Views.EsquemasViews
{
    public partial class EsquemasView : ContentPage
    {
        public EsquemasView(EsquemasViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as EsquemasViewModel)?.CargarCommand.Execute(null);
        }
    }
}

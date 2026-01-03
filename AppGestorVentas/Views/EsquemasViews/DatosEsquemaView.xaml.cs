using AppGestorVentas.ViewModels.EsquemaViewModels;

namespace AppGestorVentas.Views.EsquemasViews
{
    public partial class DatosEsquemaView : ContentPage
    {
        public DatosEsquemaView(DatosEsquemaViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}

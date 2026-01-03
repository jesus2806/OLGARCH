using AppGestorVentas.ViewModels.IngredienteViewModels;

namespace AppGestorVentas.Views.IngredientesViews
{
    public partial class DatosIngredienteView : ContentPage
    {
        public DatosIngredienteView(DatosIngredienteViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}

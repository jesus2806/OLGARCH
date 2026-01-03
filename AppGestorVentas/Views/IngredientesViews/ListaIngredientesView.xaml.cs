using AppGestorVentas.ViewModels.IngredienteViewModels;

namespace AppGestorVentas.Views.IngredientesViews
{
    public partial class ListaIngredientesView : ContentPage
    {
        private readonly ListaIngredientesViewModel _vm;

        public ListaIngredientesView(ListaIngredientesViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = _vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.CargarIngredientesAsync();
        }
    }
}

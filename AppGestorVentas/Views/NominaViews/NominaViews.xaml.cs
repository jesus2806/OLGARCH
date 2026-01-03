using AppGestorVentas.ViewModels.NominaViewModels;

namespace AppGestorVentas.Views.NominaViews
{
    public partial class NominaView : ContentPage
    {
        public NominaView(NominaViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is NominaViewModel vm)
            {
                vm.InitCommand.Execute(null);
            }
        }
    }
}

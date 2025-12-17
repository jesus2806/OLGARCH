using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppGestorVentas.ViewModels.Popup
{
    public partial class AlertaGeneralPopupViewModel : ObservableObject
    {

        private IPopupService _oPopupService;

        [ObservableProperty]
        private string sMensaje = string.Empty;


        public AlertaGeneralPopupViewModel(IPopupService popupService)
        {
            _oPopupService = popupService;
        }


        public void EstablecerMsj(string sMensaje)
        {
            SMensaje = sMensaje;
        }

        [RelayCommand]
        public async Task Cerrar()
        {
            await _oPopupService.ClosePopupAsync(true);
        }


    }
}

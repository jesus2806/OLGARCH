using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AppGestorVentas.ViewModels.Popup
{
    public partial class CargaGeneralPopupViewModel : ObservableObject
    {
        private IPopupService _oPopupService;

        public CargaGeneralPopupViewModel(IPopupService oPopupService) {
            _oPopupService = oPopupService;
        }


        public async Task Cerrar()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await _oPopupService.ClosePopupAsync(true);
            });
        }        
        
        
        //public async Task Cerrar()
        //{
        //    await _oPopupService.ClosePopupAsync(true);
        //}
    }
}

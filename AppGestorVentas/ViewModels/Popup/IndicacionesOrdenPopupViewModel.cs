using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppGestorVentas.ViewModels.Popup
{
    /// <summary>
    /// ViewModel para el popup de indicaciones de la orden (Pantalla 6)
    /// </summary>
    public partial class IndicacionesOrdenPopupViewModel : ObservableObject
    {
        private readonly IPopupService _popupService;

        [ObservableProperty]
        private string sIndicaciones = string.Empty;

        [ObservableProperty]
        private string sTitulo = "Indicaciones de la Orden";

        public Action<string>? OnGuardar { get; set; }

        public IndicacionesOrdenPopupViewModel(IPopupService popupService)
        {
            _popupService = popupService;
        }

        [RelayCommand]
        private async Task Guardar()
        {
            OnGuardar?.Invoke(SIndicaciones);
            await _popupService.ClosePopupAsync();
        }

        [RelayCommand]
        private async Task Cancelar()
        {
            await _popupService.ClosePopupAsync();
        }
    }
}

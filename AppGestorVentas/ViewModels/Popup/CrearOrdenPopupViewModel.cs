using AppGestorVentas.Classes;
using AppGestorVentas.Models;
using AppGestorVentas.Services;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace AppGestorVentas.ViewModels.Popup
{
    /// <summary>
    /// ViewModel para el popup de crear nueva orden.
    /// La orden se crea LOCALMENTE y no se envía al backend hasta "Tomar Orden".
    /// </summary>
    public partial class CrearOrdenPopupViewModel : ObservableObject
    {
        #region SERVICIOS

        private readonly HttpApiService _httpApiService;
        private readonly IPopupService _popupService;
        private readonly OrdenDraftService _ordenDraftService;
        private List<int> _lstMesasOcupadas = new();

        #endregion

        #region PROPIEDADES

        [ObservableProperty]
        private ObservableCollection<int> lstMesas;

        [ObservableProperty]
        private int iMesa;

        [ObservableProperty]
        private bool isLoading;

        #endregion

        #region CONSTRUCTOR

        public CrearOrdenPopupViewModel(
            HttpApiService httpApiService, 
            IPopupService popupService,
            OrdenDraftService ordenDraftService)
        {
            _httpApiService = httpApiService;
            _popupService = popupService;
            _ordenDraftService = ordenDraftService;

            LstMesas = new ObservableCollection<int>(Enumerable.Range(1, 50));
        }

        #endregion

        #region INICIALIZACIÓN

        public async Task InitializeAsync()
        {
            await ObtenerMesasOcupadasAsync();
            if (LstMesas.Any())
            {
                IMesa = LstMesas.First();
            }
        }

        private async Task ObtenerMesasOcupadasAsync()
        {
            try
            {
                var response = await _httpApiService.GetAsync("api/ordenes/mesas-ocupadas");

                if (response != null && response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<int>>();

                    if (apiResponse?.bSuccess == true && apiResponse.lData != null)
                    {
                        _lstMesasOcupadas = apiResponse.lData;
                        
                        foreach (var mesaOcupada in _lstMesasOcupadas)
                        {
                            if (LstMesas.Contains(mesaOcupada))
                            {
                                LstMesas.Remove(mesaOcupada);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener mesas ocupadas: {ex.Message}");
            }
        }

        #endregion

        #region CREAR ORDEN

        [RelayCommand]
        public async Task CrearOrden()
        {
            IsLoading = true;

            try
            {
                // Obtener datos del usuario actual
                string sIdMongoDBUsuario = await AdministradorSesion.GetAsync(KeysSesion.sIdUsuarioMongoDB);
                string sUsuarioMesero = await AdministradorSesion.GetAsync(KeysSesion.sNombreUsuario);

                // Generar identificador único
                string identificador = Guid.NewGuid().ToString();

                // Crear orden LOCALMENTE usando OrdenDraftService
                await _ordenDraftService.IniciarNuevaOrdenAsync(
                    identificador,
                    IMesa,
                    sUsuarioMesero,
                    sIdMongoDBUsuario);

                // Cerrar popup
                await _popupService.ClosePopupAsync(true);

                // Navegar a la vista de detalles con el ID local
                var idOrden = _ordenDraftService.OrdenActual?.sIdLocal ?? string.Empty;
                
                await Shell.Current.GoToAsync("datosOrdenes", new Dictionary<string, object>
                {
                    { "sIdOrden", idOrden },
                    { "bEsNueva", true }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear la orden: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", $"No se pudo crear la orden: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}

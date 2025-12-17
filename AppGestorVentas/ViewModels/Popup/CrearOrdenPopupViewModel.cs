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
    public partial class CrearOrdenPopupViewModel : ObservableObject
    {
        private HttpApiService _httpApiService;
        private IPopupService _oIPopupService;
        private List<int> _lstMesasOcupadas = new List<int>();

        [ObservableProperty]
        private ObservableCollection<int> lstMesas;

        [ObservableProperty]
        private int iMesa;

        // Constructor sin operaciones asíncronas
        public CrearOrdenPopupViewModel(HttpApiService httpApiService, IPopupService popupService)
        {
            _httpApiService = httpApiService;
            _oIPopupService = popupService;

            LstMesas = new ObservableCollection<int>()
            {
                1,2,3,4,5,6,7,8,9,10,
                11,12,13,14,15,16,17,18,19,20,
                21,22,23,24,25,26,27,28,29,30,
                31,32,33,34,35,36,37,38,39,40,
                41,42,43,44,45,46,47,48,49,50
            };
        }

        // Método de inicialización que debe ser llamado antes de mostrar el popup
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
            var response = await _httpApiService.GetAsync("api/ordenes/mesas-ocupadas");

            if (response != null)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<int>>();

                if (apiResponse != null && apiResponse.bSuccess && apiResponse.lData != null)
                {
                    _lstMesasOcupadas = apiResponse.lData;
                    // Recorremos las mesas ocupadas y las removemos de LstMesas
                    foreach (var mesaOcupada in _lstMesasOcupadas)
                    {
                        if (LstMesas.Contains(mesaOcupada))
                        {
                            LstMesas.Remove(mesaOcupada);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Error al obtener mesas ocupadas.");
            }
        }

        [RelayCommand]
        public async Task CrearOrden()
        {
            // Construir el objeto que se enviará en el body de la petición
            string sIdMongoDBUsuario = await AdministradorSesion.GetAsync(KeysSesion.sIdUsuarioMongoDB);
            string sUsuarioMesero = await AdministradorSesion.GetAsync(KeysSesion.sNombreUsuario);

            var nuevaOrden = new
            {
                sIdentificadorOrden = Guid.NewGuid(),
                iMesa = IMesa,
                iTipoOrden = 1, // Orden primaria
                sUsuarioMesero,
                sIdMongoDBMesero = sIdMongoDBUsuario
            };

            try
            {
                // Realiza la petición POST a la ruta "nueva-orden"
                var response = await _httpApiService.PostAsync("api/nueva-orden", nuevaOrden);

                if (response != null)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<Orden>>();

                    if (apiResponse != null && apiResponse.bSuccess && apiResponse.lData != null)
                    {
                        Orden oOrdenNueva = apiResponse.lData[0];
                        await _oIPopupService.ClosePopupAsync(true);
                        await Shell.Current.GoToAsync("datosOrdenes", new Dictionary<string, object>
                        {
                            { "sIdOrden", oOrdenNueva.sIdMongoDB }
                        });
                    }
                }
                else
                {
                    Console.WriteLine("Error al crear la orden");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepción al crear la orden: {ex.Message}");
            }
        }
    }
}

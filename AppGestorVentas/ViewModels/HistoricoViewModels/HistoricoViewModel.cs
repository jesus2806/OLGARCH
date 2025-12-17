using AppGestorVentas.Models;
using AppGestorVentas.Services;
using AppGestorVentas.ViewModels.Popup;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace AppGestorVentas.ViewModels.HistoricoViewModels
{
    public partial class HistoricoViewModel : ObservableObject
    {

        private readonly HttpApiService _httpApiService;
        private readonly LocalDatabaseService _localDatabaseService;
        private IPopupService _IPopupService;

        [ObservableProperty]
        private ObservableCollection<RegistroHistorico> oRegistrosHistorico = new();

        [ObservableProperty]
        private decimal dTotalCostoPublico;

        [ObservableProperty]
        private decimal dTotalCostoReal;

        [ObservableProperty]
        private decimal dTotalGanancia;

        [ObservableProperty]
        private decimal dTotalEfectivo;


        [ObservableProperty]
        private decimal dTotalTransferencia;

        [ObservableProperty]
        private DateTime dFechaConsulta;

        [ObservableProperty]
        private string sTituloDiaResumen;


        public HistoricoViewModel(HttpApiService httpApiService,
                                LocalDatabaseService localDatabaseService,
                                IPopupService popupService)
        {
            _httpApiService = httpApiService;
            _localDatabaseService = localDatabaseService;
            _IPopupService = popupService;
        }


        public async Task InicializarDatos()
        {
            DFechaConsulta = DateTime.Now;
            STituloDiaResumen = "Dia actual";
        }

        /// <summary>
        /// Obtiene el listado de órdenes desde la API, lo almacena en la base local y carga la primera página.
        /// </summary>
        public async Task ObtenerRegistroHistorico()
        {
            try
            {
                ORegistrosHistorico = [];
                DTotalGanancia = 0;
                DTotalCostoPublico = 0;
                DTotalCostoReal = 0;
                DTotalGanancia = 0;
                DTotalEfectivo = 0;
                DTotalTransferencia = 0;


                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }

                //PopupService popupService = new PopupService();
                await _IPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
                {
                    try
                    {
                        ORegistrosHistorico.Clear();

                        // Si la tabla no existe, se crea; de lo contrario se limpia.
                        if (!await _localDatabaseService.TableExistsAsync<Orden>())
                        {
                            await _localDatabaseService.CreateTableAsync<Orden>();
                        }
                        else
                        {
                            await _localDatabaseService.DeleteAllRecordsAsync<Orden>();
                        }

                        var fechaAjustada = DFechaConsulta.Date.AddHours(6).ToUniversalTime();

                        var Datos = new
                        {
                            dFecha = fechaAjustada
                        };

                        var response = await _httpApiService.PostAsync("api/historico/resumen", Datos, bRequiereToken: true);
                        if (response != null && response.IsSuccessStatusCode)
                        {
                            var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<Historico>>();
                            if (apiResponse != null && apiResponse.bSuccess &&
                                apiResponse.lData != null && apiResponse.lData.Count > 0)
                            {
                                Historico oHistorico = (Historico)apiResponse.lData[0];

                                DTotalCostoPublico = oHistorico.Totals.TotalCostoPublico;

                                DTotalCostoPublico = Math.Round(DTotalCostoPublico, 2);

                                DTotalCostoReal = oHistorico.Totals.TotalCostoReal;

                                DTotalCostoReal = Math.Round(DTotalCostoReal, 2);

                                if (DTotalCostoPublico != 0 && DTotalCostoReal != 0)
                                    DTotalGanancia = DTotalCostoPublico - DTotalCostoReal;

                                DTotalEfectivo = oHistorico.Totals.TotalEfectivo;

                                DTotalEfectivo = Math.Round(DTotalEfectivo, 2);

                                DTotalTransferencia = oHistorico.Totals.TotalBanco;

                                DTotalTransferencia = Math.Round(DTotalTransferencia, 2);

                                foreach (RegistroHistorico oRegistroHistorico in oHistorico.Registros)
                                {
                                    try
                                    {
                                        ORegistrosHistorico.Add(oRegistroHistorico);
                                    }
                                    catch (Exception ex)
                                    {
                                        MostrarError($"Ocurrió un error inesperado al insertar el producto: {ex.Message} {ex.StackTrace}");
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MostrarError($"Ocurrió un error al obtener el listado de productos desde la API: {ex.Message} {ex.StackTrace}");
                    }
                    finally
                    {
                        await vm.Cerrar();
                    }
                });
            }
            catch (Exception ex)
            {
                MostrarError($"Ocurrió un error al obtener el listado de productos desde la API: {ex.Message} {ex.StackTrace}");
            }
        }

        [RelayCommand]
        public async Task ConsultarHistorico()
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                return;
            }

            await ObtenerRegistroHistorico();
            STituloDiaResumen = $"Resumen histórico del  {DFechaConsulta.ToString("dd/MM/yyyy")}";
        }


        [RelayCommand]
        public async Task ImprimirCorte()
        {
            try
            {
                Corte oCorte = new Corte();
                oCorte.dTotalEfectivo = DTotalEfectivo;
                oCorte.dTotalTransferencia = DTotalTransferencia;
                oCorte.dTotalGanancia = DTotalGanancia;
                oCorte.dTotalCostoPublico = DTotalCostoPublico;
                oCorte.dTotalCostoReal = DTotalCostoReal;
                oCorte.dFechaCorte = DFechaConsulta;


                await Shell.Current.GoToAsync("impresora", new Dictionary<string, object>
                {
                    { "oCorte", oCorte },
                });
            }
            catch (Exception ex)
            {

                throw;
            }
        }


        [RelayCommand]
        public async Task ImprimirTicketOrden(string sIdOrdenPrimaria)
        {
            bool bConfirmar = false;
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }

                bConfirmar = await Shell.Current.DisplayAlert("Confirmar", "¿Deseas imprimir el ticket de esta orden?", "Sí", "No");

                if (bConfirmar)
                {
                    await Shell.Current.GoToAsync("impresora", new Dictionary<string, object>
                                        {
                                            { "sIdOrden",  sIdOrdenPrimaria}
                                        });
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error en ImprimirTicketOrden: {ex.Message} {ex.StackTrace}");
            }
        }


        #region MostrarError

        private async void MostrarError(string sMensaje)
        {
            try
            {
                var mainPage = Application.Current?.Windows[0].Page;
                if (mainPage != null)
                {
                    await mainPage.DisplayAlert("Error", sMensaje, "OK");
                }
            }
            catch (Exception ex)
            {

            }
        }

        #endregion





    }
}

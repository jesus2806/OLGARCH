using AppGestorVentas.Classes;
using AppGestorVentas.Models;
using AppGestorVentas.Services;
using AppGestorVentas.ViewModels.Popup;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;

namespace AppGestorVentas.ViewModels.OrdenViewModels
{
    public partial class DatosOrdenViewModel : ObservableObject, IQueryAttributable
    {
        #region PROPIEDADES

        private HttpApiService _oHttpApiService;
        private IPopupService _oIPopupService;
        private SocketIoService _oSocketIoService;

        [ObservableProperty]
        private ObservableCollection<OrdenProducto> lstOrdenProducto;

        [ObservableProperty]
        private Orden oOrden;

        [ObservableProperty]
        private string sNombreMesaro;

        private string sIdOrdenMongoDB = string.Empty;
        public string IdOrdenMongoDB => sIdOrdenMongoDB;

        [ObservableProperty]
        private bool bHabilitarAccionesEdicion;

        [ObservableProperty]
        private bool bHabilitarBotonTomarOrden;

        [ObservableProperty]
        private bool bHabilitarBotonPrepararOrden;

        [ObservableProperty]
        private bool bHabilitarBotonOrdenPreparada;

        // Propiedades para totales (Pantalla 1)
        [ObservableProperty]
        private decimal totalExtrasOrden;

        [ObservableProperty]
        private decimal totalOrden;

        #endregion


        #region CONSTRUCTORES 

        public DatosOrdenViewModel(HttpApiService httpApiService, IPopupService oIPopupService, SocketIoService socketIoService)
        {
            _oHttpApiService = httpApiService;
            LstOrdenProducto = new ObservableCollection<OrdenProducto>();
            _oIPopupService = oIPopupService;
            _oSocketIoService = socketIoService;
        }

        #endregion

        #region Manejo de Navegación

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("bSoloLectura", out var bValorSoloLectura) && bValorSoloLectura != null)
            {
                bool bSoloLectura = (bool)bValorSoloLectura;
                if (bSoloLectura)
                {
                    BHabilitarAccionesEdicion = false;
                }
            }
            else
            {
                BHabilitarAccionesEdicion = true;
            }


            if (query.TryGetValue("sIdOrden", out var sIdOrden) && sIdOrden != null)
            {
                sIdOrdenMongoDB = sIdOrden.ToString() ?? string.Empty;
            }

        }

        #endregion

        #region LoadDataApi

        public async Task LoadDataApi()
        {
            int iRol = -111;
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }


                iRol = int.Parse(await AdministradorSesion.GetAsync(KeysSesion.iRol));

                try
                {
                    OOrden = new();
                    LstOrdenProducto.Clear();
                    // Llamar a la API de productos
                    HttpResponseMessage? oRespuestaHTTP = await _oHttpApiService.GetAsync($"api/orden/{sIdOrdenMongoDB}");

                    if (oRespuestaHTTP != null && oRespuestaHTTP.IsSuccessStatusCode)
                    {
                        var oRespuestaAPI = await oRespuestaHTTP.Content.ReadFromJsonAsync<ApiRespuesta<Orden>>();

                        if (oRespuestaAPI != null && oRespuestaAPI.bSuccess)
                        {
                            Orden? oOrdenObtenida = oRespuestaAPI.lData!.FirstOrDefault();

                            if (oOrdenObtenida != null)
                            {
                                OOrden = oOrdenObtenida;

                                //SNombreMesaro = $"{oOrdenObtenida.oMesero.sNombre} {oOrdenObtenida.oMesero.sApellidoPaterno} {oOrdenObtenida.oMesero.sApellidoMaterno}";
                                SNombreMesaro = $"{oOrdenObtenida.sUsuarioMesero}";

                                if (oOrdenObtenida.aProductos != null && oOrdenObtenida.aProductos.Count > 0)
                                {
                                    LstOrdenProducto.Clear();

                                    foreach (OrdenProducto oOrdenProducto in oOrdenObtenida.aProductos)
                                    {
                                        oOrdenProducto.IsExpanded = false;
                                        SuscribirToggle(oOrdenProducto);
                                        LstOrdenProducto.Add(oOrdenProducto);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MostrarError($"Error al listar los productos: {ex.Message} {ex.StackTrace}");
                }
                finally
                {
                    //await vm.Cerrar();
                }

                //await _oIPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
                //{
              
                //});
            }
            catch (Exception ex)
            {
                MostrarError($"Error al eliminar el producto: {ex.Message} {ex.StackTrace}");
            }
            finally
            {
                if (LstOrdenProducto != null && LstOrdenProducto.Count > 0 &&
                    BHabilitarAccionesEdicion == true && OOrden.iEstatus == 0)
                {
                    BHabilitarBotonPrepararOrden = false;
                    BHabilitarBotonTomarOrden = true;
                }
                else
                {
                    BHabilitarBotonTomarOrden = false;

                    // ANTES
                    //if (OOrden.iEstatus == 1 && iRol == 3) // Estatus tomada
                    // AHORA
                    if (OOrden.iEstatus == 1) // Estatus tomada
                    {
                        BHabilitarBotonPrepararOrden = true;
                    }
                    else if(OOrden.iEstatus == 2) // En preparacion
                    {
                        BHabilitarAccionesEdicion = false;

                        // ANTES

                        //if (iRol != 2) // Solo administrador y cocina pueden señalar que una orden esta lista
                        //{
                        //    BHabilitarBotonOrdenPreparada = true; // Habilita el boton para indicar que la orden esta preparada
                        //}

                        // AHORA

                        BHabilitarBotonOrdenPreparada = true; // Habilita el boton para indicar que la orden esta preparada

                    }
                }
            }
        }


        private void SuscribirToggle(OrdenProducto prod)
        {
            prod.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(OrdenProducto.IsExpanded) && prod.IsExpanded)
                {
                    foreach (var other in LstOrdenProducto.Where(x => x != prod))
                        other.IsExpanded = false;
                }
            };
        }

        #endregion

        #region EliminarProductoOrden

        [RelayCommand]
        public async Task EliminarProductoOrden(string sIDMongoDB)
        {
            bool confirmar = false;
            try
            {
                var mainPage = Application.Current?.Windows[0].Page;
                if (mainPage != null)
                {
                    confirmar = await mainPage.DisplayAlert(
                        "Confirmar Eliminación",
                        "¿Estás seguro de que deseas eliminar este producto?",
                        "Sí",
                        "No");
                }


                if (confirmar)
                {
                    await EliminarProductoOrdenApi(sIDMongoDB);
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error al eliminar el producto: {ex.Message} {ex.StackTrace}");
            }
        }

        private async Task EliminarProductoOrdenApi(string sIDMongoDB)
        {
            string sMensajeErrorProceso = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(sIDMongoDB)) return;

                await _oIPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
                {
                    try
                    {
                        var oResp = await _oHttpApiService.DeleteAsync($"api/orden-productos/{sIDMongoDB}", true);
                        if (oResp != null)
                        {
                            var oRespJson = await oResp.Content.ReadFromJsonAsync<ApiRespuesta<OrdenProducto>>();
                            if (oResp.IsSuccessStatusCode && oRespJson != null && oRespJson.bSuccess)
                            {
                                await vm.Cerrar();
                                var mainPage = Application.Current?.Windows[0].Page;
                                if (mainPage != null)
                                {
                                    await mainPage.DisplayAlert("Eliminado", "Producto eliminado correctamente", "OK");
                                    await LoadDataApi();
                                }
                            }
                            else
                            {
                                sMensajeErrorProceso = oRespJson?.Error?.sDetails ?? "Error desconocido al eliminar.";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"{ex.Message} {ex.StackTrace}");
                    }
                    finally
                    {
                        await vm.Cerrar();
                    }
                });
            }
            catch (Exception ex)
            {
                sMensajeErrorProceso = $"{ex.Message} {ex.StackTrace}";
            }

            if (!string.IsNullOrWhiteSpace(sMensajeErrorProceso))
            {
                MostrarError(sMensajeErrorProceso);
            }
        }

        #endregion

        #region DuplicarProductoOrden

        [RelayCommand]
        private async Task DuplicarProductoOrden(OrdenProducto oOrdenProducto)
        {
            var mainPage = Application.Current?.Windows[0].Page;
            if (mainPage == null)
                return;

            // 1) Confirmar acción
            bool confirmar = await mainPage.DisplayAlert(
                "Agregar más unidades",
                "¿Confirmas que deseas agregar más unidades de este producto?",
                "Sí", "No");

            if (!confirmar)
                return;

            // 2) Pedir cantidad
            string? result = await mainPage.DisplayPromptAsync(
                "Agregar más unidades",
                "¿Cuántas unidades deseas agregar? (1–10)",
                initialValue: "1",
                maxLength: 2,
                keyboard: Keyboard.Numeric);

            // Si el usuario canceló o no ingresó nada, salimos
            if (string.IsNullOrWhiteSpace(result))
                return;

            // 3) Validar entrada numérica
            if (!int.TryParse(result, out int cantidad))
            {
                await mainPage.DisplayAlert("Valor inválido", "Debes ingresar un número válido.", "OK");
                return;
            }

            // 4) Validar rango
            if (cantidad < 1 || cantidad > 10)
            {
                await mainPage.DisplayAlert("Valor fuera de rango", "El número debe estar entre 1 y 10.", "OK");
                return;
            }

            // 5) Llamar al API para duplicar
            await DuplicarProductoAPI(oOrdenProducto, cantidad);
        }

        public async Task DuplicarProductoAPI(OrdenProducto oOrdenProducto, int cantidad)
        {
            await _oIPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
            {
                try
                {
                    var mainPage = Application.Current?.Windows[0].Page;
                    int exitos = 0;

                    // Crear y enviar tantas unidades como pida el usuario
                    for (int i = 0; i < cantidad; i++)
                    {
                        var nuevo = new OrdenProducto
                        {
                            sIdOrdenMongoDB = oOrdenProducto.sIdOrdenMongoDB,
                            sNombre = oOrdenProducto.sNombre,
                            iCostoReal = oOrdenProducto.iCostoReal,
                            iCostoPublico = oOrdenProducto.iCostoPublico,
                            sURLImagen = oOrdenProducto.sURLImagen,
                            //sIndicaciones = oOrdenProducto.sIndicaciones,
                            sIndicaciones = "",
                            iIndexVarianteSeleccionada = oOrdenProducto.iIndexVarianteSeleccionada,
                            aVariantes = oOrdenProducto.aVariantes,
                            iTipoProducto = oOrdenProducto.iTipoProducto,
                            //aExtras = oOrdenProducto.aExtras
                            aExtras = []
                        };

                        var response = await _oHttpApiService.PostAsync(
                            "api/orden-productos/", nuevo, bRequiereToken: true);

                        if (response != null && response.IsSuccessStatusCode)
                            exitos++;
                        else
                            break;
                    }

                    if (mainPage != null)
                    {
                        if (exitos == cantidad)
                            await mainPage.DisplayAlert("Correcto", $"{cantidad} unidad(es) agregada(s) con éxito.", "OK");
                        else
                            await mainPage.DisplayAlert("Parcialmente exitoso",
                                $"Se agregaron {exitos} de {cantidad} unidad(es).", "OK");

                        await LoadDataApi();
                    }
                }
                catch (Exception ex)
                {
                    MostrarError($"Ocurrió un error inesperado: {ex.Message}");
                }
                finally
                {
                    await vm.Cerrar();
                }
            });
        }

        #endregion

        #region IncrementarCantidad (Pantalla 1)

        [RelayCommand]
        public async Task IncrementarCantidad(OrdenProducto oProducto)
        {
            if (oProducto == null) return;

            try
            {
                var response = await _oHttpApiService.PatchAsync(
                    $"api/orden-productos/{oProducto.sIdMongo}/cantidad",
                    new { iCantidad = oProducto.iCantidad + 1 });

                if (response != null && response.IsSuccessStatusCode)
                {
                    await LoadDataApi();
                }
                else
                {
                    MostrarError("Error al incrementar la cantidad.");
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error: {ex.Message}");
            }
        }

        #endregion

        #region DecrementarCantidad (Pantalla 1 - Escenario 1 y 3)

        [RelayCommand]
        public async Task DecrementarCantidad(OrdenProducto oProducto)
        {
            if (oProducto == null) return;

            try
            {
                // Si la cantidad es 1, preguntar si eliminar
                if (oProducto.iCantidad <= 1)
                {
                    bool confirmar = await Shell.Current.DisplayAlert(
                        "Eliminar producto",
                        "¿Deseas eliminar este producto de la orden?",
                        "Sí", "No");

                    if (confirmar)
                    {
                        await EliminarProductoOrdenApi(oProducto.sIdMongo);
                    }
                    return;
                }

                // Verificar si tiene extras (Escenario 3)
                var responseCheck = await _oHttpApiService.GetAsync(
                    $"api/orden-productos/{oProducto.sIdMongo}/tiene-extras");

                if (responseCheck != null && responseCheck.IsSuccessStatusCode)
                {
                    var checkResult = await responseCheck.Content.ReadFromJsonAsync<ApiRespuesta<TieneExtrasResponse>>();
                    
                    if (checkResult?.lData?.FirstOrDefault()?.bTieneExtras == true)
                    {
                        // Redirigir a pantalla de consumos (Escenario 3)
                        await Shell.Current.DisplayAlert(
                            "Administrar consumos",
                            "Este producto tiene extras. Debes eliminar los consumos específicos.",
                            "OK");

                        await IrAdministrarConsumos(oProducto);
                        return;
                    }
                }

                // Decrementar normalmente
                var response = await _oHttpApiService.PatchAsync(
                    $"api/orden-productos/{oProducto.sIdMongo}/cantidad",
                    new { iCantidad = oProducto.iCantidad - 1 });

                if (response != null && response.IsSuccessStatusCode)
                {
                    var apiResp = await response.Content.ReadFromJsonAsync<ApiRespuesta<CantidadResponse>>();
                    
                    // Verificar si requiere administrar consumos
                    if (apiResp?.lData?.FirstOrDefault()?.requiereAdminConsumos == true)
                    {
                        await IrAdministrarConsumos(oProducto);
                    }
                    else
                    {
                        await LoadDataApi();
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error: {ex.Message}");
            }
        }

        #endregion

        #region IrAdministrarConsumos (Pantalla 2)

        [RelayCommand]
        public async Task IrAdministrarConsumos(OrdenProducto oProducto)
        {
            if (oProducto == null) return;

            await Shell.Current.GoToAsync("consumosProducto", new Dictionary<string, object>
            {
                { "sIdOrdenProducto", oProducto.sIdMongo },
                { "sIdOrden", sIdOrdenMongoDB },
                { "sNombreProducto", oProducto.sNombre },
                { "iCantidad", oProducto.iCantidad > 0 ? oProducto.iCantidad : 1 }
            });
        }

        #endregion

        #region MostrarIndicaciones (Pantalla 6)

        [RelayCommand]
        public async Task MostrarIndicaciones()
        {
            await _oIPopupService.ShowPopupAsync<IndicacionesOrdenPopupViewModel>(vm =>
            {
                vm.SIndicaciones = OOrden?.sIndicaciones ?? string.Empty;
                vm.OnGuardar = async (indicaciones) =>
                {
                    await GuardarIndicaciones(indicaciones);
                };
            });
        }

        private async Task GuardarIndicaciones(string indicaciones)
        {
            try
            {
                var response = await _oHttpApiService.PatchAsync(
                    $"api/orden/{sIdOrdenMongoDB}/indicaciones",
                    new { sIndicaciones = indicaciones });

                if (response != null && response.IsSuccessStatusCode)
                {
                    if (OOrden != null)
                    {
                        OOrden.sIndicaciones = indicaciones;
                        OnPropertyChanged(nameof(OOrden));
                    }
                }
                else
                {
                    MostrarError("Error al guardar las indicaciones.");
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error: {ex.Message}");
            }
        }

        #endregion


        #region IrAgregarProductoOrden

        [RelayCommand]
        public async Task IrAgregarProductoOrden()
        {
            try
            {
                ValidacionOrden oValidacion = new ValidacionOrden(_oHttpApiService);

                (int iEstatusActual, string sMensaje) = await oValidacion.ObtenerEstatusActual(sIdOrdenMongoDB);

                if (iEstatusActual != -404 && iEstatusActual != -500)
                {
                    if (iEstatusActual == 1 || iEstatusActual == 0) // orden en estatus pendiente o confirmada
                    {
                        await Shell.Current.GoToAsync("datosProductoOrden", new Dictionary<string, object>
                                        {
                                            { "sIdOrden",  sIdOrdenMongoDB}
                                        });
                    }
                    else
                    {
                        MostrarError("La orden ya se encuentra en un estatus distinto a \"Confirmada\". Si desea agregar más productos, por favor, cree una Sub Orden.");
                    }

                }
                else
                {
                    MostrarError(sMensaje);
                }

            }
            catch (Exception ex)
            {
                MostrarError($"ERROR al navegar a la vista para agregar un nuevo producto: {ex.Message} {ex.StackTrace}");
            }
        }

        #endregion

        #region IrEditarProductoOrden

        [RelayCommand]
        public async Task IrEditarProductoOrden(OrdenProducto oOrdenProducto)
        {
            await Shell.Current.GoToAsync("datosProductoOrden", new Dictionary<string, object>
                                        {
                                            { "sIdOrden",  sIdOrdenMongoDB},
                                            {"ordenProducto", oOrdenProducto }
                                        });
        }

        #endregion

        #region TomarOrden

        [RelayCommand]
        public async Task TomarOrden(string idOrden)
        {
            try
            {
                var validacionOrden = new ValidacionOrden(_oHttpApiService);

                (bool bCodigoRespuesta, string sMensaje) = await validacionOrden.ActualizarEstatusOrden(idOrden, 1);

                // Se verifica si la respuesta fue exitosa.
                if (bCodigoRespuesta)
                {
                    await _oSocketIoService.SendMessageAsync("mensaje", "NuevaOrden");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    MostrarError(sMensaje);
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error al eliminar el producto: {ex.Message} {ex.StackTrace}");
            }
        }

        #endregion

        #region ActualizarAEnpreparacion

        [RelayCommand]
        public async Task ActualizarAEnpreparacion(string sIdMongoDB)
        {
            bool bRespuesta = (bool)await Shell.Current.DisplayAlert("Confirmar actividad", $"¿Estás seguro de iniciar esta orden?", "Si", "No");

            if (bRespuesta)
            {
                var validacionOrden = new ValidacionOrden(_oHttpApiService);

                (bool bCodigoRespuesta, string sMensaje) = await validacionOrden.ActualizarEstatusOrden(sIdMongoDB, 2);

                if (bCodigoRespuesta)
                {
                    BHabilitarBotonPrepararOrden = false;
                    await LoadDataApi();
                }
                else
                {
                    MostrarError(sMensaje);
                }
            }
        }

        #endregion

        #region ActualizarAPreparada

        [RelayCommand]
        public async Task ActualizarAPreparada(string sIdMongoDB)
        {
            bool bRespuesta = (bool)await Shell.Current.DisplayAlert("Confirmar actividad", $"¿Estás seguro de marcar la orden como Preparada?", "Si", "No");

            if (bRespuesta)
            {
                var validacionOrden = new ValidacionOrden(_oHttpApiService);

                (bool bCodigoRespuesta, string sMensaje) = await validacionOrden.ActualizarEstatusOrden(sIdMongoDB, 3);

                if (bCodigoRespuesta)
                {
                    BHabilitarBotonOrdenPreparada = false;
                    await _oSocketIoService.SendMessageAsync("mensaje", "OrdenLista");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    MostrarError(sMensaje);
                }
            }
        }

        #endregion

        #region MostrarError

        private async void MostrarError(string sMensaje)
        {
            await Shell.Current.DisplayAlert("Error", sMensaje, "OK");
        }

        #endregion
    }

    #region CLASES AUXILIARES PARA RESPUESTAS API

    /// <summary>
    /// Respuesta para verificar si un producto tiene extras
    /// </summary>
    public class TieneExtrasResponse
    {
        public bool bTieneExtras { get; set; }
    }

    /// <summary>
    /// Respuesta para actualización de cantidad
    /// </summary>
    public class CantidadResponse
    {
        public int iCantidad { get; set; }
        public bool requiereAdminConsumos { get; set; }
    }

    #endregion
}

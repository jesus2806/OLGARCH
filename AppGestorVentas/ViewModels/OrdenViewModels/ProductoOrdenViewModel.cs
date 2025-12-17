using AppGestorVentas.Helpers;
using AppGestorVentas.Models;
using AppGestorVentas.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace AppGestorVentas.ViewModels.OrdenViewModels
{
    public partial class ProductoOrdenViewModel : ObservableObject, IQueryAttributable
    {
        private readonly HttpApiService _httpApiService;

        private string sIdOrdenMongoDB = string.Empty;

        // Para saber si se trata de una actualización (edición)
        [ObservableProperty]
        private bool esEdicion = false;

        // Al editar, almacenamos el _id del producto de orden (documento en Mongo)
        [ObservableProperty]
        private string sIdOrdenProducto = string.Empty;

        // 1. Buscador principal (platillos/bebidas)
        [ObservableProperty]
        private string sBusquedaPrincipal = string.Empty;

        // Resultados de la búsqueda principal
        [ObservableProperty]
        private ObservableCollection<Producto> lstResultadosProductos;

        // 2. Producto seleccionado (solo 1)
        [ObservableProperty]
        private Producto oProductoSeleccionado;

        // La variante seleccionada (se asume clase Variante con sVariante)
        [ObservableProperty]
        private Variante varianteSeleccionada;

        // 3. Buscador de extras
        [ObservableProperty]
        private string sBusquedaExtras = string.Empty;

        // Resultados de búsqueda de extras
        [ObservableProperty]
        private ObservableCollection<Producto> lstResultadosExtras;

        // 4. Extras seleccionados
        [ObservableProperty]
        private ObservableCollection<Producto> lstExtrasSeleccionados;

        //// 5. Indicaciones
        [ObservableProperty]
        private string sIndicaciones = string.Empty;

        [ObservableProperty]
        private bool bNoHayUnoProductoSeleccionado;

        // Propiedades para el título de la página y el texto del botón de confirmación
        public string PageTitle => EsEdicion ? "Actualizar Producto" : "Agregar Producto";
        public string ConfirmButtonText => EsEdicion ? "Actualizar Producto" : "Agregar al Pedido";

        public ProductoOrdenViewModel(HttpApiService apiService)
        {
            _httpApiService = apiService;
            LstResultadosProductos = new ObservableCollection<Producto>();
            LstResultadosExtras = new ObservableCollection<Producto>();
            LstExtrasSeleccionados = new ObservableCollection<Producto>();
            OProductoSeleccionado = null;
            BNoHayUnoProductoSeleccionado = true;
        }

        #region Manejo de Navegación

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            // Parámetro obligatorio: sIdOrden (el _id de la orden en Mongo)
            if (query.TryGetValue("sIdOrden", out var sIdOrden) && sIdOrden != null)
            {
                sIdOrdenMongoDB = sIdOrden.ToString() ?? string.Empty;
            }

            // Si se pasa un parámetro "ordenProducto" significa que se quiere editar
            if (query.TryGetValue("ordenProducto", out var ordenProductoParam) && ordenProductoParam != null)
            {
                try
                {
                    OrdenProducto ordenProducto = null;
                    // Si viene como cadena JSON se deserializa; de lo contrario se intenta un cast directo.
                    if (ordenProductoParam is string jsonString)
                    {
                        ordenProducto = JsonSerializer.Deserialize<OrdenProducto>(jsonString);
                    }
                    else if (ordenProductoParam is OrdenProducto op)
                    {
                        ordenProducto = op;
                    }

                    if (ordenProducto == null)
                    {
                        MostrarError("No se pudo interpretar el parámetro de edición.");
                        return;
                    }

                    // Activamos el modo edición
                    EsEdicion = true;
                    SIdOrdenProducto = ordenProducto.sIdMongo;

                    // Convertir el OrdenProducto a un Producto "dummy" para reutilizar la vista.
                    OProductoSeleccionado = new Producto
                    {
                        sIdMongo = ordenProducto.sIdMongo,
                        sNombre = ordenProducto.sNombre,
                        iCostoReal = ordenProducto.iCostoReal,
                        iCostoPublico = ordenProducto.iCostoPublico,
                        iTipoProducto = ordenProducto.iTipoProducto,
                        // Se asume que se usa la imagen principal
                        aImagenes = new List<Imagen>
                        {
                            new Imagen { sURLImagen = ordenProducto.sURLImagen }
                        },
                        // Se asigna la lista completa de variantes
                        aVariantes = ordenProducto.aVariantes
                    };

                    SIndicaciones = ordenProducto.sIndicaciones;

                    // Seleccionar la variante actual según el índice almacenado
                    if (OProductoSeleccionado.aVariantes != null &&
                        OProductoSeleccionado.aVariantes.Count > ordenProducto.iIndexVarianteSeleccionada)
                    {
                        VarianteSeleccionada = OProductoSeleccionado.aVariantes[ordenProducto.iIndexVarianteSeleccionada];
                    }
                    else
                    {
                        MostrarError("La variante seleccionada no se encuentra en la lista.");
                    }

                    // Cargar los extras ya asignados convirtiéndolos a Producto
                    LstExtrasSeleccionados.Clear();
                    if (ordenProducto.aExtras != null)
                    {
                        foreach (var extra in ordenProducto.aExtras)
                        {
                            LstExtrasSeleccionados.Add(new Producto
                            {
                                sIdMongo = extra.sIdExtra,
                                sNombre = extra.sNombre,
                                iCostoReal = extra.iCostoReal,
                                iCostoPublico = extra.iCostoPublico,
                                aImagenes = new List<Imagen>
                                {
                                    new Imagen { sURLImagen = extra.sURLImagen }
                                }
                            });
                        }
                    }

                    // Al editar ya existe un producto seleccionado, se deshabilita la búsqueda
                    BNoHayUnoProductoSeleccionado = false;
                    LstResultadosProductos.Clear();

                    // Notificar cambios en propiedades dependientes
                    OnPropertyChanged(nameof(PageTitle));
                    OnPropertyChanged(nameof(ConfirmButtonText));
                    OnPropertyChanged(nameof(VarianteSeleccionada));
                }
                catch (Exception ex)
                {
                    MostrarError($"Error al procesar los parámetros de edición: {ex.Message}");
                }
            }
        }

        #endregion

        #region BÚSQUEDA PRINCIPAL (POST)
        [RelayCommand]
        public async Task OnBusquedaProductosTextChanged(string texto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(texto) || texto.Length < 3)
                {
                    LstResultadosProductos.Clear();
                    return;
                }

                var payload = new
                {
                    texto = texto.Trim(),
                    // Filtrar iTipoProducto en [1,2] (platillos/bebidas)
                    tipoEn = new int[] { 1, 2 }
                };

                var response = await _httpApiService.PostAsync("api/productos/search", payload, bRequiereToken: true);
                if (response == null)
                {
                    MostrarError("No se recibió respuesta del servidor al buscar productos.");
                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    MostrarError($"Error en la búsqueda de productos: {response.ReasonPhrase}");
                    return;
                }

                var respData = await response.Content.ReadFromJsonAsync<ApiRespuesta<Producto>>();
                if (respData != null && respData.bSuccess && respData.lData != null)
                {
                    LstResultadosProductos.Clear();
                    foreach (var prod in respData.lData)
                    {
                        // Si este producto ya está seleccionado se marca
                        prod.isSeleccionado = (OProductoSeleccionado != null &&
                                                 OProductoSeleccionado.sIdMongo == prod.sIdMongo);
                        LstResultadosProductos.Add(prod);
                    }
                }
                else
                {
                    MostrarError("Error al deserializar la respuesta de productos o la respuesta no fue exitosa.");
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Ocurrió un error inesperado al buscar productos: {ex.Message}");
            }
        }
        #endregion

        #region SELECCIONAR / QUITAR PRODUCTO
        [RelayCommand]
        public async Task OnSeleccionarProducto(Producto prod)
        {
            try
            {
                // Si ya estaba seleccionado => lo quitamos
                if (OProductoSeleccionado != null && OProductoSeleccionado.sIdMongo == prod.sIdMongo)
                {
                    OProductoSeleccionado = null;
                    VarianteSeleccionada = null;
                    prod.isSeleccionado = false;
                    LstExtrasSeleccionados.Clear();
                    SBusquedaExtras = string.Empty;
                    BNoHayUnoProductoSeleccionado = true;
                }
                else
                {
                    if (OProductoSeleccionado != null)
                    {
                        var old = LstResultadosProductos.FirstOrDefault(x => x.sIdMongo == OProductoSeleccionado.sIdMongo);
                        if (old != null)
                            old.isSeleccionado = false;
                    }

                    OProductoSeleccionado = prod;
                    BNoHayUnoProductoSeleccionado = false;
                    SBusquedaPrincipal = string.Empty;
                    // Se asume que se selecciona la primera variante disponible
                    VarianteSeleccionada = prod.aVariantes.FirstOrDefault();
                    prod.isSeleccionado = true;
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Ocurrió un error inesperado al seleccionar/quitar producto: {ex.Message}");
            }
        }
        #endregion

        #region BÚSQUEDA DE EXTRAS (POST)
        [RelayCommand]
        public async Task OnBusquedaExtrasTextChanged(string texto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(texto) || texto.Length < 3)
                {
                    LstResultadosExtras.Clear();
                    return;
                }

                var payload = new
                {
                    texto = texto,
                    tipo = 3
                };

                var resp = await _httpApiService.PostAsync("api/productos/search", payload, bRequiereToken: true);
                if (resp == null)
                {
                    MostrarError("No se recibió respuesta del servidor al buscar extras.");
                    return;
                }
                if (!resp.IsSuccessStatusCode)
                {
                    MostrarError($"Error en la búsqueda de extras: {resp.ReasonPhrase}");
                    return;
                }

                var data = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Producto>>();
                if (data != null && data.bSuccess && data.lData != null)
                {
                    LstResultadosExtras.Clear();
                    foreach (var extra in data.lData)
                    {
                        bool existe = LstExtrasSeleccionados.Any(x => x.sIdMongo == extra.sIdMongo);
                        if (!existe)
                        {
                            LstResultadosExtras.Add(extra);
                        }
                    }
                }
                else
                {
                    MostrarError("Error al deserializar la respuesta de extras o la respuesta no fue exitosa.");
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Ocurrió un error inesperado al buscar extras: {ex.Message}");
            }
        }
        #endregion

        #region SELECCIONAR / ELIMINAR EXTRAS
        [RelayCommand]
        private void OnSeleccionarExtra(Producto extra)
        {
            try
            {
                if (!LstExtrasSeleccionados.Any(x => x.sIdMongo == extra.sIdMongo))
                {
                    LstExtrasSeleccionados.Add(extra);
                    SBusquedaExtras = string.Empty;
                    LstResultadosExtras.Remove(extra);
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Ocurrió un error al seleccionar el extra: {ex.Message}");
            }
        }

        [RelayCommand]
        private void OnEliminarExtra(Producto extra)
        {
            try
            {
                if (LstExtrasSeleccionados.Contains(extra))
                {
                    LstExtrasSeleccionados.Remove(extra);
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Ocurrió un error al eliminar el extra: {ex.Message}");
            }
        }

        [RelayCommand]
        public void QuitarProducto()
        {
            try
            {
                if (OProductoSeleccionado != null)
                {
                    var producto = LstResultadosProductos.FirstOrDefault(p => p.sIdMongo == OProductoSeleccionado.sIdMongo);
                    if (producto != null)
                    {
                        producto.isSeleccionado = false;
                    }
                    OProductoSeleccionado = null;
                    VarianteSeleccionada = null;
                    SIndicaciones = string.Empty;
                    LstExtrasSeleccionados.Clear();
                    BNoHayUnoProductoSeleccionado = true;
                    SBusquedaExtras = string.Empty;
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Ocurrió un error al quitar el producto: {ex.Message}");
            }
        }
        #endregion

        #region CONFIRMAR (CREAR O ACTUALIZAR)
        [RelayCommand]
        private async Task OnConfirmar()
        {
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }

                ValidacionOrden oValidacion = new ValidacionOrden(_httpApiService);

                (int iEstatusActual, string sMensaje) = await oValidacion.ObtenerEstatusActual(sIdOrdenMongoDB);

                if (iEstatusActual != -404 && iEstatusActual != -500)
                {
                    if (iEstatusActual == 1 || iEstatusActual == 0) // orden en estatus pendiente o confirmada
                    {
                        var mainPage = Application.Current?.Windows[0].Page;

                        if (OProductoSeleccionado == null)
                        {
                            if (mainPage != null)
                                await mainPage.DisplayAlert("Validación", "Debes seleccionar un platillo o bebida.", "OK");
                            return;
                        }
                        if (VarianteSeleccionada == null)
                        {
                            if (mainPage != null)
                                await mainPage.DisplayAlert("Validación", "Debes elegir la variante del producto.", "OK");
                            return;
                        }

                        if (!string.IsNullOrWhiteSpace(SIndicaciones) && !EntryValidations.IsValidText(SIndicaciones))
                        {
                            if (mainPage != null)
                                await mainPage.DisplayAlert("Validación", "El campo 'Indicaciones' no admite caracteres especiales.", "OK");
                            return;
                        }
                        else if (EntryValidations.IsOnlyNumbers(SIndicaciones))
                        {
                            if (mainPage != null)
                                await mainPage.DisplayAlert("Validación", "El campo 'Indicaciones' no debe ser solo números.", "OK");
                            return;
                        }

                        // Construir la lista de extras
                        var lstExtras = new List<ExtraOrdenProducto>();
                        string sURLImagenExtra = "";
                        if (LstExtrasSeleccionados.Count > 0)
                        {
                            foreach (Producto oProductoExtra in LstExtrasSeleccionados)
                            {
                                sURLImagenExtra = "";

                                if (oProductoExtra.aImagenes != null && oProductoExtra.aImagenes.Count > 0)
                                {
                                    sURLImagenExtra = oProductoExtra.aImagenes[0].sURLImagen;
                                }

                                lstExtras.Add(new ExtraOrdenProducto
                                {
                                    sNombre = oProductoExtra.sNombre,
                                    iCostoReal = oProductoExtra.iCostoReal,
                                    iCostoPublico = oProductoExtra.iCostoPublico,
                                    sURLImagen = sURLImagenExtra
                                });
                            }
                        }

                        string sURLImagenProducto = "";

                        if (OProductoSeleccionado.aImagenes != null && OProductoSeleccionado.aImagenes.Count > 0)
                        {
                            sURLImagenProducto = OProductoSeleccionado.aImagenes[0].sURLImagen;
                        }

                        // Construir el objeto que se enviará al endpoint
                        var oOrdenProductoData = new OrdenProducto
                        {
                            sIdOrdenMongoDB = sIdOrdenMongoDB,
                            sNombre = OProductoSeleccionado.sNombre,
                            iCostoReal = OProductoSeleccionado.iCostoReal,
                            iCostoPublico = OProductoSeleccionado.iCostoPublico,
                            sURLImagen = sURLImagenProducto,
                            sIndicaciones = SIndicaciones,
                            // Se asigna el índice de la variante seleccionada dentro del arreglo de variantes
                            iIndexVarianteSeleccionada = OProductoSeleccionado.aVariantes.IndexOf(VarianteSeleccionada),
                            // Se envía la lista completa de variantes (asegúrate que OProductoSeleccionado.aVariantes tenga el formato adecuado)
                            aVariantes = OProductoSeleccionado.aVariantes,
                            iTipoProducto = OProductoSeleccionado.iTipoProducto,
                            aExtras = lstExtras
                        };

                        HttpResponseMessage response = null;
                        if (EsEdicion)
                        {
                            // Modo edición: se actualiza el producto existente (PUT)
                            response = await _httpApiService.PutAsync($"api/orden-productos/{SIdOrdenProducto}", oOrdenProductoData, bRequiereToken: true);
                        }
                        else
                        {
                            // Modo creación: se agrega un nuevo producto a la orden (POST)
                            response = await _httpApiService.PostAsync("api/orden-productos/", oOrdenProductoData, bRequiereToken: true);
                        }

                        if (response == null)
                        {
                            MostrarError("No se recibió respuesta del servidor al procesar la operación.");
                            return;
                        }

                        if (!response.IsSuccessStatusCode)
                        {
                            MostrarError($"Error en la operación: {response.ReasonPhrase}");
                            return;
                        }

                        ApiRespuesta<OrdenProducto> apiRespuesta = null;
                        try
                        {
                            apiRespuesta = await response.Content.ReadFromJsonAsync<ApiRespuesta<OrdenProducto>>();
                        }
                        catch (Exception exJson)
                        {
                            MostrarError($"Error al deserializar la respuesta: {exJson.Message}");
                            return;
                        }

                        if (apiRespuesta != null && apiRespuesta.bSuccess)
                        {
                            if (mainPage != null)
                            {
                                var mensaje = EsEdicion ? "Producto actualizado con éxito." : "Producto agregado con éxito.";
                                await mainPage.DisplayAlert("OK", mensaje, "OK");
                                await Shell.Current.GoToAsync("..");
                            }
                        }
                        else
                        {
                            if (apiRespuesta != null && apiRespuesta.Error != null)
                            {
                                MostrarError(apiRespuesta.Error.sDetails);
                            }
                            else
                            {
                                MostrarError("Ocurrió un error inesperado al procesar la operación.");
                            }
                        }
                    }
                    else
                    {
                        MostrarError("La orden ya se encuentra en un estatus distinto a \"Confirmada\". Si desea agregar más productos, por favor, cree una Sub Orden.");
                        await Shell.Current.GoToAsync("adminOrdenes");
                    }

                }
                else
                {
                    MostrarError(sMensaje);
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Ocurrió un error inesperado al confirmar: {ex.Message}");
            }
        }
        #endregion

        private async void MostrarError(string sMensaje)
        {
            var mainPage = Application.Current?.Windows[0].Page;
            if (mainPage != null)
            {
                await mainPage.DisplayAlert("Error", sMensaje, "OK");
            }
        }
    }
}

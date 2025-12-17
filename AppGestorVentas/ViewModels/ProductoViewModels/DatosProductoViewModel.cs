using AppGestorVentas.Helpers;
using AppGestorVentas.Models;
using AppGestorVentas.Services;
using AppGestorVentas.ViewModels.Popup;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace AppGestorVentas.ViewModels.ProductoViewModels
{
    public partial class DatosProductoViewModel : ObservableObject, IQueryAttributable
    {
        private readonly HttpApiService _httpApiService;
        private readonly IPopupService _popupService;
        private readonly LocalDatabaseService _localDbService;

        [ObservableProperty]
        private string sTituloPagina = string.Empty;

        [ObservableProperty]
        public ObservableCollection<string> lstProductos = new();

        // MODELO principal. Si viene con sIdMongo, es EDICIÓN.
        [ObservableProperty]
        private Producto oProducto = new Producto();

        // Variantes en memoria 
        [ObservableProperty]
        private ObservableCollection<Variante> variantes = new();

        // Variante temporal
        [ObservableProperty]
        private string sVarianteNueva;

        [ObservableProperty]
        private string sProductoSeleccionado;

        // Imagen en bytes + nombre de archivo
        private byte[] _imagenBytes;
        private string _nombreArchivoImagen;

        // Vista previa
        [ObservableProperty]
        private ImageSource sImagenPreview;

        [ObservableProperty]
        private ImageSource sImagenPreviewEscritorio;

        // Mostrar errores
        [ObservableProperty]
        private bool bHayError;
        [ObservableProperty]
        private string sMensajeError;

        // Opciones para Picker
        public List<int> ListaTiposProducto { get; set; } = new() { 1, 2, 3 };

        // Indica si es modo edición (se setea en ApplyQueryAttributes)
        [ObservableProperty]
        public bool bEsEdicion;

        [ObservableProperty]
        public bool bHabilitaraAgregarVarientes;

        // ¿Hay imagen para "Quitar"?
        [ObservableProperty]
        private bool bHabilitarQuitarImagen;

        #region CONSTRUCTOR

        public DatosProductoViewModel(HttpApiService apiService,
                                      IPopupService popupService,
                                      LocalDatabaseService localDbService)
        {
            _httpApiService = apiService;
            _popupService = popupService;
            _localDbService = localDbService;

            // Si no se pasa nada, se asume modo creación
            if (OProducto == null)
                OProducto = new Producto { iTipoProducto = 1 };

            LstProductos = new ObservableCollection<string>
                        {
                            "Platillo",
                            "Bebida",
                            "Extra"
                        };

            SProductoSeleccionado = LstProductos[0];

            STituloPagina = "Crear Producto";

        }

        #endregion

        #region APLICACIÓN DE PARÁMETROS (Edición vs Creación)

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
                // Determinamos si es edición al checar sIdMongo
                BEsEdicion = !string.IsNullOrWhiteSpace(OProducto?.sIdMongo);

            if (query.TryGetValue("oProducto", out var oProd) && oProd != null)
            {
                OProducto = (Producto)oProd;
                STituloPagina = "Actualizar Producto";
                BEsEdicion = !string.IsNullOrWhiteSpace(OProducto.sIdMongo);
                SProductoSeleccionado = LstProductos[OProducto.iTipoProducto - 1];

                // Cargar variantes en la ObservableCollection
                if (OProducto.aVariantes != null)
                {
                    Variantes.Clear();
                    foreach (var v in OProducto.aVariantes)
                        Variantes.Add(v);
                }

                // Si hay imágenes, mostrar la primera como preview
                if (OProducto.aImagenes != null && OProducto.aImagenes.Count > 0)
                {
                    var primerUrl = OProducto.aImagenes[0].sURLImagen;
                    if (!string.IsNullOrWhiteSpace(primerUrl))
                    {
                        SImagenPreview = ImageSource.FromUri(new Uri(primerUrl));
                        SImagenPreviewEscritorio = ImageSource.FromUri(new Uri(primerUrl));
                        BHabilitarQuitarImagen = true;
                    }
                }

                OnPropertyChanged(nameof(OProducto));
                //BHabilitarQuitarImagen = _imagenBytes != null && _imagenBytes.Length > 0;
            }
            else
            {
                // Modo creación
                OProducto = new Producto { iTipoProducto = 1 };
                BEsEdicion = false;
            }
            OnPropertyChanged(nameof(BEsEdicion));
            OnPropertyChanged(nameof(BHabilitarQuitarImagen));
        }

        #endregion

        public void PreparaVistaSoloLectura()
        {

        }

        #region COMANDOS IMAGEN

        [RelayCommand]
        public async Task SeleccionarImagen()
        {
            try
            {
                var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.image" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.image" } },
                    { DevicePlatform.Android, new[] { "image/*" } },
                    { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" } }
                });

                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Selecciona imagen",
                    FileTypes = customFileType
                });
                if (result == null) return; // user canceled

                using var pickedStream = await result.OpenReadAsync();
                using var ms = new MemoryStream();
                await pickedStream.CopyToAsync(ms);
                _imagenBytes = ms.ToArray();
                _nombreArchivoImagen = result.FileName;

                SImagenPreview = ImageSource.FromStream(() => new MemoryStream(_imagenBytes));
                SImagenPreviewEscritorio = SImagenPreview;
                BHabilitarQuitarImagen = true;
            }
            catch (Exception ex)
            {
                MostrarError($"Error al seleccionar imagen: {ex.Message}");
            }
        }

        [RelayCommand]
        public void QuitarImagen()
        {
            _imagenBytes = null;
            _nombreArchivoImagen = null;
            SImagenPreview = null;
            SImagenPreviewEscritorio = null;
            BHabilitarQuitarImagen = false;
        }

        #endregion

        #region COMANDOS VARIANTES

        [RelayCommand]
        public async void AgregarVariante()
        {
            var mainPage = Application.Current?.Windows[0].Page;
            try
            {
                if (string.IsNullOrWhiteSpace(SVarianteNueva))
                    return;

                if (!EntryValidations.IsValidText(SVarianteNueva))
                {
                    if (mainPage != null)
                        await mainPage.DisplayAlert("Validación", "El campo 'Variante' no admite caracteres especiales.", "OK");
                    return;
                }
                else if (EntryValidations.IsOnlyNumbers(SVarianteNueva))
                {
                    if (mainPage != null)
                        await mainPage.DisplayAlert("Validación", "El campo 'Variante' no debe ser solo números.", "OK");
                    return;
                }

                Variantes.Add(new Variante { sVariante = SVarianteNueva.Trim() });
                SVarianteNueva = string.Empty;
            }
            catch (Exception ex)
            {
                MostrarError($"Error al tratar de agregar la variante: {ex.Message}");
            }
        }

        [RelayCommand]
        public void EliminarVariante(Variante varItem)
        {
            if (Variantes.Contains(varItem))
                Variantes.Remove(varItem);
        }

        #endregion

        #region COMANDOS SEPARADOS: CREAR y ACTUALIZAR

        [RelayCommand]
        public async Task CrearProducto()
        {

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                return;
            }


            BHayError = false;
            SMensajeError = string.Empty;

            // 1) Validaciones
            if (!ValidarCamposBasicos()) return;

            await _popupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
            {
                try
                {
                    // 2) Subir imagen (si existe)
                    string urlFinal = await SubirImagenAsync();
                    if (urlFinal == null && _imagenBytes != null && _imagenBytes.Length > 0)
                    {
                        // Falla en subir, abortamos
                        return;
                    }

                    // 3) Armar body con la nueva imagen
                    var imagenesList = new List<object>();
                    if (!string.IsNullOrWhiteSpace(urlFinal))
                    {
                        imagenesList.Add(new { sURLImagen = urlFinal });
                    }

                    var listVars = Variantes.Select(v => new { sVariante = v.sVariante }).ToList();

                    int iTipoProducto;
                    switch (SProductoSeleccionado)
                    {
                        case "Platillo":
                            iTipoProducto = 1;
                            break;
                        case "Bebida":
                            iTipoProducto = 2;
                            break;
                        case "Extra":
                            iTipoProducto = 3;
                            break;
                        default:
                            iTipoProducto = 0;
                            break;
                    }

                    var body = new
                    {
                        sNombre = OProducto.sNombre,
                        iCostoReal = OProducto.iCostoReal,
                        iCostoPublico = OProducto.iCostoPublico,
                        imagenes = imagenesList,
                        aVariantes = listVars,
                        iTipoProducto = iTipoProducto
                    };

                    // 4) POST
                    var resp = await _httpApiService.PostAsync("api/productos", body, true);
                    if (resp != null && resp.IsSuccessStatusCode)
                    {
                        var respJson = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Producto>>();
                        if (respJson != null && respJson.bSuccess)
                        {
                            await vm.Cerrar();
                            var mainPage = Application.Current?.Windows[0].Page;
                            if (mainPage != null)
                            {
                                await mainPage.DisplayAlert("Éxito", "Producto creado correctamente", "OK");
                            }
                            ResetFormulario();
                        }
                        else
                        {
                            MostrarError(respJson?.Error?.sDetails ?? "Error desconocido al crear producto.");
                        }
                    }
                    else
                    {
                        MostrarError("No se pudo conectar para crear producto.");
                    }
                }
                catch (Exception ex)
                {
                    MostrarError($"Excepción: {ex.Message}");
                }
                finally
                {
                    await vm.Cerrar();
                }
            });
        }


        public void CambioTipoProducto()
        {
            if (SProductoSeleccionado.Equals("Extra"))
            {
                BHabilitaraAgregarVarientes = false;
                Variantes.Clear();
            }
            else
            {
                BHabilitaraAgregarVarientes = true;
            }
        }



        [RelayCommand]
        public async Task ActualizarProducto()
        {

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                return;
            }

            BHayError = false;
            SMensajeError = string.Empty;

            // 1) Validaciones
            if (!ValidarCamposBasicos()) return;

            if (string.IsNullOrWhiteSpace(OProducto.sIdMongo))
            {
                MostrarError("No se puede actualizar sin sIdMongo.");
                return;
            }

            await _popupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
            {
                try
                {
                    // 2) Subir imagen
                    string urlFinal = await SubirImagenAsync();
                    if (urlFinal == null && _imagenBytes != null && _imagenBytes.Length > 0)
                    {
                        // Falla
                        return;
                    }

                    // 3) Si no hay imagen nueva, conservamos la(s) antigua(s)
                    List<object> imagenesList = new();
                    if (!string.IsNullOrEmpty(urlFinal))
                    {
                        imagenesList.Add(new { sURLImagen = urlFinal });
                    }
                    else if (OProducto.aImagenes != null && OProducto.aImagenes.Count > 0)
                    {
                        // Mantener la existente
                        imagenesList.AddRange(OProducto.aImagenes.Select(i => new { i.sURLImagen }));
                    }

                    var listVars = Variantes.Select(v => new { sVariante = v.sVariante }).ToList();


                    int iTipoProducto;
                    switch (SProductoSeleccionado)
                    {
                        case "Platillo":
                            iTipoProducto = 1;
                            break;
                        case "Bebida":
                            iTipoProducto = 2;
                            break;
                        case "Extra":
                            iTipoProducto = 3;
                            break;
                        default:
                            iTipoProducto = 0;
                            break;
                    }


                    var body = new
                    {
                        sNombre = OProducto.sNombre,
                        iCostoReal = OProducto.iCostoReal,
                        iCostoPublico = OProducto.iCostoPublico,
                        imagenes = imagenesList,
                        aVariantes = listVars,
                        iTipoProducto = iTipoProducto
                    };

                    // 4) PUT /api/productos/{id}
                    var resp = await _httpApiService.PutAsync($"api/productos/{OProducto.sIdMongo}", body, true);
                    if (resp != null && resp.IsSuccessStatusCode)
                    {
                        var respJson = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Producto>>();
                        if (respJson != null && respJson.bSuccess)
                        {
                            await vm.Cerrar();
                            var mainPage = Application.Current?.Windows[0].Page;
                            if (mainPage != null)
                            {
                                await mainPage.DisplayAlert("Actualizado", "Producto actualizado correctamente", "OK");
                            }
                            //await Shell.Current.GoToAsync("..");
                        }
                        else
                        {
                            MostrarError(respJson?.Error?.sDetails ?? "Error desconocido al actualizar.");
                        }
                    }
                    else
                    {
                        MostrarError("No se pudo conectar para actualizar producto.");
                    }
                }
                catch (Exception ex)
                {
                    MostrarError($"Excepción: {ex.Message}");
                }
                finally
                {
                    await vm.Cerrar();
                }
            });
        }

        #endregion

        #region MÉTODOS AUXILIARES

        /// <summary>
        /// Retorna false si un campo básico es inválido.
        /// </summary>
        private bool ValidarCamposBasicos()
        {
            if (string.IsNullOrWhiteSpace(OProducto.sNombre))
            {
                MostrarError("El nombre es requerido.");
                return false;
            }
            if (OProducto.iCostoReal <= 0)
            {
                MostrarError("Costo real inválido.");
                return false;
            }
            if (OProducto.iCostoPublico <= 0)
            {
                MostrarError("Costo público inválido.");
                return false;
            }
            if (Variantes.Count < 1 && !SProductoSeleccionado.Equals("Extra"))
            {
                MostrarError("Debes agregar al menos 1 variante.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Sube la imagen si existe. Retorna la URL en S3, o string vacío 
        /// si no subimos nada. Retorna null si hubo error.
        /// </summary>
        private async Task<string> SubirImagenAsync()
        {
            if (!string.IsNullOrWhiteSpace(_nombreArchivoImagen) && !EntryValidations.HasValidImageExtension(_nombreArchivoImagen))
            {
                MostrarError("Imagenes con extensiones permitidas: jpg, jpeg, png, gif, webp.");
                return null;
            }
            if (_imagenBytes != null && _imagenBytes.Length > 0 && !string.IsNullOrWhiteSpace(_nombreArchivoImagen))
            {
                using var msUpload = new MemoryStream(_imagenBytes);
                var respUpload = await _httpApiService.PostMultipartAsync(
                    route: "api/images/productos/upload",
                    fileStream: msUpload,
                    fileName: _nombreArchivoImagen,
                    fieldName: "image",
                    bRequiereToken: true
                );
                if (respUpload != null && respUpload.IsSuccessStatusCode)
                {
                    var respJson = await respUpload.Content.ReadFromJsonAsync<ApiRespuesta<RespImagen>>();
                    if (respJson != null && respJson.bSuccess && respJson.lData?.Count > 0)
                    {
                        return respJson.lData[0].sRutaImagenS3;
                    }
                    else
                    {
                        MostrarError("No se pudo procesar la respuesta de la imagen.");
                        return null;
                    }
                }
                else
                {
                    MostrarError("Error al subir imagen a S3.");
                    return null;
                }
            }
            // Si no hay imagen que subir, regresamos string vacío para indicar "nada subido"
            return string.Empty;
        }

        private void ResetFormulario()
        {
            OProducto.sNombre = string.Empty;
            OProducto.iCostoReal = 0;
            OProducto.iCostoPublico = 0;
            OProducto.iTipoProducto = 1;
            Variantes.Clear();
            OnPropertyChanged(nameof(OProducto));
            SVarianteNueva = string.Empty;
            _imagenBytes = null;
            _nombreArchivoImagen = null;
            SImagenPreview = null;
            SImagenPreviewEscritorio = null;
            BHabilitarQuitarImagen = false;
        }

        private async void MostrarError(string sMensaje)
        {
            //BHayError = true;
            //SMensajeError = sMensaje;
            var mainPage = Application.Current?.Windows[0].Page;
            if (mainPage != null)
            {
                await mainPage.DisplayAlert("Error", sMensaje, "OK");
            }
        }

        #endregion
    }

    // Clase para mapear data.sRutaImagenS3
    public class RespImagen
    {
        public string sRutaImagenS3 { get; set; }
    }
}

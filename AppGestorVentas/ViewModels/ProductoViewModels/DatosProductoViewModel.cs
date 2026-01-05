using AppGestorVentas.Helpers;
using AppGestorVentas.Models;
using AppGestorVentas.Services;
using AppGestorVentas.ViewModels.Popup;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;

namespace AppGestorVentas.ViewModels.ProductoViewModels
{
    public partial class DatosProductoViewModel : ObservableObject, IQueryAttributable
    {
        private readonly HttpApiService _httpApiService;
        private readonly IPopupService _popupService;
        private readonly LocalDatabaseService _localDbService;

        [ObservableProperty] private string sTituloPagina = string.Empty;

        [ObservableProperty] public ObservableCollection<string> lstProductos = new();

        [ObservableProperty] private Producto oProducto = new Producto();

        [ObservableProperty] private ObservableCollection<Variante> variantes = new();

        [ObservableProperty] private string sVarianteNueva;

        [ObservableProperty] private string sProductoSeleccionado;

        private byte[] _imagenBytes;
        private string _nombreArchivoImagen;

        [ObservableProperty] private ImageSource sImagenPreview;
        [ObservableProperty] private ImageSource sImagenPreviewEscritorio;

        [ObservableProperty] private bool bHayError;
        [ObservableProperty] private string sMensajeError;

        public List<int> ListaTiposProducto { get; set; } = new() { 1, 2, 3 };

        [ObservableProperty] public bool bEsEdicion;

        [ObservableProperty] public bool bHabilitaraAgregarVarientes;

        [ObservableProperty] private bool bHabilitarQuitarImagen;

        // ==============================
        // ✅ INGREDIENTES (NUEVO)
        // ==============================
        private List<Ingrediente> _cacheIngredientes; // cache local
        private bool _ingredientesInicializados;

        [ObservableProperty] private bool bLoadingIngredientes;
        [ObservableProperty] private string sBusquedaIngrediente = "";

        [ObservableProperty] private ObservableCollection<Ingrediente> lstResultadosIngredientes = new();
        [ObservableProperty] private ObservableCollection<IngredienteUsoItem> lstIngredientesSeleccionados = new();

        [ObservableProperty] private string sErrorIngredientes = "";

        public bool BRequiereIngredientes => !string.Equals(SProductoSeleccionado, "Extra", StringComparison.OrdinalIgnoreCase);

        // ==============================

        #region CONSTRUCTOR

        public DatosProductoViewModel(HttpApiService apiService,
                                      IPopupService popupService,
                                      LocalDatabaseService localDbService)
        {
            _httpApiService = apiService;
            _popupService = popupService;
            _localDbService = localDbService;

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

            // Reacciona cuando cambian seleccionados para validaciones visuales si quieres
            LstIngredientesSeleccionados.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(BRequiereIngredientes));
            };
        }

        #endregion

        #region APLICACIÓN DE PARÁMETROS

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            BEsEdicion = !string.IsNullOrWhiteSpace(OProducto?.sIdMongo);

            if (query.TryGetValue("oProducto", out var oProd) && oProd != null)
            {
                OProducto = (Producto)oProd;
                STituloPagina = "Actualizar Producto";
                BEsEdicion = !string.IsNullOrWhiteSpace(OProducto.sIdMongo);
                SProductoSeleccionado = LstProductos[Math.Max(0, OProducto.iTipoProducto - 1)];

                // Variantes
                if (OProducto.aVariantes != null)
                {
                    Variantes.Clear();
                    foreach (var v in OProducto.aVariantes)
                        Variantes.Add(v);
                }

                // Preview Imagen
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
            }
            else
            {
                OProducto = new Producto { iTipoProducto = 1 };
                BEsEdicion = false;
            }

            OnPropertyChanged(nameof(BEsEdicion));
            OnPropertyChanged(nameof(BHabilitarQuitarImagen));
            OnPropertyChanged(nameof(BRequiereIngredientes));
        }

        #endregion

        // ==============================
        // ✅ INGREDIENTES - CARGA / BUSQUEDA / SELECCIÓN
        // ==============================

        [RelayCommand]
        public async Task InitIngredientes()
        {
            try
            {
                if (_ingredientesInicializados) return;

                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                    return;

                BLoadingIngredientes = true;

                var resp = await _httpApiService.GetAsync("api/ingredientes", bRequiereToken: true);
                if (resp == null) return;

                var api = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Ingrediente>>();
                if (!(resp.IsSuccessStatusCode && api != null && api.bSuccess && api.lData != null))
                    return;

                _cacheIngredientes = api.lData.ToList();
                _ingredientesInicializados = true;

                // Si vienes en edición y ya tiene aIngredientes, hidrata para mostrar nombres + cantidades
                HidratarSeleccionadosDesdeProducto();
            }
            catch
            {
                // silencioso
            }
            finally
            {
                BLoadingIngredientes = false;
            }
        }

        private void HidratarSeleccionadosDesdeProducto()
        {
            try
            {
                if (OProducto?.aIngredientes == null || OProducto.aIngredientes.Count == 0) return;

                LstIngredientesSeleccionados.Clear();

                foreach (var pi in OProducto.aIngredientes)
                {
                    var ing = _cacheIngredientes?.FirstOrDefault(x => x.sIdMongo == pi.sIdIngrediente);

                    // Si no existe en cache, crea placeholder mínimo
                    if (ing == null)
                    {
                        ing = new Ingrediente
                        {
                            sIdMongo = pi.sIdIngrediente,
                            sNombre = "(Ingrediente)"
                        };
                    }

                    LstIngredientesSeleccionados.Add(new IngredienteUsoItem(ing)
                    {
                        SCantidadUso = pi.iCantidadUso.ToString(CultureInfo.InvariantCulture)
                    });
                }
            }
            catch
            {
                // silencioso
            }
        }

        [RelayCommand]
        public void BuscarIngredientes()
        {
            try
            {
                if (!_ingredientesInicializados || _cacheIngredientes == null)
                {
                    LstResultadosIngredientes = new ObservableCollection<Ingrediente>();
                    return;
                }

                var q = (SBusquedaIngrediente ?? "").Trim();
                if (string.IsNullOrWhiteSpace(q))
                {
                    LstResultadosIngredientes = new ObservableCollection<Ingrediente>();
                    return;
                }

                var normQ = Normalizar(q);

                // Evitar mostrar ya seleccionados
                var setSel = new HashSet<string>(LstIngredientesSeleccionados.Select(x => x.Ingrediente.sIdMongo));

                var filtrados = _cacheIngredientes
                    .Where(i => !setSel.Contains(i.sIdMongo))
                    .Where(i => MatchDesdePrimerCaracter(i, normQ))
                    .Take(50)
                    .ToList();

                LstResultadosIngredientes = new ObservableCollection<Ingrediente>(filtrados);
            }
            catch
            {
                // silencioso
            }
        }

        private static bool MatchDesdePrimerCaracter(Ingrediente i, string normQ)
        {
            // "desde el primer carácter": validamos por palabra (tokens)
            var texto = Normalizar(i?.sNombre ?? "");
            if (texto.StartsWith(normQ)) return true;

            var tokens = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return tokens.Any(t => t.StartsWith(normQ));
        }

        [RelayCommand]
        public void AgregarIngrediente(Ingrediente ing)
        {
            if (ing == null) return;

            if (LstIngredientesSeleccionados.Any(x => x.Ingrediente.sIdMongo == ing.sIdMongo))
                return;

            LstIngredientesSeleccionados.Add(new IngredienteUsoItem(ing));

            var r = LstResultadosIngredientes.FirstOrDefault(x => x.sIdMongo == ing.sIdMongo);
            if (r != null) LstResultadosIngredientes.Remove(r);

            SErrorIngredientes = "";
            OnPropertyChanged(nameof(BRequiereIngredientes));
        }

        [RelayCommand]
        public void QuitarIngrediente(IngredienteUsoItem item)
        {
            if (item == null) return;
            if (LstIngredientesSeleccionados.Contains(item))
                LstIngredientesSeleccionados.Remove(item);
        }

        [RelayCommand]
        public void LimpiarIngredientes()
        {
            LstIngredientesSeleccionados.Clear();
            SBusquedaIngrediente = "";
            LstResultadosIngredientes = new ObservableCollection<Ingrediente>();
            SErrorIngredientes = "";
        }

        // ==============================

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
                if (result == null) return;

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

        #region VARIANTES

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

        public void CambioTipoProducto()
        {
            if (SProductoSeleccionado.Equals("Extra", StringComparison.OrdinalIgnoreCase))
            {
                BHabilitaraAgregarVarientes = false;
                Variantes.Clear();
                LimpiarIngredientes();
            }
            else
            {
                BHabilitaraAgregarVarientes = true;

                // ✅ si vuelves a Platillo/Bebida, garantiza que ya estén cargados
                _ = InitIngredientes();
            }

            OnPropertyChanged(nameof(BRequiereIngredientes));
        }


        #region CREAR / ACTUALIZAR

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

            if (!ValidarCamposBasicos()) return;

            await _popupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
            {
                try
                {
                    string urlFinal = await SubirImagenAsync();
                    if (urlFinal == null && _imagenBytes != null && _imagenBytes.Length > 0)
                        return;

                    var imagenesList = new List<object>();
                    if (!string.IsNullOrWhiteSpace(urlFinal))
                        imagenesList.Add(new { sURLImagen = urlFinal });

                    var listVars = Variantes.Select(v => new { sVariante = v.sVariante }).ToList();

                    int iTipoProducto = SProductoSeleccionado switch
                    {
                        "Platillo" => 1,
                        "Bebida" => 2,
                        "Extra" => 3,
                        _ => 0
                    };

                    // ✅ Ingredientes para enviar
                    var ingredientesBody = BuildIngredientesBody();
                    if (ingredientesBody == null) return; // ya mostró error

                    var body = new
                    {
                        sNombre = OProducto.sNombre,
                        iCostoReal = OProducto.iCostoReal,
                        iCostoPublico = OProducto.iCostoPublico,
                        imagenes = imagenesList,
                        aVariantes = listVars,
                        iTipoProducto = iTipoProducto,
                        aIngredientes = ingredientesBody
                    };

                    var resp = await _httpApiService.PostAsync("api/productos", body, true);
                    if (resp != null && resp.IsSuccessStatusCode)
                    {
                        var respJson = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Producto>>();
                        if (respJson != null && respJson.bSuccess)
                        {
                            await vm.Cerrar();
                            var mainPage = Application.Current?.Windows[0].Page;
                            if (mainPage != null)
                                await mainPage.DisplayAlert("Éxito", "Producto creado correctamente", "OK");

                            ResetFormulario(); // ✅ limpia todo incluyendo ingredientes
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
                    string urlFinal = await SubirImagenAsync();
                    if (urlFinal == null && _imagenBytes != null && _imagenBytes.Length > 0)
                        return;

                    List<object> imagenesList = new();
                    if (!string.IsNullOrEmpty(urlFinal))
                    {
                        imagenesList.Add(new { sURLImagen = urlFinal });
                    }
                    else if (OProducto.aImagenes != null && OProducto.aImagenes.Count > 0)
                    {
                        imagenesList.AddRange(OProducto.aImagenes.Select(i => new { i.sURLImagen }));
                    }

                    var listVars = Variantes.Select(v => new { sVariante = v.sVariante }).ToList();

                    int iTipoProducto = SProductoSeleccionado switch
                    {
                        "Platillo" => 1,
                        "Bebida" => 2,
                        "Extra" => 3,
                        _ => 0
                    };

                    // ✅ Ingredientes para enviar
                    var ingredientesBody = BuildIngredientesBody();
                    if (ingredientesBody == null) return;

                    var body = new
                    {
                        sNombre = OProducto.sNombre,
                        iCostoReal = OProducto.iCostoReal,
                        iCostoPublico = OProducto.iCostoPublico,
                        imagenes = imagenesList,
                        aVariantes = listVars,
                        iTipoProducto = iTipoProducto,
                        aIngredientes = ingredientesBody
                    };

                    var resp = await _httpApiService.PutAsync($"api/productos/{OProducto.sIdMongo}", body, true);
                    if (resp != null && resp.IsSuccessStatusCode)
                    {
                        var respJson = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Producto>>();
                        if (respJson != null && respJson.bSuccess)
                        {
                            await vm.Cerrar();
                            var mainPage = Application.Current?.Windows[0].Page;
                            if (mainPage != null)
                                await mainPage.DisplayAlert("Actualizado", "Producto actualizado correctamente", "OK");
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

        // ==============================
        // ✅ VALIDACIÓN + BUILD INGREDIENTES
        // ==============================

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

            // ✅ Ingredientes obligatorios (si NO es Extra)
            if (BRequiereIngredientes)
            {
                if (LstIngredientesSeleccionados.Count < 1)
                {
                    MostrarError("Debes agregar al menos 1 ingrediente.");
                    return false;
                }

                foreach (var item in LstIngredientesSeleccionados)
                {
                    var s = (item.SCantidadUso ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(s))
                    {
                        MostrarError($"Captura cantidad para: {item.Ingrediente?.sNombre}");
                        return false;
                    }

                    if (!TryParseCantidad(s, out var cant))
                    {
                        MostrarError($"Cantidad inválida en: {item.Ingrediente?.sNombre}");
                        return false;
                    }

                    if (cant <= 0)
                    {
                        MostrarError($"La cantidad debe ser > 0 en: {item.Ingrediente?.sNombre}");
                        return false;
                    }
                }
            }

            return true;
        }

        private List<object> BuildIngredientesBody()
        {
            SErrorIngredientes = "";

            if (!BRequiereIngredientes)
                return new List<object>(); // Extra: mandamos vacío

            var lista = new List<object>();

            foreach (var item in LstIngredientesSeleccionados)
            {
                var s = (item.SCantidadUso ?? "").Trim();

                if (!TryParseCantidad(s, out var cant) || cant <= 0)
                {
                    MostrarError($"Cantidad inválida para: {item.Ingrediente?.sNombre}");
                    return null;
                }

                lista.Add(new
                {
                    sIdIngrediente = item.Ingrediente.sIdMongo,
                    iCantidadUso = cant
                });
            }

            return lista;
        }

        private static bool TryParseCantidad(string input, out decimal value)
        {
            input = (input ?? "").Trim();

            // Acepta "1", "1.5", "1,5"
            if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return true;

            if (decimal.TryParse(input, NumberStyles.Any, new CultureInfo("es-MX"), out value))
                return true;

            value = 0;
            return false;
        }

        private static string Normalizar(string s)
        {
            s = (s ?? "").Trim().ToLowerInvariant();
            var formD = s.Normalize(System.Text.NormalizationForm.FormD);
            var chars = formD.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
            return new string(chars.ToArray());
        }

        // ==============================

        #region AUX: SUBIR IMAGEN + RESET

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

            return string.Empty;
        }

        private void ResetFormulario()
        {
            OProducto.sNombre = string.Empty;
            OProducto.iCostoReal = 0;
            OProducto.iCostoPublico = 0;
            OProducto.iTipoProducto = 1;

            Variantes.Clear();
            SVarianteNueva = string.Empty;

            // ✅ Limpia ingredientes
            LimpiarIngredientes();

            OnPropertyChanged(nameof(OProducto));

            _imagenBytes = null;
            _nombreArchivoImagen = null;
            SImagenPreview = null;
            SImagenPreviewEscritorio = null;
            BHabilitarQuitarImagen = false;

            SProductoSeleccionado = LstProductos[0];
            STituloPagina = "Crear Producto";
            BEsEdicion = false;
            OnPropertyChanged(nameof(BEsEdicion));
        }

        private async void MostrarError(string sMensaje)
        {
            var mainPage = Application.Current?.Windows[0].Page;
            if (mainPage != null)
                await mainPage.DisplayAlert("Error", sMensaje, "OK");
        }

        #endregion
    }

    public partial class IngredienteUsoItem : ObservableObject
    {
        private bool _sanitizing;

        public Ingrediente Ingrediente { get; }

        public string SNombre => Ingrediente?.sNombre ?? "";

        public string SUnidad
            => string.IsNullOrWhiteSpace(Ingrediente?.sUnidad)
                ? ""
                : Ingrediente.sUnidad.Trim().ToUpperInvariant();

        [ObservableProperty]
        private string sCantidadUso = "1";

        partial void OnSCantidadUsoChanged(string oldValue, string newValue)
        {
            if (_sanitizing) return;

            var v = newValue ?? "";

            // Permitir vacío mientras el usuario edita
            if (v.Length == 0) return;

            var sb = new System.Text.StringBuilder();
            bool hasSep = false;

            foreach (var ch in v.Trim())
            {
                if (ch == '-') continue;

                if (char.IsDigit(ch))
                {
                    sb.Append(ch);
                    continue;
                }

                if ((ch == '.' || ch == ',') && !hasSep)
                {
                    sb.Append(ch);
                    hasSep = true;
                }
                // Ignora cualquier otro caracter
            }

            var sanitized = sb.ToString();

            if (!string.Equals(sanitized, v, StringComparison.Ordinal))
            {
                _sanitizing = true;
                SCantidadUso = sanitized; // NO fuerza "0"
                _sanitizing = false;
            }
        }

        public IngredienteUsoItem(Ingrediente ing)
        {
            Ingrediente = ing;
        }
    }


    public class RespImagen
    {
        public string sRutaImagenS3 { get; set; }
    }
}

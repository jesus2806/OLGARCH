using AppGestorVentas.Models;
using AppGestorVentas.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace AppGestorVentas.ViewModels.IngredienteViewModels
{
    public partial class DatosIngredienteViewModel : ObservableObject, IQueryAttributable
    {
        private readonly HttpApiService _httpApiService;

        private Ingrediente? _ingredienteActualizar;
        private string _idMongo = string.Empty;

        // Evita loops al sanitizar texto en partial methods
        private bool _bSanitizing;

        [ObservableProperty]
        public string sTituloPagina = "Alta Ingrediente";

        [ObservableProperty]
        public bool bProcesoActualizacion;

        [ObservableProperty]
        public bool bProcesoRegistrar = true;

        // ====== UNIDADES (Picker) ======
        [ObservableProperty]
        public ObservableCollection<string> lstUnidades = new();

        [ObservableProperty]
        private string sUnidadSeleccionada = string.Empty;

        // ====== Campos (UI como string para validar/convertir) ======
        [ObservableProperty]
        private string sNombre = string.Empty;

        [ObservableProperty]
        private string sCantidadEnAlmacen = "0";
        partial void OnSCantidadEnAlmacenChanged(string value)
            => SCantidadEnAlmacen = SanitizeIntText(value, maxDigits: 3);

        [ObservableProperty]
        private string sCantidadMinima = "0";
        partial void OnSCantidadMinimaChanged(string value)
            => SCantidadMinima = SanitizeIntText(value, maxDigits: 3);

        [ObservableProperty]
        private string sCostoUnidad = "0";
        partial void OnSCostoUnidadChanged(string value)
            => SCostoUnidad = SanitizeIntText(value, maxDigits: 3);

        // ====== Errores ======
        [ObservableProperty] private string sErrorNombre = string.Empty;
        [ObservableProperty] private string sErrorUnidad = string.Empty;
        [ObservableProperty] private string sErrorCantidadEnAlmacen = string.Empty;
        [ObservableProperty] private string sErrorCantidadMinima = string.Empty;
        [ObservableProperty] private string sErrorCostoUnidad = string.Empty;

        public DatosIngredienteViewModel(HttpApiService httpApiService)
        {
            _httpApiService = httpApiService;

            // Unidades predeterminadas (ajústalas a tu negocio)
            LstUnidades = new ObservableCollection<string>
            {
                "PZA",
                "G",
                "KG",
                "ML",
                "L"
            };

            SUnidadSeleccionada = LstUnidades.First(); // default
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("oIngrediente", out var obj) && obj is Ingrediente ing)
            {
                _ingredienteActualizar = ing;
                _idMongo = ing.sIdMongo;

                STituloPagina = "Actualizar Ingrediente";
                BProcesoRegistrar = false;
                BProcesoActualizacion = true;

                SNombre = ing.sNombre;
                SCantidadEnAlmacen = ing.iCantidadEnAlmacen.ToString();
                SCantidadMinima = ing.iCantidadMinima.ToString();
                SCostoUnidad = ing.iCostoUnidad.ToString();

                // Setear unidad seleccionada; si no existe en el catálogo, la agregamos
                string unidad = (ing.sUnidad ?? "").Trim().ToUpper();
                if (!string.IsNullOrWhiteSpace(unidad))
                {
                    if (!LstUnidades.Contains(unidad))
                        LstUnidades.Add(unidad);

                    SUnidadSeleccionada = unidad;
                }
                else
                {
                    SUnidadSeleccionada = LstUnidades.First();
                }
            }
            else
            {
                STituloPagina = "Alta Ingrediente";
                BProcesoRegistrar = true;
                BProcesoActualizacion = false;

                _ingredienteActualizar = null;
                _idMongo = string.Empty;

                LimpiarFormulario();
                LimpiarErrores();
            }
        }

        // =========================
        // Registrar (ALTA)
        // =========================
        [RelayCommand]
        public async Task RegistrarAsync()
        {
            NormalizarNumericos();
            string sMensajeError = string.Empty;

            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    sMensajeError = "No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.";
                    return;
                }

                if (!ValidarEntradas(out int iAlmacen, out int iMin, out int iCosto)) return;

                var nuevo = new Ingrediente
                {
                    sNombre = SNombre.Trim(),
                    sUnidad = (SUnidadSeleccionada ?? "").Trim(),
                    iCantidadEnAlmacen = iAlmacen,
                    iCantidadMinima = iMin,
                    iCostoUnidad = iCosto
                };

                HttpResponseMessage? resp = await _httpApiService.PostAsync("api/ingredientes", nuevo, bRequiereToken: true);
                if (resp != null)
                {
                    var api = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Ingrediente>>();

                    if (resp.IsSuccessStatusCode && api != null && api.bSuccess)
                    {
                        await MostrarInfo("Alta", "Ingrediente agregado correctamente.");
                        // ✅ Se limpia para capturar otro
                        LimpiarFormulario();
                        LimpiarErrores();
                        return;
                    }

                    sMensajeError = api?.Error?.sDetails ?? "Error al crear ingrediente.";
                }
                else
                {
                    sMensajeError = "No se recibió respuesta del servidor.";
                }
            }
            catch (Exception ex)
            {
                sMensajeError = $"{ex.Message}";
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(sMensajeError))
                {
                    await MostrarError(sMensajeError);
                    // ✅ También limpia para poder intentar agregar otro (como pediste)
                    LimpiarFormulario();
                    LimpiarErrores();
                }
            }
        }

        // =========================
        // Actualizar
        // =========================
        [RelayCommand]
        public async Task ActualizarAsync()
        {
            NormalizarNumericos();
            string sMensajeError = string.Empty;

            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    await MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(_idMongo))
                {
                    await MostrarError("No se encontró el ID del ingrediente a actualizar.");
                    return;
                }

                if (!ValidarEntradas(out int iAlmacen, out int iMin, out int iCosto)) return;

                var upd = new Ingrediente
                {
                    sNombre = SNombre.Trim(),
                    sUnidad = (SUnidadSeleccionada ?? "").Trim(),
                    iCantidadEnAlmacen = iAlmacen,
                    iCantidadMinima = iMin,
                    iCostoUnidad = iCosto
                };

                HttpResponseMessage? resp = await _httpApiService.PutAsync($"api/ingredientes/{_idMongo}", upd, bRequiereToken: true);
                if (resp != null)
                {
                    var api = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Ingrediente>>();
                    if (resp.IsSuccessStatusCode && api != null && api.bSuccess)
                    {
                        await MostrarInfo("Actualizado", "Ingrediente actualizado con éxito.");
                        await Shell.Current.GoToAsync(".."); // en update sí regresa
                        return;
                    }

                    sMensajeError = api?.Error?.sDetails ?? "Error al actualizar ingrediente.";
                }
                else
                {
                    sMensajeError = "No se recibió respuesta del servidor.";
                }
            }
            catch (Exception ex)
            {
                sMensajeError = $"{ex.Message}";
            }

            if (!string.IsNullOrWhiteSpace(sMensajeError))
                await MostrarError(sMensajeError);
        }

        // =========================
        // Validaciones
        // =========================
        private bool ValidarEntradas(out int iAlmacen, out int iMin, out int iCosto)
        {
            LimpiarErrores();

            bool ok = true;

            iAlmacen = 0;
            iMin = 0;
            iCosto = 0;

            // Nombre: mínimo 2 letras
            var nombre = (SNombre ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Length < 2)
            {
                SErrorNombre = "El nombre debe tener mínimo 2 letras.";
                ok = false;
            }

            // Unidad: debe estar seleccionada
            if (string.IsNullOrWhiteSpace(SUnidadSeleccionada))
            {
                SErrorUnidad = "Selecciona una unidad.";
                ok = false;
            }

            // Números: no negativos y <= 999 (por tu requerimiento)
            if (!int.TryParse(SCantidadEnAlmacen, out iAlmacen) || iAlmacen < 0 || iAlmacen > 999)
            {
                SErrorCantidadEnAlmacen = "Cantidad inválida (0 a 999).";
                ok = false;
            }

            if (!int.TryParse(SCantidadMinima, out iMin) || iMin < 0 || iMin > 999)
            {
                SErrorCantidadMinima = "Cantidad inválida (0 a 999).";
                ok = false;
            }

            if (!int.TryParse(SCostoUnidad, out iCosto) || iCosto < 0 || iCosto > 999)
            {
                SErrorCostoUnidad = "Costo inválido (0 a 999).";
                ok = false;
            }

            return ok;
        }

        private void LimpiarErrores()
        {
            SErrorNombre = "";
            SErrorUnidad = "";
            SErrorCantidadEnAlmacen = "";
            SErrorCantidadMinima = "";
            SErrorCostoUnidad = "";
        }

        private void LimpiarFormulario()
        {
            SNombre = "";
            SCantidadEnAlmacen = "0";
            SCantidadMinima = "0";
            SCostoUnidad = "0";
            SUnidadSeleccionada = LstUnidades.FirstOrDefault() ?? "";
        }

        [RelayCommand]
        private void NormalizarNumericos()
        {
            if (string.IsNullOrWhiteSpace(SCantidadEnAlmacen)) SCantidadEnAlmacen = "0";
            if (string.IsNullOrWhiteSpace(SCantidadMinima)) SCantidadMinima = "0";
            if (string.IsNullOrWhiteSpace(SCostoUnidad)) SCostoUnidad = "0";
        }

        // =========================
        // Sanitización (evita negativos y texto no numérico)
        // =========================
        private string SanitizeIntText(string input, int maxDigits)
        {
            if (_bSanitizing) return input ?? "";

            try
            {
                _bSanitizing = true;

                var s = (input ?? "").Trim();

                // ✅ Si está vacío, lo dejamos vacío mientras edita
                if (string.IsNullOrEmpty(s))
                    return "";

                // dejar solo dígitos
                var digits = new string(s.Where(char.IsDigit).ToArray());

                if (string.IsNullOrEmpty(digits))
                    return "";

                if (digits.Length > maxDigits)
                    digits = digits.Substring(0, maxDigits);

                // quitar ceros a la izquierda (pero conservar "0" si todo eran ceros)
                digits = digits.TrimStart('0');
                if (digits.Length == 0) digits = "0";

                return digits;
            }
            finally
            {
                _bSanitizing = false;
            }
        }


        // =========================
        // Alerts
        // =========================
        private static async Task MostrarError(string sMensaje)
        {
            var mainPage = Application.Current?.Windows[0].Page;
            if (mainPage != null)
                await mainPage.DisplayAlert("Error", sMensaje, "OK");
        }

        private static async Task MostrarInfo(string titulo, string sMensaje)
        {
            var mainPage = Application.Current?.Windows[0].Page;
            if (mainPage != null)
                await mainPage.DisplayAlert(titulo, sMensaje, "OK");
        }
    }
}

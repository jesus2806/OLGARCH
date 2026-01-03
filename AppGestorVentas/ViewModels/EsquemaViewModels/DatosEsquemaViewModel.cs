using AppGestorVentas.Models;
using AppGestorVentas.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Linq;

namespace AppGestorVentas.ViewModels.EsquemaViewModels
{
    public partial class DatosEsquemaViewModel : ObservableObject, IQueryAttributable
    {
        private readonly HttpApiService _http;
        private string _idEsquema = "";

        [ObservableProperty] private string sTituloPagina = "Alta Esquema";
        [ObservableProperty] private string sNombre = "";
        [ObservableProperty] private string sErrorNombre = "";

        // ✅ Error general para los montos por día
        [ObservableProperty] private string sErrorDias = "";

        [ObservableProperty] private ObservableCollection<DiaEsquema> lstDias = new();

        [ObservableProperty] private bool bProcesoActualizacion;
        [ObservableProperty] private bool bProcesoRegistrar;

        private static readonly string[] DIAS = new[]
        {
            "lunes","martes","miercoles","jueves","viernes","sabado","domingo"
        };

        public DatosEsquemaViewModel(HttpApiService httpApiService)
        {
            _http = httpApiService;

            // Alta por default
            BProcesoRegistrar = true;
            BProcesoActualizacion = false;
            AsegurarSemana();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            try
            {
                if (query.TryGetValue("oEsquema", out var o) && o is Esquema esquema)
                {
                    // Edición
                    _idEsquema = esquema.sIdMongo;
                    STituloPagina = "Actualizar Esquema";
                    SNombre = esquema.sNombre ?? "";
                    LstDias = esquema.aDia ?? new ObservableCollection<DiaEsquema>();
                    AsegurarSemana();

                    BProcesoRegistrar = false;
                    BProcesoActualizacion = true;
                }
                else
                {
                    // Alta
                    LimpiarFormulario();
                }
            }
            catch (Exception ex)
            {
                _ = MostrarError($"ApplyQueryAttributes: {ex.Message}");
            }
        }

        private void AsegurarSemana()
        {
            var map = LstDias.ToDictionary(d => (d.sDia ?? "").ToLowerInvariant(), d => d);

            var nueva = new ObservableCollection<DiaEsquema>();
            foreach (var dia in DIAS)
            {
                if (map.TryGetValue(dia, out var ex))
                {
                    ex.sDia = dia;
                    nueva.Add(ex);
                }
                else
                {
                    // ✅ vacío por default (obligatorio llenar para registrar)
                    nueva.Add(new DiaEsquema { sDia = dia, dValor = "" });
                }
            }

            LstDias = nueva;
        }

        private static bool TryParseMonto(string input, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(input)) return false;

            input = input.Trim();

            // Acepta "10.5" y "10,5"
            return
                decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value) ||
                decimal.TryParse(input, NumberStyles.Number, new CultureInfo("es-MX"), out value);
        }

        private bool ValidarParaRegistrar()
        {
            SErrorNombre = "";
            SErrorDias = "";

            bool ok = true;

            // ✅ nombre obligatorio
            if (string.IsNullOrWhiteSpace(SNombre))
            {
                SErrorNombre = "El nombre del esquema es obligatorio.";
                ok = false;
            }

            // ✅ todos los días obligatorios + numéricos + NO negativos
            foreach (var d in LstDias)
            {
                var dia = (d.sDia ?? "").Trim();
                var txt = (d.dValor ?? "").Trim();

                if (string.IsNullOrWhiteSpace(txt))
                {
                    SErrorDias = $"Debes capturar el pago para {dia}.";
                    ok = false;
                    break;
                }

                if (!TryParseMonto(txt, out var monto))
                {
                    SErrorDias = $"El pago de {dia} no es válido.";
                    ok = false;
                    break;
                }

                if (monto < 0)
                {
                    SErrorDias = $"No se permiten negativos (revisa {dia}).";
                    ok = false;
                    break;
                }
            }

            return ok;
        }

        private bool ValidarParaActualizar()
        {
            // mismas reglas (si quieres permitir vacíos en update, aquí sería distinto)
            return ValidarParaRegistrar();
        }

        private Esquema BuildPayload()
        {
            // ✅ ya validado: todos traen valor numérico >= 0
            var dias = new ObservableCollection<DiaEsquema>(
                LstDias.Select(x =>
                {
                    TryParseMonto(x.dValor ?? "0", out var monto);

                    return new DiaEsquema
                    {
                        sDia = x.sDia,
                        // ✅ manda invariant para evitar problemas con coma/punto
                        dValor = monto.ToString(CultureInfo.InvariantCulture)
                    };
                })
            );

            return new Esquema
            {
                sNombre = SNombre.Trim(),
                aDia = dias
            };
        }

        private void LimpiarFormulario()
        {
            _idEsquema = "";
            STituloPagina = "Alta Esquema";

            SNombre = "";
            SErrorNombre = "";
            SErrorDias = "";

            LstDias = new ObservableCollection<DiaEsquema>();
            AsegurarSemana();

            BProcesoRegistrar = true;
            BProcesoActualizacion = false;
        }

        // ✅ Crear esquema (obligatorio todo + no negativos + limpiar al éxito)
        [RelayCommand]
        private async Task Registrar()
        {
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    await MostrarError("No tienes acceso a Internet.");
                    return;
                }

                if (!ValidarParaRegistrar()) return;

                var payload = BuildPayload();
                var resp = await _http.PostAsync("api/esquemas", payload, bRequiereToken: true);
                if (resp == null) return;

                var api = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Esquema>>();
                var esquemaCreado = api?.lData?.FirstOrDefault();

                if (resp.IsSuccessStatusCode && api != null && api.bSuccess && esquemaCreado != null)
                {
                    await MostrarOk("Esquema creado correctamente.");

                    // ✅ limpiar formulario después de registrar exitosamente
                    LimpiarFormulario();
                }
                else
                {
                    await MostrarError(api?.Error?.sDetails ?? "No se pudo crear el esquema.");
                }
            }
            catch (Exception ex)
            {
                await MostrarError($"Registrar: {ex.Message}");
            }
        }

        // ✅ Actualizar esquema (misma validación)
        [RelayCommand]
        private async Task Actualizar()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_idEsquema))
                {
                    await MostrarError("No hay esquema para actualizar.");
                    return;
                }

                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    await MostrarError("No tienes acceso a Internet.");
                    return;
                }

                if (!ValidarParaActualizar()) return;

                var payload = BuildPayload();
                var resp = await _http.PutAsync($"api/esquemas/{_idEsquema}", payload, bRequiereToken: true);
                if (resp == null) return;

                var api = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Esquema>>();
                if (resp.IsSuccessStatusCode && api != null && api.bSuccess)
                    await MostrarOk("Esquema actualizado.");
                else
                    await MostrarError(api?.Error?.sDetails ?? "No se pudo actualizar el esquema.");
            }
            catch (Exception ex)
            {
                await MostrarError($"Actualizar: {ex.Message}");
            }
        }

        // ✅ Eliminar esquema
        [RelayCommand]
        private async Task Eliminar()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_idEsquema))
                {
                    await MostrarError("No hay esquema seleccionado.");
                    return;
                }

                var main = Application.Current?.Windows[0].Page;
                if (main == null) return;

                bool confirm = await main.DisplayAlert("Eliminar", "¿Eliminar este esquema?", "Sí", "No");
                if (!confirm) return;

                var resp = await _http.DeleteAsync($"api/esquemas/{_idEsquema}", bRequiereToken: true);
                if (resp == null) return;

                var api = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Esquema>>();
                if (resp.IsSuccessStatusCode && api != null && api.bSuccess)
                {
                    await MostrarOk("Esquema eliminado.");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await MostrarError(api?.Error?.sDetails ?? "No se pudo eliminar.");
                }
            }
            catch (Exception ex)
            {
                await MostrarError($"Eliminar: {ex.Message}");
            }
        }

        private static async Task MostrarError(string msg)
        {
            var mainPage = Application.Current?.Windows[0].Page;
            if (mainPage != null) await mainPage.DisplayAlert("Error", msg, "OK");
        }

        private static async Task MostrarOk(string msg)
        {
            var mainPage = Application.Current?.Windows[0].Page;
            if (mainPage != null) await mainPage.DisplayAlert("OK", msg, "OK");
        }
    }
}

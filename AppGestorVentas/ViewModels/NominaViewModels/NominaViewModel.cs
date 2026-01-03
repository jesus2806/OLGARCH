using AppGestorVentas.Models;
using AppGestorVentas.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Linq;
using System.Globalization;

namespace AppGestorVentas.ViewModels.NominaViewModels
{
    public partial class NominaViewModel : ObservableObject
    {
        private readonly HttpApiService _http;

        private CancellationTokenSource? _ctsBuscar;

        // Cache de usuarios e índice normalizado para búsquedas rápidas
        private List<Usuario>? _cacheUsuarios;
        private List<UsuarioIndex>? _idxUsuarios;

        [ObservableProperty] private bool bLoading;

        [ObservableProperty] private ObservableCollection<Esquema> lstEsquemas = new();
        [ObservableProperty] private Esquema? esquemaSeleccionado;

        [ObservableProperty] private string sBusqueda = "";
        [ObservableProperty] private ObservableCollection<Usuario> lstResultados = new();
        [ObservableProperty] private ObservableCollection<Usuario> lstSeleccionados = new();

        [ObservableProperty] private Usuario? resultadoSeleccionado;

        [ObservableProperty] private string sErrorEsquema = "";
        [ObservableProperty] private string sErrorBusqueda = "";
        [ObservableProperty] private string sErrorAsignacion = "";

        [ObservableProperty] private bool bAsignando;

        public bool BPuedeAsignar =>
            !BAsignando &&
            EsquemaSeleccionado != null &&
            LstSeleccionados.Count > 0;

        // ✅ Para mostrar/ocultar las sugerencias
        public bool BMostrarSugerencias =>
            !string.IsNullOrWhiteSpace(SBusqueda) &&
            LstResultados != null &&
            LstResultados.Count > 0;

        partial void OnEsquemaSeleccionadoChanged(Esquema? value)
        {
            SErrorEsquema = "";
            OnPropertyChanged(nameof(BPuedeAsignar));
        }

        partial void OnBAsignandoChanged(bool value)
        {
            OnPropertyChanged(nameof(BPuedeAsignar));
        }

        // ✅ Tap en un item del autocomplete = agregar
        partial void OnResultadoSeleccionadoChanged(Usuario? value)
        {
            if (value == null) return;
            AgregarUsuario(value);
            ResultadoSeleccionado = null; // importante para poder seleccionar el mismo tipo de item después
        }

        // ✅ Autocomplete en vivo (debounce)
        partial void OnSBusquedaChanged(string value)
        {
            SErrorBusqueda = "";
            SErrorAsignacion = "";

            _ctsBuscar?.Cancel();
            _ctsBuscar = new CancellationTokenSource();
            var token = _ctsBuscar.Token;

            _ = AutocompleteDebouncedAsync(value, token);
        }

        public NominaViewModel(HttpApiService httpApiService)
        {
            _http = httpApiService;

            LstSeleccionados.CollectionChanged += (_, __) =>
                OnPropertyChanged(nameof(BPuedeAsignar));
        }

        [RelayCommand]
        private async Task Init()
        {
            await CargarEsquemas();
            // ✅ Opcional: precargar usuarios para que el autocomplete sea instantáneo
            await EnsureUsuariosCargadosAsync(CancellationToken.None);
        }

        [RelayCommand]
        private async Task CargarEsquemas()
        {
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    await MostrarError("No tienes acceso a Internet.");
                    return;
                }

                BLoading = true;

                var resp = await _http.GetAsync("api/esquemas", bRequiereToken: true);
                if (resp == null) return;

                var api = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Esquema>>();
                if (resp.IsSuccessStatusCode && api != null && api.bSuccess && api.lData != null)
                {
                    LstEsquemas = new ObservableCollection<Esquema>(api.lData);
                }
                else
                {
                    await MostrarError(api?.Error?.sDetails ?? "No se pudieron cargar los esquemas.");
                }
            }
            catch (Exception ex)
            {
                await MostrarError($"CargarEsquemas: {ex.Message}");
            }
            finally { BLoading = false; }
        }

        [RelayCommand]
        private async Task RefrescarUsuarios()
        {
            _cacheUsuarios = null;
            _idxUsuarios = null;

            LstResultados = new ObservableCollection<Usuario>();
            OnPropertyChanged(nameof(BMostrarSugerencias));

            // si ya hay texto, re-filtrar
            if (!string.IsNullOrWhiteSpace(SBusqueda))
                await Buscar();
        }

        // ✅ Por si el usuario presiona Enter/Buscar
        [RelayCommand]
        private async Task Buscar()
        {
            await AutocompleteAsync(SBusqueda, CancellationToken.None);
        }

        private async Task AutocompleteDebouncedAsync(string texto, CancellationToken token)
        {
            try
            {
                await Task.Delay(120, token);
                if (token.IsCancellationRequested) return;

                await AutocompleteAsync(texto, token);
            }
            catch (TaskCanceledException) { }
            catch { }
        }

        private async Task AutocompleteAsync(string texto, CancellationToken token)
        {
            var q = (texto ?? "").Trim();

            if (string.IsNullOrWhiteSpace(q))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    LstResultados = new ObservableCollection<Usuario>();
                    OnPropertyChanged(nameof(BMostrarSugerencias));
                });
                return;
            }

            await EnsureUsuariosCargadosAsync(token);
            if (_idxUsuarios == null) return;

            var normQ = Normalizar(q);

            var idsSeleccionados = new HashSet<string>(
                LstSeleccionados.Select(x => x.sIdMongo)
            );

            // ✅ Desde el 1er carácter (StartsWith), sin acentos/mayúsculas
            var filtrados = _idxUsuarios
                .Where(ix => !idsSeleccionados.Contains(ix.User.sIdMongo))
                .Where(ix => ix.Match(normQ))
                .Select(ix => ix.User)
                .Take(15)
                .ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                LstResultados = new ObservableCollection<Usuario>(filtrados);
                OnPropertyChanged(nameof(BMostrarSugerencias));
            });
        }

        private async Task EnsureUsuariosCargadosAsync(CancellationToken token)
        {
            if (_idxUsuarios != null) return;

            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                    return;

                BLoading = true;

                var resp = await _http.GetAsync("api/usuarios", bRequiereToken: true);
                if (resp == null) return;

                // ✅ Tu API regresa: data: [ {usuario}, {usuario} ]
                var api = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Usuario>>();
                if (!(resp.IsSuccessStatusCode && api != null && api.bSuccess && api.lData != null))
                {
                    await MostrarError(api?.Error?.sDetails ?? "No se pudieron cargar usuarios.");
                    return;
                }

                // Empleados: sin Admin
                _cacheUsuarios = api.lData.Where(u => u.iRol != 1).ToList();

                // Index normalizado
                _idxUsuarios = _cacheUsuarios.Select(u => new UsuarioIndex(u)).ToList();
            }
            catch (Exception ex)
            {
                await MostrarError($"CargarUsuarios: {ex.Message}");
            }
            finally
            {
                BLoading = false;
            }
        }

        [RelayCommand]
        private void AgregarUsuario(Usuario u)
        {
            if (u == null) return;

            if (LstSeleccionados.Any(x => x.sIdMongo == u.sIdMongo))
                return;

            LstSeleccionados.Add(u);

            // Limpia búsqueda y sugerencias para “feel” de autocomplete
            SBusqueda = "";
            LstResultados = new ObservableCollection<Usuario>();
            OnPropertyChanged(nameof(BMostrarSugerencias));
            OnPropertyChanged(nameof(BPuedeAsignar));
        }

        [RelayCommand]
        private void QuitarUsuario(Usuario u)
        {
            if (u == null) return;

            var item = LstSeleccionados.FirstOrDefault(x => x.sIdMongo == u.sIdMongo);
            if (item != null) LstSeleccionados.Remove(item);

            OnPropertyChanged(nameof(BPuedeAsignar));
        }

        [RelayCommand]
        private void LimpiarSeleccionados()
        {
            LstSeleccionados.Clear();
            OnPropertyChanged(nameof(BPuedeAsignar));
        }

        // ✅ POST /api/esquemas/:esquemaId/usuarios/:usuarioId
        [RelayCommand]
        private async Task AsignarEsquema()
        {
            try
            {
                SErrorEsquema = "";
                SErrorAsignacion = "";

                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    await MostrarError("No tienes acceso a Internet.");
                    return;
                }

                if (EsquemaSeleccionado == null)
                {
                    SErrorEsquema = "Selecciona un esquema primero.";
                    return;
                }

                if (LstSeleccionados.Count == 0)
                {
                    SErrorAsignacion = "Agrega al menos un usuario.";
                    return;
                }

                var main = Application.Current?.Windows[0].Page;
                if (main == null) return;

                bool confirm = await main.DisplayAlert(
                    "Confirmar",
                    $"¿Asignar \"{EsquemaSeleccionado.sNombre}\" a {LstSeleccionados.Count} usuario(s)?",
                    "Sí",
                    "No"
                );
                if (!confirm) return;

                BAsignando = true;
                BLoading = true;

                string esquemaId = EsquemaSeleccionado.sIdMongo;

                int ok = 0;
                var errores = new List<string>();

                foreach (var u in LstSeleccionados.ToList())
                {
                    var resp = await _http.PostAsync(
                        $"api/esquemas/{esquemaId}/usuarios/{u.sIdMongo}",
                        new { }, // body vacío
                        bRequiereToken: true
                    );

                    if (resp == null)
                    {
                        errores.Add($"{u.sUsuario}: sin respuesta");
                        continue;
                    }

                    var api = await resp.Content.ReadFromJsonAsync<ApiRespuesta<Usuario>>();
                    if (resp.IsSuccessStatusCode && api != null && api.bSuccess)
                        ok++;
                    else
                        errores.Add($"{u.sUsuario}: {api?.Error?.sDetails ?? "No se pudo asignar"}");
                }

                if (errores.Count == 0)
                {
                    await MostrarOk($"Asignación completa. Usuarios actualizados: {ok}");
                    LimpiarSeleccionados();
                    SBusqueda = "";
                    LstResultados = new ObservableCollection<Usuario>();
                    OnPropertyChanged(nameof(BMostrarSugerencias));
                }
                else
                {
                    await MostrarError(
                        $"Se asignó a {ok}, pero hubo {errores.Count} error(es):\n\n" +
                        string.Join("\n", errores.Take(10)) +
                        (errores.Count > 10 ? "\n..." : "")
                    );
                }
            }
            catch (Exception ex)
            {
                await MostrarError($"AsignarEsquema: {ex.Message}");
            }
            finally
            {
                BAsignando = false;
                BLoading = false;
                OnPropertyChanged(nameof(BPuedeAsignar));
            }
        }

        // ===== Helpers =====

        private static string Normalizar(string s)
        {
            s = (s ?? "").Trim().ToLowerInvariant();
            var normalized = s.Normalize(System.Text.NormalizationForm.FormD);

            var chars = normalized.Where(c =>
                CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);

            return new string(chars.ToArray());
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

        // Índice de búsqueda
        private sealed class UsuarioIndex
        {
            public Usuario User { get; }
            private readonly string _usuario;
            private readonly string[] _tokens;

            public UsuarioIndex(Usuario u)
            {
                User = u;

                _usuario = Normalizar(u.sUsuario);

                // Tokens: cada palabra del nombre + apellidos + usuario separado por puntos
                var tokens = new List<string>();

                void addTokens(string? value)
                {
                    var norm = Normalizar(value ?? "");
                    foreach (var t in norm.Split(new[] { ' ', '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries))
                        tokens.Add(t);
                }

                addTokens(u.sUsuario);
                addTokens(u.sNombre);
                addTokens(u.sApellidoPaterno);
                addTokens(u.sApellidoMaterno);

                _tokens = tokens.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToArray();
            }

            // ✅ Desde el 1er carácter: StartsWith
            public bool Match(string q)
            {
                if (string.IsNullOrWhiteSpace(q)) return false;

                if (_usuario.StartsWith(q)) return true;

                // cualquier token (nombre, apellido o partes del usuario)
                return _tokens.Any(t => t.StartsWith(q));
            }
        }
    }
}

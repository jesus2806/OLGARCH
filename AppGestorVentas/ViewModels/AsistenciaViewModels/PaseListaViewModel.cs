using System.Collections.ObjectModel;
using AppGestorVentas.Models.Asistencia;
using AppGestorVentas.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppGestorVentas.ViewModels.AsistenciaViewModels
{
    public partial class PaseListaViewModel : ObservableObject
    {
        private readonly HttpApiService _api;

        public ObservableCollection<RosterItemVm> LstRoster { get; } = new();

        [ObservableProperty] private bool bIsBusy;
        [ObservableProperty] private bool bHayCambios;
        [ObservableProperty] private string sBusqueda = "";
        [ObservableProperty] private DateTime dtDia = DateTime.Today;

        [ObservableProperty] private string sError = "";

        // ✅ para UI (simple)
        public bool BErrorVisible => !string.IsNullOrWhiteSpace(SError);

        partial void OnSErrorChanged(string value) => OnPropertyChanged(nameof(BErrorVisible));

        // Para habilitar/deshabilitar botones
        [ObservableProperty] private bool bPuedeCargar = true;
        [ObservableProperty] private bool bPuedeGuardar = false;

        public PaseListaViewModel(HttpApiService api)
        {
            _api = api;
            RecalcularBotones();
        }

        private string DiaApi => DtDia.ToString("yyyy-MM-dd");

        partial void OnBIsBusyChanged(bool value) => RecalcularBotones();
        partial void OnBHayCambiosChanged(bool value) => RecalcularBotones();

        private void RecalcularBotones()
        {
            BPuedeCargar = !BIsBusy;
            BPuedeGuardar = !BIsBusy && BHayCambios;
        }

        [RelayCommand]
        public async Task CargarAsync()
        {
            if (BIsBusy) return;

            try
            {
                BIsBusy = true;
                SError = "";

                var resp = await _api.GetAsistenciaRosterAsync(DiaApi, SBusqueda);

                if (!resp.bSuccess || resp.lData == null || resp.lData.Count == 0)
                {
                    SError = string.IsNullOrWhiteSpace(resp.sMessage) ? "No se pudo cargar la lista." : resp.sMessage;
                    LstRoster.Clear();
                    BHayCambios = false;
                    return;
                }

                var dto = resp.lData[0]; // data objeto -> lData[0]
                LstRoster.Clear();

                foreach (var it in dto.roster)
                {
                    var u = it.usuario;
                    var a = it.asistencia;

                    var estatus = string.IsNullOrWhiteSpace(a.sEstatus) ? "sin_marcar" : a.sEstatus;

                    // ✅ Si antes existían valores tipo "tarde/justificado", aquí los tomamos como asistió
                    var asistio = estatus == "presente" || estatus == "tarde" || estatus == "justificado";

                    LstRoster.Add(new RosterItemVm(
                        usuarioId: u._id,
                        nombre: u.NombreCompleto,
                        usuario: u.sUsuario,
                        asistio: asistio,
                        onChanged: () => BHayCambios = true
                    ));
                }

                BHayCambios = false;
            }
            catch (Exception ex)
            {
                SError = ex.Message;
            }
            finally
            {
                BIsBusy = false;
            }
        }

        [RelayCommand]
        public void TodosPresente()
        {
            foreach (var x in LstRoster) x.BAsistio = true;
            BHayCambios = true;
        }

        [RelayCommand]
        public void Limpiar()
        {
            foreach (var x in LstRoster) x.BAsistio = false;
            BHayCambios = true;
        }

        [RelayCommand]
        public async Task GuardarAsync()
        {
            if (BIsBusy) return;

            try
            {
                BIsBusy = true;
                SError = "";

                var req = new AsistenciaBulkRequest
                {
                    sDia = DiaApi,
                    items = LstRoster.Select(x => new AsistenciaBulkItem
                    {
                        oUsuario = x.UsuarioId,
                        sEstatus = x.SEstatus, // "presente" / "ausente"
                        sNotas = "" // ya no lo usas (simple)
                    }).ToList()
                };

                var resp = await _api.GuardarAsistenciaBulkAsync(req);

                if (!resp.bSuccess)
                {
                    SError = string.IsNullOrWhiteSpace(resp.sMessage) ? "No se pudo guardar." : resp.sMessage;
                    return;
                }

                BHayCambios = false;
            }
            catch (Exception ex)
            {
                SError = ex.Message;
            }
            finally
            {
                BIsBusy = false;
            }
        }
    }

    public partial class RosterItemVm : ObservableObject
    {
        private readonly Action _onChanged;

        public string UsuarioId { get; }
        public string Nombre { get; }
        public string Usuario { get; }

        // Esto se manda al backend
        [ObservableProperty] private string sEstatus;

        // ✅ lo que maneja el UI
        [ObservableProperty] private bool bAsistio;

        public RosterItemVm(string usuarioId, string nombre, string usuario, bool asistio, Action onChanged)
        {
            UsuarioId = usuarioId;
            Nombre = nombre;
            Usuario = usuario;
            _onChanged = onChanged;

            BAsistio = asistio;
            SEstatus = asistio ? "presente" : "ausente";
        }

        partial void OnBAsistioChanged(bool value)
        {
            SEstatus = value ? "presente" : "ausente";
            _onChanged?.Invoke();
        }
    }
}

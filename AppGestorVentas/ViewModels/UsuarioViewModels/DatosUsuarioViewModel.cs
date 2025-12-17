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

namespace AppGestorVentas.ViewModels.UsuarioViewModels
{
    public partial class DatosUsuarioViewModel : ObservableObject, IQueryAttributable
    {
        private LocalDatabaseService _localDatabaseService;
        private HttpApiService _httpApiService;
        private IPopupService _oIPopupService;
        private string _sNombreCampoUsuario = string.Empty;
        private string _sApellidoPaternoCampoUsuario = string.Empty;
        private Usuario? oUsuarioActualizar = null;
        private string _iIdMongoDB = string.Empty;

        [ObservableProperty]
        public ObservableCollection<string> lstRoles = new();

        [ObservableProperty]
        public string sTituloPagina;

        [ObservableProperty]
        public bool bDesabilitarPass;

        [ObservableProperty]
        public bool bProcesoActualizacion;

        [ObservableProperty]
        public bool bProcesoRegistrar;

        // Campos a validar
        [ObservableProperty]
        private string sNombre = string.Empty;
        partial void OnSNombreChanged(string sNombreValor)
        {
            if (!string.IsNullOrWhiteSpace(sNombreValor))
            {
                _sNombreCampoUsuario = sNombreValor.Split(" ").First().ToUpper();
                SUsuario = $"{_sNombreCampoUsuario}.{_sApellidoPaternoCampoUsuario}";
            }
        }

        [ObservableProperty]
        private string sErrorNombre = string.Empty;

        [ObservableProperty]
        private string sApellidoPaterno = string.Empty;
        partial void OnSApellidoPaternoChanged(string sApellidoPeternoValor)
        {
            if (!string.IsNullOrWhiteSpace(sApellidoPeternoValor))
            {
                _sApellidoPaternoCampoUsuario = sApellidoPeternoValor.Split(" ").First().ToUpper();
                SUsuario = $"{_sNombreCampoUsuario}.{_sApellidoPaternoCampoUsuario}";
            }
        }

        [ObservableProperty]
        private string sErrorApellidoPaterno = string.Empty;

        [ObservableProperty]
        private string sApellidoMaterno = string.Empty;

        [ObservableProperty]
        private string sErrorApellidoMaterno = string.Empty;

        // Se selecciona en el Picker
        [ObservableProperty]
        private string sRol = string.Empty;

        // Usuario generado concatenando Nombre + "." + ApellidoPaterno
        [ObservableProperty]
        private string sUsuario = string.Empty;

        [ObservableProperty]
        private string sErrorUsuario = string.Empty;

        // Contraseña por defecto, se genera automáticamente
        [ObservableProperty]
        private string sPassword = string.Empty;

        [ObservableProperty]
        private string sErrorPassword = string.Empty;

        [ObservableProperty]
        private string sRolSeleccionado = string.Empty;

        // Constructor
        public DatosUsuarioViewModel(HttpApiService oHttpApiService, LocalDatabaseService oLocalDatabaseService, IPopupService popupService)
        {
            _oIPopupService = popupService;
            SPassword = "P@$$w0rd123";
            _localDatabaseService = oLocalDatabaseService;
            _httpApiService = oHttpApiService;
            LstRoles = new ObservableCollection<string>
                        {
                            "Administrador",
                            "Mesero",
                            "Cocina"
                        };
            SRolSeleccionado = LstRoles[0];
        }

        #region Navegación

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            try
            {
                if (query.TryGetValue("oUsuario", out var oUsuario) && oUsuario != null)
                {
                    oUsuarioActualizar = (Usuario)oUsuario;
                    EstablecerDatosUsuarioActualizar(oUsuarioActualizar);
                }
                else
                {
                    STituloPagina = "Alta Usuario";
                    BDesabilitarPass = true;
                    BProcesoActualizacion = false;
                    BProcesoRegistrar = true;
                }
            }
            catch (Exception ex)
            {
                MostrarError($"ApplyQueryAttributes: {ex.Message}");
            }
        }

        #endregion

        #region Registro de Usuario

        [RelayCommand]
        private async Task RegistrarAsync()
        {
            string sMensajeErrorProceso = string.Empty;
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }

                if (ValidarEntradas())
                {
                    int iRol = SRolSeleccionado switch
                    {
                        "Administrador" => 1,
                        "Mesero" => 2,
                        "Cocina" => 3,
                        _ => 0
                    };

                    var nuevoUsuario = new Usuario
                    {
                        sNombre = SNombre,
                        sApellidoPaterno = SApellidoPaterno,
                        sApellidoMaterno = SApellidoMaterno,
                        sUsuario = SUsuario.Replace(" ", ""),
                        sPassword = SPassword.Trim(),
                        iRol = iRol
                    };

                    HttpResponseMessage? oRespuestaHTTP = await _httpApiService.PostAsync("api/usuarios", nuevoUsuario, bRequiereToken: true);
                    if (oRespuestaHTTP != null)
                    {
                        var oRespuestaApi = await oRespuestaHTTP.Content.ReadFromJsonAsync<ApiRespuesta<Usuario>>();
                        if (oRespuestaHTTP.IsSuccessStatusCode)
                        {
                            if (oRespuestaApi != null && oRespuestaApi.bSuccess)
                            {
                                LimpiarCamposEntrada();
                                var mainPage = Application.Current?.Windows[0].Page;
                                if (mainPage != null)
                                {
                                    await mainPage.DisplayAlert("Alta Usuarios", "Usuario creado con éxito.", "OK");
                                }
                            }
                        }
                        else
                        {
                            sMensajeErrorProceso = oRespuestaApi?.Error?.sDetails ?? "Ha ocurrido un error inesperado.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sMensajeErrorProceso = $"{ex.Message}\n{ex.StackTrace}";
            }

            if (!string.IsNullOrWhiteSpace(sMensajeErrorProceso))
            {
                MostrarError($"ERROR RegistrarAsync: {sMensajeErrorProceso}");
            }
        }

        #endregion

        #region Validaciones y Limpieza

        private bool ValidarEntradas()
        {
            bool bResultado = true;
            try
            {
                LimpiarMsjError();

                // Validar nombre
                if (string.IsNullOrWhiteSpace(SNombre))
                {
                    SErrorNombre = "El campo nombre es obligatorio.";
                    bResultado = false;
                }
                else if (!EntryValidations.IsOnlyLetters(SNombre.Trim()) || SNombre.Length < 3)
                {
                    SErrorNombre = "Introduce un nombre válido.";
                    bResultado = false;
                }

                // Validar apellido paterno
                if (string.IsNullOrWhiteSpace(SApellidoPaterno))
                {
                    SErrorApellidoPaterno = "El campo apellido paterno es obligatorio.";
                    bResultado = false;
                }
                else if (!EntryValidations.IsOnlyLetters(SApellidoPaterno) || SApellidoPaterno.Length < 3)
                {
                    SErrorApellidoPaterno = "Introduce un apellido válido.";
                    bResultado = false;
                }

                // Validar apellido materno
                if (string.IsNullOrWhiteSpace(SApellidoMaterno))
                {
                    SErrorApellidoMaterno = "El campo apellido materno es obligatorio.";
                    bResultado = false;
                }
                else if (!EntryValidations.IsOnlyLetters(SApellidoMaterno))
                {
                    SErrorApellidoMaterno = "Introduce un apellido válido.";
                    bResultado = false;
                }

                // Validar usuario
                if (string.IsNullOrWhiteSpace(SUsuario))
                {
                    SErrorUsuario = "El campo usuario es obligatorio.";
                    bResultado = false;
                }
                else if (EntryValidations.IsValidUsuario(SUsuario) && SUsuario.Replace(".", "").Length < 3)
                {
                    SErrorUsuario = "El nombre de usuario debe contener al menos 3 letras.";
                    bResultado = false;
                }

                // Validar password
                if (!string.IsNullOrWhiteSpace(SPassword))
                {
                    if (SPassword.Length <= 7)
                    {
                        SErrorPassword = "El campo debe contener al menos 8 caracteres.";
                        bResultado = false;
                    }
                }
            }
            catch (Exception ex)
            {
                bResultado = false;
                MostrarError($"ValidarEntradas: {ex.Message}");
            }

            return bResultado;
        }

        private void LimpiarMsjError()
        {
            try
            {
                SErrorNombre = string.Empty;
                SErrorApellidoPaterno = string.Empty;
                SErrorApellidoMaterno = string.Empty;
                SErrorUsuario = string.Empty;
                SErrorPassword = string.Empty;
            }
            catch (Exception ex)
            {
                MostrarError($"LimpiarMsjError: {ex.Message}");
            }
        }

        private void LimpiarCamposEntrada()
        {
            SNombre = string.Empty;
            SApellidoPaterno = string.Empty;
            SApellidoMaterno = string.Empty;
            SUsuario = string.Empty;
            SRolSeleccionado = LstRoles[0];
        }

        #endregion

        #region Actualización de Usuario

        private void EstablecerDatosUsuarioActualizar(Usuario oUsuario)
        {
            try
            {
                STituloPagina = "Actualizar Usuario";
                SNombre = oUsuario.sNombre;
                SApellidoPaterno = oUsuario.sApellidoPaterno;
                SApellidoMaterno = oUsuario.sApellidoMaterno;
                SUsuario = oUsuario.sUsuario;
                SRolSeleccionado = LstRoles[oUsuario.iRol - 1];
                SPassword = "";
                BDesabilitarPass = false;
                BProcesoActualizacion = true;
                _iIdMongoDB = oUsuario.sIdMongo;
            }
            catch (Exception ex)
            {
                MostrarError($"EstablecerDatosUsuarioActualizar: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task Actualizar()
        {
            string sMensajeErrorProceso = string.Empty;
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }

                if (ValidarEntradas())
                {
                    int iRol = SRolSeleccionado switch
                    {
                        "Administrador" => 1,
                        "Mesero" => 2,
                        "Cocina" => 3,
                        _ => 0
                    };

                    Usuario oDatosUsuarioActualizados = string.IsNullOrWhiteSpace(SPassword)
                        ? new Usuario
                        {
                            sNombre = SNombre,
                            sApellidoPaterno = SApellidoPaterno,
                            sApellidoMaterno = SApellidoMaterno,
                            sUsuario = SUsuario,
                            iRol = iRol
                        }
                        : new Usuario
                        {
                            sNombre = SNombre,
                            sApellidoPaterno = SApellidoPaterno,
                            sApellidoMaterno = SApellidoMaterno,
                            sUsuario = SUsuario,
                            sPassword = SPassword,
                            iRol = iRol
                        };

                    HttpResponseMessage? oRespuestaHTTP = await _httpApiService.PutAsync($"api/usuarios/{_iIdMongoDB}", oDatosUsuarioActualizados, bRequiereToken: true);
                    if (oRespuestaHTTP != null)
                    {
                        var oRespuestaApi = await oRespuestaHTTP.Content.ReadFromJsonAsync<ApiRespuesta<Usuario>>();
                        if (oRespuestaHTTP.IsSuccessStatusCode)
                        {
                            if (oRespuestaApi != null && oRespuestaApi.bSuccess)
                            {
                                var mainPage = Application.Current?.Windows[0].Page;
                                if (mainPage != null)
                                {
                                    await mainPage.DisplayAlert("Actualizado", "Usuario actualizado con éxito.", "OK");
                                }
                            }
                        }
                        else
                        {
                            sMensajeErrorProceso = oRespuestaApi?.Error?.sDetails ?? "Ha ocurrido un error inesperado.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sMensajeErrorProceso = $"{ex.Message}\n{ex.StackTrace}";
            }

            if (!string.IsNullOrWhiteSpace(sMensajeErrorProceso))
            {
                MostrarError($"ERROR: {sMensajeErrorProceso}");
            }
        }

        #endregion

        /// <summary>
        /// Muestra un mensaje de error en un diálogo.
        /// </summary>
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
            catch
            {
                // Si falla el diálogo, se puede registrar el error en un log
            }
        }
    }
}

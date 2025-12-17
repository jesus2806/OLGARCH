using AppGestorVentas.Models;
using AppGestorVentas.Services;
using AppGestorVentas.ViewModels.Popup;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace AppGestorVentas.ViewModels.UsuarioViewModels
{
    public partial class AdministracionUsuariosViewModel : ObservableObject
    {
        private readonly HttpApiService _httpApiService;
        private readonly LocalDatabaseService _localDatabaseService;
        private readonly IPopupService _oIPopupService;

        private const int PageSize = 5;
        private bool _isNoMoreData;
        private bool _isSearching;
        private int _currentPage;

        [ObservableProperty]
        private ObservableCollection<Usuario> oUsuarios = new();

        [ObservableProperty]
        private string sTextoBusqueda = string.Empty;

        [ObservableProperty]
        private string sNumeroPaginaActual = string.Empty;

        [ObservableProperty]
        private bool bNohayResultados = false;

        #region CONSTRUCTOR

        public AdministracionUsuariosViewModel(HttpApiService oHttpApiService,
                                                LocalDatabaseService localDatabaseService,
                                                IPopupService oPopupService)
        {
            _httpApiService = oHttpApiService;
            _localDatabaseService = localDatabaseService;
            _oIPopupService = oPopupService;
            BNohayResultados = false;
        }

        #endregion

        #region CARGA INICIAL DESDE API

        /// <summary>
        /// Llama la API para obtener la lista de usuarios, los guarda localmente, 
        /// y luego carga la primera página (página 1).
        /// </summary>
        public async Task ObtenerListadoUsuariosAPI()
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                return;
            }

            try
            {
                // Mostramos un Popup de "cargando..."
                await _oIPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
                {
                    try
                    {
                        // Limpiar la lista antes de volver a cargar
                        OUsuarios.Clear();
                        _currentPage = 1;

                        HttpResponseMessage? oRespuestaHTTP = await _httpApiService.GetAsync("api/usuarios", bRequiereToken: true);

                        if (oRespuestaHTTP != null && oRespuestaHTTP.IsSuccessStatusCode)
                        {
                            var oListadoUsuarios = await oRespuestaHTTP.Content.ReadFromJsonAsync<ApiRespuesta<Usuario>>();

                            if (oListadoUsuarios != null && oListadoUsuarios.bSuccess)
                            {
                                if (oListadoUsuarios.lData != null && oListadoUsuarios.lData.Count > 0)
                                {
                                    // Crear la tabla si no existe o limpiar la existente
                                    if (!await _localDatabaseService.TableExistsAsync<Usuario>())
                                    {
                                        await _localDatabaseService.CreateTableAsync<Usuario>();
                                    }
                                    else
                                    {
                                        await _localDatabaseService.DeleteAllRecordsAsync<Usuario>();
                                    }

                                    // Insertamos cada usuario y capturamos errores individualmente
                                    foreach (Usuario oUsuario in oListadoUsuarios.lData)
                                    {
                                        try
                                        {
                                            await InsertarUsuarioSQLite(oUsuario);
                                        }
                                        catch (Exception ex)
                                        {
                                            // Se muestra error pero se continúa con los demás
                                            throw new Exception($"Error al insertar el usuario {oUsuario.sIdMongo}: {ex.Message}");
                                        }
                                    }

                                    // Cargamos la primera página desde la base local
                                    await LoadUsuariosFromDatabaseAsync();
                                }
                            }
                        }
                        else
                        {
                            MostrarError("Error en la respuesta del servidor al obtener usuarios.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MostrarError($"ObtenerListadoUsuariosAPI: {ex.Message}\n{ex.StackTrace}");
                    }
                    finally
                    {
                        SNumeroPaginaActual = $"Página 1";
                        await vm.Cerrar();
                    }
                });
            }
            catch (Exception ex)
            {
                MostrarError($"ObtenerListadoUsuariosAPI: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Inserta un usuario en la base local.
        /// </summary>
        public async Task InsertarUsuarioSQLite(Usuario oUsuario)
        {
            try
            {
                await _localDatabaseService.SaveItemAsync(oUsuario);
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR InsertarUsuarioSQLite: {ex.Message}", ex);
            }
        }

        #endregion

        #region PAGINACIÓN

        /// <summary>
        /// Método que se llama al presionar el botón "Siguiente".
        /// Verifica primero si hay datos para la "próxima página".
        /// </summary>
        [RelayCommand]
        public async Task OnPaginaSiguiente()
        {
            try
            {
                if (_isNoMoreData)
                    return;

                int nextOffset = _currentPage * PageSize;
                var queryResult = _isSearching
                    ? BuildSearchQuery(nextOffset)
                    : BuildPaginationQuery(nextOffset);

                var nextPageData = await _localDatabaseService.GetItemsAsync<Usuario>(queryResult.query, queryResult.parameters);

                if (nextPageData is null || nextPageData.Count == 0)
                {
                    _isNoMoreData = true;
                    return;
                }

                _currentPage++;
                OUsuarios.Clear();

                foreach (var usuario in nextPageData)
                    OUsuarios.Add(usuario);

                if (nextPageData.Count < PageSize)
                    _isNoMoreData = true;

                SNumeroPaginaActual = $"Página {_currentPage}";
            }
            catch (Exception ex)
            {
                MostrarError($"OnPaginaSiguiente: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Retrocede una página si no estamos en la primera.
        /// </summary>
        [RelayCommand]
        public async Task OnPaginaAnterior()
        {
            try
            {
                if (_currentPage <= 1)
                    return;

                _currentPage--;
                _isNoMoreData = false;

                await LoadUsuariosFromDatabaseAsync();
                SNumeroPaginaActual = $"Página {_currentPage}";
            }
            catch (Exception ex)
            {
                MostrarError($"OnPaginaAnterior: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Carga la página actual desde la base de datos local.
        /// </summary>
        private async Task LoadUsuariosFromDatabaseAsync()
        {
            try
            {
                var queryResult = _isSearching
                    ? BuildSearchQuery((_currentPage - 1) * PageSize)
                    : BuildPaginationQuery((_currentPage - 1) * PageSize);

                var lstUsuariosObtenidos = await _localDatabaseService.GetItemsAsync<Usuario>(queryResult.query, queryResult.parameters);

                OUsuarios.Clear();

                if (lstUsuariosObtenidos != null && lstUsuariosObtenidos.Count > 0)
                {
                    foreach (var usuario in lstUsuariosObtenidos)
                    {
                        OUsuarios.Add(usuario);
                    }

                    _isNoMoreData = lstUsuariosObtenidos.Count < PageSize;
                }
                else
                {
                    _isNoMoreData = true;
                }

                BNohayResultados = OUsuarios.Count == 0;
            }
            catch (Exception ex)
            {
                MostrarError($"LoadUsuariosFromDatabaseAsync: {ex.Message}\n{ex.StackTrace}");
            }
        }

        #endregion

        #region BÚSQUEDA

        /// <summary>
        /// Ejecuta la búsqueda al presionar Enter o llamar al comando.
        /// </summary>
        [RelayCommand]
        public async Task OnBuscar()
        {
            try
            {
                _isSearching = !string.IsNullOrWhiteSpace(STextoBusqueda);
                _currentPage = 1;
                _isNoMoreData = false;

                await LoadUsuariosFromDatabaseAsync();
                SNumeroPaginaActual = $"Página {_currentPage}";
            }
            catch (Exception ex)
            {
                MostrarError($"OnBuscar: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Limpia el texto de búsqueda y recarga la paginación en modo normal.
        /// </summary>
        [RelayCommand]
        public async Task OnLimpiarBusqueda()
        {
            try
            {
                _isSearching = false;
                STextoBusqueda = string.Empty;
                _currentPage = 1;
                _isNoMoreData = false;

                await LoadUsuariosFromDatabaseAsync();
                SNumeroPaginaActual = $"Página {_currentPage}";
            }
            catch (Exception ex)
            {
                MostrarError($"OnLimpiarBusqueda: {ex.Message}\n{ex.StackTrace}");
            }
        }

        #endregion

        #region QUERIES

        /// <summary>
        /// Query normal paginada (excluye un usuario en particular).
        /// </summary>
        private (string query, object[] parameters) BuildPaginationQuery(int offset)
        {
            string query = @"
                SELECT * 
                FROM tb_Usuario 
                WHERE sUsuario <> ? 
                LIMIT ? 
                OFFSET ?;
            ";
            object[] parameters = { "ADMIN.ADMIN", PageSize, offset };
            return (query, parameters);
        }

        /// <summary>
        /// Query de búsqueda paginada (filtra por sUsuario o sNombre).
        /// </summary>
        private (string query, object[] parameters) BuildSearchQuery(int offset)
        {
            string searchValue = $"%{STextoBusqueda}%";
            string query = @"
                SELECT * 
                FROM tb_Usuario 
                WHERE (sUsuario LIKE ? OR sNombre LIKE ?) 
                  AND sUsuario <> ? 
                LIMIT ? 
                OFFSET ?;
            ";
            object[] parameters = { searchValue, searchValue, "ADMIN.ADMIN", PageSize, offset };
            return (query, parameters);
        }

        #endregion

        #region COMANDOS (Agregar, Eliminar, Actualizar)

        /// <summary>
        /// Navega a la vista para agregar un nuevo usuario.
        /// </summary>
        [RelayCommand]
        public async Task AgregarUsuario()
        {
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }

                await Shell.Current.GoToAsync("datosUsuarios");
            }
            catch (Exception ex)
            {
                MostrarError($"AgregarUsuario: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Lanza un Popup de confirmación para eliminar usuario.
        /// Si el usuario confirma, se llama a la API y se elimina de la DB local.
        /// </summary>
        [RelayCommand]
        public async Task EliminarUsuario(string sIDMongoDB)
        {
            try
            {
                var mainPage = Application.Current?.Windows[0].Page;
                bool confirmar = false;
                if (mainPage != null)
                {
                    confirmar = await mainPage.DisplayAlert(
                        "Confirmar Eliminación",
                        "¿Estás seguro de que deseas eliminar este usuario?",
                        "Sí",
                        "No");
                }

                if (confirmar)
                {
                    await EliminarUsuarioApi(sIDMongoDB);
                }
            }
            catch (Exception ex)
            {
                MostrarError($"EliminarUsuario: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task EliminarUsuarioApi(string sIDMongoDB)
        {
            string sMensajeErrorProceso = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(sIDMongoDB))
                    return;

                await _oIPopupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
                {
                    try
                    {
                        HttpResponseMessage? oRespuestaHTTP = await _httpApiService.DeleteAsync($"api/usuarios/{sIDMongoDB}", bRequiereToken: true);

                        if (oRespuestaHTTP != null)
                        {
                            var oListadoUsuarios = await oRespuestaHTTP.Content.ReadFromJsonAsync<ApiRespuesta<Usuario>>();

                            if (oRespuestaHTTP.IsSuccessStatusCode && oListadoUsuarios != null)
                            {
                                if (oListadoUsuarios.bSuccess && oListadoUsuarios.lData != null)
                                {
                                    foreach (Usuario oUsuario in oListadoUsuarios.lData)
                                    {
                                        await _localDatabaseService.DeleteRecordsAsync<Usuario>("sIdMongo = ?", sIDMongoDB);
                                        await OnLimpiarBusqueda();
                                    }
                                }
                                else
                                {
                                    sMensajeErrorProceso = oListadoUsuarios?.Error?.sDetails
                                        ?? "Error desconocido al eliminar.";
                                }
                            }
                            else
                            {
                                sMensajeErrorProceso = "Error en la respuesta del servidor al eliminar el usuario.";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        sMensajeErrorProceso = $"{ex.Message}\n{ex.StackTrace}";
                    }
                    finally
                    {
                        await vm.Cerrar();
                    }
                });
            }
            catch (Exception ex)
            {
                sMensajeErrorProceso = $"{ex.Message}\n{ex.StackTrace}";
            }

            if (!string.IsNullOrWhiteSpace(sMensajeErrorProceso))
            {
                MostrarError($"ERROR al eliminar el usuario: {sMensajeErrorProceso}");
            }
        }

        /// <summary>
        /// Navega a la vista para actualizar el usuario.
        /// </summary>
        [RelayCommand]
        public async Task ActualizarUsuario(Usuario usuarioSeleccionado)
        {
            try
            {
                await Shell.Current.GoToAsync("datosUsuarios", new Dictionary<string, object>
                {
                    { "oUsuario", usuarioSeleccionado },
                });
            }
            catch (Exception ex)
            {
                MostrarError($"ActualizarUsuario: {ex.Message}\n{ex.StackTrace}");
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
                // En caso de que falle el diálogo, se puede registrar el error
            }
        }
    }
}

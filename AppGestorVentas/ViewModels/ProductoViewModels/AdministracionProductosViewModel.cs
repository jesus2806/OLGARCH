using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppGestorVentas.Models;
using AppGestorVentas.Services;
using AppGestorVentas.ViewModels.Popup;
using CommunityToolkit.Maui.Core;

namespace AppGestorVentas.ViewModels.ProductoViewModels
{
    public partial class AdministracionProductosViewModel : ObservableObject
    {
        private readonly HttpApiService _httpApiService;
        private readonly LocalDatabaseService _localDatabaseService;
        private readonly IPopupService _popupService;

        // Instancias de sección para cada tipo de producto
        public ProductoSectionViewModel PlatillosVM { get; }
        public ProductoSectionViewModel BebidasVM { get; }
        public ProductoSectionViewModel ExtrasVM { get; }

        public AdministracionProductosViewModel(HttpApiService httpApiService,
                                                LocalDatabaseService localDatabaseService,
                                                IPopupService popupService)
        {
            _httpApiService = httpApiService;
            _localDatabaseService = localDatabaseService;
            _popupService = popupService;

            // 1 = Platillos, 2 = Bebidas, 3 = Extras
            PlatillosVM = new ProductoSectionViewModel(1, _localDatabaseService);
            BebidasVM = new ProductoSectionViewModel(2, _localDatabaseService);
            ExtrasVM = new ProductoSectionViewModel(3, _localDatabaseService);
        }

        public async Task ObtenerListadoProductosAPI()
        {
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }
                // Muestra popup de carga
                await _popupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
                {
                    try
                    {
                        await _localDatabaseService.CreateTableAsync<Producto>();
                        await _localDatabaseService.CreateTableAsync<Variante>();
                        await _localDatabaseService.CreateTableAsync<Imagen>();

                        // Limpia registros locales
                        await _localDatabaseService.DeleteAllRecordsAsync<Producto>();
                        await _localDatabaseService.DeleteAllRecordsAsync<Variante>();
                        await _localDatabaseService.DeleteAllRecordsAsync<Imagen>();

                        // Llama a la API para obtener productos
                        var response = await _httpApiService.GetAsync("api/productos", true);
                        if (response != null && response.IsSuccessStatusCode)
                        {
                            var apiRespuesta = await response.Content.ReadFromJsonAsync<ApiRespuesta<Producto>>();
                            if (apiRespuesta != null && apiRespuesta.bSuccess && apiRespuesta.lData != null)
                            {
                                if (apiRespuesta.lData.Count > 0)
                                {
                                    // Guarda cada producto y sus relaciones en la base local
                                    foreach (var producto in apiRespuesta.lData)
                                    {
                                        try
                                        {
                                            await _localDatabaseService.SaveItemAsync(producto);
                                            foreach (var variante in producto.aVariantes ?? new List<Variante>())
                                            {
                                                variante.sIdMongoDBProducto = producto.sIdMongo;
                                                await _localDatabaseService.SaveItemAsync(variante);
                                            }
                                            foreach (var imagen in producto.aImagenes ?? new List<Imagen>())
                                            {
                                                imagen.sIdMongoDBProducto = producto.sIdMongo;
                                                await _localDatabaseService.SaveItemAsync(imagen);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            // Se notifica el error por producto, pero el proceso continúa con los demás
                                            MostrarError($"Error al guardar producto {producto.sIdMongo}: {ex.Message}");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                MostrarError("No se encontraron productos en la respuesta de la API.");
                            }
                        }
                        else
                        {
                            MostrarError("La llamada a la API no fue exitosa.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MostrarError($"Error en ObtenerListadoProductosAPI: {ex.Message}");
                    }
                    finally
                    {
                        await vm.Cerrar();
                    }
                });

                // Cargar cada sección desde la base local
                await PlatillosVM.LoadProductosAsync();
                await BebidasVM.LoadProductosAsync();
                await ExtrasVM.LoadProductosAsync();
            }
            catch (Exception ex)
            {
                MostrarError($"Error general al obtener productos: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task EliminarProducto(Producto oProducto)
        {
            try
            {
                var mainPage = Application.Current?.Windows[0].Page;
                bool confirmar = false;
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
                    await EliminarProductoApi(oProducto);
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error al intentar eliminar el producto: {ex.Message}");
            }
        }

        private async Task EliminarProductoApi(Producto oProducto)
        {
            string sMensajeErrorProceso = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(oProducto.sIdMongo))
                    return;

                await _popupService.ShowPopupAsync<CargaGeneralPopupViewModel>(async vm =>
                {
                    try
                    {
                        var oResp = await _httpApiService.DeleteAsync($"api/productos/{oProducto.sIdMongo}", true);
                        if (oResp != null)
                        {
                            var oRespJson = await oResp.Content.ReadFromJsonAsync<ApiRespuesta<Producto>>();
                            if (oResp.IsSuccessStatusCode && oRespJson != null && oRespJson.bSuccess)
                            {
                                foreach (var prodEliminado in oRespJson.lData)
                                {
                                    // Eliminar de la base local
                                    await _localDatabaseService.DeleteRecordsAsync<Producto>("sIdMongo = ?", oProducto.sIdMongo);
                                }
                                // Recargar la lista según el tipo de producto
                                switch (oProducto.iTipoProducto)
                                {
                                    case 1:
                                        await PlatillosVM.LimpiarBusqueda();
                                        break;
                                    case 2:
                                        await BebidasVM.LimpiarBusqueda();
                                        break;
                                    case 3:
                                        await ExtrasVM.LimpiarBusqueda();
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                sMensajeErrorProceso = oRespJson?.Error?.sDetails ?? "Error desconocido al eliminar.";
                            }
                        }
                        else
                        {
                            sMensajeErrorProceso = "No se recibió respuesta de la API al intentar eliminar el producto.";
                        }
                    }
                    catch (Exception ex)
                    {
                        sMensajeErrorProceso = $"Excepción en EliminarProductoApi: {ex.Message}";
                    }
                    finally
                    {
                        await vm.Cerrar();
                    }
                });
            }
            catch (Exception ex)
            {
                sMensajeErrorProceso = $"Error en EliminarProductoApi: {ex.Message}";
            }

            if (!string.IsNullOrWhiteSpace(sMensajeErrorProceso))
            {
                MostrarError(sMensajeErrorProceso);
            }
        }

        [RelayCommand]
        public async Task ActualizarProducto(Producto productoSeleccionado)
        {
            try
            {
                // Navega a la vista de detalles/editar
                await Shell.Current.GoToAsync("datosProductos", new Dictionary<string, object>
                {
                    { "oProducto", productoSeleccionado }
                });
            }
            catch (Exception ex)
            {
                MostrarError($"Error al actualizar el producto: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task IrAgregarProducto()
        {
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    MostrarError("No tienes acceso a Internet. Revisa tu conexión y vuelve a intentarlo.");
                    return;
                }

                await Shell.Current.GoToAsync("datosProductos");
            }
            catch (Exception ex)
            {
                MostrarError($"Error al navegar para agregar producto: {ex.Message}");
            }
        }

        /// <summary>
        /// Muestra un mensaje de error mediante un DisplayAlert en la página principal.
        /// </summary>
        /// <param name="sMensaje">Mensaje de error a mostrar.</param>
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

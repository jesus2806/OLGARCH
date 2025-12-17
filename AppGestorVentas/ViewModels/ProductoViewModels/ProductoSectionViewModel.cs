using AppGestorVentas.Models;
using AppGestorVentas.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AppGestorVentas.ViewModels.ProductoViewModels
{
    public partial class ProductoSectionViewModel : ObservableObject
    {
        private readonly int productType;
        private readonly LocalDatabaseService _localDatabaseService;
        private const int PageSize = 5;
        private int currentPage = 1;
        private bool isNoMoreData;

        public ProductoSectionViewModel(int type, LocalDatabaseService localDatabaseService)
        {
            productType = type;
            _localDatabaseService = localDatabaseService;
            Productos = new ObservableCollection<Producto>();
            TextoBusqueda = string.Empty;
            NumeroPaginaActual = "Página 1";
        }

        [ObservableProperty]
        private ObservableCollection<Producto> productos;

        [ObservableProperty]
        private string textoBusqueda;

        [ObservableProperty]
        private string numeroPaginaActual;

        /// <summary>
        /// Muestra un mensaje de error utilizando un DisplayAlert en la página principal.
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

        /// <summary>
        /// Carga los productos filtrados por tipo, búsqueda y paginación.
        /// </summary>
        public async Task LoadProductosAsync()
        {
            try
            {
                // Solicitar un elemento extra para detectar si hay más datos.
                int limiteConsulta = PageSize + 1;
                string query;
                object[] parameters;
                if (!string.IsNullOrWhiteSpace(TextoBusqueda))
                {
                    query = "SELECT * FROM tb_Producto WHERE iTipoProducto = ? AND sNombre LIKE ? LIMIT ? OFFSET ?";
                    parameters = new object[] { productType, $"%{TextoBusqueda}%", limiteConsulta, (currentPage - 1) * PageSize };
                }
                else
                {
                    query = "SELECT * FROM tb_Producto WHERE iTipoProducto = ? LIMIT ? OFFSET ?";
                    parameters = new object[] { productType, limiteConsulta, (currentPage - 1) * PageSize };
                }

                var items = await _localDatabaseService.GetItemsAsync<Producto>(query, parameters);
                Productos.Clear();

                if (items != null && items.Count > 0)
                {
                    // Si se obtuvo más de PageSize, hay más datos. Removemos el extra.
                    if (items.Count > PageSize)
                    {
                        items.RemoveAt(items.Count - 1);
                        isNoMoreData = false;
                    }
                    else
                    {
                        isNoMoreData = true;
                    }

                    foreach (Producto producto in items)
                    {
                        try
                        {
                            // Carga imágenes y variantes para cada producto
                            var imagenes = await _localDatabaseService.GetItemsAsync<Imagen>(
                                "SELECT * FROM tb_Imagen WHERE sIdMongoDBProducto = ?", producto.sIdMongo);
                            producto.aImagenes = imagenes;
                            var variantes = await _localDatabaseService.GetItemsAsync<Variante>(
                                "SELECT * FROM tb_Variante WHERE sIdMongoDBProducto = ?", producto.sIdMongo);
                            producto.aVariantes = variantes;
                            Productos.Add(producto);
                        }
                        catch (Exception ex)
                        {
                            MostrarError($"Error al cargar detalles del producto {producto.sIdMongo}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    isNoMoreData = true;
                }
                NumeroPaginaActual = $"Página {currentPage}";
            }
            catch (Exception ex)
            {
                MostrarError($"Error al cargar productos: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task Buscar()
        {
            try
            {
                currentPage = 1;
                isNoMoreData = false;
                await LoadProductosAsync();
            }
            catch (Exception ex)
            {
                MostrarError($"Error al buscar productos: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task LimpiarBusqueda()
        {
            try
            {
                TextoBusqueda = string.Empty;
                currentPage = 1;
                isNoMoreData = false;
                await LoadProductosAsync();
            }
            catch (Exception ex)
            {
                MostrarError($"Error al limpiar la búsqueda: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task PaginaSiguiente()
        {
            try
            {
                if (isNoMoreData) return;
                currentPage++;
                await LoadProductosAsync();
            }
            catch (Exception ex)
            {
                MostrarError($"Error al cargar la página siguiente: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task PaginaAnterior()
        {
            try
            {
                if (currentPage <= 1) return;
                currentPage--;
                isNoMoreData = false;
                await LoadProductosAsync();
            }
            catch (Exception ex)
            {
                MostrarError($"Error al cargar la página anterior: {ex.Message}");
            }
        }
    }
}

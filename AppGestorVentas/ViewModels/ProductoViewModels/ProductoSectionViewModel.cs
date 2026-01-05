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

        private async void MostrarError(string sMensaje)
        {
            var mainPage = Application.Current?.Windows[0].Page;
            if (mainPage != null)
                await mainPage.DisplayAlert("Error", sMensaje, "OK");
        }

        /// <summary>
        /// Carga los productos filtrados por tipo, búsqueda y paginación.
        /// </summary>
        public async Task LoadProductosAsync()
        {
            try
            {
                int limiteConsulta = PageSize + 1; // pedir 1 extra para detectar siguiente página

                string query;
                object[] parameters;

                if (!string.IsNullOrWhiteSpace(TextoBusqueda))
                {
                    query = "SELECT * FROM tb_Producto WHERE iTipoProducto = ? AND sNombre LIKE ? LIMIT ? OFFSET ?";
                    parameters = new object[]
                    {
                        productType,
                        $"%{TextoBusqueda}%",
                        limiteConsulta,
                        (currentPage - 1) * PageSize
                    };
                }
                else
                {
                    query = "SELECT * FROM tb_Producto WHERE iTipoProducto = ? LIMIT ? OFFSET ?";
                    parameters = new object[]
                    {
                        productType,
                        limiteConsulta,
                        (currentPage - 1) * PageSize
                    };
                }

                var items = await _localDatabaseService.GetItemsAsync<Producto>(query, parameters);

                Productos.Clear();

                if (items != null && items.Count > 0)
                {
                    if (items.Count > PageSize)
                    {
                        items.RemoveAt(items.Count - 1);
                        isNoMoreData = false;
                    }
                    else
                    {
                        isNoMoreData = true;
                    }

                    foreach (var producto in items)
                    {
                        try
                        {
                            // ✅ IMPORTANTE: como aImagenes/aVariantes tienen [Ignore],
                            // al venir de SQLite pueden estar null: inicialízalas siempre.
                            producto.aImagenes ??= new List<Imagen>();
                            producto.aVariantes ??= new List<Variante>();

                            // ✅ Cargar imágenes relacionadas (usa object[] para parámetros)
                            var imagenes = await _localDatabaseService.GetItemsAsync<Imagen>(
                                "SELECT * FROM tb_Imagen WHERE sIdMongoDBProducto = ?",
                                new object[] { producto.sIdMongo }
                            );

                            producto.aImagenes = imagenes ?? new List<Imagen>();

                            // ✅ Cargar variantes relacionadas
                            var variantes = await _localDatabaseService.GetItemsAsync<Variante>(
                                "SELECT * FROM tb_Variante WHERE sIdMongoDBProducto = ?",
                                new object[] { producto.sIdMongo }
                            );

                            producto.aVariantes = variantes ?? new List<Variante>();

                            // ✅ Cargar ingredientes relacionados
                            var ingredientesLocal = await _localDatabaseService.GetItemsAsync<ProductoIngredienteLocal>(
                                "SELECT * FROM tb_ProductoIngrediente WHERE sIdMongoDBProducto = ?",
                                new object[] { producto.sIdMongo }
                            );

                            // Reconstruye aIngredientes (porque viene [Ignore] en Producto)
                            producto.aIngredientes ??= new List<ProductoIngrediente>();
                            producto.aIngredientes = (ingredientesLocal ?? new List<ProductoIngredienteLocal>())
                                .Select(x => new ProductoIngrediente
                                {
                                    sIdIngrediente = x.sIdIngrediente,
                                    iCantidadUso = x.iCantidadUso
                                })
                                .ToList();

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

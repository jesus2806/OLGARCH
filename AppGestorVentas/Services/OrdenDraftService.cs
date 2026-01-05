using AppGestorVentas.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace AppGestorVentas.Services
{
    /// <summary>
    /// Servicio que gestiona el borrador de una orden en edición.
    /// Todos los cambios se guardan localmente hasta que se confirmen.
    /// </summary>
    public class OrdenDraftService
    {
        #region PROPIEDADES

        private readonly LocalDatabaseService _localDb;
        private readonly HttpApiService _httpApi;

        /// <summary>
        /// Orden actual en edición
        /// </summary>
        public Orden? OrdenActual { get; private set; }

        /// <summary>
        /// Productos de la orden actual
        /// </summary>
        public ObservableCollection<OrdenProducto> Productos { get; private set; } = new();

        /// <summary>
        /// Indica si es una orden nueva (true) o existente (false)
        /// </summary>
        public bool EsOrdenNueva { get; private set; }

        /// <summary>
        /// Indica si hay cambios pendientes de guardar
        /// </summary>
        public bool TieneCambiosPendientes { get; private set; }

        /// <summary>
        /// Evento cuando cambian los productos
        /// </summary>
        public event EventHandler? OnProductosChanged;

        /// <summary>
        /// Evento cuando hay cambios pendientes
        /// </summary>
        public event EventHandler<bool>? OnCambiosPendientesChanged;

        #endregion

        #region CONSTRUCTOR

        public OrdenDraftService(LocalDatabaseService localDb, HttpApiService httpApi)
        {
            _localDb = localDb;
            _httpApi = httpApi;
        }

        #endregion

        #region INICIALIZACIÓN

        /// <summary>
        /// Inicializa un nuevo borrador para una orden NUEVA
        /// </summary>
        public async Task IniciarNuevaOrdenAsync(string identificador, int mesa, string mesero, string idMesero)
        {
            // Limpiar estado anterior
            await LimpiarBorradorAsync();

            OrdenActual = new Orden
            {
                sIdLocal = Guid.NewGuid().ToString(),
                sIdentificadorOrden = identificador,
                iMesa = mesa,
                iTipoOrden = 1, // Orden primaria
                sUsuarioMesero = mesero,
                sIdMongoDBMesero = idMesero,
                iEstatus = 0, // Pendiente
                dtFechaAlta = DateTime.UtcNow,
                bSincronizado = false,
                bTieneCambiosPendientes = true
            };

            EsOrdenNueva = true;
            TieneCambiosPendientes = true;

            // Guardar en SQLite (crear ambas tablas)
            await _localDb.CreateTableAsync<Orden>();
            await _localDb.CreateTableAsync<OrdenProducto>();
            await _localDb.SaveItemAsync(OrdenActual);

            NotificarCambios();
        }

        /// <summary>
        /// Carga una orden EXISTENTE del backend para editarla
        /// </summary>
        public async Task CargarOrdenExistenteAsync(string idOrdenMongoDB)
        {
            // Limpiar estado anterior
            await LimpiarBorradorAsync();

            try
            {
                // Obtener orden del backend
                var response = await _httpApi.GetAsync($"api/orden/{idOrdenMongoDB}/resumen");

                if (response != null && response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<Orden>>();

                    if (apiResponse?.bSuccess == true && apiResponse.lData?.Count > 0)
                    {
                        var ordenBackend = apiResponse.lData[0];

                        // Crear copia local
                        OrdenActual = new Orden
                        {
                            sIdLocal = Guid.NewGuid().ToString(),
                            sIdMongoDB = ordenBackend.sIdMongoDB,
                            sIdentificadorOrden = ordenBackend.sIdentificadorOrden,
                            iMesa = ordenBackend.iMesa,
                            iTipoOrden = ordenBackend.iTipoOrden,
                            iNumeroOrden = ordenBackend.iNumeroOrden,
                            sUsuarioMesero = ordenBackend.sUsuarioMesero,
                            sIdMongoDBMesero = ordenBackend.sIdMongoDBMesero,
                            iEstatus = ordenBackend.iEstatus,
                            iTipoPago = ordenBackend.iTipoPago,
                            dtFechaAlta = ordenBackend.dtFechaAlta,
                            iTotalOrden = ordenBackend.iTotalOrden,
                            sIndicaciones = ordenBackend.sIndicaciones,
                            bSincronizado = true,
                            bTieneCambiosPendientes = false
                        };

                        // Cargar productos
                        if (ordenBackend.aProductos != null)
                        {
                            foreach (var prod in ordenBackend.aProductos)
                            {
                                var productoLocal = new OrdenProducto
                                {
                                    sIdLocal = Guid.NewGuid().ToString(),
                                    sIdMongo = prod.sIdMongo,
                                    sIdOrdenLocal = OrdenActual.sIdLocal,
                                    sIdOrdenMongoDB = OrdenActual.sIdMongoDB,
                                    sIdProductoMongoDB = prod.sIdProductoMongoDB,
                                    sNombre = prod.sNombre,
                                    iCostoReal = prod.iCostoReal,
                                    iCostoPublico = prod.iCostoPublico,
                                    sURLImagen = prod.sURLImagen,
                                    sIndicaciones = prod.sIndicaciones,
                                    iIndexVarianteSeleccionada = prod.iIndexVarianteSeleccionada,
                                    aVariantes = prod.aVariantes ?? new List<Variante>(),
                                    iCantidad = prod.iCantidad,
                                    iTipoProducto = prod.iTipoProducto,
                                    aExtras = prod.aExtras ?? new List<ExtraOrdenProducto>(),
                                    aConsumos = prod.aConsumos ?? new List<Consumo>(),
                                    bTieneExtras = prod.bTieneExtras,
                                    bSincronizado = true,
                                    bTieneCambiosPendientes = false
                                };

                                // Asegurar que los consumos tengan IDs locales
                                foreach (var consumo in productoLocal.aConsumos)
                                {
                                    if (string.IsNullOrEmpty(consumo.sIdLocal))
                                        consumo.sIdLocal = Guid.NewGuid().ToString();
                                    consumo.sIdOrdenProductoLocal = productoLocal.sIdLocal;
                                }

                                Productos.Add(productoLocal);
                            }
                        }

                        EsOrdenNueva = false;
                        TieneCambiosPendientes = false;

                        // Guardar en SQLite
                        await _localDb.CreateTableAsync<Orden>();
                        await _localDb.CreateTableAsync<OrdenProducto>();
                        await _localDb.SaveItemAsync(OrdenActual);

                        foreach (var prod in Productos)
                        {
                            prod.SerializarListas();
                            await _localDb.SaveItemAsync(prod);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cargar orden: {ex.Message}", ex);
            }

            NotificarCambios();
        }

        /// <summary>
        /// Limpia el borrador actual
        /// </summary>
        public async Task LimpiarBorradorAsync()
        {
            OrdenActual = null;
            Productos.Clear();
            EsOrdenNueva = false;
            TieneCambiosPendientes = false;

            // Limpiar tablas locales de borradores
            try
            {
                await _localDb.DeleteAllRecordsAsync<Orden>();
                await _localDb.DeleteAllRecordsAsync<OrdenProducto>();
            }
            catch { /* Ignorar si las tablas no existen */ }

            OnProductosChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region GESTIÓN DE PRODUCTOS

        /// <summary>
        /// Agrega un nuevo producto al borrador o incrementa cantidad si ya existe
        /// </summary>
        public async Task<OrdenProducto> AgregarProductoAsync(
            Producto producto,
            Variante variante,
            string indicaciones,
            List<ExtraOrdenProducto>? extras = null)
        {
            if (OrdenActual == null)
                throw new InvalidOperationException("No hay orden activa. Inicie una nueva orden primero.");

            int indexVariante = producto.aVariantes?.IndexOf(variante) ?? 0;

            // Buscar si ya existe un producto igual
            var productoExistente = Productos.FirstOrDefault(p =>
                p.sNombre == producto.sNombre &&
                p.iIndexVarianteSeleccionada == indexVariante &&
                (string.IsNullOrEmpty(indicaciones) || p.sIndicaciones == indicaciones));

            if (productoExistente != null)
            {
                // Incrementar cantidad
                productoExistente.iCantidad++;

                // IMPORTANTE: Leer lista, modificar, y REASIGNAR
                var consumos = productoExistente.aConsumos;
                consumos.Add(new Consumo
                {
                    sIdLocal = Guid.NewGuid().ToString(),
                    sIdOrdenProductoLocal = productoExistente.sIdLocal,
                    iIndex = productoExistente.iCantidad,
                    aExtras = new List<ExtraConsumo>()
                });
                productoExistente.aConsumos = consumos; // Forzar serialización

                productoExistente.bTieneCambiosPendientes = true;
                await _localDb.UpdateItemAsync(productoExistente);

                MarcarCambiosPendientes();
                OnProductosChanged?.Invoke(this, EventArgs.Empty);

                return productoExistente;
            }

            // Crear nuevo producto
            var nuevoProducto = new OrdenProducto
            {
                sIdLocal = Guid.NewGuid().ToString(),
                sIdOrdenLocal = OrdenActual.sIdLocal,
                sIdOrdenMongoDB = OrdenActual.sIdMongoDB,
                sIdProductoMongoDB = producto.sIdMongo ?? string.Empty,
                sNombre = producto.sNombre,
                iCostoReal = producto.iCostoReal,
                iCostoPublico = producto.iCostoPublico,
                sURLImagen = producto.aImagenes?.FirstOrDefault()?.sURLImagen ?? string.Empty,
                sIndicaciones = indicaciones,
                iIndexVarianteSeleccionada = indexVariante,
                aVariantes = producto.aVariantes ?? new List<Variante>(),
                iCantidad = 1,
                iTipoProducto = producto.iTipoProducto,
                aExtras = extras ?? new List<ExtraOrdenProducto>(),
                bSincronizado = false,
                bTieneCambiosPendientes = true
            };

            // Inicializar consumo (asignar directamente serializa)
            nuevoProducto.aConsumos = new List<Consumo>
            {
                new Consumo
                {
                    sIdLocal = Guid.NewGuid().ToString(),
                    sIdOrdenProductoLocal = nuevoProducto.sIdLocal,
                    iIndex = 1,
                    aExtras = new List<ExtraConsumo>()
                }
            };

            Productos.Add(nuevoProducto);

            await _localDb.CreateTableAsync<OrdenProducto>();
            await _localDb.SaveItemAsync(nuevoProducto);

            MarcarCambiosPendientes();
            OnProductosChanged?.Invoke(this, EventArgs.Empty);

            return nuevoProducto;
        }

        /// <summary>
        /// Actualiza un producto existente en el borrador
        /// </summary>
        public async Task ActualizarProductoAsync(
            string idLocalProducto,
            Variante variante,
            string indicaciones,
            List<ExtraOrdenProducto>? extras = null)
        {
            var producto = Productos.FirstOrDefault(p => p.sIdLocal == idLocalProducto);
            if (producto == null)
                throw new InvalidOperationException("Producto no encontrado.");

            producto.iIndexVarianteSeleccionada = producto.aVariantes?.IndexOf(variante) ?? 0;
            producto.sIndicaciones = indicaciones;
            
            if (extras != null)
                producto.aExtras = extras;

            producto.bTieneCambiosPendientes = true;

            // Serializar listas y actualizar en SQLite
            producto.SerializarListas();
            await _localDb.UpdateItemAsync(producto);

            MarcarCambiosPendientes();
            OnProductosChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Elimina un producto del borrador
        /// </summary>
        public async Task EliminarProductoAsync(string idLocalProducto)
        {
            var producto = Productos.FirstOrDefault(p => p.sIdLocal == idLocalProducto);
            if (producto == null) return;

            Productos.Remove(producto);

            // Eliminar de SQLite
            await _localDb.DeleteRecordsAsync<OrdenProducto>("sIdLocal = ?", idLocalProducto);

            MarcarCambiosPendientes();
            OnProductosChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Actualiza la cantidad de un producto
        /// </summary>
        public async Task ActualizarCantidadProductoAsync(string idLocalProducto, int nuevaCantidad)
        {
            var producto = Productos.FirstOrDefault(p => p.sIdLocal == idLocalProducto);
            if (producto == null) return;

            if (nuevaCantidad <= 0)
            {
                await EliminarProductoAsync(idLocalProducto);
                return;
            }

            var cantidadAnterior = producto.iCantidad;
            producto.iCantidad = nuevaCantidad;

            // IMPORTANTE: Leer la lista UNA VEZ, modificarla, y reasignar al final
            var consumos = producto.aConsumos;

            if (nuevaCantidad > cantidadAnterior)
            {
                // Agregar nuevos consumos
                for (int i = cantidadAnterior + 1; i <= nuevaCantidad; i++)
                {
                    consumos.Add(new Consumo
                    {
                        sIdLocal = Guid.NewGuid().ToString(),
                        sIdOrdenProductoLocal = producto.sIdLocal,
                        iIndex = i,
                        aExtras = new List<ExtraConsumo>()
                    });
                }
            }
            else if (nuevaCantidad < cantidadAnterior)
            {
                // Eliminar consumos excedentes
                consumos = consumos
                    .OrderBy(c => c.iIndex)
                    .Take(nuevaCantidad)
                    .ToList();

                // Re-indexar
                for (int i = 0; i < consumos.Count; i++)
                {
                    consumos[i].iIndex = i + 1;
                }
            }

            // REASIGNAR para forzar serialización
            producto.aConsumos = consumos;
            producto.bTieneCambiosPendientes = true;

            await _localDb.UpdateItemAsync(producto);

            MarcarCambiosPendientes();
            OnProductosChanged?.Invoke(this, EventArgs.Empty);
        }


        public async Task ActualizarEstatusOrdenAsync(int iEstatus)
        {
            if (OrdenActual == null) return;    

            OrdenActual.iEstatus = iEstatus;

            await _localDb.UpdateItemAsync(OrdenActual);
        }

        #endregion

            #region GESTIÓN DE CONSUMOS Y EXTRAS

            /// <summary>
            /// Obtiene un producto por su ID local
            /// </summary>
        public OrdenProducto? ObtenerProducto(string idLocalProducto)
        {
            return Productos.FirstOrDefault(p => p.sIdLocal == idLocalProducto);
        }

        /// <summary>
        /// Obtiene un producto por su ID de MongoDB
        /// </summary>
        public OrdenProducto? ObtenerProductoPorIdMongo(string idMongo)
        {
            return Productos.FirstOrDefault(p => p.sIdMongo == idMongo || p.sIdLocal == idMongo);
        }

        /// <summary>
        /// Agrega un extra a consumos específicos de un producto
        /// </summary>
        public async Task AgregarExtraAConsumosAsync(
            string idProducto,
            Extra extra,
            List<int> indicesConsumos)
        {
            var producto = ObtenerProductoPorIdMongo(idProducto) ?? ObtenerProducto(idProducto);
            if (producto == null)
                throw new InvalidOperationException("Producto no encontrado.");

            // Leer lista UNA VEZ
            var consumos = producto.aConsumos;

            foreach (var index in indicesConsumos)
            {
                var consumo = consumos.FirstOrDefault(c => c.iIndex == index);
                if (consumo != null)
                {
                    consumo.aExtras ??= new List<ExtraConsumo>();

                    // Evitar duplicados
                    if (!consumo.aExtras.Any(e => e.sNombre == extra.sNombre))
                    {
                        consumo.aExtras.Add(new ExtraConsumo
                        {
                            sIdLocal = Guid.NewGuid().ToString(),
                            sIdConsumoLocal = consumo.sIdLocal,
                            sIdExtra = extra.sIdMongo,
                            sNombre = extra.sNombre,
                            iCostoReal = extra.iCostoReal,
                            iCostoPublico = extra.iCostoPublico,
                            sURLImagen = extra.sURLImagen
                        });
                    }
                }
            }

            // REASIGNAR para forzar serialización
            producto.aConsumos = consumos;
            producto.bTieneExtras = consumos.Any(c => c.aExtras?.Count > 0);
            producto.bTieneCambiosPendientes = true;

            await _localDb.UpdateItemAsync(producto);

            MarcarCambiosPendientes();
            OnProductosChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Elimina un extra de un consumo específico
        /// </summary>
        public async Task EliminarExtraDeConsumoAsync(
            string idProducto,
            int indexConsumo,
            string idExtra)
        {
            var producto = ObtenerProductoPorIdMongo(idProducto) ?? ObtenerProducto(idProducto);
            if (producto == null) return;

            var consumos = producto.aConsumos;
            var consumo = consumos.FirstOrDefault(c => c.iIndex == indexConsumo);
            if (consumo?.aExtras == null) return;

            var extraToRemove = consumo.aExtras.FirstOrDefault(e =>
                e.sIdExtra == idExtra || e.sIdLocal == idExtra || e.sIdMongo == idExtra);

            if (extraToRemove != null)
            {
                consumo.aExtras.Remove(extraToRemove);
            }

            // Forzar serialización
            producto.aConsumos = consumos;
            producto.bTieneExtras = consumos.Any(c => c.aExtras?.Count > 0);
            producto.bTieneCambiosPendientes = true;

            await _localDb.UpdateItemAsync(producto);

            MarcarCambiosPendientes();
            OnProductosChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Elimina un consumo específico de un producto
        /// </summary>
        public async Task EliminarConsumoAsync(string idProducto, int indexConsumo)
        {
            var producto = ObtenerProductoPorIdMongo(idProducto) ?? ObtenerProducto(idProducto);
            if (producto == null) return;

            // Leer lista UNA VEZ
            var consumos = producto.aConsumos;
            var consumo = consumos.FirstOrDefault(c => c.iIndex == indexConsumo);

            if (consumo != null)
            {
                consumos.Remove(consumo);
                producto.iCantidad = consumos.Count;

                // Re-indexar
                for (int i = 0; i < consumos.Count; i++)
                {
                    consumos[i].iIndex = i + 1;
                }
            }

            // Si no quedan consumos, eliminar el producto
            if (consumos.Count == 0)
            {
                await EliminarProductoAsync(producto.sIdLocal);
                return;
            }

            // REASIGNAR para forzar serialización
            producto.aConsumos = consumos;
            producto.bTieneExtras = consumos.Any(c => c.aExtras?.Count > 0);
            producto.bTieneCambiosPendientes = true;

            await _localDb.UpdateItemAsync(producto);

            MarcarCambiosPendientes();
            OnProductosChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region INDICACIONES

        /// <summary>
        /// Actualiza las indicaciones de la orden
        /// </summary>
        public async Task ActualizarIndicacionesAsync(string indicaciones)
        {
            if (OrdenActual == null) return;

            OrdenActual.sIndicaciones = indicaciones;
            OrdenActual.bTieneCambiosPendientes = true;

            await _localDb.UpdateItemAsync(OrdenActual);

            MarcarCambiosPendientes();
        }

        #endregion

        #region SINCRONIZACIÓN CON BACKEND

        /// <summary>
        /// Guarda todos los cambios en el backend (TOMAR ORDEN o GUARDAR CAMBIOS)
        /// </summary>
        public async Task<(bool exito, string mensaje)> GuardarEnBackendAsync()
        {
            if (OrdenActual == null)
                return (false, "No hay orden activa.");

            try
            {
                if (EsOrdenNueva)
                {
                    return await CrearOrdenEnBackendAsync();
                }
                else
                {
                    return await ActualizarOrdenEnBackendAsync();
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Crea una orden nueva en el backend con todos sus productos
        /// </summary>
        private async Task<(bool exito, string mensaje)> CrearOrdenEnBackendAsync()
        {
            try
            {
                // 1. Crear la orden
                var ordenData = new
                {
                    sIdentificadorOrden = OrdenActual!.sIdentificadorOrden,
                    iMesa = OrdenActual.iMesa,
                    iTipoOrden = OrdenActual.iTipoOrden,
                    sUsuarioMesero = OrdenActual.sUsuarioMesero,
                    sIdMongoDBMesero = OrdenActual.sIdMongoDBMesero,
                    sIndicaciones = OrdenActual.sIndicaciones ?? string.Empty
                };

                Console.WriteLine($"[DEBUG] Creando orden: Mesa {ordenData.iMesa}, Mesero: {ordenData.sUsuarioMesero}");

                var responseOrden = await _httpApi.PostAsync("api/nueva-orden", ordenData);

                if (responseOrden == null)
                {
                    return (false, "Error: No hubo respuesta del servidor al crear la orden.");
                }

                if (!responseOrden.IsSuccessStatusCode)
                {
                    var errorContent = await responseOrden.Content.ReadAsStringAsync();
                    Console.WriteLine($"[DEBUG] Error al crear orden: {responseOrden.StatusCode} - {errorContent}");
                    return (false, $"Error al crear orden: {errorContent}");
                }

                var apiResponseOrden = await responseOrden.Content.ReadFromJsonAsync<ApiRespuesta<Orden>>();

                if (apiResponseOrden?.bSuccess != true || apiResponseOrden.lData == null || apiResponseOrden.lData.Count == 0)
                {
                    return (false, "No se pudo obtener la orden creada del servidor.");
                }

                var ordenCreada = apiResponseOrden.lData[0];
                OrdenActual.sIdMongoDB = ordenCreada.sIdMongoDB;
                OrdenActual.iNumeroOrden = ordenCreada.iNumeroOrden;

                Console.WriteLine($"[DEBUG] Orden creada con ID: {OrdenActual.sIdMongoDB}, Número: {OrdenActual.iNumeroOrden}");
                Console.WriteLine($"[DEBUG] Productos a enviar: {Productos.Count}");

                // 2. Crear cada producto
                foreach (var producto in Productos)
                {
                    try
                    {
                        // Obtener consumos de forma segura
                        var consumosList = producto.aConsumos ?? new List<Consumo>();

                        Console.WriteLine($"[DEBUG] Enviando producto: {producto.sNombre}, Cantidad: {producto.iCantidad}, Consumos: {consumosList.Count}");

                        var productoData = new
                        {
                            sIdOrdenMongoDB = OrdenActual.sIdMongoDB,
                            sIdProductoMongoDB = producto.sIdProductoMongoDB,
                            sNombre = producto.sNombre,
                            iCostoReal = producto.iCostoReal,
                            iCostoPublico = producto.iCostoPublico,
                            sURLImagen = producto.sURLImagen ?? string.Empty,
                            sIndicaciones = producto.sIndicaciones ?? string.Empty,
                            iIndexVarianteSeleccionada = producto.iIndexVarianteSeleccionada,
                            aVariantes = producto.aVariantes ?? new List<Variante>(),
                            iCantidad = producto.iCantidad,
                            iTipoProducto = producto.iTipoProducto,
                            aExtras = (producto.aExtras ?? new List<ExtraOrdenProducto>()).Select(e => new
                            {
                                sNombre = e.sNombre,
                                iCostoReal = e.iCostoReal,
                                iCostoPublico = e.iCostoPublico,
                                sURLImagen = e.sURLImagen ?? string.Empty
                            }).ToList(),
                            aConsumos = consumosList.Select(c => new
                            {
                                iIndex = c.iIndex,
                                aExtras = (c.aExtras ?? new List<ExtraConsumo>()).Select(e => new
                                {
                                    sIdExtra = e.sIdExtra ?? string.Empty,
                                    sNombre = e.sNombre,
                                    iCostoReal = e.iCostoReal,
                                    iCostoPublico = e.iCostoPublico,
                                    sURLImagen = e.sURLImagen ?? string.Empty
                                }).ToList()
                            }).ToList()
                        };

                        var responseProducto = await _httpApi.PostAsync("api/orden-productos/", productoData);

                        if (responseProducto == null || !responseProducto.IsSuccessStatusCode)
                        {
                            var errorProd = responseProducto != null
                                ? await responseProducto.Content.ReadAsStringAsync()
                                : "Sin respuesta";
                            Console.WriteLine($"[DEBUG] Error al crear producto {producto.sNombre}: {errorProd}");
                            continue;
                        }

                        var apiResponseProducto = await responseProducto.Content.ReadFromJsonAsync<ApiRespuesta<OrdenProducto>>();
                        if (apiResponseProducto?.bSuccess == true && apiResponseProducto.lData?.Count > 0)
                        {
                            producto.sIdMongo = apiResponseProducto.lData[0].sIdMongo;
                            producto.sIdOrdenMongoDB = OrdenActual.sIdMongoDB;
                            producto.bSincronizado = true;
                            producto.bTieneCambiosPendientes = false;
                            Console.WriteLine($"[DEBUG] Producto creado con ID: {producto.sIdMongo}");
                        }
                    }
                    catch (Exception exProd)
                    {
                        Console.WriteLine($"[DEBUG] Excepción al crear producto {producto.sNombre}: {exProd.Message}");
                        continue;
                    }
                }

                // Marcar orden como sincronizada
                OrdenActual.bSincronizado = true;
                OrdenActual.bTieneCambiosPendientes = false;
                TieneCambiosPendientes = false;

                // Actualizar SQLite
                await _localDb.UpdateItemAsync(OrdenActual);

                return (true, $"Orden #{OrdenActual.iNumeroOrden} creada exitosamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Excepción en CrearOrdenEnBackendAsync: {ex.Message}");
                Console.WriteLine($"[DEBUG] StackTrace: {ex.StackTrace}");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza una orden existente en el backend
        /// </summary>
        private async Task<(bool exito, string mensaje)> ActualizarOrdenEnBackendAsync()
        {
            // 1. Actualizar datos generales de la orden
            var ordenData = new
            {
                iMesa = OrdenActual!.iMesa,
                sIndicaciones = OrdenActual.sIndicaciones,
                bOrdenModificada = true
            };

            var responseOrden = await _httpApi.PatchAsync(
                $"api/orden/{OrdenActual.sIdMongoDB}/indicaciones", 
                new { sIndicaciones = OrdenActual.sIndicaciones });

            // 2. Procesar productos
            foreach (var producto in Productos.ToList())
            {
                if (!producto.bSincronizado)
                {
                    // Producto nuevo - crear
                    var productoData = new
                    {
                        sIdOrdenMongoDB = OrdenActual.sIdMongoDB,
                        sIdProductoMongoDB = producto.sIdProductoMongoDB,
                        sNombre = producto.sNombre,
                        iCostoReal = producto.iCostoReal,
                        iCostoPublico = producto.iCostoPublico,
                        sURLImagen = producto.sURLImagen,
                        sIndicaciones = producto.sIndicaciones,
                        iIndexVarianteSeleccionada = producto.iIndexVarianteSeleccionada,
                        aVariantes = producto.aVariantes,
                        iCantidad = producto.iCantidad,
                        iTipoProducto = producto.iTipoProducto,
                        aConsumos = producto.aConsumos.Select(c => new
                        {
                            iIndex = c.iIndex,
                            aExtras = c.aExtras.Select(e => new
                            {
                                sIdExtra = e.sIdExtra,
                                sNombre = e.sNombre,
                                iCostoReal = e.iCostoReal,
                                iCostoPublico = e.iCostoPublico,
                                sURLImagen = e.sURLImagen
                            })
                        })
                    };

                    var response = await _httpApi.PostAsync("api/orden-productos/", productoData);
                    
                    if (response?.IsSuccessStatusCode == true)
                    {
                        var apiResponse = await response.Content.ReadFromJsonAsync<ApiRespuesta<OrdenProducto>>();
                        if (apiResponse?.lData?.Count > 0)
                        {
                            producto.sIdMongo = apiResponse.lData[0].sIdMongo;
                            producto.bSincronizado = true;
                        }
                    }
                }
                else if (producto.bTieneCambiosPendientes)
                {
                    // Producto existente con cambios - actualizar
                    var productoData = new
                    {
                        sNombre = producto.sNombre,
                        iCostoReal = producto.iCostoReal,
                        iCostoPublico = producto.iCostoPublico,
                        sIndicaciones = producto.sIndicaciones,
                        iIndexVarianteSeleccionada = producto.iIndexVarianteSeleccionada,
                        aVariantes = producto.aVariantes,
                        iCantidad = producto.iCantidad,
                        aConsumos = producto.aConsumos.Select(c => new
                        {
                            iIndex = c.iIndex,
                            aExtras = c.aExtras.Select(e => new
                            {
                                sIdExtra = e.sIdExtra,
                                sNombre = e.sNombre,
                                iCostoReal = e.iCostoReal,
                                iCostoPublico = e.iCostoPublico,
                                sURLImagen = e.sURLImagen
                            })
                        })
                    };

                    await _httpApi.PutAsync($"api/orden-productos/{producto.sIdMongo}", productoData);
                }

                producto.bTieneCambiosPendientes = false;
            }

            // Marcar todo como sincronizado
            OrdenActual.bSincronizado = true;
            OrdenActual.bTieneCambiosPendientes = false;
            TieneCambiosPendientes = false;

            // Actualizar SQLite
            await _localDb.UpdateItemAsync(OrdenActual);
            foreach (var prod in Productos)
            {
                prod.SerializarListas();
                await _localDb.UpdateItemAsync(prod);
            }

            return (true, "Cambios guardados exitosamente.");
        }

        #endregion

        #region HELPERS

        private void MarcarCambiosPendientes()
        {
            TieneCambiosPendientes = true;
            
            if (OrdenActual != null)
            {
                OrdenActual.bTieneCambiosPendientes = true;
            }

            OnCambiosPendientesChanged?.Invoke(this, true);
        }

        private void NotificarCambios()
        {
            OnProductosChanged?.Invoke(this, EventArgs.Empty);
            OnCambiosPendientesChanged?.Invoke(this, TieneCambiosPendientes);
        }

        /// <summary>
        /// Calcula el total de la orden actual
        /// </summary>
        public decimal CalcularTotalOrden()
        {
            decimal total = 0;

            foreach (var producto in Productos)
            {
                // Precio base del producto * cantidad
                total += producto.iCostoPublico * producto.iCantidad;

                // Extras de cada consumo
                foreach (var consumo in producto.aConsumos)
                {
                    total += consumo.aExtras.Sum(e => e.iCostoPublico);
                }

                // Extras generales (sistema legacy)
                total += producto.aExtras.Sum(e => e.iCostoPublico);
            }

            return total;
        }

        #endregion
    }
}

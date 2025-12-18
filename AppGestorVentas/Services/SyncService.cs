using AppGestorVentas.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace AppGestorVentas.Services
{
    /// <summary>
    /// Servicio de sincronización que permite trabajar offline-first
    /// Las operaciones se almacenan localmente y se sincronizan cuando el usuario lo solicita
    /// </summary>
    public class SyncService
    {
        #region PROPIEDADES

        private readonly LocalDatabaseService _localDb;
        private readonly HttpApiService _httpApi;
        private bool _isInitialized = false;

        /// <summary>
        /// Evento que se dispara cuando cambia el número de operaciones pendientes
        /// </summary>
        public event EventHandler<int>? OnPendingOperationsChanged;

        /// <summary>
        /// Evento que se dispara durante la sincronización para reportar progreso
        /// </summary>
        public event EventHandler<SyncProgressEventArgs>? OnSyncProgress;

        #endregion

        #region CONSTRUCTOR

        public SyncService(LocalDatabaseService localDb, HttpApiService httpApi)
        {
            _localDb = localDb;
            _httpApi = httpApi;
        }

        #endregion

        #region INICIALIZACIÓN

        /// <summary>
        /// Inicializa las tablas necesarias para la sincronización
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _localDb.CreateTableAsync<SyncOperation>();
            await _localDb.CreateTableAsync<Orden>();
            await _localDb.CreateTableAsync<OrdenProducto>();
            await _localDb.CreateTableAsync<Consumo>();
            await _localDb.CreateTableAsync<ExtraConsumo>();
            await _localDb.CreateTableAsync<ExtraOrdenProducto>();

            _isInitialized = true;
        }

        #endregion

        #region REGISTRO DE OPERACIONES

        /// <summary>
        /// Registra una operación de crear orden
        /// </summary>
        public async Task<Orden> RegistrarCrearOrdenAsync(Orden orden)
        {
            await InitializeAsync();

            // Asegurar que tiene ID local
            if (string.IsNullOrEmpty(orden.sIdLocal))
                orden.sIdLocal = Guid.NewGuid().ToString();

            orden.bSincronizado = false;
            orden.bTieneCambiosPendientes = true;
            orden.dtFechaAlta = DateTime.UtcNow;

            // Guardar en SQLite local
            await _localDb.SaveItemAsync(orden);

            // Crear operación de sincronización
            var syncOp = new SyncOperation
            {
                TipoOperacion = TipoOperacionSync.CREAR_ORDEN,
                sIdEntidadLocal = orden.sIdLocal,
                dtTimestampLocal = DateTime.UtcNow,
                iOrdenEjecucion = await ObtenerSiguienteOrdenEjecucionAsync()
            };

            syncOp.EstablecerDatos(new
            {
                sIdentificadorOrden = orden.sIdentificadorOrden,
                iMesa = orden.iMesa,
                iTipoOrden = orden.iTipoOrden,
                sUsuarioMesero = orden.sUsuarioMesero,
                sIdMongoDBMesero = orden.sIdMongoDBMesero,
                sIndicaciones = orden.sIndicaciones
            });

            await _localDb.SaveItemAsync(syncOp);
            NotificarCambioPendientes();

            return orden;
        }

        /// <summary>
        /// Registra una operación de actualizar orden
        /// </summary>
        public async Task RegistrarActualizarOrdenAsync(Orden orden)
        {
            await InitializeAsync();

            orden.bTieneCambiosPendientes = true;
            await _localDb.UpdateItemAsync(orden);

            var syncOp = new SyncOperation
            {
                TipoOperacion = TipoOperacionSync.ACTUALIZAR_ORDEN,
                sIdEntidadLocal = orden.sIdLocal,
                sIdEntidadMongoDB = orden.sIdMongoDB,
                dtTimestampLocal = DateTime.UtcNow,
                iOrdenEjecucion = await ObtenerSiguienteOrdenEjecucionAsync()
            };

            syncOp.EstablecerDatos(new
            {
                sIdMongoDB = orden.sIdMongoDB,
                iMesa = orden.iMesa,
                iEstatus = orden.iEstatus,
                iTipoPago = orden.iTipoPago,
                bOrdenModificada = orden.bOrdenModificada,
                sIndicaciones = orden.sIndicaciones
            });

            await _localDb.SaveItemAsync(syncOp);
            NotificarCambioPendientes();
        }

        /// <summary>
        /// Registra una operación de eliminar orden
        /// </summary>
        public async Task RegistrarEliminarOrdenAsync(Orden orden)
        {
            await InitializeAsync();

            var syncOp = new SyncOperation
            {
                TipoOperacion = TipoOperacionSync.ELIMINAR_ORDEN,
                sIdEntidadLocal = orden.sIdLocal,
                sIdEntidadMongoDB = orden.sIdMongoDB,
                dtTimestampLocal = DateTime.UtcNow,
                iOrdenEjecucion = await ObtenerSiguienteOrdenEjecucionAsync()
            };

            syncOp.EstablecerDatos(new
            {
                sIdMongoDB = !string.IsNullOrEmpty(orden.sIdMongoDB) ? orden.sIdMongoDB : orden.sIdLocal
            });

            // Eliminar de SQLite local
            await _localDb.DeleteRecordsAsync<Orden>("sIdLocal = ?", orden.sIdLocal);

            await _localDb.SaveItemAsync(syncOp);
            NotificarCambioPendientes();
        }

        /// <summary>
        /// Registra una operación de actualizar indicaciones de orden
        /// </summary>
        public async Task RegistrarActualizarIndicacionesOrdenAsync(Orden orden, string indicaciones)
        {
            await InitializeAsync();

            orden.sIndicaciones = indicaciones;
            orden.bTieneCambiosPendientes = true;
            await _localDb.UpdateItemAsync(orden);

            var syncOp = new SyncOperation
            {
                TipoOperacion = TipoOperacionSync.ACTUALIZAR_INDICACIONES_ORDEN,
                sIdEntidadLocal = orden.sIdLocal,
                sIdEntidadMongoDB = orden.sIdMongoDB,
                dtTimestampLocal = DateTime.UtcNow,
                iOrdenEjecucion = await ObtenerSiguienteOrdenEjecucionAsync()
            };

            syncOp.EstablecerDatos(new
            {
                sIdMongoDB = !string.IsNullOrEmpty(orden.sIdMongoDB) ? orden.sIdMongoDB : orden.sIdLocal,
                sIndicaciones = indicaciones
            });

            await _localDb.SaveItemAsync(syncOp);
            NotificarCambioPendientes();
        }

        /// <summary>
        /// Registra una operación de crear producto
        /// </summary>
        public async Task<OrdenProducto> RegistrarCrearProductoAsync(OrdenProducto producto, Orden orden)
        {
            await InitializeAsync();

            if (string.IsNullOrEmpty(producto.sIdLocal))
                producto.sIdLocal = Guid.NewGuid().ToString();

            producto.sIdOrdenLocal = orden.sIdLocal;
            producto.sIdOrdenMongoDB = orden.sIdMongoDB;
            producto.bSincronizado = false;
            producto.bTieneCambiosPendientes = true;

            // Inicializar consumos según cantidad
            producto.aConsumos = new List<Consumo>();
            for (int i = 1; i <= producto.iCantidad; i++)
            {
                producto.aConsumos.Add(new Consumo
                {
                    sIdOrdenProductoLocal = producto.sIdLocal,
                    iIndex = i,
                    aExtras = new List<ExtraConsumo>()
                });
            }

            await _localDb.SaveItemAsync(producto);

            var syncOp = new SyncOperation
            {
                TipoOperacion = TipoOperacionSync.CREAR_PRODUCTO,
                sIdEntidadLocal = producto.sIdLocal,
                dtTimestampLocal = DateTime.UtcNow,
                iOrdenEjecucion = await ObtenerSiguienteOrdenEjecucionAsync()
            };

            // Usar ID efectivo de la orden (puede ser local si aún no está sincronizada)
            syncOp.EstablecerDatos(new
            {
                sIdOrdenMongoDB = orden.IdEfectivo,
                sNombre = producto.sNombre,
                iCostoReal = producto.iCostoReal,
                iCostoPublico = producto.iCostoPublico,
                sURLImagen = producto.sURLImagen,
                sIndicaciones = producto.sIndicaciones,
                iIndexVarianteSeleccionada = producto.iIndexVarianteSeleccionada,
                aVariantes = producto.aVariantes,
                iCantidad = producto.iCantidad,
                iTipoProducto = producto.iTipoProducto,
                aConsumos = producto.aConsumos.Select(c => new { iIndex = c.iIndex, aExtras = new List<object>() }).ToList()
            });

            await _localDb.SaveItemAsync(syncOp);
            NotificarCambioPendientes();

            return producto;
        }

        /// <summary>
        /// Registra una operación de actualizar producto
        /// </summary>
        public async Task RegistrarActualizarProductoAsync(OrdenProducto producto)
        {
            await InitializeAsync();

            producto.bTieneCambiosPendientes = true;
            await _localDb.UpdateItemAsync(producto);

            var syncOp = new SyncOperation
            {
                TipoOperacion = TipoOperacionSync.ACTUALIZAR_PRODUCTO,
                sIdEntidadLocal = producto.sIdLocal,
                sIdEntidadMongoDB = producto.sIdMongo,
                dtTimestampLocal = DateTime.UtcNow,
                iOrdenEjecucion = await ObtenerSiguienteOrdenEjecucionAsync()
            };

            syncOp.EstablecerDatos(new
            {
                sIdMongoDB = producto.IdEfectivo,
                sNombre = producto.sNombre,
                iCostoReal = producto.iCostoReal,
                iCostoPublico = producto.iCostoPublico,
                sURLImagen = producto.sURLImagen,
                sIndicaciones = producto.sIndicaciones,
                iIndexVarianteSeleccionada = producto.iIndexVarianteSeleccionada,
                aVariantes = producto.aVariantes,
                iCantidad = producto.iCantidad,
                iTipoProducto = producto.iTipoProducto
            });

            await _localDb.SaveItemAsync(syncOp);
            NotificarCambioPendientes();
        }

        /// <summary>
        /// Registra una operación de eliminar producto
        /// </summary>
        public async Task RegistrarEliminarProductoAsync(OrdenProducto producto)
        {
            await InitializeAsync();

            var syncOp = new SyncOperation
            {
                TipoOperacion = TipoOperacionSync.ELIMINAR_PRODUCTO,
                sIdEntidadLocal = producto.sIdLocal,
                sIdEntidadMongoDB = producto.sIdMongo,
                dtTimestampLocal = DateTime.UtcNow,
                iOrdenEjecucion = await ObtenerSiguienteOrdenEjecucionAsync()
            };

            syncOp.EstablecerDatos(new
            {
                sIdMongoDB = producto.IdEfectivo
            });

            await _localDb.DeleteRecordsAsync<OrdenProducto>("sIdLocal = ?", producto.sIdLocal);

            await _localDb.SaveItemAsync(syncOp);
            NotificarCambioPendientes();
        }

        /// <summary>
        /// Registra una operación de actualizar cantidad de producto
        /// </summary>
        public async Task RegistrarActualizarCantidadProductoAsync(OrdenProducto producto, int nuevaCantidad)
        {
            await InitializeAsync();

            producto.iCantidad = nuevaCantidad;
            producto.bTieneCambiosPendientes = true;

            // Ajustar consumos
            if (nuevaCantidad > producto.aConsumos.Count)
            {
                for (int i = producto.aConsumos.Count + 1; i <= nuevaCantidad; i++)
                {
                    producto.aConsumos.Add(new Consumo
                    {
                        sIdOrdenProductoLocal = producto.sIdLocal,
                        iIndex = i,
                        aExtras = new List<ExtraConsumo>()
                    });
                }
            }
            else if (nuevaCantidad < producto.aConsumos.Count)
            {
                producto.aConsumos = producto.aConsumos.Take(nuevaCantidad).ToList();
                // Re-indexar
                for (int i = 0; i < producto.aConsumos.Count; i++)
                {
                    producto.aConsumos[i].iIndex = i + 1;
                }
            }

            await _localDb.UpdateItemAsync(producto);

            var syncOp = new SyncOperation
            {
                TipoOperacion = TipoOperacionSync.ACTUALIZAR_CANTIDAD_PRODUCTO,
                sIdEntidadLocal = producto.sIdLocal,
                sIdEntidadMongoDB = producto.sIdMongo,
                dtTimestampLocal = DateTime.UtcNow,
                iOrdenEjecucion = await ObtenerSiguienteOrdenEjecucionAsync()
            };

            syncOp.EstablecerDatos(new
            {
                sIdMongoDB = producto.IdEfectivo,
                iCantidad = nuevaCantidad,
                aConsumos = producto.aConsumos.Select(c => new 
                { 
                    iIndex = c.iIndex, 
                    aExtras = c.aExtras.Select(e => new 
                    { 
                        sIdExtra = e.sIdExtra,
                        sNombre = e.sNombre,
                        iCostoReal = e.iCostoReal,
                        iCostoPublico = e.iCostoPublico
                    }).ToList()
                }).ToList()
            });

            await _localDb.SaveItemAsync(syncOp);
            NotificarCambioPendientes();
        }

        /// <summary>
        /// Registra una operación de agregar extra a consumos
        /// </summary>
        public async Task RegistrarAgregarExtraConsumosAsync(OrdenProducto producto, ExtraConsumo extra, List<int> indexConsumos)
        {
            await InitializeAsync();

            foreach (var index in indexConsumos)
            {
                var consumo = producto.aConsumos.FirstOrDefault(c => c.iIndex == index);
                if (consumo != null && !consumo.aExtras.Any(e => e.sNombre == extra.sNombre))
                {
                    var nuevoExtra = new ExtraConsumo
                    {
                        sIdConsumoLocal = consumo.sIdLocal,
                        sIdExtra = extra.sIdExtra,
                        sNombre = extra.sNombre,
                        iCostoReal = extra.iCostoReal,
                        iCostoPublico = extra.iCostoPublico,
                        sURLImagen = extra.sURLImagen
                    };
                    consumo.aExtras.Add(nuevoExtra);
                }
            }

            producto.bTieneCambiosPendientes = true;
            producto.bTieneExtras = producto.aConsumos.Any(c => c.aExtras.Count > 0);
            await _localDb.UpdateItemAsync(producto);

            var syncOp = new SyncOperation
            {
                TipoOperacion = TipoOperacionSync.AGREGAR_EXTRA_CONSUMOS,
                sIdEntidadLocal = producto.sIdLocal,
                sIdEntidadMongoDB = producto.sIdMongo,
                dtTimestampLocal = DateTime.UtcNow,
                iOrdenEjecucion = await ObtenerSiguienteOrdenEjecucionAsync()
            };

            syncOp.EstablecerDatos(new
            {
                sIdProductoMongoDB = producto.IdEfectivo,
                extra = new
                {
                    sIdExtra = extra.sIdExtra,
                    sNombre = extra.sNombre,
                    iCostoReal = extra.iCostoReal,
                    iCostoPublico = extra.iCostoPublico,
                    sURLImagen = extra.sURLImagen
                },
                aIndexConsumos = indexConsumos
            });

            await _localDb.SaveItemAsync(syncOp);
            NotificarCambioPendientes();
        }

        /// <summary>
        /// Registra una operación de eliminar extra de consumo
        /// </summary>
        public async Task RegistrarEliminarExtraConsumoAsync(OrdenProducto producto, int indexConsumo, string idExtra)
        {
            await InitializeAsync();

            var consumo = producto.aConsumos.FirstOrDefault(c => c.iIndex == indexConsumo);
            if (consumo != null)
            {
                var extra = consumo.aExtras.FirstOrDefault(e => e.sIdExtra == idExtra || e.sIdLocal == idExtra);
                if (extra != null)
                {
                    consumo.aExtras.Remove(extra);
                }
            }

            producto.bTieneCambiosPendientes = true;
            producto.bTieneExtras = producto.aConsumos.Any(c => c.aExtras.Count > 0);
            await _localDb.UpdateItemAsync(producto);

            var syncOp = new SyncOperation
            {
                TipoOperacion = TipoOperacionSync.ELIMINAR_EXTRA_CONSUMO,
                sIdEntidadLocal = producto.sIdLocal,
                sIdEntidadMongoDB = producto.sIdMongo,
                dtTimestampLocal = DateTime.UtcNow,
                iOrdenEjecucion = await ObtenerSiguienteOrdenEjecucionAsync()
            };

            syncOp.EstablecerDatos(new
            {
                sIdProductoMongoDB = producto.IdEfectivo,
                indexConsumo = indexConsumo,
                idExtra = idExtra
            });

            await _localDb.SaveItemAsync(syncOp);
            NotificarCambioPendientes();
        }

        /// <summary>
        /// Registra una operación de eliminar consumo
        /// </summary>
        public async Task RegistrarEliminarConsumoAsync(OrdenProducto producto, int indexConsumo)
        {
            await InitializeAsync();

            var consumo = producto.aConsumos.FirstOrDefault(c => c.iIndex == indexConsumo);
            if (consumo != null)
            {
                producto.aConsumos.Remove(consumo);
                producto.iCantidad = Math.Max(0, producto.iCantidad - 1);

                // Re-indexar
                for (int i = 0; i < producto.aConsumos.Count; i++)
                {
                    producto.aConsumos[i].iIndex = i + 1;
                }
            }

            producto.bTieneCambiosPendientes = true;
            
            if (producto.iCantidad == 0)
            {
                await _localDb.DeleteRecordsAsync<OrdenProducto>("sIdLocal = ?", producto.sIdLocal);
            }
            else
            {
                await _localDb.UpdateItemAsync(producto);
            }

            var syncOp = new SyncOperation
            {
                TipoOperacion = TipoOperacionSync.ELIMINAR_CONSUMO,
                sIdEntidadLocal = producto.sIdLocal,
                sIdEntidadMongoDB = producto.sIdMongo,
                dtTimestampLocal = DateTime.UtcNow,
                iOrdenEjecucion = await ObtenerSiguienteOrdenEjecucionAsync()
            };

            syncOp.EstablecerDatos(new
            {
                sIdProductoMongoDB = producto.IdEfectivo,
                indexConsumo = indexConsumo
            });

            await _localDb.SaveItemAsync(syncOp);
            NotificarCambioPendientes();
        }

        #endregion

        #region SINCRONIZACIÓN

        /// <summary>
        /// Sincroniza todas las operaciones pendientes con el backend
        /// </summary>
        public async Task<SyncResult> SincronizarAsync()
        {
            await InitializeAsync();

            var resultado = new SyncResult();

            try
            {
                // Verificar conectividad
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "No hay conexión a Internet";
                    return resultado;
                }

                // Obtener operaciones pendientes ordenadas
                var operacionesPendientes = await ObtenerOperacionesPendientesAsync();

                if (operacionesPendientes.Count == 0)
                {
                    resultado.Exitoso = true;
                    resultado.Mensaje = "No hay operaciones pendientes";
                    return resultado;
                }

                OnSyncProgress?.Invoke(this, new SyncProgressEventArgs
                {
                    TotalOperaciones = operacionesPendientes.Count,
                    OperacionesProcesadas = 0,
                    Estado = "Iniciando sincronización..."
                });

                // Construir payload
                var payload = new SyncPayload
                {
                    Operaciones = operacionesPendientes.Select(op => new SyncOperationRequest
                    {
                        TipoOperacion = op.sTipoOperacion,
                        IdLocal = op.sIdEntidadLocal,
                        Datos = op.ObtenerDatos<object>(),
                        TimestampLocal = op.dtTimestampLocal
                    }).ToList()
                };

                // Enviar al backend
                var response = await _httpApi.PostAsync("api/sync/ordenes", payload);

                if (response == null)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "No se recibió respuesta del servidor";
                    return resultado;
                }

                if (!response.IsSuccessStatusCode)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = $"Error del servidor: {response.StatusCode}";
                    return resultado;
                }

                // Procesar respuesta
                var syncResponse = await response.Content.ReadFromJsonAsync<SyncResponse>();

                if (syncResponse == null || !syncResponse.Success)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = syncResponse?.Message ?? "Error desconocido en la sincronización";
                    return resultado;
                }

                // Actualizar IDs locales con IDs de MongoDB
                if (syncResponse.Data?.IdMapping != null)
                {
                    await ActualizarIdsMappingAsync(syncResponse.Data.IdMapping);
                }

                // Marcar operaciones como sincronizadas
                foreach (var opPendiente in operacionesPendientes)
                {
                    var resultadoOp = syncResponse.Data?.Resultados
                        .FirstOrDefault(r => r.IdLocal == opPendiente.sIdEntidadLocal);

                    if (resultadoOp?.Resultado == "EXITOSO")
                    {
                        opPendiente.Estado = EstadoOperacionSync.EXITOSO;
                        opPendiente.sIdEntidadMongoDB = resultadoOp.IdMongoDB ?? string.Empty;
                    }
                    else
                    {
                        opPendiente.Estado = EstadoOperacionSync.ERROR;
                        opPendiente.sErrorMensaje = resultadoOp?.Error ?? "Error desconocido";
                        opPendiente.iIntentos++;
                    }

                    await _localDb.UpdateItemAsync(opPendiente);
                }

                // Eliminar operaciones exitosas
                await _localDb.DeleteRecordsAsync<SyncOperation>("sEstado = ?", EstadoOperacionSync.EXITOSO.ToString());

                // Marcar entidades como sincronizadas
                await MarcarEntidadesSincronizadasAsync();

                resultado.Exitoso = syncResponse.Data?.Resumen?.Fallidas == 0;
                resultado.Mensaje = syncResponse.Message;
                resultado.TotalOperaciones = syncResponse.Data?.Resumen?.TotalOperaciones ?? 0;
                resultado.Exitosas = syncResponse.Data?.Resumen?.Exitosas ?? 0;
                resultado.Fallidas = syncResponse.Data?.Resumen?.Fallidas ?? 0;
                resultado.IdMapping = syncResponse.Data?.IdMapping ?? new Dictionary<string, string>();

                OnSyncProgress?.Invoke(this, new SyncProgressEventArgs
                {
                    TotalOperaciones = resultado.TotalOperaciones,
                    OperacionesProcesadas = resultado.TotalOperaciones,
                    Estado = resultado.Exitoso ? "Sincronización completada" : "Sincronización con errores"
                });

                NotificarCambioPendientes();

                return resultado;
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = $"Error durante la sincronización: {ex.Message}";
                return resultado;
            }
        }

        /// <summary>
        /// Verifica el estado del servicio de sincronización en el backend
        /// </summary>
        public async Task<bool> VerificarEstadoServicioAsync()
        {
            try
            {
                var response = await _httpApi.GetAsync("api/sync/status");
                
                if (response == null || !response.IsSuccessStatusCode)
                    return false;

                var statusResponse = await response.Content.ReadFromJsonAsync<SyncStatusResponse>();
                return statusResponse?.Data?.ServicioActivo ?? false;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region CONSULTAS

        /// <summary>
        /// Obtiene el número de operaciones pendientes
        /// </summary>
        public async Task<int> ObtenerCantidadPendientesAsync()
        {
            await InitializeAsync();
            var pendientes = await _localDb.GetItemsAsync<SyncOperation>(
                "SELECT * FROM tb_SyncOperation WHERE sEstado = ?",
                EstadoOperacionSync.PENDIENTE.ToString());
            return pendientes?.Count ?? 0;
        }

        /// <summary>
        /// Obtiene todas las operaciones pendientes ordenadas
        /// </summary>
        public async Task<List<SyncOperation>> ObtenerOperacionesPendientesAsync()
        {
            await InitializeAsync();
            var pendientes = await _localDb.GetItemsAsync<SyncOperation>(
                "SELECT * FROM tb_SyncOperation WHERE sEstado = ? ORDER BY iOrdenEjecucion ASC, dtTimestampLocal ASC",
                EstadoOperacionSync.PENDIENTE.ToString());
            return pendientes ?? new List<SyncOperation>();
        }

        /// <summary>
        /// Verifica si hay cambios pendientes
        /// </summary>
        public async Task<bool> HayCambiosPendientesAsync()
        {
            return await ObtenerCantidadPendientesAsync() > 0;
        }

        /// <summary>
        /// Obtiene una orden por su ID local
        /// </summary>
        public async Task<Orden?> ObtenerOrdenPorIdLocalAsync(string idLocal)
        {
            await InitializeAsync();
            var ordenes = await _localDb.GetItemsAsync<Orden>(
                "SELECT * FROM tb_Orden WHERE sIdLocal = ?", idLocal);
            return ordenes?.FirstOrDefault();
        }

        /// <summary>
        /// Obtiene un producto por su ID local
        /// </summary>
        public async Task<OrdenProducto?> ObtenerProductoPorIdLocalAsync(string idLocal)
        {
            await InitializeAsync();
            var productos = await _localDb.GetItemsAsync<OrdenProducto>(
                "SELECT * FROM tb_OrdenProducto WHERE sIdLocal = ?", idLocal);
            return productos?.FirstOrDefault();
        }

        /// <summary>
        /// Obtiene productos de una orden por ID local de orden
        /// </summary>
        public async Task<List<OrdenProducto>> ObtenerProductosPorOrdenLocalAsync(string idOrdenLocal)
        {
            await InitializeAsync();
            var productos = await _localDb.GetItemsAsync<OrdenProducto>(
                "SELECT * FROM tb_OrdenProducto WHERE sIdOrdenLocal = ?", idOrdenLocal);
            return productos ?? new List<OrdenProducto>();
        }

        #endregion

        #region MÉTODOS AUXILIARES

        private async Task<int> ObtenerSiguienteOrdenEjecucionAsync()
        {
            var operaciones = await _localDb.GetItemsAsync<SyncOperation>(
                "SELECT MAX(iOrdenEjecucion) as iOrdenEjecucion FROM tb_SyncOperation");
            return (operaciones?.FirstOrDefault()?.iOrdenEjecucion ?? 0) + 1;
        }

        private async void NotificarCambioPendientes()
        {
            var cantidad = await ObtenerCantidadPendientesAsync();
            OnPendingOperationsChanged?.Invoke(this, cantidad);
        }

        private async Task ActualizarIdsMappingAsync(Dictionary<string, string> idMapping)
        {
            foreach (var mapping in idMapping)
            {
                var idLocal = mapping.Key;
                var idMongo = mapping.Value;

                // Actualizar órdenes
                var ordenes = await _localDb.GetItemsAsync<Orden>(
                    "SELECT * FROM tb_Orden WHERE sIdLocal = ?", idLocal);
                
                if (ordenes?.FirstOrDefault() is Orden orden)
                {
                    orden.sIdMongoDB = idMongo;
                    orden.bSincronizado = true;
                    orden.bTieneCambiosPendientes = false;
                    await _localDb.UpdateItemAsync(orden);

                    // Actualizar productos que referencian esta orden
                    var productos = await _localDb.GetItemsAsync<OrdenProducto>(
                        "SELECT * FROM tb_OrdenProducto WHERE sIdOrdenLocal = ?", idLocal);
                    
                    foreach (var prod in productos ?? new List<OrdenProducto>())
                    {
                        prod.sIdOrdenMongoDB = idMongo;
                        await _localDb.UpdateItemAsync(prod);
                    }
                }

                // Actualizar productos
                var productosDirectos = await _localDb.GetItemsAsync<OrdenProducto>(
                    "SELECT * FROM tb_OrdenProducto WHERE sIdLocal = ?", idLocal);
                
                if (productosDirectos?.FirstOrDefault() is OrdenProducto producto)
                {
                    producto.sIdMongo = idMongo;
                    producto.bSincronizado = true;
                    producto.bTieneCambiosPendientes = false;
                    await _localDb.UpdateItemAsync(producto);
                }
            }
        }

        private async Task MarcarEntidadesSincronizadasAsync()
        {
            // Marcar órdenes sin cambios pendientes como sincronizadas
            var ordenes = await _localDb.GetItemsAsync<Orden>(
                "SELECT * FROM tb_Orden WHERE bTieneCambiosPendientes = 1");
            
            foreach (var orden in ordenes ?? new List<Orden>())
            {
                // Verificar si hay operaciones pendientes para esta orden
                var opsPendientes = await _localDb.GetItemsAsync<SyncOperation>(
                    "SELECT * FROM tb_SyncOperation WHERE sIdEntidadLocal = ? AND sEstado = ?",
                    orden.sIdLocal, EstadoOperacionSync.PENDIENTE.ToString());
                
                if (opsPendientes == null || opsPendientes.Count == 0)
                {
                    orden.bTieneCambiosPendientes = false;
                    orden.bSincronizado = !string.IsNullOrEmpty(orden.sIdMongoDB);
                    await _localDb.UpdateItemAsync(orden);
                }
            }

            // Similar para productos
            var productos = await _localDb.GetItemsAsync<OrdenProducto>(
                "SELECT * FROM tb_OrdenProducto WHERE bTieneCambiosPendientes = 1");
            
            foreach (var producto in productos ?? new List<OrdenProducto>())
            {
                var opsPendientes = await _localDb.GetItemsAsync<SyncOperation>(
                    "SELECT * FROM tb_SyncOperation WHERE sIdEntidadLocal = ? AND sEstado = ?",
                    producto.sIdLocal, EstadoOperacionSync.PENDIENTE.ToString());
                
                if (opsPendientes == null || opsPendientes.Count == 0)
                {
                    producto.bTieneCambiosPendientes = false;
                    producto.bSincronizado = !string.IsNullOrEmpty(producto.sIdMongo);
                    await _localDb.UpdateItemAsync(producto);
                }
            }
        }

        /// <summary>
        /// Limpia todas las operaciones de sincronización (para debug/reset)
        /// </summary>
        public async Task LimpiarOperacionesAsync()
        {
            await InitializeAsync();
            await _localDb.DeleteAllRecordsAsync<SyncOperation>();
            NotificarCambioPendientes();
        }

        #endregion
    }

    #region CLASES AUXILIARES

    /// <summary>
    /// Resultado de una operación de sincronización
    /// </summary>
    public class SyncResult
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int TotalOperaciones { get; set; }
        public int Exitosas { get; set; }
        public int Fallidas { get; set; }
        public Dictionary<string, string> IdMapping { get; set; } = new();
    }

    /// <summary>
    /// Argumentos para eventos de progreso de sincronización
    /// </summary>
    public class SyncProgressEventArgs : EventArgs
    {
        public int TotalOperaciones { get; set; }
        public int OperacionesProcesadas { get; set; }
        public string Estado { get; set; } = string.Empty;
        public double Porcentaje => TotalOperaciones > 0 
            ? (double)OperacionesProcesadas / TotalOperaciones * 100 
            : 0;
    }

    #endregion
}

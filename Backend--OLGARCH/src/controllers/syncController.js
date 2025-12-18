/**
 * Controller de Sincronización Unificada
 * 
 * Este controlador recibe un array de operaciones desde el frontend
 * y las procesa secuencialmente, garantizando consistencia de datos.
 * 
 * Tipos de operaciones soportadas:
 * - CREAR_ORDEN
 * - ACTUALIZAR_ORDEN
 * - ELIMINAR_ORDEN
 * - ACTUALIZAR_INDICACIONES_ORDEN
 * - CREAR_PRODUCTO
 * - ACTUALIZAR_PRODUCTO
 * - ELIMINAR_PRODUCTO
 * - ACTUALIZAR_CANTIDAD_PRODUCTO
 * - AGREGAR_EXTRA_CONSUMOS
 * - ELIMINAR_EXTRA_CONSUMO
 * - ELIMINAR_CONSUMO
 */

import mongoose from 'mongoose';
import Orden from '../models/orden.js';
import OrdenProducto from '../models/ordenProducto.js';
import SyncLog from '../models/syncLog.js';

/**
 * Mapeo de IDs locales a IDs de MongoDB
 * Se usa durante la sincronización para resolver referencias
 */
const idMapping = new Map();

/**
 * Procesar sincronización de operaciones
 * POST /api/sync/ordenes
 * 
 * Body esperado:
 * {
 *   "operaciones": [
 *     {
 *       "tipoOperacion": "CREAR_ORDEN",
 *       "idLocal": "uuid-local-123",
 *       "datos": { ... },
 *       "timestampLocal": "2024-01-15T10:30:00Z"
 *     },
 *     ...
 *   ]
 * }
 */
export const sincronizarOrdenes = async (req, res) => {
  const session = await mongoose.startSession();
  
  // Limpiar el mapeo de IDs al inicio de cada sincronización
  idMapping.clear();
  
  try {
    const { operaciones } = req.body;
    const usuario = req.user; // Viene del middleware de autenticación

    // Validaciones iniciales
    if (!operaciones || !Array.isArray(operaciones) || operaciones.length === 0) {
      return res.status(400).json({
        success: false,
        message: 'El array de operaciones es requerido y no puede estar vacío',
        error: { code: 400, details: 'Payload inválido' }
      });
    }

    // Crear registro de sincronización
    const syncLog = new SyncLog({
      sIdUsuario: usuario.id,
      sNombreUsuario: usuario.sUsuario,
      operaciones: operaciones.map(op => ({
        tipoOperacion: op.tipoOperacion,
        idLocal: op.idLocal,
        datos: op.datos,
        timestampLocal: op.timestampLocal,
        resultado: 'PENDIENTE'
      })),
      resumen: {
        totalOperaciones: operaciones.length,
        exitosas: 0,
        fallidas: 0
      }
    });

    await syncLog.save();

    // Iniciar transacción
    session.startTransaction();

    const resultados = [];
    let exitosas = 0;
    let fallidas = 0;

    // Ordenar operaciones por timestamp para garantizar el orden correcto
    const operacionesOrdenadas = [...operaciones].sort(
      (a, b) => new Date(a.timestampLocal) - new Date(b.timestampLocal)
    );

    // Procesar cada operación secuencialmente
    for (let i = 0; i < operacionesOrdenadas.length; i++) {
      const operacion = operacionesOrdenadas[i];
      
      try {
        const resultado = await procesarOperacion(operacion, session);
        
        resultados.push({
          idLocal: operacion.idLocal,
          tipoOperacion: operacion.tipoOperacion,
          resultado: 'EXITOSO',
          idMongoDB: resultado.idMongoDB || null,
          datos: resultado.datos || null
        });

        // Actualizar el log
        const opIndex = syncLog.operaciones.findIndex(op => op.idLocal === operacion.idLocal);
        if (opIndex !== -1) {
          syncLog.operaciones[opIndex].resultado = 'EXITOSO';
          syncLog.operaciones[opIndex].idMongoDB = resultado.idMongoDB || null;
        }

        exitosas++;

      } catch (error) {
        resultados.push({
          idLocal: operacion.idLocal,
          tipoOperacion: operacion.tipoOperacion,
          resultado: 'ERROR',
          error: error.message
        });

        // Actualizar el log
        const opIndex = syncLog.operaciones.findIndex(op => op.idLocal === operacion.idLocal);
        if (opIndex !== -1) {
          syncLog.operaciones[opIndex].resultado = 'ERROR';
          syncLog.operaciones[opIndex].errorDetalle = error.message;
        }

        fallidas++;

        // Si una operación falla, decidimos si continuar o abortar
        // Por ahora, continuamos con las demás operaciones
        console.error(`[Sync] Error en operación ${operacion.tipoOperacion}:`, error.message);
      }
    }

    // Commit de la transacción
    await session.commitTransaction();

    // Actualizar resumen del log
    syncLog.resumen.exitosas = exitosas;
    syncLog.resumen.fallidas = fallidas;
    syncLog.estadoGeneral = fallidas === 0 
      ? 'COMPLETADO' 
      : exitosas === 0 
        ? 'FALLIDO' 
        : 'COMPLETADO_CON_ERRORES';

    await syncLog.save();

    return res.status(200).json({
      success: true,
      message: `Sincronización completada. ${exitosas} exitosas, ${fallidas} fallidas.`,
      data: {
        syncLogId: syncLog._id,
        resumen: syncLog.resumen,
        estadoGeneral: syncLog.estadoGeneral,
        resultados,
        // Mapeo de IDs locales a MongoDB para que el frontend actualice sus referencias
        idMapping: Object.fromEntries(idMapping)
      }
    });

  } catch (error) {
    await session.abortTransaction();

    console.error('[Sync] Error general:', error);

    return res.status(500).json({
      success: false,
      message: 'Error durante la sincronización',
      error: {
        code: 500,
        details: error.message
      }
    });

  } finally {
    session.endSession();
  }
};


/**
 * Procesar una operación individual
 */
async function procesarOperacion(operacion, session) {
  const { tipoOperacion, idLocal, datos } = operacion;

  switch (tipoOperacion) {
    case 'CREAR_ORDEN':
      return await crearOrden(idLocal, datos, session);

    case 'ACTUALIZAR_ORDEN':
      return await actualizarOrden(idLocal, datos, session);

    case 'ELIMINAR_ORDEN':
      return await eliminarOrden(idLocal, datos, session);

    case 'ACTUALIZAR_INDICACIONES_ORDEN':
      return await actualizarIndicacionesOrden(idLocal, datos, session);

    case 'CREAR_PRODUCTO':
      return await crearProducto(idLocal, datos, session);

    case 'ACTUALIZAR_PRODUCTO':
      return await actualizarProducto(idLocal, datos, session);

    case 'ELIMINAR_PRODUCTO':
      return await eliminarProducto(idLocal, datos, session);

    case 'ACTUALIZAR_CANTIDAD_PRODUCTO':
      return await actualizarCantidadProducto(idLocal, datos, session);

    case 'AGREGAR_EXTRA_CONSUMOS':
      return await agregarExtraConsumos(idLocal, datos, session);

    case 'ELIMINAR_EXTRA_CONSUMO':
      return await eliminarExtraConsumo(idLocal, datos, session);

    case 'ELIMINAR_CONSUMO':
      return await eliminarConsumo(idLocal, datos, session);

    default:
      throw new Error(`Tipo de operación no soportado: ${tipoOperacion}`);
  }
}


/**
 * Resolver ID: Si es un ID local, buscar en el mapeo. Si no, es un ID MongoDB.
 */
function resolverIdMongoDB(idLocalOrMongo) {
  // Si el ID está en el mapeo, usar el ID de MongoDB
  if (idMapping.has(idLocalOrMongo)) {
    return idMapping.get(idLocalOrMongo);
  }
  // Si no, asumir que ya es un ID de MongoDB válido
  return idLocalOrMongo;
}


// ============================================================
// OPERACIONES DE ORDEN
// ============================================================

async function crearOrden(idLocal, datos, session) {
  const nuevaOrden = new Orden({
    sIdentificadorOrden: datos.sIdentificadorOrden,
    iMesa: datos.iMesa,
    iTipoOrden: datos.iTipoOrden || 1,
    sUsuarioMesero: datos.sUsuarioMesero,
    sIdMongoDBMesero: datos.sIdMongoDBMesero,
    sIndicaciones: datos.sIndicaciones || ''
  });

  const ordenGuardada = await nuevaOrden.save({ session });

  // Guardar el mapeo de ID local a MongoDB
  idMapping.set(idLocal, ordenGuardada._id.toString());

  return {
    idMongoDB: ordenGuardada._id.toString(),
    datos: ordenGuardada.toObject()
  };
}


async function actualizarOrden(idLocal, datos, session) {
  const idMongoDB = resolverIdMongoDB(datos.sIdMongoDB || idLocal);

  const ordenActualizada = await Orden.findByIdAndUpdate(
    idMongoDB,
    {
      iMesa: datos.iMesa,
      iEstatus: datos.iEstatus,
      iTipoPago: datos.iTipoPago,
      bOrdenModificada: datos.bOrdenModificada,
      sIndicaciones: datos.sIndicaciones
    },
    { new: true, session }
  );

  if (!ordenActualizada) {
    throw new Error(`Orden no encontrada: ${idMongoDB}`);
  }

  return {
    idMongoDB: ordenActualizada._id.toString(),
    datos: ordenActualizada.toObject()
  };
}


async function eliminarOrden(idLocal, datos, session) {
  const idMongoDB = resolverIdMongoDB(datos.sIdMongoDB || idLocal);

  // Primero eliminar productos asociados
  await OrdenProducto.deleteMany(
    { sIdOrdenMongoDB: idMongoDB },
    { session }
  );

  // Si es orden primaria, eliminar secundarias
  const orden = await Orden.findById(idMongoDB).session(session);
  
  if (orden && orden.iTipoOrden === 1) {
    // Obtener órdenes secundarias
    const ordenesSecundarias = await Orden.find({
      sIdentificadorOrden: orden.sIdentificadorOrden,
      iTipoOrden: 2
    }).session(session);

    // Eliminar productos de órdenes secundarias
    for (const ordenSec of ordenesSecundarias) {
      await OrdenProducto.deleteMany(
        { sIdOrdenMongoDB: ordenSec._id },
        { session }
      );
    }

    // Eliminar órdenes secundarias
    await Orden.deleteMany({
      sIdentificadorOrden: orden.sIdentificadorOrden,
      iTipoOrden: 2
    }, { session });
  }

  // Eliminar la orden principal
  const ordenEliminada = await Orden.findByIdAndDelete(idMongoDB, { session });

  if (!ordenEliminada) {
    throw new Error(`Orden no encontrada para eliminar: ${idMongoDB}`);
  }

  return {
    idMongoDB: idMongoDB,
    datos: { eliminada: true }
  };
}


async function actualizarIndicacionesOrden(idLocal, datos, session) {
  const idMongoDB = resolverIdMongoDB(datos.sIdMongoDB || idLocal);

  const ordenActualizada = await Orden.findByIdAndUpdate(
    idMongoDB,
    { sIndicaciones: datos.sIndicaciones },
    { new: true, session }
  );

  if (!ordenActualizada) {
    throw new Error(`Orden no encontrada: ${idMongoDB}`);
  }

  return {
    idMongoDB: ordenActualizada._id.toString(),
    datos: { sIndicaciones: ordenActualizada.sIndicaciones }
  };
}


// ============================================================
// OPERACIONES DE PRODUCTO
// ============================================================

async function crearProducto(idLocal, datos, session) {
  // Resolver el ID de la orden (puede ser local o MongoDB)
  const idOrdenMongoDB = resolverIdMongoDB(datos.sIdOrdenMongoDB);

  const nuevoProducto = new OrdenProducto({
    sIdOrdenMongoDB: idOrdenMongoDB,
    sNombre: datos.sNombre,
    iCostoReal: datos.iCostoReal,
    iCostoPublico: datos.iCostoPublico,
    sURLImagen: datos.sURLImagen,
    sIndicaciones: datos.sIndicaciones || 'Sin indicaciones adicionales.',
    iIndexVarianteSeleccionada: datos.iIndexVarianteSeleccionada,
    aVariantes: datos.aVariantes || [],
    iCantidad: datos.iCantidad || 1,
    aExtras: datos.aExtras || [],
    aConsumos: datos.aConsumos || [],
    iTipoProducto: datos.iTipoProducto
  });

  const productoGuardado = await nuevoProducto.save({ session });

  // Agregar el producto al array de productos de la orden
  await Orden.findByIdAndUpdate(
    idOrdenMongoDB,
    { $push: { aProductos: productoGuardado._id } },
    { session }
  );

  // Guardar el mapeo de ID local a MongoDB
  idMapping.set(idLocal, productoGuardado._id.toString());

  return {
    idMongoDB: productoGuardado._id.toString(),
    datos: productoGuardado.toObject()
  };
}


async function actualizarProducto(idLocal, datos, session) {
  const idMongoDB = resolverIdMongoDB(datos.sIdMongoDB || idLocal);

  const productoActualizado = await OrdenProducto.findByIdAndUpdate(
    idMongoDB,
    {
      sNombre: datos.sNombre,
      iCostoReal: datos.iCostoReal,
      iCostoPublico: datos.iCostoPublico,
      sURLImagen: datos.sURLImagen,
      sIndicaciones: datos.sIndicaciones,
      iIndexVarianteSeleccionada: datos.iIndexVarianteSeleccionada,
      aVariantes: datos.aVariantes,
      iCantidad: datos.iCantidad,
      aExtras: datos.aExtras,
      aConsumos: datos.aConsumos,
      iTipoProducto: datos.iTipoProducto
    },
    { new: true, session }
  );

  if (!productoActualizado) {
    throw new Error(`Producto no encontrado: ${idMongoDB}`);
  }

  return {
    idMongoDB: productoActualizado._id.toString(),
    datos: productoActualizado.toObject()
  };
}


async function eliminarProducto(idLocal, datos, session) {
  const idMongoDB = resolverIdMongoDB(datos.sIdMongoDB || idLocal);

  const productoEliminado = await OrdenProducto.findByIdAndDelete(idMongoDB, { session });

  if (!productoEliminado) {
    throw new Error(`Producto no encontrado para eliminar: ${idMongoDB}`);
  }

  // Remover la referencia de la orden
  await Orden.findByIdAndUpdate(
    productoEliminado.sIdOrdenMongoDB,
    { $pull: { aProductos: productoEliminado._id } },
    { session }
  );

  return {
    idMongoDB: idMongoDB,
    datos: { eliminado: true }
  };
}


async function actualizarCantidadProducto(idLocal, datos, session) {
  const idMongoDB = resolverIdMongoDB(datos.sIdMongoDB || idLocal);

  const producto = await OrdenProducto.findById(idMongoDB).session(session);

  if (!producto) {
    throw new Error(`Producto no encontrado: ${idMongoDB}`);
  }

  // Si la cantidad es 0, eliminar el producto
  if (datos.iCantidad === 0) {
    await OrdenProducto.findByIdAndDelete(idMongoDB, { session });
    await Orden.findByIdAndUpdate(
      producto.sIdOrdenMongoDB,
      { $pull: { aProductos: producto._id } },
      { session }
    );

    return {
      idMongoDB: idMongoDB,
      datos: { eliminado: true, razon: 'cantidad_cero' }
    };
  }

  // Actualizar cantidad
  producto.iCantidad = datos.iCantidad;
  
  // Si también se envían consumos actualizados, aplicarlos
  if (datos.aConsumos) {
    producto.aConsumos = datos.aConsumos;
  }

  await producto.save({ session });

  return {
    idMongoDB: producto._id.toString(),
    datos: producto.toObject()
  };
}


// ============================================================
// OPERACIONES DE EXTRAS Y CONSUMOS
// ============================================================

async function agregarExtraConsumos(idLocal, datos, session) {
  const idMongoDB = resolverIdMongoDB(datos.sIdProductoMongoDB || idLocal);

  const producto = await OrdenProducto.findById(idMongoDB).session(session);

  if (!producto) {
    throw new Error(`Producto no encontrado: ${idMongoDB}`);
  }

  const { extra, aIndexConsumos } = datos;
  let extrasAgregados = 0;
  let extrasDescartados = 0;

  for (const indexConsumo of aIndexConsumos) {
    const consumo = producto.aConsumos.find(c => c.iIndex === indexConsumo);
    
    if (consumo) {
      // Verificar si el extra ya existe en este consumo
      const existeExtra = consumo.aExtras.some(e => e.sNombre === extra.sNombre);
      
      if (!existeExtra) {
        consumo.aExtras.push({
          sIdExtra: extra.sIdExtra || null,
          sNombre: extra.sNombre,
          iCostoReal: extra.iCostoReal,
          iCostoPublico: extra.iCostoPublico,
          sURLImagen: extra.sURLImagen || ''
        });
        extrasAgregados++;
      } else {
        extrasDescartados++;
      }
    }
  }

  await producto.save({ session });

  return {
    idMongoDB: producto._id.toString(),
    datos: {
      aConsumos: producto.aConsumos,
      extrasAgregados,
      extrasDescartados
    }
  };
}


async function eliminarExtraConsumo(idLocal, datos, session) {
  const idMongoDB = resolverIdMongoDB(datos.sIdProductoMongoDB || idLocal);

  const producto = await OrdenProducto.findById(idMongoDB).session(session);

  if (!producto) {
    throw new Error(`Producto no encontrado: ${idMongoDB}`);
  }

  const { indexConsumo, idExtra } = datos;

  const consumo = producto.aConsumos.find(c => c.iIndex === parseInt(indexConsumo));

  if (!consumo) {
    throw new Error(`Consumo no encontrado con índice: ${indexConsumo}`);
  }

  const extraIndex = consumo.aExtras.findIndex(e => e._id.toString() === idExtra);

  if (extraIndex === -1) {
    throw new Error(`Extra no encontrado en el consumo: ${idExtra}`);
  }

  consumo.aExtras.splice(extraIndex, 1);
  await producto.save({ session });

  return {
    idMongoDB: producto._id.toString(),
    datos: {
      consumoActualizado: consumo
    }
  };
}


async function eliminarConsumo(idLocal, datos, session) {
  const idMongoDB = resolverIdMongoDB(datos.sIdProductoMongoDB || idLocal);

  const producto = await OrdenProducto.findById(idMongoDB).session(session);

  if (!producto) {
    throw new Error(`Producto no encontrado: ${idMongoDB}`);
  }

  const { indexConsumo } = datos;

  const consumoIndex = producto.aConsumos.findIndex(c => c.iIndex === parseInt(indexConsumo));

  if (consumoIndex === -1) {
    throw new Error(`Consumo no encontrado con índice: ${indexConsumo}`);
  }

  // Eliminar el consumo
  producto.aConsumos.splice(consumoIndex, 1);

  // Decrementar la cantidad
  producto.iCantidad = Math.max(0, producto.iCantidad - 1);

  // Re-indexar los consumos restantes
  producto.aConsumos.forEach((consumo, idx) => {
    consumo.iIndex = idx + 1;
  });

  // Si la cantidad llegó a 0, eliminar el producto completo
  if (producto.iCantidad === 0) {
    await OrdenProducto.findByIdAndDelete(idMongoDB, { session });
    await Orden.findByIdAndUpdate(
      producto.sIdOrdenMongoDB,
      { $pull: { aProductos: producto._id } },
      { session }
    );

    return {
      idMongoDB: idMongoDB,
      datos: { productoEliminado: true }
    };
  }

  await producto.save({ session });

  return {
    idMongoDB: producto._id.toString(),
    datos: {
      iCantidadActual: producto.iCantidad,
      aConsumos: producto.aConsumos
    }
  };
}


// ============================================================
// ENDPOINTS AUXILIARES
// ============================================================

/**
 * Obtener historial de sincronizaciones de un usuario
 * GET /api/sync/historial
 */
export const obtenerHistorialSync = async (req, res) => {
  try {
    const usuario = req.user;
    const { limite = 10, pagina = 1 } = req.query;

    const skip = (parseInt(pagina) - 1) * parseInt(limite);

    const historial = await SyncLog.find({ sIdUsuario: usuario.id })
      .sort({ dtFechaSincronizacion: -1 })
      .skip(skip)
      .limit(parseInt(limite))
      .select('-operaciones.datos'); // Excluir datos detallados para reducir payload

    const total = await SyncLog.countDocuments({ sIdUsuario: usuario.id });

    return res.status(200).json({
      success: true,
      message: 'Historial de sincronizaciones obtenido',
      data: {
        historial,
        paginacion: {
          total,
          pagina: parseInt(pagina),
          limite: parseInt(limite),
          totalPaginas: Math.ceil(total / parseInt(limite))
        }
      }
    });

  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al obtener historial de sincronizaciones',
      error: { code: 500, details: error.message }
    });
  }
};


/**
 * Obtener detalle de una sincronización específica
 * GET /api/sync/:id
 */
export const obtenerDetalleSync = async (req, res) => {
  try {
    const { id } = req.params;

    const syncLog = await SyncLog.findById(id);

    if (!syncLog) {
      return res.status(404).json({
        success: false,
        message: 'Registro de sincronización no encontrado',
        error: { code: 404, details: 'No existe' }
      });
    }

    return res.status(200).json({
      success: true,
      message: 'Detalle de sincronización obtenido',
      data: syncLog
    });

  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al obtener detalle de sincronización',
      error: { code: 500, details: error.message }
    });
  }
};


/**
 * Verificar estado de conexión y preparación para sync
 * GET /api/sync/status
 */
export const verificarEstadoSync = async (req, res) => {
  try {
    // Verificar conexión a MongoDB
    const mongoStatus = mongoose.connection.readyState === 1 ? 'connected' : 'disconnected';

    return res.status(200).json({
      success: true,
      message: 'Estado del servicio de sincronización',
      data: {
        servicioActivo: true,
        mongoDBStatus: mongoStatus,
        timestamp: new Date().toISOString(),
        version: '1.0.0'
      }
    });

  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al verificar estado',
      error: { code: 500, details: error.message }
    });
  }
};

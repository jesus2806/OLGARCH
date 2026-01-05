// controllers/ordenProductoController.js
import OrdenProducto from '../models/ordenProducto.js';
import Orden from '../models/orden.js';
import Producto from '../models/producto.js';
import mongoose from 'mongoose';

const mapOrdenProductoData = (data) => {
  return {
    // ✅ NUEVO: id real del Producto (catálogo) para receta/ingredientes
    sIdProductoMongoDB: data.sIdProductoMongoDB
      ? new mongoose.Types.ObjectId(data.sIdProductoMongoDB)
      : undefined,

    sIdOrdenMongoDB: data.sIdOrdenMongoDB,
    sNombre: data.sNombre,
    iCostoReal: data.iCostoReal,
    iCostoPublico: data.iCostoPublico,
    sURLImagen: data.sURLImagen,
    sIndicaciones: data.sIndicaciones,
    iIndexVarianteSeleccionada: data.iIndexVarianteSeleccionada,
    aVariantes: Array.isArray(data.aVariantes)
      ? data.aVariantes.map((variant) => ({ sVariante: variant.sVariante }))
      : [],
    iCantidad: data.iCantidad || 1,
    aExtras: Array.isArray(data.aExtras)
      ? data.aExtras.map((extra) => ({
          sNombre: extra.sNombre,
          iCostoReal: extra.iCostoReal,
          iCostoPublico: extra.iCostoPublico,
          sURLImagen: extra.sURLImagen,
        }))
      : [],
    aConsumos: Array.isArray(data.aConsumos)
      ? data.aConsumos.map((consumo) => ({
          iIndex: consumo.iIndex,
          aExtras: Array.isArray(consumo.aExtras)
            ? consumo.aExtras.map((extra) => ({
                sIdExtra: extra.sIdExtra,
                sNombre: extra.sNombre,
                iCostoReal: extra.iCostoReal,
                iCostoPublico: extra.iCostoPublico,
                sURLImagen: extra.sURLImagen,
              }))
            : [],
        }))
      : [],
    iTipoProducto: data.iTipoProducto,
  };
};

/**
 * Crear un nuevo producto de orden
 */
export const createOrdenProducto = async (req, res) => {
  try {
    // ✅ VALIDACIÓN: debe venir el id del producto real del catálogo
    if (!req.body?.sIdProductoMongoDB) {
      return res.status(400).json({
        success: false,
        message: 'Falta sIdProductoMongoDB (id del Producto del catálogo).',
        error: { code: 400, details: 'Debes mandar sIdProductoMongoDB para poder descontar ingredientes.' }
      });
    }

    // ✅ VALIDACIÓN: que sea ObjectId válido
    if (!mongoose.Types.ObjectId.isValid(req.body.sIdProductoMongoDB)) {
      return res.status(400).json({
        success: false,
        message: 'sIdProductoMongoDB no es un ObjectId válido.',
        error: { code: 400, details: 'Formato inválido para sIdProductoMongoDB.' }
      });
    }

    // ✅ VALIDACIÓN: que exista el Producto en catálogo (para receta)
    const existeProducto = await Producto.exists({ _id: req.body.sIdProductoMongoDB });
    if (!existeProducto) {
      return res.status(404).json({
        success: false,
        message: 'El Producto del catálogo no existe.',
        error: { code: 404, details: 'No se encontró Producto con ese sIdProductoMongoDB.' }
      });
    }

    const mappedData = mapOrdenProductoData(req.body);

    const nuevoProducto = new OrdenProducto(mappedData);
    const productoGuardado = await nuevoProducto.save();

    // ✅ usa await para que errores se manejen en este try/catch
    await Orden.findByIdAndUpdate(
      productoGuardado.sIdOrdenMongoDB,
      { $push: { aProductos: productoGuardado._id } },
      { new: true }
    );

    return res.status(201).json({
      success: true,
      message: 'Producto de orden creado exitosamente',
      data: productoGuardado
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al crear el producto de orden',
      error: { code: 500, details: error.message }
    });
  }
};

/**
 * Obtener todos los productos de orden
 */
export const getOrdenProductos = async (req, res) => {
  try {
    const { idOrden } = req.params;
    const productos = await OrdenProducto.find({ sIdOrdenMongoDB: idOrden });

    return res.status(200).json({
      success: true,
      message: 'Lista de productos obtenida exitosamente',
      data: productos
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al obtener productos de orden',
      error: {
        code: 500,
        details: error.message,
      }
    });
  }
};

/**
 * Obtener un producto de orden por ID
 */
export const getOrdenProductoById = async (req, res) => {
  try {
    const { id } = req.params;
    const producto = await OrdenProducto.findById(id);

    if (!producto) {
      return res.status(404).json({
        success: false,
        message: 'Producto de orden no encontrado',
        error: {
          code: 404,
          details: 'Producto de orden no encontrado',
        }
      });
    }

    return res.status(200).json({
      success: true,
      message: 'Producto de orden obtenido exitosamente',
      data: producto
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al obtener el producto de orden',
      error: {
        code: 500,
        details: error.message,
      }
    });
  }
};

/**
 * Actualizar un producto de orden por ID
 */
export const updateOrdenProducto = async (req, res) => {
  try {
    const { id } = req.params;

    // (Opcional) si te mandan sIdProductoMongoDB, validar que exista
    if (req.body?.sIdProductoMongoDB) {
      if (!mongoose.Types.ObjectId.isValid(req.body.sIdProductoMongoDB)) {
        return res.status(400).json({
          success: false,
          message: 'sIdProductoMongoDB no es un ObjectId válido.',
          error: { code: 400, details: 'Formato inválido para sIdProductoMongoDB.' }
        });
      }
      const existeProducto = await Producto.exists({ _id: req.body.sIdProductoMongoDB });
      if (!existeProducto) {
        return res.status(404).json({
          success: false,
          message: 'El Producto del catálogo no existe.',
          error: { code: 404, details: 'No se encontró Producto con ese sIdProductoMongoDB.' }
        });
      }
    }

    const mappedData = mapOrdenProductoData(req.body);
    const productoActualizado = await OrdenProducto.findByIdAndUpdate(id, mappedData, { new: true });

    if (!productoActualizado) {
      return res.status(404).json({
        success: false,
        message: 'Producto de orden no encontrado',
        error: { code: 404, details: 'Producto de orden no encontrado' }
      });
    }

    return res.status(200).json({
      success: true,
      message: 'Producto de orden actualizado exitosamente',
      data: productoActualizado
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al actualizar el producto de orden',
      error: { code: 500, details: error.message }
    });
  }
};

/**
 * Eliminar un producto de orden por ID
 */
export const deleteOrdenProducto = async (req, res) => {
  try {
    const { id } = req.params;
    const productoEliminado = await OrdenProducto.findByIdAndDelete(id);

    if (!productoEliminado) {
      return res.status(404).json({
        success: false,
        message: 'Producto de orden no encontrado',
        error: {
          code: 404,
          details: 'Producto de orden no encontrado',
        }
      });
    }

    // Remover la referencia del producto en el arreglo aProductos de la Orden
    await Orden.findByIdAndUpdate(
      productoEliminado.sIdOrdenMongoDB,
      { $pull: { aProductos: productoEliminado._id } },
      { new: true }
    );

    return res.status(200).json({
      success: true,
      message: 'Producto de orden eliminado exitosamente',
      data: productoEliminado,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al eliminar el producto de orden',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

// ============================================================
// NUEVOS ENDPOINTS PARA GESTIÓN DE CONSUMOS Y EXTRAS (Mockups)
// ============================================================

/**
 * Obtener consumos de un producto (Pantalla 2)
 * GET /api/orden-productos/:id/consumos
 * 
 * Retorna la lista de consumos individuales con sus extras
 */
export const getConsumos = async (req, res) => {
  try {
    const { id } = req.params;
    const producto = await OrdenProducto.findById(id);

    if (!producto) {
      return res.status(404).json({
        success: false,
        message: 'Producto de orden no encontrado',
        error: {
          code: 404,
          details: 'Producto de orden no encontrado'
        }
      });
    }

    return res.status(200).json({
      success: true,
      message: 'Consumos obtenidos exitosamente',
      data: {
        sIdOrdenProducto: producto._id,
        sNombre: producto.sNombre,
        iCantidad: producto.iCantidad,
        aConsumos: producto.aConsumos,
        iTotalExtras: producto.iTotalPublicoExtrasConsumos
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al obtener consumos',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};

/**
 * Agregar extra a consumos específicos (Pantalla 4)
 * POST /api/orden-productos/:id/consumos/extras
 * 
 * Body:
 * {
 *   "extra": {
 *     "sIdExtra": "optional_mongo_id",
 *     "sNombre": "Pollo",
 *     "iCostoReal": 25,
 *     "iCostoPublico": 30,
 *     "sURLImagen": "url"
 *   },
 *   "aIndexConsumos": [1, 3] // Índices de consumos a los que aplicar el extra
 * }
 * 
 * Nota: Si un consumo ya tiene ese extra, se descarta silenciosamente
 */
export const addExtraToConsumos = async (req, res) => {
  try {
    const { id } = req.params;
    const { extra, aIndexConsumos } = req.body;

    if (!extra || !aIndexConsumos || !Array.isArray(aIndexConsumos)) {
      return res.status(400).json({
        success: false,
        message: 'Datos incompletos',
        error: {
          code: 400,
          details: 'Se requiere extra y aIndexConsumos (array de índices)'
        }
      });
    }

    const producto = await OrdenProducto.findById(id);

    if (!producto) {
      return res.status(404).json({
        success: false,
        message: 'Producto de orden no encontrado',
        error: {
          code: 404,
          details: 'Producto de orden no encontrado'
        }
      });
    }

    // Asegurar que aConsumos existe
    if (!producto.aConsumos || producto.aConsumos.length === 0) {
      // Inicializar consumos si no existen
      producto.aConsumos = [];
      for (let i = 1; i <= producto.iCantidad; i++) {
        producto.aConsumos.push({ iIndex: i, aExtras: [] });
      }
    }

    let extrasAgregados = 0;
    let extrasDescartados = 0;

    // Agregar el extra a cada consumo especificado
    for (const indexConsumo of aIndexConsumos) {
      const consumo = producto.aConsumos.find(c => c.iIndex === indexConsumo);
      
      if (consumo) {
        // Verificar si el extra ya existe en este consumo (por nombre)
        const extraExistente = consumo.aExtras.find(e => e.sNombre === extra.sNombre);
        
        if (!extraExistente) {
          // Agregar el extra
          consumo.aExtras.push({
            sIdExtra: extra.sIdExtra ? new mongoose.Types.ObjectId(extra.sIdExtra) : undefined,
            sNombre: extra.sNombre,
            iCostoReal: extra.iCostoReal,
            iCostoPublico: extra.iCostoPublico,
            sURLImagen: extra.sURLImagen
          });
          extrasAgregados++;
        } else {
          // Descartar silenciosamente
          extrasDescartados++;
        }
      }
    }

    await producto.save();

    return res.status(200).json({
      success: true,
      message: `Extra agregado exitosamente. Agregados: ${extrasAgregados}, Descartados (duplicados): ${extrasDescartados}`,
      data: {
        sIdOrdenProducto: producto._id,
        aConsumos: producto.aConsumos,
        iTotalExtras: producto.iTotalPublicoExtrasConsumos,
        extrasAgregados,
        extrasDescartados
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al agregar extra a consumos',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};

/**
 * Eliminar extra de un consumo específico (Pantalla 2 - ícono basura)
 * DELETE /api/orden-productos/:id/consumos/:indexConsumo/extras/:idExtra
 */
export const removeExtraFromConsumo = async (req, res) => {
  try {
    const { id, indexConsumo, idExtra } = req.params;

    const producto = await OrdenProducto.findById(id);

    if (!producto) {
      return res.status(404).json({
        success: false,
        message: 'Producto de orden no encontrado',
        error: {
          code: 404,
          details: 'Producto de orden no encontrado'
        }
      });
    }

    const consumo = producto.aConsumos.find(c => c.iIndex === parseInt(indexConsumo));

    if (!consumo) {
      return res.status(404).json({
        success: false,
        message: 'Consumo no encontrado',
        error: {
          code: 404,
          details: `No se encontró el consumo con índice ${indexConsumo}`
        }
      });
    }

    // Buscar y eliminar el extra por su _id
    const extraIndex = consumo.aExtras.findIndex(e => e._id.toString() === idExtra);

    if (extraIndex === -1) {
      return res.status(404).json({
        success: false,
        message: 'Extra no encontrado en el consumo',
        error: {
          code: 404,
          details: 'El extra especificado no existe en este consumo'
        }
      });
    }

    const extraEliminado = consumo.aExtras.splice(extraIndex, 1)[0];
    await producto.save();

    return res.status(200).json({
      success: true,
      message: 'Extra eliminado exitosamente del consumo',
      data: {
        extraEliminado,
        consumoActualizado: consumo,
        iTotalExtras: producto.iTotalPublicoExtrasConsumos
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al eliminar extra del consumo',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};

/**
 * Eliminar un consumo específico (cuando tiene extras y se decrementa cantidad)
 * DELETE /api/orden-productos/:id/consumos/:indexConsumo
 * 
 * Este endpoint permite eliminar un consumo específico cuando el producto
 * tiene extras asociados (Escenario 3 del mockup)
 */
export const deleteConsumo = async (req, res) => {
  try {
    const { id, indexConsumo } = req.params;

    const producto = await OrdenProducto.findById(id);

    if (!producto) {
      return res.status(404).json({
        success: false,
        message: 'Producto de orden no encontrado',
        error: {
          code: 404,
          details: 'Producto de orden no encontrado'
        }
      });
    }

    const consumoIndex = producto.aConsumos.findIndex(c => c.iIndex === parseInt(indexConsumo));

    if (consumoIndex === -1) {
      return res.status(404).json({
        success: false,
        message: 'Consumo no encontrado',
        error: {
          code: 404,
          details: `No se encontró el consumo con índice ${indexConsumo}`
        }
      });
    }

    // Eliminar el consumo
    const consumoEliminado = producto.aConsumos.splice(consumoIndex, 1)[0];

    // Decrementar la cantidad
    producto.iCantidad = Math.max(0, producto.iCantidad - 1);

    // Re-indexar los consumos restantes
    producto.aConsumos.forEach((consumo, idx) => {
      consumo.iIndex = idx + 1;
    });

    // Si la cantidad llegó a 0, eliminar el producto completo
    if (producto.iCantidad === 0) {
      await OrdenProducto.findByIdAndDelete(id);
      await Orden.findByIdAndUpdate(
        producto.sIdOrdenMongoDB,
        { $pull: { aProductos: producto._id } }
      );

      return res.status(200).json({
        success: true,
        message: 'Producto eliminado completamente (cantidad llegó a 0)',
        data: {
          productoEliminado: true,
          consumoEliminado
        }
      });
    }

    await producto.save();

    return res.status(200).json({
      success: true,
      message: 'Consumo eliminado exitosamente',
      data: {
        consumoEliminado,
        iCantidadActual: producto.iCantidad,
        aConsumos: producto.aConsumos,
        iTotalExtras: producto.iTotalPublicoExtrasConsumos
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al eliminar consumo',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};

/**
 * Actualizar cantidad de un producto (Pantalla 1 - incrementar/decrementar)
 * PATCH /api/orden-productos/:id/cantidad
 * 
 * Body: { "iCantidad": 5 }
 * 
 * Retorna información sobre si el producto tiene extras para manejar
 * el Escenario 3 (redirección a pantalla de administración de extras)
 */
export const updateCantidad = async (req, res) => {
  try {
    const { id } = req.params;
    const { iCantidad } = req.body;

    if (iCantidad === undefined || iCantidad < 0) {
      return res.status(400).json({
        success: false,
        message: 'Cantidad inválida',
        error: {
          code: 400,
          details: 'La cantidad debe ser un número >= 0'
        }
      });
    }

    const producto = await OrdenProducto.findById(id);

    if (!producto) {
      return res.status(404).json({
        success: false,
        message: 'Producto de orden no encontrado',
        error: {
          code: 404,
          details: 'Producto de orden no encontrado'
        }
      });
    }

    const cantidadAnterior = producto.iCantidad;
    const tieneExtras = producto.bTieneExtras;

    // Si se está decrementando y hay extras, notificar al frontend
    if (iCantidad < cantidadAnterior && tieneExtras) {
      return res.status(200).json({
        success: true,
        message: 'El producto tiene extras asociados. Debe administrar los consumos manualmente.',
        data: {
          requiereAdminConsumos: true,
          sIdOrdenProducto: producto._id,
          iCantidadActual: cantidadAnterior,
          iCantidadSolicitada: iCantidad,
          bTieneExtras: true,
          aConsumos: producto.aConsumos
        }
      });
    }

    // Si la cantidad es 0, eliminar el producto
    if (iCantidad === 0) {
      await OrdenProducto.findByIdAndDelete(id);
      await Orden.findByIdAndUpdate(
        producto.sIdOrdenMongoDB,
        { $pull: { aProductos: producto._id } }
      );

      return res.status(200).json({
        success: true,
        message: 'Producto eliminado (cantidad = 0)',
        data: {
          productoEliminado: true
        }
      });
    }

    // Actualizar cantidad
    producto.iCantidad = iCantidad;
    await producto.save();

    return res.status(200).json({
      success: true,
      message: 'Cantidad actualizada exitosamente',
      data: {
        sIdOrdenProducto: producto._id,
        iCantidadAnterior: cantidadAnterior,
        iCantidadActual: producto.iCantidad,
        aConsumos: producto.aConsumos,
        iTotalExtras: producto.iTotalPublicoExtrasConsumos
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al actualizar cantidad',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};

/**
 * Verificar si un producto tiene extras (útil para el frontend antes de decrementar)
 * GET /api/orden-productos/:id/tiene-extras
 */
export const checkTieneExtras = async (req, res) => {
  try {
    const { id } = req.params;
    const producto = await OrdenProducto.findById(id);

    if (!producto) {
      return res.status(404).json({
        success: false,
        message: 'Producto de orden no encontrado',
        error: {
          code: 404,
          details: 'Producto de orden no encontrado'
        }
      });
    }

    return res.status(200).json({
      success: true,
      message: 'Verificación completada',
      data: {
        sIdOrdenProducto: producto._id,
        bTieneExtras: producto.bTieneExtras,
        iCantidad: producto.iCantidad,
        iTotalExtras: producto.iTotalPublicoExtrasConsumos
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al verificar extras',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};

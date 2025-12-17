import Orden from '../models/orden.js';
import Usuario from '../models/usuario.js';
// import OrdenProducto from '../models/OrdenProducto.js';
import OrdenProducto from '../models/ordenProducto.js';
import mongoose from 'mongoose';

/**
 * Crear una nueva orden
 */
export const createOrden = async (req, res) => {
  try {
    const {sIdentificadorOrden, iMesa, iTipoOrden, sUsuarioMesero, sIdMongoDBMesero} = req.body;
    
    if (sIdentificadorOrden == null || iMesa == null || iTipoOrden == null || sUsuarioMesero == null || sIdMongoDBMesero == null) {
      return res.status(500).json({
        success: false,
        message: 'Faltan datos para dar de alta una orden.',
        error: {
          code: 500,
          details: 'Faltan datos para dar de alta una orden.',
        }
      });
    }

    const nuevaOrden = new Orden({
        sIdentificadorOrden: sIdentificadorOrden,
        iMesa: iMesa,
        iTipoOrden: iTipoOrden,
        sUsuarioMesero: sUsuarioMesero,
        sIdMongoDBMesero: sIdMongoDBMesero
    });

    const ordenGuardada = await nuevaOrden.save();
    ordenGuardada.oMesero = new Usuario();
    return res.status(201).json({
      success: true,
      message: 'Orden creada exitosamente',
      data: ordenGuardada
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al crear la orden',
      error: {
        code: 500,
        details: error.message,
      }
    });
  }
};

/**
 * Obtener todas las órdenes
 */
export const getOrdenes = async (req, res) => {
  try {
    // // Obtener la fecha actual
    // const hoy = new Date();
    // // Calcular el inicio y fin del día en México (UTC-6)
    // const mexicoStart = new Date(Date.UTC(hoy.getFullYear(), hoy.getMonth(), hoy.getDate(), 6, 0, 0));
    // const mexicoEnd = new Date(Date.UTC(hoy.getFullYear(), hoy.getMonth(), hoy.getDate() + 1, 6, 0, 0));

    // Consulta: órdenes con dtFechaAlta dentro del día en curso o con iEstatus distinto de 5
    // const ordenes = await Orden.find({
    //   $and: [
    //     { dtFechaAlta: { $gte: mexicoStart, $lt: mexicoEnd } },
    //     { iEstatus: { $ne: 5 } }
    //   ]
    // })
    //   .populate('aProductos')
    //   .populate({
    //     path: 'ordenesSecundarias',
    //     populate: { path: 'aProductos' }
    //   });


      const ordenes = await Orden.find(
          { iEstatus: { $ne: 5 } })
        .populate('aProductos')
        .populate({
          path: 'ordenesSecundarias',
          populate: { path: 'aProductos' }
        });



    // Mapear cada orden para extraer solo los campos requeridos
    const data = ordenes.map(orden => ({
      _id: orden._id,
      sIdentificadorOrden: orden.sIdentificadorOrden,
      iNumeroOrden: orden.iNumeroOrden,
      iMesa: orden.iMesa,
      sUsuarioMesero: orden.sUsuarioMesero,
      dtFechaAlta: orden.dtFechaAlta,
      // Se utiliza la propiedad virtual 'totalPublicoOrden' (si no está definida, se podría usar 'iTotalPublicoOrden')
      iTotalOrden: orden.iTotalPublicoOrden,
      iTotalOrdenCostoReal: orden.iTotalRealOrden,
      iTipoOrden: orden.iTipoOrden,
      iEstatus: orden.iEstatus
    }));

    return res.status(200).json({
      success: true,
      message: 'Lista de órdenes obtenida exitosamente',
      data
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al obtener órdenes',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

export const confirmarPagoOrden = async (req, res) => {
  const session = await mongoose.startSession();

  try {
    const { id } = req.params; // ID de la orden primaria
    const { iTipoPago } = req.body; // Valor del tipo de pago a actualizar

    // Iniciar la transacción
    session.startTransaction();

    // Buscar la orden primaria y popular sus órdenes secundarias
    const ordenPrimaria = await Orden.findById(id)
      .populate('ordenesSecundarias')
      .session(session);

    if (!ordenPrimaria) {
      await session.abortTransaction();
      session.endSession();
      return res.status(404).json({
        success: false,
        message: 'Orden no encontrada',
        error: { code: 404, details: 'La orden primaria no existe' }
      });
    }

    // Verificar que la orden primaria esté en estatus "Entregada" (4)
    if (ordenPrimaria.iEstatus !== 4) {
      await session.abortTransaction();
      session.endSession();
      return res.status(400).json({
        success: false,
        message: 'La orden primaria no se encuentra en estatus entregada',
        error: { code: 400, details: 'El estatus de la orden primaria debe ser 4 (Entregada)' }
      });
    }

    // Verificar que todas las órdenes secundarias, si existen, estén en estatus "Entregada" (4)
    if (ordenPrimaria.ordenesSecundarias && ordenPrimaria.ordenesSecundarias.length > 0) {
      const noEntregadas = ordenPrimaria.ordenesSecundarias.filter(ordenSec => ordenSec.iEstatus !== 4);
      if (noEntregadas.length > 0) {
        await session.abortTransaction();
        session.endSession();
        return res.status(400).json({
          success: false,
          message: 'No todas las órdenes secundarias se encuentran en estatus entregada',
          error: { code: 400, details: 'Verifica que todas las órdenes secundarias tengan estatus de Entregada.' }
        });
      }
    }

    // Actualizar todas las órdenes (primaria y secundarias) que compartan el mismo sIdentificadorOrden
    await Orden.updateMany(
      { sIdentificadorOrden: ordenPrimaria.sIdentificadorOrden },
      { iEstatus: 5, iTipoPago: iTipoPago },
      { session }
    );

    // Confirmar la transacción
    await session.commitTransaction();
    session.endSession();

    return res.status(200).json({
      success: true,
      message: 'Todas las órdenes han sido actualizadas a Pagada y se asignó el tipo de pago indicado.',
      data: { sIdentificadorOrden: ordenPrimaria.sIdentificadorOrden }
    });
  } catch (error) {
    // En caso de error, revertir la transacción
    await session.abortTransaction();
    session.endSession();

    return res.status(500).json({
      success: false,
      message: 'Error al actualizar el estatus de la orden',
      error: { code: 500, details: error.message }
    });
  }
};


export const verifyOrdenStatus = async (req, res) => {
  try {
    const { id } = req.params;
    
    // Buscar la orden primaria e incluir las órdenes secundarias mediante populate
    const orden = await Orden.findById(id).populate('ordenesSecundarias');
    if (!orden) {
      return res.status(404).json({
        success: false,
        message: 'Orden no encontrada',
        error: {
          code: 404,
          details: 'Orden no encontrada'
        }
      });
    }
    
    // Arreglo para almacenar las órdenes que no estén en estatus 4
    const ordersNotStatus4 = [];

    // Verificar la orden primaria
    if (orden.iEstatus !== 4) {
      ordersNotStatus4.push({
        ordenNumber: ordenSec.iNumeroOrden,
        type: 'Primaria',
        status: orden.iEstatus
      });
    }

    // Verificar las órdenes secundarias, en caso de existir
    if (orden.ordenesSecundarias && orden.ordenesSecundarias.length > 0) {
      orden.ordenesSecundarias.forEach((ordenSec) => {
        if (ordenSec.iEstatus !== 4) {
          ordersNotStatus4.push({
            ordenNumber: ordenSec.iNumeroOrden,
            type: 'Secundaria',
            status: ordenSec.iEstatus
          });
        }
      });
    }

    // Si el arreglo tiene elementos, significa que hay órdenes que no están en estatus 4
    if (ordersNotStatus4.length > 0) {
      return res.status(200).json({
        success: false,
        message: 'No todas las órdenes están en estatus 4',
        data: ordersNotStatus4
      });
    }

    // Todas las órdenes (primaria y secundarias) están en estatus 4
    return res.status(200).json({
      success: true,
      message: 'Todas las órdenes están en estatus 4'
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al verificar el estatus de la orden',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};



/**
 * Obtener una orden por ID
 */
export const getOrdenById = async (req, res) => {
  try {
    const { id } = req.params;
    const orden = await Orden.findById(id).populate('aProductos').populate('ordenesSecundarias');

    if (!orden) {
      return res.status(404).json({
        success: false,
        message: 'Orden no encontrada',
        error: {
          code: 404,
          details: error.message,
        },
      });
    }

    return res.status(200).json({
      success: true,
      message: 'Orden obtenida exitosamente',
      data: orden
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al obtener la orden',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

/**
 * Actualizar una orden por ID
 */
export const updateOrden = async (req, res) => {
  try {
    const { id } = req.params;
    const ordenActualizada = await Orden.findByIdAndUpdate(id, req.body, { new: true });

    if (!ordenActualizada) {
      return res.status(404).json({
        success: false,
        message: 'Orden no encontrada',
        error: {
          code: 404,
          details: error.message,
        },
      });
    }

    return res.status(200).json({
      success: true,
      message: 'Orden actualizada exitosamente',
      data: ordenActualizada
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al actualizar la orden',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

/**
 * Eliminar una orden por ID
 */
export const deleteOrden = async (req, res) => {
  // Inicia una sesión
  const session = await mongoose.startSession();

  try {
    const { id } = req.params;
    
    // Inicia la transacción
    session.startTransaction();

    // Eliminar la orden utilizando la sesión de transacción
    const ordenEliminada = await Orden.findByIdAndDelete(id, { session });
    if (!ordenEliminada) {
      // Si no se encontró la orden, aborta la transacción y cierra la sesión
      await session.abortTransaction();
      session.endSession();
      return res.status(404).json({
        success: false,
        message: 'Orden no encontrada',
        error: {
          code: 404,
          details: 'La orden no existe',
        },
      });
    }

    // Eliminar todos los productos asociados a la orden eliminada
    await OrdenProducto.deleteMany(
      { sIdOrdenMongoDB: ordenEliminada._id },
      { session }
    );

    // Si la orden eliminada es primaria, eliminar también sus órdenes secundarias
    if (ordenEliminada.iTipoOrden === 1) {
      // Buscar las órdenes secundarias que tengan el mismo identificador y iTipoOrden: 2
      const ordenesSecundarias = await Orden.find(
        { 
          sIdentificadorOrden: ordenEliminada.sIdentificadorOrden, 
          iTipoOrden: 2 
        },
        '_id',
        { session }
      );

      if (ordenesSecundarias && ordenesSecundarias.length > 0) {
        const idsOrdenesSecundarias = ordenesSecundarias.map(o => o._id);
        
        // Eliminar los productos asociados a las órdenes secundarias
        await OrdenProducto.deleteMany(
          { sIdOrdenMongoDB: { $in: idsOrdenesSecundarias } },
          { session }
        );
        
        // Eliminar las órdenes secundarias
        await Orden.deleteMany(
          { _id: { $in: idsOrdenesSecundarias } },
          { session }
        );
      }
    }

    // Si todo salió bien, se comete (confirma) la transacción
    await session.commitTransaction();
    session.endSession();

    return res.status(200).json({
      success: true,
      message: 'Orden y productos asociados eliminados exitosamente',
      data: {
        orden: ordenEliminada,
      }
    });
  } catch (error) {
    // En caso de error, se revierte la transacción
    await session.abortTransaction();
    session.endSession();

    return res.status(500).json({
      success: false,
      message: 'Error al eliminar la orden',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};


/**
 * Obtener números de mesa de órdenes con iEstatus diferente a 4
 */
export const getMesasConOrdenesActivas = async (req, res) => {
  try {
    // Utilizamos el método distinct para obtener valores únicos de iMesa
    const mesas = await Orden.distinct('iMesa', { iEstatus: { $ne: 5 } });
    return res.status(200).json({
      success: true,
      message: 'Números de mesa obtenidos exitosamente',
      data: mesas
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al obtener los números de mesa',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};


export async function getInfoTicket(req, res) {
  try {
    const { id } = req.params;

    // 1. Buscar la orden y popular: aProductos, ordenesSecundarias + sus aProductos
    const orden = await Orden.findById(id)
      .populate({
        path: 'aProductos',
        model: 'OrdenProducto'
      })
      .populate({
        path: 'ordenesSecundarias',
        populate: {
          path: 'aProductos',
          model: 'OrdenProducto'
        }
      })
      .exec();

    if (!orden) {
      return res.status(404).json({ 
        success: false, 
        message: 'Orden no encontrada' 
      });
    }

    // 2. El virtual iTotalPublicoOrden ya suma orden primaria + secundarias
    const totalPublicoCompleto = orden.iTotalPublicoOrden;

    // 3. Combinar los productos de la orden primaria y secundarios en un solo array
    const productosPrimaria = orden.aProductos || [];
    const productosSecundarias = (orden.ordenesSecundarias || []).flatMap(sec => sec.aProductos || []);
    const allProductos = [...productosPrimaria, ...productosSecundarias];

    // 4. Vamos a crear una lista temporal que incluya:
    //    - Un "item" por cada producto base
    //    - Un "item" por cada extra (así podemos agruparlos también)
    const itemsParaAgrupar = [];

    for (const p of allProductos) {
      const cantidadProducto = p.iCantidad || 1;

      // 4a. Agregamos el producto base
      itemsParaAgrupar.push({
        nombre: p.sNombre,
        costoPublico: p.iCostoPublico,
        cantidad: cantidadProducto,
        esExtra: false  // Marca para saber que es producto base
      });

      // 4b. Agregamos cada extra como un item separado
      if (p.aExtras && p.aExtras.length > 0) {
        for (const e of p.aExtras) {
          // Suponiendo que el extra también se repite `cantidadProducto` veces
          itemsParaAgrupar.push({
            nombre: e.sNombre,
            costoPublico: e.iCostoPublico,
            cantidad: cantidadProducto,
            esExtra: true
          });
        }
      }
    }

    // 5. Agrupar todos estos items por (nombre, costoPublico, esExtra)
    //    y sumar "cantidad"
    const mapAgrupado = {};
    for (const item of itemsParaAgrupar) {
      // Creamos la clave de agrupación
      const key = `${item.esExtra ? 'EXTRA:' : 'PROD:'}__${item.nombre}__${item.costoPublico}`;
      if (!mapAgrupado[key]) {
        mapAgrupado[key] = {
          nombre: item.nombre,
          costoPublico: item.costoPublico,
          cantidad: 0,
          esExtra: item.esExtra
        };
      }
      mapAgrupado[key].cantidad += item.cantidad;
    }

    // 6. Convertir mapAgrupado en un arreglo final "elementos"
    const elementos = Object.values(mapAgrupado);

    // 7. Construir el objeto de respuesta
    const resultado = {
      mesa: orden.iMesa,
      mesero: orden.sUsuarioMesero,
      fechaAlta: orden.dtFechaAlta,
      totalPublico: totalPublicoCompleto,
      elementos
    };

    // Respuesta estandarizada
    return res.status(200).json({
      success: true,
      message: 'Información del ticket obtenida exitosamente',
      data: resultado
    });

  } catch (error) {
    console.error(error);
    return res.status(500).json({
      success: false,
      message: 'Error interno al obtener la orden',
      error: error.message
    });
  }
}




// PROBAR 

/**
 * Obtener órdenes vigentes (del día actual, sin fecha fin)
 */
export const getOrdenesVigentes = async (req, res) => {
  try {
    // Tomamos la fecha de "hoy"
    const now = new Date();
    const startOfDay = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 0, 0, 0);
    const endOfDay = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59);

    // Consulta: las órdenes que tengan dtFechaAlta entre startOfDay y endOfDay,
    // y que NO tengan dtFechaFin (o sea null/indefinida).
    const ordenesVigentes = await Orden.find({
      dtFechaAlta: {
        $gte: startOfDay,
        $lte: endOfDay
      },
      // Filtras por dtFechaFin nulo o ausente
      dtFechaFin: { $exists: false }
    }).populate('aProductos');

    return res.status(200).json({
      success: true,
      message: 'Órdenes vigentes obtenidas exitosamente',
      data: ordenesVigentes
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al obtener órdenes vigentes',
      error: error.message
    });
  }
};



/**
 * Obtener órdenes en un rango de fechas y el total general
 */
export const postOrdenesByDateRange = async (req, res) => {
  try {
    const { startDate, endDate } = req.body;
    if (!startDate || !endDate) {
      return res.status(400).json({
        success: false,
        message: 'Faltan parámetros startDate o endDate en el body'
      });
    }
    // Convertimos a tipo Date
    const start = new Date(startDate);
    const end = new Date(endDate);
    // Ajustamos end al final del día (23:59:59.999)
    end.setHours(23, 59, 59, 999);

    // Buscamos las órdenes cuyo dtFechaAlta esté dentro del rango
    const ordenes = await Orden.find({
      dtFechaAlta: {
        $gte: start,
        $lte: end
      }
    }).populate('aProductos');

    // Calculamos el total general de iTotalOrden
    const totalGeneral = ordenes.reduce((acumulado, orden) => {
      return acumulado + orden.iTotalOrden;
    }, 0);

    return res.status(200).json({
      success: true,
      message: 'Órdenes encontradas en el rango de fechas',
      data: {
        ordenes,
        totalGeneral
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al obtener órdenes por rango de fechas',
      error: error.message
    });
  }
};



/**
 * Obtener productos vendidos por rango de fechas, agrupados por ID
 * y con un total general de la suma de todos los productos
 */
export const postProductosVendidosByDateRange = async (req, res) => {
  try {
    const { startDate, endDate } = req.body;
    if (!startDate || !endDate) {
      return res.status(400).json({
        success: false,
        message: 'Faltan parámetros startDate o endDate en el body'
      });
    }

    const start = new Date(startDate);
    const end = new Date(endDate);
    end.setHours(23, 59, 59, 999);

    // Construimos el pipeline de agregación
    const pipeline = [
      // 1) Filtrar Órdenes en el rango de fechas
      { 
        $match: {
          dtFechaAlta: { $gte: start, $lte: end }
        }
      },
      // 2) "Desenrollar" el arreglo aProductos
      {
        $unwind: '$aProductos'
      },
      // 3) Unir con la colección OrdenProducto para obtener datos de cada producto
      {
        $lookup: {
          from: 'ordenproductos',           // Nombre de la colección (en minúscula y plural, según tu config)
          localField: 'aProductos',
          foreignField: '_id',
          as: 'productoInfo'
        }
      },
      {
        $unwind: '$productoInfo'
      },
      // 4) Agrupar por el _id del producto y sumar su iCostoPublico (como ejemplo de "total vendido")
      {
        $group: {
          _id: '$productoInfo._id',
          sNombre: { $first: '$productoInfo.sNombre' },
          totalVendido: { $sum: '$productoInfo.iCostoPublico' },
          cantidadVendida: { $sum: 1 }
        }
      },
      // 5) Segundo agrupamiento para obtener total global y "empaquetar" resultados
      {
        $group: {
          _id: null,
          productos: {
            $push: {
              _id: '$_id',
              sNombre: '$sNombre',
              totalVendido: '$totalVendido'
            }
          },
          totalGeneral: { $sum: '$totalVendido' }
        }
      },
      // 6) Retornamos un objeto más limpio (sin _id a nivel global)
      {
        $project: {
          _id: 0,
          productos: 1,
          totalGeneral: 1
        }
      }
    ];

    // Ejecutamos la agregación sobre la colección de órdenes
    const resultado = await Orden.aggregate(pipeline);

    // Por cómo está definido el pipeline, `resultado` será un arreglo
    // con un solo elemento que contiene { productos, totalGeneral }
    // o puede venir vacío si no hay coincidencias.
    const data = resultado.length > 0 ? resultado[0] : { productos: [], totalGeneral: 0 };

    return res.status(200).json({
      success: true,
      message: 'Productos vendidos en el rango de fechas',
      data
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al obtener productos vendidos',
      error: error.message
    });
  }
};


// ============================================================
// NUEVOS ENDPOINTS PARA MOCKUPS
// ============================================================

/**
 * Actualizar indicaciones de una orden (Pantalla 6 - Bottom Sheet)
 * PATCH /api/orden/:id/indicaciones
 * 
 * Body: { "sIndicaciones": "Una orden de enchiladas sin..." }
 */
export const updateIndicaciones = async (req, res) => {
  try {
    const { id } = req.params;
    const { sIndicaciones } = req.body;

    if (sIndicaciones === undefined) {
      return res.status(400).json({
        success: false,
        message: 'Falta el campo sIndicaciones',
        error: {
          code: 400,
          details: 'Se requiere el campo sIndicaciones en el body'
        }
      });
    }

    const ordenActualizada = await Orden.findByIdAndUpdate(
      id,
      { sIndicaciones },
      { new: true }
    );

    if (!ordenActualizada) {
      return res.status(404).json({
        success: false,
        message: 'Orden no encontrada',
        error: {
          code: 404,
          details: 'La orden especificada no existe'
        }
      });
    }

    return res.status(200).json({
      success: true,
      message: 'Indicaciones actualizadas exitosamente',
      data: {
        _id: ordenActualizada._id,
        sIndicaciones: ordenActualizada.sIndicaciones
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al actualizar indicaciones',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};

/**
 * Obtener resumen de orden para Pantalla 1
 * GET /api/orden/:id/resumen
 * 
 * Retorna la orden con productos populados y totales calculados
 */
export const getOrdenResumen = async (req, res) => {
  try {
    const { id } = req.params;
    
    const orden = await Orden.findById(id)
      .populate('aProductos')
      .populate({
        path: 'ordenesSecundarias',
        populate: { path: 'aProductos' }
      });

    if (!orden) {
      return res.status(404).json({
        success: false,
        message: 'Orden no encontrada',
        error: {
          code: 404,
          details: 'La orden especificada no existe'
        }
      });
    }

    // Calcular totales
    const productos = orden.aProductos || [];
    
    // Total de productos (sin extras)
    const iTotalProductos = productos.reduce((total, p) => {
      return total + (p.iCostoPublico * p.iCantidad);
    }, 0);

    // Total de extras
    const iTotalExtras = productos.reduce((total, p) => {
      return total + (p.iTotalPublicoExtrasOrden || 0);
    }, 0);

    // Total general
    const iTotalGeneral = iTotalProductos + iTotalExtras;

    return res.status(200).json({
      success: true,
      message: 'Resumen de orden obtenido exitosamente',
      data: {
        _id: orden._id,
        sIdentificadorOrden: orden.sIdentificadorOrden,
        iNumeroOrden: orden.iNumeroOrden,
        iMesa: orden.iMesa,
        sUsuarioMesero: orden.sUsuarioMesero,
        sIndicaciones: orden.sIndicaciones,
        iEstatus: orden.iEstatus,
        dtFechaAlta: orden.dtFechaAlta,
        aProductos: productos.map(p => ({
          _id: p._id,
          sNombre: p.sNombre,
          iCostoPublico: p.iCostoPublico,
          iCantidad: p.iCantidad,
          sURLImagen: p.sURLImagen,
          aVariantes: p.aVariantes,
          iIndexVarianteSeleccionada: p.iIndexVarianteSeleccionada,
          bTieneExtras: p.bTieneExtras,
          iTotalExtras: p.iTotalPublicoExtrasOrden,
          iTotalProducto: p.iTotalGeneralPublicoOrdenProducto
        })),
        iTotalProductos,
        iTotalExtras,
        iTotalGeneral
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al obtener resumen de orden',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};

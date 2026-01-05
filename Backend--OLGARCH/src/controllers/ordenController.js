import Orden from '../models/orden.js';
import Usuario from '../models/usuario.js';
// import OrdenProducto from '../models/OrdenProducto.js';
import OrdenProducto from '../models/ordenProducto.js';
import mongoose from 'mongoose';
import Producto from '../models/producto.js';
import Ingrediente from '../models/ingrediente.js';

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
  const session = await mongoose.startSession();

  try {
    const { id } = req.params;

    session.startTransaction();

    // 1) Trae la orden actual (para comparar estatus)
    const orden = await Orden.findById(id).session(session);
    if (!orden) {
      await session.abortTransaction();
      session.endSession();
      return res.status(404).json({
        success: false,
        message: 'Orden no encontrada',
        error: { code: 404, details: 'Orden no encontrada' }
      });
    }

    const estatusAnterior = Number(orden.iEstatus);
    const estatusNuevo = req.body?.iEstatus !== undefined ? Number(req.body.iEstatus) : null;

    // 2) Si están intentando pasar a 3, y antes NO era 3, y NO se ha descontado inventario:
    const debeDescontarInventario =
      estatusNuevo === 3 &&
      estatusAnterior !== 3;

    // 3) Si debe descontar, primero descuenta (para poder abortar si no hay stock)
    if (debeDescontarInventario) {
      await descontarInventarioPorOrden({ ordenId: orden._id, session });
    }

    // 4) Aplica el update normal (tu endpoint sigue funcionando igual)
    Object.assign(orden, req.body);
    const ordenActualizada = await orden.save({ session });

    await session.commitTransaction();
    session.endSession();

    return res.status(200).json({
      success: true,
      message: 'Orden actualizada exitosamente',
      data: ordenActualizada
    });
    } catch (error) {
      await session.abortTransaction();
      session.endSession();

      const code = error.statusCode || 500;

      return res.status(code).json({
        success: false,
        message: error.statusCode === 409 ? error.message : 'Error al actualizar la orden',
        error: {
          code,
          details: error.message,
          faltantes: error.faltantes || undefined
        }
      });
    }
};

async function descontarInventarioPorOrden({ ordenId, session }) {
  // Trae la orden con sus aProductos (solo IDs, no necesitas populate)
  const orden = await Orden.findById(ordenId).session(session);
  if (!orden) throw new Error('Orden no encontrada para descontar inventario.');

  if (!orden.aProductos || orden.aProductos.length === 0) return; // no hay nada que descontar

  // 1) Traer OrdenProducto docs
  const ordenProductos = await OrdenProducto.find({ _id: { $in: orden.aProductos } })
    .session(session)
    .lean();

  if (!ordenProductos || ordenProductos.length === 0) return;

  // 2) Detectar productId y cantidad por cada OrdenProducto (AJUSTA aquí si tu schema usa otros nombres)
  const items = ordenProductos
    .map((op) => {
      const productoId = op.sIdProductoMongoDB;

      const cantidad =
        Number(
          op.iCantidad ??
          op.iCantidadProducto ??
          op.iCantidadOrdenProducto ??
          op.cantidad ??
          1
        );

      if (!productoId) return null;

      return {
        productoId: String(productoId),
        cantidad: Number.isFinite(cantidad) && cantidad > 0 ? cantidad : 1
      };
    })
    .filter(Boolean);

  if (items.length === 0) return;

  // 3) Traer productos (recetas)
  const uniqueProductoIds = [...new Set(items.map((x) => x.productoId))];

  const productos = await Producto.find(
    { _id: { $in: uniqueProductoIds } },
    { aIngredientes: 1, iTipoProducto: 1 } // receta + tipo (por si quieres ignorar Extras)
  )
    .session(session)
    .lean();

  const mapProducto = new Map(productos.map((p) => [String(p._id), p]));

  // 4) Acumular consumo por ingrediente (ingredienteId => totalUso)
  const consumoPorIngrediente = new Map();

  for (const it of items) {
    const prod = mapProducto.get(it.productoId);
    if (!prod) continue;

    // Si quieres ignorar extras, puedes descomentar:
    // if (Number(prod.iTipoProducto) === 3) continue;

    const receta = Array.isArray(prod.aIngredientes) ? prod.aIngredientes : [];
    if (receta.length === 0) continue;

    for (const r of receta) {
      const ingId = r?.sIdIngrediente ? String(r.sIdIngrediente) : null;
      const uso = Number(r?.iCantidadUso ?? 0);

      if (!ingId) continue;
      if (!Number.isFinite(uso) || uso <= 0) continue;

      const totalUso = uso * it.cantidad;
      consumoPorIngrediente.set(ingId, (consumoPorIngrediente.get(ingId) || 0) + totalUso);
    }
  }

  if (consumoPorIngrediente.size === 0) return;

  // 5) Validar stock antes de descontar (para no permitir negativos)
  const idsIngredientes = [...consumoPorIngrediente.keys()];

  const ingredientes = await Ingrediente.find(
    { _id: { $in: idsIngredientes } },
    { iCantidadEnAlmacen: 1, sNombre: 1, sUnidad: 1 }
  )
    .session(session)
    .lean();

  const mapIng = new Map(ingredientes.map((i) => [String(i._id), i]));

  const faltantes = [];
  for (const [ingId, reqUso] of consumoPorIngrediente.entries()) {
    const ing = mapIng.get(ingId);
    if (!ing) {
      faltantes.push({ sIdIngrediente: ingId, motivo: 'Ingrediente no existe' });
      continue;
    }
    const stock = Number(ing.iCantidadEnAlmacen ?? 0);
    if (stock < reqUso) {
      faltantes.push({
        sIdIngrediente: ingId,
        sNombre: ing.sNombre,
        sUnidad: ing.sUnidad,
        stock,
        requerido: reqUso
      });
    }
  }

  if (faltantes.length > 0) {
    const err = new Error('Stock insuficiente para completar la preparación.');
    err.statusCode = 409;
    err.faltantes = faltantes;
    throw err;
  }

  // 6) Descontar (bulk)
  const ops = [];
  for (const [ingId, reqUso] of consumoPorIngrediente.entries()) {
    ops.push({
      updateOne: {
        filter: { _id: ingId },
        update: { $inc: { iCantidadEnAlmacen: -reqUso } }
      }
    });
  }

  if (ops.length > 0) {
    await Ingrediente.bulkWrite(ops, { session });
  }
}


/**
 * Eliminar una orden por ID
 */
export const deleteOrden = async (req, res) => {
  const { id } = req.params;

  // ✅ log base
  console.log("[deleteOrden] INICIO", {
    id,
    time: new Date().toISOString(),
    isValidObjectId: mongoose.Types.ObjectId.isValid(id),
  });

  // Si el id no es ObjectId, evita reventar en el driver
  if (!mongoose.Types.ObjectId.isValid(id)) {
    console.warn("[deleteOrden] ID inválido (no ObjectId)", { id });
    return res.status(400).json({
      success: false,
      message: "ID inválido",
      error: { code: 400, details: "El id no es un ObjectId válido" },
    });
  }

  let session;
  try {
    session = await mongoose.startSession();
    console.log("[deleteOrden] session creada");

    session.startTransaction();
    console.log("[deleteOrden] transaction START");

    // 1) Eliminar orden principal
    console.log("[deleteOrden] findByIdAndDelete orden", { id });

    const ordenEliminada = await Orden.findByIdAndDelete(id, { session });

    console.log("[deleteOrden] ordenEliminada", {
      existe: !!ordenEliminada,
      _id: ordenEliminada?._id?.toString(),
      iTipoOrden: ordenEliminada?.iTipoOrden,
      sIdentificadorOrden: ordenEliminada?.sIdentificadorOrden,
    });

    if (!ordenEliminada) {
      console.warn("[deleteOrden] NO encontrada -> abort");
      await session.abortTransaction();
      session.endSession();
      return res.status(404).json({
        success: false,
        message: "Orden no encontrada",
        error: { code: 404, details: "La orden no existe" },
      });
    }

    // 2) Eliminar productos de esa orden
    console.log("[deleteOrden] deleteMany productos de orden", {
      sIdOrdenMongoDB: ordenEliminada._id?.toString(),
    });

    const delProductosMain = await OrdenProducto.deleteMany(
      { sIdOrdenMongoDB: ordenEliminada._id },
      { session }
    );

    console.log("[deleteOrden] productos eliminados (main)", {
      deletedCount: delProductosMain?.deletedCount,
      acknowledged: delProductosMain?.acknowledged,
    });

    // 3) Si es primaria, borrar secundarias y sus productos
    if (ordenEliminada.iTipoOrden === 1) {
      console.log("[deleteOrden] orden primaria => buscar secundarias", {
        sIdentificadorOrden: ordenEliminada.sIdentificadorOrden,
      });

      const ordenesSecundarias = await Orden.find(
        {
          sIdentificadorOrden: ordenEliminada.sIdentificadorOrden,
          iTipoOrden: 2,
        },
        "_id",
        { session }
      );

      console.log("[deleteOrden] secundarias encontradas", {
        count: ordenesSecundarias?.length ?? 0,
        ids: (ordenesSecundarias ?? []).map(o => o._id.toString()),
      });

      if (ordenesSecundarias?.length) {
        const idsOrdenesSecundarias = ordenesSecundarias.map(o => o._id);

        console.log("[deleteOrden] deleteMany productos secundarias", {
          ids: idsOrdenesSecundarias.map(x => x.toString()),
        });

        const delProductosSec = await OrdenProducto.deleteMany(
          { sIdOrdenMongoDB: { $in: idsOrdenesSecundarias } },
          { session }
        );

        console.log("[deleteOrden] productos eliminados (sec)", {
          deletedCount: delProductosSec?.deletedCount,
          acknowledged: delProductosSec?.acknowledged,
        });

        console.log("[deleteOrden] deleteMany ordenes secundarias");

        const delOrdenesSec = await Orden.deleteMany(
          { _id: { $in: idsOrdenesSecundarias } },
          { session }
        );

        console.log("[deleteOrden] ordenes secundarias eliminadas", {
          deletedCount: delOrdenesSec?.deletedCount,
          acknowledged: delOrdenesSec?.acknowledged,
        });
      }
    }

    await session.commitTransaction();
    console.log("[deleteOrden] transaction COMMIT OK");

    session.endSession();
    console.log("[deleteOrden] session END OK");

    return res.status(200).json({
      success: true,
      message: "Orden y productos asociados eliminados exitosamente",
      data: { orden: ordenEliminada },
    });
  } catch (error) {
    console.error("[deleteOrden] ERROR", {
      name: error?.name,
      message: error?.message,
      code: error?.code,
      stack: error?.stack,
    });

    // 🔥 Si el error es por transacciones / replica set, lo verás aquí
    // Ejemplos típicos:
    // - "Transaction numbers are only allowed on a replica set member or mongos"
    // - "This MongoDB deployment does not support retryable writes"
    // - "Cannot call abortTransaction after calling commitTransaction" (si ya se cerró)
    try {
      if (session) {
        console.log("[deleteOrden] abortTransaction...");
        await session.abortTransaction();
        console.log("[deleteOrden] abortTransaction OK");
        session.endSession();
        console.log("[deleteOrden] session END (error) OK");
      }
    } catch (abortErr) {
      console.error("[deleteOrden] ERROR abortTransaction/endSession", {
        message: abortErr?.message,
        stack: abortErr?.stack,
      });
    }

    return res.status(500).json({
      success: false,
      message: "Error al eliminar la orden",
      error: { code: 500, details: error.message },
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

    const orden = await Orden.findById(id)
      .populate({ path: "aProductos", model: "OrdenProducto" })
      .populate({
        path: "ordenesSecundarias",
        populate: { path: "aProductos", model: "OrdenProducto" },
      })
      .exec();

    if (!orden) {
      return res.status(404).json({
        bSuccess: false,
        success: false,
        sMessage: "Orden no encontrada",
        message: "Orden no encontrada",
        lData: [],
        data: [],
      });
    }

    const productosPrimaria = orden.aProductos || [];
    const productosSecundarias = (orden.ordenesSecundarias || []).flatMap(
      (sec) => sec.aProductos || []
    );
    const allProductos = [...productosPrimaria, ...productosSecundarias];

    // Agrupar por producto (nombre + costo) y sumar extras por consumo
    const prodMap = new Map(); // key -> { sNombre, iCostoPublico, iCantidad, extrasMap }

    for (const p of allProductos) {
      const nombreProd = p.sNombre || "";
      const costoProd = Number(p.iCostoPublico || 0);

      // Cantidad del producto (preferimos iCantidad, si no existe usamos length de consumos)
      const qtyProd = Number(
        p.iCantidad || (Array.isArray(p.aConsumos) ? p.aConsumos.length : 1) || 1
      );

      const prodKey = `${nombreProd}__${costoProd}`;

      if (!prodMap.has(prodKey)) {
        prodMap.set(prodKey, {
          sNombre: nombreProd,
          iCostoPublico: costoProd,
          iCantidad: 0,
          extrasMap: new Map(), // extraKey -> { sNombre, iCostoPublico, iCantidad }
        });
      }

      const g = prodMap.get(prodKey);
      g.iCantidad += qtyProd;

      // (A) Extras "generales" del producto (si los usas): se asumen por unidad
      if (Array.isArray(p.aExtras) && p.aExtras.length > 0) {
        for (const e of p.aExtras) {
          const exNombre = e.sNombre || "";
          const exCosto = Number(e.iCostoPublico || 0);
          const exKey = `${exNombre}__${exCosto}`;

          if (!g.extrasMap.has(exKey)) {
            g.extrasMap.set(exKey, { sNombre: exNombre, iCostoPublico: exCosto, iCantidad: 0 });
          }
          g.extrasMap.get(exKey).iCantidad += qtyProd;
        }
      }

      // (B) Extras INDIVIDUALES por consumo: cuentan 1 por aparición real
      if (Array.isArray(p.aConsumos) && p.aConsumos.length > 0) {
        for (const c of p.aConsumos) {
          if (Array.isArray(c.aExtras) && c.aExtras.length > 0) {
            for (const ex of c.aExtras) {
              const exNombre = ex.sNombre || "";
              const exCosto = Number(ex.iCostoPublico || 0);
              const exKey = `${exNombre}__${exCosto}`;

              if (!g.extrasMap.has(exKey)) {
                g.extrasMap.set(exKey, { sNombre: exNombre, iCostoPublico: exCosto, iCantidad: 0 });
              }
              g.extrasMap.get(exKey).iCantidad += 1; // 👈 por consumo
            }
          }
        }
      }
    }

    // Construir lista PLANA y ORDENADA: producto -> extras -> producto -> extras...
    const Productos = [];
    for (const g of prodMap.values()) {
      // Producto base
      Productos.push({
        sNombre: g.sNombre,
        iCantidad: g.iCantidad,
        iCostoPublico: g.iCostoPublico,
        bEsExtra: false,
      });

      // Extras agrupados debajo del producto
      const extrasOrdenados = Array.from(g.extrasMap.values()).sort((a, b) =>
        String(a.sNombre).localeCompare(String(b.sNombre), "es")
      );

      for (const ex of extrasOrdenados) {
        Productos.push({
          sNombre: ex.sNombre,
          iCantidad: ex.iCantidad,
          iCostoPublico: ex.iCostoPublico,
          bEsExtra: true,
        });
      }
    }

    // Total (producto + extras)
    const dTotalPublico = Productos.reduce((acc, it) => {
      return acc + Number(it.iCantidad || 0) * Number(it.iCostoPublico || 0);
    }, 0);

    const resultado = {
      iMesa: orden.iMesa,
      dTotalPublico,
      Productos,
    };

    return res.status(200).json({
      bSuccess: true,
      success: true,
      sMessage: "Información del ticket obtenida exitosamente",
      message: "Información del ticket obtenida exitosamente",
      lData: [resultado],
      data: [resultado],
    });
  } catch (error) {
    console.error("[getInfoTicket] ERROR:", error);
    return res.status(500).json({
      bSuccess: false,
      success: false,
      sMessage: "Error interno al obtener la orden",
      message: "Error interno al obtener la orden",
      lData: [],
      data: [],
      error: error.message,
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
    
    // Usar .lean() para obtener objetos JS planos (incluye subdocumentos anidados)
    const orden = await Orden.findById(id)
      .populate('aProductos')
      .populate({
        path: 'ordenesSecundarias',
        populate: { path: 'aProductos' }
      })
      .lean();

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
      return total + ((p.iCostoPublico || 0) * (p.iCantidad || 1));
    }, 0);

    // Total de extras (desde consumos)
    const iTotalExtras = productos.reduce((total, p) => {
      if (!p.aConsumos || p.aConsumos.length === 0) return total;
      return total + p.aConsumos.reduce((subtotal, consumo) => {
        if (!consumo.aExtras || consumo.aExtras.length === 0) return subtotal;
        return subtotal + consumo.aExtras.reduce((extraTotal, extra) => {
          return extraTotal + (extra.iCostoPublico || 0);
        }, 0);
      }, 0);
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
        sIdMongoDBMesero: orden.sIdMongoDBMesero,
        sIndicaciones: orden.sIndicaciones,
        iEstatus: orden.iEstatus,
        iTipoOrden: orden.iTipoOrden,
        dtFechaAlta: orden.dtFechaAlta,
        aProductos: productos.map(p => ({
          _id: p._id,
          sNombre: p.sNombre,
          iCostoReal: p.iCostoReal,
          iCostoPublico: p.iCostoPublico,
          iCantidad: p.iCantidad,
          sURLImagen: p.sURLImagen,
          sIndicaciones: p.sIndicaciones,
          aVariantes: p.aVariantes || [],
          iIndexVarianteSeleccionada: p.iIndexVarianteSeleccionada,
          iTipoProducto: p.iTipoProducto,
          bTieneExtras: (p.aConsumos || []).some(c => c.aExtras && c.aExtras.length > 0),
          aExtras: p.aExtras || [],
          aConsumos: p.aConsumos || [],
          iTotalExtras: (p.aConsumos || []).reduce((total, c) => {
            return total + (c.aExtras || []).reduce((st, e) => st + (e.iCostoPublico || 0), 0);
          }, 0),
          iTotalProducto: ((p.iCostoPublico || 0) * (p.iCantidad || 1)) + 
            (p.aConsumos || []).reduce((total, c) => {
              return total + (c.aExtras || []).reduce((st, e) => st + (e.iCostoPublico || 0), 0);
            }, 0)
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
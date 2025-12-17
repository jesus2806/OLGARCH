// controllers/historico.controller.js
import Historico from '../models/historico.js';
import Orden from '../models/orden.js';
import mongoose from 'mongoose';

export const updateOrdenAndRegisterHistorico = async (req, res) => {
    const session = await mongoose.startSession();
    session.startTransaction();
    try {
      const { id } = req.params; // ID de la orden a actualizar
      const { iTipoPago, iTotalBanco, iTotalEfectivo } = req.body; // Nuevo tipo de pago
  
      // 1. Obtener la orden a actualizar
      let ordenToUpdate = await Orden.findById(id).session(session);
      if (!ordenToUpdate) {
        throw new Error("Orden no encontrada");
      }
  
      // 2. Determinar cuáles órdenes deben revisarse para confirmar que estén entregadas (estatus 4)
      let ordersToCheck = [];
      if (ordenToUpdate.iTipoOrden === 1) {
        // Si es orden primaria, obtenerla con sus subórdenes pobladas
        ordenToUpdate = await Orden.findById(id)
          .populate('ordenesSecundarias')
          .session(session);
        ordersToCheck = [ordenToUpdate, ...(ordenToUpdate.ordenesSecundarias || [])];
      } else {
        // Si es una orden secundaria, obtener todas las órdenes con el mismo sIdentificadorOrden
        ordersToCheck = await Orden.find({ sIdentificadorOrden: ordenToUpdate.sIdentificadorOrden }).session(session);
      }
  
      // 3. Verificar que todas las órdenes tengan estatus "Entregada" (4)
      const noEntregadas = ordersToCheck.filter(order => order.iEstatus !== 4);
      if (noEntregadas.length > 0) {
        await session.abortTransaction();
        session.endSession();
        return res.status(400).json({
          success: false,
          message: 'No se puede confirmar el pago porque existen órdenes que no están en estatus entregada (4).',
          error: { code: 400, details: 'Verifica que todas las órdenes estén en estatus entregada.' }
        });
      }
  
      // 4. Actualizar todas las órdenes (primaria y subórdenes) a Pagada (5) y asignar el tipo de pago
      await Orden.updateMany(
        { sIdentificadorOrden: ordenToUpdate.sIdentificadorOrden },
        { iTipoPago: iTipoPago, dtFechaFin: new Date(), iEstatus: 5 },
        { session }
      );
  
      // 5. Re-obtener la orden primaria con sus productos y subórdenes (con sus productos) poblados
      //    para obtener los totales a través de los virtuales
      const updatedOrden = await Orden.findOne({ 
        sIdentificadorOrden: ordenToUpdate.sIdentificadorOrden, 
        iTipoOrden: 1 
      })
        .populate('aProductos')
        .populate({
          path: 'ordenesSecundarias',
          populate: { path: 'aProductos' }
        })
        .session(session);
  
      // 6. Convertir el documento a objeto incluyendo los virtuales (totales calculados)
      const ordenObj = updatedOrden.toObject({ virtuals: true });
  
      // 7. Crear el registro en el histórico utilizando los totales calculados
      const newHistorico = new Historico({
        sIdOrdenPrimaria: ordenObj._id,
        sIdentificadorOrdenPrimaria: ordenObj.sIdentificadorOrden,
        iMesa: ordenObj.iMesa,
        iNumeroOrden: ordenObj.iNumeroOrden,
        sUsuarioMesero: ordenObj.sUsuarioMesero,
        dtFechaAlta: ordenToUpdate.dtFechaAlta,
        dtFechaFin: ordenObj.dtFechaFin,
        iTotalCostoPublico: ordenObj.iTotalPublicoOrden || 0,
        iTotalCostoReal: ordenObj.iTotalRealOrden || 0,
        iTotalBanco: iTotalBanco,
        iTotalEfectivo: iTotalEfectivo,
        iTipoPago: ordenObj.iTipoPago,
        iEstatus: ordenObj.iEstatus,
      });
  
      await newHistorico.save({ session });
  
      // 8. Confirmar la transacción
      await session.commitTransaction();
      session.endSession();
  
      return res.status(200).json({
        success: true,
        message: "Orden y subórdenes actualizadas a pagada y registrada en histórico exitosamente.",
        data: { updatedOrden, newHistorico },
      });
    } catch (error) {
      await session.abortTransaction();
      session.endSession();
      return res.status(500).json({
        success: false,
        message: "Error al procesar la solicitud",
        error: { code: 500, details: error.message },
      });
    }
  };


export const createHistorico = async (req, res) => {
  try {
    const {
      sIdentificadorOrden,
      iNumeroOrden, // Recuerda: la lógica de auto incremento debe implementarse por separado
      iMesa,
      sUsuarioMesero,
      dtFechaFin,
      iTotalCostoPublico,
      iTotalCostoReal,
      iTipoPago,
      iEstatus,
    } = req.body;

    const nuevoHistorico = new Historico({
      sIdentificadorOrden,
      iMesa,
      iNumeroOrden, // Opcional, según tu lógica de auto incremento
      sUsuarioMesero,
      dtFechaFin, // Puede ser null o una fecha, según corresponda
      iTotalCostoPublico,
      iTotalCostoReal,
      iTipoPago,
      iEstatus,
    });

    const historicoGuardado = await nuevoHistorico.save();

    return res.status(201).json({
      success: true,
      message: 'Histórico creado exitosamente',
      data: historicoGuardado,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al procesar la solicitud',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

export const getHistoricoResumen = async (req, res) => {
  try {
    // Obtener la fecha solicitada o la fecha actual si no viene en la solicitud
    const { dFecha } = req.body;
    let fechaBase = dFecha ? new Date(dFecha) : new Date();

    // Calcular el inicio y fin del día en México (UTC-6)
    const mexicoStart = new Date(Date.UTC(fechaBase.getFullYear(), fechaBase.getMonth(), fechaBase.getDate(), 6, 0, 0));
    const mexicoEnd = new Date(Date.UTC(fechaBase.getFullYear(), fechaBase.getMonth(), fechaBase.getDate() + 1, 6, 0, 0));

    // const mexicoStart = new Date(Date.UTC(hoyMX.getFullYear(), hoyMX.getMonth(), hoyMX.getDate(), 0, 0, 0));
    // const mexicoEnd = new Date(Date.UTC(hoyMX.getFullYear(), hoyMX.getMonth(), hoyMX.getDate(), 23, 59, 59));

    // *** TESTING

    // console.log(fechaBase)
    // console.log(mexicoStart)
    // console.log(mexicoEnd)
    // console.log("----------------------------------")

    // *** TESTING

    // Agregación para sumar campos
    const result = await Historico.aggregate([
      {
        $match: {
          dtFechaFin: { $gte: mexicoStart, $lt: mexicoEnd }
        }
      },
      {
        $group: {
          _id: null,
          totalCostoPublico: { $sum: "$iTotalCostoPublico" },
          totalCostoReal: { $sum: "$iTotalCostoReal" },
          totalEfectivo: { $sum: "$iTotalEfectivo" },
          totalBanco: { $sum: "$iTotalBanco" }
        }
      }
    ]);

    // Si no hay resultados, inicializamos con cero
    const {
      totalCostoPublico = 0,
      totalCostoReal = 0,
      totalEfectivo = 0,
      totalBanco = 0
    } = result[0] || {};

    // Obtener todos los registros del histórico para el día actual
    const registros = await Historico.find({
      dtFechaFin: { $gte: mexicoStart, $lt: mexicoEnd }
    });

    return res.status(200).json({
      success: true,
      message: 'Operación exitosa',
      data: {
        totals: {
          totalCostoPublico,
          totalCostoReal,
          totalEfectivo,
          totalBanco
        },
        registros
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al procesar la solicitud',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};

export const getHistoricoById = async (req, res) => {
  try {
    const { id } = req.params;
    const historico = await Historico.findById(id);
    if (!historico) {
      return res.status(404).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: {
          code: 404,
          details: 'Histórico no encontrado',
        },
      });
    }
    return res.status(200).json({
      success: true,
      message: 'Operación exitosa',
      data: historico,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al procesar la solicitud',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

export const updateHistorico = async (req, res) => {
  try {
    const { id } = req.params;

    // Buscamos el registro a actualizar
    const historico = await Historico.findById(id);
    if (!historico) {
      return res.status(404).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: {
          code: 404,
          details: 'Histórico no encontrado',
        },
      });
    }

    // Actualizamos los campos si se envían en el body
    const {
      sIdentificadorOrden,
      iMesa,
      sUsuarioMesero,
      dtFechaFin,
      iTotalCostoPublico,
      iTotalCostoReal,
      iTipoPago,
      iEstatus,
    } = req.body;

    if (sIdentificadorOrden) historico.sIdentificadorOrden = sIdentificadorOrden;
    if (iMesa !== undefined) historico.iMesa = iMesa;
    if (sUsuarioMesero) historico.sUsuarioMesero = sUsuarioMesero;
    if (dtFechaFin !== undefined) historico.dtFechaFin = dtFechaFin;
    if (iTotalCostoPublico !== undefined) historico.iTotalCostoPublico = iTotalCostoPublico;
    if (iTotalCostoReal !== undefined) historico.iTotalCostoReal = iTotalCostoReal;
    if (iTipoPago !== undefined) historico.iTipoPago = iTipoPago;
    if (iEstatus !== undefined) historico.iEstatus = iEstatus;

    const historicoActualizado = await historico.save();

    return res.status(200).json({
      success: true,
      message: 'Histórico actualizado correctamente',
      data: historicoActualizado,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al procesar la solicitud',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

export const deleteHistorico = async (req, res) => {
  try {
    const { id } = req.params;
    const historicoEliminado = await Historico.findByIdAndDelete(id);
    if (!historicoEliminado) {
      return res.status(404).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: {
          code: 404,
          details: 'Histórico no encontrado',
        },
      });
    }
    return res.status(200).json({
      success: true,
      message: 'Histórico eliminado correctamente',
      data: historicoEliminado,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al procesar la solicitud',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};
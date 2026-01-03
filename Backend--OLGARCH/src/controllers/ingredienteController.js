import Ingrediente from "../models/ingrediente.js";

/**
 * Crea un nuevo ingrediente
 */
export const createIngrediente = async (req, res) => {
  try {
    const { sNombre, iCantidadEnAlmacen, iCantidadMinima, sUnidad, iCostoUnidad } = req.body;

    const nuevoIngrediente = new Ingrediente({
      sNombre,
      iCantidadEnAlmacen,
      iCantidadMinima,
      sUnidad,
      iCostoUnidad,
    });

    const ingredienteGuardado = await nuevoIngrediente.save();

    return res.status(201).json({
      success: true,
      message: "Ingrediente creado exitosamente",
      data: ingredienteGuardado,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: "Error al procesar la solicitud",
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

/**
 * Obtiene todos los ingredientes
 */
export const getIngredientes = async (req, res) => {
  try {
    const ingredientes = await Ingrediente.find().sort({ sNombre: 1 });

    return res.status(200).json({
      success: true,
      message: "Operación exitosa",
      data: ingredientes,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: "Error al procesar la solicitud",
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

/**
 * Obtiene un ingrediente por su ID
 */
export const getIngredienteById = async (req, res) => {
  try {
    const { id } = req.params;

    const ingrediente = await Ingrediente.findById(id);
    if (!ingrediente) {
      return res.status(404).json({
        success: false,
        message: "Error al procesar la solicitud",
        error: {
          code: 404,
          details: "Ingrediente no encontrado",
        },
      });
    }

    return res.status(200).json({
      success: true,
      message: "Operación exitosa",
      data: ingrediente,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: "Error al procesar la solicitud",
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

/**
 * Actualiza un ingrediente
 */
export const updateIngrediente = async (req, res) => {
  try {
    const { id } = req.params;
    const { sNombre, iCantidadEnAlmacen, iCantidadMinima, sUnidad, iCostoUnidad } = req.body;

    const ingrediente = await Ingrediente.findById(id);
    if (!ingrediente) {
      return res.status(404).json({
        success: false,
        message: "Error al procesar la solicitud",
        error: {
          code: 404,
          details: "Ingrediente no encontrado",
        },
      });
    }

    if (sNombre !== undefined) ingrediente.sNombre = sNombre;
    if (iCantidadEnAlmacen !== undefined) ingrediente.iCantidadEnAlmacen = iCantidadEnAlmacen;
    if (iCantidadMinima !== undefined) ingrediente.iCantidadMinima = iCantidadMinima;
    if (sUnidad !== undefined) ingrediente.sUnidad = sUnidad;
    if (iCostoUnidad !== undefined) ingrediente.iCostoUnidad = iCostoUnidad;

    const ingredienteActualizado = await ingrediente.save();

    return res.status(200).json({
      success: true,
      message: "Ingrediente actualizado correctamente",
      data: ingredienteActualizado,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: "Error al procesar la solicitud",
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

/**
 * Elimina un ingrediente
 */
export const deleteIngrediente = async (req, res) => {
  try {
    const { id } = req.params;

    const ingrediente = await Ingrediente.findById(id);
    if (!ingrediente) {
      return res.status(404).json({
        success: false,
        message: "Error al procesar la solicitud",
        error: {
          code: 404,
          details: "Ingrediente no encontrado",
        },
      });
    }

    const ingredienteEliminado = await Ingrediente.findByIdAndDelete(id);

    return res.status(200).json({
      success: true,
      message: "Ingrediente eliminado correctamente",
      data: ingredienteEliminado,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: "Error al procesar la solicitud",
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

/**
 * Búsqueda de ingredientes (opcional, como tu searchProductos)
 * Body ejemplo:
 * { "texto": "pollo", "unidad": "kg", "bajoStock": true }
 */
export const searchIngredientes = async (req, res) => {
  try {
    const { texto, unidad, bajoStock } = req.body;

    const query = {};

    if (texto) {
      query.sNombre = { $regex: texto, $options: "i" };
    }

    if (unidad) {
      query.sUnidad = { $regex: `^${unidad}$`, $options: "i" }; // match exacto ignorando may/min
    }

    // bajoStock: cuando iCantidadEnAlmacen <= iCantidadMinima (comparación campo vs campo)
    if (bajoStock) {
      query.$expr = { $lte: ["$iCantidadEnAlmacen", "$iCantidadMinima"] };
    }

    const ingredientes = await Ingrediente.find(query).limit(50).sort({ sNombre: 1 });

    return res.status(200).json({
      success: true,
      message: "Búsqueda exitosa",
      data: ingredientes,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: "Error al procesar la solicitud",
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

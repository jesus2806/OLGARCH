// controllers/extraController.js
import Extra from '../models/extra.js';

/**
 * Crear un nuevo extra
 */
export const createExtra = async (req, res) => {
  try {
    const { sNombre, iCostoReal, iCostoPublico, imagenes } = req.body;

    const nuevoExtra = new Extra({
      sNombre,
      iCostoReal,
      iCostoPublico,
      imagenes: imagenes || []
    });

    const extraGuardado = await nuevoExtra.save();

    return res.status(201).json({
      success: true,
      message: 'Extra creado exitosamente',
      data: extraGuardado
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al crear el extra',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};

/**
 * Obtener todos los extras activos
 */
export const getExtras = async (req, res) => {
  try {
    const extras = await Extra.find({ bActivo: true });
    
    return res.status(200).json({
      success: true,
      message: 'Extras obtenidos exitosamente',
      data: extras
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al obtener extras',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};

/**
 * Obtener un extra por ID
 */
export const getExtraById = async (req, res) => {
  try {
    const { id } = req.params;
    const extra = await Extra.findById(id);

    if (!extra) {
      return res.status(404).json({
        success: false,
        message: 'Extra no encontrado',
        error: {
          code: 404,
          details: 'Extra no encontrado'
        }
      });
    }

    return res.status(200).json({
      success: true,
      message: 'Extra obtenido exitosamente',
      data: extra
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al obtener el extra',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};

/**
 * Actualizar un extra
 */
export const updateExtra = async (req, res) => {
  try {
    const { id } = req.params;
    const { sNombre, iCostoReal, iCostoPublico, imagenes, bActivo } = req.body;

    const extra = await Extra.findById(id);
    if (!extra) {
      return res.status(404).json({
        success: false,
        message: 'Extra no encontrado',
        error: {
          code: 404,
          details: 'Extra no encontrado'
        }
      });
    }

    if (sNombre !== undefined) extra.sNombre = sNombre;
    if (iCostoReal !== undefined) extra.iCostoReal = iCostoReal;
    if (iCostoPublico !== undefined) extra.iCostoPublico = iCostoPublico;
    if (imagenes !== undefined) extra.imagenes = imagenes;
    if (bActivo !== undefined) extra.bActivo = bActivo;

    const extraActualizado = await extra.save();

    return res.status(200).json({
      success: true,
      message: 'Extra actualizado exitosamente',
      data: extraActualizado
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al actualizar el extra',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};

/**
 * Eliminar un extra (soft delete - desactivar)
 */
export const deleteExtra = async (req, res) => {
  try {
    const { id } = req.params;
    
    const extra = await Extra.findByIdAndUpdate(
      id,
      { bActivo: false },
      { new: true }
    );

    if (!extra) {
      return res.status(404).json({
        success: false,
        message: 'Extra no encontrado',
        error: {
          code: 404,
          details: 'Extra no encontrado'
        }
      });
    }

    return res.status(200).json({
      success: true,
      message: 'Extra eliminado exitosamente',
      data: extra
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al eliminar el extra',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};

/**
 * Buscar extras por nombre (Pantalla 3 - búsqueda de extras)
 */
export const searchExtras = async (req, res) => {
  try {
    const { texto } = req.body;
    
    const query = { bActivo: true };
    
    if (texto) {
      query.sNombre = { $regex: texto, $options: 'i' };
    }

    const extras = await Extra.find(query).limit(50);

    return res.status(200).json({
      success: true,
      message: 'Búsqueda de extras exitosa',
      data: extras
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error en la búsqueda de extras',
      error: {
        code: 500,
        details: error.message
      }
    });
  }
};

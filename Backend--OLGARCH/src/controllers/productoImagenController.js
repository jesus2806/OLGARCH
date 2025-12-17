// controladorImagen.js
import { uploadFile, getFileUrl,getObjectUrl, deleteFile } from '../services/s3Service.js';

/**
 * Sube una imagen a S3.
 */
export const subirImagen = async (req, res) => {
  try {
    const file = req.file;
    if (!file) {
      return res.status(400).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: {
          code: 400,
          details: 'No se proporcionó ningún archivo',
        },
      });
    }

    const sNombreImgProducto = `productos/${Date.now()}_${file.originalname}`;
    // Genera un nombre único para el archivo
    const key = `productos/${Date.now()}_${file.originalname}`;

    // Sube a S3
    await uploadFile(key, file.buffer, file.mimetype);
    var sRutaImagenS3 = getObjectUrl("" + sNombreImgProducto + "");
    // Respuesta exitosa
    return res.status(201).json({
      success: true,
      message: 'Imagen subida exitosamente',
      data: {
        sRutaImagenS3, // o cualquier información adicional
      },
    });
  } catch (error) {
    console.error('Error subiendo la imagen: ', error);
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

/**
 * Obtiene la URL pre-firmada de la imagen en S3.
 */
export const obtenerUrlImagen = async (req, res) => {
  try {
    const { key } = req.params;
    // const url = await getFileUrl("productos/" + key);
    const url = getObjectUrl("productos/" + key);
    return res.status(200).json({
      success: true,
      message: 'Operación exitosa',
      data: {
        url,
      },
    });
  } catch (error) {
    console.error('Error obteniendo la imagen: ', error);
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

/**
 * Elimina la imagen de S3.
 */
export const eliminarImagen = async (req, res) => {
  try {
    const { key } = req.params;
    await deleteFile("productos/" + key);
    return res.status(200).json({
      success: true,
      message: 'Imagen eliminada exitosamente',
      data: {
        key,
      },
    });
  } catch (error) {
    console.error('Error eliminando la imagen: ', error);
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

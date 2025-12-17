// controllers/producto.controller.js
import Producto from '../models/producto.js';
import { deleteFile } from '../services/s3Service.js';

/**
 * Crea un nuevo producto
 */
export const createProducto = async (req, res) => {
  try {
    const { sNombre, iCostoReal, iCostoPublico, imagenes, aVariantes, iTipoProducto } = req.body;

    const nuevoProducto = new Producto({
      sNombre,
      iCostoReal,
      iCostoPublico,
      imagenes,
      aVariantes,
      iTipoProducto,
    });

    const productoGuardado = await nuevoProducto.save();

    return res.status(201).json({
      success: true,
      message: 'Producto creado exitosamente',
      data: productoGuardado,
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

/**
 * Obtiene todos los productos
 */
export const getProductos = async (req, res) => {
  try {
    const productos = await Producto.find();
    return res.status(200).json({
      success: true,
      message: 'Operación exitosa',
      data: productos,
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

/**
 * Obtiene un producto por su ID
 */
export const getProductoById = async (req, res) => {
  try {
    const { id } = req.params;
    const producto = await Producto.findById(id);

    if (!producto) {
      return res.status(404).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: {
          code: 404,
          details: 'Producto no encontrado',
        },
      });
    }

    return res.status(200).json({
      success: true,
      message: 'Operación exitosa',
      data: producto,
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

/**
 * Actualiza un producto
 */
export const updateProducto = async (req, res) => {
  try {
    const { id } = req.params;
    const { sNombre, iCostoReal, iCostoPublico, imagenes, aVariantes, iTipoProducto } = req.body;

    const producto = await Producto.findById(id);

    if (!producto) {
      return res.status(404).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: {
          code: 404,
          details: 'Producto no encontrado',
        },
      });
    }

    // Guardamos las imágenes actuales (antes de actualizar)
    const oldImages = producto.imagenes || [];

    // Actualizamos los campos básicos si vienen en el body
    if (sNombre !== undefined) producto.sNombre = sNombre;
    if (iCostoReal !== undefined) producto.iCostoReal = iCostoReal;
    if (iCostoPublico !== undefined) producto.iCostoPublico = iCostoPublico;
    if (aVariantes !== undefined) producto.aVariantes = aVariantes;
    if (iTipoProducto !== undefined) producto.iTipoProducto = iTipoProducto;

    // Si en la petición vienen "imagenes",
    // significa que queremos "reemplazar" la lista completa de imágenes
    if (imagenes) {
      // 1) Obtenemos la nueva lista
      const newImages = imagenes; // Array de objetos { sURLImagen: '...' }

      // 2) Identificamos qué URLs ya no están en newImages
      //    (las que se eliminaron)
      const oldUrls = oldImages.map(img => img.sURLImagen);
      const newUrls = newImages.map(img => img.sURLImagen);

      // 3) Recorremos oldUrls y si alguna no está en newUrls => se borrará de S3
      for (const oldUrl of oldUrls) {
        if (!newUrls.includes(oldUrl)) {
          // Borrar de S3
          const key = extraerKeyS3(oldUrl);
          if (key) {
            await deleteFile(key);
          }
        }
      }

      // 4) Asignamos las nuevas imágenes
      producto.imagenes = newImages;
    }

    // Guardamos los cambios en MongoDB
    const productoActualizado = await producto.save();

    return res.status(200).json({
      success: true,
      message: 'Producto actualizado correctamente',
      data: productoActualizado,
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

/**
 * Elimina un producto
 */
export const deleteProducto = async (req, res) => {
  try {
    const { id } = req.params;

    // 1) Buscar el producto primero
    const producto = await Producto.findById(id);
    if (!producto) {
      return res.status(404).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: {
          code: 404,
          details: 'Producto no encontrado',
        },
      });
    }

    // 2) Eliminar las imágenes de S3, si existen
    if (producto.imagenes && producto.imagenes.length > 0) {
      for (let imagen of producto.imagenes) {
        if (imagen.sURLImagen) {
          // Extraemos la key S3 a partir de la URL
          const key = extraerKeyS3(imagen.sURLImagen);

          if (key) {
            // Llamar a deleteFile(key)
            await deleteFile(key);
          }
        }
      }
    }

    // 3) Ahora que las imágenes están borradas en S3, 
    //    borramos el producto en la DB
    const productoEliminado = await Producto.findByIdAndDelete(id);

    return res.status(200).json({
      success: true,
      message: 'Producto eliminado correctamente',
      data: productoEliminado,
    });

  } catch (error) {
    console.error('Error al eliminar producto: ', error);
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


export const searchProductos = async (req, res) => {
  try {
    
    const { texto, tipo, tipoEn } = req.body;  // <-- Leer desde body

    const query = {};
    // 1) Filtrar por nombre con regex
    if (texto) {
      query.sNombre = { $regex: texto, $options: 'i' };
    }

    // 2) Filtrar por iTipoProducto
    if (tipo) {
      const tipoNum = parseInt(tipo, 10);
      if (!isNaN(tipoNum)) {
        query.iTipoProducto = tipoNum;
      }
    }

    if (tipoEn) {
      let arr = [];
      // tipoEn podría ser array real o string
      if (Array.isArray(tipoEn)) {
        arr = tipoEn.map(n => parseInt(n, 10)).filter(n => !isNaN(n));
      } else if (typeof tipoEn === 'string') {
        // Convertir "1,2" en array
        arr = tipoEn.split(',').map(n => parseInt(n, 10)).filter(n => !isNaN(n));
      }
      if (arr.length > 0) {
        query.iTipoProducto = { $in: arr };
      }
    }

    // 3) Buscar en MongoDB (opcional limit(50))
    const productos = await Producto.find(query).limit(50);
    return res.status(200).json({
      success: true,
      message: 'Búsqueda exitosa',
      data: productos,
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


/**
 * Función auxiliar que extrae la parte "key" de la URL S3,
 * por ejemplo:
 *  https://...amazonaws.com/productos%2F1737784120578_snow-16130_256.gif
 * retorna:
 *  productos/1737784120578_snow-16130_256.gif
 */
function extraerKeyS3(sURLImagen) {
  try {
    // Dividimos la URL por ".com/"
    const partes = sURLImagen.split('.com/');
    if (partes.length < 2) {
      return null; // URL no válida
    }
    // La parte que sigue al .com/ es la key con posible encoding
    const keyCodificada = partes[1]; // "productos%2F1737784120578_snow-16130_256.gif"
    
    // Decodificar el string
    const key = decodeURIComponent(keyCodificada); 
    return key; // "productos/1737784120578_snow-16130_256.gif"
  } catch (error) {
    console.error('Error extrayendo key S3: ', error);
    return null;
  }
}
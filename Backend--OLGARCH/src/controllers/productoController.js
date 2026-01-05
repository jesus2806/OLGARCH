import mongoose from "mongoose";
import Producto from '../models/producto.js';
import Ingrediente from "../models/ingrediente.js";
import { deleteFile } from '../services/s3Service.js';

/** =========================
 * Helpers Ingredientes
 * ========================= */

function validarYNormalizarIngredientes(aIngredientes) {
  // Permite: undefined => "no tocar"
  if (aIngredientes === undefined) return { ok: true, value: undefined };

  if (!Array.isArray(aIngredientes)) {
    return { ok: false, error: "aIngredientes debe ser un arreglo" };
  }

  const errores = [];
  const normalizados = [];
  const seen = new Set();

  for (let i = 0; i < aIngredientes.length; i++) {
    const it = aIngredientes[i] || {};
    const id = it.sIdIngrediente;
    const cant = it.iCantidadUso;

    if (!id || !mongoose.Types.ObjectId.isValid(id)) {
      errores.push(`aIngredientes[${i}].sIdIngrediente inválido`);
      continue;
    }

    const nCant = Number(cant);
    if (!Number.isFinite(nCant)) {
      errores.push(`aIngredientes[${i}].iCantidadUso debe ser numérico`);
      continue;
    }
    if (nCant < 0) {
      errores.push(`aIngredientes[${i}].iCantidadUso no puede ser negativo`);
      continue;
    }

    const key = String(id);
    if (seen.has(key)) {
      errores.push(`Ingrediente duplicado en aIngredientes: ${key}`);
      continue;
    }
    seen.add(key);

    normalizados.push({
      sIdIngrediente: new mongoose.Types.ObjectId(id),
      iCantidadUso: nCant
    });
  }

  if (errores.length > 0) {
    return { ok: false, error: errores.join(" | ") };
  }

  return { ok: true, value: normalizados };
}

async function verificarIngredientesExisten(normalizados) {
  // normalizados: [{sIdIngrediente:ObjectId, iCantidadUso:number}]
  const ids = normalizados.map(x => x.sIdIngrediente);
  if (ids.length === 0) return { ok: true };

  const encontrados = await Ingrediente.find({ _id: { $in: ids } }).select("_id");
  const setFound = new Set(encontrados.map(x => String(x._id)));

  const faltantes = ids
    .map(x => String(x))
    .filter(id => !setFound.has(id));

  if (faltantes.length > 0) {
    return { ok: false, error: `Ingredientes no encontrados: ${faltantes.join(", ")}` };
  }
  return { ok: true };
}

/**
 * Crea un nuevo producto
 */
export const createProducto = async (req, res) => {
  try {
    const {
      sNombre, iCostoReal, iCostoPublico,
      imagenes, aVariantes, iTipoProducto,
      aIngredientes // ✅ NUEVO
    } = req.body;

    // ✅ Validar ingredientes si vienen
    const valIng = validarYNormalizarIngredientes(aIngredientes);
    if (!valIng.ok) {
      return res.status(400).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: { code: 400, details: valIng.error },
      });
    }

    // ✅ Verificar que existan en BD (si vienen)
    if (valIng.value !== undefined) {
      const exist = await verificarIngredientesExisten(valIng.value);
      if (!exist.ok) {
        return res.status(400).json({
          success: false,
          message: 'Error al procesar la solicitud',
          error: { code: 400, details: exist.error },
        });
      }
    }

    const nuevoProducto = new Producto({
      sNombre,
      iCostoReal,
      iCostoPublico,
      imagenes,
      aVariantes,
      iTipoProducto,
      aIngredientes: valIng.value ?? [] // ✅
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
      error: { code: 500, details: error.message },
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
  console.error("getProductos ERROR:", error);
  return res.status(500).json({
    success: false,
    message: "Error al procesar la solicitud",
    error: { code: 500, details: error.message },
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
        error: { code: 404, details: 'Producto no encontrado' },
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
      error: { code: 500, details: error.message },
    });
  }
};

/**
 * Actualiza un producto
 */
export const updateProducto = async (req, res) => {
  try {
    const { id } = req.params;
    const {
      sNombre, iCostoReal, iCostoPublico,
      imagenes, aVariantes, iTipoProducto,
      aIngredientes // ✅ NUEVO
    } = req.body;

    const producto = await Producto.findById(id);

    if (!producto) {
      return res.status(404).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: { code: 404, details: 'Producto no encontrado' },
      });
    }

    // Guardamos las imágenes actuales (antes de actualizar)
    const oldImages = producto.imagenes || [];

    // Campos básicos
    if (sNombre !== undefined) producto.sNombre = sNombre;
    if (iCostoReal !== undefined) producto.iCostoReal = iCostoReal;
    if (iCostoPublico !== undefined) producto.iCostoPublico = iCostoPublico;
    if (aVariantes !== undefined) producto.aVariantes = aVariantes;
    if (iTipoProducto !== undefined) producto.iTipoProducto = iTipoProducto;

    // ✅ Ingredientes: reemplaza lista completa si viene en body (incluye [] para limpiar)
    const valIng = validarYNormalizarIngredientes(aIngredientes);
    if (!valIng.ok) {
      return res.status(400).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: { code: 400, details: valIng.error },
      });
    }
    if (valIng.value !== undefined) {
      const exist = await verificarIngredientesExisten(valIng.value);
      if (!exist.ok) {
        return res.status(400).json({
          success: false,
          message: 'Error al procesar la solicitud',
          error: { code: 400, details: exist.error },
        });
      }
      producto.aIngredientes = valIng.value;
    }

    // Imágenes (reemplazo + borrado S3)
    if (imagenes) {
      const newImages = imagenes;

      const oldUrls = oldImages.map(img => img.sURLImagen);
      const newUrls = newImages.map(img => img.sURLImagen);

      for (const oldUrl of oldUrls) {
        if (!newUrls.includes(oldUrl)) {
          const key = extraerKeyS3(oldUrl);
          if (key) await deleteFile(key);
        }
      }

      producto.imagenes = newImages;
    }

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
      error: { code: 500, details: error.message },
    });
  }
};

/**
 * Elimina un producto
 */
export const deleteProducto = async (req, res) => {
  try {
    const { id } = req.params;

    const producto = await Producto.findById(id);
    if (!producto) {
      return res.status(404).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: { code: 404, details: 'Producto no encontrado' },
      });
    }

    if (producto.imagenes && producto.imagenes.length > 0) {
      for (let imagen of producto.imagenes) {
        if (imagen.sURLImagen) {
          const key = extraerKeyS3(imagen.sURLImagen);
          if (key) await deleteFile(key);
        }
      }
    }

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
      error: { code: 500, details: error.message },
    });
  }
};

export const searchProductos = async (req, res) => {
  try {
    const { texto, tipo, tipoEn } = req.body;
    const query = {};

    if (texto) query.sNombre = { $regex: texto, $options: 'i' };

    if (tipo) {
      const tipoNum = parseInt(tipo, 10);
      if (!isNaN(tipoNum)) query.iTipoProducto = tipoNum;
    }

    if (tipoEn) {
      let arr = [];
      if (Array.isArray(tipoEn)) {
        arr = tipoEn.map(n => parseInt(n, 10)).filter(n => !isNaN(n));
      } else if (typeof tipoEn === 'string') {
        arr = tipoEn.split(',').map(n => parseInt(n, 10)).filter(n => !isNaN(n));
      }
      if (arr.length > 0) query.iTipoProducto = { $in: arr };
    }

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
      error: { code: 500, details: error.message },
    });
  }
};

function extraerKeyS3(sURLImagen) {
  try {
    const partes = sURLImagen.split('.com/');
    if (partes.length < 2) return null;
    const keyCodificada = partes[1];
    return decodeURIComponent(keyCodificada);
  } catch (error) {
    console.error('Error extrayendo key S3: ', error);
    return null;
  }
}

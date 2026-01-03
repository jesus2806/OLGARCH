import mongoose from "mongoose";
import Esquema, { DIAS_KEYS } from "../models/esquema.js";
import Usuario from "../models/usuario.js";

const normalize = (s) =>
  String(s || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .trim();

const toDecimal128 = (v) => {
  // Acepta number o string. Si viene vacío/null => 0
  if (v === null || v === undefined || v === "") {
    return mongoose.Types.Decimal128.fromString("0");
  }
  return mongoose.Types.Decimal128.fromString(String(v));
};

const buildWeek = (inputArray = [], fallbackArray = []) => {
  // inputArray: lo que manda el cliente
  // fallbackArray: valores actuales (para update)
  const inputMap = new Map();
  for (const item of Array.isArray(inputArray) ? inputArray : []) {
    const key = normalize(item?.sDia);
    if (DIAS_KEYS.includes(key)) {
      inputMap.set(key, item?.dValor);
    }
  }

  const fallbackMap = new Map();
  for (const item of Array.isArray(fallbackArray) ? fallbackArray : []) {
    const key = normalize(item?.sDia);
    if (DIAS_KEYS.includes(key)) {
      fallbackMap.set(key, item?.dValor);
    }
  }

  // Siempre regresamos los 7 días
  return DIAS_KEYS.map((d) => ({
    sDia: d,
    dValor: toDecimal128(
      inputMap.has(d) ? inputMap.get(d) : fallbackMap.get(d) ?? "0"
    ),
  }));
};

export const createEsquema = async (req, res) => {
  try {
    const { sNombre, aDia } = req.body;

    if (!sNombre || !String(sNombre).trim()) {
      return res.status(400).json({
        success: false,
        message: "Error al procesar la solicitud",
        error: { code: 400, details: "sNombre es requerido" },
      });
    }

    const nombre = String(sNombre).trim();

    // ✅ Validar duplicado (case-insensitive)
    const existente = await Esquema.findOne({
      sNombre: { $regex: `^${escapeRegex(nombre)}$`, $options: "i" },
    });

    if (existente) {
      return res.status(400).json({
        success: false,
        message: "Error al procesar la solicitud",
        error: { code: 400, details: "Ya existe un esquema con ese nombre" },
      });
    }

    const week = buildWeek(aDia);

    const nuevo = new Esquema({
      sNombre: nombre,
      aDia: week,
    });

    const guardado = await nuevo.save();

    return res.status(201).json({
      success: true,
      message: "Esquema creado exitosamente",
      data: guardado,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: "Error al procesar la solicitud",
      error: { code: 500, details: error.message },
    });
  }
};

function escapeRegex(str) {
  return String(str).replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}


export const getEsquemas = async (req, res) => {
  try {
    const esquemas = await Esquema.find().sort({ createdAt: -1 });
    return res.status(200).json({
      success: true,
      message: "Operación exitosa",
      data: esquemas,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: "Error al procesar la solicitud",
      error: { code: 500, details: error.message },
    });
  }
};

export const getEsquemaById = async (req, res) => {
  try {
    const { id } = req.params;

    const esquema = await Esquema.findById(id);
    if (!esquema) {
      return res.status(404).json({
        success: false,
        message: "Error al procesar la solicitud",
        error: { code: 404, details: "Esquema no encontrado" },
      });
    }

    return res.status(200).json({
      success: true,
      message: "Operación exitosa",
      data: esquema,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: "Error al procesar la solicitud",
      error: { code: 500, details: error.message },
    });
  }
};

export const updateEsquema = async (req, res) => {
  try {
    const { id } = req.params;
    const { sNombre, aDia } = req.body;

    const esquema = await Esquema.findById(id);
    if (!esquema) {
      return res.status(404).json({
        success: false,
        message: "Error al procesar la solicitud",
        error: { code: 404, details: "Esquema no encontrado" },
      });
    }

    if (sNombre && String(sNombre).trim()) esquema.sNombre = String(sNombre).trim();

    // Si mandan aDia, se reconstruye asegurando 7 días
    if (aDia !== undefined) {
      esquema.aDia = buildWeek(aDia, esquema.aDia);
    }

    const actualizado = await esquema.save();

    return res.status(200).json({
      success: true,
      message: "Esquema actualizado correctamente",
      data: actualizado,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: "Error al procesar la solicitud",
      error: { code: 500, details: error.message },
    });
  }
};

export const deleteEsquema = async (req, res) => {
  try {
    const { id } = req.params;

    const eliminado = await Esquema.findByIdAndDelete(id);
    if (!eliminado) {
      return res.status(404).json({
        success: false,
        message: "Error al procesar la solicitud",
        error: { code: 404, details: "Esquema no encontrado" },
      });
    }

    return res.status(200).json({
      success: true,
      message: "Esquema eliminado correctamente",
      data: eliminado,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: "Error al procesar la solicitud",
      error: { code: 500, details: error.message },
    });
  }
};

export const addEsquemaToUser = async (req, res) => {
  try {
    const { esquemaId, usuarioId } = req.params;

    // Validación ObjectId (evita errores por ids inválidos)
    if (
      !mongoose.Types.ObjectId.isValid(esquemaId) ||
      !mongoose.Types.ObjectId.isValid(usuarioId)
    ) {
      return res.status(400).json({
        success: false,
        message: "Error al procesar la solicitud",
        error: { code: 400, details: "ID inválido" },
      });
    }

    // Verifica que exista el esquema
    const esquema = await Esquema.findById(esquemaId);
    if (!esquema) {
      return res.status(404).json({
        success: false,
        message: "Error al procesar la solicitud",
        error: { code: 404, details: "Esquema no encontrado" },
      });
    }

    // Agrega el esquema al usuario (sin duplicados)
    const usuarioActualizado = await Usuario.findByIdAndUpdate(
      usuarioId,
      { $addToSet: { aEsquemas: esquemaId } },
      { new: true }
    );

    if (!usuarioActualizado) {
      return res.status(404).json({
        success: false,
        message: "Error al procesar la solicitud",
        error: { code: 404, details: "Usuario no encontrado" },
      });
    }

    return res.status(200).json({
      success: true,
      message: "Esquema agregado al usuario correctamente",
      data: usuarioActualizado,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: "Error al procesar la solicitud",
      error: { code: 500, details: error.message },
    });
  }
};

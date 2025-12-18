import mongoose from 'mongoose';

/**
 * Schema para registrar el historial de sincronizaciones
 * Útil para auditoría, debugging y recuperación de errores
 */
const OperacionSchema = new mongoose.Schema({
  tipoOperacion: {
    type: String,
    required: true,
    enum: [
      'CREAR_ORDEN',
      'ACTUALIZAR_ORDEN',
      'ELIMINAR_ORDEN',
      'ACTUALIZAR_INDICACIONES_ORDEN',
      'CREAR_PRODUCTO',
      'ACTUALIZAR_PRODUCTO',
      'ELIMINAR_PRODUCTO',
      'ACTUALIZAR_CANTIDAD_PRODUCTO',
      'AGREGAR_EXTRA_CONSUMOS',
      'ELIMINAR_EXTRA_CONSUMO',
      'ELIMINAR_CONSUMO'
    ]
  },
  idLocal: { type: String, required: true }, // UUID generado en frontend
  idMongoDB: { type: String, default: null }, // ID asignado después de crear en MongoDB
  datos: { type: mongoose.Schema.Types.Mixed, required: true },
  timestampLocal: { type: Date, required: true }, // Timestamp del frontend
  resultado: {
    type: String,
    enum: ['PENDIENTE', 'EXITOSO', 'ERROR'],
    default: 'PENDIENTE'
  },
  errorDetalle: { type: String, default: null }
}, { _id: true });

const SyncLogSchema = new mongoose.Schema({
  sIdUsuario: { type: String, required: true },
  sNombreUsuario: { type: String, required: true },
  dtFechaSincronizacion: { type: Date, default: Date.now, index: true },
  operaciones: [OperacionSchema],
  resumen: {
    totalOperaciones: { type: Number, default: 0 },
    exitosas: { type: Number, default: 0 },
    fallidas: { type: Number, default: 0 }
  },
  estadoGeneral: {
    type: String,
    enum: ['EN_PROCESO', 'COMPLETADO', 'COMPLETADO_CON_ERRORES', 'FALLIDO'],
    default: 'EN_PROCESO'
  }
}, {
  timestamps: true
});

// Índices para consultas frecuentes
SyncLogSchema.index({ sIdUsuario: 1, dtFechaSincronizacion: -1 });
SyncLogSchema.index({ estadoGeneral: 1 });

export default mongoose.model('SyncLog', SyncLogSchema);

import mongoose from 'mongoose';

const VarianteSchema = new mongoose.Schema({
  sVariante: { type: String, required: true }
});

// Esquema para extras dentro de un consumo
const ExtraConsumoSchema = new mongoose.Schema({
  sIdExtra: { type: mongoose.Schema.Types.ObjectId }, // Referencia al catálogo de extras (opcional)
  sNombre: { type: String, required: true },
  iCostoReal: { type: Number, required: true },
  iCostoPublico: { type: Number, required: true },
  sURLImagen: { type: String }
});

// Esquema para cada consumo individual (ej: cada enchilada de una orden de 4)
const ConsumoSchema = new mongoose.Schema({
  iIndex: { type: Number, required: true }, // Índice del consumo (1, 2, 3, 4...)
  aExtras: { type: [ExtraConsumoSchema], default: [] } // Extras aplicados a este consumo específico
});

const OrdenProductoSchema = new mongoose.Schema({
  sIdOrdenMongoDB: { type: mongoose.Schema.Types.ObjectId, required: true },
  sIdProductoMongoDB: {type: mongoose.Schema.Types.ObjectId,ref: "Producto",required: false},
  sNombre: { type: String, required: true },
  iCostoReal: { type: Number, required: true },
  iCostoPublico: { type: Number, required: true },
  sURLImagen: { type: String },
  sIndicaciones: { type: String, default: 'Sin indicaciones adicionales.' },
  iIndexVarianteSeleccionada: { type: Number, required: true },
  aVariantes: { type: [VarianteSchema], required: true },
  iCantidad: { type: Number, required: true, default: 1, min: 1 },
  
  // Array de consumos individuales con sus extras (nueva funcionalidad - Pantalla 2)
  aConsumos: { type: [ConsumoSchema], default: [] },
  
  // Campo legacy para compatibilidad con código existente
  // Se mantiene para no romper endpoints existentes
  aExtras: [
    {
      sNombre: { type: String, required: true },
      iCostoReal: { type: Number, required: true },
      iCostoPublico: { type: Number, required: true },
      sURLImagen: { type: String }
    }
  ],
  
  iTipoProducto: {
    type: Number,
    enum: [1, 2], // 1 = Platillo, 2 = Bebida
    required: true
  }
},
{
  toJSON: { virtuals: true },
  toObject: { virtuals: true }
});

// Middleware pre-save para sincronizar consumos con cantidad
OrdenProductoSchema.pre('save', function(next) {
  // Si aConsumos está vacío pero iCantidad > 0, inicializar consumos
  if ((!this.aConsumos || this.aConsumos.length === 0) && this.iCantidad > 0) {
    this.aConsumos = [];
    for (let i = 1; i <= this.iCantidad; i++) {
      this.aConsumos.push({ iIndex: i, aExtras: [] });
    }
  }
  
  // Si la cantidad cambió, ajustar consumos
  if (this.aConsumos && this.aConsumos.length !== this.iCantidad) {
    const currentLength = this.aConsumos.length;
    
    if (this.iCantidad > currentLength) {
      // Agregar nuevos consumos
      for (let i = currentLength + 1; i <= this.iCantidad; i++) {
        this.aConsumos.push({ iIndex: i, aExtras: [] });
      }
    } else if (this.iCantidad < currentLength) {
      // Remover consumos del final (solo si no tienen extras)
      // Nota: El frontend debe manejar la lógica de confirmar eliminación de consumos con extras
      this.aConsumos = this.aConsumos.slice(0, this.iCantidad);
      // Re-indexar
      this.aConsumos.forEach((consumo, idx) => {
        consumo.iIndex = idx + 1;
      });
    }
  }
  
  next();
});

// Virtual: Total de extras usando el nuevo sistema de consumos
OrdenProductoSchema.virtual('iTotalRealExtrasConsumos').get(function () {
  if (!this.aConsumos || this.aConsumos.length === 0) return 0;
  return this.aConsumos.reduce((total, consumo) => {
    if (!consumo.aExtras || consumo.aExtras.length === 0) return total;
    return total + consumo.aExtras.reduce((subtotal, extra) => subtotal + extra.iCostoReal, 0);
  }, 0);
});

OrdenProductoSchema.virtual('iTotalPublicoExtrasConsumos').get(function () {
  if (!this.aConsumos || this.aConsumos.length === 0) return 0;
  return this.aConsumos.reduce((total, consumo) => {
    if (!consumo.aExtras || consumo.aExtras.length === 0) return total;
    return total + consumo.aExtras.reduce((subtotal, extra) => subtotal + extra.iCostoPublico, 0);
  }, 0);
});

// Virtual: Total de extras usando el sistema legacy (aExtras directo)
OrdenProductoSchema.virtual('iTotalRealExtrasOrden').get(function () {
  // Primero intentar con el nuevo sistema de consumos
  const totalConsumos = this.iTotalRealExtrasConsumos;
  if (totalConsumos > 0) return totalConsumos;
  
  // Fallback al sistema legacy
  if (!this.aExtras || this.aExtras.length === 0) return 0;
  return this.aExtras.reduce((acumulador, extra) => acumulador + extra.iCostoReal, 0);
});

OrdenProductoSchema.virtual('iTotalPublicoExtrasOrden').get(function () {
  // Primero intentar con el nuevo sistema de consumos
  const totalConsumos = this.iTotalPublicoExtrasConsumos;
  if (totalConsumos > 0) return totalConsumos;
  
  // Fallback al sistema legacy
  if (!this.aExtras || this.aExtras.length === 0) return 0;
  return this.aExtras.reduce((acumulador, extra) => acumulador + extra.iCostoPublico, 0);
});

// Virtual para el total general real (producto * cantidad + extras)
OrdenProductoSchema.virtual('iTotalGeneralRealOrdenProducto').get(function () {
  const totalProducto = this.iCostoReal * this.iCantidad;
  const totalExtras = this.iTotalRealExtrasOrden;
  return totalProducto + totalExtras;
});

// Virtual para el total general público (producto * cantidad + extras)
OrdenProductoSchema.virtual('iTotalGeneralPublicoOrdenProducto').get(function () {
  const totalProducto = this.iCostoPublico * this.iCantidad;
  const totalExtras = this.iTotalPublicoExtrasOrden;
  return totalProducto + totalExtras;
});

// Virtual para verificar si tiene extras en algún consumo
OrdenProductoSchema.virtual('bTieneExtras').get(function () {
  // Verificar en consumos
  if (this.aConsumos && this.aConsumos.length > 0) {
    const tieneExtrasEnConsumos = this.aConsumos.some(c => c.aExtras && c.aExtras.length > 0);
    if (tieneExtrasEnConsumos) return true;
  }
  
  // Verificar en sistema legacy
  return this.aExtras && this.aExtras.length > 0;
});

export default mongoose.model('OrdenProducto', OrdenProductoSchema);

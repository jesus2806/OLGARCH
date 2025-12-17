import mongoose from 'mongoose';
import Counter from './Counter.js'; 

const OrdenSchema = new mongoose.Schema({
  sIdentificadorOrden: { type: String, required: true },
  iMesa: { type: Number, required: true },
  iTipoOrden: {type: Number, 
              required: true, 
              enum: [1, 2], // 1 = Orden primaria, 2 = Orden secundaria 
              default: 1},
  iNumeroOrden: { type: Number, autoIncrement: true }, // Mongoose no maneja autoincrement, debe controlarse en código
  sUsuarioMesero:  { type: String, required: true },
  sIdMongoDBMesero:  { type: String, required: true },
  aProductos: [
    {
      type: mongoose.Schema.Types.ObjectId,
      ref: 'OrdenProducto'
    }
  ],
  dtFechaAlta: { type: Date, default: Date.now, index: true },
  dtFechaFin: { type: Date, default: () => null },
  iTotalOrden: { type: Number, default:0},
  iEstatus: {
    type: Number,
    enum: [0,1, 2, 3, 4, 5], //0 = Pendiente, 1 = Confirmada, 2 = En preparación, 3 = Preparada, 4 = Entregada, 5 = Pagada
    required: true,
    default: 0
  },
  bOrdenModificada: { type: Boolean, default: false },
  iTipoPago: {
    type: Number,
    enum: [0, 1, 2], // 1 = Efectivo, 2 = Transferencia
    default: 0
  },
  iTotalExtrasOrden: { type: Number, default: 0 },
  sIndicaciones: { type: String, default: '' } // Indicaciones generales de la orden (Pantalla 6)
},{
  toJSON: { virtuals: true },
  toObject: { virtuals: true }
});


// Virtual populate: Obtiene las órdenes secundarias
OrdenSchema.virtual('ordenesSecundarias', {
  ref: 'Orden',
  localField: 'sIdentificadorOrden',
  foreignField: 'sIdentificadorOrden',
  match: { iTipoOrden: 2 },
  justOne: false
});

OrdenSchema.virtual('iTotalRealOrden').get(function () {
  let total = 0;

  // Sumar el total de los productos de la orden
  if (this.aProductos && this.aProductos.length > 0) {
    total += this.aProductos.reduce((acum, producto) => {
      return acum + (producto.iTotalGeneralRealOrdenProducto || 0);
    }, 0);
  }

  // Si es orden primaria, sumar los totales de las órdenes secundarias (asegúrate de hacer populate de 'ordenesSecundarias')
  if (this.iTipoOrden === 1 && this.ordenesSecundarias && Array.isArray(this.ordenesSecundarias)) {
    total += this.ordenesSecundarias.reduce((acum, ordenSec) => {
      if (ordenSec.aProductos && ordenSec.aProductos.length > 0) {
        return acum + ordenSec.aProductos.reduce((acumProd, producto) => {
          return acumProd + (producto.iTotalGeneralRealOrdenProducto || 0);
        }, 0);
      }
      return acum;
    }, 0);
  }

  return total;
});



OrdenSchema.virtual('iTotalPublicoOrden').get(function () {
  let total = 0;

  // Sumar el total público de los productos de la orden
  if (this.aProductos && this.aProductos.length > 0) {
    total += this.aProductos.reduce((acum, producto) => {
      return acum + (producto.iTotalGeneralPublicoOrdenProducto || 0);
    }, 0);
  }

  // Si es una orden primaria, sumar el total público de los productos en sus órdenes secundarias
  if (this.iTipoOrden === 1 && this.ordenesSecundarias && Array.isArray(this.ordenesSecundarias)) {
    total += this.ordenesSecundarias.reduce((acum, ordenSec) => {
      if (ordenSec.aProductos && ordenSec.aProductos.length > 0) {
        return acum + ordenSec.aProductos.reduce((acumProd, producto) => {
          return acumProd + (producto.iTotalGeneralPublicoOrdenProducto || 0);
        }, 0);
      }
      return acum;
    }, 0);
  }

  return total;
});


// Middleware pre-save para autoincrementar iNumeroOrden
OrdenSchema.pre('save', async function (next) {
  const doc = this;
  
  // Solo asignar el número de orden si es un documento nuevo
  if (doc.isNew) {
    try {
      // Buscar y actualizar (o crear) el contador para 'ordenNumero'
      const counter = await Counter.findByIdAndUpdate(
        { _id: 'ordenNumero' },
        { $inc: { sequence_value: 1 } },
        { new: true, upsert: true }
      );

      // Asigna el valor incrementado al campo iNumeroOrden
      doc.iNumeroOrden = counter.sequence_value;
      next();
    } catch (error) {
      next(error);
    }
  } else {
    next();
  }
});


export default mongoose.model('Orden', OrdenSchema);

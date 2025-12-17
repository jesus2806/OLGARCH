import mongoose from 'mongoose';

const HistoricoSchema = new mongoose.Schema({
  sIdOrdenPrimaria: { type: String, required: true },
  sIdentificadorOrdenPrimaria: { type: String, required: true },
  iMesa: { type: Number, required: true },
  iNumeroOrden: { type: Number, autoIncrement: true },
  sUsuarioMesero:  { type: String, required: true },
  dtFechaAlta: { type: Date, default: () => null, index: true },
  dtFechaFin: { type: Date, default: Date.now, index: true },
  iTotalCostoPublico: { type: Number, default:0},
  iTotalCostoReal: { type: Number, default:0},
  iTotalBanco:{ type: Number, default:0},
  iTotalEfectivo:{ type: Number, default:0},
  iTipoPago: {
    type: Number,
    enum: [0, 1, 2, 3], // 1 = Efectivo, 2 = Transferencia, 3 = Pago combinado
    default: 0
  },
  iEstatus: {
    type: Number,
    enum: [0,1, 2, 3, 4, 5], //0 = Pendiente, 1 = Confirmada, 2 = En preparación, 3 = Preparada, 4 = Entregada, 5 = Pagada
    required: true,
    default: 5
  }
});


export default mongoose.model('Historico', HistoricoSchema);

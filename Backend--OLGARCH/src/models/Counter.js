import mongoose from 'mongoose';

const CounterSchema = new mongoose.Schema({
  _id: { type: String, required: true }, // nombre de la secuencia, por ejemplo: 'ordenNumero'
  sequence_value: { type: Number, default: 0 }
});

export default mongoose.model('Counter', CounterSchema);
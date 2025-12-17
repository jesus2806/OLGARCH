import mongoose, { Schema } from 'mongoose';

// Esquema para imágenes
const ImagenSchema = new Schema({
  sURLImagen: { 
    type: String, 
    required: true,
    match: [/^https?:\/\/.+\.(jpg|jpeg|png|gif|webp)$/, 'URL de imagen inválida']
  }
});

// Esquema principal de Extra
const ExtraSchema = new Schema({
  sNombre: { type: String, required: true },
  iCostoReal: { type: Number, required: true },
  iCostoPublico: { type: Number, required: true },
  imagenes: { type: [ImagenSchema], default: [] },
  bActivo: { type: Boolean, default: true }
}, {
  timestamps: true
});

// Índices
ExtraSchema.index({ sNombre: 1 });
ExtraSchema.index({ bActivo: 1 });

export default mongoose.model('Extra', ExtraSchema);

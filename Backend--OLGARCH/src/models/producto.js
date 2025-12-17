import mongoose, { Schema } from 'mongoose';

// Esquema para imágenes
const ImagenSchema = new Schema({
  sURLImagen: { 
    type: String, 
    required: true,
    match: [/^https?:\/\/.+\.(jpg|jpeg|png|gif|webp)$/, 'URL de imagen inválida']
  }
});

// Esquema para variantes
const VarianteSchema = new Schema({
  sVariante: { type: String, required: true }
});

// Esquema principal de tb_Producto
const ProductoSchema = new Schema({
  sNombre: { type: String, required: true },
  iCostoReal: { type: Number, required: true },
  iCostoPublico: { type: Number, required: true },
  imagenes: { type: [ImagenSchema], required: true }, // Arreglo de objetos tipo Imagen
  aVariantes: { type: [VarianteSchema], required: true }, // Arreglo de objetos tipo Variante
  iTipoProducto: { 
    type: Number, 
    required: true,
    enum: {
      values: [1, 2, 3],
      message: '{VALUE} no es un tipo de producto válido'
    }
  }
}, {
  timestamps: true // Agrega createdAt y updatedAt automáticamente
});

// Índices (opcional)
ProductoSchema.index({ sNombre: 1 });

export default mongoose.model('Producto', ProductoSchema);

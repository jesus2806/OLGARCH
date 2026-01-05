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

// NUEVO: Ingredientes usados por producto
const IngredienteUsoSchema = new Schema(
  {
    sIdIngrediente: {
      type: Schema.Types.ObjectId,
      ref: "Ingrediente",
      required: true
    },
    iCantidadUso: {
      type: Number,
      required: true,
      min: [0, "iCantidadUso no puede ser negativo"]
    }
  },
  { _id: false }
);

// Esquema principal de tb_Producto
const ProductoSchema = new Schema({
  sNombre: { type: String, required: true },
  iCostoReal: { type: Number, required: true },
  iCostoPublico: { type: Number, required: true },
  imagenes: { type: [ImagenSchema], required: true },
  aVariantes: { type: [VarianteSchema], required: true },
  iTipoProducto: {
    type: Number,
    required: true,
    enum: {
      values: [1, 2, 3],
      message: '{VALUE} no es un tipo de producto válido'
    }
  },

  // ✅ NUEVO: lista de ingredientes del producto
  aIngredientes: {
    type: [IngredienteUsoSchema],
    default: []
  }
}, {
  timestamps: true
});

ProductoSchema.index({ sNombre: 1 });

export default mongoose.model('Producto', ProductoSchema);

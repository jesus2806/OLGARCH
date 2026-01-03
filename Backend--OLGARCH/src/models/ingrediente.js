import mongoose, { Schema } from "mongoose";

const IngredienteSchema = new Schema(
  {
    sNombre: { type: String, required: true, trim: true },
    iCantidadEnAlmacen: { type: Number, required: true, default: 0, min: 0 },
    iCantidadMinima: { type: Number, required: true, default: 0, min: 0 },
    sUnidad: { type: String, required: true, trim: true }, // ej: "kg", "g", "pza", "ml", "l"
    iCostoUnidad: { type: Number, required: true, default: 0, min: 0 },
  },
  {
    timestamps: true,
  }
);

// Índices (opcional)
IngredienteSchema.index({ sNombre: 1 });

export default mongoose.model("Ingrediente", IngredienteSchema);

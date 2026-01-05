import mongoose from "mongoose";

const AsistenciaSchema = new mongoose.Schema(
  {
    oUsuario: { type: mongoose.Schema.Types.ObjectId, ref: "Usuario", required: true },

    // Día “lógico” (ej. "2026-01-04") para evitar problemas de timezone
    sDia: { type: String, required: true },

    sEstatus: {
      type: String,
      enum: ["sin_marcar", "presente", "ausente", "tarde", "justificado", "vacaciones", "incapacidad"],
      default: "sin_marcar",
      index: true,
    },

    dtCheckIn: { type: Date, default: null },
    dtCheckOut: { type: Date, default: null },

    sNotas: { type: String, default: "" },

    // quién pasó lista (admin/supervisor)
    oTomadaPor: { type: mongoose.Schema.Types.ObjectId, ref: "Usuario", default: null },
  },
  { timestamps: true }
);

// Un solo registro por usuario por día
AsistenciaSchema.index({ oUsuario: 1, sDia: 1 }, { unique: true });

export default mongoose.model("Asistencia", AsistenciaSchema);
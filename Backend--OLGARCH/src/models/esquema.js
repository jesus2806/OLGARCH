import mongoose from "mongoose";

const normalize = (s) =>
  String(s || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .trim();

// Días canónicos (sin acentos para evitar problemas)
export const DIAS_KEYS = [
  "lunes",
  "martes",
  "miercoles",
  "jueves",
  "viernes",
  "sabado",
  "domingo",
];

const diaSchema = new mongoose.Schema(
  {
    sDia: {
      type: String,
      enum: DIAS_KEYS,
      required: true,
    },
    dValor: {
      type: mongoose.Schema.Types.Decimal128,
      required: true,
      default: mongoose.Types.Decimal128.fromString("0"),
    },
  },
  { _id: false }
);

const EsquemaSchema = new mongoose.Schema(
  {
    sNombre: { type: String, required: true, trim: true },
    aDia: {
      type: [diaSchema],
      required: true,
      validate: [
        {
          validator: function (arr) {
            if (!Array.isArray(arr) || arr.length !== 7) return false;
            const set = new Set(arr.map((x) => normalize(x?.sDia)));
            return set.size === 7 && DIAS_KEYS.every((d) => set.has(d));
          },
          message:
            "aDia debe contener exactamente los 7 días de la semana (lunes..domingo) sin repetidos.",
        },
      ],
    },
  },
  { timestamps: true }
);

export default mongoose.model("Esquema", EsquemaSchema);

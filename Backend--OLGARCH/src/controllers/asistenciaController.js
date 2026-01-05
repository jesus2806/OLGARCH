import Usuario from "../models/usuario.js";
import Asistencia from "../models/asistencia.js";

// helper: fecha "YYYY-MM-DD" en zona horaria MX (no depende del timezone del servidor)
const todayMx = () => {
  const fmt = new Intl.DateTimeFormat("en-CA", { timeZone: "America/Mexico_City" });
  return fmt.format(new Date()); // "YYYY-MM-DD"
};

export const getRoster = async (req, res) => {
  try {
    const sDia = String(req.query.dia || todayMx());
    const q = String(req.query.q || "").trim();

    // Ajusta este filtro a tu negocio (ej: solo empleados)
    // Si iRol=1 es admin, normalmente no pasas lista a admins:
    const usuarioFiltro = { iRol: { $ne: 1 } };

    if (q) {
      usuarioFiltro.$or = [
        { sNombre: new RegExp(q, "i") },
        { sApellidoPaterno: new RegExp(q, "i") },
        { sApellidoMaterno: new RegExp(q, "i") },
        { sUsuario: new RegExp(q, "i") },
      ];
    }

    const usuarios = await Usuario.find(usuarioFiltro)
      .select("sNombre sApellidoPaterno sApellidoMaterno sUsuario iRol")
      .lean();

    const ids = usuarios.map((u) => u._id);

    const asistencias = await Asistencia.find({ sDia, oUsuario: { $in: ids } }).lean();
    const map = new Map(asistencias.map((a) => [String(a.oUsuario), a]));

    const roster = usuarios.map((u) => {
      const a = map.get(String(u._id));
      return {
        usuario: u,
        asistencia: a
          ? {
              _id: a._id,
              sDia: a.sDia,
              sEstatus: a.sEstatus,
              dtCheckIn: a.dtCheckIn,
              dtCheckOut: a.dtCheckOut,
              sNotas: a.sNotas,
              oTomadaPor: a.oTomadaPor,
            }
          : {
              sDia,
              sEstatus: "sin_marcar",
              dtCheckIn: null,
              dtCheckOut: null,
              sNotas: "",
            },
      };
    });

    return res.json({ success: true, data: { sDia, roster } });
  } catch (error) {
    return res.status(500).json({ success: false, message: "Error al obtener roster", error: String(error) });
  }
};

export const upsertBulk = async (req, res) => {
  try {
    const sDia = String(req.body?.sDia || "").trim();
    const items = Array.isArray(req.body?.items) ? req.body.items : [];

    if (!/^\d{4}-\d{2}-\d{2}$/.test(sDia)) {
      return res.status(400).json({ success: false, message: "sDia inválido (YYYY-MM-DD)" });
    }
    if (!items.length) {
      return res.status(400).json({ success: false, message: "items es requerido" });
    }

    const allowed = new Set(["sin_marcar", "presente", "ausente", "tarde", "justificado", "vacaciones", "incapacidad"]);

    const ops = items.map((it) => {
      const oUsuario = it.oUsuario;
      const sEstatus = String(it.sEstatus || "sin_marcar");
      if (!oUsuario) throw new Error("Falta oUsuario en un item");
      if (!allowed.has(sEstatus)) throw new Error(`Estatus inválido: ${sEstatus}`);

      const update = {
        $set: {
          sEstatus,
          dtCheckIn: it.dtCheckIn ?? null,
          dtCheckOut: it.dtCheckOut ?? null,
          sNotas: String(it.sNotas || ""),
          oTomadaPor: req.user?.id || null, // asumiendo middleware auth que setea req.user
        },
        $setOnInsert: { oUsuario, sDia },
      };

      return {
        updateOne: {
          filter: { oUsuario, sDia },
          update,
          upsert: true,
        },
      };
    });

    await Asistencia.bulkWrite(ops, { ordered: false });

    return res.json({ success: true, message: "Asistencia guardada" });
  } catch (error) {
    return res.status(500).json({ success: false, message: "Error al guardar asistencia", error: String(error) });
  }
};

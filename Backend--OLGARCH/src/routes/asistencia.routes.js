import { Router } from "express";
import { getRoster, upsertBulk } from "../controllers/asistenciaController.js";
import { authMiddleware } from '../middlewares/auth.middleware.js';

const router = Router();

// obtener lista del día
router.get("/asistencia/roster", authMiddleware, getRoster);

// guardar pase de lista (bulk)
router.post("/asistencia/bulk", authMiddleware, upsertBulk);

export default router;

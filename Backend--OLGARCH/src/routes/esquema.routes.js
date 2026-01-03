import { Router } from "express";
import {
  createEsquema,
  getEsquemas,
  getEsquemaById,
  updateEsquema,
  deleteEsquema,
  addEsquemaToUser
} from "../controllers/esquemaController.js";

const router = Router();

router.post("/esquemas", createEsquema);
router.get("/esquemas", getEsquemas);
router.get("/esquemas/:id", getEsquemaById);
router.put("/esquemas/:id", updateEsquema);
router.delete("/esquemas/:id", deleteEsquema);
router.post("/esquemas/:esquemaId/usuarios/:usuarioId", addEsquemaToUser);

export default router;

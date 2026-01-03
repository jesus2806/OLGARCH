import { Router } from "express";
import {
  createIngrediente,
  getIngredientes,
  getIngredienteById,
  updateIngrediente,
  deleteIngrediente,
  searchIngredientes,
} from "../controllers/ingredienteController.js";

import { authMiddleware } from "../middlewares/auth.middleware.js";

const router = Router();

/**
 * POST /api/ingredientes
 */
router.post("/ingredientes", authMiddleware, createIngrediente);

/**
 * GET /api/ingredientes
 */
router.get("/ingredientes", authMiddleware, getIngredientes);

/**
 * GET /api/ingredientes/:id
 */
router.get("/ingredientes/:id", authMiddleware, getIngredienteById);

/**
 * PUT /api/ingredientes/:id
 */
router.put("/ingredientes/:id", authMiddleware, updateIngrediente);

/**
 * DELETE /api/ingredientes/:id
 */
router.delete("/ingredientes/:id", authMiddleware, deleteIngrediente);

/**
 * POST /api/ingredientes/search
 */
router.post("/ingredientes/search", authMiddleware, searchIngredientes);

export default router;
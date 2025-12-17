// routes/extra.routes.js
import { Router } from 'express';
import {
  createExtra,
  getExtras,
  getExtraById,
  updateExtra,
  deleteExtra,
  searchExtras
} from '../controllers/extraController.js';
import { authMiddleware } from '../middlewares/auth.middleware.js';

const router = Router();

/**
 * Crear un nuevo extra
 * POST /api/extras
 */
router.post('/extras', authMiddleware, createExtra);

/**
 * Obtener todos los extras activos
 * GET /api/extras
 */
router.get('/extras', authMiddleware, getExtras);

/**
 * Buscar extras por nombre (Pantalla 3)
 * POST /api/extras/search
 */
router.post('/extras/search', authMiddleware, searchExtras);

/**
 * Obtener un extra por ID
 * GET /api/extras/:id
 */
router.get('/extras/:id', authMiddleware, getExtraById);

/**
 * Actualizar un extra
 * PUT /api/extras/:id
 */
router.put('/extras/:id', authMiddleware, updateExtra);

/**
 * Eliminar un extra (soft delete)
 * DELETE /api/extras/:id
 */
router.delete('/extras/:id', authMiddleware, deleteExtra);

export default router;

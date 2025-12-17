import express from 'express';
import {
  createHistorico,
  getHistoricoResumen,
  getHistoricoById,
  updateHistorico,
  deleteHistorico,
  updateOrdenAndRegisterHistorico
} from '../controllers/historicoController.js';

import { authMiddleware } from '../middlewares/auth.middleware.js';

const router = express.Router();

router.post('/historicos', authMiddleware, createHistorico);
router.post('/historico/crear/:id', authMiddleware,  updateOrdenAndRegisterHistorico);
router.post('/historico/resumen', authMiddleware, getHistoricoResumen);
router.get('/historicos/:id', authMiddleware, getHistoricoById);
router.put('/historicos/:id', authMiddleware, updateHistorico);
router.delete('/historicos/:id', authMiddleware, deleteHistorico);

export default router;
// routes/usuario.routes.js
import { Router } from 'express';
import {
  createUser,
  getUsers,
  getUserById,
  updateUser,
  deleteUser,
  login,
} from '../controllers/usuarioController.js';

import { authMiddleware } from '../middlewares/auth.middleware.js';

const router = Router();

// Ruta de autenticación (login)
router.post('/login', login);

// Rutas protegidas del CRUD
router.post('/usuarios', authMiddleware, createUser);
router.get('/usuarios', authMiddleware, getUsers);
router.get('/usuarios/:id', authMiddleware, getUserById);
router.put('/usuarios/:id', authMiddleware, updateUser);
router.delete('/usuarios/:id', authMiddleware, deleteUser);

export default router;
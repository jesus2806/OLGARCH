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

const router = Router();

// Ruta de autenticación (login)
router.post('/login', login);

// Rutas protegidas del CRUD
router.post('/usuarios', createUser);
router.get('/usuarios', getUsers);
router.get('/usuarios/:id', getUserById);
router.put('/usuarios/:id', updateUser);
router.delete('/usuarios/:id', deleteUser);

export default router;
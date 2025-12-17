// routes/producto.routes.js

import { Router } from 'express';
import {
  createProducto,
  getProductos,
  getProductoById,
  updateProducto,
  deleteProducto,
  searchProductos
} from '../controllers/productoController.js';

import { authMiddleware } from '../middlewares/auth.middleware.js';

const router = Router();

// Rutas protegidas del CRUD para Productos

/**
 * Crea un nuevo producto
 * POST /api/productos
 */
router.post('/productos', authMiddleware, createProducto);

/**
 * Obtiene todos los productos
 * GET /api/productos
 */
router.get('/productos', authMiddleware, getProductos);

/**
 * Obtiene un producto por su ID
 * GET /api/productos/:id
 */
router.get('/productos/:id', authMiddleware, getProductoById);

/**
 * Actualiza un producto existente
 * PUT /api/productos/:id
 */
router.put('/productos/:id', authMiddleware, updateProducto);

/**
 * Elimina un producto
 * DELETE /api/productos/:id
 */
router.delete('/productos/:id', authMiddleware, deleteProducto);

router.post('/productos/search', authMiddleware, searchProductos);

export default router;

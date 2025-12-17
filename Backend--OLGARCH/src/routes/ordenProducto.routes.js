// routes/ordenProducto.routes.js
import { Router } from 'express';
import { 
  createOrdenProducto, 
  getOrdenProductos, 
  getOrdenProductoById, 
  updateOrdenProducto, 
  deleteOrdenProducto,
  // Nuevos endpoints para gestión de consumos y extras
  getConsumos,
  addExtraToConsumos,
  removeExtraFromConsumo,
  deleteConsumo,
  updateCantidad,
  checkTieneExtras
} from '../controllers/ordenProductoController.js';
import { authMiddleware } from '../middlewares/auth.middleware.js';

const router = Router();

// ============================================================
// ENDPOINTS EXISTENTES (CRUD básico)
// ============================================================

/**
 * Crear un nuevo producto de orden
 * POST /api/orden-productos/
 */
router.post('/orden-productos/', authMiddleware, createOrdenProducto);

/**
 * Obtener todos los productos de una orden
 * GET /api/orden-productos/:idOrden
 */
router.get('/orden-productos/orden/:idOrden', authMiddleware, getOrdenProductos);

/**
 * Obtener un producto de orden por ID
 * GET /api/orden-productos/:id
 */
router.get('/orden-productos/:id', authMiddleware, getOrdenProductoById);

/**
 * Actualizar un producto de orden
 * PUT /api/orden-productos/:id
 */
router.put('/orden-productos/:id', authMiddleware, updateOrdenProducto);

/**
 * Eliminar un producto de orden
 * DELETE /api/orden-productos/:id
 */
router.delete('/orden-productos/:id', authMiddleware, deleteOrdenProducto);

// ============================================================
// NUEVOS ENDPOINTS PARA MOCKUPS (Gestión de consumos y extras)
// ============================================================

/**
 * Obtener consumos de un producto (Pantalla 2)
 * GET /api/orden-productos/:id/consumos
 * 
 * Retorna la lista de consumos individuales con sus extras
 */
router.get('/orden-productos/:id/consumos', authMiddleware, getConsumos);

/**
 * Agregar extra a consumos específicos (Pantalla 3 y 4)
 * POST /api/orden-productos/:id/consumos/extras
 * 
 * Body:
 * {
 *   "extra": {
 *     "sIdExtra": "optional",
 *     "sNombre": "Pollo",
 *     "iCostoReal": 25,
 *     "iCostoPublico": 30,
 *     "sURLImagen": "url"
 *   },
 *   "aIndexConsumos": [1, 3]
 * }
 */
router.post('/orden-productos/:id/consumos/extras', authMiddleware, addExtraToConsumos);

/**
 * Eliminar extra de un consumo específico (Pantalla 2 - ícono basura)
 * DELETE /api/orden-productos/:id/consumos/:indexConsumo/extras/:idExtra
 */
router.delete('/orden-productos/:id/consumos/:indexConsumo/extras/:idExtra', authMiddleware, removeExtraFromConsumo);

/**
 * Eliminar un consumo específico (Escenario 3 - decrementar con extras)
 * DELETE /api/orden-productos/:id/consumos/:indexConsumo
 */
router.delete('/orden-productos/:id/consumos/:indexConsumo', authMiddleware, deleteConsumo);

/**
 * Actualizar cantidad de un producto (Pantalla 1 - incrementar/decrementar)
 * PATCH /api/orden-productos/:id/cantidad
 * 
 * Body: { "iCantidad": 5 }
 */
router.patch('/orden-productos/:id/cantidad', authMiddleware, updateCantidad);

/**
 * Verificar si un producto tiene extras
 * GET /api/orden-productos/:id/tiene-extras
 */
router.get('/orden-productos/:id/tiene-extras', authMiddleware, checkTieneExtras);

export default router;

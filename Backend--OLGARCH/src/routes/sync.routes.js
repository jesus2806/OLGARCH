/**
 * Rutas de Sincronización Unificada
 * 
 * Endpoint principal: POST /api/sync/ordenes
 * 
 * Este endpoint recibe todas las operaciones acumuladas desde el frontend
 * y las procesa de manera transaccional.
 */

import { Router } from 'express';
import {
  sincronizarOrdenes,
  obtenerHistorialSync,
  obtenerDetalleSync,
  verificarEstadoSync
} from '../controllers/syncController.js';
import { authMiddleware } from '../middlewares/auth.middleware.js';

const router = Router();

/**
 * Verificar estado del servicio de sincronización
 * GET /api/sync/status
 * 
 * Útil para verificar conectividad antes de sincronizar
 */
router.get('/sync/status', authMiddleware, verificarEstadoSync);

/**
 * Sincronizar operaciones de órdenes (ENDPOINT PRINCIPAL)
 * POST /api/sync/ordenes
 * 
 * Body:
 * {
 *   "operaciones": [
 *     {
 *       "tipoOperacion": "CREAR_ORDEN",
 *       "idLocal": "uuid-generado-en-frontend",
 *       "datos": {
 *         "sIdentificadorOrden": "ORD-001",
 *         "iMesa": 5,
 *         "sUsuarioMesero": "Juan Pérez",
 *         "sIdMongoDBMesero": "64a5..."
 *       },
 *       "timestampLocal": "2024-01-15T10:30:00.000Z"
 *     },
 *     {
 *       "tipoOperacion": "CREAR_PRODUCTO",
 *       "idLocal": "uuid-producto-001",
 *       "datos": {
 *         "sIdOrdenMongoDB": "uuid-generado-en-frontend", // Referencia al idLocal de la orden
 *         "sNombre": "Enchiladas",
 *         "iCostoReal": 50,
 *         "iCostoPublico": 85,
 *         "iCantidad": 3,
 *         ...
 *       },
 *       "timestampLocal": "2024-01-15T10:30:05.000Z"
 *     }
 *   ]
 * }
 * 
 * Respuesta exitosa:
 * {
 *   "success": true,
 *   "message": "Sincronización completada. 5 exitosas, 0 fallidas.",
 *   "data": {
 *     "syncLogId": "64a5...",
 *     "resumen": {
 *       "totalOperaciones": 5,
 *       "exitosas": 5,
 *       "fallidas": 0
 *     },
 *     "estadoGeneral": "COMPLETADO",
 *     "resultados": [...],
 *     "idMapping": {
 *       "uuid-local-orden": "64a5...mongoId",
 *       "uuid-local-producto": "64b6...mongoId"
 *     }
 *   }
 * }
 * 
 * Tipos de operación soportados:
 * - CREAR_ORDEN
 * - ACTUALIZAR_ORDEN
 * - ELIMINAR_ORDEN
 * - ACTUALIZAR_INDICACIONES_ORDEN
 * - CREAR_PRODUCTO
 * - ACTUALIZAR_PRODUCTO
 * - ELIMINAR_PRODUCTO
 * - ACTUALIZAR_CANTIDAD_PRODUCTO
 * - AGREGAR_EXTRA_CONSUMOS
 * - ELIMINAR_EXTRA_CONSUMO
 * - ELIMINAR_CONSUMO
 */
router.post('/sync/ordenes', authMiddleware, sincronizarOrdenes);

/**
 * Obtener historial de sincronizaciones del usuario
 * GET /api/sync/historial?limite=10&pagina=1
 * 
 * Query params:
 * - limite: Número de registros por página (default: 10)
 * - pagina: Número de página (default: 1)
 */
router.get('/sync/historial', authMiddleware, obtenerHistorialSync);

/**
 * Obtener detalle de una sincronización específica
 * GET /api/sync/:id
 */
router.get('/sync/:id', authMiddleware, obtenerDetalleSync);

export default router;

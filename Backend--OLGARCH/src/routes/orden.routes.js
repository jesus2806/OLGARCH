import { Router } from 'express';
import { createOrden, 
    getOrdenes, 
    getOrdenById, 
    updateOrden, 
    deleteOrden, 
    getOrdenesVigentes,
    postOrdenesByDateRange,
    postProductosVendidosByDateRange,
    getMesasConOrdenesActivas,
    verifyOrdenStatus,
    confirmarPagoOrden,
    getInfoTicket,
    updateIndicaciones,
    getOrdenResumen } from '../controllers/ordenController.js';

    import { authMiddleware } from '../middlewares/auth.middleware.js';

const router = Router();


router.post('/nueva-orden',authMiddleware, createOrden);
router.get('/ordenes',authMiddleware, getOrdenes);
router.get('/orden/:id',authMiddleware, getOrdenById);
router.put('/orden/:id',authMiddleware, updateOrden);
router.delete('/orden/:id',authMiddleware, deleteOrden);
router.get('/ordenes/mesas-ocupadas', authMiddleware, getMesasConOrdenesActivas);
router.get('/ordenes/verifyOrdenStatus/:id', authMiddleware, verifyOrdenStatus);
router.post('/ordenes/confirmarPagoOrden/:id', authMiddleware, confirmarPagoOrden);
router.get('/ordenes/getInfoTicket/:id', authMiddleware, getInfoTicket);

// SIN PROBAR
router.get('/ordenes/vigentes/hoy',authMiddleware, getOrdenesVigentes);
router.post('/ordenes/rango-fechas',authMiddleware, postOrdenesByDateRange); 
router.post('/ordenes/productos-vendidos',authMiddleware, postProductosVendidosByDateRange);

// ============================================================
// NUEVAS RUTAS PARA MOCKUPS
// ============================================================

/**
 * Actualizar indicaciones de una orden (Pantalla 6 - Bottom Sheet)
 * PATCH /api/orden/:id/indicaciones
 */
router.patch('/orden/:id/indicaciones', authMiddleware, updateIndicaciones);

/**
 * Obtener resumen de orden para Pantalla 1
 * GET /api/orden/:id/resumen
 */
router.get('/orden/:id/resumen', authMiddleware, getOrdenResumen);


export default router;

// rutasImagen.js
import express from 'express';
import multer from 'multer';
import { subirImagen, obtenerUrlImagen, eliminarImagen } from '../controllers/productoImagenController.js';

const router = express.Router();

// Configuración de Multer para guardar en memoria
const storage = multer.memoryStorage();
const upload = multer({ storage });

/**
 * POST /api/images/upload
 * Sube un archivo a S3.
 */
router.post('/productos/upload', upload.single('image'), subirImagen);

/**
 * GET /api/images/:key
 * Obtiene la URL pre-firmada de la imagen en S3.
 */
router.get('/producto/:key', obtenerUrlImagen);

/**
 * DELETE /api/images/:key
 * Elimina la imagen de S3.
 */
router.delete('/producto/:key', eliminarImagen);

export default router;
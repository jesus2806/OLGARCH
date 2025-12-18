// app.js
import express from 'express';
import mongoose from 'mongoose';
import cors from 'cors';
import dotenv from 'dotenv';
import morgan from 'morgan';
import usuarioRoutes from './src/routes/usuario.routes.js';
import productoRoutes from './src/routes/producto.routes.js';
import ordenRoutes from './src/routes/orden.routes.js';
import ordenProductoRoutes from './src/routes/ordenProducto.routes.js';
import imageRoutes from './src/routes/image.routes.js';
import rutasHistorico from './src/routes/historico.routes.js';
import extraRoutes from './src/routes/extra.routes.js';
import syncRoutes from './src/routes/sync.routes.js';

dotenv.config();
const app = express();

// Middlewares básicos
app.use(cors());
app.use(express.json());
app.use(morgan('dev'));

// Conexión a MongoDB
mongoose
  .connect(process.env.MONGODB_URI, {
    serverSelectionTimeoutMS: 20000
  })
  .then(() => console.log('Conectado a la base de datos MongoDB'))
  .catch((error) => console.error('Error al conectar a MongoDB:', error));

// Rutas usuario
app.use('/api', usuarioRoutes);

// Rutas de productos
app.use('/api', productoRoutes);

// Rutas de ordenes
app.use('/api', ordenRoutes);

// Rutas de Ordenes Producto
app.use('/api', ordenProductoRoutes);

// Rutas para imagenes S3
app.use('/api/images', imageRoutes);

// Rutas para historico
app.use('/api', rutasHistorico);

// Rutas para extras
app.use('/api', extraRoutes);

// Rutas para sincronización unificada
app.use('/api', syncRoutes);

app.get('/health', (req, res) => {
  res.status(200).json({ status: 'OK' });
});

// Manejo de rutas no encontradas
app.use((req, res) => {
  res.status(404).json({
    success: false,
    message: 'Ruta no encontrada',
    error: {
      code: 404,
      details: 'La ruta que estás buscando no existe',
    },
  });
});

export default app;
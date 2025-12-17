// middlewares/auth.middleware.js
import jwt from 'jsonwebtoken';
import dotenv from 'dotenv';

dotenv.config();

export const authMiddleware = (req, res, next) => {
  const authHeader = req.headers.authorization;

  if (!authHeader) {
    return res.status(401).json({
      success: false,
      message: 'Error al procesar la solicitud',    
      error: {
        code: 401,
        details: 'No se proporcionó un token de autenticación',
      },
    });
  }

  // Se asume que el formato del header es "Bearer <token>"
  const token = authHeader.split(' ')[1];

  if (!token) {
    return res.status(401).json({
      success: false,
      message: 'Error al procesar la solicitud',
      error: {
        code: 401,
        details: 'Formato de token no válido',
      },
    });
  }

  try {
    const decoded = jwt.verify(token, process.env.JWT_SECRET);
    req.user = decoded; // Se puede usar en otros endpoints
    next();
  } catch (error) {
    return res.status(401).json({
      success: false,
      message: 'Error al procesar la solicitud',
      error: {
        code: 401,
        details: 'Token de autenticación inválido o expirado',
      },
    });
  }
};

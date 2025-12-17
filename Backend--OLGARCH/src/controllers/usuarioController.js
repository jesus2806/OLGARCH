// controllers/usuario.controller.js
import Usuario from '../models/usuario.js';
import bcrypt from 'bcrypt';
import jwt from 'jsonwebtoken';

export const createUser = async (req, res) => {
  try {
    const { sNombre, sApellidoPaterno, sApellidoMaterno, sUsuario, sPassword, iRol } = req.body;

    // Verificar que no exista ya un usuario con el mismo sUsuario
    const usuarioExistente = await Usuario.findOne({ sUsuario });
    if (usuarioExistente) {
      return res.status(400).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: {
          code: 400,
          details: 'El nombre de usuario ya existe',
        },
      });
    }

    const nuevoUsuario = new Usuario({
      sNombre,
      sApellidoPaterno,
      sApellidoMaterno,
      sUsuario,
      sPassword,
      iRol,
    });

    const usuarioGuardado = await nuevoUsuario.save();

    return res.status(201).json({
      success: true,
      message: 'Usuario creado exitosamente',
      data: usuarioGuardado,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al procesar la solicitud',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

export const getUsers = async (req, res) => {
  try {
    const usuarios = await Usuario.find();
    return res.status(200).json({
      success: true,
      message: 'Operación exitosa',
      data: usuarios,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al procesar la solicitud',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

export const getUserById = async (req, res) => {
  try {
    const { id } = req.params;
    const usuario = await Usuario.findById(id);
    if (!usuario) {
      return res.status(404).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: {
          code: 404,
          details: 'Usuario no encontrado',
        },
      });
    }

    return res.status(200).json({
      success: true,
      message: 'Operación exitosa',
      data: usuario,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al procesar la solicitud',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

export const updateUser = async (req, res) => {
  try {
    const { id } = req.params;
    const { sNombre, sApellidoPaterno, sApellidoMaterno, sUsuario, sPassword, iRol } = req.body;

    // Buscamos el usuario a actualizar
    const usuario = await Usuario.findById(id);
    if (!usuario) {
      return res.status(404).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: {
          code: 404,
          details: 'Usuario no encontrado',
        },
      });
    }

    // Actualizamos los campos
    if (sNombre) usuario.sNombre = sNombre;
    if (sApellidoPaterno) usuario.sApellidoPaterno = sApellidoPaterno;
    if (sApellidoMaterno) usuario.sApellidoMaterno = sApellidoMaterno;
    if (sUsuario) usuario.sUsuario = sUsuario;
    if (sPassword) usuario.sPassword = sPassword; // bcrypt se aplicará en el pre('save')
    if (iRol !== undefined && iRol != 0) usuario.iRol = iRol;

    const usuarioActualizado = await usuario.save();

    return res.status(200).json({
      success: true,
      message: 'Usuario actualizado correctamente',
      data: usuarioActualizado,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al procesar la solicitud',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

export const deleteUser = async (req, res) => {
  try {
    const { id } = req.params;
    const usuarioEliminado = await Usuario.findByIdAndDelete(id);
    if (!usuarioEliminado) {
      return res.status(404).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: {
          code: 404,
          details: 'Usuario no encontrado',
        },
      });
    }

    return res.status(200).json({
      success: true,
      message: 'Usuario eliminado correctamente',
      data: usuarioEliminado,
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al procesar la solicitud',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

/**
 * Autenticación (Login)
 * Valida las credenciales y genera un JWT en caso de éxito.
 */
export const login = async (req, res) => {
  try {
    const { sUsuario, sPassword } = req.body;

    // Verificar que el usuario exista
    const usuario = await Usuario.findOne({ sUsuario });
    if (!usuario) {
      return res.status(401).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: {
          code: 401,
          details: 'Credenciales inválidas',
        },
      });
    }

    // Comparar el password
    const esPasswordValido = await bcrypt.compare(sPassword, usuario.sPassword);
    if (!esPasswordValido) {
      return res.status(401).json({
        success: false,
        message: 'Error al procesar la solicitud',
        error: {
          code: 401,
          details: 'Credenciales inválidas',
        },
      });
    }

    // Generar token JWT
    const token = jwt.sign(
      {
        id: usuario._id,
        sUsuario: usuario.sUsuario,
        iRol: usuario.iRol,
      },
      process.env.JWT_SECRET, // Definido en tu .env
      { expiresIn: '2h' }
      // { expiresIn: '1m' }
    );

    return res.status(200).json({
      success: true,
      message: 'Login exitoso',
      data: {
        sIdUsuarioMongoDB: usuario._id,
        sNombreUsuario: usuario.sNombre + " " + usuario.sApellidoPaterno + " " + usuario.sApellidoMaterno,
        sUsuario: usuario.sUsuario,
        iRol: usuario.iRol,
        sTokenAcceso: token
      },
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Error al procesar la solicitud',
      error: {
        code: 500,
        details: error.message,
      },
    });
  }
};

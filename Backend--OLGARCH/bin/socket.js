import { Server } from 'socket.io';

/**
 * Crea e inicializa la configuración de Socket.io
 * @param {http.Server} server - Instancia del servidor HTTP
 */
export function createSocketServer(server) {
  const io = new Server(server, {
    cors: {
      origin: process.env.CORS_ORIGIN || '*',
      methods: ['GET', 'POST'],
    },
  });

  // Manejo de eventos de conexión
  io.on('connection', (socket) => {
    const clients = Array.from(io.sockets.sockets.keys());
    console.log(clients);
    console.log('Cliente conectado:', socket.id);

    // Ejemplo: Escucha un evento 'mensaje' desde el cliente
    // (Mensaje general para todos los sockets)
    socket.on('mensaje', (data) => {
      console.log(`Mensaje recibido de ${socket.id}:`, data);
      // Reemite el mensaje a todos los clientes
      io.emit('mensaje', data);
    });

    /* 
     * -------------------------
     * Manejo de SALAS (Rooms)
     * -------------------------
     */

    // Unirse a una sala específica
    socket.on('unirseSala', (nombreSala) => {
      socket.join(nombreSala);
      console.log(`${socket.id} se unió a la sala: ${nombreSala}`);

      // Opcional: notificar al resto en la sala de la llegada de un nuevo usuario
      socket.to(nombreSala).emit('usuarioNuevo', {
        socketId: socket.id,
        mensaje: `El usuario ${socket.id} se unió a la sala ${nombreSala}`,
      });
    });

    // Enviar mensaje a usuarios de una sala
    socket.on('mensajeSala', ({ sala, mensaje }) => {
      console.log(`Mensaje en sala ${sala} de ${socket.id}: ${mensaje}`);
      io.to(sala).emit('mensajeSala', {
        socketId: socket.id,
        mensaje,
      });
    });

    // Salir de una sala
    socket.on('salirSala', (nombreSala) => {
      socket.leave(nombreSala);
      console.log(`${socket.id} salió de la sala: ${nombreSala}`);
    });


      // Evento personalizado para desconexión permanente.
      socket.on('forceDisconnect', () => {
        console.log(`Recibido 'forceDisconnect' para ${socket.id}. Desconectando permanentemente.`);
        // El parámetro true cierra la conexión subyacente.
        socket.disconnect(true);
      });

    // Manejo de desconexión
    socket.on('disconnect', () => {
      console.log('Cliente desconectado:', socket.id);
    });
  });

  // Mensaje de confirmación en consola
  console.log('Socket.IO server initialized');
}

// src/services/s3Service.js

import { S3Client, PutObjectCommand, GetObjectCommand, DeleteObjectCommand } from '@aws-sdk/client-s3';
import { getSignedUrl } from '@aws-sdk/s3-request-presigner'; // Para URL pre-firmadas si lo deseas
import dotenv from 'dotenv';

// Carga las variables de entorno
dotenv.config();

const s3Client = new S3Client({
  region: process.env.AWS_REGION,
  credentials: {
    accessKeyId: process.env.AWS_ACCESS_KEY_ID,
    secretAccessKey: process.env.AWS_SECRET_ACCESS_KEY,
  },
});

const bucketName = process.env.S3_BUCKET_NAME;

/**
 * Sube un archivo a S3.
 * @param {String} key - Nombre con el que se guardará el archivo en S3.
 * @param {Buffer} fileBuffer - Contenido del archivo en binario.
 * @param {String} mimetype - Tipo MIME del archivo.
 */
export async function uploadFile(key, fileBuffer, mimetype) {
  const command = new PutObjectCommand({
    Bucket: bucketName,
    Key: key,
    Body: fileBuffer,
    ContentType: mimetype,
  });
  return await s3Client.send(command);
}

/**
 * Genera una URL pre-firmada para obtener el archivo.
 * @param {String} key
 */
export async function getFileUrl(key) {
  const command = new GetObjectCommand({
    Bucket: bucketName,
    Key: key,
  });
  // Pre-signed URL válida por 1 hora (3600 segundos)
  return await getSignedUrl(s3Client, command, { expiresIn: 3600 });
}

/**
 * Construye y devuelve la URL pública del objeto en S3.
 * @param {String} key - Clave del objeto en S3.
 * @returns {String} URL del objeto.
 */
export function getObjectUrl(key) {
  // Verifica si estás utilizando una región que requiere un formato específico
  // Por ejemplo, us-east-1 tiene un formato ligeramente diferente
  const region = process.env.AWS_REGION === 'us-east-1' ? 'us-east-1' : process.env.AWS_REGION;
  return `https://${bucketName}.s3.${region}.amazonaws.com/${encodeURIComponent(key)}`;
}

/**
 * Elimina un archivo de S3.
 * @param {String} key
 */
export async function deleteFile(key) {
  const command = new DeleteObjectCommand({
    Bucket: bucketName,
    Key: key,
  });
  return await s3Client.send(command);
}

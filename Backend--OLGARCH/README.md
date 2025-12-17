# BACKEND -- OLGARCH

Backend para sistema de administración de órdenes de restaurante.

## Configuración

1. Copiar `.env.example` a `.env` y configurar las variables de entorno
2. Instalar dependencias: `npm install`
3. Ejecutar: `npm start`

## Estructura del Proyecto

```
src/
├── controllers/          # Controladores de la API
├── middlewares/          # Middlewares (autenticación)
├── models/              # Modelos de MongoDB
├── routes/              # Definición de rutas
└── services/            # Servicios externos (S3)
```

---

## API Endpoints

### Autenticación

Todos los endpoints (excepto `/health`) requieren autenticación mediante token JWT.

Header requerido: `Authorization: Bearer <token>`

---

## Órdenes

### Crear orden
```
POST /api/nueva-orden
```

### Obtener todas las órdenes
```
GET /api/ordenes
```

### Obtener orden por ID
```
GET /api/orden/:id
```

### Actualizar orden
```
PUT /api/orden/:id
```

### Eliminar orden
```
DELETE /api/orden/:id
```

### Obtener resumen de orden (Pantalla 1) ⭐ NUEVO
```
GET /api/orden/:id/resumen
```

Respuesta:
```json
{
  "success": true,
  "data": {
    "_id": "...",
    "iNumeroOrden": 10,
    "iMesa": 2,
    "sUsuarioMesero": "Carlos Soto",
    "sIndicaciones": "Una orden de enchiladas sin...",
    "aProductos": [
      {
        "_id": "...",
        "sNombre": "Enchiladas suizas",
        "iCostoPublico": 65,
        "iCantidad": 4,
        "bTieneExtras": true,
        "iTotalExtras": 90,
        "iTotalProducto": 350
      }
    ],
    "iTotalProductos": 260,
    "iTotalExtras": 90,
    "iTotalGeneral": 350
  }
}
```

### Actualizar indicaciones (Pantalla 6) ⭐ NUEVO
```
PATCH /api/orden/:id/indicaciones
```

Body:
```json
{
  "sIndicaciones": "Una orden de enchiladas sin cebolla"
}
```

---

## Productos de Orden

### Crear producto en orden
```
POST /api/orden-productos/
```

Body:
```json
{
  "sIdOrdenMongoDB": "orden_id",
  "sNombre": "Enchiladas suizas",
  "iCostoReal": 50,
  "iCostoPublico": 65,
  "sURLImagen": "https://...",
  "iIndexVarianteSeleccionada": 0,
  "aVariantes": [{"sVariante": "Normal"}],
  "iCantidad": 4,
  "iTipoProducto": 1
}
```

### Obtener productos de una orden
```
GET /api/orden-productos/orden/:idOrden
```

### Obtener producto por ID
```
GET /api/orden-productos/:id
```

### Actualizar producto
```
PUT /api/orden-productos/:id
```

### Eliminar producto
```
DELETE /api/orden-productos/:id
```

---

## Gestión de Consumos y Extras ⭐ NUEVOS

Los "consumos" representan cada unidad individual de un producto. Por ejemplo, si se piden 4 enchiladas, hay 4 consumos (Consumo 1, Consumo 2, Consumo 3, Consumo 4). Cada consumo puede tener sus propios extras.

### Obtener consumos de un producto (Pantalla 2)
```
GET /api/orden-productos/:id/consumos
```

Respuesta:
```json
{
  "success": true,
  "data": {
    "sIdOrdenProducto": "...",
    "sNombre": "Enchiladas suizas",
    "iCantidad": 4,
    "aConsumos": [
      {
        "iIndex": 1,
        "aExtras": [
          {"_id": "...", "sNombre": "Pollo", "iCostoPublico": 30},
          {"_id": "...", "sNombre": "2 Huevos", "iCostoPublico": 15}
        ]
      },
      {
        "iIndex": 2,
        "aExtras": [
          {"_id": "...", "sNombre": "Aguacate", "iCostoPublico": 15}
        ]
      },
      {
        "iIndex": 3,
        "aExtras": [
          {"_id": "...", "sNombre": "Pollo", "iCostoPublico": 30}
        ]
      },
      {
        "iIndex": 4,
        "aExtras": []
      }
    ],
    "iTotalExtras": 90
  }
}
```

### Agregar extra a consumos específicos (Pantalla 3 y 4)
```
POST /api/orden-productos/:id/consumos/extras
```

Body:
```json
{
  "extra": {
    "sIdExtra": "optional_mongo_id",
    "sNombre": "Pollo",
    "iCostoReal": 25,
    "iCostoPublico": 30,
    "sURLImagen": "https://..."
  },
  "aIndexConsumos": [1, 3]
}
```

**Nota:** Si un consumo ya tiene ese extra (mismo nombre), se descarta silenciosamente.

Respuesta:
```json
{
  "success": true,
  "message": "Extra agregado exitosamente. Agregados: 2, Descartados (duplicados): 0",
  "data": {
    "aConsumos": [...],
    "iTotalExtras": 90,
    "extrasAgregados": 2,
    "extrasDescartados": 0
  }
}
```

### Eliminar extra de un consumo (Pantalla 2 - ícono basura)
```
DELETE /api/orden-productos/:id/consumos/:indexConsumo/extras/:idExtra
```

Ejemplo:
```
DELETE /api/orden-productos/abc123/consumos/1/extras/extra456
```

### Eliminar un consumo específico
```
DELETE /api/orden-productos/:id/consumos/:indexConsumo
```

Útil cuando el producto tiene extras y se necesita eliminar un consumo específico (Escenario 3 del mockup).

### Actualizar cantidad de un producto (Pantalla 1)
```
PATCH /api/orden-productos/:id/cantidad
```

Body:
```json
{
  "iCantidad": 5
}
```

**Comportamiento especial:**
- Si se decrementa la cantidad y el producto tiene extras, retorna `requiereAdminConsumos: true` para que el frontend redirija a la Pantalla 2.
- Si `iCantidad = 0`, el producto se elimina automáticamente.

Respuesta cuando tiene extras:
```json
{
  "success": true,
  "message": "El producto tiene extras asociados. Debe administrar los consumos manualmente.",
  "data": {
    "requiereAdminConsumos": true,
    "sIdOrdenProducto": "...",
    "bTieneExtras": true,
    "aConsumos": [...]
  }
}
```

### Verificar si producto tiene extras
```
GET /api/orden-productos/:id/tiene-extras
```

---

## Catálogo de Extras ⭐ NUEVO

### Crear extra
```
POST /api/extras
```

Body:
```json
{
  "sNombre": "Pollo extra",
  "iCostoReal": 25,
  "iCostoPublico": 30,
  "imagenes": [{"sURLImagen": "https://..."}]
}
```

### Obtener todos los extras activos
```
GET /api/extras
```

### Buscar extras por nombre (Pantalla 3)
```
POST /api/extras/search
```

Body:
```json
{
  "texto": "pollo"
}
```

### Obtener extra por ID
```
GET /api/extras/:id
```

### Actualizar extra
```
PUT /api/extras/:id
```

### Eliminar extra (soft delete)
```
DELETE /api/extras/:id
```

---

## Productos (Catálogo)

### Buscar productos (Pantalla 5)
```
POST /api/productos/search
```

Body:
```json
{
  "texto": "enchiladas",
  "tipo": 1,
  "tipoEn": "1,2"
}
```

---

## Flujos de los Mockups

### Agregar producto a la orden (Pantalla 1 → 5 → 1)

1. Usuario en Pantalla 1 hace clic en "Agregar producto"
2. Frontend navega a Pantalla 5
3. Usuario busca producto: `POST /api/productos/search`
4. Usuario selecciona producto
5. Frontend crea el producto: `POST /api/orden-productos/`
6. Frontend regresa a Pantalla 1

### Agregar extras a un producto (Pantalla 1 → 2 → 3 → 4 → 2 → 1)

1. Usuario en Pantalla 1 hace clic en "+" del producto
2. Frontend obtiene consumos: `GET /api/orden-productos/:id/consumos`
3. Frontend muestra Pantalla 2 con los consumos
4. Usuario hace clic en "Agregar extra"
5. Frontend navega a Pantalla 3
6. Usuario busca extra: `POST /api/extras/search`
7. Usuario selecciona extra y hace clic en "+"
8. Si hay más de 1 consumo, se muestra Pantalla 4
9. Usuario selecciona consumos y hace clic en "Agregar"
10. Frontend agrega extra: `POST /api/orden-productos/:id/consumos/extras`
11. Frontend regresa a Pantalla 2
12. Usuario hace clic en "Regresar"
13. Frontend regresa a Pantalla 1

### Agregar indicaciones (Pantalla 1 → 6 → 1)

1. Usuario en Pantalla 1 hace clic en ícono de lápiz
2. Frontend muestra Bottom Sheet (Pantalla 6)
3. Usuario escribe indicaciones
4. Usuario hace clic en "Guardar"
5. Frontend actualiza: `PATCH /api/orden/:id/indicaciones`
6. Bottom Sheet se oculta

### Eliminar producto con extras (Escenario 3)

1. Usuario intenta decrementar cantidad de producto con extras
2. Frontend llama: `PATCH /api/orden-productos/:id/cantidad`
3. Backend retorna `requiereAdminConsumos: true`
4. Frontend redirige automáticamente a Pantalla 2
5. Usuario elimina consumos específicos: `DELETE /api/orden-productos/:id/consumos/:indexConsumo`

---

## Modelos de Datos

### Orden
```javascript
{
  sIdentificadorOrden: String,
  iMesa: Number,
  iTipoOrden: Number,      // 1 = Primaria, 2 = Secundaria
  iNumeroOrden: Number,    // Auto-incremento
  sUsuarioMesero: String,
  sIdMongoDBMesero: String,
  aProductos: [ObjectId],  // Refs a OrdenProducto
  dtFechaAlta: Date,
  dtFechaFin: Date,
  iTotalOrden: Number,
  iEstatus: Number,        // 0-5
  bOrdenModificada: Boolean,
  iTipoPago: Number,
  iTotalExtrasOrden: Number,
  sIndicaciones: String    // ⭐ NUEVO
}
```

### OrdenProducto
```javascript
{
  sIdOrdenMongoDB: ObjectId,
  sNombre: String,
  iCostoReal: Number,
  iCostoPublico: Number,
  sURLImagen: String,
  sIndicaciones: String,
  iIndexVarianteSeleccionada: Number,
  aVariantes: [{sVariante: String}],
  iCantidad: Number,
  aConsumos: [              // ⭐ NUEVO
    {
      iIndex: Number,
      aExtras: [
        {
          sIdExtra: ObjectId,
          sNombre: String,
          iCostoReal: Number,
          iCostoPublico: Number,
          sURLImagen: String
        }
      ]
    }
  ],
  aExtras: [...],           // Legacy - compatibilidad
  iTipoProducto: Number
}
```

### Extra (Catálogo) ⭐ NUEVO
```javascript
{
  sNombre: String,
  iCostoReal: Number,
  iCostoPublico: Number,
  imagenes: [{sURLImagen: String}],
  bActivo: Boolean
}
```

---

## Notas de Compatibilidad

- El campo `aExtras` en `OrdenProducto` se mantiene para compatibilidad con código existente
- El nuevo campo `aConsumos` permite gestionar extras por consumo individual
- Los virtuals calculan totales considerando ambos sistemas
- El middleware `pre-save` sincroniza automáticamente `aConsumos` con `iCantidad`

# Sistema Offline-First para Gestión de Órdenes

## Resumen del Cambio

Se eliminaron las llamadas individuales a endpoints para cada operación (agregar producto, cambiar cantidad, agregar extras, etc.). Ahora **todos los cambios se acumulan localmente** y se envían al backend en una sola operación:

- **Orden Nueva**: Al presionar "🚀 Tomar Orden"
- **Orden Existente**: Al presionar "💾 Guardar Cambios"

---

## Flujo de Trabajo

### 1. Crear Nueva Orden

```
┌──────────────────────────────────────────────────────────────────┐
│  USUARIO                                                         │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. Presiona "Agregar Orden"                                     │
│          │                                                       │
│          ▼                                                       │
│  2. Selecciona Mesa en popup ───► OrdenDraftService              │
│          │                         .IniciarNuevaOrdenAsync()     │
│          │                              │                        │
│          │                              ▼                        │
│          │                        [SQLite LOCAL]                 │
│          │                        - tb_Orden (bSincronizado=0)   │
│          ▼                                                       │
│  3. Agrega productos ───────────► OrdenDraftService              │
│     (sin llamar al backend)        .AgregarProductoAsync()       │
│          │                              │                        │
│          │                              ▼                        │
│          │                        [SQLite LOCAL]                 │
│          │                        - tb_OrdenProducto             │
│          ▼                                                       │
│  4. Modifica cantidades ────────► OrdenDraftService              │
│     Agrega extras                  .ActualizarCantidadAsync()    │
│     (sin llamar al backend)        .AgregarExtraAsync()          │
│          │                              │                        │
│          │                              ▼                        │
│          │                        [SQLite LOCAL]                 │
│          ▼                                                       │
│  5. Presiona "🚀 Tomar Orden" ──► OrdenDraftService              │
│          │                         .GuardarEnBackendAsync()      │
│          │                              │                        │
│          │                              ▼                        │
│          │                   ┌──────────────────────┐            │
│          │                   │   POST /api/nueva-orden           │
│          │                   │   POST /api/orden-productos (x N) │
│          │                   │   PATCH /api/orden/{id}/estatus   │
│          │                   └──────────────────────┘            │
│          │                              │                        │
│          ▼                              ▼                        │
│  6. ¡Orden enviada a cocina!    [MongoDB actualizado]            │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### 2. Editar Orden Existente

```
┌──────────────────────────────────────────────────────────────────┐
│  USUARIO                                                         │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. Abre orden existente ───────► OrdenDraftService              │
│          │                         .CargarOrdenExistenteAsync()  │
│          │                              │                        │
│          │                              ▼                        │
│          │                   ┌──────────────────────┐            │
│          │                   │   GET /api/orden/{id}/resumen     │
│          │                   └──────────────────────┘            │
│          │                              │                        │
│          │                              ▼                        │
│          │                        [SQLite LOCAL]                 │
│          │                        - Copia de la orden            │
│          ▼                                                       │
│  2. Modifica productos ─────────► OrdenDraftService              │
│     Agrega/elimina                 (operaciones locales)         │
│     Cambia cantidades                   │                        │
│          │                              ▼                        │
│          │                        [SQLite LOCAL]                 │
│          │                        - bTieneCambiosPendientes=1    │
│          ▼                                                       │
│                                                                  │
│     ⚠️ UI muestra: "Tienes cambios sin guardar"                  │
│                                                                  │
│          │                                                       │
│          ▼                                                       │
│  3. Presiona "💾 Guardar Cambios" ► OrdenDraftService            │
│          │                           .GuardarEnBackendAsync()    │
│          │                                │                      │
│          │                                ▼                      │
│          │                   ┌──────────────────────┐            │
│          │                   │   POST /api/orden-productos (nuevos)
│          │                   │   PUT /api/orden-productos (edit) │
│          │                   │   DELETE /api/orden-productos     │
│          │                   └──────────────────────┘            │
│          │                                │                      │
│          ▼                                ▼                      │
│  4. ¡Cambios guardados!          [MongoDB actualizado]           │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Archivos Modificados/Creados

### Nuevos Servicios

| Archivo | Descripción |
|---------|-------------|
| `Services/OrdenDraftService.cs` | **Servicio principal** - Gestiona el borrador de orden en SQLite |

### ViewModels Modificados

| Archivo | Cambios |
|---------|---------|
| `ViewModels/Popup/CrearOrdenPopupViewModel.cs` | Ya NO llama al backend. Crea orden localmente. |
| `ViewModels/OrdenViewModels/DatosOrdenViewModel.cs` | Nuevo flujo con botones "Tomar Orden" y "Guardar Cambios" |
| `ViewModels/OrdenViewModels/ProductoOrdenViewModel.cs` | Ya NO llama al backend. Guarda localmente. |
| `ViewModels/OrdenViewModels/ConsumosProductoViewModel.cs` | Ya NO llama al backend. Guarda localmente. |
| `ViewModels/OrdenViewModels/BuscarExtrasViewModel.cs` | Ya NO llama al backend. Guarda localmente. |

### Vistas Modificadas

| Archivo | Cambios |
|---------|---------|
| `Views/OrdenViews/DatosOrdenView.xaml` | Nueva UI con indicador de cambios pendientes y botones |
| `Views/OrdenViews/DatosOrdenView.xaml.cs` | Simplificado |

### Modelos con Soporte Offline

| Archivo | Nuevos Campos |
|---------|---------------|
| `Models/Orden.cs` | `sIdLocal`, `bSincronizado`, `bTieneCambiosPendientes` |
| `Models/OrdenProducto.cs` | `sIdLocal`, `sIdOrdenLocal`, `bSincronizado`, `bTieneCambiosPendientes` |
| `Models/Consumo.cs` | `sIdLocal`, `sIdOrdenProductoLocal` |

---

## API del OrdenDraftService

### Inicialización

```csharp
// Inyección de dependencias (ya registrado en MauiProgram.cs)
private readonly OrdenDraftService _ordenDraftService;

public MiViewModel(OrdenDraftService ordenDraftService)
{
    _ordenDraftService = ordenDraftService;
}
```

### Crear Nueva Orden

```csharp
// 1. Iniciar nueva orden (crea localmente)
await _ordenDraftService.IniciarNuevaOrdenAsync(
    identificador: Guid.NewGuid().ToString(),
    mesa: 5,
    mesero: "Juan Pérez",
    idMesero: "64abc123..."
);

// 2. Agregar productos
await _ordenDraftService.AgregarProductoAsync(producto, variante, "sin cebolla");

// 3. Modificar cantidad
await _ordenDraftService.ActualizarCantidadProductoAsync(idLocalProducto, 3);

// 4. Agregar extras a consumos
await _ordenDraftService.AgregarExtraAConsumosAsync(idProducto, extra, new List<int>{1,2});

// 5. Guardar todo en backend
var (exito, mensaje) = await _ordenDraftService.GuardarEnBackendAsync();
```

### Editar Orden Existente

```csharp
// 1. Cargar orden desde backend
await _ordenDraftService.CargarOrdenExistenteAsync("64abc123...");

// 2. Modificar localmente
await _ordenDraftService.AgregarProductoAsync(...);
await _ordenDraftService.EliminarProductoAsync(idLocal);
await _ordenDraftService.ActualizarCantidadProductoAsync(...);

// 3. Verificar si hay cambios
if (_ordenDraftService.TieneCambiosPendientes)
{
    // Mostrar botón "Guardar Cambios"
}

// 4. Guardar cambios
var (exito, mensaje) = await _ordenDraftService.GuardarEnBackendAsync();
```

### Propiedades Útiles

```csharp
// Orden actual en edición
Orden? orden = _ordenDraftService.OrdenActual;

// Productos de la orden
ObservableCollection<OrdenProducto> productos = _ordenDraftService.Productos;

// ¿Es orden nueva?
bool esNueva = _ordenDraftService.EsOrdenNueva;

// ¿Hay cambios pendientes?
bool pendientes = _ordenDraftService.TieneCambiosPendientes;

// Calcular total
decimal total = _ordenDraftService.CalcularTotalOrden();
```

### Eventos

```csharp
// Cuando cambian los productos
_ordenDraftService.OnProductosChanged += (sender, e) =>
{
    // Refrescar UI
};

// Cuando cambia el estado de cambios pendientes
_ordenDraftService.OnCambiosPendientesChanged += (sender, tieneCambios) =>
{
    // Mostrar/ocultar botón "Guardar Cambios"
};
```

---

## Interfaz de Usuario

### Indicador de Cambios Pendientes

Cuando hay cambios sin guardar, se muestra un banner amarillo:

```
┌─────────────────────────────────────────┐
│  ⚠️ Tienes cambios sin guardar          │
└─────────────────────────────────────────┘
```

### Botones de Acción

**Para orden NUEVA:**
- `🚀 Tomar Orden` - Envía todo al backend y cambia estatus a "Tomada"

**Para orden EXISTENTE con cambios:**
- `💾 Guardar Cambios` - Envía los cambios al backend (visible solo si hay cambios)

**Para órdenes en proceso:**
- `👨‍🍳 Preparar` - Cambia estatus a "En preparación"
- `✅ Preparada` - Cambia estatus a "Preparada"

---

## Beneficios del Nuevo Flujo

1. **Mejor UX**: El usuario puede hacer múltiples cambios sin esperar respuestas del servidor
2. **Funciona offline**: Los cambios se guardan en SQLite y se sincronizan cuando hay conexión
3. **Menos llamadas al servidor**: En lugar de N llamadas, solo 1-3 al confirmar
4. **Consistencia**: Todos los cambios se aplican en una transacción
5. **Reversible**: Si el usuario no confirma, puede descartar los cambios

---

## Consideraciones Importantes

1. **Los cambios NO se guardan automáticamente en el backend**
   - El usuario DEBE presionar "Tomar Orden" o "Guardar Cambios"
   
2. **Al salir de la app, los cambios locales persisten**
   - Gracias a SQLite, el borrador sobrevive si se cierra la app
   
3. **Solo una orden a la vez**
   - El `OrdenDraftService` maneja una sola orden activa
   - Al cargar otra orden, se limpia el borrador anterior

4. **Conflictos de concurrencia**
   - Si otro usuario modifica la orden mientras se edita localmente, el último en guardar "gana"
   - Considerar agregar validación de versión en el futuro

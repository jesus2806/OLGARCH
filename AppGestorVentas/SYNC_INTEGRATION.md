# Sistema de Sincronización Offline-First

## Descripción

Este sistema permite trabajar con órdenes de manera offline, almacenando los cambios localmente en SQLite y sincronizándolos con el backend cuando el usuario lo solicite.

## Archivos Creados/Modificados

### Nuevos Archivos

| Archivo | Descripción |
|---------|-------------|
| `Models/SyncOperation.cs` | Modelo para operaciones pendientes de sincronización |
| `Models/SyncResponse.cs` | Modelos para respuestas del backend |
| `Services/SyncService.cs` | Servicio principal de sincronización |
| `ViewModels/SyncViewModel.cs` | ViewModel para el componente de sincronización |
| `Views/Components/SyncButtonView.xaml` | Componente visual del botón de sincronización |
| `Views/Components/SyncButtonView.xaml.cs` | Code-behind del componente |
| `Converters/PercentToDecimalConverter.cs` | Converter para la barra de progreso |

### Archivos Modificados

| Archivo | Cambios |
|---------|---------|
| `Models/Orden.cs` | Agregados: sIdLocal, bSincronizado, bTieneCambiosPendientes, IdEfectivo |
| `Models/OrdenProducto.cs` | Agregados: sIdLocal, sIdOrdenLocal, bSincronizado, bTieneCambiosPendientes |
| `Models/Consumo.cs` | Agregados: sIdLocal, sIdOrdenProductoLocal |
| `MauiProgram.cs` | Registro de SyncService y SyncViewModel |
| `App.xaml` | Registro de converters globales |

## Flujo de Uso

### 1. Crear una nueva orden (offline)

```csharp
// En tu ViewModel
private readonly SyncService _syncService;

public async Task CrearNuevaOrden()
{
    var nuevaOrden = new Orden
    {
        sIdentificadorOrden = GenerarIdentificador(),
        iMesa = 5,
        sUsuarioMesero = "Juan Pérez",
        sIdMongoDBMesero = "64a5b3c2..."
    };

    // Esto guarda en SQLite y crea la operación de sync
    var ordenCreada = await _syncService.RegistrarCrearOrdenAsync(nuevaOrden);
    
    // ordenCreada.sIdLocal contiene el UUID local
    // Puedes usar este ID para agregar productos
}
```

### 2. Agregar un producto a la orden (offline)

```csharp
public async Task AgregarProducto(Orden orden)
{
    var producto = new OrdenProducto
    {
        sNombre = "Enchiladas Suizas",
        iCostoReal = 50,
        iCostoPublico = 85,
        iCantidad = 3,
        iTipoProducto = 1
    };

    // Esto guarda el producto y lo asocia a la orden
    var productoCreado = await _syncService.RegistrarCrearProductoAsync(producto, orden);
}
```

### 3. Modificar cantidad de producto

```csharp
public async Task CambiarCantidad(OrdenProducto producto, int nuevaCantidad)
{
    await _syncService.RegistrarActualizarCantidadProductoAsync(producto, nuevaCantidad);
}
```

### 4. Agregar extras a consumos

```csharp
public async Task AgregarExtra(OrdenProducto producto)
{
    var extra = new ExtraConsumo
    {
        sIdExtra = "64abc...",
        sNombre = "Pollo Extra",
        iCostoReal = 25,
        iCostoPublico = 35
    };

    // Agregar a consumos 1 y 2
    var indexConsumos = new List<int> { 1, 2 };
    
    await _syncService.RegistrarAgregarExtraConsumosAsync(producto, extra, indexConsumos);
}
```

### 5. Sincronizar con el backend

```csharp
public async Task Sincronizar()
{
    var resultado = await _syncService.SincronizarAsync();
    
    if (resultado.Exitoso)
    {
        // resultado.IdMapping contiene el mapeo de IDs locales a MongoDB
        // Por ejemplo: { "uuid-local-1": "64a5b3c2..." }
        
        Console.WriteLine($"Sincronizadas {resultado.Exitosas} operaciones");
    }
    else
    {
        Console.WriteLine($"Error: {resultado.Mensaje}");
    }
}
```

## Integración del Componente Visual

### En XAML

```xml
<ContentPage xmlns:components="clr-namespace:AppGestorVentas.Views.Components"
             xmlns:viewmodels="clr-namespace:AppGestorVentas.ViewModels">

    <!-- En tu layout -->
    <components:SyncButtonView x:Name="syncButton" />
    
</ContentPage>
```

### En Code-Behind

```csharp
public partial class MiVista : ContentPage
{
    private readonly SyncViewModel _syncViewModel;

    public MiVista(SyncViewModel syncViewModel)
    {
        InitializeComponent();
        _syncViewModel = syncViewModel;
        syncButton.SetViewModel(_syncViewModel);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _syncViewModel.CargarEstadoInicialAsync();
    }
}
```

## Integración en AdministracionOrdenViewModel

Para integrar completamente en el flujo existente:

```csharp
public partial class AdministracionOrdenViewModel : ObservableObject
{
    private readonly SyncService _syncService;
    
    // Agregar al constructor
    public AdministracionOrdenViewModel(
        HttpApiService httpApiService,
        LocalDatabaseService localDatabaseService,
        IPopupService popupService,
        SocketIoService socketIoService,
        NotificationService notificationService,
        SyncService syncService)  // <-- Nuevo parámetro
    {
        // ... código existente ...
        _syncService = syncService;
    }

    // Modificar el comando de sincronización
    [RelayCommand]
    public async Task SincronizarDatos()
    {
        // 1. Primero sincronizar cambios locales al backend
        var hayCambios = await _syncService.HayCambiosPendientesAsync();
        
        if (hayCambios)
        {
            var resultadoSync = await _syncService.SincronizarAsync();
            
            if (!resultadoSync.Exitoso)
            {
                var continuar = await Shell.Current.DisplayAlert(
                    "Sincronización Parcial",
                    $"Hubo errores al sincronizar: {resultadoSync.Mensaje}\n¿Deseas continuar y obtener datos del servidor?",
                    "Sí", "No");
                    
                if (!continuar) return;
            }
        }
        
        // 2. Luego obtener datos actualizados del servidor
        await ObtenerListadoOrdenesAPI();
    }
}
```

## Eventos Disponibles

### OnPendingOperationsChanged

Se dispara cuando cambia el número de operaciones pendientes:

```csharp
_syncService.OnPendingOperationsChanged += (sender, cantidad) =>
{
    Console.WriteLine($"Operaciones pendientes: {cantidad}");
};
```

### OnSyncProgress

Se dispara durante la sincronización para mostrar progreso:

```csharp
_syncService.OnSyncProgress += (sender, args) =>
{
    Console.WriteLine($"Progreso: {args.Porcentaje}% - {args.Estado}");
};
```

## Tipos de Operaciones Soportadas

| Tipo | Método del SyncService |
|------|------------------------|
| CREAR_ORDEN | `RegistrarCrearOrdenAsync()` |
| ACTUALIZAR_ORDEN | `RegistrarActualizarOrdenAsync()` |
| ELIMINAR_ORDEN | `RegistrarEliminarOrdenAsync()` |
| ACTUALIZAR_INDICACIONES_ORDEN | `RegistrarActualizarIndicacionesOrdenAsync()` |
| CREAR_PRODUCTO | `RegistrarCrearProductoAsync()` |
| ACTUALIZAR_PRODUCTO | `RegistrarActualizarProductoAsync()` |
| ELIMINAR_PRODUCTO | `RegistrarEliminarProductoAsync()` |
| ACTUALIZAR_CANTIDAD_PRODUCTO | `RegistrarActualizarCantidadProductoAsync()` |
| AGREGAR_EXTRA_CONSUMOS | `RegistrarAgregarExtraConsumosAsync()` |
| ELIMINAR_EXTRA_CONSUMO | `RegistrarEliminarExtraConsumoAsync()` |
| ELIMINAR_CONSUMO | `RegistrarEliminarConsumoAsync()` |

## Consultas Útiles

```csharp
// Cantidad de operaciones pendientes
int pendientes = await _syncService.ObtenerCantidadPendientesAsync();

// Verificar si hay cambios pendientes
bool hayCambios = await _syncService.HayCambiosPendientesAsync();

// Obtener una orden por ID local
var orden = await _syncService.ObtenerOrdenPorIdLocalAsync("uuid-local");

// Obtener productos de una orden
var productos = await _syncService.ObtenerProductosPorOrdenLocalAsync("uuid-orden-local");

// Limpiar todas las operaciones (debug)
await _syncService.LimpiarOperacionesAsync();
```

## Notas Importantes

1. **IDs Locales vs MongoDB**: Cuando creas una entidad offline, usa `sIdLocal`. Después de sincronizar, tendrás tanto `sIdLocal` como `sIdMongoDB`.

2. **Referencias entre entidades**: Cuando creas un producto y la orden aún no está sincronizada, usa el `sIdLocal` de la orden. El backend resolverá las referencias automáticamente.

3. **Orden de operaciones**: Las operaciones se ejecutan en el orden en que fueron creadas (por timestamp).

4. **Tolerancia a fallos**: Si una operación falla, las demás continúan. Puedes revisar los errores en el resultado.

5. **Reconexión**: Si pierdes conexión durante la sincronización, los cambios quedan pendientes para el próximo intento.

# Diseño Técnico: Reporte de "Productos con Stock Bajo"

## Resumen

Se agrega un reporte de "Productos con Stock Bajo" al Sistema Almacén replicando end-to-end el patrón del reporte de **inventario**, el reporte existente más cercano porque tampoco usa rango de fechas y es de solo lectura sobre `Productos`. El cambio atraviesa las cinco capas del proyecto sin introducir librerías ni patrones nuevos: un DTO en `SistemaAlmacen.Shared`, una firma en `IReporteService` y su implementación en `ReporteService` (Business), un endpoint GET en `ReportesController` (Server), soporte de exportación PDF/Excel/CSV en el mismo servicio y controller, y una pestaña con su render y lógica en `Reportes.razor` (Client).

La lógica de negocio reutiliza exactamente el cálculo de umbral del dashboard (`GenerarDashboardAsync`): umbral global configurable `Dashboard:StockBajoUmbral` (default 5) combinado con el `StockMinimo` propio del producto. El reporte lista productos **activos** que están **agotados** (`Stock <= 0`) o con **stock bajo** (`Stock > 0 && Stock <= mínimoEfectivo`), ordenados por `Stock` ascendente y luego por `Nombre`.

La verificación del codebase confirma: `ReporteService` inyecta `IConfiguration _configuration` en su constructor; la entidad `Producto` (`src/SistemaAlmacen.Data/Entities/Producto.cs`) expone `Nombre`, `Stock`, `StockMinimo`, `Activo` y navegación `Categoria`; el reporte de inventario usa `[Authorize(Policy = "RequireAdmin")]` en el server y `@attribute [Authorize(Roles = "Administrador")]` a nivel de página.

## Stack tecnológico (bloqueado)

.NET 10, C#, Blazor WebAssembly (Client) + ASP.NET Core Web API (Server), EF Core sobre `ApplicationDbContext`, ClosedXML para Excel, QuestPDF para PDF, Bootstrap 5 para la UI. No se agregan dependencias. Ningún cambio de base de datos (sin migraciones, sin tocar entidades).

## Decisiones de diseño

### Patrón de referencia: inventario, no ventas

El reporte de inventario es el molde exacto porque comparte las dos características que lo distinguen de ventas/productos-más-vendidos: no recibe rango de fechas y opera sobre el catálogo de productos. Todo el nuevo código imita ese flujo (firma sin parámetros, endpoint sin query string, UI en la rama `else` sin selector de fechas, nombre de archivo sin rango). Se descarta modelar sobre ventas porque introduciría parámetros de fecha que el requisito prohíbe para este reporte.

### Filtrado y clasificación en memoria (igual que el dashboard)

`GenerarReporteInventarioAsync` proyecta directamente en la consulta SQL con `.Select(...).ToListAsync()`. Sin embargo, la clasificación de stock bajo requiere la expresión condicional `p.StockMinimo > 0 ? p.StockMinimo : umbralStockBajo`, que el dashboard resuelve **en memoria** tras traer los campos con `ToListAsync()` para evitar traducir a SQL una comparación que depende de una variable de configuración. Se elige deliberadamente la estrategia del dashboard (traer a memoria y luego filtrar/clasificar) en lugar de la proyección directa de inventario, porque garantiza una lógica de umbral idéntica byte a byte a `GenerarDashboardAsync` — que es el requisito central (Opción A confirmada). El volumen de productos activos es acotado (el mismo `ToListAsync` ya lo hace el dashboard sin problema), así que no hay preocupación de rendimiento.

La consulta carga `Include(p => p.Categoria)` porque el DTO necesita `Categoria.Nombre`, y usa `AsNoTracking()` por ser solo lectura, igual que todos los reportes.

### Alcance de autorización

Se mantiene `RequireAdmin` (server) y `Administrador` (página), coherente con inventario y productos-más-vendidos. El requisito original no pide acceso para Vendedor y el reporte expone información sensible de stock, así que se restringe a Administrador.

## Cambios por archivo

### 1. DTO — `src/SistemaAlmacen.Shared/DTOs/Reportes/ReporteStockBajoDto.cs` (nuevo)

Namespace `SistemaAlmacen.Shared.DTOs.Reportes`. Modelado idéntico en estilo a `ReporteInventarioDto` (clase simple con auto-propiedades, comentario XML en español).

```csharp
namespace SistemaAlmacen.Shared.DTOs.Reportes;

/// <summary>
/// DTO con datos de un producto en situación de stock bajo o agotado para reportes.
/// </summary>
public class ReporteStockBajoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public int MinimoEfectivo { get; set; }
    public string Estado { get; set; } = string.Empty;
}
```

`MinimoEfectivo` almacena el mínimo que se usó para evaluar (`StockMinimo` propio si `> 0`, o el umbral global). `Estado` será `"Agotado"` o `"Stock Bajo"`.

### 2. Interfaz — `src/SistemaAlmacen.Business/Interfaces/IReporteService.cs`

Agregar la firma tras `GenerarReporteInventarioAsync`, con comentario XML en español al estilo del resto:

```csharp
/// <summary>
/// Genera un listado de los productos activos que están agotados (stock ≤ 0)
/// o con stock bajo (stock positivo pero menor o igual a su mínimo efectivo),
/// reutilizando el umbral de stock bajo del dashboard.
/// </summary>
/// <returns>Lista de productos con estado "Agotado" o "Stock Bajo", ordenada por stock ascendente.</returns>
Task<List<ReporteStockBajoDto>> GenerarReporteStockBajoAsync();
```

### 3. Servicio — `src/SistemaAlmacen.Business/Services/ReporteService.cs`

Nuevo método público `GenerarReporteStockBajoAsync`, ubicado tras `GenerarReporteInventarioAsync`, con `/// <inheritdoc />`:

```csharp
/// <inheritdoc />
public async Task<List<ReporteStockBajoDto>> GenerarReporteStockBajoAsync()
{
    var umbralStockBajo = _configuration.GetValue<int>("Dashboard:StockBajoUmbral", 5);

    // Solo productos activos; se traen a memoria para aplicar la misma lógica
    // de umbral que el dashboard (depende de una variable de configuración).
    var productos = await _context.Productos
        .AsNoTracking()
        .Include(p => p.Categoria)
        .Where(p => p.Activo)
        .Select(p => new
        {
            p.Id,
            p.Nombre,
            Categoria = p.Categoria.Nombre,
            p.Stock,
            p.StockMinimo
        })
        .ToListAsync();

    var resultado = productos
        .Select(p =>
        {
            var minimo = p.StockMinimo > 0 ? p.StockMinimo : umbralStockBajo;
            return new
            {
                p.Id,
                p.Nombre,
                p.Categoria,
                p.Stock,
                p.StockMinimo,
                MinimoEfectivo = minimo,
                EsAgotado = p.Stock <= 0,
                EsStockBajo = p.Stock > 0 && p.Stock <= minimo
            };
        })
        .Where(p => p.EsAgotado || p.EsStockBajo)
        .OrderBy(p => p.Stock)
        .ThenBy(p => p.Nombre)
        .Select(p => new ReporteStockBajoDto
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Categoria = p.Categoria,
            Stock = p.Stock,
            StockMinimo = p.StockMinimo,
            MinimoEfectivo = p.MinimoEfectivo,
            Estado = p.EsAgotado ? "Agotado" : "Stock Bajo"
        })
        .ToList();

    return resultado;
}
```

Notas de implementación:
- El `.Select` anónimo intermedio evita recalcular `minimo` y la clasificación en cada etapa, y mantiene la lógica de umbral idéntica a `GenerarDashboardAsync`.
- El orden es `OrderBy(Stock).ThenBy(Nombre)`: como los agotados tienen `Stock <= 0`, quedan primero naturalmente, luego los de menor stock, y a igual stock por nombre (criterio de aceptación 5).
- `Estado` se deriva de `EsAgotado`; si `Stock <= 0` es `"Agotado"`, en cualquier otro caso incluido en el resultado es `"Stock Bajo"`.

### 4. Exportación — `ObtenerDatosReporteAsync` en `ReporteService.cs`

Agregar la rama `"stock-bajo"` al `switch` (que ya usa `tipoReporte.ToLowerInvariant()`, equivalente case-insensitive a lo pedido), junto a `"inventario"`:

```csharp
"stock-bajo" => await ObtenerDatosStockBajoAsync(),
```

Y un método privado nuevo, modelado sobre `ObtenerDatosInventarioAsync`:

```csharp
private async Task<(string[] Headers, List<string[]> Rows)> ObtenerDatosStockBajoAsync()
{
    var stockBajo = await GenerarReporteStockBajoAsync();

    var headers = new[] { "Producto", "Categoría", "Stock", "Stock Mínimo", "Estado" };
    var rows = stockBajo.Select(p => new[]
    {
        p.Nombre,
        p.Categoria,
        p.Stock.ToString(),
        p.MinimoEfectivo.ToString(),
        p.Estado
    }).ToList();

    return (headers, rows);
}
```

La columna "Stock Mínimo" usa `MinimoEfectivo` (criterio 8). `GenerarPdf` ya ramifica el encabezado por `tipoReporte == "inventario"` para decidir si muestra rango de fechas o "al {fecha}". Para que el PDF de stock-bajo tampoco muestre rango, se amplía esa condición en `GenerarPdf`:

```csharp
if (string.Equals(tipoReporte, "inventario", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(tipoReporte, "stock-bajo", StringComparison.OrdinalIgnoreCase))
{
    text.Span($"al {DateTime.Now:dd/MM/yyyy}").FontSize(12);
}
```

Esto es consistente con el requisito de que stock-bajo no use rango de fechas. Sin este ajuste el PDF mostraría un rango de fechas espurio (las fechas por defecto enviadas desde el cliente), lo cual sería incorrecto aunque no rompa la compilación.

### 5. Controller — `src/SistemaAlmacen.Server/Controllers/ReportesController.cs`

Nuevo endpoint tras `GetReporteInventario`, copiando exactamente sus atributos:

```csharp
/// <summary>
/// Genera un reporte de los productos activos con stock bajo o agotados.
/// Acceso restringido a Administrador.
/// </summary>
[HttpGet("stock-bajo")]
[Authorize(Policy = "RequireAdmin")]
[ProducesResponseType(typeof(List<ReporteStockBajoDto>), StatusCodes.Status200OK)]
public async Task<ActionResult<List<ReporteStockBajoDto>>> GetReporteStockBajo()
{
    var result = await _reporteService.GenerarReporteStockBajoAsync();
    return Ok(result);
}
```

En `GenerateFileName`, ampliar la condición que omite el rango de fechas para incluir `"stock-bajo"`:

```csharp
if (string.Equals(request.TipoReporte, "inventario", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(request.TipoReporte, "stock-bajo", StringComparison.OrdinalIgnoreCase))
{
    return $"{request.TipoReporte}_{DateTime.Now:yyyyMMdd}.{extension}";
}
```

Esto produce `stock-bajo_{yyyyMMdd}.{ext}` en el servidor (criterio 8).

### 6. Frontend — `src/SistemaAlmacen.Client/Pages/Reportes/Reportes.razor`

**Pestaña** — agregar al `<ul class="nav nav-tabs mb-3">` tras la de inventario:

```razor
<li class="nav-item">
    <button class="nav-link @(_activeTab == "stock-bajo" ? "active" : "")" @onclick='() => SetTab("stock-bajo")'>Stock Bajo</button>
</li>
```

**Botón del bloque `else`** — la condición `_activeTab == "ventas" || _activeTab == "productos"` ya deja a `inventario` y `stock-bajo` en el `else` (sin selector de fechas). Hacer condicional el texto del botón:

```razor
<button class="btn btn-primary" @onclick="GenerateReport" disabled="@_isLoading">
    @(_activeTab == "stock-bajo" ? "Generar Reporte de Stock Bajo" : "Generar Reporte de Inventario")
</button>
```

**Bloque de render** — agregar tras el `else if (_activeTab == "inventario" ...)`:

```razor
else if (_activeTab == "stock-bajo" && _stockBajo != null)
{
    @if (_stockBajo.Any())
    {
        <table class="table table-sm">
            <thead>
                <tr>
                    <th>Producto</th>
                    <th>Categoría</th>
                    <th>Stock Actual</th>
                    <th>Stock Mínimo</th>
                    <th>Estado</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in _stockBajo)
                {
                    <tr>
                        <td>@item.Nombre</td>
                        <td>@item.Categoria</td>
                        <td>@item.Stock</td>
                        <td>@item.MinimoEfectivo</td>
                        <td>
                            <span class="badge @(item.Estado == "Agotado" ? "bg-danger" : "bg-warning text-dark")">
                                @item.Estado
                            </span>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    }
    else
    {
        <p class="text-muted">No hay productos con stock bajo.</p>
    }
}
```

**Campo y case en `@code`** — agregar el campo junto a `_inventario`:

```csharp
private List<ReporteStockBajoDto>? _stockBajo;
```

y el case en el `switch` de `GenerateReport`:

```csharp
case "stock-bajo":
    _stockBajo = await Http.GetFromJsonAsync<List<ReporteStockBajoDto>>("api/reportes/stock-bajo");
    break;
```

**Nombre de archivo en `Export`** — ampliar la condición para que stock-bajo tampoco use rango:

```csharp
var fileName = (_activeTab == "inventario" || _activeTab == "stock-bajo")
    ? $"reporte_{_activeTab}_{DateTime.Now:yyyyMMdd}.{GetExtension(formato)}"
    : $"reporte_{_activeTab}_{_fechaInicio:yyyyMMdd}_{_fechaFin:yyyyMMdd}.{GetExtension(formato)}";
```

Nota: se reemplaza el literal `reporte_inventario_` por `reporte_{_activeTab}_` para cubrir ambos casos sin romper inventario; para `inventario` produce exactamente el mismo nombre que antes (`reporte_inventario_{yyyyMMdd}`), y para stock-bajo produce `reporte_stock-bajo_{yyyyMMdd}` (criterio 8). El tipo `ReporteStockBajoDto` ya está disponible por el `@using` del namespace `SistemaAlmacen.Shared.DTOs.Reportes` (confirmar que `_Imports.razor` incluye ese namespace; los otros DTOs de reportes se usan sin `@using` explícito en esta página, así que ya está cubierto).

## Manejo de errores

- **`GenerarReporteStockBajoAsync`**: no valida parámetros (no recibe ninguno). El único punto de fallo es la consulta EF Core, que ante error de base de datos lanza excepción propagada al controller — mismo comportamiento que `GenerarReporteInventarioAsync`, que tampoco captura. No recuperable a este nivel; no se loguea aquí (coherente con el patrón existente). `_configuration.GetValue` con default 5 nunca falla por ausencia de la clave.
- **Endpoint `GetReporteStockBajo`**: no hay `ArgumentException` esperada (sin parámetros), por eso no lleva `try/catch` ni `ProducesResponseType(400)`, igual que `GetReporteInventario`. Una excepción inesperada resulta en 500 gestionado por el middleware global de ASP.NET Core.
- **Exportación (`ObtenerDatosStockBajoAsync`)**: delega en `GenerarReporteStockBajoAsync`; cualquier fallo se propaga a `ExportarAsync` → `Exportar` del controller, que ya captura `ArgumentException` y devuelve 400; otras excepciones → 500 por middleware. Consistente con inventario.
- **Frontend `GenerateReport`/`Export`**: el `try/catch` existente ya captura cualquier excepción del `case "stock-bajo"` y muestra `"Error al generar el reporte."` / mensajes de exportación. No se requiere lógica nueva de error.

## Validación de entradas

- El endpoint no recibe entradas del cliente (sin query string ni body), por lo que no hay nada que validar — eliminando una clase entera de riesgo.
- La exportación recibe `ExportRequest.TipoReporte`. La validación es la comparación case-insensitive en el `switch` de `ObtenerDatosReporteAsync`: un `tipoReporte` desconocido cae en el `_ => throw new ArgumentException(...)` existente, que el controller traduce a 400. `"stock-bajo"` se agrega como valor válido; cualquier otra cosa sigue rechazándose igual que hoy.
- La configuración `Dashboard:StockBajoUmbral` es opcional con default 5 (tipo `int`), replicando el dashboard; un valor ausente o no-entero cae al default sin fallar.

## Invariantes

- **Lógica de umbral única**: la fórmula `minimo = StockMinimo > 0 ? StockMinimo : umbralStockBajo` y los predicados de agotado/stock-bajo deben ser idénticos a `GenerarDashboardAsync`. Este invariante lo posee la capa Business (`ReporteService`), porque es la única fuente de verdad de la regla de negocio; replicar el código textualmente (en vez de extraer un helper) mantiene el requisito de "no introducir patrones nuevos" y minimiza el riesgo de regresión en el dashboard. Se acepta la duplicación consciente de ~3 líneas como costo de no refactorizar código existente fuera de alcance.
- **Solo activos**: el filtro `Where(p => p.Activo)` en la consulta SQL garantiza que ningún producto inactivo aparezca, aun si está agotado (criterio 6). La capa de datos (consulta EF) es dueña de este invariante.
- **Estado consistente con stock**: `Estado == "Agotado"` ⟺ `Stock <= 0` dentro del resultado. Garantizado por derivar `Estado` del mismo booleano `EsAgotado` usado para filtrar.

## Testabilidad

- **Unit test (Business)**: `GenerarReporteStockBajoAsync` es testeable con `ApplicationDbContext` en memoria (EF Core InMemory o SQLite in-memory, según lo que use el proyecto de tests existente) e `IConfiguration` en memoria. Casos clave: producto agotado incluido con `Estado="Agotado"`; producto con stock bajo por mínimo propio; producto con stock bajo por umbral global (StockMinimo=0); producto por encima del mínimo excluido; producto inactivo agotado excluido; orden por Stock y luego Nombre; `MinimoEfectivo` correcto en ambas ramas. Se consideran triviales y alineados con el patrón si ya existe un proyecto de tests para `ReporteService`; de lo contrario quedan fuera de alcance por el requisito de no agregar tests nuevos salvo que encajen.
- **Integration test (Server)**: el endpoint `GET api/reportes/stock-bajo` es testeable con `WebApplicationFactory` verificando 200 y el contrato `List<ReporteStockBajoDto>`, más 401/403 sin rol Administrador.
- **No unit-testeable fácilmente**: el render Blazor y la descarga por JS interop (`downloadFileFromBytes`) requieren bUnit o prueba manual; se verifica por compilación y, si es posible, prueba visual.
- La separación DTO/servicio/controller mantiene la lógica de negocio aislada de infraestructura, por lo que el núcleo (clasificación y orden) es directamente unit-testeable sin levantar la app.

## Verificación planificada

1. `dotnet build SistemaAlmacen.slnx` desde el workdir debe compilar sin errores ni warnings nuevos.
2. Revisión de que los reportes existentes (ventas, productos-más-vendidos, inventario, dashboard) y su exportación no cambian de comportamiento: los únicos puntos tocados en código existente son adiciones de ramas `||` y un `switch` con un caso extra, sin alterar las ramas previas.
3. Si no es posible levantar la app para prueba visual, se declarará explícitamente; como mínimo la compilación debe pasar.

## Respuesta a la revisión de diseño

No existe revisión de diseño para esta ejecución. Los archivos `design-review.json` / `03-design-review.md` hallados en el workspace pertenecen a una ejecución anterior (directorio `2026-10-27_20-15-46`), no al run actual (`2026-10-27_21-29-10`), cuyo directorio no contiene ningún archivo de revisión. Por lo tanto este documento es una primera autoría, no una iteración de loop-back.

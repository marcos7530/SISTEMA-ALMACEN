# Requisitos: Reporte de "Productos con Stock Bajo"

## Resumen

Agregar un nuevo reporte de "Productos con Stock Bajo" al Sistema Almacén (app .NET 10 Blazor, solución `SistemaAlmacen.slnx`). El reporte lista los productos activos que están **agotados** (stock ≤ 0) o con **stock bajo** (stock positivo pero por debajo o igual a su mínimo efectivo), reutilizando exactamente la misma lógica de umbral que ya emplea el dashboard (`ReporteService.GenerarDashboardAsync`).

La funcionalidad debe seguir el patrón de los reportes existentes en todas las capas (DTO en Shared, método en interfaz/servicio del Business, endpoint en el controller del Server, pestaña y render en el frontend Client, y soporte de exportación PDF/Excel/CSV). No se introducen librerías ni patrones nuevos, no se crean migraciones y no se tocan entidades.

Decisiones ya confirmadas por el usuario:
- **Opción A**: reutilizar la lógica de umbral del dashboard (umbral global de configuración + mínimo propio del producto).
- **Opción B**: el reporte incluye tanto los productos con stock bajo como los agotados.

## Requisitos Funcionales

### RF1 — DTO del reporte
Debe existir un DTO `ReporteStockBajoDto` en `src/SistemaAlmacen.Shared/DTOs/Reportes/ReporteStockBajoDto.cs`, namespace `SistemaAlmacen.Shared.DTOs.Reportes`, modelado según `ReporteInventarioDto`. Propiedades:
- `int Id`
- `string Nombre` (valor por defecto `""`)
- `string Categoria` (valor por defecto `""`)
- `int Stock`
- `int StockMinimo`
- `int MinimoEfectivo` — mínimo que se usó para evaluar al producto (su `StockMinimo` propio si es > 0, o el umbral global en caso contrario).
- `string Estado` (valor por defecto `""`) — será `"Agotado"` o `"Stock Bajo"`.

### RF2 — Método en la interfaz del servicio
Agregar a `IReporteService` (`src/SistemaAlmacen.Business/Interfaces/IReporteService.cs`) la firma:
`Task<List<ReporteStockBajoDto>> GenerarReporteStockBajoAsync();`
con comentario XML en español describiéndolo, siguiendo el estilo de los demás métodos de la interfaz.

### RF3 — Lógica de negocio del servicio
Implementar `GenerarReporteStockBajoAsync()` en `src/SistemaAlmacen.Business/Services/ReporteService.cs`, siguiendo el estilo de `GenerarReporteInventarioAsync` y la lógica de stock de `GenerarDashboardAsync`:
- Leer el umbral global: `var umbralStockBajo = _configuration.GetValue<int>("Dashboard:StockBajoUmbral", 5);` (el campo de configuración inyectado es `_configuration`, confirmado en el constructor de la clase).
- Consultar `_context.Productos` con `AsNoTracking()`, `Include(p => p.Categoria)` y `Where(p => p.Activo)`.
- Traer a memoria (`ToListAsync`) los campos necesarios y luego filtrar/clasificar en memoria.
- Por cada producto, el mínimo efectivo es: `p.StockMinimo > 0 ? p.StockMinimo : umbralStockBajo`.
- Un producto se incluye si está **Agotado** (`p.Stock <= 0`) o con **Stock Bajo** (`p.Stock > 0 && p.Stock <= minimo`).
- Mapear a `ReporteStockBajoDto` calculando `Estado` (`"Agotado"` o `"Stock Bajo"`) y `MinimoEfectivo`.
- Ordenar por `Stock` ascendente (agotados primero, luego menor stock) y, dentro de eso, por `Nombre`.

### RF4 — Endpoint en el controller
Agregar a `ReportesController` (`src/SistemaAlmacen.Server/Controllers/ReportesController.cs`) un endpoint GET siguiendo el patrón de `GetReporteInventario`:
- Ruta `[HttpGet("stock-bajo")]`
- `[Authorize(Policy = "RequireAdmin")]`
- `[ProducesResponseType(typeof(List<ReporteStockBajoDto>), StatusCodes.Status200OK)]`
- Llama a `_reporteService.GenerarReporteStockBajoAsync()` y retorna `Ok(result)`.

### RF5 — Soporte de exportación (PDF/Excel/CSV)
- En `ObtenerDatosReporteAsync(tipoReporte, fechaInicio, fechaFin)` de `ReporteService.cs`, agregar el caso `"stock-bajo"` (comparado de forma case-insensitive, igual que `"inventario"`), devolviendo headers y rows apropiados. Headers: `Producto`, `Categoría`, `Stock`, `Stock Mínimo`, `Estado`. La columna `Stock Mínimo` usa `MinimoEfectivo`.
- En `GenerateFileName` del controller, el tipo `"stock-bajo"` NO usa rango de fechas (igual que `"inventario"`): agregar `"stock-bajo"` a la condición que genera el nombre `{tipo}_{yyyyMMdd}`.

### RF6 — Frontend (Reportes.razor)
En `src/SistemaAlmacen.Client/Pages/Reportes/Reportes.razor`:
- Agregar una pestaña "Stock Bajo" en el `<ul class="nav nav-tabs">` que active `_activeTab == "stock-bajo"`.
- El selector de fechas solo se muestra para `ventas`/`productos`; `stock-bajo` cae en el bloque `else` con botón directo (sin fechas). El texto del botón del `else` debe ser condicional: `"Generar Reporte de Inventario"` para inventario y `"Generar Reporte de Stock Bajo"` para stock-bajo.
- Agregar un bloque de render `else if (_activeTab == "stock-bajo" && _stockBajo != null)` con una tabla de columnas: `Producto`, `Categoría`, `Stock Actual`, `Stock Mínimo`, `Estado`. En `Stock Mínimo` mostrar `MinimoEfectivo`. En `Estado` mostrar un badge: `bg-danger` para `"Agotado"`, `bg-warning text-dark` para `"Stock Bajo"`. Si la lista está vacía, mostrar `<p class="text-muted">No hay productos con stock bajo.</p>`.
- En `@code`: agregar `private List<ReporteStockBajoDto>? _stockBajo;` y un `case "stock-bajo"` en el switch de `GenerateReport` que haga `_stockBajo = await Http.GetFromJsonAsync<List<ReporteStockBajoDto>>("api/reportes/stock-bajo");`.
- En `Export`: el `fileName` para `stock-bajo` tampoco usa fechas (igual que inventario); ajustar la condición para que produzca `reporte_stock-bajo_{DateTime.Now:yyyyMMdd}` sin rango.

## Requisitos No Funcionales

- **Consistencia de patrón**: cada cambio debe imitar el estilo del reporte de `inventario` en su capa (nombres, comentarios XML en español, firmas, atributos). Sin librerías ni patrones nuevos.
- **Autorización**: el endpoint y la página mantienen el acceso restringido a Administrador (política `RequireAdmin` / `[Authorize(Roles = "Administrador")]`), coherente con los demás reportes que no son de ventas.
- **Sin cambios de datos**: no crear migraciones ni modificar entidades; el reporte es de solo lectura sobre `Productos` activos.
- **No regresión**: los reportes existentes (ventas, productos más vendidos, inventario, dashboard) y su exportación deben seguir funcionando sin cambios de comportamiento.

## Criterios de Aceptación

1. Existe `ReporteStockBajoDto` en la ruta y namespace indicados, con las 7 propiedades especificadas y sus valores por defecto (`Nombre`, `Categoria`, `Estado` = `""`).
2. `IReporteService` declara `Task<List<ReporteStockBajoDto>> GenerarReporteStockBajoAsync();` con comentario XML en español.
3. `GenerarReporteStockBajoAsync` devuelve únicamente productos activos que están agotados (`Stock <= 0`) o con stock bajo (`Stock > 0 && Stock <= minimoEfectivo`), usando `minimoEfectivo = StockMinimo > 0 ? StockMinimo : umbralStockBajo` y leyendo `Dashboard:StockBajoUmbral` (default 5) de la configuración.
4. Cada ítem del resultado trae `Estado = "Agotado"` cuando `Stock <= 0` y `Estado = "Stock Bajo"` en el otro caso, y `MinimoEfectivo` igual al mínimo que se usó para evaluarlo.
5. El resultado está ordenado por `Stock` ascendente y, a igual stock, por `Nombre` ascendente.
6. Un producto activo cuyo stock supera su mínimo efectivo NO aparece en el reporte; un producto inactivo nunca aparece aunque esté agotado.
7. `GET api/reportes/stock-bajo` responde 200 con `List<ReporteStockBajoDto>`, requiere política `RequireAdmin` y declara el `ProducesResponseType` correspondiente.
8. La exportación con `TipoReporte = "stock-bajo"` en PDF, Excel y CSV produce un archivo con las columnas `Producto`, `Categoría`, `Stock`, `Stock Mínimo` (= `MinimoEfectivo`), `Estado`; el nombre de archivo usa `{tipo}_{yyyyMMdd}` sin rango de fechas (servidor) y `reporte_stock-bajo_{yyyyMMdd}` (cliente).
9. En la página de Reportes aparece la pestaña "Stock Bajo"; al seleccionarla no se muestra el selector de fechas y el botón dice "Generar Reporte de Stock Bajo".
10. Al generar el reporte en la pestaña "Stock Bajo", se muestra una tabla con columnas Producto, Categoría, Stock Actual, Stock Mínimo (= `MinimoEfectivo`) y un badge de Estado (`bg-danger` para "Agotado", `bg-warning text-dark` para "Stock Bajo"); si la lista está vacía se muestra "No hay productos con stock bajo."
11. `dotnet build SistemaAlmacen.slnx` compila sin errores.
12. Los reportes existentes (ventas, productos más vendidos, inventario, dashboard) y su exportación siguen funcionando sin regresiones.

## Fuera de Alcance

- Crear migraciones o modificar entidades de la base de datos.
- Introducir nuevas librerías, dependencias o patrones.
- Cambiar la lógica de umbral del dashboard existente.
- Agregar tests nuevos, salvo que sea trivial y encaje con lo existente.
- Cambios de permisos o roles distintos a los ya usados por los reportes actuales.
- Prueba visual con la app levantada (si no es posible, se declara explícitamente; como mínimo debe pasar la compilación).

## Supuestos

- El campo de configuración inyectado en `ReporteService` es `_configuration` (confirmado en el constructor).
- La entidad `Producto` expone `StockMinimo`, `Stock`, `Activo` y navegación `Categoria` (confirmado por el uso en `GenerarDashboardAsync` y `GenerarReporteInventarioAsync`).
- El valor de `_activeTab` y el `TipoReporte` enviado al endpoint de exportación es literalmente `"stock-bajo"`, coincidiendo con la ruta del endpoint y las comparaciones case-insensitive del servidor.
- Se mantiene la restricción a Administrador porque el reporte de inventario (el patrón de referencia) usa `RequireAdmin`; el requisito original no pide acceso para Vendedor.

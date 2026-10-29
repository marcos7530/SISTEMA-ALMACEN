# Revisión de Código: Reporte de "Productos con Stock Bajo"

Diff revisado: commit `3cfeadd` — "feat: agregar reporte de productos con stock bajo"
Evidencia de verificación: commit `2c5be05` — "docs: registrar evidencia de build/tests…" (`05-build-evidence.md`)
Requisitos de referencia: `01-requirements.md`
Diseño de referencia: `02-design.md`
Revisión de diseño de referencia: `03-design-review.md`
Pase anterior de code-review: 1 bloqueante (ausencia de evidencia de build/tests)
Código verificado contra: `c:\Users\Marcos\Desktop\SISTEMA-ALMACEN`

## Veredicto

**APPROVED** — 0 bloqueantes.

Segundo pase. El único bloqueante del pase anterior (falta de evidencia de compilación/tests registrada) quedó resuelto por el commit `2c5be05`, que añade `05-build-evidence.md` con el resultado de `dotnet build` (exit 0, 0 errores) y `dotnet test` (exit 0, 17/17). La implementación de las 5 capas y los 2 MEDIUM autorizados siguen conformes y no se introdujo ningún hallazgo nuevo.

---

## Conformidad por capa (contra 02-design.md / 01-requirements.md)

1. **DTO `ReporteStockBajoDto`** — `src/SistemaAlmacen.Shared/DTOs/Reportes/ReporteStockBajoDto.cs` (nuevo). Namespace `SistemaAlmacen.Shared.DTOs.Reportes`; 7 propiedades (`Id`, `Nombre`, `Categoria`, `Stock`, `StockMinimo`, `MinimoEfectivo`, `Estado`) con defaults `string.Empty` en los strings. Conforme (RF1, criterio 1).
2. **Interfaz** — `IReporteService.cs` declara `Task<List<ReporteStockBajoDto>> GenerarReporteStockBajoAsync();` con comentario XML en español, tras `GenerarReporteInventarioAsync`. Conforme (RF2, criterio 2).
3. **Servicio `GenerarReporteStockBajoAsync`** — `ReporteService.cs`. Lee `Dashboard:StockBajoUmbral` default 5; `AsNoTracking()` + `Include(p => p.Categoria)` + `Where(p => p.Activo)`; proyecta y trae a memoria con `ToListAsync()`; `minimo = StockMinimo > 0 ? StockMinimo : umbralStockBajo`; incluye `EsAgotado (Stock <= 0)` o `EsStockBajo (Stock > 0 && Stock <= minimo)`; orden `OrderBy(Stock).ThenBy(Nombre)`; `MinimoEfectivo` = mínimo usado; `Estado` derivado de `EsAgotado`. La lógica de umbral es idéntica a `GenerarDashboardAsync`. Conforme (RF3, criterios 3, 4, 5, 6).
4. **Endpoint** — `ReportesController.cs`: `[HttpGet("stock-bajo")]`, `[Authorize(Policy = "RequireAdmin")]`, `[ProducesResponseType(typeof(List<ReporteStockBajoDto>), StatusCodes.Status200OK)]`, retorna `Ok(result)`. Conforme (RF4, criterio 7).
5. **Exportación** — `ObtenerDatosReporteAsync` agrega `"stock-bajo" => await ObtenerDatosStockBajoAsync()` en el `switch` sobre `tipoReporte.ToLowerInvariant()` (case-insensitive preservado). `ObtenerDatosStockBajoAsync` produce headers `Producto, Categoría, Stock, Stock Mínimo, Estado` y usa `MinimoEfectivo` para la columna "Stock Mínimo". `GenerateFileName` del controller amplía la condición con `|| "stock-bajo"` → `{tipo}_{yyyyMMdd}.{ext}` sin rango. Conforme (RF5, criterio 8).
6. **Frontend `Reportes.razor`** — pestaña "Stock Bajo"; botón del `else` condicional ("Generar Reporte de Stock Bajo"); bloque de render `else if (_activeTab == "stock-bajo" && _stockBajo != null)` con tabla (Producto, Categoría, Stock Actual, Stock Mínimo=`MinimoEfectivo`, badge Estado `bg-danger`/`bg-warning text-dark`) y vacío "No hay productos con stock bajo."; campo `_stockBajo`; `case "stock-bajo"` en `GenerateReport` llamando a `api/reportes/stock-bajo`; `fileName` de `Export` sin rango para stock-bajo. El namespace del DTO está en `_Imports.razor` línea 30 (verificado). Conforme (RF6, criterios 9, 10).

## MEDIUM autorizados

- **MEDIUM-1 (rango espurio en PDF):** `GenerarPdf` amplía la rama "sin rango" con `string.Equals(tipoReporte, "stock-bajo", OrdinalIgnoreCase)`, mostrando "al {fecha}" en lugar del rango de fechas por defecto del cliente. Resuelto (verificado en el estado actual del archivo).
- **MEDIUM-2 (título legible):** se introdujo el helper privado `ObtenerTituloLegible(tipoReporte)`, usado en el encabezado del PDF (`Reporte de {ObtenerTituloLegible(...)} — `) y en el nombre de hoja de Excel. Para stock-bajo produce "Reporte de Stock Bajo" / hoja "Stock Bajo" en vez del literal "stock-bajo". Esto corresponde exactamente a la autorización MEDIUM-2 ("título legible 'Reporte de Stock Bajo', no el literal stock-bajo"). Resuelto.

## Librerías / entidades / migraciones / regresiones

- Sin nuevas librerías ni patrones nuevos: se reutilizan EF Core, ClosedXML, QuestPDF. El helper `ObtenerTituloLegible` es un método privado estático dentro de la clase existente, no un patrón nuevo.
- Sin migraciones y sin cambios de entidades (el diff no toca `src/SistemaAlmacen.Data`).
- No-regresión funcional: el `switch` de `ObtenerDatosReporteAsync` solo añade un caso; las ramas de fecha en `GenerarPdf`, `GenerateFileName` y `Export` solo se amplían con `||`, sin alterar las ramas previas.
- Cambio cosmético autorizado (no bloqueante): `ObtenerTituloLegible` también cambia el rótulo visible de los reportes existentes en el encabezado del PDF y el nombre de hoja de Excel (ventas → "Ventas", productos → "Productos Más Vendidos", inventario → "Inventario"). Es consecuencia directa del fix autorizado MEDIUM-2 (título legible) y no altera datos, columnas, filas ni comportamiento. "Productos Más Vendidos" (22 chars) respeta el límite de 31 de ClosedXML y la truncación `[..31]` se preserva. El default `_ => tipoReporte` conserva el comportamiento original para cualquier tipo desconocido. Dentro del alcance autorizado.

## Evidencia de verificación (leída, no re-ejecutada)

`05-build-evidence.md` (commit `2c5be05`) registra, ejecutado desde `c:\Users\Marcos\Desktop\SISTEMA-ALMACEN`:

- `dotnet build SistemaAlmacen.slnx` → Compilación correcta, exit 0, 0 errores, 49 warnings preexistentes (NU1510/NU1903/NU1902 de NuGet y CS8620/CS8619 en `AfipClientWrapper.cs`), ninguno en los archivos tocados. Cumple criterio 11.
- `dotnet test SistemaAlmacen.slnx --no-build` → exit 0, 17 pruebas superadas, 0 fallos (Business 15, Integration 1, Data 1). Confirma ausencia de regresiones en el comportamiento cubierto por tests (criterio 12).

La evidencia es específica (comandos, exit codes, conteos) y cierra el bloqueante del pase anterior sin necesidad de recompilar.

## Conclusión

Las 5 capas cumplen el diseño y los 12 criterios de aceptación; los 2 MEDIUM autorizados están resueltos; no hay nuevas librerías, migraciones ni cambios de entidades; la evidencia de build/tests está registrada y es verde. No quedan hallazgos bloqueantes.

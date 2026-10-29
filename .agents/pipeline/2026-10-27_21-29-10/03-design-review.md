# Revisión de Diseño: Reporte de "Productos con Stock Bajo"

Documento revisado: `02-design.md`
Requisitos de referencia: `01-requirements.md`
Código verificado contra: `c:\Users\Marcos\Desktop\SISTEMA-ALMACEN`

## Veredicto

**CHANGES_REQUESTED** — 0 HIGH, 2 MEDIUM, 3 NIT.

El diseño es fiel al código real: todas las afirmaciones sobre el código existente (inyección de `_configuration`, forma de la entidad `Producto`, atributos de autorización, estructura de `GenerateFileName`, ramas de `GenerarPdf`, condición del `else` en `Reportes.razor`, `_Imports.razor`) se verificaron y son correctas. Los hallazgos MEDIUM son gaps de especificación/alcance que conviene cerrar antes de implementar, no errores de hechos.

---

## Hallazgos

### 1. [MEDIUM] Cambio a `GenerarPdf` no está cubierto por ningún requisito (scope / trazabilidad)

**Dónde:** `02-design.md`, sección "4. Exportación — `ObtenerDatosReporteAsync`", bloque que amplía la condición de `GenerarPdf`.

**Problema:** RF5 enumera explícitamente solo DOS puntos de toque para exportación: `ObtenerDatosReporteAsync` (agregar caso `"stock-bajo"`) y `GenerateFileName` del controller. El diseño introduce un tercer cambio sobre código existente — ampliar la condición de encabezado de fecha en `GenerarPdf` — que ningún requisito menciona. Verifiqué el código: `GenerarPdf` efectivamente ramifica hoy con `string.Equals(tipoReporte, "inventario", StringComparison.OrdinalIgnoreCase)`, así que sin el cambio el PDF de stock-bajo mostraría el rango de fechas por defecto del cliente (`_fechaInicio`/`_fechaFin`), que es espurio. El cambio es técnicamente correcto y evita un bug real, pero queda fuera del alcance escrito, y la sección "Fuera de alcance" de los requisitos dice "No regresión / solo adiciones". Esto crea una ambigüedad: ¿es un cambio autorizado o scope creep?

**Fix concreto:** Confirmar que este ajuste está dentro de alcance y, preferiblemente, agregar un sub-ítem a RF5 que lo documente, por ejemplo:

> RF5 (añadir): En `GenerarPdf` de `ReporteService.cs`, ampliar la condición que decide mostrar "al {fecha}" en lugar del rango, para incluir `"stock-bajo"` además de `"inventario"` (comparación `OrdinalIgnoreCase`), de modo que el PDF no muestre un rango de fechas espurio.

Sin esta confirmación el implementador no sabe si debe tocar `GenerarPdf` (y arriesgar una observación de scope) o dejarlo (y producir un PDF con fechas incorrectas).

### 2. [MEDIUM] Título del PDF/hoja Excel renderiza el tipo crudo "stock-bajo" — no especificado

**Dónde:** `02-design.md`, sección 4 (exportación). Interacción con `GenerarPdf` (`text.Span($"Reporte de {tipoReporte} — ")`) y `GenerarExcel` (nombre de hoja = `tipoReporte`).

**Problema:** Verifiqué `GenerarPdf`: el encabezado se arma como `"Reporte de {tipoReporte} — ..."`, por lo que el PDF dirá literalmente **"Reporte de stock-bajo"** (con guion). En `GenerarExcel` el nombre de la hoja será `"stock-bajo"`. El requisito no define cómo debe rotularse el reporte en el archivo exportado, y el guion queda visible al usuario final. Es consistente con el comportamiento actual de inventario ("Reporte de inventario", en minúscula), así que no es regresión, pero sí es una decisión de presentación no especificada que el implementador resolverá por omisión. Dado que el reporte es visible al usuario, conviene decidirlo explícitamente en vez de dejarlo implícito.

**Fix concreto:** Elegir una de estas opciones y dejarla escrita:
- (a) Aceptar el literal `stock-bajo` como está (coherente con inventario; cero código extra). Documentarlo como decisión consciente.
- (b) Mapear `tipoReporte` a una etiqueta legible ("Stock Bajo") solo para el encabezado del PDF y el nombre de hoja. Esto implica tocar `GenerarPdf`/`GenerarExcel`, lo que amplía el alcance y rompe la simetría con inventario — probablemente no deseado.

Recomendado: opción (a), declarada explícitamente, para no introducir más cambios en código compartido.

### 3. [NIT] El orden "agotados primero" con `OrderBy(Stock)` asume que no hay stock negativo relevante entre sí

**Dónde:** `02-design.md`, sección 3, nota "los agotados tienen `Stock <= 0`, quedan primero naturalmente"; criterio de aceptación 5.

**Problema:** `OrderBy(p => p.Stock).ThenBy(p => p.Nombre)` ordena correctamente, pero entre varios productos agotados con stock negativo (p. ej. -3 y 0), el de -3 quedará antes del de 0. El criterio 5 solo pide "`Stock` ascendente y a igual stock por `Nombre`", así que esto es conforme. Lo marco solo porque la justificación "quedan primero naturalmente" podría leerse como que todos los agotados forman un bloque indistinto; en realidad se subordenan por su valor de stock. No requiere cambio de código.

**Fix concreto:** Ninguno en código. Opcional: ajustar la redacción de la nota a "los agotados (`Stock <= 0`) quedan primero por tener el stock más bajo, subordenados entre sí por su propio valor de stock y luego por nombre".

### 4. [NIT] Manejo de valores de `Estado` por igualdad de string en el frontend

**Dónde:** `02-design.md`, sección 6, badge: `item.Estado == "Agotado" ? "bg-danger" : "bg-warning text-dark"`.

**Problema:** El badge decide por comparación exacta de string `"Agotado"`. El servidor es la única fuente de ese literal y lo deriva del mismo booleano `EsAgotado`, así que el acople es seguro hoy. Es frágil ante un cambio futuro del literal (mayúsculas/acento), pero replica el patrón existente (inventario compara por `item.Activo` bool, aquí no hay bool equivalente en el DTO). Aceptable dado el requisito de "no introducir patrones nuevos".

**Fix concreto:** Ninguno obligatorio. Opcional y de bajo costo: el DTO ya trae todo lo necesario; se podría derivar el badge de `item.Stock <= 0` en vez del string para desacoplar de la etiqueta, pero no es requerido.

### 5. [NIT] La nota de "Respuesta a la revisión de diseño" puede confundir al lector

**Dónde:** `02-design.md`, última sección "Respuesta a la revisión de diseño".

**Problema:** La sección explica que archivos de revisión de un run anterior no aplican. Es correcta y verificable, pero es metadata del proceso, no diseño; no afecta la implementación. No es un defecto técnico.

**Fix concreto:** Ninguno. Se puede dejar como está.

---

## Supuestos Verificados

Todas estas afirmaciones del diseño fueron contrastadas con el código fuente y son correctas:

1. **`ReporteService` inyecta `IConfiguration _configuration`** — confirmado en el constructor (`ReporteService.cs`, campo privado `_configuration` y asignación en ctor).
2. **Lógica de umbral del dashboard** — `GenerarDashboardAsync` usa `_configuration.GetValue<int>("Dashboard:StockBajoUmbral", 5)`, `minimo = p.StockMinimo > 0 ? p.StockMinimo : umbralStockBajo`, `agotados: Stock <= 0`, `stock bajo: Stock > 0 && Stock <= minimo`, y trae a memoria con `ToListAsync()` antes de clasificar. El diseño la replica byte a byte. Confirmado.
3. **Entidad `Producto`** en `src/SistemaAlmacen.Data/Entities/Producto.cs` expone `Id`, `Nombre`, `Stock`, `StockMinimo`, `Activo`, `Precio`, `PrecioCosto` y navegación `Categoria`. Confirmado (la ruta del diseño es exacta).
4. **`StockMinimo == 0` significa "sin mínimo propio"** — confirmado por el comentario XML en la entidad y por el uso en `GenerarDashboardAsync`.
5. **Patrón `GenerarReporteInventarioAsync`** — sin parámetros, `AsNoTracking()`, `Include(p => p.Categoria)`, proyección a DTO, `ToListAsync()`. Confirmado.
6. **`ReporteInventarioDto`** en `src/SistemaAlmacen.Shared/DTOs/Reportes/`, namespace `SistemaAlmacen.Shared.DTOs.Reportes`, clase simple con auto-propiedades y defaults `= string.Empty`. El `ReporteStockBajoDto` propuesto sigue el mismo estilo. Confirmado.
7. **`ObtenerDatosReporteAsync` usa `tipoReporte.ToLowerInvariant()`** en un switch expression con `_ => throw new ArgumentException(...)`. El caso `"stock-bajo"` con sintaxis `=>` encaja exactamente. Confirmado.
8. **`ObtenerDatosInventarioAsync`** existe como método privado que devuelve `(string[] Headers, List<string[]> Rows)`; el `ObtenerDatosStockBajoAsync` propuesto lo imita. Confirmado.
9. **`GenerateFileName` (controller)** hoy ramifica solo con `string.Equals(request.TipoReporte, "inventario", StringComparison.OrdinalIgnoreCase)` y produce `{tipo}_{yyyyMMdd}.{ext}`. La ampliación con `|| "stock-bajo"` es correcta y produce `stock-bajo_{yyyyMMdd}.{ext}`. Confirmado.
10. **Endpoint de inventario** usa `[HttpGet("inventario")]`, `[Authorize(Policy = "RequireAdmin")]`, `[ProducesResponseType(typeof(List<ReporteInventarioDto>), StatusCodes.Status200OK)]`, sin try/catch. El `GetReporteStockBajo` propuesto copia estos atributos. Confirmado.
11. **Página `Reportes.razor`** tiene `@attribute [Authorize(Roles = "Administrador")]`. Confirmado.
12. **Condición del `else` en `Reportes.razor`** es `_activeTab == "ventas" || _activeTab == "productos"`, por lo que `inventario` y `stock-bajo` caen en el `else` sin selector de fechas. Confirmado.
13. **Texto del botón del `else`** hoy es el literal "Generar Reporte de Inventario"; hacerlo condicional es necesario y correcto. Confirmado.
14. **`Export` (cliente)** usa hoy el literal `reporte_inventario_{yyyyMMdd}` para inventario y rango para el resto. El reemplazo por `reporte_{_activeTab}_...` preserva el nombre de inventario y añade `reporte_stock-bajo_{yyyyMMdd}`. Confirmado.
15. **`_Imports.razor` del Client** incluye `@using SistemaAlmacen.Shared.DTOs.Reportes` (línea 30), por lo que `ReporteStockBajoDto` está disponible sin `@using` extra en la página. Confirmado (el diseño pedía confirmarlo; queda confirmado).
16. **`appsettings.json`** del Server contiene `"Dashboard": { "StockBajoUmbral": 5 }`, coherente con el default del diseño. Confirmado.
17. **`GenerarPdf` ramifica el encabezado** con `string.Equals(tipoReporte, "inventario", StringComparison.OrdinalIgnoreCase)`. Confirmado (base del hallazgo 1).

## Supuestos No Verificados / Incorrectos

- **Afirmación: "sin warnings nuevos" (sección Verificación planificada, punto 1).** No ejecuté `dotnet build`; no puedo confirmar que la base compila hoy sin warnings ni que la adición no introduzca ninguno. Es una meta de verificación para la fase de implementación, no un hecho verificable en revisión.
- **Afirmación: existe (o no) un proyecto de tests para `ReporteService` (sección Testabilidad).** El diseño condiciona los unit tests a "si ya existe un proyecto de tests". No busqué/confirmé la existencia de ese proyecto en el repo; queda como condicional sin verificar. No bloquea porque los tests están fuera de alcance salvo que encajen trivialmente.
- **Afirmación: `ReporteService` se registra en DI de forma que el nuevo método quede disponible por la interfaz.** No es necesario verificar (la interfaz ya se inyecta en el controller existente), pero no inspeccioné el registro de DI; dado que el método se agrega a una interfaz ya inyectada, no hay riesgo material.

Ninguna afirmación del diseño sobre el código existente resultó **incorrecta**. Todas las verificadas coincidieron con la fuente.

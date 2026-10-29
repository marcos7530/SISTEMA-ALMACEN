# Revisión de diseño: Botón "Regenerar datos demo" en el Dashboard

Revisión del documento `02-design.md` contra `01-requirements.md`, verificando sus afirmaciones
sobre el código existente contra la fuente real en `c:\Users\Marcos\Desktop\SISTEMA-ALMACEN`.

## Veredicto

**CHANGES_REQUESTED** — 1 HIGH, 3 MEDIUM, 4 NIT.

---

## Hallazgos

### 1. [HIGH] El stock de productos se decrementa en cada regeneración, sin restaurarse

**Dónde:** sección "Método público de regeneración (Data)" (paso 4) y "Edge cases". El diseño reutiliza
`GenerarDemoVentasAsync` tal cual, que incluye la línea heredada del seeder:

```csharp
producto.Stock = Math.Max(0, producto.Stock - cantidad);
```

**Problema:** en el arranque esto corre una sola vez (idempotencia), pero el botón de regeneración está
pensado para ejecutarse **repetidamente bajo demanda**. En cada clic se borran las ventas demo del mes
pero **no se devuelve el stock** que esas ventas habían descontado; luego se vuelve a generar y se vuelve
a descontar. El stock de los productos activos deriva monotónicamente hacia 0 con cada regeneración, lo
que corrompe las métricas de inventario del propio dashboard ("Productos Agotados", "Stock Bajo",
"Inventario (costo/venta)") — justo lo que el usuario quiere apreciar. El diseño no detecta ni aborda este
efecto; su edge case "Mes sin ventas demo previas" solo cubre la primera ejecución.

**Fix concreto (elegir uno y documentarlo en el diseño):**
- **(a) Restaurar stock antes de borrar:** en `RegenerarDemoVentasAsync`, tras cargar `ventasDemo` con
  `Include(v => v.Detalles)`, sumar de vuelta las cantidades a `Producto.Stock` antes del `RemoveRange`:
  ```csharp
  var productosPorId = await context.Productos.ToDictionaryAsync(p => p.Id);
  foreach (var det in ventasDemo.SelectMany(v => v.Detalles))
      if (productosPorId.TryGetValue(det.ProductoId, out var prod))
          prod.Stock += det.Cantidad;
  ```
  Así el ciclo borrar→regenerar es neutro respecto del stock.
- **(b) No tocar el stock en la ruta de regeneración:** extraer la mutación de stock de
  `GenerarDemoVentasAsync` a un flag/parámetro (`descontarStock`) que sea `true` en el arranque y `false`
  en la regeneración. Esto desvía de "reutilizar exactamente el mismo generador", así que debe justificarse.

La opción (a) es la más fiel a la intención (el stock refleja las ventas vigentes) y mantiene el generador
intacto. Sea cual sea la elección, debe quedar en el diseño como paso explícito con su efecto sobre las
métricas de inventario.

---

### 2. [MEDIUM] La justificación del borrado explícito contradice la configuración real de FKs

**Dónde:** sección "Método público de regeneración (Data)", paso 3:
> "Marcar las dependientes explícitamente evita suponer que exista `ON DELETE CASCADE` en la base."

**Problema:** la afirmación implícita de que podría no haber cascade es incorrecta y puede llevar a
decisiones equivocadas. Verifiqué `VentaConfiguration.cs` y el `ApplicationDbContextModelSnapshot`:
`Detalles`, `Pagos` (VentaPago) y `Comprobantes` están configurados con `OnDelete(DeleteBehavior.Cascade)`
hacia `Venta`. El borrado explícito **funciona igual** (no es un defecto en sí), pero el razonamiento es
falso y, más importante, oculta la verdadera restricción relevante (ver hallazgo 3).

**Fix concreto:** corregir el texto para que refleje la realidad: "Las dependientes `Detalles`, `Pagos` y
`Comprobantes` tienen `ON DELETE CASCADE` hacia `Venta`, por lo que bastaría con `RemoveRange(ventasDemo)`;
se marcan igualmente de forma explícita para que el `AuditoriaInterceptor` registre cada dependiente borrada
(el cascade a nivel de base de datos NO pasa por el ChangeTracker ni genera auditoría de las dependientes)."
Ese matiz además justifica correctamente por qué cargar con `Include` y borrar en EF en lugar de confiar en
el cascade de SQL.

---

### 3. [MEDIUM] No se especifica el comportamiento frente a la FK `MovimientoCuentaCorriente → Venta` (Restrict)

**Dónde:** sección "Invariantes" y "Método público de regeneración (Data)". El diseño afirma que el filtro
`ClienteId == null` deja fuera cualquier venta con cliente y que por eso el borrado es seguro, pero no
menciona la FK `MovimientoCuentaCorriente.VentaId`.

**Problema:** verifiqué el snapshot: `MovimientoCuentaCorriente → Venta` usa `OnDelete(DeleteBehavior.Restrict)`.
Si por cualquier motivo una venta seleccionada para borrado tuviera un `MovimientoCuentaCorriente` asociado,
el `SaveChangesAsync` lanzaría `DbUpdateException` y la regeneración fallaría con 500. En la práctica las
ventas demo tienen `ClienteId == null` y el seeder actual nunca crea movimientos de CC para ellas, por lo
que el filtro las excluye — pero el diseño afirma la seguridad del borrado como invariante sin analizar esta
FK, que es precisamente la única con `Restrict` que apunta a `Venta`. Es un hueco en el análisis de
integridad referencial (RF integridad, criterio 5), no solo un detalle.

**Fix concreto:** agregar al diseño una frase explícita: "Las ventas seleccionadas (`ClienteId == null`) no
tienen `MovimientoCuentaCorriente` asociado (esos movimientos solo se crean para ventas con cliente), por lo
que la FK `MovimientoCuentaCorriente → Venta` (configurada como `Restrict`) nunca se viola en el borrado. El
filtro `ClienteId == null` es también la garantía contra esa FK." Si se quiere robustez extra, verificar en
el método que `ventasDemo` no tenga movimientos de CC antes de borrar.

---

### 4. [MEDIUM] Desajuste de mayúsculas entre el JSON del servidor y el DTO del cliente no está confirmado

**Dónde:** servidor devuelve objeto anónimo `new { ventasCreadas, mensaje }` (camelCase por defecto de
`System.Text.Json` web) y el cliente deserializa a `RegenerarResult { int VentasCreadas; string? Mensaje; }`
(PascalCase) con `ReadFromJsonAsync<RegenerarResult>()`.

**Problema:** el diseño da por hecho que la deserialización funciona pero no lo afirma ni lo justifica. El
mapeo PascalCase←camelCase solo funciona porque `ReadFromJsonAsync` en Blazor WASM usa
`JsonSerializerDefaults.Web` (property-name-case-insensitive = true). Es correcto por defecto, pero es una
dependencia implícita frágil: si alguien pasara opciones personalizadas sin `PropertyNameCaseInsensitive`,
`Mensaje`/`VentasCreadas` quedarían en `null`/`0` silenciosamente y el mensaje de éxito mostraría "0
venta(s)" aun habiendo creado ventas. El requisito 16 pide mostrar la cantidad real creada.

**Fix concreto:** documentar explícitamente la dependencia ("`ReadFromJsonAsync` usa defaults Web →
case-insensitive, por eso el DTO PascalCase mapea el JSON camelCase") y, para blindarlo, anotar las
propiedades del DTO con `[JsonPropertyName("ventasCreadas")]` / `[JsonPropertyName("mensaje")]`, o bien
devolver desde el servidor un objeto cuyos nombres coincidan exactamente. Alternativa más simple: el cliente
lee el fallback `payload?.Mensaje ?? $"...{payload?.VentasCreadas ?? 0}..."`, así que al menos definir el
comportamiento esperado cuando el mapeo falla.

---

### 5. [NIT] El manejo de 404 en el cliente es código muerto dada la decisión de 403

**Dónde:** sección "Botón y modal" (`ConfirmarRegeneracion`) y "Elección del código para el gating".

**Problema:** el servidor nunca devuelve 404 para el gating (se eligió 403, correctamente justificado), y la
ruta existe, así que `api/demo/regenerar` tampoco cae en el fallback SPA. La rama
`resp.StatusCode == HttpStatusCode.NotFound` del cliente nunca se ejecuta por esta vía. El diseño la incluye
"por robustez (S-2)", lo cual es defendible, pero conviene dejar claro que es defensiva.

**Fix concreto:** mantener la rama 404 (no molesta) pero comentar en el diseño: "El servidor solo emite 403
para el gating; la rama 404 es puramente defensiva por S-2 y no se ejercita en el flujo normal." Sin cambio
de código.

---

### 6. [NIT] "Firma pública de SeedDemoVentasAsync" — redacción imprecisa

**Dónde:** sección "Refactorización del seeder": "La firma pública de `SeedDemoVentasAsync` no cambia (sigue
siendo `private static async Task`)".

**Problema:** `SeedDemoVentasAsync` es `private`, no público; "firma pública" es contradictorio. Verifiqué
que efectivamente es `private static async Task SeedDemoVentasAsync(...)`. Es solo redacción.

**Fix concreto:** cambiar a "La firma de `SeedDemoVentasAsync` no cambia (sigue siendo
`private static async Task`) y `SeedAsync` lo sigue invoando igual."

---

### 7. [NIT] `[ProducesResponseType]` no declara 500 ni 401

**Dónde:** el `DemoController` de ejemplo declara `Status200OK` y `Status403Forbidden`.

**Problema:** la sección "Manejo de errores" describe un 500 vía `GlobalExceptionMiddleware` (verifiqué que
el middleware traduce excepciones genéricas y `DbUpdateException` a `InternalServerError`) y un 401 para
no autenticados, pero el contrato OpenAPI no los anuncia. No afecta el comportamiento, solo la documentación
del endpoint. El patrón de `ReportesController` tampoco es exhaustivo, así que es menor.

**Fix concreto (opcional):** agregar `[ProducesResponseType(StatusCodes.Status401Unauthorized)]` y
`[ProducesResponseType(StatusCodes.Status500InternalServerError)]` al método para completar el contrato.

---

### 8. [NIT] Ruido de auditoría potencialmente grande no cuantificado

**Dónde:** sección "Interacción con el AuditoriaInterceptor".

**Problema:** el diseño acepta correctamente el ruido de auditoría (está permitido por los requisitos), y
verifiqué que `Venta`, `DetalleVenta` y `Comprobante` están en `EntidadesAuditables` y `VentaPago` no. Pero
una regeneración borra ~100-150 ventas + sus detalles/comprobantes y crea otras tantas, todo en dos
`SaveChangesAsync` que el interceptor amplía con un `AuditoriaLog` por entidad Added/Modified/Deleted. Son
varios cientos de inserciones de auditoría por clic. Es aceptable por requisito, pero el diseño no menciona
el orden de magnitud ni que ocurre dentro de la misma transacción (impacto en duración/locks).

**Fix concreto:** añadir una nota: "Cada regeneración produce del orden de cientos de registros
`AuditoriaLog` (borrado + alta de ventas, detalles y comprobantes); es aceptable por requisito y ocurre
dentro de las transacciones de los dos `SaveChangesAsync`." Sin cambio de código.

---

## Supuestos verificados (correctos contra el código)

- **DbSets:** `context.Ventas`, `context.DetallesVenta`, `context.VentaPagos`, `context.Comprobantes` existen
  con esos nombres exactos en `ApplicationDbContext.cs`. ✔
- **Propiedades de navegación de `Venta`:** `Detalles`, `Pagos`, `Comprobantes` existen en `Venta.cs` con
  esos nombres; los `Include(v => v.Detalles/.Pagos/.Comprobantes)` del diseño son válidos. ✔
- **Policy `RequireAdmin`:** existe en `Program.cs` como `RequireRole("Administrador")`. ✔
- **Registro de `ApplicationDbContext`:** registrado vía `AddDbContext` (Scoped) en `Program.cs`. ✔
- **`GlobalExceptionMiddleware`:** registrado en `Program.cs` y traduce excepciones genéricas y
  `DbUpdateException` a 500; devuelve JSON. El claim de "500 para excepción durante el borrado" es correcto. ✔
- **Patrón de `ReportesController`:** `[ApiController]`, `[Route("api/...")]`, DI por constructor,
  `[Authorize(Policy = "RequireAdmin")]`, `[ProducesResponseType]`, `return Ok(...)`. El `DemoController`
  propuesto lo sigue fielmente. ✔
- **`EntidadesAuditables`:** `Venta`, `DetalleVenta`, `Comprobante` están incluidas; `VentaPago` NO. El claim
  del diseño sobre qué se audita y qué no es exacto. ✔
- **`GetCurrentUserId` / auditoría por request:** el interceptor resuelve el usuario desde
  `ClaimTypes.NameIdentifier`/`sub` y solo audita si hay `HttpContext`; el claim del diseño es correcto. ✔
- **`_Imports.razor` del cliente:** incluye `System.Net.Http.Json` (`ReadFromJsonAsync` disponible) pero NO
  `System.Net` (`HttpStatusCode`). El diseño anticipa correctamente que hay que agregar `@using System.Net`
  localmente. ✔
- **Cabecera del Dashboard:** el markup actual coincide con lo descrito (`<div class="d-flex justify-content-between
  align-items-center mb-3">` con `<h3>` y un botón "Actualizar"); el patrón `_errorMessage`, `_isLoading` y el
  método `Cargar()` existen tal como asume el diseño. ✔
- **`@attribute [Authorize(Roles = "Administrador")]`** ya está en `Dashboard.razor`. ✔
- **appsettings:** `appsettings.json` y `appsettings.Development.json` existen y pueden recibir la sección
  `Demo`. ✔
- **Numeración de comprobantes:** el generador calcula `MAX(NumeroComprobante) + 1`; como el borrado se
  persiste antes de regenerar, la numeración recalcula sobre los comprobantes restantes. Claim correcto. ✔

## Supuestos no verificados / incorrectos

- **"Evita suponer que exista ON DELETE CASCADE" (INCORRECTO):** sí existe cascade para `Detalles`, `Pagos`
  y `Comprobantes` hacia `Venta` (ver hallazgo 2). La afirmación del diseño es falsa; el enfoque funciona
  igual pero por otra razón (auditoría de dependientes).
- **FK `MovimientoCuentaCorriente → Venta` no analizada (HUECO):** es la única FK `Restrict` que apunta a
  `Venta`; el diseño no la menciona. En la práctica el filtro `ClienteId == null` la evita, pero no está
  verificado/documentado en el diseño (ver hallazgo 3).
- **Neutralidad del stock en regeneraciones repetidas (INCORRECTO/OMITIDO):** el diseño asume implícitamente
  que reutilizar el generador es seguro, pero el decremento de `Producto.Stock` no se compensa al borrar, por
  lo que el stock deriva a la baja con cada clic (ver hallazgo 1). No verificado por el diseño.
- **Mapeo JSON camelCase↔PascalCase (NO CONFIRMADO en el diseño):** funciona por los defaults Web de
  `ReadFromJsonAsync`, pero el diseño no lo establece explícitamente (ver hallazgo 4).
- **Comportamiento en runtime (fuera de alcance por requisito 19):** borrado/regeneración reales contra SQL
  Server, render del modal e invocación HTTP no se verifican; el diseño lo declara correctamente fuera de
  alcance. No evaluado aquí.

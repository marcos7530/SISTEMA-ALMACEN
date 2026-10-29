# Code review: Botón "Regenerar datos demo"

Commit revisado: `0fd00a0` — feat: boton Regenerar datos demo en dashboard (Data/Server/Client).

## Evidencia de build

El mensaje de commit registra:
`dotnet build SistemaAlmacen.slnx -c Debug --nologo -v minimal -> exit 0, 49 advertencias preexistentes (NuGet/CS8620), sin warnings nuevos. App no ejecutada (requiere SQL Server).`

Evidencia de build presente y con exit 0 / sin warnings nuevos de código. No se reejecutó el build (instrucción del step). La no ejecución de la app es parte del alcance (requiere SQL Server), no un hallazgo.

## Verificación de los puntos requeridos

**(a) FIX HIGH — el stock no deriva a 0.** `RegenerarDemoVentasAsync` (DataSeeder.cs) carga los detalles de las ventas a borrar, agrupa por `ProductoId`, carga esos productos y hace `producto.Stock += detalle.Cantidad` ANTES del `RemoveRange`. Luego regenera con `GenerarDemoVentasAsync(..., descontarStock: true)`, que vuelve a descontar. El neto deja el stock consistente en regeneraciones repetidas. `GenerarDemoVentasAsync` quedó parametrizado con `bool descontarStock`; el arranque lo invoca con `true`. Correcto.

**(b) Solo ventas de mostrador del mes en curso.** El filtro de borrado es idéntico en `SeedDemoVentasAsync` y `RegenerarDemoVentasAsync`: `v.Fecha >= inicioMes && (Estado == Confirmada || Estado == PendienteFacturacion) && v.ClienteId == null`, con `inicioMes = new DateTime(ahora.Year, ahora.Month, 1)` y `ahora = DateTime.Now`. Las ventas con cliente (`ClienteId != null`) y las de meses anteriores (`Fecha < inicioMes`) quedan excluidas. Correcto.

**(c) Gating por config.** `DemoController.RegenerarDemo` lee `_configuration.GetValue<bool>("Demo:RegeneracionHabilitada", false)` (default `false`), y si está deshabilitado retorna `StatusCode(403, { mensaje })` con `LogWarning`, sin tocar datos. `appsettings.json` -> `"Demo": { "RegeneracionHabilitada": false }`; `appsettings.Development.json` -> `true`. Correcto.

**(d) Alineación camelCase/JsonPropertyName.** El DTO local `RegenerarResult` anota `[JsonPropertyName("ventasCreadas")]` y `[JsonPropertyName("mensaje")]`; el servidor emite objeto anónimo camelCase `{ ventasCreadas, mensaje }`. Alineado. Correcto.

**Otros requisitos:**
- `SeedDemoVentasAsync` sigue siendo `private static async Task` con la guarda `if (ventasMostradorMes >= 30) return;` intacta, invocado desde `SeedAsync`. ✓
- Borrado en orden de FK seguro: se marca `RemoveRange` de DetallesVenta, VentaPagos, Comprobantes y luego Ventas, con un único `SaveChangesAsync`. Las navegaciones `Venta.Detalles/.Pagos/.Comprobantes` existen (verificado en Venta.cs). ✓
- `[ProducesResponseType]` para 200, 401, 403, 500. ✓
- Botón + modal de confirmación in-Blazor sin JS (`_mostrarConfirmacion`, patrón `modal fade show d-block` + backdrop), botones deshabilitados durante `_regenerando`, texto "Regenerando...". ✓
- Manejo de 403 (y 404 defensivo) en el cliente con mensaje de deshabilitado; refresco con `Cargar()` y `alert-success` en éxito. ✓
- Policy `RequireAdmin` existe en Program.cs; `@attribute [Authorize(Roles = "Administrador")]` en la página. ✓
- Sin warnings nuevos de código según evidencia de build.

## Spot-check

Verificadas con grep las entidades usadas: `DetalleVenta.ProductoId/.Cantidad`, `Producto.Stock`, navegaciones de `Venta`, y la policy `RequireAdmin`. Todo lo referenciado por el diff existe y es de los tipos usados.

## Hallazgos de la revisión de diseño

Los 8 hallazgos del design-review fueron atendidos (el HIGH de stock, el MEDIUM de camelCase, la aclaración del cascade/Restrict y el gating 403 están reflejados tanto en código como en comentarios). No quedan bloqueantes.

## Veredicto

APPROVED. No hay bloqueantes de corrección, seguridad ni integridad de datos.

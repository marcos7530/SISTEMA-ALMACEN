# Diseño técnico: Botón "Regenerar datos demo" en el Dashboard

## Resumen

Esta función agrega la capacidad de **borrar y regenerar bajo demanda** las ventas de
demostración del mes en curso, disparada por el Administrador desde el Dashboard. La solución
toca tres proyectos: `SistemaAlmacen.Data` (refactorizar el seeder y exponer un método público
`RegenerarDemoVentasAsync`), `SistemaAlmacen.Server` (un `DemoController` nuevo con el endpoint
`POST api/demo/regenerar`, gateado por configuración) y `SistemaAlmacen.Client` (un botón nuevo en
la cabecera del Dashboard con un modal de confirmación in-Blazor). No se modifican `Shared` ni
`Business`; la respuesta del endpoint es un objeto anónimo serializado a JSON.

El stack queda fijado así: **.NET 10**, **Blazor WebAssembly** (cliente), **ASP.NET Core** con
controllers MVC (`[ApiController]`), **EF Core + SQL Server** (acceso a datos), **Bootstrap 5 +
bootstrap-icons** (UI), todo en **español**. No se introduce ninguna librería nueva.

El flujo completo es: el Administrador hace clic en "Regenerar datos demo" → aparece un modal de
confirmación → al confirmar, el cliente hace `POST api/demo/regenerar` → el server valida la bandera
`Demo:RegeneracionHabilitada`, llama a `DataSeeder.RegenerarDemoVentasAsync`, que borra las ventas
demo del mes (con dependientes, en orden de FK) y regenera → devuelve `{ ventasCreadas, mensaje }` →
el cliente refresca el Dashboard con `Cargar()` y muestra el resultado.

## Decisiones de diseño

### Refactorización del seeder (Data)

El método privado `SeedDemoVentasAsync` actual hace dos cosas acopladas: (1) el chequeo de
idempotencia de arranque (`ventasMostradorMes >= 30 → return`) y (2) la generación propiamente dicha
(construir ventas, detalles, pagos, comprobantes y persistir). Para que arranque y regeneración
compartan exactamente el mismo código de generación sin duplicarlo, se extrae la generación a un
método privado nuevo:

```csharp
private static async Task<int> GenerarDemoVentasAsync(ApplicationDbContext context, ILogger logger)
```

Este método contiene **todo el bloque actual que sigue al `if (ventasMostradorMes >= 30) return;`**:
la carga de productos/usuarios/medios de pago, el `Random(20260101)`, el bucle de 30 días, y el
`AddRange` + `SaveChangesAsync` final. Devuelve la cantidad de ventas creadas (`ventasNuevas.Count`),
o `0` si se omite por falta de productos o usuarios (hoy esos casos hacen `return;` sin valor; pasan a
`return 0;`). El log informativo final se conserva dentro de este método.

`SeedDemoVentasAsync` queda reducido a la guarda de idempotencia y una llamada al nuevo método:

```csharp
private static async Task SeedDemoVentasAsync(ApplicationDbContext context, ILogger logger)
{
    var ahora = DateTime.Now;
    var inicioMes = new DateTime(ahora.Year, ahora.Month, 1);

    var ventasMostradorMes = await context.Ventas
        .Where(v => v.Fecha >= inicioMes
                    && (v.Estado == EstadoVenta.Confirmada || v.Estado == EstadoVenta.PendienteFacturacion)
                    && v.ClienteId == null)
        .CountAsync();

    if (ventasMostradorMes >= 30)
        return;

    await GenerarDemoVentasAsync(context, logger);
}
```

Esto **preserva la idempotencia de arranque** (RF-2, criterios 7): el arranque sigue contando y
saliendo temprano si ya hay ≥ 30 ventas de mostrador en el mes. La firma pública de
`SeedDemoVentasAsync` no cambia (sigue siendo `private static async Task`), y `SeedAsync` lo sigue
invocando igual.

**Criterio del "mes en curso" y de "venta demo".** Se reutiliza literalmente el mismo filtro que ya
usa la idempotencia: mostrador (`ClienteId == null`), `Estado ∈ { Confirmada, PendienteFacturacion }`,
`Fecha >= inicioMes` donde `inicioMes = new DateTime(ahora.Year, ahora.Month, 1)` con
`ahora = DateTime.Now` (hora local, consistente con el dashboard y con el generador). No existe un
marcador `esDemo` en la entidad `Venta`; se asume (S-3) que toda venta que cumple ese filtro en el mes
es demo. Las ventas con cliente (CC/fiadas) tienen `ClienteId != null` y quedan excluidas por el filtro
(criterio 3); las de meses anteriores quedan excluidas por `Fecha >= inicioMes` (criterio 4). Las
ventas en estado `Borrador` o `Anulada` no se tocan (no entran en el filtro de estados).

### Método público de regeneración (Data)

Se agrega a `DataSeeder`:

```csharp
public static async Task<int> RegenerarDemoVentasAsync(ApplicationDbContext context, ILogger logger)
```

Pasos:

1. Calcular `inicioMes` igual que arriba.
2. Cargar en memoria las ventas demo del mes **con sus dependientes** para que EF las rastree y pueda
   borrarlas respetando las FKs:

   ```csharp
   var ventasDemo = await context.Ventas
       .Where(v => v.Fecha >= inicioMes
                   && (v.Estado == EstadoVenta.Confirmada || v.Estado == EstadoVenta.PendienteFacturacion)
                   && v.ClienteId == null)
       .Include(v => v.Detalles)
       .Include(v => v.Pagos)
       .Include(v => v.Comprobantes)
       .ToListAsync();
   ```

3. Borrar en orden compatible con las FKs: primero dependientes, luego las ventas. Con EF Core, dado
   que las dependientes están cargadas vía `Include`, se las marca `Remove` explícitamente antes que la
   `Venta` para no depender de cascadas configuradas:

   ```csharp
   context.DetallesVenta.RemoveRange(ventasDemo.SelectMany(v => v.Detalles));
   context.VentaPagos.RemoveRange(ventasDemo.SelectMany(v => v.Pagos));
   context.Comprobantes.RemoveRange(ventasDemo.SelectMany(v => v.Comprobantes));
   context.Ventas.RemoveRange(ventasDemo);
   await context.SaveChangesAsync();
   ```

   Se hace un único `SaveChangesAsync` tras marcar todo: EF Core ordena las sentencias `DELETE`
   respetando el grafo de dependencias dentro de la misma transacción implícita, por lo que no se viola
   ninguna restricción referencial (criterio 5). Marcar las dependientes explícitamente evita suponer
   que exista `ON DELETE CASCADE` en la base.
4. Regenerar llamando a `GenerarDemoVentasAsync(context, logger)` (el método extraído) y **devolver su
   resultado** (cantidad de ventas creadas). No se vuelve a verificar la guarda de idempotencia: tras el
   borrado, el conteo del mes es bajo y la generación debe correr siempre; por eso la regeneración llama
   directamente al generador y **no** a `SeedDemoVentasAsync`.
5. Registrar un `logger.LogInformation` con las ventas borradas y creadas.

**Nota sobre el número correlativo de comprobante.** `GenerarDemoVentasAsync` calcula
`siguienteNroComprobante` a partir de `MAX(NumeroComprobante)` existente. Como el borrado ocurre y se
persiste **antes** de regenerar, los comprobantes demo del mes ya no existen al recalcular el máximo,
de modo que la numeración se mantiene coherente con los comprobantes que quedan (los de meses
anteriores y los de datos de prueba fijos).

### Interacción con el AuditoriaInterceptor

El `AuditoriaInterceptor` audita `Venta`, `DetalleVenta` y `Comprobante` (están en
`EntidadesAuditables`) en estados Added/Modified/Deleted, pero **solo cuando hay `HttpContext`**. Como
esta operación llega por un request autenticado, se generarán registros `AuditoriaLog` por cada venta y
dependiente borrado y creado. Según los requisitos (RNF de auditoría, S-3) ese "ruido" es **aceptable y
no bloqueante**; no se modifica el interceptor ni se intenta silenciarlo. Se deja constancia explícita
en el log informativo y en este documento. Nota: `VentaPago` **no** está en `EntidadesAuditables`, así
que sus borrados/altas no generan auditoría; esto no requiere acción.

El interceptor resuelve `GetCurrentUserId()` desde los claims del JWT; el Administrador autenticado
tendrá un `NameIdentifier` válido, por lo que los `AuditoriaLog` se atribuyen a ese usuario sin
problemas de FK.

### Endpoint de regeneración (Server)

Se crea `src\SistemaAlmacen.Server\Controllers\DemoController.cs` siguiendo el patrón de
`ReportesController`:

```csharp
[ApiController]
[Route("api/demo")]
[Authorize(Policy = "RequireAdmin")]
public class DemoController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DemoController> _logger;

    public DemoController(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<DemoController> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("regenerar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RegenerarDemo()
    {
        var habilitado = _configuration.GetValue<bool>("Demo:RegeneracionHabilitada", false);
        if (!habilitado)
        {
            _logger.LogWarning("Intento de regeneración de datos demo con la función deshabilitada por configuración.");
            return StatusCode(StatusCodes.Status403Forbidden,
                new { mensaje = "La regeneración de datos demo está deshabilitada en este entorno." });
        }

        var ventasCreadas = await DataSeeder.RegenerarDemoVentasAsync(_context, _logger);

        return Ok(new
        {
            ventasCreadas,
            mensaje = $"Se regeneraron los datos demo del mes: {ventasCreadas} venta(s) creada(s)."
        });
    }
}
```

**Dependencias por DI.** `ApplicationDbContext` ya está registrado como Scoped en `Program.cs`;
`IConfiguration` e `ILogger<DemoController>` están disponibles por defecto. No hace falta registrar
nada nuevo. `DataSeeder.RegenerarDemoVentasAsync` recibe directamente este `_context` (el mismo del
request, con el `AuditoriaInterceptor` ya enganchado), por eso el logger que se le pasa es
`ILogger<DemoController>`; el parámetro del método está tipado como `ILogger`, que acepta cualquier
`ILogger<T>`.

**Autorización.** `[Authorize(Policy = "RequireAdmin")]` a nivel de clase cubre el endpoint (la policy
ya existe: `RequireRole("Administrador")`). Un no-autenticado recibe 401 y un autenticado sin rol
Administrador recibe 403, antes de ejecutar nada.

**Elección del código para el gating (403 vs 404).** Se elige **403 Forbidden**. Justificación: la ruta
`api/demo/regenerar` **existe** y el usuario **está autorizado por rol**, pero la operación está
deshabilitada por una política del entorno; semánticamente eso es "entendí la petición pero me niego a
ejecutarla", que es 403, no "no existe el recurso" (404). Además, devolver 403 evita que el cliente
confunda el gating con un error de ruteo. Se documenta y el cliente igualmente maneja 403 y 404 de
forma robusta (S-2). El cuerpo incluye `mensaje` para que el cliente pueda mostrar texto claro.

**Tipo de retorno.** Se usa `IActionResult` (no `ActionResult<T>`) porque el cuerpo es un objeto
anónimo distinto en el camino OK (`{ ventasCreadas, mensaje }`) y en el camino 403 (`{ mensaje }`); un
objeto anónimo es suficiente (S-6) y evita crear un DTO en `Shared`. `System.Text.Json` serializa
objetos anónimos sin problema.

### Configuración (Server)

Se agrega la sección `Demo` en ambos `appsettings`:

- `appsettings.json` (producción, valor por defecto seguro): `"Demo": { "RegeneracionHabilitada": false }`.
- `appsettings.Development.json` (desarrollo): `"Demo": { "RegeneracionHabilitada": true }`.

`GetValue<bool>("Demo:RegeneracionHabilitada", false)` lee el valor efectivo según el entorno (en
Development, el archivo de Development sobrescribe al base). El default `false` garantiza que, aun si
faltara la clave, la operación queda deshabilitada (criterios 10, 11).

### Botón y modal en el Dashboard (Client)

En `src\SistemaAlmacen.Client\Pages\Dashboard\Dashboard.razor`:

**Cabecera.** El bloque actual tiene el `<h3>` y un único botón "Actualizar". Se agrupan los dos botones
en un contenedor a la derecha:

```razor
<div class="d-flex justify-content-between align-items-center mb-3">
    <h3 class="mb-0">Dashboard</h3>
    <div class="d-flex gap-2">
        <button class="btn btn-sm btn-outline-secondary" @onclick="Cargar"
                disabled="@(_isLoading || _regenerando)">
            <i class="bi bi-arrow-clockwise"></i> Actualizar
        </button>
        <button class="btn btn-sm btn-outline-primary" @onclick="MostrarConfirmacion"
                disabled="@(_isLoading || _regenerando)">
            <i class="bi bi-database-fill-gear"></i>
            @(_regenerando ? "Regenerando..." : "Regenerar datos demo")
        </button>
    </div>
</div>
```

**Modal de confirmación in-Blazor (sin JS).** Se implementa con un `bool _mostrarConfirmacion` que
controla el markup de un modal Bootstrap renderizado manualmente (sin la API JS de Bootstrap, para no
depender de `IJSRuntime` — RF-5, S-5). Se renderiza solo cuando la bandera está activa:

```razor
@if (_mostrarConfirmacion)
{
    <div class="modal fade show d-block" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-dialog-centered" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Regenerar datos demo</h5>
                    <button type="button" class="btn-close" aria-label="Cerrar"
                            @onclick="CancelarConfirmacion" disabled="@_regenerando"></button>
                </div>
                <div class="modal-body">
                    <p>Esta acción <strong>borra las ventas de demostración del mes en curso</strong>
                       (ventas de mostrador) y las vuelve a generar.</p>
                    <p class="mb-0 text-muted">No afecta ventas con cliente (cuenta corriente) ni
                       ventas de meses anteriores.</p>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary"
                            @onclick="CancelarConfirmacion" disabled="@_regenerando">Cancelar</button>
                    <button type="button" class="btn btn-primary"
                            @onclick="ConfirmarRegeneracion" disabled="@_regenerando">
                        @(_regenerando ? "Regenerando..." : "Confirmar")
                    </button>
                </div>
            </div>
        </div>
    </div>
    <div class="modal-backdrop fade show"></div>
}
```

La combinación `modal fade show d-block` + un `modal-backdrop show` es el patrón estándar para mostrar
un modal Bootstrap 5 "a mano" en Blazor sin invocar JS. El ícono `bi bi-database-fill-gear` ya pertenece
al set de bootstrap-icons incluido.

**Estado y feedback.** Se añade un mensaje de éxito independiente del error para no pisar el patrón
`_errorMessage` existente. Nuevos campos y métodos en `@code`:

```csharp
private bool _mostrarConfirmacion;
private bool _regenerando;
private string? _successMessage;

private void MostrarConfirmacion()
{
    _successMessage = null;
    _errorMessage = null;
    _mostrarConfirmacion = true;
}

private void CancelarConfirmacion()
{
    if (_regenerando) return; // no cerrar mientras corre
    _mostrarConfirmacion = false;
}

private async Task ConfirmarRegeneracion()
{
    _regenerando = true;
    _errorMessage = null;
    _successMessage = null;

    try
    {
        var resp = await Http.PostAsync("api/demo/regenerar", null);

        if (resp.IsSuccessStatusCode)
        {
            var payload = await resp.Content.ReadFromJsonAsync<RegenerarResult>();
            _mostrarConfirmacion = false;
            await Cargar(); // refresca métricas y gráficos
            _successMessage = payload?.Mensaje
                ?? $"Datos demo regenerados: {payload?.VentasCreadas ?? 0} venta(s) creada(s).";
        }
        else if (resp.StatusCode == HttpStatusCode.Forbidden
                 || resp.StatusCode == HttpStatusCode.NotFound)
        {
            _errorMessage = "La regeneración de datos demo está deshabilitada en este entorno.";
        }
        else
        {
            _errorMessage = "No se pudieron regenerar los datos demo. Intente nuevamente.";
        }
    }
    catch (Exception)
    {
        _errorMessage = "Error de comunicación al regenerar los datos demo.";
    }
    finally
    {
        _regenerando = false;
    }
}

private sealed class RegenerarResult
{
    public int VentasCreadas { get; set; }
    public string? Mensaje { get; set; }
}
```

El mensaje de éxito se muestra junto al de error existente, arriba del contenido:

```razor
@if (!string.IsNullOrEmpty(_successMessage))
{
    <div class="alert alert-success">@_successMessage</div>
}
```

**Nota de `using`.** El uso de `HttpStatusCode` requiere `@using System.Net` en el `.razor` (o en
`_Imports.razor`); `ReadFromJsonAsync` requiere `System.Net.Http.Json`, que normalmente ya está en
`_Imports.razor` del cliente (se verifica durante la implementación y se agrega el `@using` localmente
si falta, sin tocar `_Imports.razor` salvo necesidad).

`Http.PostAsync("api/demo/regenerar", null)` envía el POST sin cuerpo tal como pide el requisito (RF-6);
el `BaseAddress` del `HttpClient` ya apunta al server (mismo patrón que las llamadas `GetFromJsonAsync`
existentes).

## Archivos a modificar / crear

- **Modificar** `src\SistemaAlmacen.Data\Context\DataSeeder.cs`: extraer `GenerarDemoVentasAsync`
  (privado), simplificar `SeedDemoVentasAsync`, agregar `RegenerarDemoVentasAsync` (público).
- **Crear** `src\SistemaAlmacen.Server\Controllers\DemoController.cs`.
- **Modificar** `src\SistemaAlmacen.Server\appsettings.json`: agregar `"Demo": { "RegeneracionHabilitada": false }`.
- **Modificar** `src\SistemaAlmacen.Server\appsettings.Development.json`: agregar
  `"Demo": { "RegeneracionHabilitada": true }`.
- **Modificar** `src\SistemaAlmacen.Client\Pages\Dashboard\Dashboard.razor`: botón, modal, estado y
  métodos; `@using System.Net` si hiciera falta.

Sin cambios en `Shared`, `Business` ni `Program.cs` (la policy `RequireAdmin` y el registro de
`ApplicationDbContext` ya existen).

## Manejo de errores (concreto por operación)

- **Gating deshabilitado** (`Demo:RegeneracionHabilitada == false`): condición esperada, **no fatal**.
  El endpoint retorna **403** con `{ mensaje }` sin ejecutar borrado ni generación. Se loguea como
  `LogWarning`. El cliente muestra "La regeneración de datos demo está deshabilitada en este entorno."
- **No autorizado / no autenticado**: lo maneja el middleware de autorización (401/403) antes del cuerpo
  del método; el cliente trata 403 como "deshabilitada" (mensaje claro) y cualquier 401 lo captura el
  flujo de auth global existente.
- **Falta de productos o usuarios activos** al regenerar: **no es error** (S-4). `GenerarDemoVentasAsync`
  loguea `LogInformation("...se omite")` y devuelve `0`. El endpoint responde 200 con `ventasCreadas = 0`
  y el cliente muestra el mensaje de éxito indicando 0 ventas. Se considera comportamiento heredado del
  seeder, no una falla.
- **Excepción durante el borrado o la persistencia** (p.ej. fallo de SQL, timeout, violación inesperada
  de FK): **fatal para la operación**. La excepción se propaga desde `RegenerarDemoVentasAsync`; el
  `GlobalExceptionMiddleware` del server la captura y devuelve 500. EF Core ejecuta el `SaveChangesAsync`
  del borrado en una transacción implícita: si falla, no se borra parcialmente. El cliente cae en la rama
  `!IsSuccessStatusCode` → "No se pudieron regenerar los datos demo. Intente nuevamente." Se loguea en el
  server vía el middleware existente.
- **Error de red / cliente** (`PostAsync` lanza, JSON ilegible): el `catch` del cliente muestra "Error de
  comunicación al regenerar los datos demo." No deja el botón bloqueado (el `finally` restablece
  `_regenerando`).
- **Doble clic / reentrada**: `_regenerando` deshabilita ambos botones y los del modal mientras corre
  (criterio 15), evitando POST concurrentes desde la misma vista.

## Validación de entradas

El endpoint **no recibe entradas del cliente** (POST sin cuerpo ni parámetros), por lo que no hay
payload que validar. La única "entrada" es de configuración:

- `Demo:RegeneracionHabilitada`: opcional en el archivo; tipo `bool`; si falta o no es parseable como
  bool, `GetValue<bool>(..., false)` devuelve el default `false` → operación deshabilitada (fail-safe).

El criterio de selección de ventas a borrar es interno (no viene del cliente) y está acotado por el
filtro `ClienteId == null` + estados + `Fecha >= inicioMes`, lo que constituye la barrera de integridad:
ninguna entrada externa puede ampliar ese conjunto.

## Invariantes y dónde se aplican

- **"Solo se borran ventas demo del mes en curso; nunca ventas con cliente ni de meses anteriores."**
  La aplica la **capa de datos** (`RegenerarDemoVentasAsync`), porque es quien construye la consulta de
  borrado; el filtro vive junto a la operación de borrado para que no pueda eludirse desde otra capa. El
  cliente y el controller no pueden alterar el criterio.
- **"El borrado respeta el orden de FKs."** La aplica la **capa de datos** marcando dependientes antes
  que las ventas y delegando el ordenamiento de `DELETE` a EF Core dentro de una única transacción.
- **"La regeneración solo corre en entornos habilitados."** La aplica la **capa server** (controller) vía
  la bandera de configuración; es una barrera de entorno, independiente de la autorización por rol.
- **"Operación solo para Administrador."** La aplican dos capas de forma redundante: el **server** con la
  policy `RequireAdmin` (barrera real de seguridad) y el **cliente** con `@attribute [Authorize(Roles =
  "Administrador")]` en la página (barrera de UX). La autoridad efectiva es la del server.

## Edge cases

- **Mes sin ventas demo previas** (base recién migrada o primer uso del mes): el borrado no encuentra
  filas (lista vacía, `SaveChanges` no-op) y la generación crea el set completo. `ventasCreadas > 0`.
- **Ya hay ≥ 30 ventas demo del mes**: la regeneración **igual borra y vuelve a generar** (no aplica la
  guarda de idempotencia, que es exclusiva del arranque). Ese es el objetivo del botón.
- **Ventas de mostrador en estado `Borrador` o `Anulada` dentro del mes**: no entran en el filtro (solo
  Confirmada/PendienteFacturacion), así que no se borran. Correcto: no son ventas demo "visibles" en los
  reportes.
- **Comprobantes huérfanos**: no deberían existir porque todo comprobante demo cuelga de una venta demo;
  al cargar con `Include(v => v.Comprobantes)` se borran junto con su venta.
- **Numeración de comprobantes**: el borrado se persiste antes de regenerar, de modo que
  `MAX(NumeroComprobante)` no cuenta los comprobantes recién borrados y la numeración sigue coherente.
- **Zona horaria**: se usa `DateTime.Now` (local) para `inicioMes` tanto en borrado como en generación,
  consistente con el generador existente y con el dashboard; no se mezcla con `DateTime.UtcNow`.

## Testabilidad

- **Unit testing (Data).** `RegenerarDemoVentasAsync` y `GenerarDemoVentasAsync` son estáticos y reciben
  `ApplicationDbContext` + `ILogger`, por lo que se pueden testear con el **InMemory provider** de EF
  Core o SQLite en memoria: sembrar ventas demo del mes, ventas con cliente y ventas de un mes anterior;
  invocar `RegenerarDemoVentasAsync`; verificar que (a) se borraron solo las demo del mes con sus
  dependientes, (b) quedaron intactas las de cliente y las de meses anteriores, (c) el valor devuelto es
  el conteo de ventas nuevas (> 0 si hay productos/usuarios). Nota: el InMemory provider no impone FKs, así
  que para validar el **orden de borrado** conviene SQLite en memoria (relacional) que sí respeta FKs.
- **Integration testing (Server).** El endpoint se puede probar con `WebApplicationFactory`: con la
  bandera en `true` y un token de Administrador, `POST api/demo/regenerar` devuelve 200 y
  `ventasCreadas >= 0`; con la bandera en `false`, devuelve 403; sin rol Administrador, 403/401. Requiere
  una base de pruebas (SQLite/SQL Server de test) por el acceso real a datos.
- **Cliente.** La lógica de `ConfirmarRegeneracion` (manejo de 200/403/404/excepción y seteo de mensajes)
  es testeable con bUnit mockeando `HttpClient`; no es imprescindible para esta entrega pero el diseño no
  lo impide. El modal in-Blazor no depende de JS, lo que lo hace testeable en render.
- No existe actualmente un proyecto de tests en la solución referenciado por el prompt; la verificación
  obligatoria de esta entrega es la **compilación** (`dotnet build`). Escribir tests nuevos queda
  disponible pero no bloquea, dado que no hay infraestructura de test establecida que seguir.

## Verificación

- `dotnet build SistemaAlmacen.slnx -c Debug --nologo -v minimal` debe terminar con **exit code 0**, sin
  warnings de código nuevos (los ~49 warnings preexistentes de NuGet son aceptables). El build de la
  solución incluye el proyecto Client, por lo que compila también `Dashboard.razor`.
- **No se ejecuta la aplicación** (`dotnet run` está prohibido en este entorno): la verificación en
  runtime (borrado/regeneración reales contra SQL Server, render del modal, llamada HTTP real) **queda
  fuera de alcance** y se deja constancia aquí. La corrección en runtime se infiere de la revisión de
  código y del build exitoso.

## Alineación con los criterios de aceptación

Cubiertos por el diseño: 1 (método público `Task<int>`), 2–5 (filtro + Include + orden de borrado), 6
(regenera y devuelve conteo), 7 (idempotencia de arranque intacta), 8 (DemoController con policy y DI),
9 (200 con cuerpo), 10 (403 sin ejecutar nada), 11 (ambos appsettings), 12–17 (botón, modal, loading,
refresh, mensajes de éxito/error incluido el caso deshabilitado), 18 (build exit 0), 19 (sin runtime,
constancia dejada).

# Requisitos: Botón "Regenerar datos demo" en el Dashboard

## Resumen

El sistema POS (Blazor WebAssembly + ASP.NET Core + EF Core SQL Server, .NET 10) ya genera
ventas de demostración al arrancar mediante `DataSeeder.SeedDemoVentasAsync`, pero esa generación
es idempotente y solo corre una vez por mes. El usuario Administrador necesita una forma de **borrar
y volver a generar** las ventas demo del mes en curso bajo demanda, desde el propio Dashboard, para
poder apreciar las métricas y gráficos con datos frescos.

Esta función agrega un botón "Regenerar datos demo" en la cabecera del Dashboard que, previa
confirmación obligatoria (acción destructiva), llama a un endpoint nuevo del servidor. El endpoint
borra únicamente las ventas de mostrador (demo) del mes actual con sus entidades dependientes y
vuelve a invocar la generación, devolviendo la cantidad de ventas creadas. El Dashboard refresca sus
datos y muestra el resultado. El endpoint está protegido por rol Administrador y por una bandera de
configuración que está encendida en desarrollo y apagada en producción.

El alcance abarca cinco proyectos de la solución: Data (método nuevo en el seeder), Server
(controller + configuración), Client (botón + modal), y se apoya en Shared/Business sin cambios de
contrato previstos.

## Requisitos funcionales

### RF-1 — Método de regeneración en el seeder (Data)
- Agregar a `src\SistemaAlmacen.Data\Context\DataSeeder.cs` un método público nuevo, p.ej.
  `public static async Task<int> RegenerarDemoVentasAsync(ApplicationDbContext context, ILogger logger)`.
- El método debe:
  1. Borrar las ventas DEMO del mes en curso, definidas como: ventas de mostrador con `ClienteId == null`,
     `Estado` ∈ { `Confirmada`, `PendienteFacturacion` } y `Fecha >= inicio del mes actual`.
  2. Borrar primero las entidades dependientes de esas ventas respetando las FKs: `DetalleVenta`,
     `VentaPago` y `Comprobante`, y luego las `Venta`.
  3. Volver a generar las ventas demo del mes.
  4. Devolver la cantidad de ventas creadas.
- **NO** debe tocar ventas con cliente (cuenta corriente / fiadas, `ClienteId != null`) ni ventas de
  meses anteriores (`Fecha < inicio del mes actual`).
- Se recomienda extraer la lógica de generación existente a un método reutilizable para que tanto el
  arranque como la regeneración compartan el mismo código.

### RF-2 — Preservar el comportamiento de arranque
- `SeedDemoVentasAsync` existente debe seguir funcionando igual: idempotente al arranque (solo genera si
  el mes en curso tiene menos de 30 ventas de mostrador) e invocado desde `SeedAsync`. La refactorización
  para reutilizar la generación no debe romper esta idempotencia.

### RF-3 — Endpoint de regeneración (Server)
- Crear un controller nuevo `src\SistemaAlmacen.Server\Controllers\DemoController.cs` con ruta base
  `api/demo` y acción `POST api/demo/regenerar`, siguiendo el patrón de `ReportesController.cs`
  (`[ApiController]`, `[Route]`, DI por constructor, `ActionResult`, `[ProducesResponseType]`).
- Protegido con `[Authorize(Policy = "RequireAdmin")]` (la policy ya existe en `Program.cs`,
  `RequireRole("Administrador")`).
- Inyectar `ApplicationDbContext` (ya registrado como Scoped) e `ILogger` por constructor.
- Llamar a `DataSeeder.RegenerarDemoVentasAsync(...)` y devolver `Ok` con un objeto
  `{ ventasCreadas = N, mensaje = "..." }`.

### RF-4 — Gating por configuración
- El endpoint solo funciona si la bandera `Demo:RegeneracionHabilitada` está en `true`.
- Leer la bandera con `IConfiguration.GetValue<bool>("Demo:RegeneracionHabilitada", false)`.
- Agregar la sección `{ "Demo": { "RegeneracionHabilitada": true } }` en
  `src\SistemaAlmacen.Server\appsettings.Development.json` y dejarla en `false` en
  `appsettings.json` (producción).
- Si la bandera está en `false`, el endpoint responde con un código de error coherente (403 o 404, a
  elegir y documentar en el diseño) sin ejecutar ningún borrado ni generación.

### RF-5 — Botón y modal en el Dashboard (Client)
- En `src\SistemaAlmacen.Client\Pages\Dashboard\Dashboard.razor`, dentro de la cabecera
  (`<div class="d-flex justify-content-between align-items-center mb-3">` que contiene el `<h3>Dashboard</h3>`
  y el botón "Actualizar"), agregar un segundo botón "Regenerar datos demo" junto al de Actualizar
  (envolviendo ambos en un contenedor si hace falta para que queden agrupados a la derecha).
- Usar Bootstrap 5 + bootstrap-icons ya presentes; ícono sugerido `bi bi-database-fill-gear` o
  `bi bi-arrow-repeat`.
- Al hacer clic, mostrar una confirmación en español (preferentemente un modal in-Blazor controlado por
  una variable `bool`, sin JS) advirtiendo que la acción **BORRA las ventas de demostración del mes y las
  vuelve a generar**, con botones Cancelar / Confirmar.

### RF-6 — Ejecución y feedback en el Dashboard
- Al confirmar: `POST` a `api/demo/regenerar` con `Http.PostAsync("api/demo/regenerar", null)`.
- Mostrar estado de carga: deshabilitar los botones y mostrar texto "Regenerando...".
- Al terminar con éxito: volver a invocar el método `Cargar()` existente para refrescar métricas y
  gráficos, y mostrar un mensaje de éxito (p.ej. `alert-success`) indicando cuántas ventas se crearon.
- Al fallar: mostrar un mensaje de error reutilizando el patrón de `_errorMessage` existente.
- Si el POST devuelve 403/404 por la bandera de configuración, mostrar un mensaje claro de que la
  regeneración está deshabilitada.

## Requisitos no funcionales

- **Seguridad / autorización**: la acción es solo para Administrador, garantizado por la policy en el
  server (`RequireAdmin`) y por `@attribute [Authorize(Roles = "Administrador")]` en la página. El gating
  por configuración es una barrera adicional para evitar ejecución accidental en producción.
- **Integridad de datos**: el borrado debe respetar el orden de FKs y acotarse estrictamente a las ventas
  demo del mes en curso; nunca debe afectar ventas con cliente ni de meses anteriores.
- **Idempotencia preservada**: la siembra de arranque debe seguir comportándose igual tras la
  refactorización.
- **Auditoría**: existe un `AuditoriaInterceptor` que audita cuando hay `HttpContext`. Como esta operación
  viene de un request, generará registros de auditoría; ese "ruido" de auditoría es aceptable y no debe
  bloquear la implementación.
- **No introducir warnings nuevos de código**: la solución tiene ~49 warnings preexistentes de
  NuGet/vulnerabilidades que son aceptables; no se deben agregar warnings nuevos de compilación de código.

## Criterios de aceptación

1. `DataSeeder` expone un método público `RegenerarDemoVentasAsync(ApplicationDbContext, ILogger)` que
   devuelve la cantidad de ventas creadas (`Task<int>` o equivalente documentado).
2. Al ejecutar la regeneración, se borran todas las ventas de mostrador del mes en curso
   (`ClienteId == null`, `Estado` ∈ { Confirmada, PendienteFacturacion }, `Fecha >= inicio de mes`) junto
   con sus `DetalleVenta`, `VentaPago` y `Comprobante` asociados.
3. Tras el borrado, las ventas con `ClienteId != null` (cuenta corriente / fiadas) permanecen intactas.
4. Las ventas (demo o no) con `Fecha < inicio del mes actual` permanecen intactas.
5. El borrado ocurre en orden compatible con las FKs (dependientes antes que `Venta`) y no lanza
   excepciones por violación de restricciones referenciales.
6. Tras la regeneración, el mes en curso contiene ventas demo nuevas y el método devuelve un conteo
   mayor que cero (asumiendo que existen productos y usuarios activos).
7. `SeedDemoVentasAsync` sigue siendo idempotente al arranque: no vuelve a sembrar si ya hay ≥ 30 ventas
   de mostrador en el mes.
8. Existe un controller `DemoController` con `POST api/demo/regenerar`, decorado con
   `[Authorize(Policy = "RequireAdmin")]`, que inyecta `ApplicationDbContext` e `ILogger` y llama a
   `RegenerarDemoVentasAsync`.
9. Con `Demo:RegeneracionHabilitada = true`, el endpoint responde `200 OK` con un cuerpo
   `{ ventasCreadas = N, mensaje = "..." }`.
10. Con `Demo:RegeneracionHabilitada = false`, el endpoint responde con el código de error elegido
    (403 o 404) y **no** ejecuta borrado ni generación.
11. `appsettings.Development.json` contiene `Demo:RegeneracionHabilitada = true` y `appsettings.json`
    contiene `Demo:RegeneracionHabilitada = false`.
12. `Dashboard.razor` muestra un botón "Regenerar datos demo" en la cabecera, junto al botón "Actualizar",
    con un ícono de bootstrap-icons.
13. Al hacer clic en el botón se muestra una confirmación en español que advierte explícitamente que la
    acción borra y regenera las ventas demo del mes, con opciones Cancelar y Confirmar.
14. Cancelar la confirmación no dispara ninguna llamada al endpoint.
15. Al confirmar, durante la operación los botones quedan deshabilitados y se muestra el texto
    "Regenerando...".
16. Tras una respuesta exitosa, el Dashboard vuelve a invocar `Cargar()` y muestra un mensaje de éxito con
    la cantidad de ventas creadas.
17. Ante un error del endpoint, el Dashboard muestra un mensaje de error usando el patrón `_errorMessage`;
    si el error es 403/404 por el flag, el mensaje indica que la regeneración está deshabilitada.
18. `dotnet build SistemaAlmacen.slnx -c Debug --nologo -v minimal` termina con exit code 0 y sin warnings
    de código nuevos (los ~49 warnings preexistentes de NuGet son aceptables).
19. No se ejecuta la aplicación (`dotnet run`) en este entorno; la verificación en runtime queda fuera de
    alcance y debe dejarse constancia de ello.

## Fuera de alcance

- Ejecutar la aplicación o verificar el comportamiento en runtime (`dotnet run` está prohibido en este
  entorno; solo se valida compilación con `dotnet build`).
- Resolver o reducir los ~49 warnings preexistentes de NuGet/vulnerabilidades.
- Consultar el flag `Demo:RegeneracionHabilitada` desde el cliente para ocultar el botón (el botón se
  muestra siempre; el gating lo aplica el server y el cliente reacciona al código de error).
- Modificar la lógica de distribución, volumen o contenido de las ventas demo más allá de lo necesario
  para reutilizar la generación existente.
- Regenerar datos demo de meses distintos al mes en curso o permitir seleccionar un rango de fechas.
- Internacionalización más allá del español ya usado en la aplicación.
- Cambios en el `AuditoriaInterceptor` para silenciar la auditoría de esta operación.

## Supuestos

- **S-1**: El método `RegenerarDemoVentasAsync` devolverá un `int` con la cantidad de ventas creadas, que
  es lo que el endpoint expondrá como `ventasCreadas`. (El prompt sugiere `Task<int>`.)
- **S-2**: Para el gating deshabilitado se elegirá un único código de estado (403 o 404); la elección y su
  justificación se fijan en el documento de diseño. El cliente debe manejar ambos para ser robusto.
- **S-3**: La definición de "ventas demo del mes" para el borrado usa el mismo criterio que la idempotencia
  de `SeedDemoVentasAsync` (mostrador: `ClienteId == null`, estados Confirmada/PendienteFacturacion,
  `Fecha >= inicio de mes`). Se asume que todas las ventas que cumplen ese criterio en el mes son demo y
  pueden borrarse; no existe un marcador explícito "esDemo" en la entidad `Venta`.
- **S-4**: Existen productos y usuarios activos en la base al momento de regenerar; si no, la generación se
  omite igual que en el arranque y `ventasCreadas` será 0 (comportamiento heredado del seeder, no un error).
- **S-5**: El modal de confirmación se implementa en Blazor sin JavaScript; si resultara desproporcionado,
  el diseño puede optar por `IJSRuntime`/`window.confirm`, documentando la decisión.
- **S-6**: No se requieren cambios en los proyectos Shared ni Business; el objeto de respuesta del endpoint
  puede ser anónimo o un DTO simple definido en el Server (a decidir en diseño).

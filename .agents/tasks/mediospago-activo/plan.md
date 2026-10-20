# Implementation Plan — Filtro "Activos" para Gestión de Medios de Pago

Replicate the Categorías "estado" filter on the Medios de Pago management page: add an estado
`<select>` (Activos | Inhabilitados | Todos) wired end-to-end through an optional `?activo=`
query param, exactly as Categorías does. Default view = **Activos** (matches the Categorías page
default of `_estadoFiltro = "activos"`). Each item leaves the solution compiling.

## Facts established by reading the code (do not re-derive)

- **Enabled-flag property name = `Activo`** (bool, default `true`) on
  `src/SistemaAlmacen.Data/Entities/MedioPago.cs`. It is NOT a differently-named field; the UI
  label "Habilitado"/"Inhabilitado" is just the display text for `Activo` true/false, same as
  Categorías. So no entity, EF config, migration, or DTO change is needed — `MedioPagoDto`
  already exposes `Activo`.
- **The whole backend toggle already exists** and matches Categorías: `MedioPago.SetActivoAsync`,
  the controller `PATCH api/medios-pago/{id}/estado` taking the shared
  `SistemaAlmacen.Shared.DTOs.CambiarEstadoRequest`, and the Razor `CambiarEstado` switch. Leave
  all of that untouched.
- **The ONLY gap is the listing filter.** Categorías exposes a single
  `GET api/categorias?activo=true|false` (null = todos) backed by
  `CategoriaService.GetAllAsync(bool? activo = null)`. Medios de Pago instead has two fixed GET
  endpoints: `GET api/medios-pago` → `GetActivosAsync()` (only active) and
  `GET api/medios-pago/all` → `GetAllAsync()` (all). The admin page calls `/all` unconditionally,
  so it always shows every medio with no way to filter.

## Design decisions (made here, grounded in the codebase)

- **Add the filter to the existing admin endpoint `GET api/medios-pago/all`, giving it an
  optional `[FromQuery] bool? activo`**, and change the service `GetAllAsync()` to
  `GetAllAsync(bool? activo = null)` that filters the already-sorted projection. Rationale: this
  reproduces the Categorías convention precisely — the SAME param name `activo`, the SAME
  semantics (`true` = solo activos, `false` = solo inhabilitados, `null`/omitido = todos), and
  the SAME in-service filtering approach (`CategoriaService.GetAllAsync` filters the list, the
  repository is not touched). I keep the admin page on the `/all` route rather than switching it
  to the Vendedor-facing `GET api/medios-pago`, because `/all` is `RequireAdmin` and the page is
  admin-only; reusing it preserves the authorization shape while the new `activo` param supplies
  the filtering Categorías expresses through its single endpoint.
- **Leave `GET api/medios-pago` → `GetActivosAsync()` completely untouched.** `NuevaVenta.razor`
  (the sales screen) calls `api/medios-pago` to populate the payment selector and must keep
  receiving only active medios. Confirmed by grep: the only other consumer of the medios-pago API
  is `NuevaVenta.razor` hitting `api/medios-pago` (not `/all`). The `/all` route is used ONLY by
  the admin page, so adding the optional param there has no other blast radius.
- **UI control = a `<select>` with the exact Categorías options and default.** Options in order:
  `Activos` (value `activos`, default), `Inhabilitados` (value `inactivos`), `Todos` (value
  `todos`), bound with `@bind` + `@bind:after`, matching `Categorias.razor` verbatim in label
  text and ordering. The MediosPago page is admin-only (`[Authorize(Roles = "Administrador")]`),
  so — unlike Categorías — there is NO `_isAdmin` guard around the filter or the query param; the
  select and the param are always present. This is the faithful adaptation of the pattern to a
  page whose entire audience is admins.
- **Default behavior changes from "todos" to "activos"** for the admin page, to match the
  Categorías page default (`_estadoFiltro = "activos"` → first load sends `?activo=true`). This is
  the intended UX per the user request ("que me traiga los medios de pagos activos"). The server
  default stays "todos" (param omitted = all), identical to Categorías; only the page's initial
  selection drives the active-only first load.
- **No new DTO.** `MedioPagoDto.Activo` already exists; the shared `CambiarEstadoRequest` is
  already reused by the toggle. The `DTOs/MediosPago/` folder needs no changes.
- **No repository change.** Mirrors Categorías, where `GetAllAsync(bool? activo)` filters in the
  service and `CategoriaRepository` was left alone. `MedioPagoRepository.GetActivosAsync` /
  `GetAllAsync` stay as-is.

## Environment constraints (read before building)

- Per the Categorías task notes for this same repo: a dev server process
  (`SistemaAlmacen.Server`) may be running and LOCKS the Server project output DLLs, so a full
  `dotnet build SistemaAlmacen.slnx` can fail with file-copy errors (MSB3021/MSB3026/MSB3027)
  that are NOT compile errors. Do NOT kill or restart the server unless the user asks. To verify
  compilation without the lock, build the individual non-host projects
  (`SistemaAlmacen.Business`, `SistemaAlmacen.Client`). A full-solution build requires the server
  to be stopped first by the user.
- Toolchain: .NET SDK 10.x; solution file is `SistemaAlmacen.slnx`. Test stack is xUnit +
  FluentAssertions + Moq (placeholder tests only — do NOT add tests for this change; the repo
  does not expect them here).

## Steps

- [ ] 1. Add an optional `activo` filter to the medios-de-pago listing service method.
      In `IMedioPagoService`, change `Task<List<MedioPagoDto>> GetAllAsync();` to
      `Task<List<MedioPagoDto>> GetAllAsync(bool? activo = null);` and update its XML doc to note
      `activo`: true solo activos, false solo inhabilitados, null todos (mirror
      `ICategoriaService.GetAllAsync`). In `MedioPagoService.GetAllAsync`, after fetching
      `await _unitOfWork.MediosPago.GetAllAsync()`, if `activo.HasValue` filter
      `.Where(mp => mp.Activo == activo.Value)` BEFORE the existing `.OrderBy(mp => mp.Nombre)`
      projection (same shape as `CategoriaService.GetAllAsync`). Leave `GetActivosAsync` unchanged.
      Files: src/SistemaAlmacen.Business/Interfaces/IMedioPagoService.cs,
      src/SistemaAlmacen.Business/Services/MedioPagoService.cs
      Verify: `dotnet build src/SistemaAlmacen.Business/SistemaAlmacen.Business.csproj` succeeds with 0 errors.

- [ ] 2. Accept the optional `activo` query param on the admin listing endpoint.
      In `MediosPagoController.GetAll` (route `GET api/medios-pago/all`), add parameter
      `[FromQuery] bool? activo` and pass it through: `await _medioPagoService.GetAllAsync(activo)`.
      Update the method's XML doc to mention the optional filter (mirror `CategoriasController.GetAll`).
      Do NOT change `GetActivos` (`GET api/medios-pago`) or the PATCH `/estado` endpoint.
      Files: src/SistemaAlmacen.Server/Controllers/MediosPagoController.cs
      Verify: `dotnet build src/SistemaAlmacen.Server/SistemaAlmacen.Server.csproj` succeeds with 0
      errors (if the running server locks the output DLL, the compile itself is the check; do not
      kill the server).

- [ ] 3. Add the estado filter `<select>` to the Medios de Pago page and send `?activo=` on load.
      In `MediosPago.razor`, mirror the Categorías filter exactly:
      (a) Add a `<select class="form-select" @bind="_estadoFiltro" @bind:after="ApplyEstadoFilter">`
      with options Activos(value `activos`)/Inhabilitados(value `inactivos`)/Todos(value `todos`),
      placed in the top controls area alongside the existing "Nuevo Medio de Pago" button (keep
      that button). Use the Categorías layout (`row mb-3`, select in a `col-md-2`, button in a
      `col text-end`) so styling matches.
      (b) Add fields `private string _estadoFiltro = "activos";` and a
      `private async Task ApplyEstadoFilter() => await LoadMediosPago();`.
      (c) In `LoadMediosPago`, build the url from the base `"api/medios-pago/all"`: append
      `?activo=true` when `_estadoFiltro == "activos"`, `?activo=false` when `== "inactivos"`, and
      no param when `== "todos"` (mirror `Categorias.LoadCategorias`). Since the page is
      admin-only there is NO `_isAdmin` guard. Keep the method's try/catch/finally and the
      `GetFromJsonAsync<List<MedioPagoDto>>` call shape.
      (d) Leave the create/edit `EditForm`, the table columns (Nombre, Estado, Sistema, Acciones),
      the `CambiarEstado` switch (with its `EsSistema`/`_togglingId` disable logic), and the Editar
      button untouched.
      Files: src/SistemaAlmacen.Client/Pages/MediosPago/MediosPago.razor
      Do NOT modify NuevaVenta.razor.
      Verify: `dotnet build src/SistemaAlmacen.Client/SistemaAlmacen.Client.csproj` succeeds with 0 errors.

- [ ] 4. Full-solution verification.
      With the dev server stopped (ask the user first if it is running — do not kill it), run
      `dotnet build SistemaAlmacen.slnx -c Debug` and confirm 0 errors, then
      `dotnet test SistemaAlmacen.slnx` and confirm it is green. If the server cannot be stopped,
      state that and rely on the per-project builds from steps 1–3 as the compile check.
      Files: (none)
      Verify: solution build 0 errors; `dotnet test` passes (placeholder suite).

## Manual acceptance check (optional, once the app is running)

Open `/medios-pago` as Administrador: the page loads showing only **Habilitado** medios by
default (filter = Activos). Switching the select to **Inhabilitados** shows only disabled medios;
**Todos** shows every medio. The enable/disable switch, the Sistema column, and create/edit all
keep working. The sales screen (`NuevaVenta`) still lists only active medios (unaffected).

## Compatibility constraint (restate for the implementer)

`GET api/medios-pago` (→ `GetActivosAsync`) must stay exactly as-is — `NuevaVenta.razor` depends
on it to show only active payment methods. Only `GET api/medios-pago/all` gains the optional
`activo` param, and only `MediosPago.razor` consumes it.

## Verification (implementation iteration 1)

- **Build:** `dotnet build SistemaAlmacen.slnx -c Debug` → `Compilación correcto con 49 advertencias`,
  Exit Code 0. 0 errors; the 49 warnings are pre-existing (NuGet vulnerability advisories + unrelated
  nullable/async warnings), none introduced by this change. The dev server did not lock the output,
  so the full-solution build completed.
- **Tests:** `dotnet test SistemaAlmacen.slnx` → Exit Code 0. All suites green
  (Data.Tests 1/1, Business.Tests 15/15, Integration.Tests 1/1; 0 failed). No tests were added.
- **Full chain wired (param name `activo`, `bool?`, semantics true=solo activos / false=solo
  inhabilitados / null=todos at every layer):**
  1. UI control — `MediosPago.razor`: `<select @bind="_estadoFiltro" @bind:after="ApplyEstadoFilter">`
     with options Activos (`activos`, default) / Inhabilitados (`inactivos`) / Todos (`todos`).
     `_estadoFiltro` defaults to `"activos"`, so first load sends the active-only request.
  2. Client request — `LoadMediosPago()` builds `api/medios-pago/all`, appending `?activo=true` for
     `activos`, `?activo=false` for `inactivos`, no param for `todos`.
  3. Controller endpoint — `MediosPagoController.GetAll([FromQuery] bool? activo)` on
     `GET api/medios-pago/all`, passes `activo` to the service.
  4. Business service — `IMedioPagoService.GetAllAsync(bool? activo = null)` /
     `MedioPagoService.GetAllAsync(bool? activo = null)` filters `.Where(mp => mp.Activo == activo.Value)`
     before the `.OrderBy(mp => mp.Nombre)` projection when `activo.HasValue`.
  5. Enabled flag — filters on the actual entity property `MedioPago.Activo` (bool). The repository
     was not modified (mirrors Categorías, which filters in the service).
- **Preserved:** `GET api/medios-pago` / `GetActivosAsync` untouched (NuevaVenta unaffected); the
  create/edit form, the Estado/Sistema columns, the enable/disable toggle (`CambiarEstado`), and the
  Editar button are all unchanged.

# Implementation Plan — Activo (habilitar/inhabilitar) para Categorías

Mirror the existing Productos "Activo" feature onto Categorías: add an `Activo` flag to the
`Categoria` entity, a PATCH endpoint to toggle it, an optional `activo` query filter on the
listing, a Bootstrap form-switch + estado filter in the Categorías Razor page, and an EF
migration. Each item below leaves the solution compiling.

## Design decisions (made here, grounded in the codebase)

- **Default of `GET api/categorias` stays "todos" (all).** The endpoint gains an OPTIONAL
  `[FromQuery] bool? activo` (null = todos, true = solo activas, false = solo inactivas). The
  Categorías management page explicitly sends `?activo=true` by default; the two Productos
  dropdowns (`Productos.razor` LoadCategorias and `ProductoForm.razor` LoadCategorias) keep
  calling `api/categorias` with NO param so they still receive ALL categorías. This mirrors
  exactly how `Productos.razor` LoadProductos builds its query (adds `activo=true`/`activo=false`,
  omits the param for "todos"). Rationale: changing the server default to active-only would
  silently hide inactive categorías in the product edit dropdowns, which can drop a category
  still assigned to a product being edited. Confirmed by reading all three consumers.
- **Not-found error code = `"NO_ENCONTRADA"`** for `CategoriaService.SetActivoAsync`, matching
  this service's existing convention (`UpdateAsync`/`DeleteAsync` already return `"NO_ENCONTRADA"`).
  The controller maps that code to `NotFound`. (ProductoService uses `"PRODUCTO_NO_ENCONTRADO"`;
  we deliberately keep each service's own convention.)
- **Reuse the existing shared DTO** `SistemaAlmacen.Shared.DTOs.CambiarEstadoRequest` ({ bool Activo }).
  Do not create a new request DTO.
- **Column default = `true`** via EF config `HasDefaultValue(true)` so the migration emits
  `defaultValue: true` and existing rows (and the seed categorías, which don't set `Activo`)
  become active.
- **Filter lives in the service, not the repository.** `GetAllAsync(bool? activo)` filters the
  in-memory `OrderBy` projection, consistent with the current `GetAllAsync` which already pulls
  `GetAllAsync()` from the repo and sorts in the service. No repository change needed.

## Environment constraints (read before building)

- A dev server process (`SistemaAlmacen.Server`) is running and LOCKS the Server project output
  DLLs. A full `dotnet build SistemaAlmacen.slnx` FAILS with file-copy errors (MSB3021/MSB3026/
  MSB3027) that are NOT compile errors — the C# compiles fine. Do NOT kill or restart the server
  unless the user asks. To verify compilation without the lock, build the individual
  non-host projects (`SistemaAlmacen.Data`, `.Business`, `.Shared`, `.Client`). A full-solution
  build or `dotnet run` requires the server to be stopped first by the user.
- Toolchain present: .NET SDK 10.0.402, `dotnet-ef` 10.0.12 (global). Solution file is
  `SistemaAlmacen.slnx`. Test stack: xUnit + FluentAssertions + Moq (tests are placeholders only).

## Steps

- [ ] 1. Add the `Activo` flag to the Categoria entity.
      Add `[Required] public bool Activo { get; set; } = true;` to the entity (mirror Producto.cs).
      Files: src/SistemaAlmacen.Data/Entities/Categoria.cs
      Verify: `dotnet build src/SistemaAlmacen.Data/SistemaAlmacen.Data.csproj` succeeds.

- [ ] 2. Map `Activo` in the EF configuration with a default value of true.
      In `CategoriaConfiguration.Configure`, add `builder.Property(c => c.Activo).IsRequired().HasDefaultValue(true);`
      (mirror ProductoConfiguration's Activo mapping; include the default so the migration emits
      `defaultValue: true`).
      Files: src/SistemaAlmacen.Data/Configurations/CategoriaConfiguration.cs
      Verify: `dotnet build src/SistemaAlmacen.Data/SistemaAlmacen.Data.csproj` succeeds.

- [ ] 3. Generate the EF migration for the new column.
      Run from the repo root:
      `dotnet ef migrations add AddActivoToCategoria --project src/SistemaAlmacen.Data --startup-project src/SistemaAlmacen.Server`
      Then open the generated `*_AddActivoToCategoria.cs` and confirm the Up adds the `Activo`
      bit column to `Categorias` with `defaultValue: true`. The app applies it on startup via
      `DataSeeder.SeedAsync` -> `context.Database.MigrateAsync()`.
      Files: src/SistemaAlmacen.Data/Migrations/*_AddActivoToCategoria.cs (+ .Designer.cs + snapshot update)
      Note: this builds the startup project; if the running server locks it, the coder may need
      the server stopped first (do not kill it without the user). If `dotnet ef` genuinely cannot
      run, STOP and report rather than hand-writing a migration that may not match the snapshot.
      Verify: the migration file exists with `defaultValue: true` and
      `dotnet build src/SistemaAlmacen.Data/SistemaAlmacen.Data.csproj` succeeds.

- [ ] 4. Add `Activo` to the response DTO and map it.
      Add `public bool Activo { get; set; }` to CategoriaDto; set `Activo = categoria.Activo`
      in `CategoriaService.MapToDto`.
      Files: src/SistemaAlmacen.Shared/DTOs/Categorias/CategoriaDto.cs,
      src/SistemaAlmacen.Business/Services/CategoriaService.cs
      Verify: `dotnet build src/SistemaAlmacen.Business/SistemaAlmacen.Business.csproj` succeeds.

- [ ] 5. Extend the service interface and implementation.
      In ICategoriaService: change `GetAllAsync()` to `Task<List<CategoriaDto>> GetAllAsync(bool? activo = null)`
      and add `Task<Result> SetActivoAsync(int id, bool activo)`. In CategoriaService: filter
      `GetAllAsync` by `activo` when non-null (true=only active, false=only inactive, null=all);
      implement `SetActivoAsync` mirroring ProductoService.SetActivoAsync (fetch by id incl.
      inactive via `GetByIdAsync`, no-op `Result.Success()` if already in that state, set flag,
      `Update`, `SaveChangesAsync`, return `Result.Success()`; not-found returns
      `Result.Failure("La categoría no fue encontrada.", "NO_ENCONTRADA")`). Also set
      `Activo = true` on the entity created in `CreateAsync`.
      Files: src/SistemaAlmacen.Business/Interfaces/ICategoriaService.cs,
      src/SistemaAlmacen.Business/Services/CategoriaService.cs
      Verify: `dotnet build src/SistemaAlmacen.Business/SistemaAlmacen.Business.csproj` succeeds.

- [ ] 6. Update the controller: optional filter on GetAll + PATCH estado endpoint.
      (a) Add `[FromQuery] bool? activo` to `GetAll` and pass it to `GetAllAsync(activo)`.
      (b) Add `[HttpPatch("{id:int}/estado")]` with `[Authorize(Policy = "RequireAdmin")]`
      taking `[FromBody] SistemaAlmacen.Shared.DTOs.CambiarEstadoRequest request`, calling
      `SetActivoAsync(id, request.Activo)`, returning `NotFound` when `ErrorCode == "NO_ENCONTRADA"`,
      `BadRequest` on other failures, and `Ok(new { message = ... })` with a habilitada/inhabilitada
      message on success. Mirror ProductosController.SetEstado.
      Files: src/SistemaAlmacen.Server/Controllers/CategoriasController.cs
      Verify: `dotnet build src/SistemaAlmacen.Server/SistemaAlmacen.Server.csproj` succeeds
      (requires the running server to be stopped first; the compile itself is the check).

- [ ] 7. Add the switch + estado filter to the Categorías Razor page.
      Mirror Productos.razor: add an "Estado" column header and cell shown only when `_isAdmin`,
      containing a `form-check form-switch` checkbox bound to `categoria.Activo`, disabled while
      `_togglingId == categoria.Id`, `@onchange` calling `CambiarEstado(categoria, (bool)e.Value!)`,
      with a Habilitado/Inhabilitado label (`text-success`/`text-muted`). Add a `private int? _togglingId;`
      field and a `CambiarEstado` method (PATCH `api/categorias/{id}/estado` with
      `new CambiarEstadoRequest { Activo = nuevoEstado }`, success/error via `_successMessage`/
      `_errorMessage` + `ApiErrorParser`, reload, clear `_togglingId` in finally). Add an estado
      filter `<select>` (only `_isAdmin`) Activos(default)|Inhabilitados|Todos with
      `_estadoFiltro = "activos"` reloading on change. Update `LoadCategorias` to build the url:
      `api/categorias` + `?activo=true` (activos) / `?activo=false` (inhabilitados) / no param (todos),
      only appending the param when `_isAdmin`. Keep the inline create/edit rows and the "Nueva
      Categoría" button working; ensure the create/edit rows span the new column count. Non-admin
      behavior unchanged.
      Files: src/SistemaAlmacen.Client/Pages/Categorias/Categorias.razor
      Do NOT modify Productos.razor or ProductoForm.razor.
      Verify: `dotnet build src/SistemaAlmacen.Client/SistemaAlmacen.Client.csproj` succeeds.

- [ ] 8. Full-solution verification.
      With the dev server stopped, run `dotnet build SistemaAlmacen.slnx -c Debug` and confirm it
      succeeds with 0 errors, then `dotnet test SistemaAlmacen.slnx` and confirm all tests pass.
      Files: (none)
      Verify: solution build 0 errors; `dotnet test` green.

## Compatibility constraint (restate for the implementer)

`GET api/categorias` must keep returning ALL categorías when no `activo` param is sent. Only the
Categorías management page sends `?activo=true`. Do NOT touch `Productos.razor` or
`ProductoForm.razor` — their `LoadCategorias` must keep calling `api/categorias` with no param so
inactive categorías still appear in the product category dropdowns.

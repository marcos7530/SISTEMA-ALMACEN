# Active-only estado filter for Gestión de Medios de Pago

The Medios de Pago admin page gains an estado `<select>` (Activos | Inhabilitados | Todos) that defaults to **Activos**, so the page now opens showing only habilitado payment methods — the behavior the user asked for. The filter rides an optional `?activo=` query param threaded through the existing admin listing endpoint `GET api/medios-pago/all`, mirroring the Categorías pattern end to end: same param name, same `bool?` semantics, same service-level filtering, same UI control and Spanish labels. The entity's real `Activo` flag drives the filter — no invented field, no DTO, no migration, no repository change. The Vendedor-facing `GET api/medios-pago` (`GetActivosAsync`) is left untouched, so the sales screen is unaffected.

Watch for: nothing blocking. One behavioral note (confirmed) — unlike Categorías, this page has no `_isAdmin` guard around the filter, which is correct here because the page is `[Authorize(Roles = "Administrador")]` and has no non-admin audience.

**Verdict**: APPROVED

## High-level view

The change closes the one real gap: the admin page previously called `/all` unconditionally and had no way to filter by estado. Now the `<select>` drives `_estadoFiltro` which appends `?activo=true|false` (or nothing for "todos") to the same `/all` request, and the controller/service honor the param. The param name, type (`bool?`), and tri-state semantics (true = solo activos, false = solo inhabilitados, null/omitted = todos) match `CategoriasController.GetAll` / `CategoriaService.GetAllAsync` verbatim.

Filtering happens in the service via `.Where(mp => mp.Activo == activo.Value)` before the `OrderBy(Nombre)` projection — identical shape to `CategoriaService`, and the repository is deliberately left alone in both. This is the correct reading of "replicate the Categorías pattern": Categorías also does not touch its repository for this filter.

The filter binds on `MedioPago.Activo`, the actual bool property on the entity (default `true`). There is no "Activo" invented on top of a differently-named field; the "Habilitado"/"Inhabilitado" UI text is just display for that bool.

The default selection is "activos", so first load sends `?activo=true` and the page opens active-only — the explicit UX the user requested. The server default stays "todos" (param omitted), same as Categorías.

Existing behavior is intact: the create/edit `EditForm`, the Sistema column, the enable/disable `CambiarEstado` switch (with its `EsSistema` and `_togglingId` disable logic), and the Editar button are all unchanged. `GET api/medios-pago` / `GetActivosAsync` and `NuevaVenta.razor` are untouched.

Build evidence is recorded in the plan's verification note: full-solution `dotnet build SistemaAlmacen.slnx -c Debug` returned exit 0 with 0 errors (49 pre-existing warnings), and `dotnet test SistemaAlmacen.slnx` returned exit 0 with all suites green. Not re-run per instructions.

<details>
<summary>Issues (0)</summary>

No blocking or non-blocking action items. The change is a faithful replica of the Categorías pattern with build and test evidence recorded.

</details>

<details>
<summary>Details</summary>

### Query param threading, from select to Where clause

The chain is wired through without a break. `MediosPago.razor` holds `_estadoFiltro = "activos"` bound to the `<select>` via `@bind` + `@bind:after="ApplyEstadoFilter"`; `ApplyEstadoFilter` just re-calls `LoadMediosPago`. `LoadMediosPago` builds the URL from the base `"api/medios-pago/all"`, appending `?activo=true` for `activos`, `?activo=false` for `inactivos`, and nothing for `todos`. `MediosPagoController.GetAll` now takes `[FromQuery] bool? activo` and forwards it to `_medioPagoService.GetAllAsync(activo)`. The service filters with `.Where(mp => mp.Activo == activo.Value)` when `activo.HasValue`, before the existing `OrderBy(mp => mp.Nombre).Select(MapToDto)` projection.

```
<select> _estadoFiltro ──▶ LoadMediosPago builds ?activo=
                                   │
                        GET api/medios-pago/all?activo=true|false
                                   │
         MediosPagoController.GetAll([FromQuery] bool? activo)
                                   │
         MedioPagoService.GetAllAsync(bool? activo = null)
                                   │
         .Where(mp => mp.Activo == activo.Value)   // only when HasValue
```

The interface signature changed from `GetAllAsync()` to `GetAllAsync(bool? activo = null)`; the controller is the only caller, and it was updated — no dangling call site.

### Fidelity to the Categorías convention

Set side by side with `Categorias.razor` / `CategoriaService.GetAllAsync` / `CategoriasController.GetAll`, the adaptation is exact where it matters: the param is named `activo`, typed `bool?`, with true = solo activos / false = solo inhabilitados / null = todos; the `<select>` options and Spanish labels (Activos / Inhabilitados / Todos) and their value tokens (`activos` / `inactivos` / `todos`) are identical; the default is `"activos"`; and the filtering lives in the service with the repository untouched. The URL-building branch in `LoadMediosPago` is the same conditional structure as `LoadCategorias`.

The one intentional divergence: Categorías wraps its filter and query-param logic in an `if (_isAdmin)` guard because that page serves both admins and non-admins. MediosPago is `[Authorize(Roles = "Administrador")]` — admin-only — so the guard is dropped and the select/param are always present. This is the right adaptation, not a deviation from the pattern.

### Enabled flag is the real entity property

Confirmed by reading `MedioPago.cs`: `Activo` is a real `bool` property (default `true`). No "Activo" was invented over a differently-named column — the specific risk called out for review is not present.

### Preserved behavior

The diff leaves the create/edit `EditForm`, the Sistema column, the enable/disable switch (`CambiarEstado` → `PATCH api/medios-pago/{id}/estado`, disabled when `mp.EsSistema || _togglingId == mp.Id`), and the Editar button unchanged. `GET api/medios-pago` / `GetActivosAsync` and the PATCH endpoint were not modified, so `NuevaVenta.razor`'s active-only payment selector keeps working.

### Build and test evidence

The plan's verification note records a full-solution `dotnet build SistemaAlmacen.slnx -c Debug` at exit 0 with 0 errors (49 pre-existing warnings), and `dotnet test SistemaAlmacen.slnx` at exit 0 with Data.Tests 1/1, Business.Tests 15/15, Integration.Tests 1/1 green. Evidence is present and not re-run per the review constraints. If this note were absent the change would be rejected pending a build; it is present.

</details>

<details>
<summary>File map</summary>

- `src/SistemaAlmacen.Business/Interfaces/IMedioPagoService.cs` — `GetAllAsync()` → `GetAllAsync(bool? activo = null)` with updated XML doc.
- `src/SistemaAlmacen.Business/Services/MedioPagoService.cs` — added `.Where(mp => mp.Activo == activo.Value)` when `activo.HasValue`, before the existing sort/projection.
- `src/SistemaAlmacen.Server/Controllers/MediosPagoController.cs` — `GetAll` takes `[FromQuery] bool? activo` and forwards it; XML doc updated.
- `src/SistemaAlmacen.Client/Pages/MediosPago/MediosPago.razor` — added estado `<select>` in a `row mb-3` layout, `_estadoFiltro` field, `ApplyEstadoFilter`, and `?activo=` URL building in `LoadMediosPago`.
- `.agents/tasks/mediospago-activo/plan.md` — implementation plan and verification note (build/test evidence).

Full diff: `git show 92f388f -- <source paths>` (the full `git diff main` is dominated by regenerated compiled assemblies and is not review-relevant).

</details>

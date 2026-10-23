# Implementation Plan — Gráficos y métricas de producto en el Dashboard (Blazor WASM + ApexCharts)

## Context discovered during exploration

- Solution file is `SistemaAlmacen.slnx` at repo root. Build: `dotnet build SistemaAlmacen.slnx`. Tests: `dotnet test SistemaAlmacen.slnx` (xUnit; no client-side UI tests exist, so verification is build + manual run).
- Client project: `src/SistemaAlmacen.Client/SistemaAlmacen.Client.csproj` (`net10.0`, `Microsoft.NET.Sdk.BlazorWebAssembly`, nullable + implicit usings enabled). Only package refs today are the WASM runtime, WASM auth, DevServer, and `System.IdentityModel.Tokens.Jwt`. Project references `SistemaAlmacen.Shared`, so the real `SistemaAlmacen.Shared.DTOs.Reportes` DTOs are available to the client.
- `_Imports.razor` already has `@using SistemaAlmacen.Shared.DTOs.Reportes`, so the page can use the real DTOs (`ReporteVentasDto`, `DesgloseDiarioDto`, `DesgloseMedioPagoDto`, `ProductoMasVendidoDto`) instead of redeclaring local classes.
- `Dashboard.razor` currently declares private local `DashboardData`/`ProductoItem` classes and loads only `/api/reportes/dashboard`. We keep that pattern but add two more calls. Existing loading/error pattern: `_isLoading` bool, `_errorMessage` string, `OnInitializedAsync` → `Cargar()`, try/catch sets a generic Spanish error. Preserve the `Actualizar` refresh button that calls `Cargar()`.
- `index.html` loads `_framework/blazor.webassembly.js` and then immediately registers the service worker in an inline `<script>`. The two ApexCharts scripts must go AFTER `blazor.webassembly.js` and BEFORE that service-worker `<script>` line.
- `Program.cs` builds the WASM host; add `builder.Services.AddApexCharts();` before `await builder.Build().RunAsync();` (needs `using ApexCharts;` at top, or rely on the `@using` only for Razor — Program.cs is C#, so add the `using` there explicitly).
- `app.css` defines the palette CSS variables and `.card/.card-body/.card-header/.dashboard-card`. Reuse these; do NOT add global overrides. LAYOUT HAZARD: the project recently had double-CSS-definition bugs between `app.css` and component-scoped `.razor.css`; keep any new styles scoped in a new `Dashboard.razor.css` or inline, additive only.
- PWA SERVICE WORKER: `wwwroot/service-worker.published.js` precaches hashed assets from `service-worker-assets.js` and is cache-first for static assets. The new `_content/Blazor-ApexCharts/*` JS are static web assets and will be picked up into the published manifest automatically at build/publish time — no manual service-worker edit needed. In dev (`service-worker.js`), nothing special is required. (Verification note below covers the "integrity checks failed" hazard.)
- GIT HAZARD (observed): `bin/` and `obj/` are currently TRACKED in git for all projects (git status shows many `src/**/bin` and `src/**/obj` files as modified). Do not fight this; just be aware a build will churn these files. Stage only the intended source files plus, if the repo convention requires it, the regenerated artifacts — but prefer staging source files explicitly (`.csproj`, `index.html`, `Program.cs`, `_Imports.razor`, `Dashboard.razor`, any new `.razor.css`). Do NOT `git add -A`.

### Package / static-asset decision (verified)

- Package: `Blazor-ApexCharts` version `7.0.0`. Confirmed via the official docs and the apexcharts/Blazor-ApexCharts GitHub README that the setup is the two script tags below plus `AddApexCharts()`. v7.0.0 vendors ApexCharts.js core 6.5.0 and supports WASM on .NET 8/9/10 with no npm/CDN. ([setup docs](https://apexcharts.com/docs/blazor-charts/), [GitHub README](https://github.com/apexcharts/Blazor-ApexCharts)). Content was rephrased for compliance with licensing restrictions.
- Verified script asset paths (from the package README):
  - `_content/Blazor-ApexCharts/js/apex-charts.min.js`
  - `_content/Blazor-ApexCharts/js/blazor-apex-charts.js`
- The package is NOT yet in the local NuGet cache (searched; not present), so the implementer MUST re-verify these paths after `dotnet restore` against `src/SistemaAlmacen.Client/obj/project.assets.json` or the restored `_content/Blazor-ApexCharts/` static web assets. If v7 ships different filenames or a single combined script, use whatever it actually ships (step 2 covers this).

### Date-range math (decide once, implement in C#)

- "Hoy" = `DateTime.Today` (local; culture is es-AR, set in Program.cs). All ranges use whole dates (the endpoint takes `DateTime` query params; send `yyyy-MM-dd`).
- Chart 1 (últimos 30 días): `fechaFin = DateTime.Today`, `fechaInicio = DateTime.Today.AddDays(-29)` → exactly 30 inclusive days, well under the 365-day service limit.
- Charts 2 & 3 + all product metrics (mes actual): `fechaInicio = new DateTime(today.Year, today.Month, 1)`, `fechaFin = DateTime.Today`. Also under 365 days.
- Send dates as `.ToString("yyyy-MM-dd")` in the query string to avoid culture/locale ambiguity on the wire (es-AR default formatting would otherwise produce `dd/MM/yyyy`). Example: `api/reportes/ventas?fechaInicio=2025-01-01&fechaFin=2025-01-30`.
- 365-day guard: our ranges (30 days, ≤31 days) never trip `GenerarReporteVentasAsync`'s `ArgumentException`. Still wrap each call in the existing try/catch so a 400 degrades gracefully to the error alert rather than a crash.

### Zero-fill approach (Chart 1)

- The `/api/reportes/ventas` `DesgloseDiario` only contains days that had sales. For a continuous, readable trend line, build the full 30-day axis client-side:
  1. Create `var dias = Enumerable.Range(0, 30).Select(i => DateTime.Today.AddDays(-29 + i).Date).ToList();` (ascending).
  2. Index the response: `var porFecha = resp.DesgloseDiario.ToDictionary(d => d.Fecha.Date, d => d.Monto);`.
  3. For each day in `dias`, Y = `porFecha.TryGetValue(dia, out var m) ? m : 0m`. X label = `dia.ToString("dd/MM")`.
- This yields 30 points every time, so the line/axis is continuous even with sparse sales.

## Plan items

- [ ] 1. Add the `Blazor-ApexCharts` 7.0.0 package reference and restore; verify the shipped static-asset script paths.
      Add `<PackageReference Include="Blazor-ApexCharts" Version="7.0.0" />` to the existing first `<ItemGroup>` in `src/SistemaAlmacen.Client/SistemaAlmacen.Client.csproj`. Then restore and inspect what `_content` assets the package actually ships, to confirm the two script filenames before wiring `index.html` in step 2.
      Files: `src/SistemaAlmacen.Client/SistemaAlmacen.Client.csproj`
      Verify: `dotnet restore SistemaAlmacen.slnx` succeeds; then confirm `src/SistemaAlmacen.Client/obj/project.assets.json` lists `Blazor-ApexCharts/7.0.0`, and the restored package's `staticwebassets` contains `js/apex-charts.min.js` and `js/blazor-apex-charts.js` (check `~/.nuget/packages/blazor-apexcharts/7.0.0/staticwebassets/js/`). Record the exact filenames in step 2 if they differ.

- [ ] 2. Add the two ApexCharts script tags to `index.html` in the correct position.
      Insert, immediately AFTER the `<script src="_framework/blazor.webassembly.js"></script>` line and BEFORE the inline `navigator.serviceWorker.register(...)` script:
      `<script src="_content/Blazor-ApexCharts/js/apex-charts.min.js"></script>` and `<script src="_content/Blazor-ApexCharts/js/blazor-apex-charts.js"></script>`. If step 1 revealed different shipped paths/a single combined script, use those instead.
      Files: `src/SistemaAlmacen.Client/wwwroot/index.html`
      Verify: build the client (`dotnet build src/SistemaAlmacen.Client/SistemaAlmacen.Client.csproj`) succeeds; confirm the two `_content/Blazor-ApexCharts/js/*` entries appear in the generated `src/SistemaAlmacen.Client/bin/Debug/net10.0/wwwroot/service-worker-assets.js` (proves the assets are staged into the WASM output).

- [ ] 3. Register the ApexCharts service in `Program.cs`.
      Add `using ApexCharts;` to the usings block and `builder.Services.AddApexCharts();` after the other `builder.Services.Add...` registrations and before `await builder.Build().RunAsync();`.
      Files: `src/SistemaAlmacen.Client/Program.cs`
      Verify: `dotnet build src/SistemaAlmacen.Client/SistemaAlmacen.Client.csproj` compiles with no errors.

- [ ] 4. Add `@using ApexCharts` to `_Imports.razor`.
      Append `@using ApexCharts` so all `<ApexChart>` components resolve in Razor without per-page usings.
      Files: `src/SistemaAlmacen.Client/_Imports.razor`
      Verify: `dotnet build src/SistemaAlmacen.Client/SistemaAlmacen.Client.csproj` compiles (full verification happens in step 7 once the page uses the components).

- [ ] 5. Extend the Dashboard code-behind to load the two extra reports and compute product metrics.
      In `Dashboard.razor`'s `@code` block, keep the existing `DashboardData`/`ProductoItem` local classes and the `/api/reportes/dashboard` load. Add fields and loader logic (all inside the existing `Cargar()` so the `Actualizar` button refreshes everything, and all inside the existing try/catch):
      - Add `_ventas30` (the `ReporteVentasDto` for the last 30 days), `_ventasMes` (the `ReporteVentasDto` for the current month), and `_topProductosMes` (`List<ProductoMasVendidoDto>` from `productos-mas-vendidos` for the current month).
      - Compute date ranges per the "Date-range math" section; call `api/reportes/ventas?fechaInicio=...&fechaFin=...` twice (30-day and month) and `api/reportes/productos-mas-vendidos?fechaInicio=...&fechaFin=...` once, using `GetFromJsonAsync`. Use `yyyy-MM-dd` formatting for the query values.
      - Build the zero-filled 30-day series (`_serieVentasDias`: list of `{ string Etiqueta; decimal Monto; }`) per the "Zero-fill approach" section.
      - Product metrics computed from `_topProductosMes` (add an explicit code comment that `productos-mas-vendidos` returns up to 50 items ordered desc, and that all category aggregation here is by UNITS sold, not revenue, because `ProductoMasVendidoDto` has no monetary field):
        - `_productoMasVendido` = `_topProductosMes.FirstOrDefault()`.
        - `_productoMenosVendido` = `_topProductosMes.LastOrDefault()`.
        - `_ventasPorCategoria` = group by `Categoria`, sum `CantidadVendida`, order desc, as a `List<{ string Categoria; int Unidades; }>` (used for the category chart/list; take top N for display).
        - `_categoriasDistintas` = `_topProductosMes.Select(p => p.Categoria).Distinct().Count()`.
      - If any secondary call fails, degrade gracefully: catch and leave that section empty (null/empty collection) rather than blanking the whole dashboard; the primary `/dashboard` metric cards and the existing error alert pattern remain intact.
      Files: `src/SistemaAlmacen.Client/Pages/Dashboard/Dashboard.razor`
      Verify: `dotnet build src/SistemaAlmacen.Client/SistemaAlmacen.Client.csproj` compiles (markup rendering verified in steps 6–7).

- [ ] 6. Add the product-metric cards and the "Ventas por categoría" summary to the Dashboard markup.
      Below the existing inventario row (and keeping all existing metric cards and the Top-5 table), add a Bootstrap `row g-3 mb-2` with compact `.card` cards reusing existing classes:
      - "Producto más vendido del mes" (name + units, or "Sin ventas este mes" when null).
      - "Producto menos vendido del mes" (name + units, or placeholder when null).
      - "Categorías con ventas en el mes" (`_categoriasDistintas`).
      - "Ventas por categoría (unidades)" rendered as a compact list of top categories by units (this is the small-list option for category breakdown; the bar chart variant is optional and may reuse Chart config patterns from step 7). Keep an empty-state message when there are no sales.
      Use the project palette via existing CSS variables/classes only; no global style overrides. Any new rules go in a scoped `Dashboard.razor.css` (create if needed), additive only.
      Files: `src/SistemaAlmacen.Client/Pages/Dashboard/Dashboard.razor` (and optionally new `src/SistemaAlmacen.Client/Pages/Dashboard/Dashboard.razor.css`)
      Verify: `dotnet build src/SistemaAlmacen.Client/SistemaAlmacen.Client.csproj` compiles; manual run (step 8) shows the cards populated.

- [ ] 7. Add the three ApexCharts charts to the Dashboard markup.
      Lay the charts in Bootstrap `row g-3` / `col-md-*` columns, each inside a `.card`/`.card-body` with a `.card-header` title. Charts width 100% (ApexChart `Width="100%"` / responsive). Use the project palette (`--primary #1a237e`, `--primary-light #3949ab`, `--accent #00bfa5`, `--success #10b981`, `--warning #f59e0b`, `--danger #ef4444`) for `Colors`/`SeriesColors`. Only render each `<ApexChart>` when its data is present; otherwise show the empty-state / table fallback.
      - Chart 1 — "Ventas últimos 30 días" (line or bar): bind to `_serieVentasDias`, X = `Etiqueta` (dd/MM), Y = `Monto`. Full 30-point zero-filled series.
      - Chart 2 — "Ventas por medio de pago" (donut/pie): bind to `_ventasMes.DesgloseMediosPago`, label = `MedioPago`, value = `Total`. Empty-state message when the list is empty.
      - Chart 3 — "Top productos del mes" (horizontal bar, `PlotOptions` bar `Horizontal = true`): bind to `_topProductosMes.Take(10)`, X/value = `CantidadVendida`, Y/category = `Nombre`. KEEP the existing Top-5 table as the fallback shown when `_topProductosMes` is empty (the existing table already handles the empty case).
      Follow the Blazor-ApexCharts component API (`<ApexChart TItem="...">` + `<ApexPointSeries>` / `<ApexPieSeries>`); confirm exact generic/series syntax for v7 against the restored package or [docs](https://apexcharts.com/docs/blazor-charts/) while implementing.
      Files: `src/SistemaAlmacen.Client/Pages/Dashboard/Dashboard.razor`
      Verify: `dotnet build src/SistemaAlmacen.Client/SistemaAlmacen.Client.csproj` compiles; full manual verification in step 8.

- [ ] 8. Build the whole solution and manually verify the Dashboard renders — honoring the build-while-running hazard.
      BEFORE building: check `Get-Process -Name SistemaAlmacen.Server -ErrorAction SilentlyContinue`. If it is running, `Stop-Process -Id <id> -Force` for THAT process only. Do NOT kill generic `dotnet` processes (IDE/build servers). Rebuilding the WASM client while the host runs corrupts the SRI integrity manifest and causes "integrity checks failed"/404 on `.wasm`/`.pdb`.
      Then: `dotnet build SistemaAlmacen.slnx` (expect 0 errors). Optionally run `dotnet run --project src/SistemaAlmacen.Server`, log in as `admin@sistema.local` / `Admin123!`, open `/dashboard`, and confirm: existing metric cards still render, the three charts draw, product-metric cards populate, `Actualizar` reloads all data, and empty-data states show placeholders (no console errors, no integrity failures). If the browser shows a stale/integrity error, clear site data / unregister the service worker (per README troubleshooting) — do not treat a cached SW as a build failure.
      Files: none (verification only)
      Verify: `dotnet build SistemaAlmacen.slnx` reports 0 errors; manual dashboard check passes as above.

## Notes / assumptions

- No automated UI tests exist for the client; `dotnet test SistemaAlmacen.slnx` still must stay green (it exercises Business/Data/Integration, which this change does not touch). Run it once at the end as a regression guard.
- Prefer using the real `SistemaAlmacen.Shared.DTOs.Reportes` DTOs for the two new endpoints (already imported) instead of adding more local classes; the existing local `DashboardData`/`ProductoItem` can stay as-is to minimize churn, or be swapped for `DashboardDto` — implementer's choice, but do not change backend DTOs.
- No backend, DTO, or controller changes are allowed and none are needed. No margin/profitability metrics (deferred per request).
- Category breakdown and the "menos vendido" metric are explicitly by UNITS sold, limited by the endpoint's top-50-desc contract — annotate this in code so a later reader does not mistake it for revenue or a true global minimum.

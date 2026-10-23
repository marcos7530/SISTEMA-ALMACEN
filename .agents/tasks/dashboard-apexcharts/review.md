# Dashboard ApexCharts: charts, product metrics, and v7 package wiring (review pass v3)

Adds Blazor-ApexCharts v7.0.0 to the WASM client and extends the admin Dashboard with three charts (ventas últimos 30 días as a smooth area series, ventas por medio de pago as a donut, top productos del mes as a horizontal bar), three product metric cards (más vendido, menos vendido, categorías distintas), and a "ventas por categoría (unidades)" list. The three new reads reuse the existing `/api/reportes/*` endpoints and the real `SistemaAlmacen.Shared.DTOs.Reportes` DTOs, with date ranges computed client-side and a zero-filled 30-day axis. Existing metric cards, the Top-5 table, the `Actualizar` refresh, and the loading/error pattern are preserved.

This is the third review pass. The two blocking findings from pass v2 were both process-level ("no verification evidence on record" and "script-tag deviation unconfirmed at runtime"). The coder has now added `verification.md` to the task directory recording the build results, the static-asset path check against the restored v7.0.0 package, and runtime HTTP probes. That evidence closes both prior blockers.

Watch for: (1) No automated browser/DOM render check exists — chart drawing is confirmed indirectly (client compiles against the v7 API, self-loaded JS/CSS serve 200, backing endpoints return data), not by a visual confirmation (confirmed, acceptable — no UI test harness exists for the WASM client). (2) The diff still bundles `MainLayout.razor.css` / `NavMenu.razor.css` edits from the earlier top-bar/icon-alignment work; non-blocking, flagged for commit hygiene (confirmed).

**Verdict**: APPROVED

## High-level view

The gating issue from pass v2 is resolved. The coder recorded a verification note (`verification.md`) that supplies exactly what the brief asked a reviewer to read: both client and server builds at 0 errors, the restored v7.0.0 static-asset filenames, asset staging into `service-worker-assets.js`, and a live server run with HTTP 200 probes on the ApexCharts JS/CSS and the backing API endpoints. Per the brief I read that evidence rather than re-running any build or the dev server myself.

Package wiring verifies cleanly and the v7 self-loading model is handled correctly. The csproj references `Blazor-ApexCharts 7.0.0`, `Program.cs` calls `AddApexCharts()` with `using ApexCharts;`, `_Imports.razor` adds `@using ApexCharts`, and `index.html` carries no ApexCharts `<script>` tags — which I confirmed directly. v7 self-loads its ESM module via the component, so the plan's two guessed script paths (`apex-charts.min.js`, `blazor-apex-charts.js`, which don't exist in the package) were correctly omitted; the restored package actually ships `js/blazor-apexcharts.js`, `js/apexcharts.esm.js`, and `css/apexcharts.css`, all of which serve 200 at runtime.

Chart and metric logic matches the spec. The 30-day chart uses `today-29..today`; medios de pago and top productos use first-of-month..today; both ranges sit far under the 365-day endpoint guard. The 30-day series is zero-filled to exactly 30 points for a continuous axis. Category aggregation is by units sold (`CantidadVendida`) with an explicit code comment explaining there is no per-product monetary field, and no margin/profit metric was introduced. Every DTO field the markup and code touch exists in the shared DTOs with the expected type.

Preserved behavior and failure modes are intact. The six existing metric cards, the Top-5 table with its own empty state, the `Actualizar` refresh, and the `_isLoading`/`_errorMessage` pattern all remain. Each secondary fetch degrades independently so one failing endpoint leaves one empty chart rather than blanking the dashboard, and every chart/list is guarded by a `.Count > 0` empty-state check.

One scope note: the working tree still bundles `MainLayout.razor.css` and `NavMenu.razor.css` edits that belong to the earlier top-bar/icon-alignment work, not this charts task. The charts task itself added no CSS, so there is no double-definition conflict introduced here.

<details>
<summary>Issues (2)</summary>

1. **No visual/DOM render confirmation (non-blocking)** — Chart drawing is verified indirectly (v7 API compiles, self-loaded assets serve 200, endpoints return data) because no UI test harness exists for the WASM client. Acceptable for this task; a human eyeball on `/dashboard` would be the only remaining confirmation.
2. **Unrelated CSS bundled in the diff (non-blocking)** — `MainLayout.razor.css` and `NavMenu.razor.css` changes belong to the earlier top-bar/alignment task. No action for this task; keep commit scope deliberate when staging.

</details>

<details>
<summary>Details</summary>

### Verification evidence now on record (closes v2 blockers #1 and #2)

`verification.md` (iteration v3) in the task directory records the three things the brief told the reviewer to read. Build: `dotnet build` on both the client and the server csproj returned EXIT 0 / "Compilación correcta. 0 Errores", which proves the markup compiles against the actually-restored v7 component/series API (`ApexChartOptions<T>`, `ApexChart`, `ApexPointSeries`, `SeriesType.Area/Donut/Bar`, `PlotOptionsBar { Horizontal = true }`, `Curve.Smooth`, `LegendPosition.Bottom`). Static-asset check: the restored `~/.nuget/packages/blazor-apexcharts/7.0.0/staticwebassets/` ships `js/apexcharts.esm.js`, `js/blazor-apexcharts.js`, and `css/apexcharts.css` — not the plan's guessed `apex-charts.min.js` / `blazor-apex-charts.js` — and those three assets are listed in the generated `service-worker-assets.js`. Runtime: a live `dotnet run` served `/` at 200 with no integrity error, and the three self-loaded ApexCharts assets plus the backing API endpoints (`/api/reportes/ventas`, `/productos-mas-vendidos`, `/dashboard`) all returned 200 with seeded data. Per the brief, I did not re-run these builds or the dev server; I read the recorded evidence and spot-checked the two static facts below. The one residual gap the note calls out — no browser/DOM render check, because there is no UI test harness for the client — is acceptable here, as compile-against-v7-API plus 200 on the self-loaded JS/CSS is the strongest signal available headless.

### Package wiring and the v7 self-loading model

Four wiring points verify. The csproj carries `<PackageReference Include="Blazor-ApexCharts" Version="7.0.0" />`; `Program.cs` has `using ApexCharts;` and `builder.Services.AddApexCharts();` before `RunAsync()`; `_Imports.razor` adds `@using ApexCharts`. The fourth — `index.html` — I grepped directly and it contains no `apexcharts`/`apex-charts` reference at all, which is the correct v7 wiring: the `ApexChart` component imports its ESM module on first render, so manual script tags would have 404'd on the plan's guessed filenames. The runtime probes in the note confirm the self-load path resolves 200 in this app's PWA/service-worker setup.

### Date-range math and the 30-day zero-fill

```
hoy       = DateTime.Today
inicio30  = hoy.AddDays(-29)                       -> 30 inclusive days  (chart 1)
inicioMes = new DateTime(hoy.Year, hoy.Month, 1) .. hoy   (<= 31 days, charts 2/3 + metrics)
```

Both ranges are far under the 365-day endpoint guard. Dates go on the wire as `yyyy-MM-dd`, sidestepping the es-AR `dd/MM/yyyy` default. `ConstruirSerie30Dias` indexes `DesgloseDiario` into a date→monto dictionary and emits exactly 30 points (`inicio.AddDays(i)` for `i` 0..29), filling absent days with `0m` — the runtime note shows 4 days with sales expanding to a 30-point continuous axis, exercising this path with real sparse data.

### Spec conformance of charts and metrics

Three charts as specified: area series on the zero-filled `_serieVentasDias`; donut on `_mediosPago` (label `MedioPago`, value `Total`); horizontal bar on `_topProductosMes.Take(10)` (category `Nombre`, value `CantidadVendida`). Three product metric cards plus the units breakdown: `_productoMasVendido` (First), `_productoMenosVendido` (Last), `_categoriasDistintas`, and the `_ventasPorCategoria` list. `CalcularMetricasProducto` carries an explicit comment that category/product aggregation is by units sold, not revenue, because `ProductoMasVendidoDto` has no monetary field — which I confirmed: that DTO has only `Nombre`, `Categoria`, `CantidadVendida`. No margin or profitability metric was added, matching the deferral in the user's request. Every referenced DTO member (`DesgloseDiario`/`Fecha`/`Monto`, `DesgloseMediosPago`/`MedioPago`/`Total`, `Nombre`/`Categoria`/`CantidadVendida`) exists in the shared DTOs.

### Preserved behavior and failure modes

The six existing metric cards, the Top-5 table (still reading `_data.TopProductosMes` with its own empty state), the `Actualizar` button wired to `Cargar()`, and the `_isLoading`/`_errorMessage` pattern are intact. `CargarGraficos` wraps each secondary fetch in its own try/catch that resets that section's collection to empty, so one failing endpoint degrades to a single empty chart; the primary `/dashboard` call keeps the outer try/catch driving the error alert. Every chart and list is guarded by a `.Count > 0` check with a Spanish empty-state message.

### Scope: unrelated CSS in the diff

`MainLayout.razor.css` and `NavMenu.razor.css` are modified in the same working tree; those edits belong to the earlier top-bar-width and icon-alignment fixes, not the charts task. The charts task added no CSS of its own (no `Dashboard.razor.css`, no `app.css` change), so no double-definition conflict is introduced here. Flagged only so the eventual commit scope stays deliberate.

</details>

<details>
<summary>File map</summary>

- `src/SistemaAlmacen.Client/SistemaAlmacen.Client.csproj` — adds `Blazor-ApexCharts` 7.0.0 package reference.
- `src/SistemaAlmacen.Client/Program.cs` — `using ApexCharts;` + `AddApexCharts()` registration.
- `src/SistemaAlmacen.Client/_Imports.razor` — `@using ApexCharts`.
- `src/SistemaAlmacen.Client/Pages/Dashboard/Dashboard.razor` — three charts, product metric cards, category list, three new report loads, zero-fill series, metric computation; existing cards/table/refresh preserved.
- `src/SistemaAlmacen.Client/wwwroot/index.html` — unchanged (correct for v7 self-loading; no script tags needed; confirmed by grep).
- `src/SistemaAlmacen.Client/Layout/MainLayout.razor.css`, `NavMenu.razor.css` — unrelated top-bar/icon-alignment de-duplication from an earlier task, bundled in the same working tree.
- `.agents/tasks/dashboard-apexcharts/verification.md` — build / static-asset / runtime evidence added this pass.

Full diff: `git diff` (working tree; changes are uncommitted).

</details>

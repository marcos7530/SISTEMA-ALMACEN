# Dashboard ApexCharts — Verification note (iteration v3)

This note records the build / static-asset / runtime evidence requested by the v2
review (blocking findings #1 "Missing verification evidence" and #2 "Script-tag
deviation unconfirmed at runtime"). The implementation itself was already in place
and matches the plan; this pass produces the missing evidence. No source logic was
changed in this pass. Finding #3 (unrelated layout CSS) is non-blocking and left for
the caller to handle at commit time.

## Environment
- OS: Windows, PowerShell. .NET 10. Solution under `src/`.
- Checked `Get-Process -Name SistemaAlmacen.Server` before building: NOT running, so
  the DLL-lock / SRI-corruption hazard did not apply. No generic `dotnet` processes
  were killed.

## 1. Static-asset path check (restored v7.0.0 package)
`~/.nuget/packages/blazor-apexcharts/7.0.0/staticwebassets/js|css/` ships:
- `js/apexcharts.esm.js`
- `js/blazor-apexcharts.js`
- `css/apexcharts.css`

The plan's guessed paths (`js/apex-charts.min.js`, `js/blazor-apex-charts.js`) do NOT
exist in v7.0.0. v7 self-loads its ESM module via `AddApexCharts()` + the `ApexChart`
component, so NO manual `<script>` tags belong in `index.html`. Confirmed `index.html`
contains no ApexCharts script tags → no 404 risk. This resolves the static half of
finding #2.

## 2. Build results (both 0 errors)
Command (client):
`dotnet build src/SistemaAlmacen.Client/SistemaAlmacen.Client.csproj -c Debug --nologo`
→ EXIT 0, "Compilación correcta. 0 Errores".
This proves the markup compiles against the actual restored v7 component/series API
(`ApexChartOptions<T>`, `ApexChart`, `ApexPointSeries`, `SeriesType.Area/Donut/Bar`,
`PlotOptionsBar { Horizontal = true }`, `Curve.Smooth`, `LegendPosition.Bottom`).

Command (server):
`dotnet build src/SistemaAlmacen.Server/SistemaAlmacen.Server.csproj -c Debug --nologo`
→ EXIT 0, "Compilación correcta. 0 Errores".

Asset staging: `bin/Debug/net10.0/wwwroot/service-worker-assets.js` lists
`_content/Blazor-ApexCharts/js/blazor-apexcharts.js`, `.../js/apexcharts.esm.js`,
`.../css/apexcharts.css` and `_framework/Blazor-ApexCharts.*.wasm` → assets are staged
into the WASM output and precached by the service worker.

## 3. Runtime check (server started, then stopped)
Started `dotnet run --project src/SistemaAlmacen.Server --no-build`; it logged
"Now listening on: http://localhost:5086" with no errors.

Static-asset / SRI probes (HTTP status):
- `/`                                                → 200 (WASM app served, no integrity error)
- `/_framework/blazor.webassembly.js`                → 200
- `/_content/Blazor-ApexCharts/js/blazor-apexcharts.js` → 200
- `/_content/Blazor-ApexCharts/js/apexcharts.esm.js` → 200
- `/_content/Blazor-ApexCharts/css/apexcharts.css`   → 200

All three ApexCharts assets the component self-loads serve 200 in this app's
PWA/service-worker setup → resolves the runtime half of finding #2 (self-load path
works, no 404s).

API probes:
- `/api/reportes/dashboard` without token → 401 (endpoint present, RequireAdmin enforced).
- After `POST /api/auth/login` (admin@sistema.local / Admin123!) with Bearer token:
  - `GET /api/reportes/ventas?fechaInicio=today-29&fechaFin=today` → 200;
    total 33900.00, 14 tx, DesgloseDiario = 4 days with sales (→ 26 days zero-filled
    client-side to a continuous 30-point axis), DesgloseMediosPago = 1.
  - `GET /api/reportes/ventas?fechaInicio=firstOfMonth&fechaFin=today` → 200;
    DesgloseMediosPago = 1 (donut renders 1 slice).
  - `GET /api/reportes/productos-mas-vendidos?...` (current month) → 200; count = 1
    (horizontal bar + "más/menos vendido" metric + 1 distinct category).
  - `GET /api/reportes/dashboard` → 200 (existing metric cards keep working).

The seeded data exercises the populated branches; the empty-data branches are guarded
in markup by `.Count > 0` checks that render Spanish empty-state messages instead of
throwing. Date-range math (today-29..today for 30 days; first-of-month..today for the
month) stays well under the endpoint's 365-day guard.

## What was NOT verified
No automated browser/DOM render check was run (no UI test harness exists for the WASM
client). Chart drawing is confirmed indirectly: client compiles against the v7 API, the
self-loaded JS/CSS assets serve 200, and all backing endpoints return 200 with data.
A full visual confirmation in a browser was not performed in this headless pass.

## Cleanup
Stopped the server process started for this check (only SistemaAlmacen.Server; no
generic dotnet processes touched). Deleted temp build/probe logs and the temp probe
script.

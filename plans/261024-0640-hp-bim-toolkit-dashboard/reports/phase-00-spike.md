# Phase 0 — WebView2 spike in Revit 2026 (2026-10-24)

## Setup
- `HPRebar/HPBimToolkit/` minimal add-in: `.addin` (own GUID, `ContextName HPBimToolkit`), dockable pane, `WebViewHost` (WebView2 created on first `Loaded`), `RevitRequestQueue` (one ExternalEvent), ribbon HPRebar ▸ BIM Toolkit ▸ Dashboard.
- `Microsoft.Web.WebView2` 1.0.2478.35 `ExcludeAssets=runtime;native;build;buildTransitive` → output/deploy hold NO WebView2 DLL (checked).
- Harness: `HPBimToolkit/tools/harness/start-revit-with-toolkit.ps1` (2nd Revit on a copy of `DynamoSampleModel.rvt`, Load Once for its pid only, UIA ribbon click) + `spike-round-trip.mjs` (CDP on `HPBIM_TOOLKIT_DEBUG_PORT`).

## Result
- PASS: page button → `postMessage` → `RevitRequestQueue` → `Execute` on API thread → `PostWebMessageAsJson` → page shows `documentTitle "toolkit-20261024-064353"`.
- WebView2 managed API 1.0.2478.35 loaded from `C:\Program Files\Autodesk\Revit 2026\Microsoft.Web.WebView2.Core.dll`, load context **Default** (Revit's) — no second copy. Runtime (Edge) 155.0.4283.62. Profile `%LocalAppData%\HPBimToolkit\WebView2`.
- Revit start with model ≈ 131 s on the dev machine (cold, second instance). One unsigned prompt (new DLL).

## Finding — shared Serilog `Log.Logger` (pre-existing defect, logged not fixed)
- Toolkit's WebView lines landed in `%LocalAppData%\HPRebar\McpBridge\logs\mcpbridge-*.log`; toolkit log only has its start line.
- Cause: add-ins that ship Serilog as a loose DLL resolve one Serilog assembly instance — the toolkit's own `HPBimToolkit\Serilog.dll` resolved to the already-loaded `HPRebar.McpBridge\Serilog.dll` (the only Serilog 4.4 module in the Revit process; the loose `Addins\2026\Serilog.dll` 4.3.0 is a DMStrApp leftover and is not loaded — corrected 2026-10-24), so every `Log.Logger = …` replaces the previous one; load order is alphabetical, McpBridge wins.
- ~~Side effect on HPRebar~~ — corrected 2026-10-24 by `plans/261024-0811-hprebar-log-isolation/reports/phase-00-cause.md`: HPRebar merges Serilog into `HPRebar.dll` (ILRepack), so its `Log.Logger` is its own; its log was silent since 2026-10-08 only because no HPRebar command ran. Only add-ins shipping Serilog loose share the copy.
- Toolkit fix (phase 2): own `Serilog.Core.Logger` instance passed through constructors; never assigns `Log.Logger`.

## Not checked yet
- Dynamo opened in the same session after the pane (phase 6).
- Pane hide/show, Revit shutdown with the browser alive (phase 6).

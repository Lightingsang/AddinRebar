# Phase 0 — Toolkit cleanup, ADR-0010, WebView2 spike in HPRebar R26 (2026-10-24)

## Cleanup
- Removed from toolkit: `HPBimToolkit.Core` → `HPRebar.Core` ProjectReference, `HPBimToolkit/Services/Kata/Excel/*` (ported COM attach). `DependencyPreloader` kept.
- ADR-0009 → Superseded by 0010; D8 rewritten (toolkit ↔ rebar add-in never reference each other; HPRebar → WebView2 compile-only, R26 only); old plan 261024-2245 marked dropped; development-rules + skill `revit-webview-addin` name the ADR-0010 exception.
- Deployed toolkit folder: stale `HPRebar.Core.dll` came back from `bin/Debug.R26/publish/HPBimToolkit/` (SDK deploys from publish dir, never prunes) — deleted there, old copies renamed `.locked-*`.

## Spike code (kept, replaced by HPWebHost in phase 1)
- `HPRebar.csproj`: R26-only `Microsoft.Web.WebView2` 1.0.2478.35 compile-only; `KataExport/View/Web/**` removed from compile when `RevitVersion != 2026`.
- `KataExport/View/Web/KataWebWindow.cs` modeless WPF window + WebView2 + inline page (hello → host facts).
- `KataExport/View/Web/KataWebWarmUp.cs` called from `Application.OnStartup` (`#if REVIT2026`).
- `KataExportCommand`: `HPREBAR_KATA_UI=web` opens the web window after the session read; default WPF.

## Results
| Check | Result |
|---|---|
| `dotnet build HPRebar.slnx -c Debug.R26` | ✅ (ILRepack OK, no WebView2 file in bin) |
| `dotnet build HPRebar -c Debug.R25` (web excluded) | ✅ |
| `HPBimToolkit.Core.Tests` | ✅ 86/86 |
| Warm-up at start | ✅ WebView2 1.0.2478.35 from `C:\Program Files\Autodesk\Revit 2026\`, context Default, 0 ms |
| Web window from ribbon (UIA Invoke, harness Revit pid 43344, copy of Dam kata) | ✅ runtime 155.0.4283.62, managed API in Default, page → host → page |
| Host load context | ✅ `HPREBAR` (ILRepack-merged HPRebar.dll) |
| Session read before window | ✅ 6 beams / 6 pieces (ids 9790831…41 selected via toolkit `dashboard.showElements`) |
| Round trip | ✅ 19 ms first, 1.1 ms second |
| Revit responding after | ✅ |
| Toolkit pane after cleanup | 🟡 restored + answered `showElements`; full dashboard harness not run (active view of the copy is a Legend → known "No good view" modal defect) |

## Harness gotchas
- `revit-uia.ps1 invoke -Name 'Kata Export'` matches the ribbon item container first (no InvokePattern) and falls back to a raw mouse click at screen coords — it went to the foreground window (Claude app), not Revit. Use `-Nth 1` (the `Button`, InvokePattern). Raw-click fallback has no foreground check: never rely on it.
- Kill the harness Revit by pid only (model copy).

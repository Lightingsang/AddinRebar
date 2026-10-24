# HP BIM Toolkit — Module 1 Dashboard (Revit 2026, WebView2 + React)

Approved 2026-10-24. Full design: session plan (mirrored here in short). Decisions by user: React + WebView2 (exception to MaterialDesign rule → ADR-0008); separate add-in `HPRebar/HPBimToolkit` in `HPRebar.slnx`, R26 only; WebView2 = Revit's own copy 1.0.2478.35 (compile-only reference).

## Layout
- `HPRebar/HPBimToolkit/` add-in: `Host/` (application, pane provider, WebView host, asset server, command), `Bridge/` (transport, event publisher), `ExternalEvents/` (one queue), `Services/` (document, selection, statistics, navigation), `Modules/Dashboard/`, `Infrastructure/`.
- `HPRebar/HPBimToolkit.Core/` net8.0, no Autodesk: contracts, protocol, dispatcher, statistics.
- `HPRebar/HPBimToolkit.Core.Tests/` xUnit v3.
- `HPRebar/HPBimToolkit.Frontend/` Vite + React + TS + Tailwind, embedded into the DLL.

## Phases
| # | Phase | Status |
|---|---|---|
| 0 | Spike WebView2 pane in Revit 2026 | ✅ Verified — [reports/phase-00-spike.md](reports/phase-00-spike.md) |
| 1 | Scaffold projects, slnx, ADR-0008 | ✅ Built (slnx Debug.R26 0 errors) |
| 2 | Host: assets, CSP, lifecycle, own logger | ✅ Built |
| 3 | Frontend with mock data | ✅ Tested (typecheck, lint, 25 vitest, build) |
| 4 | Bridge protocol + dispatcher + tests | ✅ Tested (65 xUnit) |
| 5 | Real Revit data + events | ✅ Built |
| 6 | Live verify, review, install guide | ✅ Verified: protocol 22/22 (×4), events 13/13 (×2), pane hide/show, 73k-element model; theme change + Dynamo CHƯA TEST — [reports/phase-06-live-verify.md](reports/phase-06-live-verify.md) |

## Protocol v1
request `{v:1,kind:"request",id,command,payload}` · response `{v:1,kind:"response",id,ok,payload|error:{code,message,details}}` · event `{v:1,kind:"event",event,payload}`. Errors `BAD_REQUEST, UNKNOWN_COMMAND, NO_DOCUMENT, BUSY, TIMEOUT, CANCELLED, REVIT_API_ERROR, INTERNAL`. Commands `app.hello`, `dashboard.getDocumentInfo`, `dashboard.getStatistics`, `dashboard.getSelection`, `dashboard.showElements`, `dashboard.openTool`. Events `selection.changed`, `document.changed`, `model.stale`, `theme.changed`.

# 0008 — HP BIM Toolkit: separate add-in with a WebView2 + React UI

- **Status:** Accepted (2026-10-24, by user — UI stack, placement and WebView2 source chosen in planning)
- **Tags:** [REVIT] [PROJECT]

## Context
The user wants a "BIM Toolkit" platform for Revit 2026 whose modules (Dashboard first, then Element Explorer, Parameter Manager, Reports) live in one dockable pane with a web UI (React + TypeScript + Vite + Tailwind). The repository rule for WPF UI is MaterialDesignInXamlToolkit everywhere (`.claude/rules/development-rules.md`, P5). The rebar add-in's feature-folder convention (ADR-0002) fits per-command windows, not a long-lived pane shared by several modules. Revit 2026 ships WebView2 1.0.2478.35 beside `Revit.exe` (Dynamo and Sentiment use the same copy); the phase-0 spike loaded it from there into the Default context and completed a page → ExternalEvent → page round trip (`plans/261024-0640-hp-bim-toolkit-dashboard/reports/phase-00-spike.md`).

## Decision
1. The toolkit is its own add-in, `HPRebar/HPBimToolkit` (own `.addin`, GUID `39486F0D-…`, `ContextName HPBimToolkit`), built only for Revit 2026 (`Debug.R26`/`Release.R26`), listed in `HPRebar.slnx`. It never references `HPRebar/HPRebar` and the rebar add-in never references it.
2. Its UI is a WebView2 page built from `HPRebar/HPBimToolkit.Frontend` (React + TS + Vite + Tailwind), embedded in the DLL and served from a virtual host — an **explicit exception** to P5 for this add-in only. Any WPF it shows (the start-failure fallback) stays minimal.
3. WebView2 is referenced compile-only at exactly the version Revit ships (`ExcludeAssets="runtime;native;build;buildTransitive"`); no WebView2 file is copied or merged. A Revit update that changes its copy is a re-test trigger, not a code change, as long as the API used exists in both.
4. Layering inside the add-in replaces the feature-folder convention: `Host/`, `Bridge/`, `ExternalEvents/`, `Services/`, `Modules/<Module>/`, `Infrastructure/`. Pure protocol, dispatcher and aggregation logic lives in `HPRebar/HPBimToolkit.Core` (net8.0, no `Autodesk.*`), tested by `HPBimToolkit.Core.Tests`.
5. All Revit API calls run in one `RevitRequestQueue : IExternalEventHandler` (R1, L7) or inside Revit events; only DTOs cross into JavaScript.
6. The toolkit owns its own `Serilog.Core.Logger` instance passed through constructors and never assigns `Log.Logger`: add-ins that ship Serilog as a loose DLL share one copy in Revit, so the last `Log.Logger` assignment wins among them (observed in the spike: the toolkit's lines went to the MCP bridge log; HPRebar merges Serilog with ILRepack and is unaffected).
7. New packages approved with the plan (D6): `Microsoft.Web.WebView2` 1.0.2478.35 (compile-only) in the add-in; the npm packages pinned in `HPBimToolkit.Frontend/package-lock.json`.

## Consequences
- \+ Modules share one transport, queue and UI kit; the web UI is fast to iterate (Vite dev server with a mock bridge).
- \+ No second WebView2 copy in `Revit.exe`, no conflict with Dynamo.
- − Two UI stacks in the repository; a Node toolchain (≥ 24) is needed to build the add-in.
- − Bound to Revit 2026's WebView2; another Revit version means re-checking the shipped copy.

## Alternatives
WPF + MaterialDesign (rejected by user: wants a web UI shared by future modules). A feature folder inside `HPRebar/HPRebar` (rejected: pane lifetime and layering do not fit the feature convention; avoids touching the rebar add-in). Shipping a newer WebView2 in the add-in's load context (rejected: two copies of the same assembly names in one process, needless risk).

## Rules
DEPENDENCY_RULES D6, F1, L7; Revit standard R1, R4, R8, R9; core D2, M6, P5 (exception), P7.

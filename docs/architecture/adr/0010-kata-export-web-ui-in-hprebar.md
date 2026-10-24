# 0010 — Kata Export: WebView2 + React window inside HPRebar on Revit 2026

- **Status:** Accepted (2026-10-24, by user — design approved with plan `plans/261024-2345-kata-export-web-ui-hprebar/`)
- **Tags:** [REVIT] [PROJECT]
- **Supersedes:** [0009](0009-bim-toolkit-uses-hprebar-core.md)

## Context
The user wants the Kata Export window rebuilt with the web stack of the `/revit-webview-addin` workflow — not a new tool: the button HPRebar ▸ Kata Export stays, the session read, the Excel and rebar services and the `HPRebar.Core` engine stay. ADR-0009 had moved Kata into HP BIM Toolkit as a separate web module with ported adapters; the user reversed that (same day): the tool stays in the rebar add-in. ADR-0008 allows a WebView2 + React UI only in HP BIM Toolkit; HPRebar builds for R23–R27 (net48 / net8 / net10), while only Revit 2026's WebView2 (1.0.2478.35 beside `Revit.exe`) has been checked.

## Decision
1. On **Revit 2026** the Kata Export window is a modeless WPF window hosting WebView2 with a page built from `HPRebar/HPBimToolkit.Frontend` (`kata.html`) — an explicit extension of the P5 exception of ADR-0008 to this one window. It is the default there; `HPREBAR_KATA_UI=wpf` opens the WPF window instead (fallback). R23–R25 and R27 keep the WPF window (`KataExportView`) unchanged. Kata Settings stays WPF on every version.
2. `KataExportViewModel` remains the state machine for both windows. The web window talks to it through a bridge adapter (state DTO out, commands in); business rules never move into the page.
3. One drawing code path: the web window keeps the WPF `KataElevationCanvas` (bound like `KataExportView.xaml`) invisible behind the browser; after each paint the recorded WPF `Drawing` is written as SVG (`HPWebHost.Rendering.DrawingSvgWriter`) and sent to the page, and the page's gestures call the canvas's own zoom / pan / click / key logic (extracted from its mouse handlers, behaviour unchanged). The painters are not touched. *(Planned first as a second, SVG painter backend; replaced during implementation because it needed no change to the live-verified painters.)*
4. WebView2 is referenced compile-only, at exactly Revit 2026's version, by the R26 build of HPRebar only (D6 approved with the plan); nothing is copied or merged. The web window's source files compile only under `RevitVersion == 2026`.
5. The WebView2 hosting code shared with HP BIM Toolkit moves to two libraries: `HPRebar/HPWebHost.Core` (net8.0: protocol v1 envelopes, parser, dispatcher, asset routes, page origins) and `HPRebar/HPWebHost` (net8.0-windows: WebView2 host control, embedded-asset server + CSP, transport, Drawing → SVG), tested by `HPWebHost.Tests`. They are ILRepack-merged into `HPRebar.dll` and shipped loose by the toolkit (no assembly of those names exists in HPRebar's load context). The toolkit's `DependencyPreloader` stays in the toolkit (JIT-compiling all of the merged `HPRebar.dll` would cost seconds).
6. HPRebar warms the WebView2 assemblies up during `OnStartup` (R26) so the first page message never binds an assembly lazily through Revit's add-in loader (the toolkit's deadlock, ADR-0008 notes).
7. HP BIM Toolkit does not reference `HPRebar.Core` and holds no Kata code (ADR-0009 withdrawn: its reference, its ported Excel adapter and its rule D8 are removed).

## Consequences
- \+ One Kata tool, one set of services and rules; the web UI is a view over the same ViewModel the WPF window uses.
- \+ The toolkit stays free of rebar knowledge.
- − HPRebar gains a second UI stack and a Node build on the R26 configuration; the other configurations are unaffected.
- − The canvas's mouse and key handlers were split into callable methods (behaviour unchanged) that the WPF window on every version now goes through; the hidden canvas paints twice as much on R26 (once for WPF, once read back as SVG).

## Alternatives
- **Kata as a toolkit module (ADR-0009):** reversed by the user — a second tool beside the WPF one.
- **Rewrite the state and the painters in TypeScript:** two drawing and state implementations drifting while WPF still serves R23–R25/R27.
- **Web UI on every Revit version:** WebView2 shipped by Revit 2023–2025/2027 is unchecked; net48 hosting differs.

## Rules
DEPENDENCY_RULES D3, D6, D8; ADR-0002 (feature folder kept: `KataExport/View/Web/`), ADR-0006, ADR-0008; core P5 (exception).

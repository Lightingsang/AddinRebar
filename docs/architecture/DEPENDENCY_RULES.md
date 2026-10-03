# RevitAddinAI — Dependency Rules

> Binding for every change under `HPRebar/`. Each rule has a status: **Enforced** (true today, keep it true), **Target** (violated today, fixed by a refactoring wave — do not add new violations), **Proposed** (needs the ADR to be accepted first).
> A new violation of a Target rule fails review even though old violations still exist ("stop the bleeding").

## 1. Project references

| # | Rule | Status | Check |
|---|---|---|---|
| D1 | `HPRebar.Core` references nothing but the BCL + Polyfill — never `Autodesk.*`, WPF, COM, Serilog, file system services | Enforced | `grep -rn "using Autodesk\|System.Windows\|Serilog" HPRebar/HPRebar.Core --include=*.cs` → 0 |
| D2 | `HPRebar.Core.Tests` references only `HPRebar.Core` | Enforced | csproj |
| D3 | `HPRebar` (add-in) → `HPRebar.Core` only (no reference to McpBridge/Server) | Enforced | csproj |
| D4 | `HPRebar.McpBridge` → `McpShared/{Contracts, McpBridge.Core}`; links theme files from `HPRebar/Resources` (no project reference) | Enforced | csproj |
| D5 | `HPRebar.Mcp.Server` never references Revit; `McpShared` never references `HPRebar/` | Enforced | csproj (CLAUDE.md "HPRebar MCP Bridge") |
| D6 | No new NuGet package without a plan + user approval (Planning Mode trigger) | Enforced | review |
| D7 | Remove packages nothing uses (`ClosedXML` is merged into `HPRebar.dll` but unused) | Target | `grep -rn "ClosedXML\|XLWorkbook" HPRebar/HPRebar --include=*.cs` |

## 2. Between feature folders (add-in and Core)

| # | Rule | Status |
|---|---|---|
| F1 | A feature folder never references another feature's namespace (`HPRebar.<A>.*` → `HPRebar.<B>.*`, same for `HPRebar.Core.<A>` → `HPRebar.Core.<B>`) | Target — violated: KataExport ⇄ KataRebar, KataRebar → BeamRebar, Core.KataRebar → Core.BeamRebar.Models |
| F2 | Code used by ≥ 2 features lives in `Shared/` (add-in) or `HPRebar.Core/Shared/` (Core) | Proposed (ADR-0004) |
| F3 | `Shared/` never references a feature | Proposed (ADR-0004) |
| F4 | `Resources/` (themes, icons) is the only other cross-feature folder; it holds no business logic | Enforced |

Check F1: `grep -rln "using HPRebar\.\(BeamRebar\|ColumnRebar\|FoundationRebar\|KataExport\|KataRebar\)" HPRebar/HPRebar/<Feature>` must list only that feature's own namespace.

## 3. Inside a feature (layer direction)

```
Command / Handler / Request / SelectionFilter   (Entry)
        ↓                ↓
   View → ViewModel      Service (orchestrator → adapters)
        ↓                ↓
            Model (add-in DTOs)
                 ↓
        HPRebar.Core/<Feature>   (Domain)
```

| # | Rule | Status | Note |
|---|---|---|---|
| L1 | View code-behind: `InitializeComponent`, `DataContext`, theme attach, `CloseRequested += Close` — nothing else | Enforced | CLAUDE.md MVVM rule |
| L2 | ViewModel does not reference `Autodesk.*` types (`Document`, `Element`, `RebarBarType`, `Floor`, `ElementId` excepted as an opaque id only) | Target | violated in Column, Foundation, KataRebar, KataExport VMs and runner interfaces |
| L3 | ViewModel does not call the Revit API, `TaskDialog`, Excel COM, or file I/O directly — it goes through `I<Feature>Runner` or an injected infrastructure interface | Target | violated: BeamRebarViewModel → RevitDialogs; Kata VMs → static COM + settings store |
| L4 | ViewModel never constructs a `Window` | Target | KataExportViewModel.Rebar.cs:198-222 |
| L5 | `Model/` never depends on `Service/` and never queries the document | Target | violated: BeamStack→PointMapper, FoundationSession→RevitUnits, KataBeamGeometry→KataAxisFrame, `*AnnotationSettings.Load` collectors |
| L6 | Transactions/TransactionGroups are opened only in the feature's orchestrator (or a creation service it calls inside the group) — never in Command, ViewModel, View, Model | Enforced | keep |
| L7 | `ExternalEvent.Create` only in the handler; the handler is the only `IExternalEventHandler` of the feature | Enforced | |
| L8 | Pure computation with no Revit/WPF/COM type belongs in `HPRebar.Core/<Feature>` | Target | stranded logic listed in audit |
| L9 | Unit conversion mm ↔ ft only through one `RevitUnits` (no literal `304.8`) | Target | |
| L10 | User-visible strings come from the feature's UiStrings catalog or result DTO, not from Core exceptions | Target | |

## 4. Static and global state

| # | Rule | Status |
|---|---|---|
| S1 | Mutable static fields/properties only from the allowlist: `<Feature>Command._window`, Serilog `Log.Logger`, `RevitHostTheme.Instance`, `McpBridgeHost.Current` | Target — violated by `DrawPrimitives.PixelsPerDip` ×2, `KataSettingsStore._cached` |
| S2 | Public static methods allowed for pure, stable operations (Core calculators, parsers, formatting) and stateless Revit adapters | Enforced |
| S3 | No static access to infrastructure (Excel, settings file, clock, environment) from ViewModels or orchestrators — inject | Target |
| S4 | No new process-wide cache without a reset/injection seam | Enforced (for new code) |

Check S1 (lists static fields/properties that are not `readonly`/`const`; get-only immutables such as `UiStringsCatalog.English` also appear and are fine):

```bash
grep -rnP "^\s*(public|private|internal|protected)?\s*static\s+(?!readonly|class|void|async|partial|extern|const)[\w<>\[\]?,. ]+\s+\w+\s*(\{|=(?!>)|;)" HPRebar/HPRebar --include=*.cs
```
Baseline 2026-10-03: 15 hits = 5 `_window` + `RevitHostTheme.Instance` (allowlist) + 6 immutable get-only (fine) + `PixelsPerDip` ×2 + `KataSettingsStore._cached` (violations).

## 5. Revit-version dependencies

| # | Rule | Status |
|---|---|---|
| V1 | Version differences use `#if REVIT20XX_OR_GREATER` + `// Multi-version: <topic>` | Enforced |
| V2 | APIs removed in a supported version are gated, never only `#pragma warning disable CS0618` | Target — Beam ×4, Foundation ×1 |
| V3 | One version-gated factory per API family (e.g. `RebarCurveFactory` for `CreateFromCurves`/hooks), shared through `Shared/Revit/` | Proposed |

## 6. AI / MCP

| # | Rule | Status |
|---|---|---|
| M1 | Script imports/globals are read from `HostScriptContracts`, never re-typed | Target (5 copies today) |
| M2 | A tool is `tool.json + code.cs + examples.json`; never a native command class in the bridge (ADR-05 of the MCP plan) | Enforced |
| M3 | Engine changes in `McpShared/` are additive and owned by the MCP plans, not by this governance | Enforced |

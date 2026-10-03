# RevitAddinAI — Architecture

> **Status:** as-is mapped 2026-10-03 (read-only scan, no build), to-be proposed — awaiting approval before any production refactor.
> **Read with:** [DEPENDENCY_RULES.md](DEPENDENCY_RULES.md) · [REVITADDINAI_CLEAN_CODE_STANDARD.md](../clean-code/REVITADDINAI_CLEAN_CODE_STANDARD.md) · ADRs in [adr/](adr/)
> **Evidence:** per-area maps in [plans/261003-2133-pragmatic-clean-code-governance/reports/](../../plans/261003-2133-pragmatic-clean-code-governance/reports/) (map-01 … map-05).

## 1. Scope — what "RevitAddinAI" means in this repository

"RevitAddinAI" is the **Revit product line** of this repo:

| Part | Path | What it is |
|---|---|---|
| Rebar add-in | `HPRebar/HPRebar/` | The product: 5 features (Column, Beam, Foundation, Kata Export, Kata Rebar). Revit R23–R27 (net48 / net8 / net10). |
| Domain library | `HPRebar/HPRebar.Core/` | Pure rebar maths, parsers, drawing models. netstandard2.0, **no Autodesk reference**. |
| AI bridge (Revit side) | `HPRebar/HPRebar.McpBridge/` | Second add-in: runs AI-supplied C# scripts inside Revit (R25/R26). |
| AI server | `HPRebar/HPRebar.Mcp.Server/` | net10 stdio MCP server, child process of the AI client. |
| Shared MCP engine | `McpShared/` | Host-neutral engine used by every HP MCP — **consumed, not governed here** (it has its own tests and owners). |
| Tests | `HPRebar/HPRebar.Core.Tests/`, `HPRebar.Mcp.Server.Tests/`, `HPRebar.Tests/` | xUnit v3 (pure), xUnit v3 (server), TUnit in-Revit. |

Out of scope: `HPAutoCad/`, `HPCivil3d/`, `HPNavis/`, `HPEtabs/`, `HPExcel/`, `HPGeo/`, `HPPowerBi/`, `HPRobot/`, `HPSap2000/`, `HPTekla/`, `revit-market-research/`, `course-website/`. They may adopt the PCC rules later; this document does not describe them.

## 2. System context

```
                    ┌────────────────────────── Revit.exe (one process) ──────────────────────────┐
 User ── ribbon ──▶ │ HPRebar.dll (ALC "HPRebar")              HPRebar.McpBridge.dll (ALC own)      │
                    │  Command → Window (modeless, WPF/MD)      ExternalEvent → ScriptRunner        │
                    │  → ExternalEvent → Orchestrator           → TransactionGroup "MCP: <label>"   │
                    │  → TransactionGroup "<Feature>"           ▲ named pipe hprebar-mcp-r2026      │
                    │  → Revit API          ▲                   │                                    │
                    │                       │ COM (late bound)  │                                    │
                    └───────────────────────┼───────────────────┼────────────────────────────────────┘
                                            │                   │
                                      Excel.exe          HPRebar.Mcp.Server.exe (net10, stdio) ◀── AI client
                                   (Kata Pro workbook)     SQLite registry + tools-library (%AppData%)
```

No LLM is called by the product: the AI is the **client** of the MCP server. The only external processes are Excel (Kata features, COM) and the MCP server (pipe).

## 3. As-is architecture

### 3.1 Projects and dependency direction (verified by csproj)

```
HPRebar (add-in) ──▶ HPRebar.Core ◀── HPRebar.Core.Tests
HPRebar.McpBridge ──▶ McpShared/{Contracts, McpBridge.Core}       (links 5 theme/icon files from HPRebar)
HPRebar.Mcp.Server ──▶ McpShared/{Server.Core ──▶ Contracts}
HPRebar.Tests (TUnit) ──▶ HPRebar
```
`HPRebar.Core` has **0** `Autodesk` references (verified, map-01 §4). `McpShared` never references `HPRebar/`.

### 3.2 Feature anatomy (as-is, identical skeleton in all five)

```
<Feature>Command.Execute          pick + validate + read + compose the object graph (composition root) + Show()
  └ static _window guard (modeless singleton)
<Feature>ViewModel / Session      CommunityToolkit.Mvvm; edits a Session; Run → IRunner.RunAsync
<Feature>ExternalEventHandler     ConcurrentQueue<Request> + ExternalEvent.Raise + TCS; Execute on API thread
Service/<Feature>Orchestrator     TransactionGroup "<Feature>" → N Transactions → static creators → Assimilate/RollBack
Service/*Reader|*Creator|*Finder  static Revit adapters (Element → mm model, mm model → Rebar/View/Dimension)
HPRebar.Core/<Feature>/           static pure calculators + immutable models (mm), unit-tested
```

| Feature | Add-in C# / XAML LOC | Core LOC | Core tests (grep) | Add-in tests |
|---|---|---|---|---|
| ColumnRebar | 6 040 / 852 | 1 591 | 99 methods | 21 TUnit, **all skipped** (no `.rvt` fixture) |
| BeamRebar | 5 890 / 874 | 3 236 | 105 methods | 0 |
| FoundationRebar | 1 198 / 417 | 1 229 | 51 methods | 0 |
| KataExport | 5 284 / 976 | 1 145 | 85 methods | 0 |
| KataRebar | 2 304 / 335 | 7 433 | 286 methods | 0 |

### 3.3 What is already good (keep it)

- **Domain/Revit split exists and is enforced**: all heavy maths is in `HPRebar.Core`, pure, mm-based, ~630 test methods.
- **Thread/transaction discipline**: every feature marshals through `ExternalEvent`, opens its `TransactionGroup` inside `Execute`, one undo entry per run, `RollBack` + rethrow on failure.
- **Small files**: of ~37 k production lines only 10 files exceed 300 lines; no `Manager`/`Helper`/`Utils` classes.
- **Modeless window contract** is uniform (static `_window`, `Activate`, `CloseRequested`, handler disposed on `Closed`).
- **Theme**: one `MaterialThemeBridge` path, tokenised palettes, guarded by `ThemeTokenCoverageTests`.

### 3.4 Structural problems (summary — details and rule ids in [CLEAN_CODE_AUDIT.md](../clean-code/CLEAN_CODE_AUDIT.md))

1. **No shared kernel** → infrastructure copied per feature: `RevitUnits` ×4, `RevitDialogs` ×4, `RebarFailureHandling` ×3 (+ Kata variant), `PointMapper` ×2, `LocalizationService` ×2, draw primitives ×3, ExternalEvent handler skeleton ×5, Core `Point3` ×3, `Polyline3`/`Vector3` ×2.
2. **Feature folders depend on each other**: KataExport ⇄ KataRebar (cycle, 6 + 5 files); KataRebar → BeamRebar (`PointMapper`, `RevitDialogs`); Core KataRebar → Core BeamRebar models. BeamRebar has become an undeclared shared kernel.
3. **Revit types leak upward**: `Document`/`Element`/`RebarBarType`/`Floor` reach ViewModels and runner interfaces (Column, Foundation, KataRebar, KataExport).
4. **Pure logic stranded in the add-in** (therefore untested): support classification, cut stations, span assembly, spec validation, bar-type matching, preview arithmetic that re-implements Core and diverges from it.
5. **Static everywhere, seams nowhere**: ~104 static classes; orchestrators hard-wire static creators; ViewModels call static Excel COM and a static settings cache → orchestrators/VMs testable only inside Revit, and the in-Revit suite never runs.
6. **Unreleased behaviour**: UI fields that never reach the model (Beam Views tab, Beam cover, Column view names) — see audit §2.
7. **Revit 2027 break**: Beam/Foundation call APIs removed in R27 under `#pragma CS0618`; KataRebar already has a version-gated factory nobody else uses.

## 4. Target architecture (to-be)

Principle order (from the brief, applied to every choice below): **simplicity > readability > cohesion > explicit dependencies > testability > formal pattern compliance.**
The target is an evolution of what exists, not a rewrite: **feature-sliced modular monolith + pure domain library + one small shared kernel**.

### 4.1 Layers inside one feature

```
                         ┌──────────────── HPRebar add-in (Revit-bound) ────────────────┐
 Entry           <Feature>Command  ·  <Feature>ExternalEventHandler  ·  Request  ·  SelectionFilter
                 (composition root; API-thread marshalling)                         [REVIT]
                        │ constructs                          │ calls on API thread
 Presentation    View/ (XAML, code-behind = Init + DataContext + theme)  ·  ViewModel/ (state, commands)
                 talks only to I<Feature>Runner + Core types + plain DTOs — no Autodesk types   (target)
                        │ RunAsync(spec)                      │
 Application     Service/<Feature>Orchestrator (or Workflow): owns TransactionGroup + transaction plan,
                 sequences adapters, returns a result DTO                                   [REVIT]
                        │                                     │
 Revit adapters  Service/*Reader · *Finder · *Creator · *Collector — translate Element ⇄ mm model;
                 no business decisions beyond what the API forces                           [REVIT]
                        │ mm models in / out
                         └──────────────────────────────────────────────────────────────┘
 Domain          HPRebar.Core/<Feature>/ Models · Calculators · Parsers — pure, deterministic, unit-tested
 Infrastructure  Shared/Excel (COM), Shared/Settings (JSON store), Serilog — behind small interfaces only
                 where a ViewModel or orchestrator needs a seam (PCC-161, PCC-173, PCC-245)
```

Rules of thumb that decide where code goes:

| If the code… | It goes to |
|---|---|
| decides *what* to build (counts, positions, lengths, zones, validation of numbers) | `HPRebar.Core/<Feature>/` |
| reads or writes Revit objects | `Service/` adapter in the feature (or `Shared/Revit/` when ≥ 2 features use it) |
| opens a Transaction/TransactionGroup | the feature's orchestrator only (one place per feature) |
| shows a TaskDialog / window | Command or handler (never Core, never a service, never a ViewModel directly) |
| talks to Excel / files / environment | infrastructure in `Shared/`, injected where a seam is needed |
| formats text for the user | Presentation (UiStrings catalog) |

### 4.2 Shared kernel (proposed — [ADR-0004](adr/0004-shared-kernel-folders.md))

```
HPRebar/HPRebar/Shared/              add-in side, namespace HPRebar.Shared.*
  Revit/   RevitUnits · RevitDialogs · RebarFailurePolicy · PointMapper · RebarCurveFactory (version-gated)
           RevitExternalEventHandler<TRequest> (queue + Raise check + Dispose fails pending)
  Wpf/     DrawPrimitives (instance PixelsPerDip) · LocalizationService
  Excel/   ExcelComAttach · ComLateBinding (moved from KataExport)
HPRebar/HPRebar.Core/Shared/         namespace HPRebar.Core.Shared.*
  Geometry/ Point3 · Vector3 · Polyline3 · Tolerance
```
Admission rule: code enters `Shared/` only when **two or more features already use it** (PCC-234 tolerate duplication until the abstraction is clear; PCC-230 one authoritative representation). `Resources/` stays as the shared UI resource folder.

### 4.3 Kata features (decision needed — [ADR-0006](adr/0006-kata-feature-boundary.md), status *Proposed*)

KataExport already carries the full Revit → Excel → Revit round trip; KataRebar is a second window over the same `KataRebarWorkflow`. Options: (A) retire the Kata Rebar window and fold its services into KataExport; (B) keep both windows and move their common parts (run reading, Excel COM, settings store, workflow) into `Shared/Kata/`. Either removes the cycle; which one is a product decision.

### 4.4 Composition and dependency injection ([ADR-0003](adr/0003-composition-root-without-container.md))

- `<Feature>Command.Execute` stays the **composition root** ("pure DI"): it constructs everything and passes collaborators through constructors (PCC-157, PCC-160, PCC-244).
- **No DI container.** The add-in has five short object graphs; a container would add a dependency and indirection without a real variation (PCC-236 YAGNI, PCC-237 KISS). Revisit only if graphs become deep or shared across features.
- Interfaces are introduced only for a **real reason**: a ViewModel/orchestrator needs a test seam for infrastructure (Excel, file store, Revit dialogs), or a genuine second implementation exists (PCC-123, PCC-151, PCC-238). Runner interfaces (`I<Feature>Runner`) stay — they are the ViewModel's seam to the Revit thread.

### 4.5 Static policy ([ADR-0005](adr/0005-static-policy.md))

| Allowed static | Not allowed |
|---|---|
| Pure, stable Core calculators/parsers (PCC-171) | Mutable static state outside the allowlist (PCC-178) |
| Stateless private helpers (PCC-168) | Static access to Excel/files/settings from ViewModels (PCC-172, PCC-245) |
| Stateless Revit adapters that no caller needs to substitute (PCC-167) | Static `PixelsPerDip`-style render state shared between windows |
| Allowlist: command `_window` singletons, `Log.Logger`, `RevitHostTheme.Instance`, `McpBridgeHost.Current` | New process-wide caches without a reset seam |

### 4.6 AI integration (MCP)

The bridge/server split is sound and stays (two processes, contracts-only sharing, engine host-neutral). Target changes are local: split `McpBridgeExternalEventHandler` (pipe executor vs Revit-thread queue), split `ScriptRunner.Run` (131 lines), read the import/global lists from `HostScriptContracts` instead of five hand-typed copies, and add pure tests for the transaction-policy/serializer logic that can be lifted out of Revit types.

### 4.7 Testing architecture

| Layer | How it is tested |
|---|---|
| Domain (Core) | xUnit v3, fast, no mocks needed (pure) — already strong |
| Logic currently stranded in add-in | **move to Core**, then xUnit |
| ViewModels | after Revit types leave them: xUnit in a new `HPRebar.Presentation.Tests` (net8-windows) with hand-written fakes of `I<Feature>Runner` / infrastructure interfaces — decision in refactoring plan wave 7 |
| Revit adapters + orchestrators | TUnit in-Revit (`HPRebar.Tests`) — requires committed `.rvt` fixtures (missing today) |
| MCP bridge | lifted pure parts in xUnit; live harness for Revit-thread behaviour |

### 4.8 Deliberately not adopted

Onion/hexagonal multi-project split; repository pattern over `FilteredElementCollector`; an interface per class; a DI container; mediator/CQRS; abstracting `Document` (sealed — wrapping it yields a parallel API with no benefit). Each would add structure without removing a real problem found in the audit (PCC-123, PCC-196, PCC-236).

## 5. Migration

Waves, batches and gates: [REFACTORING_PLAN.md](../clean-code/REFACTORING_PLAN.md). Nothing in §4 is implemented yet.

# RevitAddinAI — Clean Code Standard (Revit / HPRebar rules)

> **Binding** for every change under `HPRebar/` (add-in, Core, Revit MCP bridge/server, their tests). Status: Accepted 2026-10-03 (ADR-0001), scope widened 2026-10-04 ([ADR-0007](../architecture/adr/0007-hp-clean-code-scope.md)); applies to new code immediately; old code is brought in line only through [REFACTORING_PLAN.md](REFACTORING_PLAN.md).
> Since ADR-0007 the host-neutral rules (§1 priorities, N1–N10, M1–M10, FM1–FM5, C1–C7, S1–S5, D2/D3/D5/D6, K1–K5, CM1–CM6, T1/T3/T4/T7, P5–P9, runtime Q-rules) live in **[HP_CLEAN_CODE_CORE.md](HP_CLEAN_CODE_CORE.md)** with the same ids. This file keeps the Revit and HPRebar rules; read both. The Revit MCP specifics are in [host-appendix/revit.md](host-appendix/revit.md).
> Rule sources are tagged: **[PCC]** = *Pragmatic Clean Code* (ids → [PRAGMATIC_CLEAN_CODE_RULES.md](PRAGMATIC_CLEAN_CODE_RULES.md)), **[REVIT]** = Autodesk Revit constraint, **[PROJECT]** = decision of this repository (ADRs in [../architecture/adr/](../architecture/adr/)).
> Where this file and an older document disagree, this file wins; superseded statements are listed in §13.

## 0. How to use this standard

1. Before designing: read core §1, §11 (Revit) and §12 (project) — they decide *where* code goes.
2. While coding: core §2–§10 plus the HPRebar additions below decide *how* it is written.
3. Before review: run [CODE_REVIEW_CHECKLIST.md](CODE_REVIEW_CHECKLIST.md); every finding cites a rule id from core, this file or a PCC id.
4. **Numbers are review triggers, not limits** (core §0).

## 1.–6. Priorities, naming, methods, formatting, classes, SOLID

→ [HP_CLEAN_CODE_CORE.md](HP_CLEAN_CODE_CORE.md) §1–§6 (N1–N10, M1–M10, FM1–FM5, C1–C7, S1–S5). In HPRebar, M6 "host-free code" means `HPRebar.Core`, and N7's catalog is the feature's `UiStrings` (EN/VI).

## 7. Dependencies and static [PCC] [PROJECT]

D2, D3, D5, D6 → core §7.

| Id | Rule | Source |
|---|---|---|
| D1 | Composition root = `<Feature>Command.Execute`; collaborators enter through constructors; no DI container, no service locator | ADR-0003, PCC-157, 160, 175 |
| D4 | Mutable static only from the allowlist (`_window`, `Log.Logger`, `RevitHostTheme.Instance`, `McpBridgeHost.Current`) | ADR-0005, PCC-178 |

## 8. Coupling, duplication, YAGNI/KISS [PCC]

K1–K5 → core §8. In HPRebar, K1's single authorities include cover, anchorage, stock length 11 700, the bar-position limit 1002 and the bar-type matching tolerance — previews and validators call them, never re-derive them.

| Id | Rule | Source |
|---|---|---|
| K6 | Features do not reference each other; shared code goes to `Shared/` | ADR-0004 |

## 9. Comments

CM1–CM6 → core §9.

## 10. Tests [PCC]

T1, T3, T4, T7 → core §10.

| Id | Rule | PCC |
|---|---|---|
| T2 | Every pure rule moved to or written in Core gets tests in `HPRebar.Core.Tests/<Feature>/` mirroring the folder | 214, 274 |
| T5 | Fakes are hand-written against interfaces (no mocking library unless approved); never mock `Document` (sealed) | 287, [REVIT] |
| T6 | A green unit suite does not prove the add-in works in Revit: Revit-bound behaviour needs TUnit with committed `.rvt` fixtures or a documented manual/live check | 271, 272 |

## 11. Revit rules [REVIT]

| Id | Rule |
|---|---|
| R1 | Revit API only on Revit's API thread: inside `IExternalCommand.Execute`, `IExternalEventHandler.Execute` or a Revit event. Modeless windows reach the API only through the feature's ExternalEvent handler. |
| R2 | `Transaction`/`TransactionGroup` are opened inside the handler's `Execute` by the feature's orchestrator, never around a window's lifetime; one user action = one `TransactionGroup` named after the feature = one undo entry; `Assimilate` on success, `RollBack` + rethrow on failure. Write `using var t = new Transaction(doc, "Name");`. |
| R3 | Every transaction installs an explicit failure policy (shared `RebarFailurePolicy`); warnings may be suppressed only when logged; errors roll back. |
| R4 | Element lifetime: sessions keep `ElementId`/`UniqueId`, not `Element`/`Face`/`Reference`, across an ExternalEvent round trip; re-fetch and check `IsValidObject` before use; check the request's document is still the active one. |
| R5 | `HPRebar.Core` never references `Autodesk.*`; Core works in **mm** (and degrees); conversion to feet happens only at the adapter boundary through one `RevitUnits` — no literal `304.8`. |
| R6 | Multi-version: `#if REVIT20XX_OR_GREATER` + `// Multi-version: <topic>`; APIs removed in a supported version are gated, never only `#pragma warning disable CS0618`; one shared version-gated factory per API family. |
| R7 | Never compare localized display strings (`AsValueString()`, `FamilyName`, `Name` of system types) for logic — use `BuiltInParameter` integer values, enums, `ElementId`s or `BuiltInCategory`. |
| R8 | `TaskDialog`/windows are shown by the Command or handler, never by Core, services or ViewModels directly. |
| R9 | ExternalEvent handler: check `Raise()`'s result; on `Dispose` fail pending requests; complete `TaskCompletionSource` with `RunContinuationsAsynchronously`. |
| R10 | Geometry (`get_Geometry`, solids, faces) is read once per element per run and passed on, not re-extracted in every getter. |
| R11 | Selection filters and validators report a wrong pick before readers can throw. |
| R12 | Excel and other out-of-process COM are infrastructure: isolated adapters, objects released in `finally`, never called inside a Revit transaction. |
| R13 | Modeless window: static `_window` singleton, second click `Activate()`, no `DialogResult`, `CloseRequested` event, handler disposed on `Closed` (CLAUDE.md). |

## 12. Project rules [PROJECT]

P5–P9 → core §12.

| Id | Rule | Source |
|---|---|---|
| P1 | Feature folder convention of CLAUDE.md (4 root files; `Model/Service/View/ViewModel` singular; namespace = folder) | CLAUDE.md, ADR-0002 |
| P2 | Pure logic → `HPRebar.Core/<Feature>/`; Revit adapters → `Service/`; one orchestrator per feature owns transactions | ADR-0002 |
| P3 | Cross-feature code only in `Shared/` (add-in) / `HPRebar.Core/Shared/` (Core); `Resources/` for themes/icons | ADR-0004 |
| P4 | ViewModels hold no Autodesk types (opaque `ElementId` allowed), call no Revit/Excel/file API directly, construct no windows | DEPENDENCY_RULES L2–L4 |

## 13. Superseded statements in older documents

| Document | Statement | Now |
|---|---|---|
| docs/code-standards.md §4 | "Constructor inject `ILogger<T>` + services" | ADR-0003: constructor injection of services, static Serilog `Log`, no `ILogger<T>` |
| .claude/rules/development-rules.md & code-standards.md §3/§6 | `using var transaction = doc.NewTransaction(...)` | R2: `new Transaction(doc, "…")` — the code base uses it 19×, `NewTransaction` 0× |
| docs/system-architecture.md §3 | "DI Container (mode container)" | target design never built; see ARCHITECTURE.md §4.4 |
| CLAUDE.md "Current State" | "Four features exist", Core.Tests 448, TUnit 16 | corrected 2026-10-03 from a test run: five features; Core.Tests 949, Mcp.Server.Tests 109, engine 743, net48 113; 21 TUnit (all skip) |
| this file before 2026-10-04 | host-neutral rules defined here | moved to HP_CLEAN_CODE_CORE.md with the same ids (ADR-0007) |

# RevitAddinAI — Clean Code Standard

> **Binding** for every change under `HPRebar/` (add-in, Core, Revit MCP bridge/server, their tests). Status: Proposed 2026-10-03 (ADR-0001), applies to new code immediately; old code is brought in line only through [REFACTORING_PLAN.md](REFACTORING_PLAN.md).
> Rule sources are tagged: **[PCC]** = *Pragmatic Clean Code* (ids → [PRAGMATIC_CLEAN_CODE_RULES.md](PRAGMATIC_CLEAN_CODE_RULES.md)), **[REVIT]** = Autodesk Revit constraint, **[PROJECT]** = decision of this repository (ADRs in [../architecture/adr/](../architecture/adr/)).
> Where this file and an older document disagree, this file wins; superseded statements are listed in §13.

## 0. How to use this standard

1. Before designing: read §1, §11 (Revit) and §12 (project) — they decide *where* code goes.
2. While coding: §2–§10 decide *how* it is written.
3. Before review: run [CODE_REVIEW_CHECKLIST.md](CODE_REVIEW_CHECKLIST.md); every finding cites a rule id from this file or a PCC id.
4. **Numbers are review triggers, not limits** (PCC-067, PCC-115, PCC-184). Crossing one means "look again and justify", never "split mechanically". Splitting cohesive code to satisfy a number is itself a violation (PCC-186, PCC-229).
5. Do not apply a rule where its *Exceptions* say it does not fit; look the rule up when in doubt.

## 1. Priorities [PROJECT]

When rules pull in different directions, decide in this order:

**simplicity > readability > cohesion > explicit dependencies > testability > formal pattern compliance**

- An abstraction (interface, base class, factory, strategy) needs at least one real reason: an existing variation, a needed substitution, a test seam for infrastructure, isolation of infrastructure, or protecting Core from a framework (PCC-123, PCC-151, PCC-236, PCC-238). "It looks more SOLID" is not a reason (PCC-104).
- No class named `*Manager`, `*Helper`, `*Utils`, `*Util`, `*Processor`, `*Data`, `*Info` unless the name is the most precise available after trying (PCC-031, PCC-032, PCC-190).
- AI-generated code is **unreviewed code** until this standard's checklist passes; compiling, looking tidy or using patterns proves nothing (PCC-017, PCC-271).

## 2. Naming [PCC]

| Id | Rule | PCC |
|---|---|---|
| N1 | Types/fields/variables = nouns; methods = verbs; Booleans = positive yes/no questions (`IsValid`, `HasHooks`, `CanCreate`) | 021–024, 057 |
| N2 | Names say what the value is in the domain (`clearCoverMm`, `SupportTopBarLayout`), never `data`, `info`, `obj`, `tmp`, `res` | 032, 033, 040 |
| N3 | A name that needs "And/Or/If" means two things — split (`ReadAndValidate` ✗) | 030, 188 |
| N4 | `Get`/`Find`/`Resolve` do not create or mutate; a method that may create says so (`GetOrCreateViewType`) | 039, 041 |
| N5 | One word per concept across the product: pick and keep (*Reader* reads Revit, *Calculator* computes in Core, *Creator* writes Revit, *Builder* assembles a model, *Validator* returns a result) | 043, 216, 249 |
| N6 | Units in names at the boundary when ambiguous: `…Mm`, `…Ft`, `…Deg` | 040, 034 |
| N7 | Identifiers in English; user-facing text in the feature's UiStrings catalog (EN/VI); log text English | 046 |
| N8 | No abbreviations beyond universal ones (`Id`, `Mm`, `Ui`, `Xml`); no Hungarian/type suffixes | 048, 050 |
| N9 | Repeated meaningful literals become named constants defined once (`MaxBarPositions = 1002`, `DefaultCoverMm`) | 055, 079 |
| N10 | Test names: `Method_Scenario_ExpectedResult` for new tests (one convention — existing names are not churned) | 038, 277 |

## 3. Methods [PCC]

| Id | Rule | PCC |
|---|---|---|
| M1 | One coherent task per method; an orchestrating method that only sequences named steps counts as one task | 068, 110 |
| M2 | One level of abstraction per method; a top-level method reads as a list of decisions | 010, 070 |
| M3 | Review trigger: body > ~20 lines → look again; > 50 lines → must be justified in review; > 100 lines → split unless it is a flat, cohesive table/switch | 067, 093, 103 |
| M4 | > 4 parameters → design question: is a concept missing (introduce a parameter object), or does the method do two things? Never hide inputs in fields to shorten a signature | 060–062, 064, 065 |
| M5 | No Boolean parameter that switches behaviour — use two named methods or an enum with meaning; bare `true, false, true` at call sites is a defect | 063 |
| M6 | Prefer pure functions in Core: output depends only on inputs; no mutation of arguments; no hidden state (`ref barId` accumulators and shared `List<string> warnings` threaded through many calls are a smell — return a result object) | 075, 076 |
| M7 | Impurity at the edges: Revit/Excel/file calls in adapters; decisions in pure code | 077 |
| M8 | Validate preconditions first and fail with an `ArgumentException`/result, not deep inside | 078 |
| M9 | Behaviour lives on the type whose data it uses (move it there) | 072, 165, 199 |
| M10 | Members ordered top-down: public/high-level first, helpers below their callers | 073, 202–204 |

## 4. Formatting [PCC]

| Id | Rule | PCC |
|---|---|---|
| FM1 | Formatting is automated (`.editorconfig` + `dotnet format`) and consistent; routine formatting is not a review topic | 086, 088, 089 |
| FM2 | Always brace blocks; one blank line max; break parameter lists all-or-nothing; long chains one call per line | 092, 096, 097, 100 |
| FM3 | A block that does not fit one screen after formatting is extracted | 093, 103 |
| FM4 | File-scoped namespaces matching the folder (CLAUDE.md); one public type per file, tiny related records may share | 207, 208 |

## 5. Classes, SRP, cohesion [PCC]

| Id | Rule | PCC |
|---|---|---|
| C1 | One responsibility = one reason to change, describable in one sentence without "and" | 105, 108, 112 |
| C2 | Review trigger: file > 300 lines, ViewModel > 250 lines → check cohesion; a large class with one cohesive purpose stays whole | 115, 184, 185 |
| C3 | Split along clusters of members that share data/dependencies, not to hide size; revert splits that leave tightly coupled fragments | 114, 186, 193 |
| C4 | Keep data private; expose the narrowest surface (`IReadOnlyList<T>`, not the backing `List<T>`) | 182, 219, 220 |
| C5 | Presenting results (messages, dialogs, tables) is separate from computing them | 246 |
| C6 | Independent axes of variation live in separate collaborators (composition), not subclass matrices | 116, 239, 241 |
| C7 | Inheritance only for framework contracts (`ObservableObject`, `IExternalEventHandler`, WPF controls) and small closed families | 243 |

## 6. SOLID contracts [PCC]

| Id | Rule | PCC |
|---|---|---|
| S1 | OCP: introduce polymorphism only for families that do grow (bar shapes, request kinds that multiply); a closed `switch` over a stable enum is fine | 118, 119, 123 |
| S2 | Concrete-type selection is contained in one factory, not repeated `switch`es | 121 |
| S3 | LSP: no subtype checks (`is`/`as`) in clients to decide behaviour; no overrides that throw or do nothing | 128, 133, 136 |
| S4 | ISP: interfaces shaped by the client's role; no stub implementations; split a runner that mixes unrelated roles | 142, 145, 148 |
| S5 | DIP: high-level code (orchestrators, ViewModels) depends on abstractions **where the collaborator varies or is infrastructure**; abstractions are shaped by the client and contain no implementation detail | 154–156 |

## 7. Dependencies and static [PCC] [PROJECT]

| Id | Rule | Source |
|---|---|---|
| D1 | Composition root = `<Feature>Command.Execute`; collaborators enter through constructors; no DI container, no service locator | ADR-0003, PCC-157, 160, 175 |
| D2 | Constructors are trivial: assign, guard, no I/O, no Revit queries, no fire-and-forget tasks | PCC-176, 289 |
| D3 | Static allowed for pure stable operations, private helpers and stateless adapters; static access to infrastructure from ViewModels/orchestrators is not | ADR-0005, PCC-167–172 |
| D4 | Mutable static only from the allowlist (`_window`, `Log.Logger`, `RevitHostTheme.Instance`, `McpBridgeHost.Current`) | ADR-0005, PCC-178 |
| D5 | Infrastructure that must be controlled (Excel COM, settings file, clock, dialogs) is wrapped in a thin, logic-free interface **when a caller needs the seam** | PCC-161, 173, 245 |
| D6 | Law of Demeter: ask for the value you need; do not navigate `a.B.C.D` through other objects' internals or pass a whole session to read one field | PCC-225, 226 |

## 8. Coupling, duplication, YAGNI/KISS [PCC]

| Id | Rule | PCC |
|---|---|---|
| K1 | Every business rule has one authoritative implementation (cover, anchorage, stock length 11 700, bar-position limit 1002, bar-type matching tolerance) — previews and validators call it, never re-derive it | 013, 230, 231 |
| K2 | Mechanical duplication is extracted when ≥ 2 copies carry the same knowledge; coincidental similarity stays separate | 232, 233 |
| K3 | Tolerate a second copy briefly; on the third, extract (rule of three, in line with "let the abstraction reveal itself") | 234 |
| K4 | YAGNI: no extension points, options, alias properties "for compatibility", or unused parameters without a present requirement; delete dead members | 236, 123 |
| K5 | KISS: the simplest design that keeps clarity and the required capability | 014, 237 |
| K6 | Features do not reference each other; shared code goes to `Shared/` | ADR-0004 |

## 9. Comments [PCC]

| Id | Rule | PCC |
|---|---|---|
| CM1 | Fix the name/structure instead of explaining it; delete comments that restate code | 251, 253 |
| CM2 | Comment only context the code cannot carry: *why*, Revit API quirks, units, references to standards/clauses (TCVN …), workarounds with how to remove them | 258, 259, 261 |
| CM3 | No commented-out code; no change-history headers (git has it) | 256, 257 |
| CM4 | TODOs: `// TODO(<owner>): <task>` only for short-lived work; otherwise a tracked item | 262 |
| CM5 | XML doc comments on public Core APIs and shared kernel types describe the contract (units, ranges, nullability) | 263 |
| CM6 | No plan/phase/finding codes in code or comments (`per F13`, `phase 3`) — explain the reason itself | project rule (review-audit-self-decision.md §5) |

## 10. Tests [PCC]

| Id | Rule | PCC |
|---|---|---|
| T1 | Unit tests are automated, fast, isolated, repeatable; whole suite from one command | 265–268 |
| T2 | Every pure rule moved to or written in Core gets tests in `HPRebar.Core.Tests/<Feature>/` mirroring the folder | 214, 274 |
| T3 | Arrange/Act/Assert visibly separated; one behaviour per test; no loops/conditions/try-catch in tests; parameterize repeated cases (`[Theory]`) | 279–282 |
| T4 | Assert promised behaviour, not implementation details; cover guard clauses and regression-prone contracts | 284, 290 |
| T5 | Fakes are hand-written against interfaces (no mocking library unless approved); never mock `Document` (sealed) | 287, [REVIT] |
| T6 | A green unit suite does not prove the add-in works in Revit: Revit-bound behaviour needs TUnit with committed `.rvt` fixtures or a documented manual/live check | 271, 272 |
| T7 | Hard-to-test code is a design signal — change the production code, do not add test-only hooks | 020, 274 |

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

| Id | Rule | Source |
|---|---|---|
| P1 | Feature folder convention of CLAUDE.md (4 root files; `Model/Service/View/ViewModel` singular; namespace = folder) | CLAUDE.md, ADR-0002 |
| P2 | Pure logic → `HPRebar.Core/<Feature>/`; Revit adapters → `Service/`; one orchestrator per feature owns transactions | ADR-0002 |
| P3 | Cross-feature code only in `Shared/` (add-in) / `HPRebar.Core/Shared/` (Core); `Resources/` for themes/icons | ADR-0004 |
| P4 | ViewModels hold no Autodesk types (opaque `ElementId` allowed), call no Revit/Excel/file API directly, construct no windows | DEPENDENCY_RULES L2–L4 |
| P5 | WPF: MaterialDesign + `{DynamicResource}` tokens + Segoe UI; code-behind = Init + DataContext + theme (CLAUDE.md Theme section) | CLAUDE.md |
| P6 | Refactoring and features never share a commit; file moves get their own commit; conventional commits, no AI references | PCC-213, development-rules |
| P7 | No new NuGet package, project, or `.csproj`/`.slnx`/manifest edit without a plan approved by the user | antigravity-workflow.md |
| P8 | Report status with the Planned/Implemented/Built/Tested/Verified vocabulary; never "verified" without running the check | CLAUDE.md Response Format |

## 13. Superseded statements in older documents

| Document | Statement | Now |
|---|---|---|
| docs/code-standards.md §4 | "Constructor inject `ILogger<T>` + services" | ADR-0003: constructor injection of services, static Serilog `Log`, no `ILogger<T>` |
| .claude/rules/development-rules.md & code-standards.md §3/§6 | `using var transaction = doc.NewTransaction(...)` | R2: `new Transaction(doc, "…")` — the code base uses it 19×, `NewTransaction` 0× |
| docs/system-architecture.md §3 | "DI Container (mode container)" | target design never built; see ARCHITECTURE.md §4.4 |
| CLAUDE.md "Current State" | "Four features exist", Core.Tests 448, TUnit 16 | five features; ~630 Core test methods, 21 TUnit (grep, 2026-10-03; to be re-counted by a test run in Wave 0) |

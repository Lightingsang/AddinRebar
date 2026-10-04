# HP Clean Code — Core Standard (host-neutral)

> **Binding** for every new or changed C# line in `HPRebar/`, `McpShared/`, `HPAutoCad/`, `HPCivil3d/`, `HPNavis/`, `HPEtabs/`, `HPSap2000/`, `HPPowerBi/`, `HPExcel/`, `HPRobot/`, `HPTekla/`, for AI-proposed runtime tools and for embedded seeds ([ADR-0007](../architecture/adr/0007-hp-clean-code-scope.md)). Existing code is not refactored; the Boy Scout rule covers only lines a diff already touches (PCC-028).
> Read together with **one host appendix** — [revit](host-appendix/revit.md) (which leads to [REVITADDINAI_CLEAN_CODE_STANDARD.md](REVITADDINAI_CLEAN_CODE_STANDARD.md)), [autocad-civil](host-appendix/autocad-civil.md), [com-standalone](host-appendix/com-standalone.md), [net48-inprocess](host-appendix/net48-inprocess.md), [powerbi](host-appendix/powerbi.md).
> Rule sources: **[PCC]** = *Pragmatic Clean Code* (ids → [PRAGMATIC_CLEAN_CODE_RULES.md](PRAGMATIC_CLEAN_CODE_RULES.md)), **[PROJECT]** = decision of this repository (ADRs in [../architecture/adr/](../architecture/adr/)). Rule ids are stable and shared with the Revit standard: an id is defined in exactly one of the two files.

## 0. How to use this standard

1. Before designing: read §1, the host appendix and §12 — they decide *where* code goes.
2. While coding: §2–§10 decide *how* it is written.
3. Before review: run [CODE_REVIEW_CHECKLIST.md](CODE_REVIEW_CHECKLIST.md); every finding cites a rule id from this file, the Revit standard, an appendix or a PCC id.
4. **Numbers are review triggers, not limits** (PCC-067, PCC-115, PCC-184). Crossing one means "look again and justify", never "split mechanically". Splitting cohesive code to satisfy a number is itself a violation (PCC-186, PCC-229). The only exceptions are the three blocking runtime rules of §13.
5. Do not apply a rule where its *Exceptions* say it does not fit; look the rule up when in doubt.

## 1. Priorities [PROJECT]

When rules pull in different directions, decide in this order:

**simplicity > readability > cohesion > explicit dependencies > testability > formal pattern compliance**

- An abstraction (interface, base class, factory, strategy) needs at least one real reason: an existing variation, a needed substitution, a test seam for infrastructure, isolation of infrastructure, or protecting host-free code from a framework (PCC-123, PCC-151, PCC-236, PCC-238). "It looks more SOLID" is not a reason (PCC-104).
- No class named `*Manager`, `*Helper`, `*Utils`, `*Util`, `*Processor`, `*Data`, `*Info` unless the name is the most precise available after trying (PCC-031, PCC-032, PCC-190).
- AI-generated code is **unreviewed code** until the checklist passes; compiling, looking tidy or using patterns proves nothing (PCC-017, PCC-271).

## 2. Naming [PCC]

| Id | Rule | PCC |
|---|---|---|
| N1 | Types/fields/variables = nouns; methods = verbs; Booleans = positive yes/no questions (`IsValid`, `HasHooks`, `CanCreate`) | 021–024, 057 |
| N2 | Names say what the value is in the domain (`clearCoverMm`, `SupportTopBarLayout`), never `data`, `info`, `obj`, `tmp`, `res` | 032, 033, 040 |
| N3 | A name that needs "And/Or/If" means two things — split (`ReadAndValidate` ✗) | 030, 188 |
| N4 | `Get`/`Find`/`Resolve` do not create or mutate; a method that may create says so (`GetOrCreateViewType`) | 039, 041 |
| N5 | One word per concept across a product: pick and keep (*Reader* reads the host, *Calculator* computes in host-free code, *Creator* writes the host, *Builder* assembles a model, *Validator* returns a result) | 043, 216, 249 |
| N6 | Units in names at the boundary when ambiguous: `…Mm`, `…Ft`, `…Deg`, `…KN` | 040, 034 |
| N7 | Identifiers in English; user-facing text in the module's string catalog where one exists (HPRebar: the feature's UiStrings, EN/VI); log text English | 046 |
| N8 | No abbreviations beyond universal ones (`Id`, `Mm`, `Ui`, `Xml`); no Hungarian/type suffixes | 048, 050 |
| N9 | Repeated meaningful literals become named constants defined once (`MaxBarPositions = 1002`, `MaxOutputBytes`) | 055, 079 |
| N10 | Test names: `Method_Scenario_ExpectedResult` for new tests (one convention — existing names are not churned) | 038, 277 |

## 3. Methods [PCC]

| Id | Rule | PCC |
|---|---|---|
| M1 | One coherent task per method; an orchestrating method that only sequences named steps counts as one task | 068, 110 |
| M2 | One level of abstraction per method; a top-level method reads as a list of decisions | 010, 070 |
| M3 | Review trigger: body > ~20 lines → look again; > 50 lines → must be justified in review; > 100 lines → split unless it is a flat, cohesive table/switch | 067, 093, 103 |
| M4 | > 4 parameters → design question: is a concept missing (introduce a parameter object), or does the method do two things? Never hide inputs in fields to shorten a signature | 060–062, 064, 065 |
| M5 | No Boolean parameter that switches behaviour — use two named methods or an enum with meaning; bare `true, false, true` at call sites is a defect | 063 |
| M6 | Prefer pure functions in host-free code (`HPRebar.Core`, `HPAutoCad.Aec` pure folders, `McpShared` engines): output depends only on inputs; no mutation of arguments; no hidden state (`ref` accumulators and shared `List<string> warnings` threaded through many calls are a smell — return a result object) | 075, 076 |
| M7 | Impurity at the edges: host API / COM / file / network calls in adapters; decisions in pure code | 077 |
| M8 | Validate preconditions first and fail with an `ArgumentException`/result, not deep inside | 078 |
| M9 | Behaviour lives on the type whose data it uses (move it there) | 072, 165, 199 |
| M10 | Members ordered top-down: public/high-level first, helpers below their callers | 073, 202–204 |

## 4. Formatting [PCC]

| Id | Rule | PCC |
|---|---|---|
| FM1 | Formatting is automated (`.editorconfig` at each solution root + `dotnet format`) and consistent; routine formatting is not a review topic | 086, 088, 089 |
| FM2 | Always brace blocks — `if (x) return;` on one line included; one blank line max; break parameter lists all-or-nothing; long chains one call per line; a line past ~120 characters is a review trigger | 067, 092, 096, 097, 100 |
| FM3 | A block that does not fit one screen after formatting is extracted | 093, 103 |
| FM4 | File-scoped namespaces matching the folder (CLAUDE.md); one public type per file, tiny related records may share | 207, 208 |
| FM5 | Members in a predictable order: fields and dependencies → constructors → public members → private helpers; a caller sits above the methods it calls (step-down) | 202, 203, 204 |

## 5. Classes, SRP, cohesion [PCC]

| Id | Rule | PCC |
|---|---|---|
| C1 | One responsibility = one reason to change, describable in one sentence without "and" | 105, 108, 112 |
| C2 | Review trigger: file > 300 lines, ViewModel > 250 lines → check cohesion; a large class with one cohesive purpose stays whole | 115, 184, 185 |
| C3 | Split along clusters of members that share data/dependencies, not to hide size; revert splits that leave tightly coupled fragments | 114, 186, 193 |
| C4 | Keep data private; expose the narrowest surface (`IReadOnlyList<T>`, not the backing `List<T>`) | 182, 219, 220 |
| C5 | Presenting results (messages, dialogs, tables, envelopes) is separate from computing them | 246 |
| C6 | Independent axes of variation live in separate collaborators (composition), not subclass matrices | 116, 239, 241 |
| C7 | Inheritance only for framework contracts (`ObservableObject`, host event handlers, plugin base classes, WPF controls) and small closed families | 243 |

## 6. SOLID contracts [PCC]

| Id | Rule | PCC |
|---|---|---|
| S1 | OCP: introduce polymorphism only for families that do grow; a closed `switch` over a stable enum is fine; a defect is fixed in the code that has it, never wrapped in a "corrected" subtype | 118, 119, 123, 126 |
| S2 | Concrete-type selection is contained in one factory, not repeated `switch`es | 121 |
| S3 | LSP: no subtype checks (`is`/`as`) in clients to decide behaviour; no overrides that throw or do nothing | 128, 133, 136 |
| S4 | ISP: interfaces shaped by the client's role; no stub implementations; split a runner that mixes unrelated roles | 142, 145, 148 |
| S5 | DIP: high-level code (orchestrators, ViewModels, services) depends on abstractions **where the collaborator varies or is infrastructure**; abstractions are shaped by the client and contain no implementation detail | 154–156 |

## 7. Dependencies and static [PCC] [PROJECT]

D1 (composition root) and D4 (mutable-static allowlist) are HPRebar decisions — see the Revit standard.

| Id | Rule | Source |
|---|---|---|
| D2 | Constructors are trivial: assign, guard, no I/O, no host queries, no fire-and-forget tasks | PCC-176, 289 |
| D3 | Static allowed for pure stable operations, private helpers and stateless adapters; static access to infrastructure from ViewModels/orchestrators is not | ADR-0005, PCC-167–172 |
| D5 | Infrastructure that must be controlled (COM, settings file, clock, dialogs, HTTP) is wrapped in a thin, logic-free interface **when a caller needs the seam** | PCC-161, 173, 245 |
| D6 | Law of Demeter: ask for the value you need; do not navigate `a.B.C.D` through other objects' internals or pass a whole session to read one field | PCC-225, 226 |

## 8. Coupling, duplication, YAGNI/KISS [PCC]

K6 (feature folders) is an HPRebar decision — see the Revit standard. Folder isolation between MCP products is CLAUDE.md "Repository Layout": MCP folders depend only on `McpShared/`, never on each other.

| Id | Rule | PCC |
|---|---|---|
| K1 | Every business rule has one authoritative implementation (cover, anchorage, stock length, caps, tolerances) — previews and validators call it, never re-derive it | 013, 230, 231 |
| K2 | Mechanical duplication is extracted when ≥ 2 copies carry the same knowledge; coincidental similarity stays separate | 232, 233 |
| K3 | Tolerate a second copy briefly; on the third, extract (rule of three, in line with "let the abstraction reveal itself") | 234 |
| K4 | YAGNI: no extension points, options, alias properties "for compatibility", or unused parameters without a present requirement; delete dead members | 236, 123 |
| K5 | KISS: the simplest design that keeps clarity and the required capability | 014, 237 |

## 9. Comments [PCC]

| Id | Rule | PCC |
|---|---|---|
| CM1 | Fix the name/structure instead of explaining it; delete comments that restate code | 251, 253 |
| CM2 | Comment only context the code cannot carry: *why*, host API quirks, units, references to standards/clauses (TCVN …), workarounds with how to remove them | 258, 259, 261 |
| CM3 | No commented-out code; no change-history headers (git has it) — blocking for runtime tools (Q-B1) | 256, 257 |
| CM4 | TODOs: `// TODO(<owner>): <task>` only for short-lived work; otherwise a tracked item | 262 |
| CM5 | XML doc comments on public host-free APIs and shared kernel types describe the contract (units, ranges, nullability) | 263 |
| CM6 | No plan/phase/finding codes in code or comments (`per F13`, `phase 3`) — explain the reason itself | project rule (review-audit-self-decision.md §5) |

## 10. Tests [PCC]

T2, T5, T6 are Revit/HPRebar rules — see the Revit standard; each host appendix names its own test projects.

| Id | Rule | PCC |
|---|---|---|
| T1 | Unit tests are automated, fast, isolated, repeatable; whole suite from one command; a test never writes the user's live `%AppData%` state | 265–268 |
| T3 | Arrange/Act/Assert visibly separated; one behaviour per test; no loops/conditions/try-catch in tests; parameterize repeated cases (`[Theory]`) | 279–282 |
| T4 | Assert promised behaviour, not implementation details; cover guard clauses and regression-prone contracts | 284, 290 |
| T7 | Hard-to-test code is a design signal — change the production code, do not add test-only hooks | 020, 274 |

## 11. Host rules

Thread model, transactions, units, snapshots, guard additions and packaging differ per host: read the appendix for the folder you change. A host-free library (`McpShared/`, `HPRebar.Core`, `HPAutoCad.Aec` pure folders) never references a host API.

## 12. Repository rules [PROJECT]

P1–P4 (feature folders, Core placement, ViewModels) are HPRebar rules — see the Revit standard.

| Id | Rule | Source |
|---|---|---|
| P5 | WPF: MaterialDesign + `{DynamicResource}` tokens + Segoe UI; code-behind = Init + DataContext + theme | development-rules.md, CLAUDE.md Theme |
| P6 | Refactoring and features never share a commit; file moves get their own commit; conventional commits, no AI references | PCC-213, development-rules |
| P7 | No new NuGet package, project, or `.csproj`/`.slnx`/manifest edit without a plan approved by the user | antigravity-workflow.md |
| P8 | Report status with the Planned/Implemented/Built/Tested/Verified vocabulary; never "verified" without running the check | CLAUDE.md Response Format |
| P9 | Wire contracts in `McpShared/HPRebar.Mcp.Contracts` change only additively (deployed bridges and servers of older builds must keep working); `tools/list` of every server exe stays byte-identical unless the change is a tool change | ADR-0007, CLAUDE.md McpShared row |

## 13. Runtime tools and embedded seeds [PROJECT]

Applies to the `code` of every tool an AI proposes (`propose_tool`) and to every embedded seed (`<Host>.Mcp.Server/Registry/SeedLibrary/**/code.cs`). Checked by the shared bridge analyzer (`McpShared/HPRebar.McpBridge.Core/Scripting/`) at propose time and by each server's seed tests.

| Id | Rule | Severity | Detection notes |
|---|---|---|---|
| Q-B1 | No commented-out code (CM3) | **error — blocks the draft** | comment text that parses as complete C# with a code token; prose, version notes (`// v2`), units and URLs are not code |
| Q-B2 | No empty `catch` without a comment giving the reason | **error** | a catch body with no statement and no comment |
| Q-B3 | Script ≤ 300 lines | **error** | lines of the trimmed text (leading/trailing blank lines do not count) |
| Q-W1 | Block, local function or script-level method > 50 lines (M3) | warning | the script's top-level body is not a block |
| Q-W2 | Nesting > 3 control-flow levels | warning | `else if` chains and `try` do not add a level |
| Q-W3 | Vague identifier (N2): `data, tmp, temp, obj, res, val, foo, bar, stuff, thing` | warning | coordinate names (`x`, `y`, `x1`…) are not vague |
| Q-W4 | `bool` parameter on a local function or script-level method (M5) | warning | |
| Q-W5 | `catch (Exception)` that neither rethrows, returns, nor uses the caught exception | warning | recording `ex` into an `errors[]` envelope counts as reporting |

- A bridge built before the check reports **"quality not analysed"**: the draft is saved, publish stays allowed under every policy, and the review file (manual policy) or the publish message (auto policy) says so.
- Seeds: existing violations live in a hash-pinned allowlist in the server's `SeedQualityTests`; an edited seed loses its entry and must be clean. Generated seeds (Civil 3D, ETABS, Excel) are fixed in their generator, never by hand.
- Warnings are review triggers; errors are the only automatic refusals.

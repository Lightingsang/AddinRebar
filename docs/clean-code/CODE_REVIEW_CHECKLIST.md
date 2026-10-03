# RevitAddinAI — Code Review Checklist

> Use for every diff under `HPRebar/` (human, `code-reviewer` agent, or self-review of AI output). Each finding is reported as `path:line — <rule id> — fact — fix`, severity High/Medium/Low.
> Rule ids: standard ([REVITADDINAI_CLEAN_CODE_STANDARD.md](REVITADDINAI_CLEAN_CODE_STANDARD.md)) and PCC ([PRAGMATIC_CLEAN_CODE_RULES.md](PRAGMATIC_CLEAN_CODE_RULES.md)). Thresholds are triggers to look closer, not automatic failures.
> **AI policy:** code written by an AI is unreviewed until this checklist passes — compiling, tidy formatting or patterns prove nothing (PCC-017, PCC-271).

## 0. Scope of the diff
- [ ] The diff does one thing: feature **or** refactor **or** fix **or** file moves (P6, PCC-213)
- [ ] No unrelated edits, debug output, commented-out code, secrets, machine paths (CM3)
- [ ] No new package / project / `.csproj` / `.slnx` / `.addin` change without an approved plan (P7)

## A. Names (N1–N10)
- [ ] Every new name says what it is in the domain; no `data/info/tmp/obj/Manager/Helper/Utils` (PCC-032, PCC-190)
- [ ] Booleans read as positive questions; methods are verbs; no "And/Or/If" names (PCC-023, PCC-030)
- [ ] `Get/Find/Resolve` do not create or mutate (PCC-041)
- [ ] One word per concept (Reader/Calculator/Creator/Builder/Validator) (PCC-043)
- [ ] Ambiguous quantities carry units (`…Mm`, `…Ft`) (PCC-040)
- [ ] Repeated literals are named constants defined once (PCC-055)
- [ ] New tests follow `Method_Scenario_ExpectedResult` (PCC-277)

## B. Methods (M1–M10)
- [ ] One task, one level of abstraction (PCC-068, PCC-070)
- [ ] > 20 lines looked at; > 50 justified; > 100 split unless a flat cohesive table (PCC-067)
- [ ] > 4 parameters: missing concept? two jobs? (PCC-060, PCC-062) — no inputs hidden in fields (PCC-065)
- [ ] No behaviour-switching `bool` parameters; no bare `true, false` at call sites (PCC-063)
- [ ] Core code is pure: no argument mutation, no `ref` accumulators, no shared mutable lists (PCC-075, PCC-076)
- [ ] Preconditions checked first (PCC-078)
- [ ] Every block braced, single-line `if` included; lines past ~120 characters looked at (FM2)
- [ ] Callers above the methods they call; members in the FM5 order (PCC-202–204)

## C. Classes and contracts (C1–C7, S1–S5)
- [ ] Each changed class still has one responsibility stated in one sentence (PCC-105, PCC-112)
- [ ] No class grew toward a god class; file > 300 / VM > 250 lines justified (PCC-183, PCC-184)
- [ ] No split that only hides size or leaves tightly coupled fragments (PCC-114, PCC-186)
- [ ] Data private; narrow return types (`IReadOnlyList<T>`) (PCC-182, PCC-219)
- [ ] Presentation separated from computation (PCC-246)
- [ ] No subtype checks in clients; no overrides that throw/do nothing (PCC-133, PCC-136)
- [ ] A defect is fixed where it lives, not wrapped in a "corrected" type (PCC-126)
- [ ] Interfaces are role-shaped, each has a stated reason; no stub implementations (PCC-142, PCC-145)
- [ ] New abstraction justified by real variation, a seam, or a boundary — otherwise remove (PCC-123, PCC-236)

## D. Dependencies (D1–D6, DEPENDENCY_RULES)
- [ ] Objects created only in the feature's Command (composition root) or passed in (PCC-157, PCC-160)
- [ ] Constructors trivial: no I/O, no Revit queries, no fire-and-forget (PCC-289)
- [ ] No new mutable static outside the allowlist; no static infrastructure access from VMs/orchestrators (PCC-178, PCC-245)
- [ ] No cross-feature `using HPRebar.<OtherFeature>` (F1); shared code is in `Shared/` (F2)
- [ ] `Model/` does not use `Service/` and does not query the document (L5)
- [ ] No Law-of-Demeter chains through other objects' internals (PCC-225)

## E. Duplication and simplicity (K1–K6)
- [ ] No business rule implemented twice (preview vs builder, VM vs Core, two tolerances) (PCC-230)
- [ ] Copied code is either coincidental (justify) or extracted (PCC-232, PCC-233)
- [ ] No speculative options, alias props, unused parameters, dead members (PCC-236)

## F. Comments and tests (CM1–CM6, T1–T7)
- [ ] Comments explain *why*/units/API quirks only; no restating, no plan/phase codes (PCC-253, CM6)
- [ ] Workarounds documented with removal condition (PCC-261)
- [ ] New/changed pure logic has Core tests; AAA, one behaviour, no logic in tests, `[Theory]` for variants (PCC-279–282)
- [ ] Tests assert promised behaviour, not internals (PCC-284)
- [ ] Hard-to-test code changed in production, not worked around in tests (PCC-274)

## G. Revit safety (R1–R13)
- [ ] Revit API called only on the API thread (command/handler/event) (R1)
- [ ] Transactions only inside the orchestrator within `Execute`; one named `TransactionGroup`; `Assimilate`/`RollBack` + rethrow (R2)
- [ ] Failure policy installed per transaction; suppressed warnings logged (R3)
- [ ] No `Element`/`Face`/`Reference` held across ExternalEvent round trips without re-fetch/`IsValidObject`; document identity checked (R4)
- [ ] Core has no Autodesk reference; mm in Core; conversion only via `RevitUnits`, no `304.8` (R5)
- [ ] Version differences `#if` + `// Multi-version:`; no removed API behind only `#pragma CS0618` (R6)
- [ ] No logic on localized display strings (R7)
- [ ] Dialogs only from Command/handler (R8); `Raise()` result checked; pending requests failed on dispose (R9)
- [ ] Geometry extracted once per element per run (R10)
- [ ] COM objects released in `finally`; no Excel call inside a transaction (R12)
- [ ] Modeless window contract kept (R13)
- [ ] Builds for every targeted Revit configuration; live check on a model **copy** done or reported "CHƯA TEST"

## H. Report
- [ ] Status words exact: Planned / Implemented / Built / Tested / Verified (P8)
- [ ] Docs/logs updated (CLAUDE.md facts, changelog, REFACTORING_LOG for refactors)

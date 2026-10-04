# HP — Tool Development Workflow

> **Mandatory** for every new tool/feature/command and every non-trivial change under `HPRebar/`, `McpShared/` and the MCP folders of [ADR-0007](../architecture/adr/0007-hp-clean-code-scope.md). Works inside the routing of [.claude/rules/antigravity-workflow.md](../../.claude/rules/antigravity-workflow.md): a new tool is always **Planning Mode** (new folder/feature ⇒ plan + `Kế hoạch Triển khai` + approval when required).
> Rules referenced: [HP_CLEAN_CODE_CORE.md](HP_CLEAN_CODE_CORE.md) (N/M/FM/C/S/D/K/CM/T/P/Q ids), the folder's [host appendix](host-appendix/), [REVITADDINAI_CLEAN_CODE_STANDARD.md](REVITADDINAI_CLEAN_CODE_STANDARD.md) (R ids and HPRebar rules), [PRAGMATIC_CLEAN_CODE_RULES.md](PRAGMATIC_CLEAN_CODE_RULES.md) (PCC ids), [DEPENDENCY_RULES.md](../architecture/DEPENDENCY_RULES.md).

## Overview

```
 1 Requirement ─▶ 2 Inspect existing code ─▶ 3 Define responsibility ─▶ 4 Host boundary analysis
 ─▶ 5 Dependency analysis ─▶ 6 Design ─▶ 7 Implementation plan ─▶ 8 Clean-code check (of the plan)
 ─▶ [user approval if any trigger] ─▶ 9 Implement ─▶ 10 Build ─▶ 11 Tests ─▶ 12 PCC review
 ─▶ 13 Host safety review ─▶ 14 Git diff review ─▶ 15 Documentation ─▶ 16 Accept (Definition of Done)
```
Steps 1–8 produce the plan in `plans/<YYMMDD-HHmm>-<slug>/plan.md` (+ phase files). Steps 9–16 repeat per phase. Skipping a step is allowed only by writing "skipped: <reason>" in the plan.

## Steps

| # | Step | Do | Output / gate |
|---|---|---|---|
| 1 | **Requirement** | Restate the user outcome in domain terms (what the engineer picks, what Revit must contain afterwards, units, standards/clauses). List acceptance examples with numbers. Ask only what the code cannot answer (scout-first). | Requirement section with ≥ 2 concrete acceptance examples |
| 2 | **Inspect existing code** | Search for reuse before writing: `HPRebar.Core/**` calculators, `Shared/`, other features' readers/creators, Revit MCP seeds. Read the closest feature end-to-end. | "Reuse" list: what is reused, what is new and why |
| 3 | **Define responsibility** | One sentence per new class, no "and" (C1). Decide which existing class must **not** grow (god-class check, PCC-183). | Class list with one-line responsibilities |
| 4 | **Host boundary analysis** | Use the folder's appendix. For Revit: mark each step: pure (→ Core), Revit read (adapter), Revit write (creator inside orchestrator's group), UI, infrastructure (Excel/files). Decide transaction plan (one group, named steps), thread path (Command → handler → orchestrator), element lifetime (ids, not elements, R4), multi-version APIs (R6), localized strings (R7). | Boundary table + transaction plan |
| 5 | **Dependency analysis** | Draw the reference direction; check DEPENDENCY_RULES (F1 no cross-feature, L2–L5 layer rules, S1–S4 statics). Each new interface needs a stated reason (S5, §1 of the standard). No new NuGet/project/manifest without approval (P7). | Dependency sketch; list of interfaces + reason each |
| 6 | **Design** | Models (immutable records, mm), calculators (pure static OK), adapters, orchestrator, ViewModel state, runner contract. Name things with the vocabulary of N5. Prefer the simplest design that meets the examples (K5). | Design section; folder tree |
| 7 | **Implementation plan** | Phases small enough to build+test each; tests written alongside (test-first for Core rules, PCC-275); golden-run spec for Revit output. | Phase list with verification per phase |
| 8 | **Clean-code check (plan)** | Run the "before code" questions below; fix the design, not the checklist. | Answers recorded in plan |
| 9 | **Implement** | Core first (with tests), then adapters, orchestrator, VM, View. Keep commits per phase. Treat AI output as unreviewed (PCC-017). | Code |
| 10 | **Build** | The host's solution build (Revit: `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` plus each targeted Revit version; other hosts: `dotnet build <Host>/<Host>.slnx` with the appendix's no-deploy switch while the host runs). Zero new warnings. | Build log result in report |
| 11 | **Tests** | The host's test projects (Revit: `dotnet test HPRebar/HPRebar.Core.Tests` + Server tests if MCP touched + TUnit under R26; MCP folders: `<Host>.Mcp.Server.Tests` and bridge tests; `McpShared/` engine tests when the engine changes). New Core logic has tests; failing tests are fixed, never skipped. | Counts before/after |
| 12 | **PCC review** | [CODE_REVIEW_CHECKLIST.md](CODE_REVIEW_CHECKLIST.md) sections A–F; `code-reviewer` agent with the checklist; findings cite rule ids. | Review notes; all High fixed |
| 13 | **Host safety review** | Checklist section G with the folder's appendix (Revit: thread, transactions, failure policy, element validity, document identity, version gates, undo entry name); live run on a **copy** of a fixture/model/drawing. | Live result or "CHƯA TEST" with reason |
| 14 | **Git diff review** | Read the whole diff: no unrelated edits, no debug leftovers, no secrets, no plan codes in comments (CM6), file moves separate (P6). | Clean diff |
| 15 | **Documentation** | Update CLAUDE.md "Current State" facts, `docs/codebase-summary.md`, `docs/project-changelog.md`, the plan's phase report; new ADR if a decision was made; REFACTORING_LOG if refactoring. Insert the same text into AGENTS.md by hand when CLAUDE.md changes — never regenerate AGENTS.md while it carries content CLAUDE.md lacks. | Docs diff |
| 16 | **Accept** | Definition of Done below fully ticked; report with `Kết quả Triển khai` (Planned/Implemented/Built/Tested/Verified wording). | User acceptance |

## "Before code" questions (step 8)

1. Can existing code be reused (Core calculator, Shared adapter, another feature's pattern)? If copying, why is it not the same knowledge? (K1–K3)
2. Does each new or changed class keep exactly one responsibility? Does any existing class grow into a god class? (C1, C3, PCC-183)
3. Is any business rule now in two places (preview vs builder, validator vs calculator)? (K1)
4. Is every dependency explicit — constructor or parameter, no hidden static I/O, no service lookup? (D1–D5)
5. Is every new abstraction justified by a real variation, seam or boundary? (§1, S5)
6. Which parts are unit-testable, and are they in host-free code (HPRebar: Core, P2/T2)?
7. Which PCC rules apply most to this change? (list ids)
8. Which host constraints apply (the folder's appendix; Revit R1–R13), and how is each satisfied?
9. Is there any user-visible text, and is it in the module's string catalog (HPRebar UiStrings)? (N7)
10. What could this break in other features or Revit versions?

Forbidden by default: new `*Manager/*Helper/*Utils/*Service` names chosen for convenience, interfaces with one implementation and no reason, base classes for code reuse, static mutable state, cross-feature references.

## Warning signs that a step was skipped

| Step | Sign in the diff or the report |
|---|---|
| 2 | A new helper/class duplicates logic that a Core calculator or another feature already has |
| 6 | Interfaces, factories or base classes for a variation that does not exist yet |
| 9 | Comments explaining tangled code instead of a clearer name or an extracted method |
| 10 | New warnings accepted; one configuration built (one Revit version, one host) and the others assumed |
| 11 | Pure logic tested through Revit or real files; a test with several Arrange/Act/Assert rounds; no edge case (empty, zero, NaN) |
| 12 | "It runs" accepted as review; findings without rule ids |
| 14 | Whole-file reformatting, commented-out code or debug logging mixed into a logic commit |
| 16 | "Done" written before tests or the live check ran |

## Definition of Done

- [ ] Build succeeds for every targeted configuration — every Revit version for HPRebar, the host solution elsewhere (no new warnings)
- [ ] Behaviour matches the acceptance examples (live in the host on a copy, or explicitly "CHƯA TEST")
- [ ] No regression: existing tests green; golden run identical where applicable
- [ ] Names are clear and domain-specific (N1–N10)
- [ ] Methods focused, one abstraction level; triggers justified (M1–M5)
- [ ] Each class has one responsibility; high cohesion (C1–C4)
- [ ] No unnecessary coupling; HPRebar: no cross-feature reference (K6, F1); MCP folders reference only `McpShared/`
- [ ] Dependencies explicit (constructor/parameters) (D2; HPRebar D1)
- [ ] No hidden static/global dependency (D3, D5; HPRebar allowlist D4)
- [ ] No duplicated business knowledge (K1)
- [ ] No premature abstraction (§1, K4)
- [ ] Host API boundaries respected: host-free code pure, adapters thin (M6, M7; HPRebar P2, P4)
- [ ] Thread and transaction rules of the host appendix kept (Revit R1–R4, R9)
- [ ] Testable logic is tested without the host (T1, T3, T4; HPRebar T2)
- [ ] Comments carry only necessary context (CM1–CM6)
- [ ] PCC review done (checklist A–F) and host safety review done (G)
- [ ] Git diff reviewed (step 14)
- [ ] Documentation updated (step 15)

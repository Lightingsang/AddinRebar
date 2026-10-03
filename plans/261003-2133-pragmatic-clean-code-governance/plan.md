# Plan 261003-2133 — Pragmatic Clean Code governance for RevitAddinAI

Scope: Revit product line = `HPRebar/` (add-in, Core, Revit MCP bridge/server, tests). McpShared boundary only. Other hosts out of scope.
Rule: no production code change in this plan. Refactoring starts only after user approval (separate plan per wave).

## Phases (brief §11)

| # | Phase | Status | Output |
|---|---|---|---|
| 1 | Extract book knowledge (NotebookLM "Pragmatic Clean Code", 1 PDF, 15 ch.) | ✅ done | full text via `notebooklm source fulltext` (scratchpad only, copyright), 6 parallel extractors read every line → 293 rules → [PRAGMATIC_CLEAN_CODE_RULES.md](../../docs/clean-code/PRAGMATIC_CLEAN_CODE_RULES.md) |
| 2 | Map RevitAddinAI | ✅ done | [reports/map-01 … map-05](reports/) (shell, Column+Foundation, Beam, Kata, MCP+tests) |
| 3 | Audit vs PCC | ✅ done | [CLEAN_CODE_AUDIT.md](../../docs/clean-code/CLEAN_CODE_AUDIT.md) — AUD-001…060, B-01…15 |
| 4 | Target architecture | ✅ proposed | [ARCHITECTURE.md](../../docs/architecture/ARCHITECTURE.md), [DEPENDENCY_RULES.md](../../docs/architecture/DEPENDENCY_RULES.md), [adr/](../../docs/architecture/adr/) 0001–0006 *Proposed* |
| 5 | Clean Code Standard | ✅ proposed | [REVITADDINAI_CLEAN_CODE_STANDARD.md](../../docs/clean-code/REVITADDINAI_CLEAN_CODE_STANDARD.md) |
| 6 | Tool Development Workflow | ✅ proposed | [TOOL_DEVELOPMENT_WORKFLOW.md](../../docs/clean-code/TOOL_DEVELOPMENT_WORKFLOW.md), [CODE_REVIEW_CHECKLIST.md](../../docs/clean-code/CODE_REVIEW_CHECKLIST.md) |
| 7 | Refactoring Plan | ✅ planned | [REFACTORING_PLAN.md](../../docs/clean-code/REFACTORING_PLAN.md), [REFACTORING_LOG.md](../../docs/clean-code/REFACTORING_LOG.md) |
| 8 | CLAUDE.md governance | ✅ done | section "Clean Code Governance — RevitAddinAI (MANDATORY)" in CLAUDE.md + inserted (not regenerated) into AGENTS.md |

## Verification done
- Key facts re-checked by hand (grep/read): copies of RevitUnits/RevitDialogs/RebarFailureHandling, Kata cycle (6+5 files), 5 CS0618 pragma sites, mutable statics (15-hit grep baseline), Beam cover 25 mm, Beam Views tab, Column view-name fields, SupportType 1/1, Hook90Down, localized strings, KataRebar Raise(), MCP timeout message, no `.editorconfig`.
- Not run: build, tests (concurrent session; counts from grep).

## Issues found outside scope
- AGENTS.md carries ~123 lines CLAUDE.md lacks (SAP2000/PowerBi/Excel/Robot/Tekla sections, ETABS changes) — someone edited the generated mirror directly; regenerating would delete them. Back-port to CLAUDE.md, then regenerate.
- `HPRebar/build/` blocked by scout hook (`.claude/.ckignore`) — not audited.

## Open decisions (user)
See final report: ADR approval, Kata boundary (ADR-0006), behaviour-defect fixes (B-01/02/03/05), Revit fixtures for Wave 0, file-name convention for governance docs.

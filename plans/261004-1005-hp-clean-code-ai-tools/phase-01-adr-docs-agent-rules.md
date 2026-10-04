# Phase 1 — ADR-0007, core standard + host appendices, agent rules

## Context
[evaluation](reports/evaluation.md) §1 (E12, E13), §4 D5/D6; [map-01 §6](reports/map-01-revit-mcpshared.md) (section-by-section split); appendix rule tables: map-02 A1–A16, map-03 C1–C13, map-04 §6, map-05 §5.

## Overview
Priority P1 · Planned · docs + agent config only, no source. Makes the governance binding for every MCP folder (new/changed code only) without renaming any file or rule id.

## Key insights
- ~70 % of `REVITADDINAI_CLEAN_CODE_STANDARD.md` is host-neutral already (§0–§10 except D1, D4, K6, T2, T5, T6; §12 P5–P8).
- Moving text, not copying it: core becomes the one home of neutral ids; the Revit standard keeps its file name and its Revit/HPRebar ids and points to core (PCC DRY, K1).
- Workflow step 15 "regenerate AGENTS.md" contradicts E12 → must change here.

## Requirements
- ADR-0007 *Accepted on approval*: scope = HPRebar, McpShared, HPAutoCad, HPCivil3d, HPNavis, HPEtabs, HPSap2000, HPPowerBi, HPExcel, HPRobot, HPTekla; new/changed code only; Boy Scout on touched lines (PCC-028); runtime quality check = Q-B1..B3 blocking, Q-W1..W5 warnings; `HPGeo/` excluded (retired).
- `docs/clean-code/HP_CLEAN_CODE_CORE.md`: §1 priorities, N1–N10, M1–M10 (M6/M7 reworded "host-free code"/"host API"), FM1–FM5, C1–C7, S1–S5, D2/D3/D5/D6, K1–K5, CM1–CM6, T1/T3/T4/T7, P5–P8, plus §"Runtime tools and seeds" (Q-ids, thresholds, allowlist rule, "not analysed" meaning).
- `docs/clean-code/host-appendix/`: `revit.md` (index to standard §11–§12 + Revit MCP seed contract), `autocad-civil.md` (A1–A16), `com-standalone.md` (C1–C13 + known gaps: SAP MTA, Robot/Excel no path policy / no analyze-time tier), `net48-inprocess.md` (Navis + Tekla), `powerbi.md` (disposal via `using var`, snapshot regex, tokens, cloud error sanitising, DAX caps).
- `REVITADDINAI_CLEAN_CODE_STANDARD.md`: neutral sections replaced by "→ core §x (ids unchanged)"; Revit/HPRebar ids stay verbatim.
- `CODE_REVIEW_CHECKLIST.md`: header scope = all MCP folders; §D split (D1/F1-F2/L5 lines → "HPRebar only"); §G → "host appendix safety", Revit lines kept.
- `TOOL_DEVELOPMENT_WORKFLOW.md`: steps 4/10/11/13 → "host boundary / host build / host tests / host safety (see appendix)"; step 15 → "update CLAUDE.md and insert the same text into AGENTS.md; never regenerate while AGENTS.md carries content CLAUDE.md lacks".
- `REFACTORING_PLAN.md` §7: other hosts move from "out of scope" to "new code governed by ADR-0007; no refactoring waves".
- `CLEAN_CODE_AUDIT.md`: new section "Other hosts — behaviour defects" with the 6 items of evaluation §6 (ids `H-01…H-06`, B-xx stay HPRebar) — **pending user decision** (recommended yes).
- CLAUDE.md governance section retitled "Clean Code Governance — HP MCP repository" (same anchor text kept in a sentence for greps), scope table → core + appendix per folder; same text inserted by hand into AGENTS.md — **pending D5**.
- `hp-mcp-*` skills ×10: one short "Proposing a tool" block (Q-B rules block, Q-W warn, `quality not analysed` meaning) — 7 in `.claude/skills` + `apply`, 3 portable-only edited in `.agents/skills` — **pending D6**.
- code-reviewer agent memory: one note "check Q-ids + host appendix on MCP diffs".

## Related files
Create: `docs/architecture/adr/0007-hp-clean-code-scope.md`, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/{revit,autocad-civil,com-standalone,net48-inprocess,powerbi}.md`.
Modify: `docs/architecture/adr/README.md`, `REVITADDINAI_CLEAN_CODE_STANDARD.md`, `CODE_REVIEW_CHECKLIST.md`, `TOOL_DEVELOPMENT_WORKFLOW.md`, `REFACTORING_PLAN.md`, `CLEAN_CODE_AUDIT.md`, `CLAUDE.md`, `AGENTS.md`, `.claude/skills/hp-mcp-*/SKILL.md` ×7 (+ mirrors), `.agents/skills/hp-mcp-{powerbi,robot,tekla}/SKILL.md`, `.claude/agent-memory/code-reviewer/` (1 note + index).

## Steps
1. Write ADR-0007; README row.
2. Write core doc by moving neutral sections; replace them in the Revit standard by pointers; script-compare rule-id lists before/after (every id still defined exactly once).
3. Write 5 appendices from the map tables (each rule with its `path:line`).
4. Update checklist, workflow, refactoring plan, audit (if approved).
5. CLAUDE.md edit; AGENTS.md hand insert of the identical block (D5).
6. Skills (D6); `python scripts/sync-agent-skills.py apply` then `check`.
7. Agent memory note.

## Todo
- [ ] ADR-0007 · [ ] core · [ ] 5 appendices · [ ] standard pointers · [ ] checklist/workflow/plan/audit · [ ] CLAUDE/AGENTS · [ ] skills + sync · [ ] memory

## Success criteria (contract §2 expected output a, b, f)
Every rule id defined once (id-list diff empty except new Q-ids); all relative links resolve (link-check script); `sync-agent-skills.py check` exit 0 with no new drift; `git diff --stat AGENTS.md` shows insertions only.

## Commit split (P6)
1. `docs(clean-code): extend the governance scope to every MCP folder` (ADR, core, appendices, standard/checklist/workflow/plan/audit).
2. `docs(agents): point CLAUDE.md, AGENTS.md and the MCP skills at the core standard` (no `chore`/`docs` type for `.claude/` files → use `feat(skills)` for the skills part per CLAUDE.md Git rule).

## Risks
AGENTS.md loss (never regenerate); id drift (id-list diff); appendix rules citing stale lines (each cites `path:line` checked at write time).

## Rollback
`git revert` of the two commits; no code depends on docs.

# Plan: adopt Matt Pocock's skill ideas into existing skills

Status: done · Source: /grill-me contract 2026-10-10 (article homus.dev/talks/5-claude-code-skills-i-use-every-single-day; user: "implement")

## Decisions (user)
- No new skills; graft ideas onto existing ones.
- Adopt: vertical slices AFK/HITL → `/bs:plan`; tracer-bullet TDD → `/bs:cook --tdd`; deep/shallow-module lens → read-only mode of `/bs:code-review`.
- Deepening findings → `plans/reports/architecture-deepening-<date>.md`; user picks, then `AUD-xxx` added by hand. Keep 3 parallel sub-agents per picked candidate → RFC in `plans/`.
- Rule: Planning Mode runs `/grill-me` before `/bs:plan` when the 5 contract fields are not concrete.
- Dropped: write-a-prd/prd-to-issues as GitHub issues (repo plans in `plans/`, `gh` absent), Ralph loop, scheduled runs.

## Steps
| # | Step | Type | Blocked by | Status |
|---|---|---|---|---|
| 1 | bs-plan: `type` field + slice rule + `references/vertical-slices.md` | AFK | — | done |
| 2 | cook: `--tdd` tracer-bullet + `references/tdd-tracer-bullet.md` (pure code only) | AFK | — | done |
| 3 | code-review: architecture-deepening mode + `references/architecture-deepening.md` | AFK | — | done |
| 4 | rules: antigravity-workflow Planning Mode step + skill-workflow-routing rows | AFK | — | done |
| 5 | Evals (subagent): plan phases typed; deepening report read-only; tdd refuses Revit API code | AFK | 1–4 | done |
| 6 | skill-sync apply + check clean + tests 44/44 | AFK | 5 | done |
| 7 | Commit | HITL | 6 | done |

## Constraints
Refactor waves + approval rule and ADR-0007 unchanged; outputs in `plans/`; no CLAUDE.md / docs/clean-code edits.

## Results
- Evals (subagents, read-only, no files changed):
  - bs:plan on "seed list_pressure_networks for HPCivil3d" → 4 vertical phases, 3 AFK + 1 HITL (live verify in Civil 3D), dependencies set, first phase = thinnest slice.
  - code-review `codebase deepen HPRebar/HPRebar/Shared` → 5 ranked candidates with file:line, 3 mapped to existing AUD-001/004/005, a B-08 scope gap logged not fixed, stopped to ask for one pick.
  - cook --tdd → Core stirrup-count function: interface agreed first, one RED test, minimal GREEN; KataRebarOrchestrator (Revit API) refused as out of TDD scope → move rule to Core + golden run/live check.
- skill-sync: apply exit 0, check clean, tests 44 (1 symlink skip).

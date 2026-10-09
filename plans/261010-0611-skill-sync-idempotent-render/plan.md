# Plan: skill-sync — idempotent portable render, 8.3 paths, apply

Status: in progress · Source: /grill-me contract 2026-10-10 (user confirmed "implement")

## Root causes (verified)
- `_fold_routing_metadata` (`scripts/skill_sync/adapters/portable_markdown.py`) strips the quotes of an already JSON-quoted description without unescaping, then `json.dumps` again → `\"` doubles on every render. 4 skills non-idempotent: find-skills, grill-me, revit-addin, skill-creator. Effect: a correctly rendered mirror is flagged as conflict.
- `discovery.discover` line 105: `path.relative_to(bundle_root)` mixes the 8.3 short temp path (unresolved) with the resolved long path → 6/41 tests error on this machine.
- `hp-mcp-excel` conflict: SKILL.md line 89 differs ("chốt kiểm soát an toàn" vs "chốt an toàn"); user chose the `.claude` wording.

## Steps
| # | Step | Commit | Status |
|---|---|---|---|
| 1 | Test: render twice == render once (`\"`, YAML `''`, unquoted) — red on old code | A | |
| 2 | Fix `_fold_routing_metadata`: double-quoted → `json.loads` (fallback strip), single-quoted → `''`→`'` | A | |
| 3 | Fix discovery relative path with the resolved path; tests 41/41 + new | B | |
| 4 | `.agents/skills/hp-mcp-excel/SKILL.md:89` ← `.claude` wording | C | |
| 5 | `check`: no conflict; pending writes ⊆ {grill-me mirror, hp-mcp-etabs .claude, robot/tekla/powerbi new .claude, manifest} else stop | C | |
| 6 | `apply`, then `check` clean | C | |

## Constraints
stdlib only; fixtures never reference live `.claude`/`.agents`; no manual edits to other skills.

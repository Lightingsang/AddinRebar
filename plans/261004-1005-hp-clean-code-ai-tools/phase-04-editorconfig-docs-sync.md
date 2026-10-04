# Phase 4 — .editorconfig for the other solutions + docs sync

## Context
[evaluation §1 E13, §4 D2](reports/evaluation.md); `HPRebar/.editorconfig` (suggestions only, Wave 0.4); [map-02 §3](reports/map-02-autocad-civil.md): a solution-root `.editorconfig` is mirror-safe, a per-project one inside `HPAutoCad.Mcp.Server/` is not.

## Overview
Priority P2 · Planned · config + docs. No file reformatted, no analyzer switched on in csproj.

## Requirements
- Copy `HPRebar/.editorconfig` verbatim (every rule `suggestion`, `root = true`) to the solution roots: `McpShared/`, `HPAutoCad/`, `HPCivil3d/`, `HPNavis/`, `HPEtabs/`, `HPSap2000/`, `HPPowerBi/`, `HPExcel/`, `HPRobot/`, `HPTekla/` (10 — **pending D2**; 9 if McpShared excluded). `HPGeo/` excluded (retired).
- net48 projects (Navis, Tekla, Net48Tests): drop any rule the C# 7.3/net48 compiler setting cannot honour if one surfaces as a build warning (check, do not guess).
- Docs: `docs/codebase-summary.md` (quality check + core standard), `docs/project-changelog.md` entry, `docs/system-architecture.md` MCP section (one paragraph), CLAUDE.md MCP rows: tests counts touched by phase 2/3; fix stale facts found by the scouts (24 → 29 mirrored files; Revit 33 tools with an isolated registry; development-rules.md:88 HPGeo pointer) — only where the text is already being edited (Boy Scout).
- Skill sync: `python scripts/sync-agent-skills.py check` clean.

## Related files
Create: `.editorconfig` ×10. Modify: `docs/codebase-summary.md`, `docs/project-changelog.md`, `docs/system-architecture.md`, `CLAUDE.md` + `AGENTS.md` (hand insert, D5).

## Steps
1. Copy files; build each solution once (`dotnet build <slnx>`; HPRebar excluded — not changed; Debug.R2x never built while Revit runs) and compare warning counts with a build before the copy.
2. `git diff --stat` must list only the new files + docs.
3. Docs edits; sync check.

## Build / test commands
`dotnet build McpShared/McpShared.slnx`, `dotnet build HP<Host>/HP<Host>.slnx` ×9 (Debug; AutoCAD/Civil with `-p:DeployBundle=false`, Navis `-p:DeployPlugin=false`); full test set of phase 2.

## Success criteria
Contract expected output (e); warnings count unchanged or only new IDE suggestions (not build warnings); no reformat.

## Commit split (P6)
1. `build: add suggestion-only .editorconfig to every MCP solution`.
2. `docs: record the script-quality check and the core standard`.

## Risks
A suggestion surfacing as a build warning under `TreatWarningsAsErrors` in some project → check per solution, drop that rule locally if needed (record why).

## Rollback
Delete the `.editorconfig` files; revert docs commit.

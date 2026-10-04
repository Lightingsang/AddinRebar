# Phase 5 — redeploy the bridges and live-verify the quality gate on the other hosts

## Context
Brief [brief-phase-05-live-verify.md](brief-phase-05-live-verify.md) (contract §2 binding); Revit run [reports/live-verify-quality.md](reports/live-verify-quality.md) (5/5); harness to generalise [reports/live-verify-quality.py](reports/live-verify-quality.py); shared helpers `McpShared/tools/{mcp-session.py, harness_common.py}` (`Checklist`).

## Overview
Priority P1 · **Planned** · no production code. One host-neutral harness, Debug bridges redeployed, one report per host, plan/CLAUDE.md/audit updated, two commits, no push.

## Fact check (2026-10-04 12:4x, read-only)
| # | Brief fact | Verdict | Evidence |
|---|---|---|---|
| F1 | installed hosts | verified + Robot **found** | `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\robot.EXE` (COM `LocalServer32` = `C:\PROGRA~1\Autodesk\ROBOTS~1\Exe\robot.EXE`); `C:\Program Files\Microsoft Power BI Desktop\bin\PBIDesktop.exe` |
| F2 | running apps | **refines** | `acad` 28688 = **AutoCAD 2026 with `T2-DY7.dwg`** (a real Kata reference drawing, not scratch); `EXCEL` 37248 with **`Kata VE DAM.xlsm`** (user workbook); `Revit` 79908 = the phase-2 test instance on `HPRebar/output/golden/fixture-build/scratch.rte` (started by the agent); `msmdsrv` 6904 without PBIDesktop (orphan); published MCP servers of other sessions running: Excel ×2, Power BI ×2, Robot ×2 |
| F3 | profile values | verified | env prefixes / context tools: `HPAUTOCAD_MCP_`/`get_autocad_context`, `HPCIVIL3D_MCP_`/`get_civil3d_context`, `HPNAVIS_MCP_`/`get_navis_context`, `HPETABS_MCP_`/`get_etabs_context`, `HPROBOT_MCP_`/`get_robot_context`, `HPEXCEL_MCP_`/`get_excel_context`, `HPPOWERBI_MCP_`/`get_powerbi_context` (`HP*/**/Hosts/*HostProfile.cs`) |
| F4 | version env key | partial | Revit `Bridge__RevitVersion`, others `Bridge__HostVersion` (CLAUDE.md per host); categories and versions read from each profile at implementation |
| F5 | harnesses | verified | Navis, AutoCAD, Civil 3D, ETABS, Robot have `tools/harness/`; Excel and Power BI none |

## Harness design (`McpShared/tools/live-verify-quality.py --host <id>`)
- Host table (one row per host: server Debug exe glob, env prefix, version key + value, context tool, category present in the profile, how to read the active document from the context, "scratch" rule). Values from the profiles; `--list` prints it.
- Isolated registry under `%TEMP%\hp-live-quality\<host>\` (deleted in `finally`); `Checklist` for PASS/FAIL + JSON summary + exit code.
- Same five checks as the Revit run; scripts are pure (`transaction: "none"`, no host call) so no snapshot/undo is produced — check ETABS/Robot/Excel snapshot folders unchanged.
- Scratch guard per host: document path under `<repo>/…/output/` **or** an untitled/new document; anything else → refuse before proposing.
- Tool names `mcp_verify_quality_*` (isolated registry anyway).
- `reports/live-verify-quality.py` becomes a 5-line call of `--host revit`.

## Steps
1. Harness + `--list` self-check; `--host revit` against the agent's Revit (scratch.rte) — **pending user confirmation**.
2. Build Debug once per solution (servers already built in phase 2/3; bridges: Navis plugin, AutoCAD + Civil bundles, ETABS/Robot/Excel/Power BI bridge apps run from `bin/Debug`).
3. Per host, in order **Navisworks → ETABS → Robot → Power BI → AutoCAD → Civil 3D → Excel** (hosts not running first; the two that hold user files last): close only what the agent opened → deploy → launch on a new/scratch document (Navis: a copy of a sample `.nwd` under `output/`; ETABS/Robot: new blank model; Power BI: blank report; AutoCAD/Civil 3D: new drawing from template; Excel: new workbook in a **separate** instance or after the user closes theirs) → bridge window → opt-in via UIA (existing harness helpers) → run harness → `reports/live-verify-quality-<host>.md` → close.
4. Failure: retry once if environmental (pipe held by another session's server, slow start), else log `H-09…` with `path:line`.
5. Update plan.md (phase 5 row, results, known gaps), CLAUDE.md per-host notes, audit §2a; commits `test(mcp): …` (harness) and `docs(plan): …` (reports + docs).

## Risks
| Risk | Mitigation |
|---|---|
| AutoCAD/Excel hold user files (F2) | **stop and ask** before closing; never close a non-scratch document |
| Bridge pipe already held by another Claude session's MCP server (seen with Revit) | the session must not call those MCP tools during the run; harness reports "bridge not connected" distinctly; retry after the other server is idle |
| Excel bridge attaches to the first running Excel (`GetActiveObject`) | run only when no user workbook is open, or ask the user to close theirs |
| Deploying while the host runs strips/locks bundles | deploy only with the host closed (each host's rule) |
| Robot/ETABS licence or first-start dialogs | UIA scoped to own pid; if a dialog blocks, report CHƯA TEST with the reason |
| Unsigned DLL prompts (AutoCAD SECURELOAD ×n, Revit) | answer *Load Once* for own pid only (existing helpers) |

## Success criteria
5/5 per host run; reports written; SAP2000/Tekla "CHƯA TEST — not installed"; no user document closed without consent; two commits, no push.

## Rollback
Redeploy the previous published bundles/plugins (`output/` folders) if a host misbehaves; harness and reports are additive.

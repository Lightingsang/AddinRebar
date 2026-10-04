---
title: "Fix H-09..H-12 — live-harness and UIA defects found by the quality-gate live checks"
status: done
priority: P2
branch: RebarVersion1
tags: [harness, uia, autocad, civil3d, navis, robot, excel, fix]
created: 2026-10-04
---

# Plan 261004-1340 — harness / UIA fixes H-09…H-12

Source: [CLEAN_CODE_AUDIT.md §2a](../../docs/clean-code/CLEAN_CODE_AUDIT.md) H-09…H-12, found in [phase 5](../261004-1005-hp-clean-code-ai-tools/plan.md). User approved "fix them in a separate plan" 2026-10-04. Fix track: no refactoring mixed in; one commit per fix kind.

| # | Defect | Fix | Files | Verify |
|---|---|---|---|---|
| H-09 | AutoCAD harness answers SECURELOAD **Always Load** for any process | answer **Load Once**, only for the harness's own pid (same code as HPCivil3d) | `HPAutoCad/tools/harness/harness-common.ps1` | live start with a fresh build (prompts appear) |
| H-10 | `/nologo /b` start: no MCP ribbon panel, script command opens no window, COM unavailable | `Start-AcadWithBridge` stops passing `/b` (the script is ignored, kept as a parameter for the callers); new `Open-BridgeWindow` waits for the loader's "ribbon panel … added" line and presses **MCP Bridge**; AutoCAD `Set-OptIn` looks in every acad window (like Civil) | both `harness-common.ps1` | AutoCAD + Civil 3D: `launch-live-cad`-style start → `live-verify-quality.py` 5/5 |
| H-11 | Robot / Excel opt-in checkboxes have no accessible name | `AutomationProperties.AutomationId` + `Name` on each checkbox (`AllowExecution`, `AllowHeavy` / `AllowWrite`, `AllowDestructive`) | `HPRobot/…/Views/MainWindow.xaml`, `HPExcel/…/Views/MainWindow.xaml` | build both solutions + server tests; UIA dump shows the ids; quality harness 5/5 |
| H-12 | Navis harness closes `Reload last file?` with WM_CLOSE, prompt loops | `Start-NavisworksWithModel` answers **No** to that prompt before closing other dialogs | `HPNavis/tools/harness/harness-common.ps1` | kill a Roamer, start through the harness → model loads; quality harness 5/5 |

Out of scope: product behaviour, server/bridge C# logic, the other harness flows beyond the start helpers (re-run of full suites not required; the start helpers are exercised live).

## Commits (no push)
1. `fix(harness): answer SECURELOAD once and open the bridge window from the ribbon` (H-09, H-10)
2. `fix(harness): answer No to Navisworks' reload prompt at start` (H-12)
3. `fix(mcp): name the opt-in checkboxes of the Robot and Excel bridges` (H-11)
4. `docs(audit): close H-09..H-12` (audit rows + plan results)

## Results (2026-10-04)
| # | Result |
|---|---|
| H-09 | ✅ AutoCAD start with a fresh bundle: 8 SECURELOAD prompts, every one answered *Load Once* for the harness pid |
| H-10 | ✅ `Start-AcadWithBridge` (both harnesses, unchanged signature) → pipe → `Open-BridgeWindow` → `Set-OptIn` → `live-verify-quality.py`: AutoCAD 5/5, Civil 3D 5/5 |
| H-11 | ✅ Robot 100/100, Excel 93/93 server tests; UIA dump `AllowExecution`/`AllowHeavy` (Robot), `AllowExecution`/`AllowWrite`/`AllowDestructive` (Excel); live 5/5 each |
| H-12 | 🟡 code in place; the `Reload last file?` prompt did not reappear after killing Roamer twice (with and without a model), so the No path is CHƯA TEST; the normal start verified 5/5 |

Not re-run: the full AutoCAD/Civil 3D suites (`run-live-verify.ps1`, isolation steps that start their own second instance with `/b` — unchanged, out of scope).

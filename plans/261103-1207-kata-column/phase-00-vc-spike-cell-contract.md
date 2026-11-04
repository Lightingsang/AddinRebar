# Phase 0 — VC spike + cell contract

## Overview
Priority high, blocks 1 and 3. Status ❌. Learn exactly how Kata Pro `VC` reads sheet `Cot` and what it draws; freeze
golden DWG fixtures for parity.

## Key insights
- No repo precedent for automating Kata Pro: beam DWGs were drawn by the user, read by MCP AutoCAD (transaction none),
  frozen as `HPRebar.Core.Tests/KataRebar/Fixtures/b0*-dwg.json`.
- MCP `execute_autocad_code` cannot send commands (guard). Only route: COM `SendCommand` from Windows PowerShell 5.1,
  detached job, pid-guarded (`HPAutoCad/tools/harness/README.md`, `harness-common.ps1` SECURELOAD answer).
- Kata Pro loads via `C:\kata_pro\Goi lenh.lsp` (netload `Kata_pro64_Cad2025.dll` + `menukata.cuix`); `vc` → `ve_cot`.
- Kata code obfuscated — behaviour only by observation.
- User's Excel has `C:\kata_pro\Kata.xlsm` open now.

## Steps
1. Copy `Kata.xlsm` → scratch `kata-col-p0.xlsm`; open it in Excel (read user's Excel state first, never touch the original);
   guard `ActiveWorkbook.FullName` = scratch.
2. Start a separate acad.exe (record pid), wait 45 s, `SendCommand` `(load "C:/kata_pro/Goi lenh.lsp")`; answer SECURELOAD
   for that pid only; restore sysvars at end.
3. New drawing → `VC` detached; capture its prompts/dialogs (`PrintWindow` acad hwnd, UIA scoped to pid). If a Kata dialog
   blocks automation → stop, fallback: user runs VC.
4. Baseline column (sample cells) → save DWG copy; read entities via MCP AutoCAD → `col-base-dwg.json` (layers
   `kata_thep chu`, `kata_thep dai`, `kata_net thay`, `kata_dim`, blocks `kata_block_KHT`, `kata_block_SECBAL`).
5. Variants, one change each: C negative vs positive step; D step; Ø `25/20`; J value; K change; PILECAPS height; hoop
   `d10a100` vs `/200`; ties M–V; F4 second number; G3..K3 flags → note diff.
6. Write `reports/kata-column-cell-contract.md` (cell → meaning → VC effect, evidence = fixture/variant) and phase files 1–5.

## Spike results (2026-11-03, in the user's AutoCAD pid 37712 — only AutoCAD running)
- `VE_COT` needs no prompt/dialog: reads the ACTIVE Excel workbook sheet `Cot`, draws at a fixed place (probe: 469 entities).
- Every change of active drawing in that AutoCAD raises "AutoCAD Error — There was an error opening the file None"
  (reproduced by switching between two saved user drawings; not HP code; suspect Kata settings `;None` lines 236–237 of
  `Goi lenh.lsp` or CadAddinManager). `tools/dlg-watch.ps1` clicks OK for that exact text/pid only.
- `SendCommand("VE_COT\n")` sometimes hangs after drawing → ESC; v00/v01 drew 0 entities, v02 drew 542
  ([variants/v02-b-pos-both.json](reports/variants/v02-b-pos-both.json)), v03 → Kata unhandled `COMException
  0x80020009` in its Excel late binding (dialog in user's AutoCAD). **Stopped: unreliable and disruptive in the user's
  session.** Batch scripts kept in [tools/](tools/) (`esc.py` = `press_escape` of `HPAutoCad/tools/harness/live-verify.py`).
- Next: run the variants in a dedicated AutoCAD when the user's AutoCAD is closed, waiting for idle between steps
  (no ESC while drawing), or user runs VC on prepared copies.

## Success criteria
- Contract answers every "Open" item of the initial read or marks it unknown with evidence.
- ≥ 2 golden fixtures (straight column; stepped column with crank / cut) committed under Core tests fixtures.
- No write to the original workbook; acad started by harness closed by pid.

## Risks
| Risk | Mitigation |
|---|---|
| VC modal dialog / license prompt blocks SendCommand | detached job + timeout; fallback user runs VC |
| VC writes to the active workbook | only scratch active; verify mtime of original unchanged |
| Kata Pro load conflicts with HPAutoCad bundle | separate acad, own profile not changed; log check |

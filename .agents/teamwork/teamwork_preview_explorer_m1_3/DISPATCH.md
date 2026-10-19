# DISPATCH — Explorer M1-3 (Unencrypted Assets Decoding)

## Working Directory
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m1_3\`

## Mission
1. Strictly Read-Only inspection on all unencrypted assets in `C:\kata_pro\`.
2. Inspect AutoLISP & Menu Files:
   - `Goi lenh.lsp`: inventory all 118 `defun c:` command aliases and all 86 configuration lines.
   - `Goi lenh - Gstarcad.lsp`, `Goi lenh - Zwcad.lsp`.
   - `menukata.cui`, `menukata.cuix`, `menukata.mnr`, `menukata_light.mnr`.
3. Inspect CAD Template & Structural Model:
   - `Iso_kata.dwg`: enumerate block definitions (119 blocks), layers (12 layers), dimstyles (15 dimstyles), text styles.
   - `ISO_KATA.EDB`: document file size, format, ETABS model characteristics.
4. Inspect Excel Workbooks:
   - `Kata.xlsm`, `Kata ENG.xlsm`, `Kata-GCAD.xlsm`, `Kata-ZwCad.xlsm`, `Iso_QHPlus.xls`.
   - Sheet names, layout, and exact cell coordinates for `Dam`, `Cot`, `Vach`, `Thong ke`.
   - Inspection of `xl/vbaProject.bin` (readable strings, module names, versus compiled/password-locked VBA P-code).
5. Inspect Visual Shape & Icon Libraries:
   - `dangthep\` (`00.wmf`..`67.wmf` + `.png`).
   - `Cot\` (`*.wmf`).
   - `Icon\` (`Ribbon\*.png`).
   - `IMG\` (`*.png`).
6. Record SHA-256, byte length, and structured inventory for each.

## 2026-10-18T05:33:55Z
Received from 41cec756-0576-4a33-970c-d780ca3d299e:
You are teamwork_preview_explorer_m1_3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m1_3\
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m1_3\DISPATCH.md
Read the authoritative request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (header ## 2026-10-18T05:29:18Z)
Read the project plan at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_26\plan.md

Constraints:
- 100% READ-ONLY on C:\kata_pro\
- No hallucination: cite exact counts, sheet names, cell coordinates, block definitions.

Tasks:
1. Decode AutoLISP & Menu Files:
   - Goi lenh.lsp (118 command aliases + 86 configuration lines)
   - Goi lenh - Gstarcad.lsp, Goi lenh - Zwcad.lsp
   - menukata.cui, menukata.cuix, menukata.mnr, menukata_light.mnr
2. Decode CAD Template & Model:
   - Iso_kata.dwg (119 block definitions, 12 layers, 15 dimstyles, text styles)
   - ISO_KATA.EDB
3. Decode Excel Workbooks:
   - Kata.xlsm, Kata ENG.xlsm, Kata-GCAD.xlsm, Kata-ZwCad.xlsm, Iso_QHPlus.xls
   - Exact sheet names, cell coordinates for Dam, Cot, Vach, Thong ke
   - Inspect xl/vbaProject.bin (readable strings vs compiled/password-locked VBA P-code)
4. Decode Visual Assets:
   - dangthep\ (00.wmf..67.wmf + .png)
   - Cot\ (*.wmf)
   - Icon\ (Ribbon\*.png)
   - IMG\ (*.png)
5. Write your complete report to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m1_3\report.md and write handoff.md. Report back via send_message to orchestrator_26.

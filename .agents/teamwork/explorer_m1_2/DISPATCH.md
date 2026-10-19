# DISPATCH: explorer_m1_2 (M1 Asset Staging & Path Rewriter Explorer)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_2\`
- Milestone: M1 — Standalone C:\HPKata\ Environment & Asset Migration
- Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`

## Mission
Investigate and produce the exact implementation specification for Features 2 & 3 (Asset staging and path rewriting):
1. Catalog the exact copy operations from `C:\kata_pro\` to `HPKata/Distribution/Assets/` and `C:\HPKata\`:
   - 11 root files: `Iso_kata.dwg`, `Kata.xlsm`, `Kata ENG.xlsm`, `Iso_QHPlus.xls`, `Goi lenh.lsp`, `GoiLenh_HPKata.lsp`, `menukata.cuix`, `menukata.cui`, `menukata.mnr`, `menukata_light.mnr`, `kata.ico`, `ISO_KATA.EDB`.
   - 4 directories: `dangthep\` (162 files), `Cot\` (13 files), `Icon\` (133 files including `Ribbon\`), `IMG\` (37 files), `Temp\`.
2. Exact path rewriting rules:
   - `Goi lenh.lsp`: replace `Kata_pro64*.dll` with `(command "netload" "C:\\HPKata\\HPKata.AutoCad.dll")`, replace `C:\kata_pro\menukata.cui(x)` with `C:\HPKata\menukata.cui(x)`, replace `;KATA_PRO@` with `;HPKATA@`.
   - `GoiLenh_HPKata.lsp`: replace `C:\kata_pro\HPKata.AutoCad.dll` with `C:\HPKata\HPKata.AutoCad.dll`, replace `;KATA_PRO@` with `;HPKATA@`.
   - `menukata.cui`: replace 14 occurrences of `C:\kata_pro\Icon\` with `C:\HPKata\Icon\`.
3. Design staging script / automated tool in `HPKata/Distribution/` or as a build step to ensure repeatability and verify 0 `kata_pro` occurrences in target files.

Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_2\handoff.md`.

## 2026-10-18T02:47:26Z
[Message from 3c323530-716e-4b3b-a14c-2a7fcee0c68a]
You are explorer_m1_2 (M1 Asset Staging & Path Rewriter Explorer).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_2\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_2\DISPATCH.md
Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a

Read ORIGINAL_REQUEST.md and DISPATCH.md first.
Your mission:
1. Provide the exact implementation specification for Features 2 & 3 (Asset staging and path rewriting).
2. Detail the exact staging process from C:\kata_pro\ to HPKata/Distribution/Assets/ and C:\HPKata\ (11 root files, dangthep\, Cot\, Icon\, IMG\, Temp\).
3. Detail exact string replacement rules for Goi lenh.lsp, GoiLenh_HPKata.lsp, and menukata.cui so that 0 kata_pro occurrences remain.
4. Design a robust C# / script tool in HPKata.Installer/Services/ or staging script to automate and verify this.
5. Write your report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_2\handoff.md
6. Send a message to parent (3c323530-716e-4b3b-a14c-2a7fcee0c68a) when done.

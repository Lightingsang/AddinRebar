# Dispatch: Explorer M5-3 (Lisp Loader, Scheduling Commands & Headless Testing)

**Agent**: `explorer_m5_3` (`teamwork_preview_explorer`)
**Parent**: `orchestrator_16` (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m5_3\`
**Date**: 2026-10-17

## Mandatory Reading
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` (Sections 2, 3, 7, 8)
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
4. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md`

## Mission
Investigate and design the AutoLISP companion loader, scheduling/export commands, and headless testing strategy:
1. AutoLISP Loader: Design `GoiLenh_HPKata.lsp` matching `Goi lenh.lsp` lines 1–151:
   - Command aliases (e.g. `(defun c:vt () (command "ve_thep"))`, `(defun c:sua () (command "sua_thep"))`, etc.).
   - Auto-loading of `HPKata.AutoCad.dll` via `(command "_.NETLOAD" ...)` or `PackageContents.xml`.
   - Persistence of the 86 configuration lines (lines 152–237) in `GoiLenh_HPKata.lsp` with round-trip compatibility with `KataConfigParser`.
2. Scheduling & Export Command Handlers:
   - `tkt` (`thong_ke_thep`): Scan drawing rebar entities -> aggregate schedule rows -> insert CAD schedule table block with shape sketches.
   - `ththep` (`tong_hop_thep`): Group diameters, compute total mass by TCVN bins, insert summary table.
   - `tkco` (`thong_ke_coupler`): Extract couplers from notes, insert coupler table.
   - `tohopthep` (`to_hop_thep`): Invoke 1D cutting stock solver, display results, export offcuts.
   - `toexcel` / `toexcelqh`: Export active schedule via `ClosedXmlExporter`.
   - `itt` (`in_tem_thep`): Generate bundle tags with QR codes via `QrTagGenerator`.
3. Headless Test Architecture: Design `HPKata.AutoCad.Tests.csproj` (net8.0-windows):
   - How to test AutoCAD command logic, XData serializers, and Lisp parser without requiring a live `acad.exe` process (using decoupled interfaces, mock document contexts, or data-driven parsing tests).
   - Validation tests for `GoiLenh_HPKata.lsp` syntax, command alias completeness, and 86-parameter integrity.
4. Clean Code structure: Ensure all classes $\le 300$ lines (Rule C2) and methods $\le 50$ lines (Rule M3).
5. Deliver structured analysis in `analysis.md` and complete handoff report in `handoff.md`.

## Mandatory Constraints
- `C:\kata_pro` is strictly read-only.
- Never inspect, decompile, or touch `Security.dll` or licensing code.
- Report all file paths and recommended class structures.


## 2026-10-17T03:25:03Z
You are explorer_m5_3, the Lisp Loader & Headless Testing Explorer for Milestone M5 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m5_3\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m5_3\DISPATCH.md.
3. Read the master specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md (Sections 2, 3, 7, 8).
4. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md.

YOUR TASK:
Investigate and design the AutoLISP companion loader, scheduling/export commands, and headless testing strategy:
1. AutoLISP Loader: Design GoiLenh_HPKata.lsp matching Goi lenh.lsp lines 1–151:
   - Command aliases (e.g. (defun c:vt () (command "ve_thep")), (defun c:sua () (command "sua_thep")), etc.).
   - Auto-loading of HPKata.AutoCad.dll via (command "_.NETLOAD" ...) or PackageContents.xml.
   - Persistence of the 86 configuration lines (lines 152–237) in GoiLenh_HPKata.lsp with round-trip compatibility with KataConfigParser.
2. Scheduling & Export Command Handlers:
   - tkt (thong_ke_thep): Scan drawing rebar entities -> aggregate schedule rows -> insert CAD schedule table block with shape sketches.
   - ththep (tong_hop_thep): Group diameters, compute total mass by TCVN bins, insert summary table.
   - tkco (thong_ke_coupler): Extract couplers from notes, insert coupler table.
   - tohopthep (to_hop_thep): Invoke 1D cutting stock solver, display results, export offcuts.
   - toexcel / toexcelqh: Export active schedule via ClosedXmlExporter.
   - itt (in_tem_thep): Generate bundle tags with QR codes via QrTagGenerator.
3. Headless Test Architecture: Design HPKata.AutoCad.Tests.csproj (net8.0-windows):
   - How to test AutoCAD command logic, XData serializers, and Lisp parser without requiring a live acad.exe process (using decoupled interfaces, mock document contexts, or data-driven parsing tests).
   - Validation tests for GoiLenh_HPKata.lsp syntax, command alias completeness, and 86-parameter integrity.
4. Clean Code structure: Ensure all classes <= 300 lines (Rule C2) and methods <= 50 lines (Rule M3).
5. Deliver structured analysis in analysis.md and complete handoff report in handoff.md.

MANDATORY CONSTRAINTS:
- C:\kata_pro is strictly read-only.
- Never inspect, decompile, or touch Security.dll or licensing code.

Send a message to orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6) when complete.

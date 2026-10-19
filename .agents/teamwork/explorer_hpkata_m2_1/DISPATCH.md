# Dispatch Specification — M2 Core Excel Engine Explorer (explorer_hpkata_m2_1)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_1\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T02:28:56Z`)
- Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Feature 5)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`

## Mission: Feature 5 — Bi-Directional Excel Engine in HPKata.Core
Investigate `HPKata.Core/Excel/` and architect the complete bi-directional Excel integration for `Kata.xlsm` (`Dam`, `Cot`, `Vach`, `Thong_ke`) and `Iso_QHPlus.xls`.

## Detailed Requirements:
1. **Engine Architecture**:
   - Primary: Active desktop Excel COM attachment via `oleaut32!GetActiveObject("Excel.Application")` with late-binding reflection to avoid COM type mismatches across Office versions.
   - Fallback: Headless file access via `ClosedXML` reading/writing `KataInstallEnvironment.Current.KataWorkbookPath` and `IsoQhPlusPath`.
   - Modularity: Keep each `.cs` file `<= 250` lines (Rule C2 `<= 300`), methods `<= 50` lines (Rule M3).
2. **Sheet Coverage**:
   - `Dam`: beam spans, supports, continuous top/bottom bars, curtailment bars, stirrups, skin bars, hanger bars.
   - `Cot`: column stories, cross-sections (b, h), longitudinal rebar layout, ties/stirrups.
   - `Vach`: shear wall thickness, length, boundary cages, web vertical and horizontal mesh.
   - `Thong_ke` / `Iso_QHPlus.xls`: rebar schedule export.
3. **Data Contracts & Interfaces**:
   - `IKataExcelAdapter`, `KataExcelSessionProvider`, `KataBeamSheetReader/Writer`, `KataColumnSheetReader/Writer`, `KataWallSheetReader/Writer`, `KataQhPlusExporter`.
4. **Hermetic Test Strategy**:
   - Design tests in `HPKata.Core.Tests` using `KataInstallEnvironment.UseRootDirectory(tempDir)` to verify round-trip read/write of sample workbooks without requiring Excel installed.

Write your findings, exact file layouts, method signatures, and class designs to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_1\handoff.md`
Send a message back to parent when done.

## 2026-10-18T03:24:39Z
Sender: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
Priority: MESSAGE_PRIORITY_HIGH
Content:
You are explorer_hpkata_m2_1 (M2 Core Excel Engine Explorer).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_1\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_1\DISPATCH.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Target Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md (Milestone M2, Feature 5).
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a

Read ORIGINAL_REQUEST.md and DISPATCH.md first.
Your mission:
1. Investigate HPKata.Core/Excel/ and existing Excel handlers, data structures, and models.
2. Architect the dual-mode Excel integration:
   - Primary: Active COM attachment to running Microsoft Excel instance via late binding reflection (avoiding hard Office version interop dependency).
   - Fallback: Headless ClosedXML access on KataInstallEnvironment.Current.KataWorkbookPath / IsoQhPlusPath.
3. Define exact data transfer objects and readers/writers for:
   - Sheet Dam (beam spans, cross sections, continuous top/bottom bars, curtailment bars, stirrups, skin bars, hanger bars).
   - Sheet Cot (column stories, dimensions b/h, longitudinal rebar layout, ties/stirrups).
   - Sheet Vach (wall thickness, length, boundary cages, web vertical/horizontal mesh).
   - Sheet Thong_ke / Iso_QHPlus.xls (rebar schedule export).
4. Strictly enforce file length <= 250 lines per .cs file (Rule C2 <= 300), methods <= 50 lines (Rule M3).
5. Design unit tests in HPKata.Core.Tests using KataInstallEnvironment.UseRootDirectory(...) for 100% isolated roundtrip verification.
6. Write your comprehensive report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_1\handoff.md
7. Send a message to parent (3c323530-716e-4b3b-a14c-2a7fcee0c68a) when done.

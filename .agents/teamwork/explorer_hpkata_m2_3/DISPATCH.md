# Dispatch Specification — M2 AutoCAD & Revit Commands Explorer (explorer_hpkata_m2_3)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_3\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T02:28:56Z`)
- Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Features 7 & 8)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`

## Mission: Features 7 & 8 — AutoCAD & Revit Detailing Commands Excel Sync
Investigate detailing commands across `HPKata.AutoCad` and `HPKata.Revit`, designing their bi-directional connection to `C:\HPKata\Kata.xlsm`.

## Detailed Requirements:
1. **AutoCAD Detailing Commands**:
   - `ve_dam` / `vd`: standard beam drawing from loaded beam parameters.
   - `ve_dam_shop` / `vds`: shop drawing beam detailing with rebar curtailment, lap splices, and elevation + cross-sections.
   - `loadbeam`: loads beam parameters from `Kata.xlsm` (`Dam` sheet) into AutoCAD memory/document cache.
   - `ve_cot` / `vc`: standard column drawing.
   - `ve_cot_shop` / `vcs`: column shop drawing detailing with story-by-story longitudinal bars and ties.
   - `loadcolumn`: loads column parameters from `Kata.xlsm` (`Cot` sheet).
   - `ve_vach` / `vv`: shear wall drawing.
   - `ve_vach_shop` / `vvs`: wall shop drawing with boundary cages and web mesh.
   - `loadwall`: loads wall parameters from `Kata.xlsm` (`Vach` sheet).
   - `toexcel`: exports detailing geometry back to `Kata.xlsm`.
   - `toexcelqh`: exports schedule to `Iso_QHPlus.xls`.
   - Ensure drawing entities carry `KATA` XData, proper layers (`THEP_DOC`, `THEP_DAI`, `TEXT`, `DIM`), and callout annotations.
2. **Revit Detailing Commands**:
   - `FramingRebarCommands`: creates 3D Revit `Rebar` elements in selected structural framing elements from `Kata.xlsm` data.
   - `ColumnRebarCommands`: creates 3D column vertical bars and stirrups in structural columns from `Kata.xlsm` data.
   - `WallRebarCommands`: creates vertical, horizontal, and boundary rebar in structural walls from `Kata.xlsm` data.
3. **Architecture & Clean Code**:
   - Commands delegate Excel I/O to `IKataExcelAdapter` via `KataInstallEnvironment.Current`.
   - Each `.cs` file `<= 300` lines (Rule C2), methods `<= 50` lines (Rule M3), 0 warnings.
4. **Testing**:
   - Design headless / mocked command execution tests in `HPKata.AutoCad.Tests` and `HPKata.Revit.Tests`.

Write your findings, command mapping, code changes, and test specs to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_3\handoff.md`
Send a message back to parent when done.


## 2026-10-18T03:24:39Z
You are explorer_hpkata_m2_3 (M2 Host Commands Explorer).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_3\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_3\DISPATCH.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Target Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md (Milestone M2, Features 7 & 8).
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a

Read ORIGINAL_REQUEST.md and DISPATCH.md first.
Your mission:
1. Investigate AutoCAD detailing commands:
   - Beam: ve_dam/vd, ve_dam_shop/vds, loadbeam.
   - Column: ve_cot/vc, ve_cot_shop/vcs, loadcolumn.
   - Wall: ve_vach/vv, ve_vach_shop/vvs, loadwall.
   - Excel export: toexcel, toexcelqh.
2. Investigate Revit detailing commands:
   - FramingRebarCommands, ColumnRebarCommands, WallRebarCommands.
3. Design their integration with IKataExcelAdapter to read/write C:\HPKata\Kata.xlsm and generate real geometry (shop drawings in CAD, 3D Rebar in Revit).
4. Verify Clean Code rules (<= 300 lines per file, methods <= 50 lines, 0 warnings).
5. Design command execution tests in HPKata.AutoCad.Tests and HPKata.Revit.Tests.
6. Write your comprehensive report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_3\handoff.md
7. Send a message to parent (3c323530-716e-4b3b-a14c-2a7fcee0c68a) when done.

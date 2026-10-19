# DISPATCH — Milestone M6: Revit Rebar Parameter & Shape Adapter Explorer

**Assigned Agent**: `explorer_m6_2` (`teamwork_preview_explorer`)  
**Parent Orchestrator**: `orchestrator_16` (Conversation ID: `4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m6_2\`  
**Target Project**: `HPKata/HPKata.Revit/`  
**Date**: 2026-10-17  

---

## 1. Context & Mission

Milestones M1–M5 are complete.
We are now initiating **Milestone M6: HPKata.Revit 2026 Add-in**.
Your task is to explore and design the Revit Rebar Parameter & Shape Adapter subsystem:
1. **Parameter Bridge (Feature 19)**:
   - Map between Revit `Autodesk.Revit.DB.Structure.Rebar` and `HPKata.Core.Models.RebarItem`:
     - Member / Partition: `BuiltInParameter.REBAR_PARTITION` <-> `TenCauKien` (`{ten_ck}`)
     - Rebar Number / Mark: `BuiltInParameter.REBAR_NUMBER` <-> `SoHieu` (`{stt}`)
     - Bar Count: `rebar.Quantity` <-> `SoLuong` (`{sl}`)
     - Bar Diameter: `rebar.LookupParameter("Bar Diameter")` or `RebarBarType.BarNominalDiameter` <-> `DuongKinh` (`{phi}`)
     - Total Bar Length: `rebar.TotalLength` (converted from feet to mm: $\times 304.8$) <-> `ChieuDai` (`{L}`)
     - Hook definitions: `rebar.GetHookTypeId(0)` and `rebar.GetHookOrientation(0)`
2. **Revit Internal Units to Metric Quarantine**:
   - Revit internal unit is decimal feet ($1\text{ ft} = 304.8\text{ mm}$).
   - `HPKata.Core` is strictly metric millimeters ($mm$).
   - Design a dedicated converter class `RevitUnitConverter.cs`:
     - `FeetToMm(double feet) => feet * 304.8`
     - `MmToFeet(double mm) => mm / 304.8`
     - Strict rule: ZERO conversion factors outside `HPKata.Revit`!
3. **Rebar Shape Adapter (Revit Shape to Kata 85 Shapes)**:
   - Revit uses `RebarShape` with parameter dimensions ($A, B, C, D, E, F$).
   - Design `RevitShapeAdapter.cs`:
     - Map standard Revit rebar shapes (M_00 straight, M_02 L-bar, M_11 U-stirrup, M_T1 closed tie, etc.) to `HPKata.Core.Shapes.RebarShapeDefinition` (`dang01`, `dang02`, `dang06`, `dang25`, etc.).
     - Extract curve lengths and segment parameters into metric $mm$.
4. **Clean Code Governance**:
   - Class length $\le 300$ lines (Rule C2), method body $\le 50$ lines (Rule M3), 0 dead code, 0 unannotated catch blocks.
5. **Deliverables**:
   - Deliver `analysis.md` and `handoff.md` with complete mapping tables and adapter designs.


## 2026-10-17T04:35:09Z
You are explorer_m6_2, the Revit Rebar Parameter & Shape Adapter Explorer for Milestone M6 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m6_2\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m6_2\DISPATCH.md.
3. Read the master specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md (Sections 4, 9).
4. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md.

YOUR TASK:
Investigate and design the Revit Rebar parameter bridge, shape adapters, and unit conversion quarantine:
1. Parameter Bridge (Feature 19): Map Revit Rebar parameters (REBAR_PARTITION <-> ten_ck, REBAR_NUMBER <-> stt, Quantity <-> sl, BarNominalDiameter <-> phi, TotalLength <-> L) to HPKata.Core models.
2. Unit Conversion Quarantine: Design RevitUnitConverter (1 ft = 304.8 mm; strict rule: ZERO feet/mm conversions outside HPKata.Revit!).
3. Rebar Shape Adapter: Map Revit standard RebarShape codes and dimension curves (A, B, C, D, E, F) to HPKata 85 shape definitions (dang01..dang67).
4. Clean Code structure: Ensure all classes <= 300 lines (Rule C2) and methods <= 50 lines (Rule M3).
5. Deliver structured analysis in analysis.md and complete handoff report in handoff.md.

MANDATORY CONSTRAINTS:
- C:\kata_pro is strictly read-only.
- Never inspect, decompile, or touch Security.dll or licensing code.

Send a message to orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6) when complete.

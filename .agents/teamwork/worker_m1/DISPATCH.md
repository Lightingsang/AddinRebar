# Dispatch: Worker M1 (Core DTOs, Notation Parser & Sheet Parser)

## 2026-09-27T16:11:27Z

- **Role**: teamwork_preview_worker
- **Milestone**: M1 - Core DTOs & Sheet Parser
- **Scope**:
  1. Create domain models in `HPRebar/HPRebar.Core/KataRebar/Models/`:
     - `KataBeamRebarSpec.cs`: beam metadata, header parameters (Name, count, b, h, slab thickness, lap multipliers, cutoff ratios, covers, continuous top/bottom, supports list, spans list).
     - `KataSpanRebarSpec.cs`: span index, length, bottom extra bars layers 1 & 2, side/skin bars, stirrup specs.
     - `KataSupportRebarSpec.cs`: support index, column width, top extra bars layers 1-4, console/cantilever indicator.
     - `KataBarItem.cs`: count, diameter, layer, offset, notation text.
     - `KataStirrupSpec.cs`: diameter, support spacing, midspan spacing, branch types (closed □, cap U, cross tie C).
     - `KataRebarCurve.cs`: bar type, role, polyline (reusing Point3/Polyline3 from HPRebar.Core.BeamRebar.Models).
     - `KataStirrupZoneResult.cs` & `KataRebarLayoutResult.cs`.
  2. Create parsers in `HPRebar/HPRebar.Core/KataRebar/Parsers/`:
     - `KataBarNotationParser.cs`: parses notation strings ('2f18', '3f20', 'a150', '2f20;2f16', '-50;5f20', etc.).
     - `IKataDamCellAccessor.cs`: abstraction interface for cell access (GetText, GetDouble, GetInt).
     - `KataCellTable.cs`: in-memory 2D grid implementing `IKataDamCellAccessor`.
     - `KataDamSheetParser.cs`: parses the grid into `KataBeamRebarSpec`.
  3. Create Excel readers in `HPRebar/HPRebar/KataRebar/Excel/`:
     - `ComKataDamReader.cs`: active Excel COM reader using Windows ROT / late binding (reading Range["A1:BZ30"].Value2 in batch into `KataCellTable`).
     - `ClosedXmlKataDamReader.cs`: disk .xlsm reader fallback using ClosedXML.
- **Reference**:
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\report.md`
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_2\report.md`
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\PROJECT.md`
  - Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (under ## 2026-09-27T15:57:37Z)
- **Constraint**: `HPRebar.Core` must remain pure `netstandard2.0` with 0 references to `Autodesk.Revit.*`.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

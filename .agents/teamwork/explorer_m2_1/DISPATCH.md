# DISPATCH: explorer_m2_1 (M2 Excel Core Engine Explorer)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_1\`
- Milestone: M2 — Bi-Directional Excel Integration & Detailing Parity (Feature 5)
- Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md`
- Test Ready Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_READY.md`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`

## Mission
Design the complete C# implementation specification for Feature 5 (Bi-directional Excel Engine in `HPKata.Core/Excel/`):
1. Interface `IKataExcelAdapter` and session provider `KataExcelSessionProvider` supporting:
   - Primary: attaching to open desktop Microsoft Excel via `oleaut32!GetActiveObject` + late-binding reflection (`Type.InvokeMember`).
   - Fallback: headless ClosedXML reading/writing directly to `KataInstallEnvironment.Current.KataWorkbookPath` (`C:\HPKata\Kata.xlsm`).
2. Modular readers & writers (each file <= 250 lines, methods <= 50 lines):
   - `KataBeamSheetReader.cs` & `KataBeamSheetWriter.cs` for sheet `Dam` (headers B3:B10, row 10 columns/spans, rows 11-27 reinforcement matrix).
   - `KataColumnSheetReader.cs` & `KataColumnSheetWriter.cs` for sheet `Cot` (headers C1:C5, rows 7+ story reinforcement).
   - `KataWallSheetReader.cs` & `KataWallSheetWriter.cs` for sheet `Vach` (headers C1:C5, rows 7+ story wall reinforcement).
   - `KataQhPlusExporter.cs` for `Iso_QHPlus.xls` / `Thong_ke`.
3. Design unit tests for `HPKata.Core.Tests/Excel/` verifying roundtrip reading and writing of `Dam`, `Cot`, `Vach` via ClosedXML fallback under test isolation (`KataInstallEnvironment.UseRootDirectory`).

Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_1\handoff.md`.

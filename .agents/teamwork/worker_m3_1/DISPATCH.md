# DISPATCH — worker_m3_1

## Task Assignment
Implement Milestone M3: Revit Element Geometry Reader & Safe Picker Fallback.

## Paths to Required Context Files
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (entry dated 2026-10-17T14:27:21Z)
- Project Plan: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
- Survey 3 Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_3\handoff.md`

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Exclusive Write Ownership
You own and may edit exclusively:
- `HPKata/HPKata.Revit/Services/RevitElementGeometryReader.cs` (Create new)
- `HPKata/HPKata.Revit/Commands/FramingRebarCommands.cs` (Modify)
- `HPKata/HPKata.Revit/Commands/ColumnRebarCommands.cs` (Modify)
- `HPKata/HPKata.Revit/Commands/WallRebarCommands.cs` (Modify)
- `HPKata/HPKata.Revit.Tests/Services/RevitElementGeometryReaderTests.cs` (Create new)

## Detailed Requirements

### 1. Create `HPKata/HPKata.Revit/Services/RevitElementGeometryReader.cs`
- Define dimension records:
  ```csharp
  namespace HPKata.Revit.Services;

  public readonly record struct FramingDimensions(string Mark, double WidthMm, double HeightMm, double LengthMm);
  public readonly record struct ColumnDimensions(string Mark, double WidthMm, double DepthMm, double HeightMm);
  public readonly record struct WallDimensions(string Mark, double ThicknessMm, double LengthMm, double HeightMm);
  public readonly record struct ElementDimensionData(double WidthMm, double HeightMm, double LengthMm);
  ```
- Expose methods:
  - `public static FramingDimensions ReadFramingDimensions(FamilyInstance beam)`
  - `public static ColumnDimensions ReadColumnDimensions(FamilyInstance column)`
  - `public static WallDimensions ReadWallDimensions(Wall wall)`
  - `public static ElementDimensionData ResolveDimensions(double? primaryWidthFt, double? primaryHeightFt, double? lengthFt, double? bboxWidthFt, double? bboxHeightFt, double? bboxLengthFt, double defaultWidthMm, double defaultHeightMm, double defaultLengthMm)`
- Extraction & Fallback Cascade:
  - Width: Try BuiltInParameter (`STRUCTURAL_SECTION_COMMON_WIDTH`), then named parameters on instance and symbol (`b`, `B`, `Width`, `b (Width)`, `Rộng`), then bounding box width, then default.
  - Height/Depth: Try BuiltInParameter (`STRUCTURAL_SECTION_COMMON_HEIGHT`), then named parameters (`h`, `H`, `Height`, `Depth`, `h (Height)`, `Cao`), then bounding box depth/height, then default.
  - Length: LocationCurve Length or `INSTANCE_LENGTH_PARAM` / `WALL_USER_HEIGHT_PARAM`, then bounding box, then default.
- Unit Conversion Rule R5 (MANDATORY):
  - Every feet-to-mm conversion MUST use `HPKata.Revit.Units.RevitUnitConverter.FeetToMm(...)`.
  - The literal `304.8` MUST NEVER appear in `RevitElementGeometryReader.cs`.
- Method size <= 50 lines (Rule M3), file size <= 300 lines (Rule C2).

### 2. Update `FramingRebarCommands.cs`, `ColumnRebarCommands.cs`, `WallRebarCommands.cs`
- Implement pre-selection check via `uidoc.Selection.GetElementIds()` matching element category (`OST_StructuralFraming`, `OST_StructuralColumns`, `OST_Walls`).
- If no pre-selection, wrap `uidoc.Selection.PickObject(...)` in try/catch catching `Autodesk.Revit.Exceptions.OperationCanceledException`.
- In the catch block: annotate with a comment explaining that ESC triggers manual input mode, set `targetElement = null`, and continue without aborting.
- If `targetElement != null`, extract actual dimensions using `RevitElementGeometryReader` and assign them to the ViewModel properties (Mark, Width, Height/Depth, Length) before creating the view.
- Show window modally: `bool? dialogResult = RevitWindowHelper.ShowModal(view, uiapp);`
- On `dialogResult == true`:
  - If `targetElement != null`: wrap rebar generation in a Revit transaction (`using var tr = new Transaction(doc, ...)`), start, call `*RebarGenerator.Generate*`, commit, and show completion message.
  - Return `Result.Succeeded`.
- If `dialogResult != true`: return `Result.Cancelled`.

### 3. Create `HPKata.Revit.Tests/Services/RevitElementGeometryReaderTests.cs`
- Unit tests for `RevitElementGeometryReader.ResolveDimensions`:
  - Primary parameter priority over bounding box and defaults.
  - Bounding box fallback when parameters are null or zero.
  - Default values fallback when parameters and bounding box are null.
  - Accurate conversion using `RevitUnitConverter.FeetToMm`.

### 4. Build & Test Verification
Run:
`dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
`dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
Ensure 0 warnings and 100% tests pass.

### 5. Handoff
Write `handoff.md` in your working directory `.agents/teamwork/worker_m3_1/handoff.md` with:
- Summary of changes made
- Build and test command output
- Verification of Rule R5 (no `304.8` literal)
- Notify `orchestrator_19`.

## 2026-10-17T15:39:21Z
Message from 6ca2dcc3-e478-4dfe-bcd3-44e77f9ffa52:
You are worker_m3_1, the implementation worker for Milestone M3 (Revit Element Geometry Reader & Safe Picker Fallback).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_1\
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_1\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-17T14:27:21Z).
Read the project plan at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Read Explorer 3's survey report at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_3\handoff.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your exclusive write ownership:
- HPKata/HPKata.Revit/Services/RevitElementGeometryReader.cs (create new)
- HPKata/HPKata.Revit/Commands/FramingRebarCommands.cs (modify)
- HPKata/HPKata.Revit/Commands/ColumnRebarCommands.cs (modify)
- HPKata/HPKata.Revit/Commands/WallRebarCommands.cs (modify)
- HPKata/HPKata.Revit.Tests/Services/RevitElementGeometryReaderTests.cs (create new)

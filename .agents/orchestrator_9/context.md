# Orchestrator 9 Context - Kata Rebar Feature

Project: Automated 3D Concrete Beam Reinforcement in Revit 2026 from Kata.xlsm (sheet 'Dam')
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (under ## 2026-09-27T15:57:37Z)
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar
Repo Root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Orchestrator Workspace: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9

## Requirements Overview
- R1. Kata Dam Sheet Data Parser (`HPRebar.Core`):
  - Parse structural and reinforcement data from sheet `Dam` of `Kata.xlsm` into strongly-typed DTO models (`KataBeamRebarSpec`):
    - Beam header: Name (`B3`), count (`B4`), dimensions b x h (`B5:B6`), slab thickness (`B7`), level (`B10`), anchorage length multipliers (`G2:G3`, `H3`, `H5`).
    - Continuous top and bottom main bars (`B11`, `B12`, e.g., `2f18`, `3f20`).
    - Additional span and support bars (top extra layers in rows 13–16 across support columns; bottom extra layers in rows 17–18 across spans).
    - Side bars / torsion bars / web skin reinforcement (`E5`, `G4`, row 20).
    - Stirrups: Bar diameter (`G6`), spacing near supports (`G7`, e.g., `a150`), spacing at mid-span (`G8`, e.g., `a200`), stirrup types from rows 25–27 (closed stirrup, cap stirrup U, cross tie C).
    - Dual data source capability: primary reading from active Excel via COM (`oleaut32`), with graceful fallback to ClosedXML reading `.xlsm` from disk.
- R2. Rebar Geometry & Distribution Calculator (`HPRebar.Core`):
  - Convert `KataBeamRebarSpec` into explicit 3D rebar curves, dimensions, and distribution arrays within the beam coordinate system:
    - Top & bottom continuous bar lengths with anchorage into end supports (hook 90° or lap length based on d).
    - Support top additional bar cutoffs based on span ratios (L/4, L/3, or sheet `Dam` settings in `H3:H5`).
    - Mid-span bottom additional bar start/end cutoffs relative to clear span faces.
    - Stirrup distribution along each span (support dense zones L/4, mid-span sparse zones L/2).
    - Concrete cover offsets and side-bar positions.
    - Zero references to `Autodesk.Revit.*` in `HPRebar.Core` (`netstandard2.0`).
- R3. Revit 3D Rebar Generation & Idempotent Update (`HPRebar`):
  - In Revit 2026 (.NET 8):
    - Match beam selection on Revit plan with beam data in sheet `Dam` by name (`B3`) and span count.
    - Smart RebarBarType resolution: Resolve bar designations (e.g. `f18`, `d8`) to project `RebarBarType` by numerical diameter (~18 mm +/- 0.5 mm) and standard `RebarHookType`.
    - Create native `Rebar` elements inside the host beam using Revit API (`Rebar.CreateFromCurves` / `Rebar.CreateFromCurvesAndShape`).
    - Idempotency & Cleanup: Tag all generated rebars with an identifier parameter (e.g., `Comments` = `"HPRebar_Kata_{BeamName}"`). On re-run, automatically locate and delete previous Kata rebars for that beam run before regenerating to prevent duplication.
- R4. User Interface & Ribbon Integration (`HPRebar`):
  - Add a "Kata Rebar" button to the existing HPRebar ribbon panel (adjacent to "Kata Export").
  - Modeless/Modal WPF Dialog matching the repo's ThemeDark/ThemeLight dynamic styling and MaterialDesign 5.3.2 tokens.
  - Dialog previews the parsed beam specification (spans, top/bottom bars, stirrups), verifies matching host beam, displays mapped `RebarBarType`s, and provides an action button to execute rebar creation inside a Revit `Transaction`.

## Acceptance Criteria
- xUnit test suite in `HPRebar.Core.Tests` verifies `KataDamSheetReader` and `KataRebarCalculator`.
- `dotnet test HPRebar.Core.Tests` passes 100% green.
- `dotnet build HPRebar.slnx -c Debug.R26` compiles cleanly with 0 errors.
- `HPRebar.Core` remains pure `netstandard2.0` with 0 Revit references.
- End-to-end Revit workflow: reading active Excel sheet `Dam` populates preview UI, generates 3D rebar in Revit, and re-running cleanly replaces prior Kata rebars without duplication.

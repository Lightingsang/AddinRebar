# Original User Request

## 2026-09-27T15:57:37Z

Implement the **Kata Rebar** feature in the `HPRebar` ecosystem, enabling automated generation of 3D concrete beam reinforcement in Revit 2026 based on structural calculation and detailing data read from sheet `Dam` of `Kata.xlsm` (via active COM or file fallback).

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar
Integrity mode: development

## Requirements

### R1. Kata Dam Sheet Data Parser (`HPRebar.Core`)
Parse structural and reinforcement data from sheet `Dam` of `Kata.xlsm` into strongly-typed DTO models (`KataBeamRebarSpec`):
- Beam header parameters: Name (`B3`), count (`B4`), dimensions $b \times h$ (`B5:B6`), slab thickness (`B7`), level (`B10`), anchorage length multipliers (`G2:G3`, `H3`, `H5`).
- Continuous top and bottom main bars (`B11`, `B12`, e.g., `2f18`, `3f20`).
- Additional span and support bars (top extra layers in rows 13–16 across support columns; bottom extra layers in rows 17–18 across spans).
- Side bars / torsion bars / web skin reinforcement (`E5`, `G4`, row 20).
- Stirrups: Bar diameter (`G6`), spacing near supports (`G7`, e.g., `a150`), spacing at mid-span (`G8`, e.g., `a200`), stirrup types from rows 25–27 (closed stirrup □, cap stirrup U, cross tie C).
- Provide dual data source capability: primary reading from active Excel via COM (`oleaut32`), with graceful fallback to ClosedXML reading `.xlsm` from disk.

### R2. Rebar Geometry & Distribution Calculator (`HPRebar.Core`)
Convert `KataBeamRebarSpec` into explicit 3D rebar curves, dimensions, and distribution arrays within the beam coordinate system:
- Calculate top and bottom continuous bar lengths with anchorage into end supports (hook 90° or lap length based on $d$).
- Calculate support top additional bar cutoffs based on span ratios ($L/4$, $L/3$, or sheet `Dam` settings in `H3:H5`).
- Calculate mid-span bottom additional bar start/end cutoffs relative to clear span faces.
- Segment stirrup distribution along each span (support dense zones $L/4$, mid-span sparse zones $L/2$).
- Calculate concrete cover offsets and side-bar positions.
- All core calculations must reside in `HPRebar.Core` (`netstandard2.0`) with 0 references to `Autodesk.Revit.*`.

### R3. Revit 3D Rebar Generation & Idempotent Update (`HPRebar`)
In Revit 2026 (.NET 8):
- Match beam selection by user on Revit plan with beam data in sheet `Dam` by name (`B3`) and span count.
- Smart RebarBarType resolution: Resolve bar designations (e.g., `f18`, `d8`) to project `RebarBarType` by numerical diameter ($\approx 18\text{ mm} \pm 0.5\text{ mm}$) and standard `RebarHookType`.
- Create native `Rebar` elements inside the host beam using Revit API (`Rebar.CreateFromCurves` / `Rebar.CreateFromCurvesAndShape`).
- Idempotency & Cleanup: Tag all generated rebars with an identifier parameter (e.g., `Comments` = `"HPRebar_Kata_{BeamName}"`). On re-run, automatically locate and delete previous Kata rebars for that beam run before regenerating to prevent duplication.

### R4. User Interface & Ribbon Integration (`HPRebar`)
- Add a "Kata Rebar" button to the existing HPRebar ribbon panel (adjacent to "Kata Export").
- Modeless/Modal WPF Dialog matching the repo's ThemeDark/ThemeLight dynamic styling and MaterialDesign 5.3.2 tokens.
- Dialog previews the parsed beam specification (spans, top/bottom bars, stirrups), verifies matching host beam, displays mapped `RebarBarType`s, and provides an action button to execute rebar creation inside a Revit `Transaction`.

## Acceptance Criteria

### Unit Testing & Core Verification (No Revit Required)
- [ ] Comprehensive xUnit test suite in `HPRebar.Core.Tests` verifies `KataDamSheetReader`: parses sample sheet `Dam` rows (continuous bars, additional top/bottom bars, stirrups, skin bars).
- [ ] xUnit test suite verifies `KataRebarCalculator`: calculates bar cutoffs, anchorage hooks, and stirrup distributions for single-span, multi-span continuous, and cantilever beams.
- [ ] `dotnet test HPRebar.Core.Tests` passes with 100% green tests.

### Build & Integration Verification
- [ ] `dotnet build HPRebar.slnx -c Debug.R26` compiles cleanly with 0 errors.
- [ ] No circular dependencies or forbidden references (`HPRebar.Core` must remain pure `netstandard2.0` with no Revit references).

### End-to-End Revit Workflow
- [ ] Reading active Excel `Kata.xlsm` sheet `Dam` populates the preview UI with beam dimensions and rebar layout.
- [ ] Executing "Tạo thép / Generate Rebar" creates actual 3D `Rebar` instances in the Revit host beam with correct diameter, layout rules, and covers.
- [ ] Running the command a second time on the same beam cleanly replaces prior Kata rebar without duplicates.

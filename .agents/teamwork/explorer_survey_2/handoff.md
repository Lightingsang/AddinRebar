# Handoff Report — explorer_survey_2 (Core Beam Rebar Math Explorer)

- **Role**: teamwork_preview_explorer (Core Beam Rebar Math Explorer)
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_2\`
- **Recipient**: Parent Agent (`aa8876fc-b61d-4725-aacd-616632eb9cc0`)
- **Date**: 2026-09-27
- **Handoff Type**: Hard (Survey & Investigation Complete)

---

## 1. Observation

1. **Existing Core Models & Tests in `HPRebar.Core/BeamRebar/`**:
   - `HPRebar.Core/BeamRebar/Models/Point3.cs:7-58`: Readonly struct $(X, Y, Z)$ in millimetres representing longitudinal station, transverse offset, and elevation.
   - `HPRebar.Core/BeamRebar/Models/Polyline3.cs:49-69`: `Simplify(double minSegmentLength = 1.0)` merges vertices closer than 1.0 mm to protect against Revit's short curve tolerance crash (~0.78 mm / 1/32").
   - `HPRebar.Core/BeamRebar/Models/BarPolyline.cs:10-77`: Rebar centerline record containing `Polyline3`, diameter, layer, hooks (`HookAngle.Hook90`), and transverse $Y$ offset.
   - `HPRebar.Core/BeamRebar/Calculators/BeamStirrupDistributionCalculator.cs:104-224`: Implements 3-zone distribution (`ThreeZoneL4`) with support dense length $L_n/4$, start offset 50 mm, and midspan sparse zone symmetrically placed in the remaining gap with boundary condition $s_2/2 < d_{\text{boundary}} \le s_2$ to eliminate bar clashes.
   - `HPRebar.Core/BeamRebar/Calculators/BeamMainBarCalculator.cs:21-45`: Implements transverse bar centering across beam width:
     $$Y_0 = -(b/2 - c - d_{\text{stirrup}} - d_{\text{bar}}/2), \quad Y_{n-1} = +(b/2 - c - d_{\text{stirrup}} - d_{\text{bar}}/2)$$
   - `HPRebar.Core/BeamRebar/Calculators/BeamMainBarCalculator.cs:76-86`: Implements 90° exterior anchorage hook length:
     $$L_{\text{hook}} = \min(h - 2c - 2d_{\text{stirrup}}, \max(30 d_{\text{bar}}, 200\text{ mm}))$$
   - `HPRebar.Core/BeamRebar/Calculators/BeamAdditionalBarCalculator.cs:22-343`: Calculates top negative-moment bars with cutoff ratio $L/3$ (Layer 1) and $L/4$ (Layer 2), with 90° hooks at exterior supports.
   - `HPRebar.Core/BeamRebar/Calculators/BeamAdditionalBarCalculator.cs:360-451`: Calculates bottom positive-moment bars with cutoff ratio $L/7$ from clear span faces.
   - `HPRebar.Core/BeamRebar/Calculators/BeamSideBarCalculator.cs:20-46`: Enforces TCVN 5574:2018 / ACI 318 side reinforcement when $h \ge 700\text{ mm}$ with vertical spacing $\le 300\text{ mm}$.
   - All 452 unit tests in `HPRebar.Core.Tests` pass 100%:
     `dotnet test HPRebar.Core.Tests` $\to$ `total: 452, failed: 0, succeeded: 452, duration: 888ms`.

2. **Sheet `Dam` in `C:\kata_pro\Kata.xlsm`**:
   - Verified live via Python ZIP/XML inspection of `C:\kata_pro\Kata.xlsm` (`inspect_dam_details.py`):
     - `B3`: Beam name (e.g., `"B01"`).
     - `B5:B6`: $h = 1100\text{ mm}, b = 500\text{ mm}$.
     - `G2:G3`: Tension lap factor $= 40d$, compression anchorage $= 30d$.
     - `H3:H5`: Cutoff ratios $= 0.2$ ($L/5$) and $0.25$ ($L/4$).
     - `I3:I5`: Data validation: `"L từ mép cột"` vs `"L từ tâm cột"`.
     - `J9`: Cover `"50/25"` (main bar cover 50 mm, stirrup cover 25 mm).
     - `B11:B12`: Top continuous `"6f25"`, bottom continuous `"6f25"`.
     - `Row 10`: Alternating `Cột | Nhịp | Cột | Nhịp...`
     - `Row 11`: Spans from 2000 mm to 10400 mm; supports 400 mm, with 0 mm joints for cantilever/consoles.
     - `Rows 13–16`: Additional top bars across support columns (`6f25`, `6f20`, `2f20;2f16`).
     - `Rows 17–18`: Additional bottom bars in spans (`6f25`, `2f20`).
     - `Row 20`: Side bars / cốt giá (`0f12`, `1f12`).
     - `G6:G8`: Stirrup diameter 10 mm, support spacing `a150`, midspan spacing `a200`.
     - `Rows 25–27`: Stirrup shapes: closed outer hoop (□), open cap (U), cross-tie (C).

3. **Revit Add-In Coordinate Transformation Adapters**:
   - `HPRebar/HPRebar/BeamRebar/Service/PointMapper.cs:10-55`: Maps local millimetres to Revit world coordinates (decimal feet) using origin and orthogonal basis vectors $(X_{\text{axis}}, Y_{\text{transverse}}, Z_{\text{up}})$.
   - `HPRebar/HPRebar/KataExport/Service/KataAxisFrame.cs:12-70`: Projects Revit framing and supports onto a dominant positive 2D axis.

---

## 2. Logic Chain

1. **Reusability of Core Math**: Because `Point3`, `Vector3`, `Polyline3`, `Tolerance`, and `BeamStirrupDistributionCalculator` operate purely on numerical `double`s in local millimetres and target `netstandard2.0` (with zero references to `Autodesk.Revit.*`), they can be directly reused by the new `KataRebar` calculation engine without modification or duplication.
2. **Need for Dedicated Kata Models**: The existing `BeamRebar` feature uses `BeamMainBarSpec` and `BeamContinuousStack` configured for UI inputs. In contrast, Kata detailing data is organized around the alternating `Cột | Nhịp` layout of sheet `Dam`, requiring multi-layer compound bar notations (`2f20;2f16`), distinct cutoff reference points (face vs center from `I3/I5`), and multi-shape stirrups (□, U, C from rows 25–27). Therefore, dedicated models (`KataBeamRebarSpec`, `KataSpanRebarSpec`, `KataSupportRebarSpec`, `KataBarItem`) and a parser (`KataBarNotationParser`) are needed in `HPRebar.Core/KataRebar/Models/`.
3. **Decoupled 3D Curve Generation**: By having `KataRebarCalculator` output a complete `KataRebarLayoutResult` containing explicit 3D `Polyline3` curves for both longitudinal bars and stirrup loops, the entire geometric pipeline can be verified via xUnit unit tests in `HPRebar.Core.Tests` without needing Revit running.
4. **Revit Generation Safety**: By simplifying all polylines with `Polyline3.Simplify(1.0)` prior to handing them to the Revit add-in layer (`HPRebar/KataRebar/`), the engine eliminates the risk of `Application.ShortCurveTolerance` exceptions during `Rebar.CreateFromCurves`.

---

## 3. Caveats

1. **Cantilever Detailing**: In Kata, when a cantilever span exists (indicated by column width $0$ in row 11), top bars must run continuously to the cantilever end face with a 90° hook down, whereas bottom bars must stop at the interior support face. Our calculator handles this, but assumes standard cantilever behavior unless sheet `Dam` specifies bottom bars in the cantilever span.
2. **Sheet `Dam` Column Capacity**: Standard Kata templates allocate columns up to `BZ` (~35 spans). Runs exceeding 35 spans are rare in practical projects, but the calculator should support an arbitrary number of spans dynamically.

---

## 4. Conclusion

1. **Architecture Blueprint**: Establish `HPRebar.Core/KataRebar/` with `Models/` and `Calculators/` containing `KataBeamRebarSpec`, `KataBarNotationParser`, `KataRebarCalculator`, and `KataRebarLayoutResult`.
2. **Reuse Strategy**: Reuse `Point3`, `Vector3`, `Polyline3`, and `Tolerance` from `HPRebar.Core.BeamRebar.Models`. Reuse the 3-zone spacing mathematics from `BeamStirrupDistributionCalculator`.
3. **Calculation Pipeline**: Implement the 7-step pipeline detailed in `report.md`: Input ingestion $\to$ Continuous bars $\to$ Support top bars $\to$ Span bottom bars $\to$ Side bars $\to$ 3-zone stirrups $\to$ Packaging.
4. **Zero-Revit Purity**: Confirmed 100% `netstandard2.0` purity. No Autodesk Revit assemblies are referenced.

---

## 5. Verification Method

1. **Unit Test Execution**:
   Run the pure domain unit tests:
   ```powershell
   dotnet test HPRebar/HPRebar.Core.Tests/HPRebar.Core.Tests.csproj
   ```
   *Expected Result*: All 452 baseline tests pass 100% with 0 failures and 0 skipped.
2. **Purity Verification**:
   Inspect `HPRebar/HPRebar.Core/HPRebar.Core.csproj`: verify `<TargetFramework>netstandard2.0</TargetFramework>` and zero `<Reference>` or `<PackageReference>` to `Autodesk.Revit.*`.
3. **Report Verification**:
   Inspect the comprehensive survey document at:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_2\report.md`.

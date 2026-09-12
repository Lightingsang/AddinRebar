# Handoff Report — explorer_m1_2: M1 Domain Calculators Architect

**Agent**: `explorer_m1_2`  
**Role**: M1 Domain Calculators Architect  
**Working Directory**: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_2`  
**Date**: 2026-09-07  
**Artifact**: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_2\calculators_plan.md`  

---

## 1. Observation

1. **Reference Implementation Patterns in `HPRebar.Core/ColumnRebar/`**:
   - `Tolerance.cs` (`HPRebar/HPRebar.Core/ColumnRebar/Tolerance.cs`, lines 7–13):
     ```csharp
     public static class Tolerance
     {
         public const double Default = 1.0e-9;
         public static bool AreEqual(double first, double second, double tolerance = Default) =>
             second - tolerance < first && first < second + tolerance;
     }
     ```
   - `StirrupDistributionCalculator.cs` (`HPRebar/HPRebar.Core/ColumnRebar/StirrupDistributionCalculator.cs`, lines 16, 90–99):
     ```csharp
     public const int MaxBarPositions = 1002;
     ...
     private static int RequireUsableCount(int count, double spacing)
     {
         if (count > MaxBarPositions)
         {
             throw new ArgumentOutOfRangeException(
                 nameof(spacing), spacing,
                 $"A spacing of {spacing} needs {count} ties, more than the {MaxBarPositions} a rebar set can hold.");
         }
         return count;
     }
     ```
   - `BarPolylineBuilder.cs` (`HPRebar/HPRebar.Core/ColumnRebar/BarPolylineBuilder.cs`, line 160):
     ```csharp
     private const double CollinearToleranceMm = 1.0e-6;
     ```
   - `CanvasScaleCalculator.cs` (`HPRebar/HPRebar.Core/ColumnRebar/CanvasScaleCalculator.cs`, lines 8–10, 41–44):
     Calculates uniform scale divisor fitting geometry within budget, generating `ElevationLayout` with baseline and scale.

2. **Legacy Requirements & Edge Cases Surveyed in `survey_source_analysis.md`**:
   - Lines 35–37: Uniform distribution (`TypeDis = 0`), 3-zone distributions (`TypeDis = 1` ($L/4-L/2-L/4$), `TypeDis = 2` ($L/3-L/3-L/3$)), node stirrups (`IsStirrupInNode`).
   - Lines 38–40: Continuous longitudinal top bars (90° downward hooks into column core) and bottom bars (90° upward hooks).
   - Lines 40: 50% staggered lap splices for spans exceeding commercial length (11.7 m), top spliced in midspan, bottom spliced at supports.
   - Lines 41–42: Top additional bars extending $L_n/3$ or $L_n/4$, bottom additional bars in midspan with cutoffs at $L_n/7$ or $L_n/8$, multi-layer offset $\Delta Z \ge 30$ mm.
   - Lines 43–44: Deep beam skin bars triggered when height $h \ge 700$ mm, spacing $s_v \le 300$ mm, transverse C-ties with alternating 90°/135° hooks.
   - Lines 45–46: Hanging stirrup cages ($n \times 2$ stirrups @ 50 mm) and 45° diagonal ties at secondary beam framing joints.
   - Lines 64–67: Revit API failure hazards:
     - `MaxBarPositions = 1002` guardrail to prevent crash in `SetLayoutAsNumberWithSpacing`.
     - Segment culling $< 1.0$ mm (`MinimumSegmentMm = 1.0`) to avoid `ArgumentException: Curve is too short`.
     - Planarity requirement: all vertices for a bar curve must share the exact same transverse coordinate ($Y$).
     - Soffit steps ($|h_1 - h_2| > 1.0$ mm): bottom bars cannot bend across vertical steps; must terminate with 90° hooks into the support column.

3. **Master Plan & Target Layout in `PROJECT.md`**:
   - Lines 150–156:
     ```
     │ ├── Calculators/
     │ │ ├── BeamStirrupDistributionCalculator.cs
     │ │ ├── BeamMainBarCalculator.cs
     │ │ ├── BeamAdditionalBarCalculator.cs
     │ │ ├── BeamSideBarCalculator.cs
     │ │ ├── BeamSpecialBarCalculator.cs
     │ │ └── BeamCanvasTransformCalculator.cs
     │ └── Tolerance.cs
     ```

---

## 2. Logic Chain

1. **Decoupling and Determinism**:
   Because `HPRebar.Core` targets `netstandard2.0` and must be 100% testable without Revit licenses (`ORIGINAL_REQUEST.md` R1, R2), all calculations must be implemented as pure, deterministic static methods taking immutable domain records and primitive numbers (`double` in millimetres) and returning immutable results (`IReadOnlyList<T>`, tuples, records).
2. **Revit API Crash Prevention (Pre-condition Guardrails)**:
   Revit's `SetLayoutAsNumberWithSpacing` throws runtime COM exceptions if a rebar set exceeds 1002 positions (Observation 1, 2). Enforcing `MaxBarPositions = 1002` in `BeamStirrupDistributionCalculator` catches this before opening any Revit transaction.
   Revit's `Rebar.CreateFreeForm` rejects curve segments shorter than 1.0 mm (Observation 2). Implementing `SimplifyPolyline` with `MinimumSegmentMm = 1.0` in `BeamMainBarCalculator` guarantees geometric validity.
3. **Structural Detailing Integrity (TCVN 5574:2018 & ACI 318)**:
   - For long beams (> 11700 mm), tensile splices must be staggered by 50% ($\Delta \ge 1.3 L_{lap}$) and situated in compression zones (top bars at midspan, bottom bars at supports).
   - In deep beams ($h \ge 700$ mm), side skin reinforcement ($s_v \le 300$ mm) and transverse anti-buckling cross-ties prevent web cracking and buckling.
   - Over intermediate supports with depth drops, soffit steps cannot support bent bottom bars; bottom bars must terminate with upward hooks into the column core.
4. **WPF UI Presentation Mathematics**:
   Canvas drawing in WPF uses downward-increasing $Y$ coordinates and fixed pixel viewports. `BeamCanvasTransformCalculator` applies aspect-ratio-preserving uniform scaling with margin padding, transforming continuous spans $(X, Z)$ in millimetres directly into screen pixel coordinates $(X_{canvas}, Y_{canvas})$, alongside an inverse transform for interactive hit-testing.

---

## 3. Caveats

1. **Data Model Dependency**: The calculators depend on the immutable records designed in parallel by `explorer_m1_1` (`BeamSpan`, `BeamSupportNode`, `BeamContinuousStack`, `BeamStirrupSpec`, `Point3`, `Polyline3`, etc.). The method signatures in `calculators_plan.md` have been aligned with `explorer_m1_1`'s specification.
2. **Revit Shape Family Resolution**: The calculators produce purely mathematical bounding boxes and polyline coordinates. Mapping these to specific Revit `RebarShape` families (e.g. `M_T1` for stirrups) or `CreateFreeForm` is the responsibility of Milestone M3 (`HPRebar/HPRebar/Beam Rebar/`).
3. No other caveats.

---

## 4. Conclusion

The algorithm designs for all 7 components in `HPRebar.Core/BeamRebar/Calculators/` are fully detailed in `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_2\calculators_plan.md`.
The design provides:
1. `Tolerance.cs`: $1.0\times 10^{-9}$ numerical comparison, $1.0$ mm minimum segment length, $1.0\times 10^{-6}$ collinear tolerance.
2. `BeamStirrupDistributionCalculator.cs`: Uniform and 3-zone ($L/4-L/2-L/4$ and $L/3-L/3-L/3$) distribution, cantilever rules, support node ties, `MaxBarPositions = 1002` guardrail, and symmetrical slack centering.
3. `BeamMainBarCalculator.cs`: Continuous top/bottom polylines, 90° standard anchorage hooks, 50% staggered lap splices in compression zones, soffit step terminations, and sub-millimeter segment culling.
4. `BeamAdditionalBarCalculator.cs`: Support top bars ($L_n/3, L_n/4$), midspan bottom bars ($L_n/7$), 2-layer vertical offset stacking ($\Delta Z \ge 30$ mm).
5. `BeamSideBarCalculator.cs`: $h \ge 700$ mm trigger, $s_v \le 300$ mm vertical spacing, alternating 90°/135° transverse cross-ties.
6. `BeamSpecialBarCalculator.cs`: Hanging stirrup cages @ 50 mm, overlap merging for adjacent secondary beams, and 45° diagonal tie bars.
7. `BeamCanvasTransformCalculator.cs`: Aspect ratio preservation, margin padding, WPF $Y$-inversion, bidirectional screen/model transformation.

---

## 5. Verification Method

1. **Independent Verification of Design Specifications**:
   - Inspect `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_2\calculators_plan.md` to verify all method signatures, formulas, and edge-case handling.
2. **Build and Test Verification (Milestones M1 and M2 Implementation)**:
   Once the classes are coded:
   ```bash
   dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false
   dotnet test HPRebar/HPRebar.Core.Tests
   ```
   All tests in `HPRebar.Core.Tests/BeamRebar/` must pass with 0 failures.
3. **Invalidation Conditions**:
   - Any introduction of `Autodesk.Revit.*` namespaces into `HPRebar.Core`.
   - Any non-millimetre units in domain calculations.
   - Exceeding 1002 stirrup positions without throwing `ArgumentOutOfRangeException`.
   - Emitting polyline segments $< 1.0$ mm that cause Revit curve creation crashes.

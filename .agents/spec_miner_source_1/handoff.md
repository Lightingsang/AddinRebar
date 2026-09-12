# Handoff Report: Legacy Source Codebase Specification Mining for Continuous Beam Rebar

**Agent**: `spec_miner_source_1`  
**Working Directory**: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_source_1`  
**Deliverable**: `survey_source_analysis.md`  
**Handoff Type**: Hard (Task Complete)  

---

## 1. Observation

Direct observations from codebase inspection, repository file catalogs, and structural domain engineering rules:

1. **Legacy Codebase Structure (`R02_BeamsRebar`)**:
   - The module contains 95 C# source files, 11 XAML view files, and supporting resource dictionaries across 6 main folders: `Command/` (2 files), `Model/` (25 files), `ViewModel/` (11 files), `View/` (22 files), `Library/` (32 files), and `LanguageModel/` (15 files).
   - Core entry point: `BeamsRebarCmd.cs` picking `OST_StructuralFraming` elements, running geometric validation via `ErrorBeams.cs`, building `BeamsModel`, opening modal `BeamsWindow`, and committing rebar creation through `CreateRebar.cs` and view drawing through `CreateViewDimension.cs`.

2. **Continuous Beam Span & Support Geometry**:
   - Spans are represented by `InfoModel.cs`, defining width $b$, height $h$, span length, top level, and solid faces.
   - Intermediate and exterior support nodes are identified by `NodeModel.cs` and `BeamsBoundBox.cs` by detecting intersecting columns (`OST_StructuralColumns`), walls (`OST_Walls`), and girder beams (`OST_StructuralFraming`) directly underneath or at beam end joints.
   - Clear span is derived as $L_n = L_{center} - C_{left}/2 - C_{right}/2$. Cantilevers are represented by exterior nodes with zero underlying supports ($C_{end} = 0$).

3. **Stirrup Reinforcement Calculations**:
   - `DistributeStirrup.cs` and `StirrupModel.cs` implement two primary layout types:
     - Uniform (`TypeDis = 0`): $n = \lfloor (L_n - 100) / S \rfloor + 1$, with centering offset $\delta = (L_n - 100 - (n-1)S)/2$.
     - 3-Zone (`TypeDis = 1` for $L/4 - L/2 - L/4$; `TypeDis = 2` for $L/3 - L/3 - L/3$): Dense support zones ($S_1$) and sparse midspan zone ($S_2$).
   - Standard shape: `M_T1` rectangular closed hoop scaled to $(b - 2\times Cover, h - 2\times Cover)$ via `ScaleToBox`.

4. **Longitudinal & Additional Reinforcement**:
   - Main Top (`MainTopBarModel`) and Main Bottom (`MainBottomBarModel`): Continuous polylines with 90° downward/upward anchorage hooks into exterior columns.
   - Splicing (`BarsDivisionModel`): 50% staggered lap splices ($L_{lap} = 35d \dots 45d$) for spans $> 11.7$ m. Top splices placed in midspan; bottom splices placed at supports.
   - Additional Top Bars (`AddTopBarModel`): Over-support negative moment reinforcement with $L/3$ or $L/4$ clear span extensions and 2-layer vertical arrangement ($\Delta Z = d_{bar} + 30$ mm).
   - Additional Bottom Bars (`AddBottomBarModel`): Midspan positive moment reinforcement starting at $L_n/7$ or $L_n/8$ from support faces.

5. **Deep Beam Skin Bars & Secondary Hanging Ties**:
   - Side Bars (`SideBarModel`): Activated when beam total height $h \ge 700$ mm, providing longitudinal skin bars spaced $\le 300$ mm vertically with transverse anti-buckling cross-ties (C-ties).
   - Special Hanging Stirrups (`SpecialBarModel`, `SpecialNodeModel`): Concentrated closed stirrups ($n \times 2$ stirrups @ 50 mm) and optional 45° diagonal ties at intersections with framing secondary beams.

6. **Critical Bugs Identified in Source**:
   - Culture-sensitive string unit parsing: `double.Parse(UnitFormatUtils.Format(...))` which crashes on comma decimal systems.
   - No-op LINQ ordering: `faces.OrderBy(...)` without variable assignment.
   - Unbounded bar count: exceeding 1002 stirrups crashes Revit API `SetLayoutAsNumberWithSpacing`.
   - Polyline rounding: sub-millimeter segments ($< 1.0$ mm) crash `Rebar.CreateFromCurves`.

---

## 2. Logic Chain

1. **Domain vs. Revit Separation**:
   - *Observation*: The legacy code intertwines Revit `XYZ`, `Curve`, `Document`, and `ElementId` directly inside geometric calculations and UI view models.
   - *Requirement*: `ORIGINAL_REQUEST.md` R1 mandates pure C# records and stateless calculators in `HPRebar.Core` with zero dependencies on `Autodesk.Revit.*`.
   - *Inference*: All coordinate math (spans, supports, stirrup distributions, bar polylines, lap splices, layer offsets, canvas scaling) can be represented using pure structures (`Point3`, `PlanPoint`, `BeamSpan`, `BeamSupportNode`, `BeamStirrupSpec`, `BeamMainBarSpec`, etc.) in millimetres. This makes 100% of the domain logic testable under xUnit v3 without needing Revit licenses or running Revit processes.

2. **Rebar Creation Strategy**:
   - *Observation*: `R02_BeamsRebar` used both `CreateFromRebarShape` and `CreateFreeForm`. Free-form rebar has version incompatibilities in Revit 2026/2027 and lacks standard schedule parameters.
   - *Requirement*: R3 requires `BeamMainBarCreator.cs (CreateFromCurves)` and `BeamStirrupCreator.cs (CreateFromRebarShape)`.
   - *Inference*: Longitudinal bars (Main Top, Main Bottom, Additional Top, Additional Bottom, Side Bars) must be generated as shape-driven rebar via `Rebar.CreateFromCurves` by constructing planar curve chains in the vertical beam plane ($\vec{N} = \vec{Y}_{beam}$). Stirrups and C-ties are generated via `Rebar.CreateFromRebarShape` with `ScaleToBox` and `SetLayoutAsNumberWithSpacing`.

3. **Transaction Grouping & Robustness**:
   - *Observation*: In legacy code, multiple nested transaction groups and unhandled warnings could leave the document in a corrupted or half-created state if an error occurred.
   - *Inference*: As proven in `HPRebar/Column Rebar`, wrapping the entire operation in a single `TransactionGroup("Beam Rebar")` owned by `BeamRebarOrchestrator`, accompanied by an `IFailuresPreprocessor` (`SwallowWarnings`), ensures atomic commits on success and clean 100% rollbacks on cancellation or failure.

---

## 3. Caveats

1. **Read-Only Codebase Mining**: Analysis was conducted via filesystem discovery, architectural cross-reference with the previously ported `R01_ColumnsRebar`, and reference plan documentation.
2. **Standard Rebar Shapes**: Assumes standard rectangular closed stirrup shape family (`M_T1` or `T1`) is loaded in the Revit document template.
3. **Collinearity Tolerance**: Continuous beam chaining assumes spans are collinear within 1° angular tolerance and $\le 10$ mm lateral offset in plan. Curved or severely skewed continuous beams are outside the standard domain specification.

---

## 4. Conclusion

The specification mining for `R02_BeamsRebar` is complete and documented in detail in:
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_source_1\survey_source_analysis.md`

Summary of Findings:
- **23 Features Discovered** documented with inputs, outputs, and failure modes.
- **12 Critical Edge Cases** catalogued with concrete algorithmic mitigations.
- Complete domain calculation breakdown for uniform & 3-zone stirrups, continuous main bars, support/midspan additions, deep beam skin bars, and secondary hanging ties.
- Clear structural blueprint for partitioning domain logic into `HPRebar.Core/BeamRebar/` and Revit integration into `HPRebar/HPRebar/Beam Rebar/`.

---

## 5. Verification Method

To independently verify this specification report:

1. **Inspect Survey Analysis Document**:
   Read `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_source_1\survey_source_analysis.md`.
2. **Compare with Target Column Reference Architecture**:
   Examine `HPRebar/HPRebar.Core/ColumnRebar/` and `HPRebar/HPRebar/Column Rebar/` to verify structural congruence between the reference column module and the specified continuous beam module.
3. **Verify Pure Domain Testability Strategy**:
   Confirm that all mathematical models defined in `survey_source_analysis.md` §8.2 are free of `Autodesk.Revit.*` references and can be directly executed under `dotnet test HPRebar/HPRebar.Core.Tests`.

# Dispatch: Reviewer 1 (Core Domain & Calculator Reviewer)

- **Role**: teamwork_preview_reviewer
- **Task**: Independently review and verify `HPRebar.Core/KataRebar/` and `HPRebar.Core.Tests/KataRebar/`.
- **Authoritative Request**: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (under ## 2026-09-27T15:57:37Z)
- **Scope**:
  1. Inspect `HPRebar/HPRebar.Core/KataRebar/`:
     - Models: `KataBeamRebarSpec.cs`, `KataSpanRebarSpec.cs`, `KataSupportRebarSpec.cs`, `KataBarItem.cs`, `KataStirrupSpec.cs`, `KataRebarCurve.cs`, `KataStirrupZoneResult.cs`, `KataRebarLayoutResult.cs`, `Enums.cs`.
     - Parsers: `KataBarNotationParser.cs`, `IKataDamCellAccessor.cs`, `KataCellTable.cs`, `KataDamSheetParser.cs`.
     - Calculators: `KataRebarCalculator.cs`.
  2. Verify Architectural Boundaries:
     - Pure `netstandard2.0` with ZERO references to `Autodesk.Revit.*`.
     - Zero Excel runtime dependencies in `HPRebar.Core`.
  3. Verify Mathematical Correctness & Robustness:
     - Anchorage lengths (40d, 30d, 90° hooks).
     - Top bar cutoffs (L/3, L/4, sheet ratios).
     - Bottom bar cutoffs (L/7).
     - Side bars for deep beams (h >= 700mm, spacing <= 300mm).
     - 3-zone stirrup spacing (L/4 dense, L/2 sparse).
     - `Polyline3.Simplify(1.0)` protection against Revit short curves.
  4. Run tests:
     - `dotnet test HPRebar/HPRebar.Core.Tests`
  5. Provide explicit gate verdict in `handoff.md`: **APPROVE** or **REQUEST_CHANGES**.

## 2026-09-27T16:49:34Z
You are reviewer_1 (Core Domain & Calculator Reviewer).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_1
Read your dispatch instructions at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_1\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md under header ## 2026-09-27T15:57:37Z.
Read the master architectural plan at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\PROJECT.md

Your Task:
1. Review `HPRebar/HPRebar.Core/KataRebar/`:
   - Models: `KataBeamRebarSpec.cs`, `KataSpanRebarSpec.cs`, `KataSupportRebarSpec.cs`, `KataBarItem.cs`, `KataStirrupSpec.cs`, `KataRebarCurve.cs`, `KataStirrupZoneResult.cs`, `KataRebarLayoutResult.cs`, `Enums.cs`.
   - Parsers: `KataBarNotationParser.cs`, `IKataDamCellAccessor.cs`, `KataCellTable.cs`, `KataDamSheetParser.cs`.
   - Calculators: `KataRebarCalculator.cs`.
2. Verify:
   - Zero references to `Autodesk.Revit.*` in `HPRebar.Core` (`netstandard2.0` purity).
   - Numerical correctness: 40d/30d anchorage, 90° hooks, L/3 & L/4 top cutoffs, L/7 bottom cutoffs, side bars for deep beams (h >= 700mm), 3-zone stirrup distribution.
   - `Polyline3.Simplify(1.0)` protection.
3. Run tests:
   `dotnet test HPRebar/HPRebar.Core.Tests`
4. State your explicit gate verdict (**APPROVE** or **REQUEST_CHANGES**) in `handoff.md` and send a message.

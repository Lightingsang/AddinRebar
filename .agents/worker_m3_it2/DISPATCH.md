# Task Assignment: worker_m3_it2

## Role
M3 Remediation Worker (`teamwork_preview_worker`)

## Working Directory
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3_it2`

## Reference Documents
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Dead Ends Log: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\orchestrator_1\DEAD_ENDS.md`
4. Reviewer 2 Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_2\handoff.md`
5. Challenger 1 Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m3_1\handoff.md`
6. Challenger 2 Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m3_2\handoff.md`

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Objective
Apply the 9 concrete fixes across the Continuous Beam Rebar module to resolve all issues identified in Gate 1.

### Detailed Fix Inventory:

1. **Elevation Coordinate Double-Counting Fix** (`HPRebar/HPRebar/Beam Rebar/BeamStackReader.cs`):
   - Compute `topElevMm` relative to the origin datum:
     `double topElevMm = RevitUnits.FtToMm(faces.Top.Origin.Z - originPoint.Z);`
   - Ensure `originPoint` represents the consistent spatial anchor for `PointMapper`.

2. **Polyline Closing Edge on Closed Polylines** (`HPRebar/HPRebar/Beam Rebar/BeamMainBarCreator.cs`):
   - In `BuildCurves`, add closing line segment when `simplified.IsClosed && simplified.Points.Count > 2`:
     ```csharp
     if (simplified.IsClosed && simplified.Points.Count > 2)
     {
         var pLast = mapper.ToXyz(simplified.Points[simplified.Points.Count - 1]);
         var pFirst = mapper.ToXyz(simplified.Points[0]);
         curves.Add(Line.CreateBound(pLast, pFirst));
     }
     ```

3. **Single Stirrup Run Crash Fix** (`HPRebar/HPRebar/Beam Rebar/BeamStirrupCreator.cs`):
   - In `CreateStirrups` (and helper methods), when configuring the rebar set:
     If `run.Count == 1`, call `accessor.SetLayoutAsSingle();`
     Otherwise, call `accessor.SetLayoutAsNumberWithSpacing(Math.Clamp(run.Count, 2, 1002), RevitUnits.MmToFt(run.Spacing), true, true, true);`

4. **Cantilever Support Preservation** (`HPRebar/HPRebar/Beam Rebar/BeamSupportFinder.cs` & `BeamStackReader.cs`):
   - Do NOT discard real columns or synthesize phantom supports when physical supports are fewer than $N_{spans} + 1$.
   - Identify whether an exterior span is a cantilever (support missing at start or end).
   - Retain detected physical supports, set `span.Cantilever` appropriately (`CantileverPosition.Start`, `CantileverPosition.End`, or `None`), and avoid synthesizing phantom columns at free ends.

5. **Stepped Beam Widths Validation** (`HPRebar/HPRebar/Beam Rebar/BeamStackValidator.cs`):
   - Add a validator rule requiring uniform width across all continuous spans:
     `$|b_i - b_0| \le 1.0$ mm`. If stepped widths are encountered, return `ValidationResult.Fail("Continuous beams with stepped widths are not currently supported.")`.

6. **Flush Secondary Beam vs Supporting Girder** (`HPRebar/HPRebar/Beam Rebar/BeamSupportFinder.cs`):
   - In `MeasureGirderSupport`: verify that the framing element's top elevation is strictly below the beam soffit elevation before classifying it as a supporting girder. Flush framing members are secondary beams, not girders.

7. **Joint Secondary Beam Null Span Guard** (`HPRebar/HPRebar/Beam Rebar/BeamSpecialBarCreator.cs` & `HPRebar.Core/BeamRebar/Calculators/BeamSpecialBarCalculator.cs`):
   - In `BeamSpecialBarCreator.cs` and `BeamSpecialBarCalculator.cs`: if `stack.FindSpanAt(sec.CenterX)` returns `null` (secondary beam framed into column support zone), safely skip with a warning log rather than throwing an unhandled `ArgumentException`.

8. **Circular Column 0-Width Measurement** (`HPRebar/HPRebar/Beam Rebar/BeamSupportFinder.cs`):
   - In `MeasureColumnSupport`: if the top face edge loop has only 1 edge or vertex (circular column), compute diameter from the bounding box:
     `widthS = RevitUnits.FtToMm(top.get_BoundingBox().Max.X - top.get_BoundingBox().Min.X);`

9. **Section View Indexing Desynchronization** (`HPRebar/HPRebar/Beam Rebar/BeamRebarOrchestrator.cs`):
   - In `CreateDimensions` and `CreateTables`: replace `spanIdx = i / _settings.SectionsPerSpan` with dynamic mapping that checks the exact number of cut stations generated per span via `SectionViewCreator.ComputeCutStations(span, _settings.SectionsPerSpan)`.

## Completion Criteria
1. All 9 fixes cleanly implemented with strict adherence to repository coding rules.
2. File-scoped namespaces (`namespace HPRebar.BeamRebar;`).
3. Write your handoff report to: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3_it2\handoff.md`.
4. Send completion message back to orchestrator.

## 2026-09-07T08:53:34Z
Apply all 9 remediation fixes across the target files:
1. `HPRebar/HPRebar/Beam Rebar/BeamStackReader.cs` (Relative top elevation: faces.Top.Origin.Z - originPoint.Z).
2. `HPRebar/HPRebar/Beam Rebar/BeamMainBarCreator.cs` (`BuildCurves` closing curve for simplified.IsClosed && Points.Count > 2).
3. `HPRebar/HPRebar/Beam Rebar/BeamStirrupCreator.cs` (`SetLayoutAsSingle` when Count == 1, clamp Count between 2 and 1002 for SetLayoutAsNumberWithSpacing).
4. `HPRebar/HPRebar/Beam Rebar/BeamSupportFinder.cs` & `BeamStackReader.cs` (Preserve detected supports for cantilevers, set span.Cantilever correctly, do not synthesize phantom supports).
5. `HPRebar/HPRebar/Beam Rebar/BeamStackValidator.cs` (Validate uniform span width across all continuous spans).
6. `HPRebar/HPRebar/Beam Rebar/BeamSupportFinder.cs` (MeasureGirderSupport: require girder top elevation below beam soffit datum).
7. `HPRebar/HPRebar/Beam Rebar/BeamSpecialBarCreator.cs` & `HPRebar.Core/BeamRebar/Calculators/BeamSpecialBarCalculator.cs` (Safely guard/skip secondary beam in support zone if hostSpan is null).
8. `HPRebar/HPRebar/Beam Rebar/BeamSupportFinder.cs` (MeasureColumnSupport: circular column diameter from top face bounding box).
9. `HPRebar/HPRebar/Beam Rebar/BeamRebarOrchestrator.cs` (Dynamic section view indexing per span using ComputeCutStations).

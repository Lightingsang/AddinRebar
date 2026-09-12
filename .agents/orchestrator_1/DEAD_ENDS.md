# Dead Ends Log

| Iteration | Approach Tried | Why It Failed | Files Touched |
|---|---|---|---|
| M3-It1 | Absolute model elevation in `span.TopElevation` while `originPoint.Z` also contains beam level elevation | Double-counts Z elevation in `PointMapper.ToXyz`: bars generated floating +10ft above the beam | `HPRebar/HPRebar/Beam Rebar/BeamStackReader.cs` |
| M3-It1 | Constructing curve segments up to `simplified.Points.Count - 1` without closing loop for `IsClosed` polylines | `Polyline3.Simplify` culls repeated 5th point; hanging stirrups created as 3-sided open bars | `HPRebar/HPRebar/Beam Rebar/BeamMainBarCreator.cs` |
| M3-It1 | Calling `SetLayoutAsNumberWithSpacing(run.Count, ...)` without checking `run.Count >= 2` | In Revit API, `SetLayoutAsNumberWithSpacing` throws `ArgumentOutOfRangeException` when count < 2 (e.g. single stirrup in narrow transition) | `HPRebar/HPRebar/Beam Rebar/BeamStirrupCreator.cs` |
| M3-It1 | Collapsing physical supports to `SynthesizeDefaultSupports` when `total < sortedBeams.Count + 1` | Overhang cantilever beams have N supports for N spans ($N < N+1$); triggers synthesis, erasing real columns and placing phantom support at cantilever tip | `HPRebar/HPRebar/Beam Rebar/BeamSupportFinder.cs` |
| M3-It1 | Center-aligned beams without cross-span width validation rule | Stepped width beams ($b_0 \ne b_1$) pass validation but main bars placed at $b_0$ protrude outside concrete in narrower span | `HPRebar/HPRebar/Beam Rebar/BeamStackValidator.cs` |
| M3-It1 | Bounding box search up to `box.Min.Z + 0.5` without checking girder soffit elevation | Flush-soffit secondary framing beams are misclassified as supporting girders | `HPRebar/HPRebar/Beam Rebar/BeamSupportFinder.cs` |
| M3-It1 | Throwing `ArgumentException` when `FindSpanAt(sec.CenterX)` returns null in `ComputeHangingStirrups` | Secondary beams intersecting within column joint zones throw unhandled exception, aborting transaction | `HPRebar/HPRebar/Beam Rebar/BeamSpecialBarCreator.cs`, `HPRebar.Core/BeamRebar/Calculators/BeamSpecialBarCalculator.cs` |
| M3-It1 | Querying top face edge loop endpoints for column width | Circular columns have periodic edge with `EndPoint(0) == EndPoint(1)`, returning width 0.0mm | `HPRebar/HPRebar/Beam Rebar/BeamSupportFinder.cs` |
| M3-It1 | `spanIdx = i / _settings.SectionsPerSpan` for mapping section views | Cantilever spans generate only 1 section cut station; static division desynchronizes span indexing for dimensions and tables | `HPRebar/HPRebar/Beam Rebar/BeamRebarOrchestrator.cs` |
| M4-It1 | Accessing `stack.OverallStartX`/`EndX` on `BeamStack` instead of `stack.ContinuousStack` | CS1061 compile error: properties exist on `ContinuousStack` model, not `BeamStack` root | `HPRebar/HPRebar/Beam Rebar/View/Controls/BeamElevationPainter.cs` |
| M4-It1 | Accessing `inter.IntersectionX` on `SecondaryBeamIntersection` instead of `inter.CenterX` | CS1061 compile error: property is named `CenterX` in domain model | `HPRebar/HPRebar/Beam Rebar/View/Controls/BeamElevationPainter.cs` |
| M4-It1 | Referencing non-existent resource keys `Spacing.SmallRight` and `Font.Size.Subtitle` | XAML runtime parsing error; keys are `Spacing.SmallHorizontal` and `Font.Size.Subheading` | `BeamRebarView.xaml`, `GeometryTabView.xaml` |
| M4-It1 | ComboBox `SelectedItem` binding to get-only `SelectedSupportEditor`/`SelectedSpanEditor` | Two-way binding fails to update current index; dropdown selection becomes unresponsive | `BeamRebarSession.cs`, `AdditionalBarsTabView.xaml` |
| M4-It1 | Validating stirrup maximum count (>1002) using only dense spacing | Sparse spacing typo (e.g. 5mm) bypasses validation and causes Revit API crash | `BeamRebarSession.cs` |
| M4-It1 | Using support node centers for model X span in `BeamElevationCanvas` | Exterior cantilever overhangs map to negative X ($X < 0$) off-screen | `BeamElevationPainter.cs` |
| M4-It1 | Hardcoded pixel layer offsets ($\pm 3.0\text{px}, \pm 5.0\text{px}$) in elevation painter | On long multi-span beams, offsets exceed beam height, causing rebar to render inverted outside beam | `BeamElevationPainter.cs` |
| M4-It1 | Instantiating `CanvasPalette.From(this)` inside `OnRender` on every frame | Unnecessary GC allocations of frozen pens and brushes per render frame | `BeamElevationCanvas.cs`, `BeamSectionCanvas.cs` |


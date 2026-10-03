# Map 03 — BeamRebar (HPRebar add-in + Core)

Read-only map, 2026-10-03. Nothing built or run (concurrent session) — every compile/runtime claim below is either read from source or quoted from CLAUDE.md and marked so.

Path prefixes: `A/` = `HPRebar/HPRebar/BeamRebar/`, `C/` = `HPRebar/HPRebar.Core/BeamRebar/`, `T/` = `HPRebar/HPRebar.Core.Tests/BeamRebar/`.

Size: A = 61 `.cs` (5 890 LOC) + 6 `.xaml` (874 LOC); C = 25 `.cs` (3 236 LOC); T = 8 files (1 803 LOC).

## 1. Behaviour

- Ribbon **HPRebar ▸ Rebar ▸ Beam Rebar** (`HPRebar/HPRebar/Application.cs:65`).
- The user picks straight structural-framing beams of one run, left to right (A/BeamRebarCommand.cs:44). The run must pass: one solid, rectangular, collinear ≤ 1°, lateral offset ≤ 10 mm, top elevation ±5 mm, gaps ≤ 2 000 mm, b ≥ 100 / h ≥ 150, uniform width (A/Service/BeamStackValidator.cs:18-45).
- A modeless window opens with 5 tabs: Geometry (read-only, with elevation/section preview canvases), Main Bars, Additional Bars, Stirrups (also side bars, cross-ties, hanging stirrups) and Views/Settings.
- **Run** creates, inside one undo entry `Beam Rebar`:
  - Views: 1 elevation view (detail or section type) and 1–3 cross-sections per span.
  - Dimensions: an elevation span chain plus a height dimension, and B/H on each section.
  - One table per section, drawn as detail lines plus TextNotes.
  - Rebar:
    - Stirrup sets: 3 zone runs per span, plus optional stirrups inside the nodes.
    - Top and bottom main bars, with hooks and splices once the bar passes the 11 700 mm stock length.
    - Support top bars and span bottom bars, 2 layers each.
    - Skin bars and cross-ties when h ≥ 700.
    - Hanging stirrups at secondary beams, and optional 45° ties.
  - The `Partition` parameter is set on every bar.
- Runtime: per CLAUDE.md the rebar add-in has **never been verified at runtime**, and **BeamRebar does not compile under R27**. Neither was re-checked here.

## 2. Flow

```
BeamRebarCommand.Execute                       A/BeamRebarCommand.cs:30
 ├ static _window? → Activate()                :35
 ├ PickObjects(BeamRebarSelectionFilter)       :44
 ├ BeamStackValidator.Validate                 :63  → RevitDialogs.Error
 ├ BeamStackReader.Read                        :71  → BeamSupportFinder.FindSupports/FindSecondaryBeams,
 │                                                     BeamSolidFaceReader, PointMapper → BeamStack{Core BeamContinuousStack + BeamFaces}
 ├ BeamDefaultSpecBuilder.Build                :75  (new RebarTypeCatalog)
 ├ RebarShapeResolver.Load + RebarCreationService.CanCreate  :76-78
 ├ BeamAnnotationSettings.Load                 :85  (3 collectors)
 ├ new BeamRebarOrchestrator → new BeamRebarExternalEventHandler (ExternalEvent.Create)  :86-87
 ├ RebarTypeCatalog.LoadBarTypes → new BeamRebarSession → new BeamRebarViewModel(+5 tab VMs, new LocalizationService) → new BeamRebarView  :89-92
 └ Owner = Revit main window; Closed → handler.Dispose, _window = null; view.Show()  :94-106
UI edits → BeamRebarSession → BeamElevationCanvas/BeamSectionCanvas (debounced) → *Painter (Core BeamCanvasTransformCalculator)
RunCommand → BeamRebarViewModel.RunAsync      A/ViewModel/BeamRebarViewModel.cs:68
 ├ Session.Validate → Session.ToSpec → BeamRebarSpec
 ├ runner.PlannedCount → ProgressMaximum
 └ runner.RunAsync → enqueue BeamRebarRequest + ExternalEvent.Raise   A/BeamRebarExternalEventHandler.cs:35
Revit API thread: Handler.Execute (:46) → BeamRebarOrchestrator.Run   A/Service/BeamRebarOrchestrator.cs:51
 └ TransactionGroup "Beam Rebar" (:59)
    ├ T "Create Detail View" → DetailViewCreator.Create
    ├ T "Create Section Views" → SectionViewCreator.Create
    ├ T "Create Elevation Dimensions" / "Create Section Dimensions" → DimensionCreator
    ├ RebarCreationService.Create → 5 × T → Beam{Stirrup,MainBar,AdditionalBar,SideBar,SpecialBar}Creator
    │      → Core Beam*Calculator (mm polylines/runs) → PointMapper (mm→ft) → Rebar.CreateFromRebarShape / CreateFromCurves
    ├ T "Create Beam Tables" → RebarTableTagCreator.Create
    └ Assimilate (:82); on exception RollBack + rethrow (:92)
 → TaskDialog summary (Handler:64) → TCS result → VM CloseRequested → window closes
```

## 3. Class inventory

The layers are Entry, Pres (Presentation), App (application orchestration), Dom (Core domain) and Rev (Revit integration).

| Class | File | LOC | Layer | Responsibility | API |
|---|---|---|---|---|---|
| BeamRebarCommand | A/BeamRebarCommand.cs | 114 | Entry | pick, validate, read, wire session/VM/view, show modeless | Y |
| BeamRebarExternalEventHandler | A/BeamRebarExternalEventHandler.cs | 95 | Entry/App | queue → ExternalEvent → orchestrator; summary TaskDialog | Y |
| BeamRebarRequest | A/BeamRebarRequest.cs | 33 | Entry | spec + progress + TCS | N |
| BeamRebarSelectionFilter | A/BeamRebarSelectionFilter.cs | 24 | Entry | framing FamilyInstance only | Y |
| BeamAnnotationSettings | A/Model/BeamAnnotationSettings.cs | 73 | Rev (in Model/) | templates/dim/text types + offsets; `Load` runs collectors | Y |
| BeamFaces | A/Model/BeamFaces.cs | 54 | Rev DTO | element + PlanarFaces per span, 8 alias props | Y |
| BeamStack | A/Model/BeamStack.cs | 128 | Rev DTO | Core stack + faces + axes + PointMapper; aliases; `Summary` | Y |
| BeamRebarSpec | A/Model/BeamRebarSpec.cs | 53 | App DTO | Core specs + 6 `RebarTypeInfo` | Y (types) |
| BeamOrchestratorResult / CreatedBeamRebar / CreatedBeamViews | A/Model/*.cs | 22/31/22 | App DTO | run outcome, lists of Rebar / ViewSection | Y |
| RebarTypeInfo | A/Model/RebarTypeInfo.cs | 15 | Rev DTO | name + Ø + RebarBarType | Y |
| BeamSectionStyle | A/Model/BeamSectionStyle.cs | 13 | Dom (add-in side) | Rectangle / Other | N |
| ValidationResult / ValidationMessages | A/Model/*.cs | 30/34 | Dom (add-in side) | numeric code → message | N |
| UiStrings / UiStringsCatalog | A/Model/*.cs | 105/99 | Pres | EN/VN strings | N |
| BeamRebarOrchestrator | A/Service/BeamRebarOrchestrator.cs | 179 | App | TransactionGroup; views → dims → rebar → tables; PlannedCount | Y |
| RebarCreationService | A/Service/RebarCreationService.cs | 124 | App | shape preflight, 5 rebar transactions, PlannedCount | Y |
| BeamDefaultSpecBuilder | A/Service/BeamDefaultSpecBuilder.cs | 104 | App | initial spec from bar catalog | Y |
| BeamStackValidator | A/Service/BeamStackValidator.cs | 203 | Rev | 10 geometric rules on Elements | Y |
| BeamStackReader | A/Service/BeamStackReader.cs | 177 | Rev | Elements → BeamStack (sort, origin, spans, faces) | Y |
| BeamSupportFinder | A/Service/BeamSupportFinder.cs | 443 | Rev | columns/walls/girders + secondary beams via bbox collectors; support classification | Y |
| BeamSolidFaceReader | A/Service/BeamSolidFaceReader.cs | 185 | Rev | solids, faces, b/h, section style | Y |
| PointMapper | A/Service/PointMapper.cs | 55 | Rev | local mm ↔ world ft | Y |
| RevitUnits | A/Service/RevitUnits.cs | 23 | Rev | mm ↔ ft, display | Y |
| RebarShapeResolver | A/Service/RebarShapeResolver.cs | 75 | Rev | RebarShape by alias names | Y |
| RebarTypeCatalog | A/Service/RebarTypeCatalog.cs | 85 | Rev | bar/cover/hook types, `FindBarType` | Y |
| RebarFailureHandling | A/Service/RebarFailureHandling.cs | 35 | Rev | warning-swallowing preprocessor | Y |
| BeamStirrupCreator | A/Service/BeamStirrupCreator.cs | 169 | Rev | `CreateFromRebarShape` stirrup sets; `SetPartition` | Y |
| BeamMainBarCreator | A/Service/BeamMainBarCreator.cs | 115 | Rev | top/bottom bars; shared `CreateBarFromPolyline`/`BuildCurves` | Y |
| BeamAdditionalBarCreator | A/Service/BeamAdditionalBarCreator.cs | 55 | Rev | support top / span bottom bars | Y |
| BeamSideBarCreator | A/Service/BeamSideBarCreator.cs | 88 | Rev | skin bars + cross-ties | Y |
| BeamSpecialBarCreator | A/Service/BeamSpecialBarCreator.cs | 98 | Rev | hanging stirrups + diagonal ties | Y |
| DetailViewCreator | A/Service/DetailViewCreator.cs | 119 | Rev | elevation view, view-type resolve, `Rename` | Y |
| SectionViewCreator | A/Service/SectionViewCreator.cs | 134 | Rev | section views; `ComputeCutStations` (pure) | Y |
| DimensionCreator | A/Service/DimensionCreator.cs | 171 | Rev | elevation/section dimensions | Y |
| RebarTableTagCreator | A/Service/RebarTableTagCreator.cs | 174 | Rev | table of detail lines + TextNotes; unused tag method | Y |
| RevitDialogs | A/Service/RevitDialogs.cs | 31 | Rev (UI) | TaskDialog wrappers | Y |
| LocalizationService | A/Service/LocalizationService.cs | 18 | Pres (in Service/) | EN/VN toggle (ObservableObject) | N |
| BeamRebarViewModel | A/ViewModel/BeamRebarViewModel.cs | 114 | Pres | tabs, Run/Cancel/Toggle, progress | Y (RevitDialogs) |
| BeamRebarSession + SupportTopBarEditor + SpanBottomBarEditor | A/ViewModel/BeamRebarSession.cs | 536 | Pres | all editor state, `Validate`, `ToSpec`, bar-type matching | N |
| IBeamRebarRunner | A/ViewModel/IBeamRebarRunner.cs | 15 | Pres contract | PlannedCount/RunAsync/LastResult | N |
| 6 tab VMs (base + Geometry/MainBars/AdditionalBars/Stirrups/Views) | A/ViewModel/Tabs/*.cs | 21–35 | Pres | titles, option lists, apply-to-all | N |
| BeamRebarView + 5 tab views | A/View/**/*.xaml(.cs) | 113–187 xaml / 14–23 cs | Pres | window, DataTemplates, `MaterialThemeBridge.Attach` | N |
| BeamElevationCanvas / BeamSectionCanvas | A/View/Controls/*.cs | 131/142 | Pres | FrameworkElement `OnRender`, 50 ms debounce | N |
| BeamElevationPainter / BeamSectionPainter | A/View/Controls/*.cs | 442/174 | Pres | preview drawing with its own rebar arithmetic | N |
| BeamDrawPrimitives / CanvasPalette | A/View/Controls/*.cs | 103/164 | Pres | static draw helpers; theme brushes | N |
| BeamStirrupDistributionCalculator | C/Calculators/… | 312 | Dom | span/node/stack stirrup runs | N |
| BeamMainBarCalculator | C/Calculators/… | 442 | Dom | top/bottom polylines, splices, `SimplifyPolyline` | N |
| BeamAdditionalBarCalculator | C/Calculators/… | 463 | Dom | support top / span bottom polylines | N |
| BeamSideBarCalculator / BeamSpecialBarCalculator | C/Calculators/… | 200/262 | Dom | skin + ties / hanging + diagonal | N |
| BeamCanvasTransformCalculator | C/Calculators/… | 175 | Dom (UI maths) | mm → screen transforms | N |
| Core models (BeamContinuousStack 163, BeamSpan 102, BeamSupportNode 67, SecondaryBeamIntersection 65, BarPolyline 77, Polyline3 81, Point3/Vector3 58, 5 specs 73–113, StirrupRun 41, StirrupZone 47, Enums 98, ValidationResult 30) | C/Models/*.cs | — | Dom | immutable records, mm | N |
| Tolerance / GlobalUsings | C/*.cs | 31/1 | Dom | float compare; `BeamBarPolyline` alias | N |

## 4. Revit API boundary

**Pure logic stranded in the add-in, which could live in Core (it reads only mm or spec data):**

- `SectionViewCreator.ComputeCutStations` (A/Service/SectionViewCreator.cs:52). The orchestrator also calls it twice (:143, :166).
- `BeamSupportFinder.FindSupports` after its collectors (A/Service/BeamSupportFinder.cs:76-174): the 100 mm dedupe, cantilever detection (> 200 mm), synthetic joints (300 mm) and the SupportType mapping. `SynthesizeDefaultSupports` (:180) belongs here too.
- Span assembly in `BeamStackReader.Read` (A/Service/BeamStackReader.cs:66-100): clear length, startX and cantilever position.
- Validator thresholds once they are projected to mm (A/Service/BeamStackValidator.cs:14-16, :155, :174).
- All three `PlannedCount`s: A/Service/BeamRebarOrchestrator.cs:44, A/Service/RebarCreationService.cs:30, A/Service/DimensionCreator.cs:170.
- `RebarTableTagCreator.BuildRows` (A/Service/RebarTableTagCreator.cs:83), which only formats text.
- `BeamRebarSession.Validate` (A/ViewModel/BeamRebarSession.cs:392) and `ToSpec` (:455).
- Bar-type matching rule `RebarTypeCatalog.FindBarType` (A/Service/RebarTypeCatalog.cs:60). It is duplicated in `BeamRebarSession.FindBarType` (A/ViewModel/BeamRebarSession.cs:356) with a different tolerance: 0.5 mm in one, 1.0 mm in the other.
- Preview arithmetic re-implements Core:
  - Stirrup zones (A/View/Controls/BeamElevationPainter.cs:172-187) do not collapse spans under 600 mm the way C/Calculators/BeamStirrupDistributionCalculator.cs:106 does.
  - Additional-bar extents are re-derived at A/View/Controls/BeamElevationPainter.cs:278-328.
  - Section bar spacing (A/View/Controls/BeamSectionPainter.cs:72-120) re-derives C/Calculators/BeamMainBarCalculator.cs:21.
- `ValidationResult` / `ValidationMessages` (A/Model), `BeamSectionStyle` and `BeamStack.Summary` (A/Model/BeamStack.cs:99) are all Revit-free.

**Core types mirrored or duplicated on the add-in side:**

- `ValidationResult` exists in both A/Model and C/Models, with different shapes: a code + message on the add-in side, a string + warnings in Core.
- `BeamStack` re-exposes the Core stack: `Spans`, `Supports`, `SecondaryIntersections`, `TotalLength`, `OverallStart/EndX`, `MaxHeightMm` (A/Model/BeamStack.cs:24-40, :90).
- `BeamRebarSpec` carries `RebarTypeInfo` objects (A/Model/BeamRebarSpec.cs:28-43) alongside the Core specs' `*BarTypeName` strings and diameters. Bar identity is therefore stored twice.

## 5. Transactions, ExternalEvent, modeless

- **TransactionGroup** `"Beam Rebar"` is opened in `BeamRebarOrchestrator.Run` (A/Service/BeamRebarOrchestrator.cs:59) and committed with `Assimilate` (:82). On an exception it rolls back and rethrows (:92). It is opened inside the handler's `Execute`, as the convention requires.
- **10 inner Transactions** are split across 2 classes, and each one calls `RebarFailureHandling.Apply`:
  - The orchestrator owns 5: "Create Detail View" :100, "Create Section Views" :110, "Create Elevation Dimensions" :127, "Create Section Dimensions" :135 and "Create Beam Tables" :158.
  - `RebarCreationService.Create` owns the other 5: "Create Stirrups" :66, "Create Main Bars" :76, "Create Additional Bars" :86, "Create Side Bars" :96 and "Create Special Bars" :106.
  - The class doc says "Sole owner of the master TransactionGroup", which is true; the inner boundaries are not owned there.
- **ExternalEvent:**
  - The handler creates it in its constructor (A/BeamRebarExternalEventHandler.cs:27), on the command thread (API context).
  - `Execute` drains a `ConcurrentQueue` (:46).
  - It is disposed from the window's `Closed` event (A/BeamRebarCommand.cs:96-100 → handler `Dispose` :78).
  - The handler is IDisposable and owned only by the closure.
- **Modeless:**
  - A static `_window` field (A/BeamRebarCommand.cs:28) means a second click calls `Activate`.
  - The Owner is set via `WindowInteropHelper` (:94).
  - There is no `DialogResult`; `CloseRequested` is subscribed in the view's code-behind.
  - The result TaskDialog is shown from `Execute` (handler :64).
  - The `IsBusy` CanExecute guard blocks a double run.
- **Facts worth knowing:**
  - The document, `BeamStack` (PlanarFace refs) and catalog are captured when the window opens. Model edits or a document switch while the window stays open are not re-read.
  - `RebarCreationService.CanCreate` runs twice, at command :78 and in the orchestrator at :53.

## 6. Dependencies

- **Construction (`new`):**
  - The command builds everything by hand: SelectionFilter, Orchestrator, Handler, Session, ViewModel, LocalizationService, View. There is no DI container.
  - The ViewModel builds its 5 tab VMs (A/ViewModel/BeamRebarViewModel.cs:41-48).
  - The orchestrator builds its own `RebarTypeCatalog` (A/Service/BeamRebarOrchestrator.cs:31).
- **Static-heavy Service folder:** 18 of the 23 classes are static. Only `BeamRebarOrchestrator`, `PointMapper`, `RebarShapeResolver`, `RebarTypeCatalog` and `LocalizationService` are instances.
  - Creators call Core calculators statically.
  - `BeamMainBarCreator.CreateBarFromPolyline` / `BuildCurves` are called by the Additional, Side and Special creators.
  - `BeamStirrupCreator.SetPartition` is called by the Main, Side and Special creators.
  - `DetailViewCreator.Rename` / `ResolveViewType` are called by `SectionViewCreator`.
- **Hidden dependencies:**
  - Static Serilog `Log` is used in 13 files.
  - TaskDialog is shown from the ViewModel.
  - The bar catalog is read 3 times per command, through 3 separate collector passes: A/BeamRebarCommand.cs:89, A/Service/BeamDefaultSpecBuilder.cs:16, A/Service/BeamRebarOrchestrator.cs:31.
  - The span cover is fixed at 25 mm (A/Service/BeamStackReader.cs:96), and the stirrups and additional bars read that value, not the UI cover. See smell #2.
- **Constructors with more than 4 parameters:**
  - `BeamRebarOrchestrator` (5; the `faces` parameter is ignored, :34-42).
  - Core `BeamSpan` (14, C/Models/BeamSpan.cs:13), `BeamSideBarSpec` (11, C/Models/BeamSideBarSpec.cs:13), `BeamSupportNode` (8) and `SecondaryBeamIntersection` (8).
- **Methods with more than 4 parameters:**
  - `BeamStirrupCreator.PlaceNodeStirrupRun` (10, :119), `PlaceStirrupRun` (8, :78) and `Create` (7).
  - `BeamSideBarCreator.Create` (8).
  - Main, Additional and Special `Create` (7 each).
  - `RebarCreationService.Create` (6).
  - `SectionViewCreator.CreateSectionAtStation` (7).
  - `RebarTableTagCreator.Create` (7) and `WriteRow` (9, :115).
  - `BeamSupportFinder.FindSupports`, `FindSecondaryBeams` and `MeasureGirderSupport` (5 each).
  - `BeamDrawPrimitives.DimensionHorizontal` / `DimensionVertical` (8) and `Line` / `Box` / `Circle` / `Caption` (6).
- **Boolean flags and magic codes:**
  - `RebarShapeResolver.Require(bool needsCrossTies, bool needsSpecialStirrups)`: the second flag is unused, and the method is never called.
  - `ComputeSpanRuns(…, bool isCantilever)`.
  - `BeamDrawPrimitives.Caption(…, bool center)`.
  - `RebarShapeResolver.CrossTie(int tieType)` takes a magic int code (:38).
  - `SetLayoutAsNumberWithSpacing(…, true, true, true)` passes 3 bare booleans (A/Service/BeamStirrupCreator.cs:111, :154).

## 7. Size metrics

- **Files over 300 lines:**
  - A/ViewModel/BeamRebarSession.cs 536 (3 classes; the session itself is about 420 lines).
  - C/Calculators/BeamAdditionalBarCalculator.cs 463.
  - A/Service/BeamSupportFinder.cs 443.
  - A/View/Controls/BeamElevationPainter.cs 442.
  - C/Calculators/BeamMainBarCalculator.cs 442.
  - C/Calculators/BeamStirrupDistributionCalculator.cs 312.
- **Methods over 50 lines** (brace-count script, approximate): 19 of 128 add-in methods and 10 of 30 Core methods. Top ones: C `ComputeSupportTopBars` 323, C `ComputeBottomMainBars` 206, C `ComputeSpanRuns` 205, A `FindSupports` 158, C `ComputeTopMainBars` 135, A `BeamSectionPainter.Paint` 132, A `BeamStackReader.Read` 115, C `ComputeSpanBottomBars` 92, A `BeamDefaultSpecBuilder.Build` 90, A `FindSecondaryBeams` 84, A `BeamRebarCommand.Execute` 84, A `Session.ToSpec` 81, A `BeamSpecialBarCreator.Create` 80, A `RebarCreationService.Create` 74, C `ComputeLongitudinalSideBars` 73, A `BeamSideBarCreator.Create` 72, C `ComputeCrossTies` 72, A `PaintMainLongitudinalBars` 70, A `Session.Validate` 62.
- **ViewModels over 250 lines:** `BeamRebarSession` (536 file). `BeamRebarViewModel` is 114 (OK).
- **Nesting hotspots:**
  - C/Calculators/BeamAdditionalBarCalculator.cs:30-340 (foreach › if › if › for › initializer).
  - A/Service/BeamSpecialBarCreator.cs:30-75 (if › foreach › if, then if › foreach).
  - A/Service/BeamSideBarCreator.cs:45-84 (if › if › foreach).
  - A/Service/BeamSupportFinder.cs:319-336 (if › foreach › if) and :359-376.
  - A/View/Controls/BeamElevationPainter.cs:63 (LINQ chains at 9 indents).

## 8. Duplication with other features

| Helper | Copies (add-in unless C) | Diff vs Beam |
|---|---|---|
| RevitUnits | Beam, Column, Foundation, KataExport | Column 4 lines (namespace only); Foundation 12; Kata 13 |
| RebarFailureHandling (SwallowWarnings) | Beam, Column, Foundation | Foundation 6 lines; Column 30 |
| RevitDialogs | Beam, Column, Foundation, KataExport | 17–48 lines |
| LocalizationService / UiStrings(Catalog) | Beam, Column | Localization 9 lines |
| PointMapper | Beam, Column (+ KataRebar **uses Beam's**) | 65 lines |
| RebarShapeResolver / RebarTypeCatalog / RebarTypeInfo | Beam, Column (+ KataRebarShapeResolver, KataRebarTypeResolver) | 66 / 97 / 6 lines |
| ValidationResult / ValidationMessages | Beam, Column | 13 / 40 lines |
| DetailViewCreator / SectionViewCreator / DimensionCreator / RebarTableTagCreator | Beam, Column (same names and roles, different content) | 156–223 lines |
| *SolidFaceReader | Beam, Column, Foundation | role duplicate |
| `LookupParameter("Partition")` setter | Beam `BeamStirrupCreator:162`, Column `MainBarCreator` | — |
| CreateFromCurves + CS0618 pragma block | Beam ×3, Foundation, KataRebar | — |
| C: Point3 | Beam, Column, Foundation | — |
| C: Polyline3 / Vector3 | Beam, Foundation | — |
| C: Tolerance | Beam, Column | — |

- **Coupling the other way:**
  - The KataRebar add-in imports `HPRebar.BeamRebar.Service.PointMapper` and `RevitDialogs` (HPRebar/HPRebar/KataRebar/Service/KataBeamPlacement.cs:34, KataRebar/KataRebarCommand.cs:71).
  - 13 Core KataRebar files import `HPRebar.Core.BeamRebar.Models` (Point3, Polyline3, HookAngle, BarType).
  - BeamRebar is therefore a de-facto shared kernel, and moving or renaming it breaks KataRebar.

## 9. Multi-version

- **`#if` blocks:** none in A or C.
- **4 `#pragma warning disable CS0618` sites, all tagged `// Multi-version:`:**
  - A/Service/BeamMainBarCreator.cs:72-86, A/Service/BeamSideBarCreator.cs:63-77 and A/Service/BeamSpecialBarCreator.cs:55-69 each call `Rebar.CreateFromCurves` with `RebarHookOrientation.Right` ×2, so 6 references in total.
  - A/Service/BeamSupportFinder.cs:268-270 calls `Curve.Intersect(Curve, out IntersectionResultArray)`.
- **The comments understate the problem.** They say "deprecated in Revit 2026, required for 2023–2025". CLAUDE.md says both APIs were **removed in 2027**, so a pragma cannot help and R27 fails. That was not re-built here.
- **Stable path already in use:** stirrups use `Rebar.CreateFromRebarShape` (A/Service/BeamStirrupCreator.cs:101, :144), the same API ColumnRebar uses.
- **Other version-sensitive APIs:** `Category.BuiltInCategory` (A/BeamRebarSelectionFilter.cs:16) and `UnitTypeId` (A/Service/RevitUnits.cs:12) both need R2023+, which is fine for R23–R27. No `ElementId.Value` / `IntegerValue` usage.

## 10. Testability

**Tests (T/, xUnit v3, no Revit):** 101 `[Fact]` + 4 `[Theory]` (21 cases), i.e. 105 methods and about 122 cases.

| Test file | Facts | Theories (cases) |
|---|---|---|
| BeamMainBarCalculatorTests | 22 | — |
| BeamAdditionalBarCalculatorTests | 19 | — |
| BeamStirrupDistributionCalculatorTests | 18 | 2 (7) |
| BeamSpecialBarCalculatorTests | 16 | — |
| BeamCanvasTransformCalculatorTests | 14 | — |
| BeamSideBarCalculatorTests | 12 | 2 (14) |

Shared fixture: TestBeamData.cs.

**Untested Core:**
- `BeamContinuousStack.Validate` (never called anywhere), `FindSpanAt`, `OverallStartX`/`EndX`, `MaxHeight`.
- `Polyline3.Simplify` / `Point3` / `Vector3` / `Tolerance`.
- `BeamSideBarCalculator.RequiresSideBars`.
- Only lightly tested: `ComputeSectionTransform` (2 references), `ComputeZoneLengths` (1), `ComputeNodeRun` (1).

**Add-in: 0 tests.**
- `HPRebar.Tests` (TUnit) holds only Column tests.
- `HPRebar.Core.Tests` cannot reference the Revit add-in.
- So all of section 4's pure logic is untested because of where it lives, not because it touches Revit: support classification, cut stations, span assembly, PlannedCount, BuildRows, Session.Validate/ToSpec, FindBarType and the preview arithmetic.

**Revit-bound code** (readers, creators, view/dim/table creators, the orchestrator) needs a Revit host. There is no fixture model.

## 11. Smells (24 most significant)

| # | Location | Category | Fact |
|---|---|---|---|
| 1 | A/ViewModel/BeamRebarSession.cs:174-183 | hidden dependency | Views-tab props bound in XAML are never read by `ToSpec` (:455-535): CreateElevationView, CreateSectionViews, SectionsPerSpan, DetailViewName, SectionPrefix, ElevationScale, CreateDimensions, CreateTags, UseRealRebar. The orchestrator always uses `BeamAnnotationSettings.Load` defaults (SectionsPerSpan = 3, A/Model/BeamAnnotationSettings.cs:67) |
| 2 | A/Service/BeamStackReader.cs:96 | hidden dependency | `span.Cover` is hard-coded 25. Span stirrups (A/Service/BeamStirrupCreator.cs:88-95) and Core additional bars (C/Calculators/BeamAdditionalBarCalculator.cs:47) use it, so the UI Cover reaches only main/side bars and node stirrups |
| 3 | A/ViewModel/BeamRebarViewModel.cs:73,102 | Revit-boundary leak | The VM calls `RevitDialogs` (Autodesk TaskDialog) directly; the `IBeamRebarRunner` seam is bypassed |
| 4 | A/Model/BeamAnnotationSettings.cs:29-69 | SRP / Revit-boundary leak | A Model type runs 3 FilteredElementCollectors and matches by display strings "Structural" (:42) and "Linear Dimension Style" (:60). The convention says readers go in Service/ |
| 5 | A/Model/BeamStack.cs:7,48 | coupling | Model depends on Service (`PointMapper`); dependency points Model → Service |
| 6 | A/Service/BeamSupportFinder.cs:18-175 | long method | `FindSupports` (158 lines) mixes collectors with pure mm classification, plus magic 1.0/3.0/0.5 ft (:37-39), 100/200/300 mm (:79, :108, :134) |
| 7 | A/Service/BeamSupportFinder.cs:25,106,310,387,411 | duplication | The anonymous 5-field tuple type is repeated 5× in place of a record |
| 8 | C/Models/Enums.cs:7-8 | naming | `SupportType.Column` and `InteriorColumn` both = 1, so the branches at A/Service/BeamSupportFinder.cs:158 are no-ops. `StirrupDistributionType` (:31) duplicates `StirrupLayout` via an int cast (C/Calculators/BeamStirrupDistributionCalculator.cs:282) |
| 9 | C/Calculators/BeamAdditionalBarCalculator.cs:22-344 | long method / duplication | `ComputeSupportTopBars` is 323 lines with 6 near-identical layer × position blocks (8 `new BarPolyline` in the file) |
| 10 | C/Calculators/BeamMainBarCalculator.cs:50,189 | duplication | Top (135) and bottom (206) bar methods mirror each other. The hook default `Math.Max(30.0*d, 200.0)` is repeated 10× in Core |
| 11 | C/Calculators/BeamStirrupDistributionCalculator.cs:20-224 | long method / duplication | `ComputeSpanRuns` is 205 lines; the StirrupRun + positions block is repeated 5× (7 `new StirrupRun`) |
| 12 | A/View/Controls/BeamElevationPainter.cs:148-205,278-328 | duplication | The preview recomputes stirrup zones and add-bar extents itself (no 600 mm collapse), so it can diverge from what Core builds |
| 13 | A/View/Controls/BeamSectionPainter.cs:42-173 | long method / duplication | `Paint` is 132 lines; bar spacing is re-derived instead of `ComputeTransverseYPositions` |
| 14 | A/Service/BeamStirrupCreator.cs:78-117 vs :119-160 | duplication | `PlaceStirrupRun` and `PlaceNodeStirrupRun` are ~90% identical; the node one takes 10 params |
| 15 | A/Service/BeamMainBarCreator.cs:65-86, BeamSideBarCreator.cs:55-77, BeamSpecialBarCreator.cs:48-69 | duplication | 3 copies of host-index clamp + `CreateFromCurves` + pragma |
| 16 | A/Service/BeamStirrupCreator.cs:162; A/Service/BeamMainBarCreator.cs:58,92 | coupling | A generic param setter lives in the stirrup creator and generic curve builders in the main-bar creator, and sibling creators call both statically |
| 17 | A/Service/RebarCreationService.cs:16-28 vs A/Service/RebarShapeResolver.cs:53-62 | duplication | Identical shape preflight and strings. `Require` is unused. Codes 20/21 also carry different text in A/Model/ValidationMessages.cs:26-27 |
| 18 | A/Model/ValidationResult.cs vs C/Models/ValidationResult.cs | duplication / naming | Two `ValidationResult` records with incompatible shapes. Core `BeamContinuousStack.Validate` (C/Models/BeamContinuousStack.cs:118) is never called and has an empty if-body (:144-147) |
| 19 | A/Model/BeamFaces.cs:45-54, A/Model/BeamStack.cs:45-69, A/Model/CreatedBeamRebar.cs:23-28, C/Models/BeamStirrupSpec.cs:78-86 | premature abstraction | Alias props "for cross-plan compatibility". `CreatedBeamRebar.MainBottomBars` / `AdditionalBottomBars` return empty while `MainTopBars` returns all main bars |
| 20 | A/Service/BeamRebarOrchestrator.cs:34-42; A/ViewModel/BeamRebarSession.cs:198; A/ViewModel/IBeamRebarRunner.cs:14 | ISP / premature abstraction | The extra ctor ignores `faces`. The session ctor duplicates `stack.Faces`. `LastResult` is in the runner interface but never read |
| 21 | A/Service/BeamSolidFaceReader.cs:18-28,63,89,117 | hidden dependency | Every face/b/h getter re-runs `get_Geometry`; `GetSectionStyle` extracts geometry about 7× per beam, and the validator, reader and finder repeat it |
| 22 | A/Service/DimensionCreator.cs:19-25 | Revit-boundary leak | References are built by string-replacing `SURFACE` → `LINEAR` in the stable representation |
| 23 | A/Model/BeamStack.cs:96; A/View/Controls/BeamDrawPrimitives.cs:26 | duplication / static state | `/ 304.8` bypasses `RevitUnits` (the documented "single boundary"). A static mutable `PixelsPerDip` is written from each canvas `OnRender` (A/View/Controls/BeamElevationCanvas.cs:85) |
| 24 | A/Service/RebarTableTagCreator.cs:60; A/Service/RebarTypeCatalog.cs:58,76,84; A/Service/RevitDialogs.cs:19; A/Service/BeamSolidFaceReader.cs:162; C/Models/StirrupZone.cs | premature abstraction | Dead members: `TagRebarOnElevation`, `BarTypesList`, `FindHook`, `DefaultCoverMm`, `Confirm`, `ProjectToPlane`, `StirrupZone`, `BeamStack.Summary`. `ComputeStackRuns` is used only by tests |

Also noted, not ranked:

- C/Models/BeamSpan.cs:13-27 has 14 constructor params, some PascalCase (`TopOffsetMm`, `CoverMm`, `ClearLengthMm`), and they silently override `topElevation` / `cover` / `clearLength`.
- `AdditionalBarsTabViewModel` (:27, :30) re-declares the base apply-to-all commands (A/ViewModel/Tabs/BeamRebarTabViewModel.cs:30, :34).
- Polylines are simplified twice: `BeamMainBarCalculator.SimplifyPolyline` (C:399), then `Polyline3.Simplify` in `BuildCurves` (A/Service/BeamMainBarCreator.cs:94).
- `DimensionCreator.PlannedCount` (:170) assumes 2 sections per span while the settings use 3.

**Status:** DONE_WITH_CONCERNS
**Summary:** BeamRebar mapped (61 + 6 add-in files, 25 Core, 8 tests); 24 smells with file:line.
**Concerns:** No build or run (read-only, concurrent session). The R27 failure and the lack of runtime verification come from CLAUDE.md. Smells #1 and #2 are behaviour-level facts (UI settings never reach the model) and need a product-owner look before any refactor.

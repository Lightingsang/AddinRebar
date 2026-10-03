# Map 02 — ColumnRebar + FoundationRebar (add-in + Core + tests)

Read-only map, 2026-10-03. No build/test run. LOC = `wc -l`; method lengths = brace-matching script (signature → closing brace); test counts = `[Fact]/[Theory]/[Test]` grep.
Abbrev: `C/` = HPRebar/HPRebar/ColumnRebar/, `F/` = HPRebar/HPRebar/FoundationRebar/, `CC/` = HPRebar/HPRebar.Core/ColumnRebar/, `CF/` = HPRebar/HPRebar.Core/FoundationRebar/, `T/` = HPRebar/HPRebar.Core.Tests/, `TU/` = HPRebar/HPRebar.Tests/.

| Area | .cs files / LOC | XAML files / LOC |
|---|---|---|
| C/ (Service 3004, View 1130, ViewModel 1028, Model 615, root 263) | 71 / 6040 | 9 / 852 |
| F/ | 20 / 1198 | 3 / 417 |
| CC/ | 25 / 1591 | — |
| CF/ | 12 / 1229 | — |

## A. ColumnRebar

### A1. Behaviour
Ribbon HPRebar ▸ Rebar ▸ Column Rebar (Application.cs:64). User multi-picks structural columns (bottom→top). Tool validates the stack (14 rules), reads it to mm numbers, opens a modeless 8-tab window (settings, geometry, stirrups, cross-ties, bars, top/bottom dowels, bar schedule) with live section/elevation previews. "Run" creates in one undo step: 2 elevation section views (`@ColumnDetail`), 1 cross-section view per segment (`@ColumnSection`), elevation + section dimensions, perimeter ties + cross-ties (shape-driven `Rebar.CreateFromRebarShape`, M_T1/M_T3/M_T10*), free-form main bars (`Rebar.CreateFreeForm`) with splices/dowels, and a detail-line + TextNote bar table beside each section. Result shown as TaskDialog.

### A2. Flow
```
ColumnRebarCommand.Execute (C/ColumnRebarCommand.cs:29)
  ├─ _window != null → Activate (:35-39)
  ├─ PickObjects + ColumnRebarSelectionFilter (:45)
  ├─ OrderBy ColumnStackReader.BottomFace (:62) → ColumnStackValidator.Validate (:65) → ColumnStackReader.Read (:74)
  ├─ DefaultRebarSpecBuilder.Build (:78) → RebarShapeResolver.Load (:86) → RebarCreationService.CanCreate (:87)
  ├─ AnnotationSettings.Load (:95) → new ColumnRebarOrchestrator(doc, stack, shapes, annotation) (:96)
  ├─ new ColumnRebarExternalEventHandler(orchestrator) (:97) → RebarTypeCatalog.BarTypes (:98) → new ColumnRebarSession (:99)
  └─ new ColumnRebarView(new ColumnRebarViewModel(session, new LocalizationService(), handler)) → Owner (:102) → Show (:114)
ColumnRebarViewModel.RunAsync (C/ViewModel/ColumnRebarViewModel.cs:79) → Session.IsValid → Session.ToSpecs
  → IColumnRebarRunner.RunAsync = handler.RunAsync (C/ColumnRebarExternalEventHandler.cs:36) → queue ColumnRebarRequest + ExternalEvent.Raise
Revit thread: handler.Execute (:47) → ColumnRebarOrchestrator.Run (C/Service/ColumnRebarOrchestrator.cs:52)
  → CanCreate again (:54) → TransactionGroup "Column Rebar" (:58)
     → CreateViews: DetailViewCreator.Create, SectionViewCreator.Create
     → CreateDimensions: DimensionCreator.CreateOnElevation ×2, CreateOnSection ×n
     → RebarCreationService.Create: StirrupCreator, AdditionalTieCreator, MainBarCreator (PointMapper, Core BarLayoutCalculator/SpliceCalculator/BarPolylineBuilder)
     → CreateTables: RebarTableTagCreator.Create ×n
  → Assimilate | RollBack+rethrow → handler: RevitDialogs.Info (:65) + TCS result → VM CloseRequested → View.Close
```

### A3. Class inventory
| Class | File | LOC | Layer | Responsibility | Revit API |
|---|---|---|---|---|---|
| ColumnRebarCommand | C/ColumnRebarCommand.cs | 122 | Entry | pick, validate, read, compose graph, open window | Y |
| ColumnRebarExternalEventHandler | C/ColumnRebarExternalEventHandler.cs | 92 | Entry | queue + ExternalEvent, result dialog | Y |
| ColumnRebarRequest | C/ColumnRebarRequest.cs | 34 | Entry | specs + progress + TCS | N |
| ColumnRebarSelectionFilter | C/ColumnRebarSelectionFilter.cs | 15 | Entry | OST_StructuralColumns only | Y |
| ColumnRebarOrchestrator | C/Service/ColumnRebarOrchestrator.cs | 182 | App-orch | TransactionGroup + 5 named transactions | Y |
| RebarCreationService | C/Service/RebarCreationService.cs | 213 | App-orch | CanCreate, PlannedCount, 2 transactions, polyline pipeline | Y |
| ColumnStackValidator | C/Service/ColumnStackValidator.cs | 295 | Revit-int | 14 ordered rules, int codes | Y |
| ColumnStackReader | C/Service/ColumnStackReader.cs | 214 | Revit-int | Element → ColumnStack (ft→mm), datum search | Y |
| ColumnSolidFaceReader | C/Service/ColumnSolidFaceReader.cs | 244 | Revit-int | solids, faces by normal, section style | Y |
| ColumnNeighbourFinder | C/Service/ColumnNeighbourFinder.cs | 121 | Revit-int | beams at top/base, foundation/floor/wall under | Y |
| StirrupGeometry (+StirrupPlacement) | C/Service/StirrupGeometry.cs | 206 | Revit-int | tie frames (mm→ft XYZ), 7 placements | Y |
| StirrupCreator | C/Service/StirrupCreator.cs | 61 | Revit-int | perimeter ties | Y |
| AdditionalTieCreator | C/Service/AdditionalTieCreator.cs | 276 | Revit-int | cross-ties rect/circle, ScaleToBox workaround | Y |
| MainBarCreator | C/Service/MainBarCreator.cs | 126 | Revit-int | free-form bar, simplify, Partition | Y |
| DetailViewCreator | C/Service/DetailViewCreator.cs | 178 | Revit-int | 2 elevations, view type resolve/duplicate, Rename | Y |
| SectionViewCreator | C/Service/SectionViewCreator.cs | 126 | Revit-int | section per segment, CutHeight | Y |
| DimensionCreator | C/Service/DimensionCreator.cs | 158 | Revit-int | elevation chain + section spans, SURFACE→LINEAR ref hack | Y |
| RebarTableTagCreator | C/Service/RebarTableTagCreator.cs | 174 | Revit-int | detail-line/TextNote table | Y |
| PointMapper | C/Service/PointMapper.cs | 50 | Revit-int | Core Point3 (mm) → XYZ (ft) | Y |
| ColumnPlanAxes | C/Service/ColumnPlanAxes.cs | 30 | Revit-int | east/north axes of a segment | Y |
| DefaultRebarSpecBuilder | C/Service/DefaultRebarSpecBuilder.cs | 63 | App/Domain mix | default specs per section | Y (catalog) |
| RebarShapeResolver | C/Service/RebarShapeResolver.cs | 73 | Revit-int | shape lookup by fixed names, Require | Y |
| RebarTypeCatalog | C/Service/RebarTypeCatalog.cs | 60 | Revit-int | bar/cover types + default picks | Y |
| RebarFailureHandling / RevitDialogs / RevitUnits | C/Service/*.cs | 49/61/23 | Revit-int | warning swallow / TaskDialog / unit conversion | Y |
| LocalizationService | C/Service/LocalizationService.cs | 21 | Presentation | EN/VI toggle (ObservableObject in Service/) | N |
| AnnotationSettings | C/Model/AnnotationSettings.cs | 94 | Model+Revit-int | view templates/dim/text types + offsets (static Load queries doc) | Y |
| ColumnStack / ColumnFaces | C/Model/*.cs | 72/39 | Model | Core sections + Revit faces/elements | Y |
| ColumnRebarSpec / RebarTypeInfo | C/Model/*.cs | 26/13 | Model | per-segment spec; bar type (holds RebarBarType) | Y (indirect) |
| CreatedRebar / CreatedViews / OrchestratorResult | C/Model/*.cs | 17/19/18 | Model | run results | Y |
| ValidationResult / ValidationMessages / ColumnSectionStyle | C/Model/*.cs | 25/38/10 | Model | int-coded result, EN text, Other/Rect/Circ | N |
| UiStrings / UiStringsCatalog | C/Model/*.cs | 127/117 | Presentation | label records EN/VI | N |
| ColumnRebarViewModel (+IColumnRebarRunner) | C/ViewModel/ColumnRebarViewModel.cs | 147 | Presentation | nav, run, progress, language | N (indirect) |
| ColumnRebarSession | C/ViewModel/ColumnRebarSession.cs | 138 | Presentation | shared tab state, ToSpecs, ApplySelectedToAll | N (holds ColumnStack) |
| ColumnSpecEditor | C/ViewModel/ColumnSpecEditor.cs | 320 | Presentation+Domain | editable spec + validation rules | N (RebarTypeInfo) |
| BarSpliceEditor | C/ViewModel/BarSpliceEditor.cs | 56 | Presentation | editable SpliceSpec | N |
| 8 tab VMs + abstract base | C/ViewModel/Tabs/*.cs | 367 | Presentation | per-tab lists/commands; BarsDivision recomputes schedule | N |
| Canvas controls/painters (9) | C/View/Controls/*.cs | 1046 | Presentation | WPF previews; ElevationBars reruns Core pipeline | N |
| Views (window + 8 tabs) | C/View/**/*.xaml(.cs) | 852 xaml | Presentation | MaterialDesign UI; code-behind = Init + DataContext + theme | N |
| Core calculators (11) | CC/*.cs | 1078 | Domain | layout, splice, polyline, schedule, shape/side classify, stirrup runs, canvas scale | N |
| Core models (13) | CC/Models/*.cs | 513 | Domain | ColumnSection, specs, Point3/PlanPoint, BarPolyline… | N |

### A4. Revit API boundary
Pure logic sitting in the add-in (Core candidates):
- ColumnSpecEditor.Validate/ValidateTies C/ViewModel/ColumnSpecEditor.cs:84-177 (clearance, 1002 positions, cross-tie rules).
- RebarCreationService.PlannedCount/RunsFor/CrossTieCount/BuildPolylines C/Service/RebarCreationService.cs:45,139,167,174 (pure apart from host/bar-type tagging).
- SectionViewCreator.CutHeight C/Service/SectionViewCreator.cs:51; RebarTableTagCreator.Rows/Spacing C/Service/RebarTableTagCreator.cs:71-98.
- MainBarCreator.Simplify C/Service/MainBarCreator.cs:96 (operates on Core Point3).
- AdditionalTieCreator inset/spacing arithmetic :80, :94, :129, :143; shape-number mapping `TypeV + 1` :199.
- DefaultRebarSpecBuilder rules :29-57; RebarTypeCatalog.DefaultBarType/DefaultCoverMm index picks :39, :54.
- ColumnStackValidator arithmetic of rules 7/8 (DoesNotGrowUpward, SitsWithin) :103-174 — could run on ColumnSection numbers after Read.
Mirrored / duplicated with Core:
- ColumnSectionStyle (C/Model/ColumnSectionStyle.cs) ≈ Core SectionShape + `Other`; mapped at C/Service/ColumnStackReader.cs:91.
- ColumnStackValidator.AreEqual :293 and ColumnSolidFaceReader.IsAngle :242 re-implement CC/Tolerance.cs:11 (same 1e-9).
- Stirrup diameter carried twice: BarLayoutSpec.StirrupDiameter (CC/Models/BarLayoutSpec.cs:174) and ColumnRebarSpec.StirrupBarType.DiameterMm, both passed separately to SpliceCalculator (RebarCreationService.cs:150-155).
- Unit conversion: own RevitUnits (C/Service/RevitUnits.cs); every Revit-int service converts inline (StirrupGeometry 30+ MmToFt calls).

### A5. Transactions / events / window
- TransactionGroup "Column Rebar" C/Service/ColumnRebarOrchestrator.cs:58; Assimilate :77; RollBack+rethrow :88.
- Nested Transactions (sequential, each + RebarFailureHandling.Apply after Start): "Create Detail View" :99, "Create Section View" :111, "Create Dimension View" :126, "Create Dimension Section" :146, "Create Tag Bars" :168; in RebarCreationService "Create Stirrup Bars" :76, "Create Main Bars" :110.
- Side effect inside a transaction: DetailViewCreator.ResolveViewType duplicates a ViewFamilyType (:53-67).
- ExternalEvent created in handler ctor C/ColumnRebarExternalEventHandler.cs:28, disposed via view.Closed (C/ColumnRebarCommand.cs:104-108). Execute ignores its `UIApplication` (:47); document is the one captured at command time (:96).
- Modeless: static `_window` :27, Activate :35-39, Owner via WindowInteropHelper :102, VM `CloseRequested` → `Close` (C/View/ColumnRebarView.xaml.cs:18). No DialogResult.

### A6. Dependencies
- Composition root = command (7 `new`s, :96-100); VM ctor `new`s 8 tab VMs (ColumnRebarViewModel.cs:54-64).
- Everything else static: orchestrator calls 6 static creators; no interface below IColumnRebarRunner → orchestrator only testable in Revit.
- Hidden deps: static Serilog `Log` in every service; RevitDialogs (UI) from handler; geometry re-read on every ColumnSolidFaceReader call (no caching; GetSectionStyle → GetSouth/North/East/West → GetVerticalFaces → get_Geometry each); ColumnNeighbourFinder.FindSupport runs `GetLowestLevel` collector per call (:85), called up to 4× in validator rules 11-14 + again in reader (:177).
- Ctor params > 4: none (orchestrator = 4). Methods > 4 params: AdditionalTieCreator.Create 11 (:22), CircleCross 10 (:186), RectangleHorizontal/Vertical/CircleClosed 9, StirrupCreator.Create 8 (:12), ColumnStackReader.ReadSection 8 (:76), DetailViewCreator.Create 8 (:70), RebarTableTagCreator.WriteRow 8 (:101), StirrupGeometry.Rectangle 7, DimensionCreator.CreateSpan 7, PlaceCrossTie/Place 7, MainBarCreator.Create 6, RebarTableTagCreator.Create 6; Core SpliceCalculator.ComputeUpperPositions 6 / RectangleTransition 7 / CircularTransition 8, BarPolylineBuilder.Build 6.
- Boolean flags: DimensionCreator.CreateOnElevation(acrossWidth) :20; DetailViewCreator.AcrossDirection/PlanHalfWidth(acrossWidth) :161,:170; ColumnStackValidator.SitsOn(useTopFace) :253 — every caller passes `true` (:212,:221,:231); ColumnNeighbourFinder.FindSupport(requireExactlyTwoFaces) :78; Core StirrupDistributionCalculator.ComputeRunLength(tiesUp).

### A7. Size
- > 300 lines: C/ViewModel/ColumnSpecEditor.cs 320 (also the only VM > 250). Near: ColumnStackValidator 295, AdditionalTieCreator 276.
- Methods > 50 lines: ColumnRebarCommand.Execute 93 (:29), RebarCreationService.Create 74 (:63), ColumnSolidFaceReader.GetSectionStyle 62 (:142), ColumnSpecEditor.ValidateTies 57 (:121), DistributionDiagram.OnRender 51; Core BarShapeClassifier.Classify 84 (CC/BarShapeClassifier.cs:14), SpliceCalculator.RectangleTransition 75 (:73), BarLayoutCalculator.ComputeRectangle 68 (:27), SpliceCalculator.CircularTransition 58 (:149).
- Nesting hotspots: ColumnSolidFaceReader.GetSolids :20 (4 levels), ColumnStackValidator.SitsWithinTheColumnBelow :128 (3), BarShapeClassifier.Classify (3).

## B. FoundationRebar

### B1. Behaviour
Ribbon HPRebar ▸ Rebar ▸ Foundation Rebar (Application.cs:66). User picks one Floor; tool checks it is a horizontal positive-thickness slab, snapshots its oriented bounding frame (mm), opens a modeless window (geometry read-out + spec: 4 diameters, 4 spacings, 3 covers, top-mat toggle, 90° hook). "Apply" creates a 2-mat 2-way mesh: one `Rebar.CreateFromCurves` element per bar (BottomX/BottomY/TopY/TopX), Partition "Foundation", summary TaskDialog (counts, length, weight).

### B2. Flow
```
FoundationRebarCommand.Execute (F/FoundationRebarCommand.cs:27) → _window? Activate (:32)
  → PickObject + FoundationRebarSelectionFilter (:42) → FoundationRebarValidator.Validate (:59) → `is Floor` (:68)
  → FoundationSessionBuilder.Build (:76) [FoundationSolidFaceReader.Read + bar-type collector]
  → new FoundationRebarExternalEventHandler(new FoundationRebarOrchestrator()) (:87) → new View(new VM(session, handler)) → Show
FoundationRebarViewModel.ApplyAsync (F/ViewModel/FoundationRebarViewModel.cs:46) → Setting.ToSpec → FoundationValidationCalculator.Validate (:49)
  → Session.Spec = spec (:60) → IFoundationRebarRunner.RunAsync(Session)
Revit thread: handler.Execute (F/FoundationRebarExternalEventHandler.cs:50) → FoundationRebarOrchestrator.Run (F/Service/FoundationRebarOrchestrator.cs:17)
  → TransactionGroup "Foundation Rebar" (:23) → FoundationMeshCalculator.Calculate (:28, validates again)
  → Transaction "Create Foundation Reinforcement" (:39) → FoundationRebarCreationService.CreateRebars (:44)
  → Assimilate | RollBack+rethrow → RevitDialogs.Info summary (handler :62) → TCS → VM CloseRequested
```

### B3. Class inventory
| Class | File | LOC | Layer | Responsibility | Revit API |
|---|---|---|---|---|---|
| FoundationRebarCommand | F/FoundationRebarCommand.cs | 113 | Entry | pick, validate, build session, open window | Y |
| FoundationRebarExternalEventHandler | F/FoundationRebarExternalEventHandler.cs | 105 | Entry | queue + ExternalEvent, summary dialog | Y |
| FoundationRebarRequest | F/FoundationRebarRequest.cs | 26 | Entry | session + TCS | N (indirect) |
| FoundationRebarSelectionFilter | F/FoundationRebarSelectionFilter.cs | 28 | Entry | Floor / OST_Floors | Y |
| FoundationSession | F/Model/FoundationSession.cs | 59 | Model | Document, Floor, snapshot, bar types, mutable spec, nearest bar type | Y |
| FoundationRebarOrchestrator | F/Service/FoundationRebarOrchestrator.cs | 60 | App-orch | group + 1 transaction | Y |
| FoundationRebarCreationService | F/Service/FoundationRebarCreationService.cs | 120 | Revit-int | Polyline3 → CreateFromCurves | Y |
| FoundationRebarValidator (+FoundationValidation) | F/Service/FoundationRebarValidator.cs | 90 | Revit-int | category, volume, horizontal faces, thickness | Y |
| FoundationSessionBuilder | F/Service/FoundationSessionBuilder.cs | 36 | Revit-int | snapshot + bar types + default spec | Y |
| FoundationSolidFaceReader | F/Service/FoundationSolidFaceReader.cs | 206 | Revit-int + domain math | largest solid, top/bottom faces, oriented bounds → snapshot | Y |
| RebarFailureHandling / RevitDialogs / RevitUnits | F/Service/*.cs | 35/18/15 | Revit-int | copies of the Column helpers | Y |
| FoundationRebarViewModel | F/ViewModel/FoundationRebarViewModel.cs | 86 | Presentation | apply/cancel, validation message | N (exposes Session) |
| FoundationSettingViewModel / FoundationGeometryViewModel | F/ViewModel/*.cs | 105/37 | Presentation | spec editing / read-only geometry | N |
| IFoundationRebarRunner | F/ViewModel/IFoundationRebarRunner.cs | 17 | Presentation | runner contract (takes FoundationSession) | N (indirect) |
| Views (window, geometry, setting) | F/View/*.xaml(.cs) | 417 xaml | Presentation | UI | N |
| FoundationMeshCalculator | CF/Calculators/FoundationMeshCalculator.cs | 380 | Domain | bar positions, 4 layers, hooks, stats | N |
| FoundationValidationCalculator | CF/Calculators/FoundationValidationCalculator.cs | 125 | Domain | spec × snapshot checks | N |
| FoundationBoundaryCalculator (+FoundationEffectiveBoundary) | CF/Calculators/FoundationBoundaryCalculator.cs | 80 | Domain | effective bounds after cover | N |
| Models (Snapshot, Spec, Bar, MeshResult, ValidationResult, HookType+BarLayer, Point3, Vector3, Polyline3) | CF/Models/*.cs | 644 | Domain | value types | N |

### B4. Revit API boundary
Pure logic in the add-in: FoundationSolidFaceReader.Read F/Service/FoundationSolidFaceReader.cs:89-205 (dominant-edge pick, canonical direction :132, u/v min-max projection :161-174 — vector math on edge endpoints); FoundationSession.FindBarTypeByDiameter F/Model/FoundationSession.cs:39 (nearest diameter); plane-normal-by-layer F/Service/FoundationRebarCreationService.cs:49-50; summary text F/FoundationRebarExternalEventHandler.cs:78.
Duplicated across the boundary: segment simplification runs in Core (CF/Calculators/FoundationMeshCalculator.cs:375-376 `Simplify(1.0)`) and again in the add-in (F/Service/FoundationRebarCreationService.cs:96 + extra 0.002 ft filter :109); thickness > 0 checked in FoundationRebarValidator :77 and again in FoundationSolidFaceReader.Read :98; category check in SelectionFilter :17-23, Validator :34-40 and Command :68. Two validation result types in one feature: F/Service/FoundationRebarValidator.cs:9 `FoundationValidation` vs CF/Models/FoundationValidationResult.cs.

### B5. Transactions / events / window
- TransactionGroup "Foundation Rebar" F/Service/FoundationRebarOrchestrator.cs:23; mesh computed inside the group :28; Transaction "Create Foundation Reinforcement" :39 + RebarFailureHandling.Apply :42; Assimilate :49 / RollBack :56.
- ExternalEvent in handler ctor F/FoundationRebarExternalEventHandler.cs:33, Dispose :76 from view.Closed (F/FoundationRebarCommand.cs:92-96). Static `_window` :25, Activate :32-36, Owner :90. No progress reporting (CreateRebars `onBarCreated` :22 never supplied).

### B6. Dependencies
- Command `new`s handler + orchestrator + VM + view (:87-88); VM `new`s Geometry/Setting VMs (FoundationRebarViewModel.cs:32-33). All services static.
- Ctor params > 4: FoundationSession 5 (F/Model/FoundationSession.cs:22); Core FoundationRebarSpec 14 (named, CF/Models/FoundationRebarSpec.cs:13), FoundationGeometrySnapshot 9.
- Methods > 4 params: CF FoundationMeshCalculator.BuildBarPolyline 9 incl. 3 bools (:320); FoundationRebarCreationService.CreateRebars 5 (:17).
- Boolean flags: Calculate(equalSpacing) CF/…/FoundationMeshCalculator.cs:71 never passed by production; BuildBarPolyline(isDirX, hasHooks, isHookUp).
- F/Model/FoundationSession.cs:6 imports `HPRebar.FoundationRebar.Service` (Model → Service dependency for RevitUnits).

### B7. Size
- > 300 lines: CF/Calculators/FoundationMeshCalculator.cs 380. No add-in file > 300, no VM > 250.
- Methods > 50: FoundationMeshCalculator.Calculate 246 (:68-313), FoundationSolidFaceReader.Read 117 (:89), FoundationValidationCalculator.Validate 107 (CF/…:18), FoundationRebarCommand.Execute 86 (:27), FoundationRebarCreationService.CreateRebars 73 (:17), FoundationRebarValidator.Validate 68 (:22), BuildBarPolyline 60 (:320).
- Deepest nesting: FoundationSolidFaceReader.Read :109-128 (5 levels: foreach loop › foreach edge › if Line › if longer › if horizontal); GetSolid :32-49 (4).

## C. Duplication (Column ↔ Foundation ↔ Beam ↔ Kata)
| Item | Copies |
|---|---|
| RevitUnits (MmToFt/FtToMm) | C/, F/, BeamRebar/, KataExport/ Service; KataRebar calls UnitUtils inline (KataBarSetCreator, KataRebarCreationService, KataRebarSectionFit) |
| RebarFailureHandling (SwallowWarnings) | C/ (internal), F/ (public), BeamRebar/; KataRebar/Service/KataTransactionRunner.cs |
| RevitDialogs | C/, F/, BeamRebar/, KataExport/ |
| ExternalEventHandler queue + TCS + Raise | Column, Foundation, Beam, KataExport, KataRebar (5 near-identical) |
| *SolidFaceReader (get_Geometry walk) | ColumnSolidFaceReader.GetSolids :20, FoundationSolidFaceReader.GetSolid :20, BeamSolidFaceReader |
| PointMapper, RebarShapeResolver, RebarTypeCatalog, RebarTypeInfo | Column + BeamRebar (same names, separate namespaces) |
| Core Point3 | CC/Models, CF/Models, Core/BeamRebar/Models (3 types); Polyline3 + Vector3 Beam ≡ Foundation (diff = 1 doc line / using) |
| Core Tolerance / BarPolyline / ValidationResult | CC + Core/BeamRebar; plus add-in C/Model/ValidationResult + F FoundationValidation + CF FoundationValidationResult |
| Partition parameter write | LookupParameter("Partition") C/Service/MainBarCreator.cs:119 + BeamStirrupCreator.cs:165 vs NUMBER_PARTITION_PARAM F/Service/FoundationRebarCreationService.cs:70 + KataRebarStamp.cs:22 |
| "Max 1002 bar positions" | C/ViewModel/ColumnSpecEditor.cs:60 and CF/Calculators/FoundationValidationCalculator.cs:13 |
| Short-segment simplify (1 mm) | MainBarCreator.Simplify :96 (add-in, Column) vs Polyline3.Simplify (Core, Foundation/Beam) |
| Bar-polyline pipeline (Compute → ComputeUpperPositions → Build) | RebarCreationService.cs:139-165, ViewModel/Tabs/BarsDivisionTabViewModel.cs:37-56, View/Controls/ElevationBars.cs:15-53 |
| Rect-section centre (west face → +B/2 east +H/2 north) | DetailViewCreator.cs:145-149, SectionViewCreator.cs:111-125 |
| Half plan width by acrossWidth | DetailViewCreator.PlanHalfWidth :170, DimensionCreator.ElevationLine :138-140, ElevationPainter.PlanExtent |
| Beams→horizontal faces→sort pattern | ColumnStackReader.cs:130,158,191 + ColumnStackValidator.cs:241 |
| Tie placement | AdditionalTieCreator.PlaceCrossTie :219 ≈ StirrupCreator.Place :40 (differs only in ScaleToBox height arg) |

## D. Testability
| Area | Tests | Notes |
|---|---|---|
| CC (Core Column) | 9 classes, 99 methods (+26 InlineData rows): BarPolylineBuilder 20, BarLayoutCalculator 14, StirrupDistribution 14, CanvasScale 11, Splice 11, BarSchedule 10, BarShapeClassifier 10, BarSideClassifier 5, DefaultOverlap 4 | DefaultUpperPositions only via SpliceCalculator; Tolerance, ColumnSection.BendDepth no direct tests |
| CF (Core Foundation) | 4 classes, 51 methods (+57 rows): Validation 16, Mesh 15, Boundary 13, Snapshot 7 | Point3/Vector3/Polyline3 only indirectly |
| TU (TUnit, in-Revit) | 4 classes, 21 tests (Orchestrator 5, Reader 6, Validator 5, RebarCreationService 5) — all Column | every test `Skip.Unless(ColumnStackFixture.Exists)`; TU/Fixtures holds only README.md → 0 execute (CLAUDE.md says 16 — stale) |
| Column add-in pure logic | 0 | ColumnSpecEditor.Validate, Session.ApplySelectedToAll, CutHeight, table Rows, MainBarCreator.Simplify, CrossTieCount — live in the Revit-bound add-in assembly (types carry RebarBarType/Element), so Core.Tests cannot reach them |
| Foundation add-in | 0 | no TUnit for Foundation at all; FoundationSolidFaceReader.Read orientation math + FindBarTypeByDiameter untested |

## E. Smells (facts only)
| # | Where | Category | Fact |
|---|---|---|---|
| 1 | CF/Calculators/FoundationMeshCalculator.cs:68-313 | long method / duplication | Calculate = 246 lines; 4 near-identical layer loops :150-177, :180-207, :213-240, :243-270; magic weight 0.006165 :286 |
| 2 | CF/Calculators/FoundationMeshCalculator.cs:320 | boolean flag | BuildBarPolyline 9 params incl. isDirX/hasHooks/isHookUp; branches duplicated :334-365 |
| 3 | C/Service/RebarCreationService.cs:139-165 | duplication | bar-polyline pipeline repeated in BarsDivisionTabViewModel.cs:37-56 and ElevationBars.cs:15-53 |
| 4 | C/, F/ Service RevitUnits/RebarFailureHandling/RevitDialogs | duplication | per-feature copies (4/4/4 incl. Beam/Kata); accessibility differs (internal vs public) |
| 5 | CF/Models/Point3.cs, Polyline3.cs, Vector3.cs | duplication | Point3 ×3 in Core; Polyline3/Vector3 Beam ≡ Foundation |
| 6 | C/ColumnRebarCommand.cs:62 | hidden dependency | OrderBy BottomFace → RequireSingleSolid throws for multi-solid columns before validator rule 3 (:65) can report code 3; falls into generic catch :116 |
| 7 | C/Service/ColumnStackValidator.cs:61; C/Model/AnnotationSettings.cs:63,75 | hidden dependency | compares localized AsValueString "Vertical", "Structural" and FamilyName "Linear Dimension Style" |
| 8 | C/ViewModel/ColumnRebarSession.cs:18-21 | premature abstraction | DetailViewName/SectionViewName/LevelPrefix/SectionPrefix bound in SettingTabView.xaml but never passed on; AnnotationSettings keeps its own "Detail"/"MC" (C/Model/AnnotationSettings.cs:40,43) |
| 9 | C/ViewModel/ColumnRebarViewModel.cs:16-17 vs C/Model/RebarTypeInfo.cs:12, ColumnRebarSession.Stack | Revit-boundary leak | runner doc claims VM free of Revit types; specs carry RebarBarType, session carries ColumnStack (Element/PlanarFace) |
| 10 | F/ViewModel/FoundationRebarViewModel.cs:36; F/ViewModel/IFoundationRebarRunner.cs:16 | Revit-boundary leak | VM exposes FoundationSession (Document, Floor, RebarBarType); runner contract takes it; VM mutates Session.Spec :60 cross-thread |
| 11 | C/ColumnRebarExternalEventHandler.cs:47; C/Model/ColumnStack.cs | hidden dependency | Execute ignores UIApplication; PlanarFace/Element captured at pick (:74) are used later in the modeless run, no re-read |
| 12 | C/ViewModel/ColumnSpecEditor.cs:84-177 | SRP | domain validation + English-only messages inside a 320-line VM; 1002 limit duplicated in CF (:13) |
| 13 | C/Service/AdditionalTieCreator.cs:22 | coupling | Create takes 11 params; 4 private helpers take 9-10, all forwarding the same document/faces/section/shapes/barType/cover/runs/partition set |
| 14 | C/Service/ColumnStackValidator.cs:253 | boolean flag | SitsOn(useTopFace) — all 3 callers pass true (:212,:221,:231) |
| 15 | C/Service/ColumnSolidFaceReader.cs:223-227; C/Service/ColumnNeighbourFinder.cs:85 | duplication | no caching: each face getter re-runs get_Geometry; GetLowestLevel collector per FindSupport, up to 5× per command |
| 16 | C/Model/ValidationMessages.cs:11-33; C/Service/RebarShapeResolver.cs:64-69; C/ColumnRebarCommand.cs:82 | OCP | int-coded results (1-14, 20-23) spread across 3 files; StirrupSpec.TypeDis / AdditionalTieSpec.TypeH/V / SpliceSpec.*DowelsType are raw ints; `CrossTie(spec.TypeV + 1)` AdditionalTieCreator.cs:199 |
| 17 | C/Service/ColumnRebarOrchestrator.cs:103-174 | DIP | orchestrator hard-wires 6 static creators; no seam → only in-Revit tests (which never run) |
| 18 | F/Service/FoundationRebarCreationService.cs:54-68 | coupling | `#pragma CS0618` around CreateFromCurves + RebarHookOrientation — removed in R27 (CLAUDE.md R27 break); Column uses CreateFromRebarShape/CreateFreeForm with `#if` |
| 19 | F/Service/FoundationRebarCreationService.cs:76-81, :22 | premature abstraction | barId computed and dropped; `onBarCreated` callback never supplied |
| 20 | F/Service/FoundationRebarCreationService.cs:30-31 | naming | `normX` = LocalY, `normY` = LocalX |
| 21 | F/ViewModel/FoundationSettingViewModel.cs:54; F/Model/FoundationSession.cs:39 | hidden dependency | hard-coded diameter list 10-32 mm; FindBarTypeByDiameter silently takes the nearest document type, no tolerance/warning |
| 22 | CF/Models/FoundationHookType.cs:15-21 | naming | enum aliases Hook90/Hook90Up = 1, Hook90Down = 2; Calculate only tests `== Hook90Degrees` (FoundationMeshCalculator.cs:110) so Hook90Down yields no hooks |
| 23 | CF/Models/Point3.cs:74,76 | LSP | Equals uses 1e-6 tolerance, GetHashCode exact → equal points can hash differently |
| 24 | CF/Models/FoundationGeometrySnapshot.cs:65-73; FoundationRebarSpec.cs:82,91-100; FoundationBoundaryCalculator.cs:50,60 | premature abstraction | alias properties, EnableTopMat, per-layer hook overrides, ComputeEffectiveBoundary/ValidateBoundary used only by tests; ValidateBoundary rules re-implemented in FoundationValidationCalculator.cs:56-62 |
| 25 | C/ (dead members) | premature abstraction | ColumnStack.Summary :41, ColumnFaces.TopLevel/BottomLevel/Cylindricals (write-only), DimensionCreator.ElevationFaces :157, RevitDialogs.Warning/Confirm, RevitUnits.Display :21, handler LastResult (both features), Top/BottomDowelsTabViewModel.DowelStyles :23 (not bound; dowel style edited as raw int column) |
| 26 | C/ColumnRebarCommand.cs:87 + C/Service/ColumnRebarOrchestrator.cs:54; F/ViewModel/FoundationRebarViewModel.cs:49 + CF/…/FoundationMeshCalculator.cs:77 | duplication | same pre-flight check run twice per path |
| 27 | CC/Models/AdditionalTieSpec.cs:8-9 | comment noise | doc says type 0 places legs "by spacing AH"; add-in treats AH as closed-tie leg length (AdditionalTieCreator.cs:76) |
| 28 | C/Model/AnnotationSettings.cs:49-87 | SRP | Model class with public setters + static Load running 3 FilteredElementCollectors |
| 29 | C/Service/DetailViewCreator.cs:53 | naming | ResolveViewType creates (Duplicate) a ViewFamilyType when missing — mutation behind a "Resolve" name, shared by SectionViewCreator :27 |
| 30 | file organisation | naming | IColumnRebarRunner inside ColumnRebarViewModel.cs:19 vs IFoundationRebarRunner own file; 2+ types per file in StirrupGeometry.cs, FoundationRebarValidator.cs, FoundationBoundaryCalculator.cs, FoundationHookType.cs; Core layout CC/*.cs + Models/ vs CF/Calculators/ + Models/; LocalizationService (ObservableObject) in Service/ |

# Map 04 — KataRebar + KataExport (HPRebar add-in + Core)

Read-only map, 2026-10-03, working tree clean for these paths. No build/test run. Counts = grep/regex scan (method length = signature→closing brace; params exclude constructors unless stated), not a runner.
Abbrev: `KR/` = HPRebar/HPRebar/KataRebar/, `KE/` = HPRebar/HPRebar/KataExport/, `CKR/` = HPRebar/HPRebar.Core/KataRebar/, `CKE/` = HPRebar/HPRebar.Core/KataExport/, `T/` = HPRebar/HPRebar.Core.Tests/, `BR/` = HPRebar/HPRebar/BeamRebar/.

| Area | .cs files | LOC | Notes |
|---|---|---|---|
| KR/ (add-in) | 33 (+1 xaml 335) | 2 304 | 4 root + Model 6 + Service 19 + View 1 + ViewModel 3 |
| KE/ (add-in) | 50 (+2 xaml 818/158) | 5 284 | View/Controls alone 20 files / 2 809 LOC (canvas) |
| CKR/ (Core) | 68 | 7 433 | Calculators 42 / 5 063, Models 20 / 1 228, Parsers 6 / 1 142 |
| CKE/ (Core) | 14 | 1 145 | segmenter, row builder, elevation model, viewport |
| T/KataRebar + T/KataExport | 39 + 9 | 6 865 + 1 297 | 286 + 85 `[Fact]/[Theory]`, 209 + 38 `[InlineData]` |

Ribbon: full build = Rebar panel buttons "Kata Export" + "Kata Rebar"; `KATA_ONLY` build = Kata panel with "Kata Export" only (HPRebar/HPRebar/Application.cs:59-69). Kata Export's window carries the whole rebar round trip; Kata Rebar is a second window over the same workflow.

## 1. Behaviour

**KataExport** — user picks a straight run of structural framing (pre-selection or PickObjects). Revit side measures it once (axis frame, pieces, supports by probe lines through solids, grids, slab, first-beam parameters) into a Revit-free `KataExportSession`. The window builds Kata sheet "Dam" (B3:B10 + rows 11/19/21/22/23 from column C) in Core, previews it as a table + zoomable elevation canvas, and writes it by late-bound COM into the workbook active in the running Excel (nothing saved). Second half ("way back"): reads sheet Dam back, plans bars on the re-measured beams (preview drawn on canvas as Kata's CAD elevation + section panel), edits office settings (modal dialog, `%AppData%\HPRebar\KataSettings.json`), and generates rebar in Revit.

**KataRebar** — modeless window: reads sheet Dam of the active Excel workbook (COM, A1:BZ44), parses it, measures pre-selected/picked beams the way Kata Export does, previews the plan (span/bar/stirrup tables, blocking/warning/skipped lists, bar-type mapping per diameter), and draws: delete previous Kata-tagged bars, stirrup sets, longitudinal bars (fixed-number sets, single fallback), C ties / inner stirrups — one undo "Kata Rebar - {beam}".

## 2. Flows

**KataExport export**
`KataExportCommand.Execute` (KE/KataExportCommand.cs:29) → filter pre-selection with `KataExportSelectionFilter`, else `Selection.PickObjects` (:59) → `KataSessionReader.Read` (KE/Service/KataSessionReader.cs:15) = `KataRunReader.Read` (axis frame `KataAxisFrame`, `KataSolidReader`) + `KataSupportCollector.Collect` (`KataCandidateCollector.Near`, probe intervals, Core `KataSupportRules`) + `KataGridReader.Read` + `KataHeaderReader` → `new KataExportExternalEventHandler(doc, view)` + `new KataExportViewModel(session, handler, new KataRebarTypeResolver(doc))` → `KataExportView.Show()` (modeless, static `_window`).
VM ctor → `LoadSession` → `RebuildSheet` (KE/ViewModel/KataExportViewModel.cs:115): `session.ToInput` → Core `KataRowBuilder.Build` (→ `KataSegmenter`) → `KataElevationBuilder.Build` → `KataPreviewBuilder.Columns`; `ProbeWorkbook` → `KataExcelWriter.Probe` (COM). Export button → `KataExcelWriter.Write(sheet, expectedWorkbook)` on the UI thread (:202; no Revit API, no transaction) → `runner.HighlightAsync` (ExternalEvent → `Selection.SetElementIds`).

**KataExport rebar round trip** (KE/ViewModel/KataExportViewModel.Rebar.cs)
`LoadRebarAsync` → `KataDamComReader.Read` (COM, lives in KR/) → Core `KataDamSheetParser.Parse(KataCellTable)` → `runner.PreviewRebarAsync` → handler `PreviewRebar` → `KataRebarWorkflow.Prepare` (KR/Service) = `KataBeamMatcher.Measure` (reuses KE readers + Core `KataSegmenter`) + Core `KataRebarPlanner.Plan` → `KataRebarPreview` (Revit-free) → `ApplyPreview` → canvas `KataRebarDrawing.For(plan)` → Core `KataBarTagBuilder`, `KataSectionCuts`, `KataElevationDrawingBuilder`. `GenerateRebarAsync` → handler → `KataRebarWorkflow.Generate` → `KataRebarOrchestrator.Execute`. `OpenSettingsAsync` → `new KataSettingsView(new KataSettingsViewModel(...)).ShowDialog()` → `KataSettingsStore.Save` → re-preview.

**KataRebar**
`KataRebarCommand.Execute` (KR/KataRebarCommand.cs:26) → filter pre-selection (no pick prompt) → `new KataRebarExternalEventHandler(doc)` → `new KataRebarViewModel(doc, ids, handler)`; ctor (KR/ViewModel/KataRebarViewModel.cs:55-63) builds `KataRebarTypeResolver(doc)` (FilteredElementCollector), `LoadSheet()` (COM read + parse + `Replan` with `KataSettingsStore.Load()`), fire-and-forget `MeasureAsync` → `View.Show()`. Commands: Refresh (COM), Repick (`Pick` request: PickObjects + `KataBeamMatcher.Measure`), Generate (`Generate` request → `KataRebarWorkflow.Generate(doc, ActiveView, ids, spec, KataSettingsStore.Load(), barTypeIds)`).

**Generation (shared)** — KR/Service/KataRebarOrchestrator.cs:19-90
`KataRebarWorkflow.Generate` re-measures + re-plans, maps diameter → `RebarBarType`, finds closed stirrup shape (`KataRebarShapeResolver`) → `TransactionGroup "Kata Rebar - {name}"` → `KataTransactionRunner.Run` ×4: `KataRebarCleanupService.DeletePrevious` → `KataStirrupSetCreator.Create` (SubTransaction per zone: `Rebar.CreateFromRebarShape` + `ScaleToBox` + `KataStirrupCoverFit`; fallback singles via `KataRebarCurveFactory`) → `KataRebarCreationService.CreateLongitudinalBars` (Core `KataLongitudinalSetGrouping`; SubTransaction per set: `SetLayoutAsFixedNumber` + `FitEnd` constraint correction + `KataRebarSectionFit`; fallback singles) → `KataBarSetCreator.Create` (SubTransaction per set: `KataRebarHookResolver`, Core `KataTieWrap`, `CreateHooked`) → `group.Assimilate()` → `KataRebarGenerationResult` (message string built in orchestrator). Every bar stamped by `KataRebarStamp` (comment tag = re-run delete key, partition, schedule mark). Placement frame = `KataBeamPlacement` (wraps `BR/Service/PointMapper`).

## 3. Class inventory (add-in; Core grouped)

| Class | File | LOC | Layer | Responsibility | Revit API |
|---|---|---|---|---|---|
| KataRebarCommand | KR/KataRebarCommand.cs | 74 | Entry | filter selection, open singleton window | Y |
| KataRebarExternalEventHandler (+IKataRebarRunner) | KR/KataRebarExternalEventHandler.cs | 110 | Entry/Revit-integration | queue Pick/Measure/Generate on API thread | Y |
| KataRebarRequest (+enum) | KR/KataRebarRequest.cs | 33 | Entry | request + TCS | Y (ElementId) |
| KataRebarSelectionFilter | KR/KataRebarSelectionFilter.cs | 17 | Entry | framing-only pick | Y |
| KataBeamMatchResult | KR/Model/KataBeamMatchResult.cs | 33 | Model (holds Revit objs) | measured run + KE `KataRunGeometry` | Y |
| KataRebarGenerationResult / Preparation / PreviewItems / BarTypeMappingItem | KR/Model/*.cs | 39/23/22/22 | Model/Presentation | results, preview rows, ObservableObject row | N |
| RebarTypeOption | KR/Model/RebarTypeOption.cs | 18 | Model | bar type row (ElementId) | Y |
| KataRebarWorkflow | KR/Service/KataRebarWorkflow.cs | 80 | Application | measure → plan → map types → orchestrate | Y |
| KataRebarOrchestrator | KR/Service/KataRebarOrchestrator.cs | 91 | Application/Revit | TransactionGroup, steps, result message | Y |
| KataTransactionRunner (+FailureCollector) | KR/Service/KataTransactionRunner.cs | 86 | Revit-integration | one Transaction per step, warnings/errors | Y |
| KataBeamMatcher | KR/Service/KataBeamMatcher.cs | 88 | Revit-integration | measure run via KE readers + Core segmenter | Y |
| KataBeamPlacement | KR/Service/KataBeamPlacement.cs | 48 | Revit-integration | local frame, host piece at station | Y |
| KataRebarCreationService | KR/Service/KataRebarCreationService.cs | 201 | Revit-integration | longitudinal bars, set + fallback, constraint fit | Y |
| KataStirrupSetCreator | KR/Service/KataStirrupSetCreator.cs | 176 | Revit-integration | shape-driven stirrup sets + fallback | Y |
| KataBarSetCreator | KR/Service/KataBarSetCreator.cs | 132 | Revit-integration | C ties / inner stirrups with hooks | Y |
| KataRebarCurveFactory | KR/Service/KataRebarCurveFactory.cs | 87 | Revit-integration | CreateFromCurves (multi-version), polyline→curves | Y |
| KataRebarSectionFit / KataStirrupCoverFit | KR/Service/*.cs | 75/37 | Revit-integration | move bar back / cover constraint fix | Y |
| KataRebarHookResolver / ShapeResolver / TypeResolver | KR/Service/*.cs | 38/41/94 | Revit-integration | project hook / shape / bar-type lookup + mapping rows | Y |
| KataRebarStamp / CleanupService | KR/Service/*.cs | 33/36 | Revit-integration | tag params / delete previous tagged bars | Y |
| KataRebarLog | KR/Service/KataRebarLog.cs | 56 | Infrastructure (log) | Serilog block per beam | Y (RebarShape) |
| KataSettingsStore | KR/Service/KataSettingsStore.cs | 55 | Infrastructure (file) | static cached JSON settings | N |
| KataDamComReader (+KataDamReadResult) | KR/Service/KataDamComReader.cs | 96 | Excel-COM | read Dam A1:BZ44 in one Value2 call | N |
| KataRebarViewModel | KR/ViewModel/KataRebarViewModel.cs | 217 | Presentation | window state, COM read, replan, commands | Y (Document ctor) |
| KataRebarPreviewBuilder | KR/ViewModel/KataRebarPreviewBuilder.cs | 99 | Presentation (pure) | plan → rows/messages | N |
| KataRebarView | KR/View/KataRebarView.xaml(.cs) | 335/25 | Presentation | window | N |
| KataExportCommand | KE/KataExportCommand.cs | 107 | Entry | select/pick, read session, open window | Y |
| KataExportExternalEventHandler (+IKataExportRunner) | KE/KataExportExternalEventHandler.cs | 167 | Entry/Revit-integration | Highlight/Repick/PreviewRebar/GenerateRebar | Y |
| KataExportRequest / SelectionFilter | KE/*.cs | 41/18 | Entry | request + TCS / framing filter | Y |
| KataBeamGeometry + KataRunGeometry | KE/Model/KataBeamGeometry.cs | 44 | Model (Revit objs) | piece/run geometry (FamilyInstance, Level, KataAxisFrame) | Y |
| KataExportSession | KE/Model/KataExportSession.cs | 67 | Model | Revit snapshot → Core `KataRunInput` | Y (ElementId) |
| KataRebarPreview | KE/Model/KataRebarPreview.cs | 24 | Model | plan handed to UI | N |
| KataSessionReader | KE/Service/KataSessionReader.cs | 44 | Application/Revit | compose readers into session | Y |
| KataRunReader / KataAxisFrame / KataSolidReader | KE/Service/*.cs | 135/70/126 | Revit-integration | straight-run check, frame math, solids/probes | Y |
| KataSupportCollector (+Scan) / KataCandidateCollector | KE/Service/*.cs | 267/73 | Revit-integration | supports by probe lines / phase+option filter | Y |
| KataGridReader / KataHeaderReader | KE/Service/*.cs | 73/78 | Revit-integration | grids crossing + axis / slab + params | Y |
| RevitUnits / RevitDialogs | KE/Service/*.cs | 16/18 | Revit-integration | mm↔ft / TaskDialog (4th copy each) | Y |
| ComLateBinding / ExcelComAttach | KE/Service/*.cs | 67/47 | Excel-COM | InvokeMember wrapper / ole32+oleaut32 GetActiveObject | N |
| KataExcelWriter (+Target, 2 records) | KE/Service/KataExcelWriter.cs | 207 | Excel-COM | probe + write Dam, clear stale columns | N |
| KataExportViewModel (3 partials) | KE/ViewModel/KataExportViewModel*.cs | 272+34+230 = 536 | Presentation | sheet, Excel, navigation, rebar round trip, settings dialog | N (ElementId via session) |
| KataSettingsViewModel | KE/ViewModel/KataSettingsViewModel.cs | 123 | Presentation | 16 settings fields, validation, save | N |
| KataPreviewBuilder / PreviewColumn / ViewFocus | KE/ViewModel/*.cs | 45/4/7 | Presentation (pure) | sheet → table/labels | N |
| KataExportView / KataSettingsView | KE/View/*.xaml(.cs) | 818+23 / 158+20 | Presentation | windows | N |
| KataElevationCanvas (4 partials) | KE/View/Controls/KataElevationCanvas*.cs | 287+94+140+57 = 578 | Presentation (canvas) | FrameworkElement.OnRender, DPs, zoom/pan, section panel | N |
| Plain pipeline: KataElevationPainter / Annotations / SectionPainter / Scene / LabelLane | KE/View/Controls/*.cs | 268/389/231/108/38 | Presentation (canvas) | Revit-read elevation, dims, section card | N |
| Kata-CAD pipeline: KataElevationCadPainter / BarTagPainter / SectionCadPainter / CadDimPainter / CadTag / CadText / CadPens | KE/View/Controls/*.cs | 185/68/163/130/96/72/29 | Presentation (canvas) | Kata drawing in model mm | N |
| KataCanvasPalette / KataDrawPrimitives / KataRebarDrawing / ListBoxScrollSelection | KE/View/Controls/*.cs | 218/149/57/30 | Presentation | brushes, primitives, per-plan drawing cache, attached prop | N |
| Core parsers (KataDamSheetParser, KataBarNotationParser, KataCellTable, IKataDamCellAccessor, KataSettingsJson, KataStirrupSectionParser) | CKR/Parsers | 1 142 | Domain(Core) | sheet Dam → `KataBeamRebarSpec`; settings JSON | N |
| Core planning/layout (Planner, Calculator, DetailingRuleBuilder, ScopeFilter, SheetGeometryCheck, 15 *Layout/*Runs/*Positions classes, Numbering, Anchorage…) | CKR/Calculators | ≈3 000 | Domain(Core) | plan + bar layout in local mm | N |
| Core drawing (ElevationDrawingBuilder, SectionDrawingBuilder, Outline, Dims, SectionTags/Lines/Bars/Style, DrawingStyle/Frame/Levels, BarTagBuilder, TagStyle, BarDrafting, Bulge, SectionCuts) | CKR/Calculators + Models | ≈1 900 | Domain(Core) | Kata CAD drawing model | N |
| Core KataExport (Segmenter, RowBuilder, ElevationBuilder, ElevationViewport, InputValidator, SupportRules, ExcelCell, ColumnLetters, Format) | CKE/ | 1 145 | Domain(Core) | run → segments → sheet + preview elevation | N |

## 4. Boundaries

- **Core is clean**: CKR/ and CKE/ never reference each other or Revit/COM; CKR/ reuses `Point3`/`Polyline3` from `HPRebar.Core.BeamRebar.Models` (14 files). Cell reading goes through `IKataDamCellAccessor` (CKR/Parsers/IKataDamCellAccessor.cs:8) → parser fully testable.
- **Revit API**: KE/Service (9 reader files) + KR/Service (16 files) + both handlers/commands. Leaks above that: `KataRebarViewModel` takes `Document` and builds `KataRebarTypeResolver` (KR/ViewModel/KataRebarViewModel.cs:55-58); models carry Revit objects (`KataBeamGeometry` FamilyInstance/Level, KE/Model/KataBeamGeometry.cs:11,29; `KataBeamMatchResult.Run`, KR/Model/KataBeamMatchResult.cs:25); `KataRunGeometry.Frame` is a Service class (Model→Service, KE/Model/KataBeamGeometry.cs:4,35).
- **Excel COM**: 4 files, all static, no interface: `ExcelComAttach` (CLSIDFromProgID + oleaut32 `GetActiveObject`, KE/Service/ExcelComAttach.cs:14-18), `ComLateBinding` (`Type.InvokeMember`, unwraps TargetInvocationException, KE/Service/ComLateBinding.cs:40-51), `KataExcelWriter` (KE/), `KataDamComReader` (KR/). Every COM object released in `finally`. Called directly from VMs on the UI thread (KE/ViewModel/KataExportViewModel.cs:179,202; KataExportViewModel.Rebar.cs:50; KR/ViewModel/KataRebarViewModel.cs:111) → VMs not unit-testable.
- **Cross-feature coupling (add-in)**: bidirectional KE↔KR — KE uses `KataRebar.Service/Model` (KE/KataExportCommand.cs:11, KE/KataExportExternalEventHandler.cs:13-14, KE/ViewModel/*), KR uses `KataExport.Service/Model` (KR/Model/KataBeamMatchResult.cs:6, KR/Service/KataBeamMatcher.cs:8, KataDamComReader.cs:4, KataRebarTypeResolver.cs:7, KataStirrupSetCreator.cs:8). KR also uses `BeamRebar.Service` (`PointMapper` KR/Service/KataBeamPlacement.cs:4, KataRebarCurveFactory.cs:5, KataRebarSectionFit.cs:5; `RevitDialogs` KR/KataRebarCommand.cs:6).
- **Pure logic sitting in the add-in** (no Revit call, could be Core): KR/ViewModel/KataRebarPreviewBuilder.cs:15-98; KE/ViewModel/KataPreviewBuilder.cs; `KataRebarSectionFit.LongestLevelSegment` (KR/Service/KataRebarSectionFit.cs:47-62); `KataRebarWorkflow.Diameters` (:74-79); `KataRebarStamp.Mark` (:15); `KataBeamMatcher.ToMeasured/SpanDepth/Describe` (:63-87); name/diameter matching in `KataRebarTypeResolver.Resolve` (:40-58); result counts + message (KataRebarOrchestrator.cs:48-69); nearest-piece pick (KataBeamPlacement.cs:40-47); `KataDamComReader.WithoutErrorCells` (:84-93); settings range validation (KE/ViewModel/KataSettingsViewModel.cs:57-71, dup of CKR/Parsers/KataSettingsJson.cs:76-103); section footer/count derivation in the canvas (KE/View/Controls/KataElevationSectionPainter.cs:81-112).

## 5. Transactions / ExternalEvent / modeless

- Both commands follow the convention: static `_window`, `Activate()` on 2nd click, Owner via `WindowInteropHelper`, `Closed` → `handler.Dispose()` + null (KR/KataRebarCommand.cs:24-66, KE/KataExportCommand.cs:27-99). Root folder = exactly 4 files in both.
- Handlers own `ExternalEvent`, `ConcurrentQueue<Request>` + `TaskCompletionSource(RunContinuationsAsynchronously)`, drain loop, document-identity check per request. KE checks `Raise()` result and fails queued requests when no UIDocument (KE/KataExportExternalEventHandler.cs:63-84); KR ignores `Raise()` result (KR/KataRebarExternalEventHandler.cs:79-84).
- Transactions only inside `KataRebarOrchestrator` (API thread): TransactionGroup → 4 `Transaction`s via `KataTransactionRunner` (failure preprocessor deletes warnings, errors → ProceedWithRollBack → throw) → SubTransactions per set/zone in 3 creators for fallback → `Assimilate()` = one undo. Catch filter rolls back group (KataRebarOrchestrator.cs:83-89).
- No `TaskDialog` result from `Execute` (convention says so); results go to the VM status line. Errors at open use `RevitDialogs` from the command.
- Excel COM runs synchronously on Revit's UI thread from VM commands (legal, not Revit API); busy HRESULTs mapped to a message, no retry.
- KE settings dialog: VM instantiates `View.KataSettingsView` and finds its own owner by scanning `PresentationSource.CurrentSources` (KE/ViewModel/KataExportViewModel.Rebar.cs:198-222), then `ShowDialog()`.

## 6. Dependencies

- Construction by `new` in commands only (handler, VM, view, `KataRebarTypeResolver`); everything else static (30 `public static class` in add-in Kata, 48 in Core Kata). No DI.
- Static/hidden deps: `KataSettingsStore` static `_cached` (KR/Service/KataSettingsStore.cs:19) read inside handler (KR/KataRebarExternalEventHandler.cs:109), VMs (KataRebarViewModel.cs:176; KataExportViewModel.Rebar.cs:80,183,202) and settings VM (:93); static COM entry points from VMs; `KataRebarViewModel` ctor does COM I/O + un-awaited `_ = MeasureAsync` (:61-62); `KataExportViewModel` ctor does COM probe (:76); optional ctor dep `KataRebarTypeResolver? typeResolver = null` (:70).
- Constructors > 4 params: `KataCadDimPainter` 9 (KE/View/Controls/KataCadDimPainter.cs:31), `KataElevationScene` 7 (:26), `KataElevationCadPainter` 6 (:45), `KataElevationBarTagPainter` 5, `KataElevationSectionPainter` 5, Core `KataSectionBars` 5.
- Methods > 4 params: ≈29 add-in, ≈51 Core. Worst: CKR/Calculators/KataSupportTopBarLayout.cs:205 `Add` 16 (incl. `ref int barId`, `List<string> warnings`, `List<string> blocking`), KataTopBarStagger.cs:64 `Side` 12, KataSideBarLayout.cs:149 `Bar` 10, KataSpanBottomBarLayout.cs:134/182 10/10; add-in KR/Service/KataRebarCurveFactory.cs:35/39 `CreateHooked` 8/9, KataStirrupSetCreator.cs:49 `TryCreateSet` 8, KataRebarWorkflow.cs:36 `Generate` 7, KE/Service/KataCandidateCollector.cs:15 `Near` 7 (positional `1.0, 2.5` at KataSupportCollector.cs:54).
- Accumulator threading: `ref barId` + shared `warnings`/`blocking` lists passed through every layout (CKR/Calculators/KataRebarCalculator.cs:73-89).
- Boolean flags: `KataRebarSectionFit.Fit(acrossToo)` (KR/Service/KataRebarSectionFit.cs:25), `KataStirrupSetCreator.Layout(barsOnNormalSide)` (:102), `KataRebarTypeResolver.Resolve(isLongitudinal)` (:40), `PreviewRebarAsync(preferReversed)` (KE/ViewModel/IKataExportRunner.cs:20), Core `KataBarTagBuilder.AtCut` 2 bools (:130), `KataSectionDrawingBuilder.Build(mirror)` (:18), `KataBarNotationParser.ParseBarList(bool)` (:48); 15 more in canvas primitives.

## 7. Size metrics

- Files > 300 LOC (prod): KE/View/Controls/KataElevationAnnotations.cs 389; CKR/Parsers/KataDamSheetParser.cs 307. Near: KataBarNotationParser 296, KataElevationCanvas.cs 287, KataSupportTopBarLayout 282, KataExportViewModel.cs 272. Partial classes over the limit as a whole: KataElevationCanvas 578, KataExportViewModel 536. XAML > 500: KE/View/KataExportView.xaml 818. Tests > 300: KataRebarCalculatorTests 863, KataStressAdversarialTests 810, KataSectionDrawingGolden 435, KataElevationTests 335, KataSupportTopBarTests 317.
- ViewModels > 250: KataExportViewModel 536 (3 partials). KataRebarViewModel 217.
- Methods > 50 lines — add-in 12: KataElevationPainter.PaintMonolithicFrame 165 (KE/View/Controls/KataElevationPainter.cs:55), KataElevationAnnotations.PaintTopChain 133 (:44) / PaintDetailChain 73 (:254), KataCanvasPalette.From 99 (:75), KataBarSetCreator.Create 83 (KR/Service:24), KataExportCommand.Execute 78 (:29), KataExportExternalEventHandler.Execute 73 (:76), KataRebarOrchestrator.Execute 72 (:19), KataExportViewModel.RebuildSheet 61 (:115), KataElevationSectionPainter.PaintCard 54 (:60), KataStirrupSetCreator.TryCreateSet 51 (:49), KataDamComReader.Read 51 (:28). Core 19: KataDamSheetParser.Parse 162 (:22) / ParseSpan 57, KataSupportTopBarLayout.Build 118 (:20), KataLayerPositions.PartitionInterleaved 98 (:72), KataDetailingRuleBuilder.Build 90, KataStirrupCurveFactory.Create 85, KataSpanBottomBarLayout.Build 81, KataBottomMainBarRuns.Plan 80, KataStirrupZoneLayout.Build 78 / ThreeZones 75, KataInnerStirrupLayout.Build 77, CKE KataElevationBuilder.Build 71, KataSideBarLayout.Build 66, KataBarNumbering.Apply 58, KataElevationDims.Build 57, KataSectionDrawingBuilder.Build 53, KataSettingsJson.ParseObject 53, KataElevationOutline.Lines 52, KataRebarCalculator.Calculate 51.
- Canvas/drawing: add-in WPF `DrawingContext` code 20 files / 2 809 LOC (one `FrameworkElement.OnRender`, KE/View/Controls/KataElevationCanvas.cs:151); two parallel pipelines chosen at render time (KataElevationCanvas.cs:169-191): "plain" 5 files ≈1 030 LOC vs "Kata CAD" 7 files ≈ 740 LOC. Core drawing model ≈1 900 LOC (CKR) + ≈420 (CKE elevation/viewport). Drawing total ≈5 100 of ≈16 200 C# LOC.

## 8. Duplication

**KataRebar ↔ KataExport (add-in)**
- Two windows over one workflow: both runners expose Generate → `KataRebarWorkflow.Generate`; KR passes no `preferReversed` and loads settings in the handler (KR/KataRebarExternalEventHandler.cs:109) vs KE passes both from the VM (KE/KataExportExternalEventHandler.cs:136-139).
- Message prefixes `[Chặn]/[Cảnh báo]/[Bỏ qua]` + assembly: KR/ViewModel/KataRebarPreviewBuilder.cs:11-13,69-78 vs KE/ViewModel/KataExportViewModel.Rebar.cs:24-26,110-115. Unmatched-bar-type guard: KR/ViewModel/KataRebarViewModel.cs:90-97 vs KataExportViewModel.Rebar.cs:171-178.
- Run read pipeline: KR/Service/KataBeamMatcher.cs:27-32 vs KE/Service/KataSessionReader.cs:17-30 (same `KataRunReader` + `KataSupportCollector` + identical `KataBeamPiece` projection).
- Excel attach + busy handling: KR/Service/KataDamComReader.cs:24-26,33-52,95 vs KE/Service/KataExcelWriter.cs:27-30,87,154-197 (same HRESULT consts, same ActiveWorkbook→Worksheets→Item("Dam") sequence, same messages). Cell-contract consts repeated: CKR/Parsers/KataDamSheetParser.cs:14-15, KE/Service/KataExcelWriter.cs:22-25, KataDamComReader.cs:21-22, CKE/Models/KataSegmentModels.cs:40.
- Selection filters identical: KR/KataRebarSelectionFilter.cs:11-14 vs KE/KataExportSelectionFilter.cs:11-15 (and KE/Service/KataRunReader.cs:63-66). Commands share the same window-singleton block.
- Nearest-piece lookup: KR/Service/KataBeamPlacement.cs:40-47 vs KE/Service/KataSupportCollector.cs:262-265.
- Multi-version `SetPreferredConstraint` block: KR/Service/KataRebarCreationService.cs:127-132 vs KataStirrupCoverFit.cs:25-30.

**Kata ↔ BeamRebar**
| Concern | Kata | BeamRebar |
|---|---|---|
| Spec model | CKR/Models KataBeamRebarSpec, KataSpan/Support/StirrupSpec | Core BeamRebar Models BeamSpan, Beam*Spec + BR/Model/BeamRebarSpec |
| Stirrup zones | CKR KataStirrupZoneLayout + KataStirrupZoneResult | Core BeamStirrupDistributionCalculator + StirrupZone/StirrupRun |
| Side / additional bars | KataSideBarLayout, KataSupportTopBarLayout, KataSpanBottomBarLayout | BeamSideBarCalculator, BeamAdditionalBarCalculator |
| Shape resolver | KR/Service/KataRebarShapeResolver.cs:16-27 | BR/Service/RebarShapeResolver.cs:16,24-29 (same name→shape dictionary, overlapping name list) |
| Bar type / hook | KataRebarTypeResolver, KataRebarHookResolver | BR/Service/RebarTypeCatalog.cs:60-82 |
| Failure handling | KR/Service/KataTransactionRunner.cs:57-85 | BR/Service/RebarFailureHandling.cs:10-35 |
| Creators | KataRebarCreationService / StirrupSetCreator / BarSetCreator | BeamMainBar/Stirrup/SideBar/AdditionalBar Creator |
| Units / dialogs | KE/Service/RevitUnits.cs, RevitDialogs.cs | same files in BR/, ColumnRebar/, FoundationRebar/ (4 copies) |
| Shared for real | `Point3`/`Polyline3` (Core.BeamRebar.Models), `PointMapper` (BR/Service) | — |
Only Kata has the version-gated `BarTerminationsData` creation path (KR/Service/KataRebarCurveFactory.cs:19-27); BeamRebar does not share it.

## 9. Testability

- Core: 371 test methods (+247 InlineData rows) in 48 files / 8 162 LOC; every Core Kata file name is referenced from ≥ 1 test file (parser 26 files, calculator 17, planner 14). Golden drawings for DY7/DY14 (T/KataRebar/KataDy7DrawingTests.cs, KataDy14DrawingTests.cs, KataSectionDrawingGolden.cs).
- Add-in Kata: **0 tests** (no TUnit test touches KR/ or KE/; HPRebar.Tests has no Kata fixture). Untested logic with no Revit dependency: the pure items in §4, `KataSettingsViewModel.Accept`, `KataExportViewModel.RebuildSheet` state machine, canvas viewport/hit-testing (`FlagAt`, `SectionPanel`), `KataCanvasPalette` luminance theme switch, `KataExcelWriter` array/clear-block logic, `KataSupportCollector.Finish` classification, `KataGridReader.Station` intersection (KE/Service/KataGridReader.cs:62-72).
- VMs not testable without Excel/Revit: static COM + static settings store + `Document` in ctor (KataRebarViewModel), View construction inside VM (KataExportViewModel.Rebar.cs:209).

## 10. Smells (facts only)

| # | Where | Category | Fact |
|---|---|---|---|
| 1 | KE/KataExportCommand.cs:11, KR/Model/KataBeamMatchResult.cs:6 | coupling | KataExport and KataRebar add-in folders depend on each other in both directions (Service + Model) |
| 2 | KR/Service/KataBeamPlacement.cs:4, KataRebarCurveFactory.cs:5, KR/KataRebarCommand.cs:6 | coupling | KataRebar uses BeamRebar's `Service` namespace (`PointMapper`, `RevitDialogs`) |
| 3 | KE/Model/KataBeamGeometry.cs:4,35 | coupling | Model type holds `KataAxisFrame` from Service (Model → Service) |
| 4 | KE/ViewModel/KataExportViewModel.Rebar.cs:198-222 | DIP / Revit-boundary leak | ViewModel constructs a WPF `Window` and locates its owner via `PresentationSource` |
| 5 | KE/ViewModel/KataExportViewModel.cs:179,202; KR/ViewModel/KataRebarViewModel.cs:111 | DIP / hidden dependency | VMs call static Excel COM classes directly; no abstraction |
| 6 | KR/ViewModel/KataRebarViewModel.cs:55-62 | hidden dependency / Revit-boundary leak | VM ctor takes `Document`, runs a collector, does COM I/O and fires an un-awaited task |
| 7 | KR/Service/KataSettingsStore.cs:19 | static state | process-wide cached settings read from handler and 3 VMs |
| 8 | KE/ViewModel/KataExportViewModel*.cs | SRP / size | one VM (536 LOC, 3 partials) owns sheet build, Excel probe/write, navigation, rebar load/preview/generate, settings dialog |
| 9 | KR/KataRebarExternalEventHandler.cs:79-84 | defect-prone duplication | `Raise()` result ignored; KE handler checks it (KE/KataExportExternalEventHandler.cs:66-68) |
| 10 | KR/Service/KataDamComReader.cs:24-52 vs KE/Service/KataExcelWriter.cs:27-30,154-197 | duplication | same COM attach sequence, HRESULT consts, messages |
| 11 | KR/Service/KataBeamMatcher.cs:27-32 vs KE/Service/KataSessionReader.cs:17-30 | duplication | same run-read + support-scan + piece projection |
| 12 | KR/ViewModel/KataRebarPreviewBuilder.cs:11-13,69-78 vs KE/ViewModel/KataExportViewModel.Rebar.cs:24-26,110-115,171-178 | duplication | message prefixes, message assembly, unmatched-type check repeated |
| 13 | KR/Service/KataRebarShapeResolver.cs:16-27, KataTransactionRunner.cs:57-85, KataRebarTypeResolver.cs:40-58 | duplication | parallel versions of BR/Service RebarShapeResolver, RebarFailureHandling, RebarTypeCatalog |
| 14 | KE/Service/RevitUnits.cs, RevitDialogs.cs | duplication | 4th copy across features; KR imports both KE's (`RevitUnits`, KataStirrupSetCreator.cs:8) and BR's (`RevitDialogs`) |
| 15 | KR/Service/KataRebarCreationService.cs:96,124,156; KataRebarSectionFit.cs:36; KataBarSetCreator.cs:22 | magic numbers | literal / local `304.8` beside an existing `RevitUnits.MmToFt` |
| 16 | KE/ViewModel/KataSettingsViewModel.cs:51-71,73-91,103-122 | duplication | 16 settings fields mapped by hand twice; range rules duplicate CKR/Parsers/KataSettingsJson.cs:76-103 |
| 17 | KE/View/Controls/KataElevationCanvas.cs:169-191 | duplication / OCP | two complete render pipelines (plain ≈1 030 LOC, Kata-CAD ≈740 LOC), each with its own section and dimension painter |
| 18 | KE/View/Controls/KataElevationPainter.cs:55, KataElevationAnnotations.cs:44 | long method | 165- and 133-line paint methods |
| 19 | KE/View/Controls/KataCanvasPalette.cs:75-173 | magic numbers / theming | 37 hard-coded RGB colours chosen by computed luminance instead of theme tokens |
| 20 | KE/View/Controls/KataElevationCadPainter.cs:154-167; KataElevationSectionPainter.cs:71-112 | magic numbers | inline CAD block coordinates (167.4, 40.3, 286.7…) and card pixel offsets (56, 62, 70…); section painter also derives counts/labels (domain in view) |
| 21 | CKR/Calculators/KataSupportTopBarLayout.cs:205; KataRebarCalculator.cs:73-89 | long parameter list / hidden coupling | 16-param `Add`; `ref barId` + shared mutable `warnings`/`blocking` lists threaded through every layout |
| 22 | CKR/Parsers/KataDamSheetParser.cs:22 | long method | 162-line `Parse` with literal cell addresses and defaults inline |
| 23 | KE/KataExportExternalEventHandler.cs:96-141 | OCP / long method | request kinds dispatched by a 73-line switch; adding a kind edits enum + switch + runner interface |
| 24 | KE/ViewModel/IKataExportRunner.cs:13-28 | ISP | one runner mixes export (Highlight, Repick) and rebar (Preview, Generate) |
| 25 | KE/Service/KataRunReader.cs:27-93, KataSupportCollector.cs:177-185, KataGridReader.cs:51-52, CKR/Calculators/KataRebarCalculator.cs:67,70, CKE/Calculators/KataRowBuilder.cs:56 | naming / consistency | user-visible warnings/exceptions in English, mixed into Vietnamese lists in the same window |

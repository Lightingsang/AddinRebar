# Project: HPDcePro Wave 2 (Phases 2 through 6)

## Architecture
- **Layer 1: Domain Core (`HPDcePro.Core`)**: Multi-targeting `netstandard2.0;net8.0`. Pure C# computational geometry, structural engineering calculations (TCVN 5574:2018, TCVN 5575:2024, TCVN 10304:2025, TCVN 2737:2023), 2D CAD drafting primitives buffer (`CadDraftingBuffer`), 24-step `AutoStr` automation pipeline with iterative auto-resizing, quantity takeoff, and bidirectional ETABS/Revit data exchange. Zero references to Autodesk or COM.
- **Layer 2: UI Presentation (`HPDcePro.UI`)**: Target `net8.0-windows`. Host-neutral WPF MVVM with `MahApps.Metro` 2.4.11 and Segoe UI theming, supporting dynamic Dark/Light palette switching, STA isolation, reactive ViewModels, and interactive diagram rendering for Beam, Column/Wall, Foundation/Slab, and Steel Connections.
- **Layer 3: AutoCAD Host Adapter (`HPDcePro.AutoCad`)**: Target `net8.0-windows`. AutoCAD 2026 adapter with 70 `[CommandMethod]` commands covering all 69 legacy aliases from `DCE Pro.lsp` plus `DCENAP`, delegating to real domain services, `CadDraftingBufferRenderer`, and modal/modeless UI windows.
- **Layer 4: Excel Host Adapter (`HPDcePro.Excel`)**: Target `net8.0`. Headless ClosedXML 0.104.2 multi-sheet calculation, rebar schedule, and quantity takeoff BOQ workbook export without background Excel COM automation.
- **Layer 5: Revit Host Adapter (`HPDcePro.Revit`)**: Target `net8.0-windows`. Revit 2026 data exchange service and `RevitRebarPlacementService` mapping `HPDcePro.Core` rebar geometry to native Revit 3D `Rebar` elements.
- **Layer 6: Test Verification (`HPDcePro.Core.Tests` & `HPDcePro.Adapters.Tests`)**: Target `net8.0` / `net8.0-windows`. xUnit v3 running on `Microsoft.Testing.Platform` verifying all calculators, geometry primitives, serialization, UI viewmodels, and host adapters with $\ge 200$ automated tests passing 100%.

## Feature Inventory
Every feature from the survey phase is enumerated below with its assigned milestone:
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | ContinuousBeamCalculator | Multi-span continuous beam design, moment redistribution $\le 20\%$, curtailment, lap zones | M1 | Survey (F1) |
| 2 | FoundationBeamCalculator | Substructure foundation/ground tie beam design with inverted moment diagram & deep shear | M1 | Survey (F2) |
| 3 | CurvedBeamCalculator | Circular arc and non-prismatic beam chord discretization, combined flexure-shear-torsion | M1 | Survey (F3) |
| 4 | BeamTrimmerV2 | Concentrated hanger rebar and dense stirrup zones at beam-beam intersections (`dam_giao`) | M1 | Survey (F4) |
| 5 | CadDraftingBuffer & Primitives | Host-independent 2D CAD vector drawing primitive buffer (`Line`, `Polyline`, `Text`, `Dim`, `XData`) | M1 | Survey (F5) |
| 6 | BeamShopDrawingGenerator | Generates longitudinal elevations (1:25) and cross-sections (1:20) with `RebarXdataDCE` metadata | M1 | Survey (F6) |
| 7 | EtabsModelSynchronizer | Bidirectional synchronization of stories, grids, frame sections, and internal forces / $A_s$ | M1 | Survey (F7) |
| 8 | ColumnPmmInteractionCalculator | TCVN 5574:2018 non-linear fiber P-M-M interaction diagram and slenderness buckling $\eta$ | M2 | Survey (F8) |
| 9 | ColumnDetailingGenerator | Multi-story column schedule, splices in $H_n/3..2H_n/3$, 1:6 cranked bends, `REC1`/`REC2`/`Ci` | M2 | Survey (F9) |
| 10 | ShearWallCalculator | Core wall boundary element cages ($l_c \ge 0.15 L_w$), web rebar, cross-ties, and shear check | M2 | Survey (F10) |
| 11 | ColumnWallDetailingWindow & VM | WPF MahApps.Metro user interface with real-time P-M-M diagram rendering and section preview | M2 | Survey (F11) |
| 12 | ColumnCommands Upgrade | Wires `VeCotDcePro`, `VeVachDcePro`, `ColumnREC1/2/Ci`, etc. to real calculation & detailing | M2 | Survey (F26a) |
| 13 | PileCapCalculator | 1 to $N$ pile cap layout, reactions $P_i$, 45° pyramid punching shear, flexure, `XuatToaDoCoc` schedule | M3 | Survey (F12) |
| 14 | PadFootingCalculator | Isolated spread footing: soil pressure distribution, uplift check, punching shear, flexural rebar | M3 | Survey (F13) |
| 15 | TwoWaySlabCalculator | Two-way slab panel moment calculation ($\lambda \le 2.0$), bottom mesh, top negative rebar ($L_0/4$) | M3 | Survey (F14) |
| 16 | StaircaseCalculator | Reinforced concrete staircase detailing for waist slab (`EngineBanThang`) and folded plate (`EngineZigZac`)| M3 | Survey (F15) |
| 17 | LintelCalculator | Cast-in-place or precast lintel beam design over door and window openings ($800-3200\text{ mm}$) | M3 | Survey (F16) |
| 18 | FoundationSlabWindow & VM | WPF MahApps.Metro user interface for foundation pile caps, pad footings, slabs, and stairs | M3 | Survey (F17) |
| 19 | Footing & Slab Commands Upgrade| Wires `VeMongCoc_DCE`, `VeMongDon_DCE`, `XuatToaDoCoc`, `VeThepSanTop/Bot`, `VeThang_DCE`, etc. | M3 | Survey (F26b) |
| 20 | AutoStrOrchestrator & 24 Steps | End-to-end automation pipeline running Steps 01 to 24 with checkpointing and event tracking | M4 | Survey (F18) |
| 21 | AutoStr Auto-Resizing Loop | Feedback loop auto-expanding column sections (`CotChoTangTietDien`) and pile count (`CocTuTangKhiKhongDat`)| M4 | Survey (F19) |
| 22 | QuantityTakeoffCalculator | Multi-story structural quantity takeoff: concrete ($m^3$), formwork ($m^2$), rebar tonnage ($kg$) | M4 | Survey (F20) |
| 23 | ClosedXmlReportExporter (Multi-Sheet BOQ)| Generates master multi-sheet project BOQ and calculation verification spreadsheet (`.xlsx`) | M4 | Survey (F21) |
| 24 | AutoStr & Rebar Commands Upgrade| Wires `AutoStr`, `GETFORM`, `TongHopThongKe_dce`, `QuetThongKe_dce` to real pipeline | M4 | Survey (F26c) |
| 25 | PreGableFrameCalculator | Pre-engineered portal frame geometry, tapered built-up I-sections $h(x)$, and purlin layout | M5 | Survey (F22) |
| 26 | SteelMomentConnectionCalculator | TCVN 5575:2024 bolted end-plate moment connection, flange/web welds, bolt tension with prying ($Q$) | M5 | Survey (F23) |
| 27 | AnchorBoltBasePlateCalculator | Column base plate bearing pressure on pedestal, plate thickness $t_{bp}$, anchor bolt embedment | M5 | Survey (F24) |
| 28 | SteelConnectionWindow & VM | WPF MahApps.Metro user interface for steel moment connections, base plates, and portal frames | M5 | Survey (F25) |
| 29 | Full 70 AutoCAD Commands Upgrade| Complete command surface execution replacing all placeholders in `HPDcePro.AutoCad/Commands/` | M5 | Survey (F26d) |
| 30 | RevitRebarPlacementService | Maps `HPDcePro.Core` detailing models to native Revit 2026 3D `Rebar` elements with hooks and covers | M5 | Survey (F27) |
| 31 | Full Test Expansion & Verification| Comprehensive test expansion across all modules reaching $\ge 200$ automated tests passing 100% | M6 | Survey (F28) |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Phase 2: Beam Detailing, Drafting Primitives & ETABS Bridge | Features 1–7 | None | DONE |
| M2 | Phase 3: Column & Shear Wall Detailing + Biaxial P-M-M Engine | Features 8–12 | M1 (Drafting Buffer) | DONE |
| M3 | Phase 4: Substructure, Two-Way Slab, Stairs & Lintel | Features 13–19 | M1 (Drafting Buffer) | DONE |
| M4 | Phase 5: 24-Step AutoStr Pipeline & Multi-Sheet BOQ | Features 20–24 | M1, M2, M3 | DONE |
| M5 | Phase 6: Steel Connections, Full CAD Commands & Revit Rebar | Features 25–30 | M1, M2, M3, M4 | DONE |
| M6 | Test Suite Expansion ($\ge 200$ tests) & Victory Audit | Feature 31 | M1, M2, M3, M4, M5 | DONE |

## Interface Contracts

### ContinuousBeam ↔ CadDraftingBuffer & BeamShopDrawingGenerator
- `ContinuousBeamResult`: Contains `IReadOnlyList<BeamSpanResult> Spans`, `IReadOnlyList<ContinuousBarDefinition> MainTopBars`, `IReadOnlyList<ContinuousBarDefinition> MainBottomBars`, `IReadOnlyList<ExtraBarDefinition> SupportExtraBars`, `IReadOnlyList<ExtraBarDefinition> SpanExtraBars`, `IReadOnlyList<StirrupZoneDefinition> StirrupZones`, `IReadOnlyList<LapSpliceDefinition> LapSplices`.
- `BeamShopDrawingGenerator.GenerateShopDrawing(ContinuousBeamResult, BeamDetailingOptions)` returns `CadDraftingBuffer`.
- Attached XData: Each rebar polyline carries `RebarXdataDCE` containing bar designation, count, diameter, segments (A, B, C, D), and total length $L$.

### Column/Wall ↔ CadDraftingBuffer & UI
- `ColumnPmmResult`: Contains `double UtilizationRatio`, `bool IsSafe`, `double NeutralAxisAngleRad`, `double UltimateAxialForce`, `double UltimateMomentX`, `double UltimateMomentY`, `IReadOnlyList<PmmPoint3D> InteractionSurface`.
- `ColumnDetailingGenerator.GenerateElevationAndSlices(ColumnDetailingInput)` returns `CadDraftingBuffer` with multi-story elevation, cranked bends (slope $\le 1:6$), splice zones ($H_n/3..2H_n/3$), and cross-sections (`ColumnREC1`, `ColumnREC2`, `ColumnCi`).

### Foundation/Slab ↔ CadDraftingBuffer & UI
- `PileCapResult`: Contains `IReadOnlyList<PileReactionResult> Piles`, `double MaxPileReaction`, `double MinPileReaction`, `double PunchingCapacityRatio`, `double RequiredBottomAsX`, `double RequiredBottomAsY`, `IReadOnlyList<PileCoordinateScheduleItem> CoordinateSchedule`.
- `PadFootingResult`: Contains `double MaxBasePressure`, `double MinBasePressure`, `double Eccentricity`, `double PunchingRatio`, `double RequiredAsX`, `double RequiredAsY`.
- `TwoWaySlabResult`: Contains `double SpanMomentX`, `double SpanMomentY`, `double SupportMomentX`, `double SupportMomentY`, `IReadOnlyList<SlabRebarLayer> Layers`.

### AutoStr Pipeline ↔ Context & Steps
- `IAutoStrStep`: `int StepNumber { get; }`, `string StepName { get; }`, `Task<AutoStrStepResult> ExecuteAsync(AutoStrStepContext context, CancellationToken cancellationToken)`.
- `AutoStrStepContext`: Shared blackboard containing `AutoStrConfig Config`, `string ProjectRootPath`, `FramingTopology Topology`, `Dictionary<string, object> StepArtifacts`, `List<AutoStrLogEntry> Logs`, `Action<int, string, double> ReportProgress`.
- Auto-resizing feedback loop: Checks `CotChoTangTietDien` and `CocTuTangKhiKhongDat` after analysis steps and updates `AutoStrConfig` sections and pile counts accordingly before re-running detailing steps.

### Steel Connections ↔ Base Plates & UI
- `SteelConnectionResult`: Contains `double RequiredPlateThickness`, `double BoltTensionRatio`, `double BoltShearRatio`, `double WeldStressRatio`, `bool StiffenersRequired`, `double StiffenerThickness`.
- `BasePlateResult`: Contains `double BearingStress`, `double AllowableBearingStress`, `double RequiredPlateThickness`, `int AnchorBoltCount`, `double AnchorBoltDiameter`, `double EmbedmentLength`.

## Code Layout
```
HPDcePro/
├── src/
│   ├── HPDcePro.Core/
│   │   ├── Geometry/               # 2D point/line/polygon/convexhull/grid/topology
│   │   ├── Drafting/               # CadDraftingBuffer, CadEntityPrimitive, BeamShopDrawingGenerator
│   │   ├── Calculations/
│   │   │   ├── Beam/               # ContinuousBeam, FoundationBeam, CurvedBeam, BeamTrimmer
│   │   │   ├── Column/             # ColumnPmmInteraction, ColumnDetailing
│   │   │   ├── Wall/               # ShearWallCalculator
│   │   │   ├── Foundation/         # PileCapCalculator, PadFootingCalculator, FoundationDetailing
│   │   │   ├── Slab/               # TwoWaySlabCalculator
│   │   │   ├── Secondary/          # StaircaseCalculator, LintelCalculator
│   │   │   ├── Steel/              # SteelMomentConnection, AnchorBoltBasePlate, PreGableFrame
│   │   │   └── Takeoff/            # QuantityTakeoffCalculator
│   │   ├── AutoStr/                # AutoStrOrchestrator, IAutoStrStep, AutoStrStep01..24
│   │   ├── Etabs/                  # EtabsDataModels, EtabsModelSynchronizer
│   │   ├── MachineLearning/        # MauHocDatabaseReader, RebarTextTokenizer
│   │   ├── Rebar/                  # RebarCuttingOptimizer, RebarScheduleAggregator
│   │   ├── Serialization/          # AutoStrConfig, AutoStrProjectSerializer
│   │   └── XData/                  # 13 XData codecs and registry
│   ├── HPDcePro.UI/
│   │   ├── Views/                  # BeamDetailingWindow, CuttingOptimizationWindow, AutoStrConfigWindow,
│   │   │                           # ColumnWallDetailingWindow, FoundationSlabWindow, SteelConnectionWindow
│   │   ├── ViewModels/             # Corresponding MVVM ViewModels
│   │   └── Styles/                 # Theme.xaml, ThemeDark.xaml, ThemeLight.xaml, ThemeManager
│   ├── HPDcePro.AutoCad/
│   │   ├── Commands/               # BeamCommands, ColumnCommands, SlabCommands, FootingCommands,
│   │   │                           # RebarCommands, AutoStrCommands (70 commands total)
│   │   ├── Drafting/               # CadDraftingBufferRenderer (AutoCAD DB transaction writer)
│   │   └── HPDcePro.lsp            # Multi-CAD loader & LISP aliases
│   ├── HPDcePro.Excel/
│   │   ├── ClosedXmlReportExporter.cs
│   │   ├── ClosedXmlBoqExporter.cs
│   │   └── CalculationUdfBridge.cs
│   └── HPDcePro.Revit/
│       ├── RevitDataExchangeService.cs
│       ├── RevitRebarPlacementService.cs
│       └── Commands/
└── tests/
    ├── HPDcePro.Core.Tests/
    └── HPDcePro.Adapters.Tests/
```

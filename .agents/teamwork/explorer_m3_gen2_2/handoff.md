# Milestone M3 Handoff Report: Specialized UI Dialogs & AutoStr Engine Exploration

**Agent**: `explorer_m3_gen2_2`  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_gen2_2\`  
**Parent Conversation ID**: `1e55a9de-0d52-405d-83fa-fa58bb05a293`  
**Target Milestone**: M3 (Specialized UI Dialogs & AutoStr Engine Realization)  
**Deliverables**: `HPDcePro.UI`, `HPDcePro.Core/AutoStr/`, `HPDcePro.AutoCad/Palettes/`, `HPDcePro.AutoCad/Ribbon/`  

---

## 1. Observation

### 1.1 Existing UI Subsystem in `HPDcePro/src/HPDcePro.UI/`
Direct inspection of `HPDcePro/src/HPDcePro.UI/` revealed a modern, fully functional WPF presentation layer:
- **Project Configuration (`HPDcePro.UI.csproj`)**:
  - Target Framework: `net8.0-windows` with `<UseWPF>true</UseWPF>`, `<Nullable>enable</Nullable>`.
  - Dependencies: `MahApps.Metro` v2.4.11, `ControlzEx` v4.4.0, `CommunityToolkit.Mvvm` v8.4.0.
  - Project Reference: references `..\HPDcePro.Core\HPDcePro.Core.csproj` only. Zero references to `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`.
- **Theme Infrastructure (`Themes/` & `Common/ThemeManager.cs`)**:
  - `Theme.xaml`: Imports MahApps base controls, font sets (`Segoe UI`), spacing tokens (`Spacing.XS`..`L`), and defines standardized control styles: `StandardTextBox`, `StandardComboBox`, `Card.Panel`, `GroupBox.Header`, `PrimaryButton`, `SecondaryButton`, and `StandardDataGrid`.
  - `ThemeDark.xaml` / `ThemeLight.xaml`: Palette brushes for AutoCAD/Revit dark and light themes (`#2B2B2B`, `#353535`, `#0696D7`, `#F06A6A`, `#5FBF7A`).
  - `ThemeManager.cs`: Provides `EnsureAssembliesLoaded()` to guarantee assembly loading before XAML parsing, `ApplyTheme(window, dark)`, and `Attach(window, host, icon)` for dynamic AutoCAD `COLORTHEME` synchronization.
- **Existing Windows & ViewModels**:
  - Currently implemented: `AutoStrConfigWindow`, `BeamDetailingWindow`, `ColumnWallDetailingWindow`, `CuttingOptimizationWindow`, `FoundationSlabWindow`, `MainControlCenterWindow`, `SteelConnectionWindow`.
  - All existing windows inherit `mah:MetroWindow`, call `ThemeManager.EnsureAssembliesLoaded()` and `ThemeManager.Apply(this, dark: true)` in code-behind, and bind to `ObservableObject` ViewModels using MVVM Toolkit source generators (`[ObservableProperty]`, `[RelayCommand]`).

### 1.2 The 12 Specialized UI Dialogs & Controls Status
Cross-referencing `PROJECT.md` Feature 7, Interface Contract 2, and `docs/rea-dce-pro/TRUTH_MATRIX.md` Subsystem 8:
All 12 items were originally decompiled WinForms/PaletteSet/Ribbon in `DCE Pro.dll` and classified as `ĐANG LÀ MOCK` pending clean-room realization in Milestone M3:
1. `DamCong_Frm` (`TypeDef: 0x02000DA6`, 2,045 lines) -> `DamCongWindow.xaml`
2. `MongCoc_Frm` (`TypeDef: 0x02000161`, 4,612 lines) -> `MongCocWindow.xaml`
3. `MongCoc_FrmLopThepThem` (`TypeDef: 0x02000163`, 1,241 lines) -> `MongCocLopThepThemWindow.xaml`
4. `MongCoc_FrmChonThepTuDong` (`TypeDef: 0x02000165`, 232 lines) -> `MongCocChonThepTuDongWindow.xaml`
5. `ThangMoi_Frm` (`TypeDef: 0x02000136`, 4,096 lines) -> `ThangMoiWindow.xaml`
6. `CocBT_Frm` (`TypeDef: 0x02000DCA`, 411 lines) -> `CocBtWindow.xaml`
7. `TKT_frmMain` (`TypeDef: 0x02000246`, 2,328 lines) -> `MasterRebarScheduleDashboardWindow.xaml`
8. `AutoStrWindow` (`TypeDef: 0x02000B23`, 8,007 lines) -> `AutoStrExecutionWindow.xaml`
9. `Bea_ToiUuCatThepV2` (`TypeDef: 0x02000E5D`, 561 lines) -> `CuttingOptimizationAdvancedWindow.xaml`
10. `DcePalette` (`TypeDef: 0x02000E12`, 81 lines) -> `DcePaletteControl.xaml`
11. `DcePalette_WallThickness` (`TypeDef: 0x02000E13`, 174 lines) -> `WallThicknessPaletteControl.xaml`
12. `DceRibbon` (`TypeDef: 0x02000E1E`, 154 lines) -> `DceRibbonBuilder.cs`

Evidence in `HPDcePro/assets/Language/Cad/en.txt` (lines 2230–2350, 3863–3868) confirms the exact field names, parameters, button labels, and prompt sequences for every single one of these dialogs.

### 1.3 AutoStr 24-Step Pipeline in `HPDcePro/src/HPDcePro.Core/AutoStr/`
Direct inspection of `HPDcePro/src/HPDcePro.Core/AutoStr/` revealed:
- `AutoStrOrchestrator.cs` manages sequential execution of 24 steps (`Steps.Add(new AutoStrStep01_TaoSheetSet())` .. `Step24_TMTT`), progress reporting via `ProgressChanged`, checkpoint persistence (`AutoStrCheckpoint.cs`), and auto-resizing loops:
  - `TryResizeOverstressedColumns(context)` triggered at Step 10 when `Config.CotChoTangTietDien == true`.
  - `TryResizeOverloadedPiles(context)` triggered at Step 15 when `Config.CocTuTangKhiKhongDat == true`.
- Current execution status of the 24 steps:
  - Steps 1, 2, 3, 4, 5, 6, 9, 11, 12, 13, 14, 17, 19, 20, 21, 22 are operational domain CAD buffer / topology steps producing authentic `CadDraftingBuffer` entities or artifacts.
  - Steps 7, 8, 10, 15, 16, 18, 23, 24 were simplified with synthetic moments or dummy flags in M1/M2 (`ĐANG LÀ MOCK`).
- All required domain calculators are already implemented in `HPDcePro.Core.Calculations`:
  - `CurvedBeamCalculator`: Curved beam flexure, shear, torsion, chord discretization, `NHAN_XDATA_DAM_CONG`.
  - `PileCapCalculator`: Column/corner punching shear, one-way shear, pile reaction distribution, flexure $A_{sx}, A_{sy}$.
  - `PileCapacityCalculator`: TCVN 10304:2025 SPT-based bearing capacity for driven and bored piles.
  - `StaircaseCalculator`: 2-flight/folded plate staircase geometry, Blondel check, design moment, main & cranked bars.
  - `BeamFlexureCalculator`, `BeamShearCalculator`, `BeamCrackCalculator`: TCVN 5574:2018 beam equations.
  - `ColumnPmmInteractionCalculator`: Non-linear fiber interaction diagram $P-M_x-M_y$.
  - `TwoWaySlabCalculator`: Wood-Armer slab design moments.
  - `QuantityTakeoffCalculator`: Concrete, formwork, and rebar tonnage aggregation.

---

## 2. Logic Chain

1. **Architectural Separation**:
   - `HPDcePro.UI` must remain pure WPF (`net8.0-windows`) referencing `HPDcePro.Core` with zero AutoCAD dependencies. This ensures UI dialogs can be launched from AutoCAD (`AutoCadUiHelper.ShowModal`), Revit, or the standalone Desktop Hub without binary coupling.
   - The 9 full dialogs (`DamCongWindow`, `MongCocWindow`, `MongCocLopThepThemWindow`, `MongCocChonThepTuDongWindow`, `ThangMoiWindow`, `CocBtWindow`, `MasterRebarScheduleDashboardWindow`, `AutoStrExecutionWindow`, `CuttingOptimizationAdvancedWindow`) belong in `HPDcePro.UI.Views` with corresponding ViewModels in `HPDcePro.UI.ViewModels`.
   - The dockable palette UserControls (`DcePaletteControl.xaml`, `WallThicknessPaletteControl.xaml`) belong in `HPDcePro.UI.Controls` (or `HPDcePro.AutoCad.Palettes`) and are hosted inside AutoCAD `PaletteSet` via `Autodesk.AutoCAD.Windows.PaletteSet.AddVisual()`.
   - `DceRibbonBuilder.cs` references `Autodesk.Windows.RibbonControl` and belongs exclusively in `HPDcePro.AutoCad/Ribbon/`.

2. **Integration of Domain Calculators to ViewModels**:
   - Instead of inventing new algorithms, the ViewModels must directly wrap the existing, verified calculators in `HPDcePro.Core.Calculations`.
   - For example:
     - `DamCongViewModel` calls `CurvedBeamCalculator.Calculate(input)`.
     - `MongCocViewModel` calls `PileCapCalculator.Calculate(input)`.
     - `ThangMoiViewModel` calls `StaircaseCalculator.Calculate(input)`.
     - `CocBtViewModel` calls `PileCapacityCalculator.Calculate(...)`.
     - `AutoStrExecutionViewModel` subscribes to `AutoStrOrchestrator.ProgressChanged` and invokes `orch.ExecuteAsync()`.

3. **AutoStr Pipeline Realization**:
   - The 8 steps marked `ĐANG LÀ MOCK` can be easily converted to authentic execution:
     - `AutoStrStep08_TinhToanDamExcel`: replace synthetic moment formula with framing span moments evaluated from `BeamFlexureCalculator` and `BeamShearCalculator`.
     - `AutoStrStep10_TinhToanCotExcel`: wire into `ColumnPmmInteractionCalculator` using actual tributary axial loads $N$ and column dimensions.
     - `AutoStrStep15_EtabsMong`: compute pile reactions using `PileCapCalculator.CalculatePileReactions` against column base loads.
     - `AutoStrStep16_TTMong`: execute `PileCapCalculator.CheckColumnPunching` and flexural rebar design.
     - `AutoStrStep18_TTGiangM`: size tie beams using `FoundationBeamCalculator`.
     - `AutoStrStep24_TMTT`: produce structural calculation summary text and report metadata using `QuantityTakeoffResult`.

4. **STA Thread Test Strategy**:
   - WPF windows require STA threads. `HPDcePro.Adapters.Tests` already has a verified `RunOnStaThread` helper in `WindowInstantiationTests.cs`.
   - Adding tests for all 9 windows + 2 palettes guarantees zero `StaticResourceExtension` failures and proves theme switching (Dark & Light) succeeds.

---

## 3. Caveats

1. **AutoCAD Process Requirement for PaletteSet and Ribbon**:
   - `Autodesk.AutoCAD.Windows.PaletteSet` and `Autodesk.Windows.ComponentManager.Ribbon` require an active AutoCAD GUI process (`acad.exe`). When running in headless xUnit tests, instantiate the underlying WPF `UserControl`s directly; wrap AutoCAD API calls with try-catch fallback to prevent `InvalidProgramException`.
2. **Sequential Build & Test Execution Constraint**:
   - User directive strictly mandates: **NEVER run `dotnet test`**. Always compile with `dotnet build --disable-build-servers -m:1` and run compiled `.exe` test runners sequentially.

---

## 4. Conclusion & Concrete Blueprint for Worker M3

### 4.1 Specification of the 12 Specialized UI Dialogs & Controls

#### 1. `DamCongWindow` (`DamCong_Frm`)
- **Location**: `src/HPDcePro.UI/Views/DamCongWindow.xaml` (`.xaml.cs`) + `ViewModels/DamCongViewModel.cs`
- **ViewModel Bindings**:
  - Properties: `BeamMark` ("CB1"), `CenterX` (0), `CenterY` (0), `RadiusMm` (6000), `StartAngleDeg` (0), `EndAngleDeg` (90), `WidthMm` (300), `HeightMm` (600), `CoverMm` (35), `DesignMomentKnM` (150), `DesignShearKn` (120), `DesignTorsionKnM` (40), `MaxChordLengthMm` (1000), `SelectedConcrete` (B25), `SelectedRebar` (CB400V).
  - Computed Outputs: `ArcLengthMm`, `SweepAngleDeg`, `TorsionCapacityMaxKnm`, `IsTorsionCompliant`, `RequiredStirrupAswPerS`, `RequiredLongitudinalAslMm2`, `FlexuralAsRequiredMm2`, `StatusSummary`.
  - Commands: `CalculateCommand`, `GenerateDraftingBufferCommand`, `CloseCommand`.

#### 2. `MongCocWindow` (`MongCoc_Frm`)
- **Location**: `src/HPDcePro.UI/Views/MongCocWindow.xaml` (`.xaml.cs`) + `ViewModels/MongCocViewModel.cs`
- **ViewModel Bindings**:
  - Properties: `CapMark` ("ĐC1"), `CapWidthBMm` (1800), `CapLengthLMm` (1800), `CapThicknessHMm` (900), `ColumnWidthMm` (400), `ColumnLengthMm` (400), `AxialForceNKn` (1500), `MomentMxKnm` (50), `MomentMyKnm` (40), `PileCapacityRcKn` (600), `PileCount` (4), `PileDiameterMm` (400), `PileEmbedmentMm` (100), `BasementSlabThicknessMm` (200), `SelectedPattern` (Enum: 1-pile, 2-pile, 3-pile triangular, 4-pile rectangular, 5-pile, 6-pile).
  - Computed Outputs: `MaxPileReactionKn`, `MinPileReactionKn`, `PileCapacityRatio`, `ColumnPunchingRatio`, `CornerPilePunchingRatio`, `OneWayShearRatio`, `RequiredAsXMm2`, `RequiredAsYMm2`, `StatusText`.
  - Child Dialog Triggers: `OpenLopThepThemCommand`, `OpenChonThepTuDongCommand`.

#### 3. `MongCocLopThepThemWindow` (`MongCoc_FrmLopThepThem`)
- **Location**: `src/HPDcePro.UI/Views/MongCocLopThepThemWindow.xaml` (`.xaml.cs`) + `ViewModels/MongCocLopThepThemViewModel.cs`
- **ViewModel Bindings**:
  - Properties: `CapMark`, `HasTopRebarMesh` (bool), `TopRebarDiameterMm` (14), `TopRebarSpacingMm` (150), `HasSideSkinRebar` (bool), `SideSkinDiameterMm` (12), `SideSkinSpacingMm` (200), `HasStirrupTiesX` (bool), `StirrupTypeX` (Đai □, Đai C), `HasStirrupTiesY` (bool), `StirrupTypeY` (Đai □, Đai C), `PileStarterBarsCount` (4), `PileStarterDiameterMm` (16), `PileStarterLapLengthMm` (600).
  - Additional Layers Table: `ObservableCollection<SecondaryRebarLayerItem>` with `LayerName`, `Direction`, `Diameter`, `Spacing`, `Description`.
  - Commands: `AddLayerCommand`, `RemoveLayerCommand`, `ApplyCommand`.

#### 4. `MongCocChonThepTuDongWindow` (`MongCoc_FrmChonThepTuDong`)
- **Location**: `src/HPDcePro.UI/Views/MongCocChonThepTuDongWindow.xaml` (`.xaml.cs`) + `ViewModels/MongCocChonThepTuDongViewModel.cs`
- **ViewModel Bindings**:
  - Properties: `RequiredAsXMm2`, `RequiredAsYMm2`, `CapWidthBMm`, `CapLengthLMm`, `SelectedDirection` (X / Y), `StrictlyPreferClosestAs` (bool, default true).
  - Candidates DataGrid: `ObservableCollection<RebarArrangementCandidate>` (`DiameterMm`, `SpacingMm`, `BarCount`, `ProvidedAsMm2`, `Ratio`, `IsSafe`, `SteelTonnageKg`).
  - Commands: `SearchOptimalConfigurationsCommand`, `SelectCandidateCommand`, `ApplyToCapCommand`.

#### 5. `ThangMoiWindow` (`ThangMoi_Frm`)
- **Location**: `src/HPDcePro.UI/Views/ThangMoiWindow.xaml` (`.xaml.cs`) + `ViewModels/ThangMoiViewModel.cs`
- **ViewModel Bindings**:
  - Properties: `StairMark` ("TH1"), `TotalRiseHMm` (3600), `FlightSpanLMm` (3000), `StairWidthBMm` (1200), `NumberOfSteps` (21), `WaistThicknessHMm` (120), `Landing1LengthMm` (1200), `Landing2LengthMm` (1200), `StairType` (WaistSlab / FoldedPlateZigZag), `PlanScale` (1:50), `SectionScale` (1:25), `MainBarDiameterMm` (12), `MainBarSpacingMm` (150), `StarterLegVerticalMm` (600), `StarterLegHorizontalMm` (400).
  - Computed Outputs: `RiserHeightMm`, `TreadWidthMm`, `BlondelValue` ($2h_r + b_t$), `IsErgonomicSafe`, `InclineAngleDeg`, `MaxDesignMomentKnm`, `RequiredMainAsMm2`, `RecommendedBarCount`, `CrankedBarLapLengthMm`, `StatusSummary`.
  - Commands: `CalculateCommand`, `GenerateDraftingBufferCommand`, `CloseCommand`.

#### 6. `CocBtWindow` (`CocBT_Frm`)
- **Location**: `src/HPDcePro.UI/Views/CocBtWindow.xaml` (`.xaml.cs`) + `ViewModels/CocBtViewModel.cs`
- **ViewModel Bindings**:
  - Properties: `PileMark` ("C1"), `InstallationType` (DrivenPile / BoredPile), `ShapeIsCircular` (bool), `DimensionMm` (350), `LengthSegment1M` (8), `LengthSegment2M` (8), `LengthSegment3M` (6), `MainBarCount` (4..8), `MainBarDiameterMm` (18..25), `DenseStirrupSpacingMm` (50..100), `DenseStirrupLengthMm` (1000), `NormalStirrupSpacingMm` (150..200), `TipSoilN60` (25), `SoilType` (Cohesive / Cohesionless).
  - Computed Outputs: `TotalPileLengthM`, `ConcreteVolumeM3`, `TipCapacityRbKn`, `ShaftCapacityRsKn`, `UltimateCapacityRuKn`, `DesignCapacityRcKn`, `StatusSummary`.
  - Commands: `CalculateCapacityCommand`, `GenerateDetailingBufferCommand`.

#### 7. `MasterRebarScheduleDashboardWindow` (`TKT_frmMain`)
- **Location**: `src/HPDcePro.UI/Views/MasterRebarScheduleDashboardWindow.xaml` (`.xaml.cs`) + `ViewModels/MasterRebarScheduleDashboardViewModel.cs`
- **ViewModel Bindings**:
  - Rebar Items DataGrid: `ObservableCollection<RebarScheduleRowItem>` with columns: `ElementMark`, `BarNumber`, `ShapeCode`, `DiameterMm`, `CountPerElement`, `ElementCount`, `TotalCount`, `DimA`, `DimB`, `DimC`, `DimD`, `TotalLengthMm`, `UnitWeightKgM`, `TotalWeightKg`.
  - Batch Edit Properties: `NewElementQuantity` (int), `NewDiameter` (int), `SelectedShapeFilter` (string).
  - Summary Cards: `TotalWeightSmallDiaKg` ($\phi \le 10$), `TotalWeightMediumDiaKg` ($10 < \phi \le 18$), `TotalWeightLargeDiaKg` ($\phi > 18$), `GrandTotalTonnageTon`.
  - Commands: `AddRowCommand`, `DeleteRowCommand`, `BatchUpdateQuantityCommand` (`TKTSSOLUONGCAUKIEN`), `BatchUpdateDiameterCommand` (`TKTSDUONGKINHTHEP`), `RecalculateWeightsCommand`, `ExportToExcelCommand`, `SendToCuttingOptimizerCommand`.

#### 8. `AutoStrExecutionWindow` (`AutoStrWindow`)
- **Location**: `src/HPDcePro.UI/Views/AutoStrExecutionWindow.xaml` (`.xaml.cs`) + `ViewModels/AutoStrExecutionViewModel.cs`
- **ViewModel Bindings**:
  - Properties: `ProjectName`, `OverallProgressPercentage` (0..100), `CurrentStepNumber` (1..24), `CurrentStepName`, `CurrentStepPercentage`, `ElapsedTimeString`, `ResizingIterationCount`, `IsExecuting` (bool), `IsPaused` (bool).
  - 24-Step Status ObservableCollection: `ObservableCollection<AutoStrStepStatusItem>` (`StepNumber`, `StepName`, `Status` [Pending/Running/Success/Failed/Skipped], `DurationSeconds`, `Message`).
  - Live Log Console: `ObservableCollection<AutoStrLogEntryItem>` (`Timestamp`, `StepNumber`, `Level` [Info/Warning/Error], `Message`).
  - Commands: `StartPipelineCommand`, `PauseResumeCommand`, `CancelCommand`, `ResumeCheckpointCommand`, `OpenOutputsFolderCommand`.

#### 9. `CuttingOptimizationAdvancedWindow` (`Bea_ToiUuCatThepV2`)
- **Location**: `src/HPDcePro.UI/Views/CuttingOptimizationAdvancedWindow.xaml` (`.xaml.cs`) + `ViewModels/CuttingOptimizationAdvancedViewModel.cs`
- **ViewModel Bindings**:
  - Demands Multi-Diameter Grid: `ObservableCollection<CuttingDemandItem>` (`Mark`, `DiameterMm`, `LengthMm`, `Quantity`).
  - Available Commercial Stock Lengths: standard 11,700mm + custom remnant sizes.
  - Cutting Patterns Results DataGrid: `ObservableCollection<CuttingPatternItem>` (`PatternId`, `StockBarCount`, `PiecesCut`, `RemnantLengthMm`, `IsReusableScrap` [remnant $\ge 1500\text{ mm}$]).
  - Summary KPI Cards: `TotalStockBarsCount`, `TotalInputWeightTon`, `TotalCutWeightTon`, `ScrapWeightTon`, `ScrapPercentage` ($< 3.0\%$).
  - Commands: `OptimizeMultiDiameterCommand`, `SaveScrapToInventoryCommand` (`XDATA_APP_KHOTHEPDU`), `GenerateCutSchemeCadBufferCommand` (`XDATA_APP_SODOCAT`), `ExportExcelReportCommand`.

#### 10. `DcePaletteControl` (`DcePalette`)
- **Location**: `src/HPDcePro.UI/Controls/DcePaletteControl.xaml` (`.xaml.cs`) + `HPDcePro.AutoCad/Palettes/DcePaletteManager.cs`
- **Structure**:
  - Tabbed dockable AutoCAD Palette:
    - Tab 1: `Dầm & Cột` (Quick buttons: `VeDam_DCE`, `VeMCD_DCE`, `VeCotDcePro`, `ColumnREC1`, `ColumnCi`).
    - Tab 2: `Móng & Sàn` (Quick buttons: `VeMongCoc_DCE`, `VeMongDon_DCE`, `VeThepSan_DCE`, `ChiaKhoangCach`).
    - Tab 3: `Thép & Thống Kê` (Quick buttons: `TKT_DCE`, `STK_DCE`, `ToiUuCatThep`, `QuetThongKe_DCE`).
    - Tab 4: `AutoStr` (Quick buttons: `AutoStr`, `GETFORM`, `DceMayHoc`).
  - Palette Manager: registers `PaletteSet` in AutoCAD, sets dock style (Left / Right / Floating), and attaches `ThemeManager.Attach(control, host)`.

#### 11. `WallThicknessPaletteControl` (`DcePalette_WallThickness`)
- **Location**: `src/HPDcePro.UI/Controls/WallThicknessPaletteControl.xaml` (`.xaml.cs`) + `HPDcePro.AutoCad/Palettes/WallThicknessPaletteControl.xaml`
- **Structure**:
  - Compact vertical toolbar with 7 quick toggle buttons: `T100`, `T110`, `T150`, `T200`, `T220`, `T250`, `T300`.
  - When pressed, updates active AutoCAD system parameter `WallThickness` and invokes command prompt filter for beam width matching.

#### 12. `DceRibbon` (`DceRibbonBuilder`)
- **Location**: `src/HPDcePro.AutoCad/Ribbon/DceRibbonBuilder.cs`
- **Structure**:
  - Creates RibbonTab: Title `"DBIM Apps"`, Id `"HPDCE_TAB"`.
  - Constructs 6 Panels matching `assets/Language/Cad/en.txt` exactly:
    1. Panel `DCE ACCOUNT`: Login (`DangNhapDce`), Logout (`DangXuatDCE`), Setup (`DceSetup`), Support (`Admin_DCE`).
    2. Panel `EXCEL - AUTOCAD`: Vẽ dầm (`VeDam_DCE`), Vẽ cột (`VeCotDcePro`), Vẽ vách (`VeVachDcePro`), Vẽ móng cọc (`VeMongCoc_DCE`), Vẽ móng đơn (`VeMongDon_DCE`), Vẽ thép sàn (`VeThepSan_DCE`), Vẽ thang (`VeThang_DCE`), Vẽ lanh tô (`VeBlockLanhTo`).
    3. Panel `REBAR DATA`: Ẩn số hiệu (`AnSoHieuThep_dce`), Dim nhanh (`DimNhanh_dce`), Dim biến thiên (`DimBienThien_Dce`), Copy số hiệu (`CopySoHieuThep`), Copy khoảng rải (`CopyKhoangRaiThep`), Đoạn trừ dim dầm (`DceDoanTruDimDam`).
    4. Panel `THỐNG KÊ THÉP`: Tạo TKT (`TKT_DCE`), Sửa TKT (`STK_DCE`), Sửa SLCK (`TKTSSoLuongCauKien`), Sửa ĐKT (`TKTSDuongKinhThep`), Tổng hợp TK (`TongHopThongKe_dce`), Quét TKT (`QuetThongKe_DCE`), TK Coupler (`TongHopCoupler_DCE`).
    5. Panel `SHOP DRAWING`: Tối ưu cắt thép (`ToiUuCatThep`), Chia thép (`ChiaThep_DCE`), Chia thép liên tục (`ChiaThepLienTuc_DCE`), Cắt thép (`CutThep_DCE`), Mạch ngừng sàn (`ChiaMachNgungThepSan_DCE`).
    6. Panel `AUTOSTR & AI`: AutoStr (`AutoStr`), AutoStr Run (`DceAutoStr`), Control Center (`GETFORM`), Máy Học AI (`DceMayHoc`), Connect CSI (`ConnectCSIAPI_DCE`).

---

### 4.2 AutoStr 24 Steps Realization Blueprint

| Step # | Class Name | Input / Precondition | Authentic Calculation / Engine | Output Artifact & CAD Buffer |
|:---:|---|---|---|---|
| **01** | `AutoStrStep01_TaoSheetSet` | `context.ProjectRootPath` | Directory setup, DST initialization | Project folder structure (`01.SheetSet`..`12.InAn`) |
| **02** | `AutoStrStep02_LuoiTruc` | Layer `KT_TRUC` / plan | Axes grouping, boundary extension | Grid lines & bubbles (`DCE_Truc`) |
| **03** | `AutoStrStep03_DinhViCot` | Layer `KT_COT` / Xref | Column bounding boxes & centroids | `ColumnSectionDefinition` & marks $C_1..C_n$ |
| **04** | `AutoStrStep04_MBKC` | Column nodes, architectural walls | Span connectivity, beam generation | Framing layout (`DCE_Beam`, `DCE_BaoBeTong`) |
| **05** | `AutoStrStep05_DuongPhanTichLuc` | Beam centerlines | Rigid end offsets, wireframe graph | 1D analysis wireframe model |
| **06** | `AutoStrStep06_TaiTuong` | Layer `KT_TUONG`, wall thickness | $q = \gamma \cdot t \cdot (H_{story} - h_{beam})$ | Uniform line loads along framing beams |
| **07** | `AutoStrStep07_BuildEtabsModel` | Framing wireframe, loads | 14 ULS & 14 SLS combos, solver matrix | Solved story shears & frame envelope |
| **08** | `AutoStrStep08_TinhToanDamExcel` | Framing spans, envelope forces | `BeamFlexureCalculator` + `BeamShearCalculator` | Beam rebar design table ($A_{stop}, A_{sbot}, A_{sw}$) |
| **09** | `AutoStrStep09_VeChiTietDam` | Designed beams | Elevation drafting, $L_0/4$ cutoffs | Longitudinal & cross-section CAD buffer |
| **10** | `AutoStrStep10_TinhToanCotExcel` | Column forces $(N, M_x, M_y)$ | `ColumnPmmInteractionCalculator` | P-M-M interaction ratios & overstressed list |
| **11** | `AutoStrStep11_VeChiTietCotVach` | Sized columns | Schedule generation, splice levels | Column schedule CAD drafting buffer |
| **12** | `AutoStrStep12_VeChiTietSan` | Floor slab panels, spans | `TwoWaySlabCalculator` (Wood-Armer) | Top & bottom rebar mesh CAD buffer |
| **13** | `AutoStrStep13_MBMongCoc` | Column base reactions $N$ | Foundation positioning, pile clusters | Foundation layout plan CAD buffer |
| **14** | `AutoStrStep14_DPTLMong` | Footing layout | Strip tributary distribution lines | Foundation load path geometry |
| **15** | `AutoStrStep15_EtabsMong` | Substructure model, soil springs | `PileCapCalculator.CalculatePileReactions` | Pile reaction distribution & overloaded list |
| **16** | `AutoStrStep16_TTMong` | Pile reactions, column axial $N$ | `PileCapCalculator.CheckColumnPunching` | Punching ratios & bottom rebar $A_{sx}, A_{sy}$ |
| **17** | `AutoStrStep17_VeMong` | Designed pile caps | Plan & section drafting, column starters | Pile cap detailing CAD drafting buffer |
| **18** | `AutoStrStep18_TTGiangM` | Column moments, settlement | `FoundationBeamCalculator` | Tie beam dimensions ($b \times h$) & rebar |
| **19** | `AutoStrStep19_VeGiangM` | Designed tie beams | Longitudinal elevation drafting | Tie beam detailing CAD buffer |
| **20** | `AutoStrStep20_CKPhu` | Story heights, openings | `StaircaseCalculator` + `LintelCalculator` | Staircase & lintel detailing CAD buffer |
| **21** | `AutoStrStep21_DanhMuc` | Drawing sheets across project | `QuantityTakeoffCalculator` | Consolidated drawing index & rebar BOQ |
| **22** | `AutoStrStep22_MoSheetSet` | Sheet layouts | Sheet Set registration & sync | Synchronized `DuAn.dst` |
| **23** | `AutoStrStep23_InAn` | Layout names | Batch PDF publisher configuration | Batch plot specification & PDF target |
| **24** | `AutoStrStep24_TMTT` | Project calculations & takeoff | Document synthesis (`ThuyetMinhTinhToan`) | Structured calculation report (.docx) |

---

## 5. Verification Method

### 5.1 Test Plan for Worker M3
Worker M3 must verify the implementation via two automated test suites:
1. **`HPDcePro.Core.Tests`**:
   - `AutoStrPipelineTests.cs`: execute full 24-step pipeline end-to-end, validating that `IsSuccess == true`, `CompletedStepsCount == 24`, and `TakeoffResult != null`.
   - `AutoStrResizingLoopTests.cs`: test `TryResizeOverstressedColumns` and `TryResizeOverloadedPiles` verifying iterative convergence.
2. **`HPDcePro.Adapters.Tests`**:
   - `SpecializedWindowInstantiationTests.cs`: run on STA thread (`RunOnStaThread`) for all 9 windows:
     - `DamCongWindow`
     - `MongCocWindow`
     - `MongCocLopThepThemWindow`
     - `MongCocChonThepTuDongWindow`
     - `ThangMoiWindow`
     - `CocBtWindow`
     - `MasterRebarScheduleDashboardWindow`
     - `AutoStrExecutionWindow`
     - `CuttingOptimizationAdvancedWindow`
   - Verify that for every window: `Assert.NotNull(window)`, `Assert.NotNull(window.ViewModel)`, `ThemeManager.Apply(window, dark: true)`, and `ThemeManager.Apply(window, dark: false)` execute with 0 exceptions.

### 5.2 Mandatory Execution Commands (Honoring Single-Node Memory Guard & Test Runner Rule)
```powershell
# 1. Single-node sequential build (0 warnings, 0 errors):
dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1

# 2. Run test binaries directly (bypassing Microsoft.Testing.Platform NamedPipeServer to avoid Unknown Hard Error):
& "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Core.Tests\bin\Debug\net8.0\HPDcePro.Core.Tests.exe"
& "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Adapters.Tests\bin\Debug\net8.0-windows\HPDcePro.Adapters.Tests.exe"
```

### 5.3 Clean Code Governance Checks
- Ensure every `.cs` file is $\le 300$ lines (Rule C2).
- Ensure every method is $\le 50$ lines (Rule M3).
- 0 dead code or commented-out code (Rule Q-B1).
- 0 unannotated empty catch blocks (Rule Q-B2).

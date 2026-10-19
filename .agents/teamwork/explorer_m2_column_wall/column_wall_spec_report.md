# MILESTONE M2 SPECIFICATION REPORT: COLUMN & SHEAR WALL SHOP DRAWING (PHASE 3)
## Master Architecture, Domain Mathematical Models, UI Design, Host Commands & Test Plan

- **Deliverable**: `HPKata/HPKata.slnx` (.NET 8 SDK 10.0.300)
- **Document ID**: `HPKATA-SPEC-M2-COLUMN-WALL-2026`
- **Author**: `explorer_m2_column_wall` (Domain & Architecture Explorer)
- **Target Subsystems**: `HPKata.Core`, `HPKata.UI`, `HPKata.AutoCad`, `HPKata.Revit`, and Test Projects
- **Status**: COMPLETE & READY FOR IMPLEMENTATION
- **Governance**: `docs/clean-code/HP_CLEAN_CODE_CORE.md` (Max 300 lines/file, max 50 lines/method, 0 dead code, 0 unannotated catch, Core zero host dependencies)

---

## 1. Executive Summary & Architectural Overview

Milestone M2 implements **Phase 3 (Column & Shear Wall Shop Drawing)** of the HPKata clean-room reconstruction, expanding upon the validated Phase 1 Baseline and Phase 2 Beam & ETABS engine.

### 1.1 Scope of Phase 3 Deliverables
1. **`HPKata.Core` (Column & Wall Domain & Calculators)**:
   - Multi-story reinforced concrete columns: elevation chaining, cross-section layout, 11.7m commercial bar division, lap splice staggering ($1.3L_{lap}$ / $600\text{ mm}$), crank/offset bends ($1:6$ slope for step-downs $\le 50\text{ mm}$, straight cutoff + separate starter dowels for step-downs $> 50\text{ mm}$), seismic confinement zones ($l_0 = \max(b, h, H_n/6, 500\text{ mm})$), perimeter hoops, diamond/rhombus hoops, and cross-ties.
   - Reinforced concrete shear walls & core walls: planar walls, L/T shapes, elevator cores (U, C, and closed boxes), boundary element confinement cages (`ve_loi_vach`), double-curtain web distributed reinforcement (vertical & horizontal bars), U-bar edge hooks, opening trimmers ($45^\circ$ diagonal crack-control bars, extra jamb/header bars).
   - ETABS column & pier/wall import engine (`loadcolumn`, `loadwall`): parsing structural design tables ($P, M_2, M_3, V_2, V_3$, required $A_{st}$, boundary $A_{sb}$, shear $A_v/s$), auto-sizing longitudinal bars and tie distributions.
   - Schedule persistence and ClosedXML export matching `Kata.xlsm` sheets `Cot` and `Vach`.
2. **`HPKata.UI` (WPF ViewModels & Views)**:
   - `ColumnDetailingViewModel` & `ColumnDetailingWindow`: Multi-story column grid, section dimensions, longitudinal bars (corner & side bars), tie patterns, splice options, dynamic Theme.xaml brush bindings.
   - `WallDetailingViewModel` & `WallDetailingWindow`: Shear wall multi-story parameters, boundary elements (`ve_loi_vach`), double-curtain web mesh, opening definition.
   - `EtabsColumnWallImportViewModel`: Import dialog for ETABS column and wall pier design tables.
3. **`HPKata.AutoCad` (AutoCAD 2026 Commands & Lisp Aliases)**:
   - 10 commands registered with full names and standard Lisp aliases: `ve_cot` (`vc`), `ve_cot_shop` (`vcs`), `raicot`, `dimcot`, `tkcot`, `loadcolumn`, `ve_vach` (`vv`), `ve_vach_shop` (`vvs`), `ve_loi_vach` (`vlv`), `loadwall`.
4. **`HPKata.Revit` (Revit 2026 Commands & Services)**:
   - Interactive and batch generation of native 3D `Rebar` in `OST_StructuralColumns` and `OST_Walls` with idempotent replacement and multi-story scheduling.
5. **Testing Suite**:
   - $\ge 25$ new comprehensive unit, host, and E2E scenario tests across `HPKata.Core.Tests`, `HPKata.UI.Tests`, `HPKata.AutoCad.Tests`, `HPKata.Revit.Tests`, and `HPKata.E2E.Tests`.

---

## 2. HPKata.Core: Domain Models, Mathematics & Calculators

### 2.1 Multi-Story Column Domain Models (`HPKata.Core.Columns.Models`)

#### 1. `ColumnStorySegment`
Represents an individual story tier within a multi-story column run:
```csharp
namespace HPKata.Core.Columns.Models
{
    public sealed class ColumnStorySegment
    {
        public int StoryIndex { get; set; }
        public string StoryName { get; set; } = string.Empty;
        public double BottomElevationMm { get; set; }
        public double StoryHeightMm { get; set; }
        public double BeamDepthMm { get; set; }
        public double ClearHeightMm => Math.Max(0.0, StoryHeightMm - BeamDepthMm);
        public double TopElevationMm => BottomElevationMm + StoryHeightMm;
        
        // Dimensions
        public double WidthMm { get; set; }  // dimension b
        public double DepthMm { get; set; }  // dimension h
        
        // Offset relative to story below (0 = concentric, >0 = eccentric reduction)
        public double OffsetXFromLowerMm { get; set; }
        public double OffsetYFromLowerMm { get; set; }
        
        public string ConcreteGrade { get; set; } = "B25";
        public double ConcreteCoverMm { get; set; } = 30.0;
        
        public ColumnSectionModel Section { get; set; } = new();
        public ColumnTieDistributionModel TieDistribution { get; set; } = new();
        
        public void Validate()
        {
            if (StoryHeightMm <= 0.0) throw new ArgumentException("Story height must be positive.");
            if (WidthMm <= 0.0 || DepthMm <= 0.0) throw new ArgumentException("Column dimensions must be positive.");
            if (BeamDepthMm < 0.0 || BeamDepthMm >= StoryHeightMm) throw new ArgumentException("Invalid beam depth.");
        }
    }
}
```

#### 2. `ColumnSectionModel`
Encapsulates cross-section geometry, bar counts, diameter sizing, and spatial $(X, Y)$ coordinate generation:
- **Geometry**: Width $b$, Depth $h$, Corner bars ($4 \times \phi_{corner}$), Side bars along $b$ ($n_b$ bars per face, $\phi_b$), Side bars along $h$ ($n_h$ bars per face, $\phi_h$).
- **Total Bar Count**: $N = 4 + 2 \cdot n_b + 2 \cdot n_h$.
- **Total Steel Area**:
  $$A_{st} = 4 \cdot \frac{\pi d_{corner}^2}{4} + 2 n_b \cdot \frac{\pi d_b^2}{4} + 2 n_h \cdot \frac{\pi d_h^2}{4}$$
- **Bar Coordinates**:
  Given core bounds $X_{core} = \pm (b/2 - c - \phi_{tie} - d_{corner}/2)$, $Y_{core} = \pm (h/2 - c - \phi_{tie} - d_{corner}/2)$:
  - 4 Corners: $(X_{core}, Y_{core}), (-X_{core}, Y_{core}), (-X_{core}, -Y_{core}), (X_{core}, -Y_{core})$.
  - Side bars along $b$: distributed uniformly between corners at spacing $s_b = \frac{2 X_{core}}{n_b + 1}$.
  - Side bars along $h$: distributed uniformly between corners at spacing $s_h = \frac{2 Y_{core}}{n_h + 1}$.
  - Spacing check: Clear spacing $s_{clear} \ge \max(d, 50\text{ mm})$ per TCVN 5574:2018.

#### 3. `ColumnRebarLayout`
Represents the complete detailing layout of a multi-story column:
```csharp
namespace HPKata.Core.Columns.Models
{
    public sealed class ColumnRebarLayout
    {
        public string ColumnMark { get; set; } = "C1";
        public double FoundationBottomElevationMm { get; set; } = -2000.0;
        public double BasePlateElevationMm { get; set; } = 0.0;
        public List<ColumnStorySegment> Stories { get; } = new();
        public List<ColumnBarRunModel> LongitudinalBars { get; } = new();
        public List<ColumnStarterDowelModel> FoundationDowels { get; } = new();
        public Dictionary<int, ColumnTieDistributionModel> StoryTies { get; } = new();
    }
}
```

#### 4. `ColumnBarRunModel`
Describes each continuous or spliced longitudinal bar path:
- `BarMark`, `DiameterMm`, `LengthMm`, `StartZMm`, `EndZMm`.
- `HasBottomHook`, `BottomHookLengthMm` ($90^\circ$ standard bend into footing / pile cap).
- `HasTopHook`, `TopHookLengthMm` ($90^\circ$ bend into roof beam / roof slab).
- `HasBottomCrank`, `BottomCrankOffsetMm`, `BottomCrankLengthMm`.
- `HasTopCrank`, `TopCrankOffsetMm`, `TopCrankLengthMm`.
- `SpliceType`: `LapSplice`, `MechanicalCoupler`, `Welded`, `ContinuousStraight`.

---

### 2.2 Mathematical Rules for Column Splices & Story Transitions

#### 1. Transition Strategy & Crank Bend (`ColumnCrankBendCalculator`)
When column cross-section decreases from lower story $(b_1 \times h_1)$ to upper story $(b_2 \times h_2)$, the step-down $\Delta$ is calculated:
- **Concentric Reduction**: $\Delta_b = (b_1 - b_2)/2$, $\Delta_h = (h_1 - h_2)/2$.
- **Eccentric / Flush Edge Reduction**: $\Delta_b = b_1 - b_2$ on the reduced face, $0.0$ on the flush face.

**Rule Decision Matrix**:
| Step-Down Condition | Detailing Action | Technical Parameters |
|---|---|---|
| $\Delta = 0$ | Continuous straight bar | No bend |
| $0 < \Delta \le 50\text{ mm}$ | Offset crank bend (uốn cổ chai) | Slope $\le 1:6$ ($\tan \theta \le 1/6$); Vertical crank length $L_{crank} = 6 \cdot \Delta$. Formed inside beam-column joint depth $h_{beam}$. |
| $\Delta > 50\text{ mm}$ | Straight cutoff + separate starter dowels | Lower bar terminates straight below beam top with $90^\circ$ hook ($12d$); separate starter dowel ($L = L_{anc} + L_{lap}$) anchored into lower column. |

```csharp
public enum ColumnTransitionStrategy
{
    ContinuousStraight,
    CrankBend,
    CutoffAndStarterDowel
}

public static class ColumnCrankBendCalculator
{
    public const double MaxCrankStepDownMm = 50.0;
    public const double CrankSlopeRatio = 6.0; // 1:6 slope per TCVN 5574:2018 / ACI 318

    public static ColumnTransitionStrategy DetermineStrategy(double stepDownMm)
    {
        if (stepDownMm <= 0.001) return ColumnTransitionStrategy.ContinuousStraight;
        if (stepDownMm <= MaxCrankStepDownMm) return ColumnTransitionStrategy.CrankBend;
        return ColumnTransitionStrategy.CutoffAndStarterDowel;
    }

    public static double CalculateCrankLength(double stepDownMm) =>
        Math.Round(stepDownMm * CrankSlopeRatio, MidpointRounding.AwayFromZero);
}
```

#### 2. Lap Splice & Staggering (`ColumnSpliceCalculator`)
- **Lap Splice Length**:
  $$L_{lap} = \alpha \cdot L_{anc} \ge 30d$$
  For CB400-V in B25 concrete: $L_{lap} = 35d$ (compression), $45d$ (tension).
- **Splice Zone Location**:
  Splices start above the floor level and outside high seismic shear zones:
  $$Z_{splice,base} = Z_{floor} + h_{beam} + Z_{confinement} \quad \text{or} \quad Z_{floor} + 500\text{ mm}$$
- **Staggering Rule**:
  No more than $50\%$ of longitudinal bars may be spliced at any single elevation.
  - Tier A bars splice at $Z_{splice,1} = Z_{floor} + 600\text{ mm}$.
  - Tier B bars splice at $Z_{splice,2} = Z_{splice,1} + \Delta Z$, where $\Delta Z \ge \max(1.3 L_{lap}, 600\text{ mm})$.
- **Commercial Bar Division (11.7m)**:
  When a multi-story column run exceeds 11,700 mm, bars are segmented into standard lengths ($11,700\text{ mm}, 5,850\text{ mm}, 3,900\text{ mm}$) to minimize cutting scrap, with lap splices positioned in column mid-height zones.

---

### 2.3 Column Ties & Seismic Confinement Zones (`ColumnConfinementCalculator`)

#### 1. Confinement Zone Height ($l_0$)
At both the bottom of the column (above floor/footing) and the top (below beam soffit):
$$l_0 = \max\left(h_{col}, \frac{H_n}{6}, 500\text{ mm}\right)$$
where $h_{col} = \max(b, h)$ and $H_n = H_{story} - h_{beam}$.

#### 2. Confinement Tie Spacing ($a_1$)
Within the $l_0$ zones at column top and bottom:
$$a_1 \le \min\left(\frac{\min(b, h)}{4}, 6 d_{long}, 100\text{ mm}\right) \quad (\text{typically } a100)$$

#### 3. Non-Confinement Mid-Height Tie Spacing ($a_2$)
In the middle zone between top and bottom confinement zones:
$$a_2 \le \min\left(15 d_{long}, \min(b, h), 200\text{ mm}\right) \quad (\text{typically } a200)$$

#### 4. Beam-Column Joint Confinement ($a_{joint}$)
Ties continuing through the beam-column core:
$$a_{joint} \le 150\text{ mm} \quad (\text{minimum 2 to 3 tie sets})$$

#### 5. Internal Tie Arrangements (`ColumnTieArrangement`)
- **Perimeter Closed Hoop**: Outer rectangular tie enclosing all corner and outer perimeter bars with $135^\circ$ seismic hooks (extension $\ge 10d$ or $75\text{ mm}$).
- **Inner Ties**:
  - If bar distance along face $> 150\text{ mm}$, intermediate bars must have lateral support:
    - **Diamond / Rhombus Hoop**: Connecting midpoints of the 4 faces.
    - **Rectangular Inner Hoop**: Encasing inner bars.
    - **Cross-Ties (Đai C / Cravat)**: $135^\circ - 90^\circ$ hooks alternating in elevation and position.

---

### 2.4 Shear Wall & Core Wall Detailing Models (`HPKata.Core.Walls.Models`)

#### 1. `ShearWallModel`
```csharp
namespace HPKata.Core.Walls.Models
{
    public enum WallShapeType
    {
        Planar_I,
        L_Shape,
        T_Shape,
        C_Shape,
        Box_Core
    }

    public sealed class ShearWallModel
    {
        public string WallMark { get; set; } = "V1";
        public WallShapeType ShapeType { get; set; } = WallShapeType.Planar_I;
        public List<WallStorySegment> Stories { get; } = new();
        public List<WallBoundaryElementModel> BoundaryElements { get; } = new();
        public WallWebReinforcementModel WebReinforcement { get; set; } = new();
        public List<WallOpeningModel> Openings { get; } = new();
    }
}
```

#### 2. `WallBoundaryElementModel` (`ve_loi_vach`)
Boundary elements (cột biên / vùng biên vách) resist boundary compressive/tensile forces:
- **Boundary Length**:
  $$b_{bz} \ge \max(0.15 L_w, 1.5 t_w, 400\text{ mm})$$
- **Boundary Longitudinal Reinforcement**:
  Concentrated vertical bars: typically $6\phi 20$ to $12\phi 25$.
  Minimum reinforcement ratio: $\rho_{bz} \ge 1.0\%$.
- **Boundary Confinement Ties**:
  Closed rectangular hoop + internal cross-ties at spacing $s_{bz} \le \min(t_w/2, 6d_{long}, 100\text{ mm})$.

#### 3. `WallWebReinforcementModel` (Thép thân & lưới vách)
- **Double Curtain (2 lớp thép)**: Mandatory when wall thickness $t_w \ge 150\text{ mm}$.
- **Vertical Distributed Bars**: Diameter $\phi_v \ge 10\text{ mm}$, spacing $s_v \le \min(2 t_w, 200\text{ mm})$ (typically $d10a200 - d14a200$).
- **Horizontal Distributed Bars**: Diameter $\phi_h \ge 10\text{ mm}$, spacing $s_h \le 200\text{ mm}$. Placed outside vertical bars for crack resistance or inside for construction convenience.
- **Web Cross-Ties (Đai cravat / Đai C)**: Diameter $\phi_{tie} \ge 8\text{ mm}$, frequency $\ge 4\text{ ties}/m^2$, staggered pattern.
- **Edge Hooks / U-Bars (`dang05` / `dang25`)**:
  U-bars enclosing the ends of horizontal bars at wall free edges, boundary transitions, and opening sides. Lap length $L_{lap} \ge 30d$.

#### 4. `WallOpeningModel` (Lỗ mở vách)
Openings in shear walls require localized crack-control and stress-redistribution rebar:
- **Diagonal Trimmer Bars (Thép chéo chống nứt góc lỗ mở)**:
  $2 \times \phi 14$ to $2 \times \phi 18$ placed diagonally at $45^\circ$ across each of the 4 opening corners, length $L_{diag} \ge 2 \cdot L_{anc} \approx 800 - 1000\text{ mm}$.
- **Jamb Extra Vertical Bars (Thép tăng cường đứng mép lỗ mở)**:
  $2 \times \phi_{jamb}$ along each vertical side, anchored past the opening top/bottom by $L_{anc}$.
- **Header & Sill Extra Horizontal Bars (Thép tăng cường ngang trên/dưới lỗ mở)**:
  $2 \times \phi_{header}$ above and below the opening, extending $L_{anc} \ge 40d$ past the opening jambs.

---

### 2.5 ETABS Column & Pier/Wall Import Service (`loadcolumn`, `loadwall`)

#### 1. Column Import (`EtabsColumnImportService`)
Parses ETABS column design tables (copied from ETABS table export or clipboard):
- **Columns in Table**: Story, Column Mark, Dimensions $b \times h$, Axial Force $P$, Moments $M_2, M_3$, Required Longitudinal Reinforcement $A_{st}$, Shear $A_{v2}/s, A_{v3}/s$.
- **Auto-Sizing Algorithm (`TcvnColumnRebarOptimizer`)**:
  - Verifies $A_{st,prov} \ge A_{st,req}$ and $0.6\% \le \rho \le 3.5\%$.
  - Selects corner bar diameter $d_c \in \{18, 20, 22, 25, 28, 32\}$.
  - Determines side bar counts $n_b, n_h$ to maintain aspect ratio and clear spacing $s_{clear} \ge 50\text{ mm}$.
  - Selects tie diameter and dense/sparse spacing to satisfy $A_v/s$.

#### 2. Wall Pier Import (`EtabsWallImportService`)
Parses ETABS wall pier design tables:
- **Columns in Table**: Story, Pier Mark, Thickness $t_w$, Length $L_w$, Axial $P$, Shear $V_2$, Moment $M_3$, Required Boundary Area $A_{sb}$, Web Shear $A_{vh}/s$.
- **Auto-Mapping Algorithm**:
  - Calculates boundary length $b_{bz} = \max(0.15 L_w, 400\text{ mm})$.
  - Sizes boundary vertical bars to provide $A_{prov} \ge A_{sb}$.
  - Selects web horizontal bar spacing $s_h$ to provide $A_{vh}/s \ge A_{req}/s$.

---

### 2.6 Persistence & Schedule Aggregation (`HPKata.Core.Columns.Persistence`)

#### 1. Schedule Aggregation
`ColumnScheduleAggregator` and `WallScheduleAggregator` aggregate detailing data into standard Vietnamese structural schedules:
- Grouped by Story (Tầng) and Component Mark (Mã cấu kiện).
- Bar schedules with Shape Code (`dang00` to `dang67`), cut lengths, hooks, diameters, and total weight.
- TCVN summary bins: $\phi \le 10\text{ mm}$, $10 < \phi \le 18\text{ mm}$, $\phi > 18\text{ mm}$.

#### 2. ClosedXML Workbook Generator (`ColumnWallWorkbookGenerator`)
Generates production `.xlsx` workbooks adhering to `Kata.xlsm` layout:
- Sheet `Cot`: Multi-story column dimensions, bar counts, tie distributions, and schedule tables.
- Sheet `Vach`: Shear wall elevation parameters, boundary element rebar, web mesh, and schedules.

---

## 3. HPKata.UI: WPF ViewModels, Windows & Themes

### 3.1 `ColumnDetailingViewModel` & `ColumnDetailingWindow`

#### ViewModel Architecture
```csharp
namespace HPKata.UI.ViewModels
{
    public sealed partial class ColumnDetailingViewModel : ObservableObject
    {
        public ObservableCollection<ColumnStoryRowViewModel> Stories { get; } = new();

        [ObservableProperty] private string _columnMark = "C1";
        [ObservableProperty] private string _concreteGrade = "B25";
        [ObservableProperty] private string _mainSteelGrade = "CB400-V";
        [ObservableProperty] private string _tieSteelGrade = "CB240-T";
        [ObservableProperty] private double _concreteCoverMm = 30.0;
        
        // Splice & Crank Settings
        [ObservableProperty] private bool _isShopMode;
        [ObservableProperty] private string _spliceMethod = "LapSplice"; // LapSplice, Coupler
        [ObservableProperty] private double _lapLengthFactor = 35.0;     // 35d
        [ObservableProperty] private double _staggerOffsetMm = 600.0;
        [ObservableProperty] private double _maxCrankStepDownMm = 50.0;
        
        // Section Rebar Configuration
        [ObservableProperty] private int _cornerDiameter = 20;
        [ObservableProperty] private int _sideCountB = 1;
        [ObservableProperty] private int _sideDiameterB = 18;
        [ObservableProperty] private int _sideCountH = 2;
        [ObservableProperty] private int _sideDiameterH = 18;
        
        // Tie Configuration
        [ObservableProperty] private int _tieDiameter = 8;
        [ObservableProperty] private double _denseSpacing = 100.0;
        [ObservableProperty] private double _sparseSpacing = 200.0;
        [ObservableProperty] private string _innerHoopType = "CrossTie"; // None, Rectangular, Diamond, CrossTie

        public ColumnRebarLayout? ResultLayout { get; private set; }

        [RelayCommand] private void AddStory() { ... }
        [RelayCommand] private void RemoveStory() { ... }
        [RelayCommand] private void AutoSizeBars() { ... }
        [RelayCommand] private void Apply() { ... }
    }
}
```

#### XAML Window Design (`ColumnDetailingWindow.xaml`)
- Standard dimensions: `Height="680" Width="960" MinHeight="550" MinWidth="800"`.
- Resource dictionaries: `Theme.xaml` (`DynamicResource Brush.Background`, `DynamicResource Brush.Text`, `DynamicResource Card.Panel`, `Text.Title`, `Button.Primary`).
- **Layout Panels**:
  1. **Header**: Title, Column Mark, Story Range, Shop Mode Toggle, Concrete/Steel Grades.
  2. **Left Panel (55% width)**: Multi-story data grid:
     - Columns: `Tầng`, `Chiều cao H (mm)`, `Dầm hd (mm)`, `Kích thước b x h (mm)`, `Thép góc`, `Thép cạnh b`, `Thép cạnh h`, `Đai a1/a2`.
     - Action buttons: `+ Thêm tầng`, `- Bớt tầng`, `Tối ưu hóa cốt thép`.
  3. **Right Panel (45% width)**:
     - Card 1: Section parameters & live schematic preview.
     - Card 2: Splice & Transition configuration (Lap splice factor, Staggering distance, Crank slope $1:6$).
     - Card 3: Tie pattern selector (Perimeter hoop, Diamond hoop, C-ties).
  4. **Footer**: `Tạo bản vẽ chi tiết (OK)` and `Hủy bỏ (Cancel)`.

---

### 3.2 `WallDetailingViewModel` & `WallDetailingWindow`

#### ViewModel Architecture
- `WallMark`: "V1", "SW1".
- `ShapeType`: Planar, L-Shape, T-Shape, C-Core, Box-Core.
- `BoundaryElements`: Length $b_{bz}$, bar count, bar diameter, tie diameter, tie spacing.
- `WebMesh`: Double-curtain vertical rebar diameter/spacing, horizontal rebar diameter/spacing, cross-tie frequency, U-bar edge hook toggle.
- `Openings`: Collection of `WallOpeningViewModel` (Mark, $X, Y, W, H$, diagonal rebar toggle).
- `ApplyCommand`: Generates `ShearWallModel`.

#### XAML Window Design (`WallDetailingWindow.xaml`)
- Multi-tab or split view:
  - Tab 1: Wall Stories & Elevation Dimensions ($L_w, t_w, H_{story}$).
  - Tab 2: Boundary Elements (`ve_loi_vach`) configuration.
  - Tab 3: Web Mesh & Edge Hooks.
  - Tab 4: Openings & Trimmer Rebar.

---

## 4. HPKata.AutoCad: Commands & Drafting Controllers

### 4.1 AutoCAD Command Inventory (10 Commands)

| # | Command Full Name | Lisp Alias | Purpose | Controller Method |
|---|---|---|---|---|
| 1 | `ve_cot` | `vc` | Draw interactive column elevation & section detail | `ColumnDrawController.DrawColumnInteractive()` |
| 2 | `ve_cot_shop` | `vcs` | Draw column shop drawings with 11.7m commercial bar division and staggered lap splices | `ColumnShopController.DrawColumnShopInteractive()` |
| 3 | `raicot` | `raicot` | Place column block markers at structural grid intersections | `ColumnPlacementController.PlaceColumnBlocksInteractive()` |
| 4 | `dimcot` | `dimcot` | Automatically dimension column blocks along grid axes | `ColumnDimensionController.DimColumnGridsInteractive()` |
| 5 | `tkcot` | `tkcot` | Column and shear wall block quantity takeoff & schedule table generation | `ColumnTakeoffController.TakeoffColumnsInteractive()` |
| 6 | `loadcolumn` | `loadcolumn` | Import column design parameters from Excel (`Cot`) or ETABS | `ColumnEtabsController.LoadColumnInteractive()` |
| 7 | `ve_vach` | `vv` | Draw shear wall elevation, boundary elements, and web mesh | `WallDrawController.DrawWallInteractive()` |
| 8 | `ve_vach_shop` | `vvs` | Draw shear wall shop drawings with 11.7m curtailment & U-bars | `WallShopController.DrawWallShopInteractive()` |
| 9 | `ve_loi_vach` | `vlv` | Draw elevator core wall details (U, C, and closed box cores) | `WallCoreController.DrawWallCoreInteractive()` |
| 10 | `loadwall` | `loadwall` | Import shear wall & pier design parameters from Excel (`Vach`) or ETABS | `WallEtabsController.LoadWallInteractive()` |

### 4.2 Controller Architectures

#### 1. `ColumnDrawController` / `ColumnShopController`
- Prompts user for insertion point `ed.GetPoint()`.
- Generates outer concrete outline (vertical lines for column edges, horizontal lines for floor levels and beam soffits).
- Draws longitudinal rebars as polylines on layer `KATA_THEP_DOC` with color-by-diameter:
  - Cranked offset polylines ($1:6$ slope) at story transitions where $\Delta \le 50\text{ mm}$.
  - Straight cutoff lines + separate starter dowels where $\Delta > 50\text{ mm}$.
  - Staggered lap splice runs in shop mode (`ve_cot_shop`).
- Draws perimeter ties and cross-ties on layer `KATA_THEP_DAI` at dense spacing ($a_1$) in $l_0$ zones and sparse spacing ($a_2$) in mid-height zones.
- Generates cross-section detail cuts beside the elevation.

#### 2. `WallDrawController` / `WallCoreController`
- Drafts wall elevation outline.
- Drafts boundary element cages (`ve_loi_vach`) with concentrated vertical bars and confinement ties.
- Drafts web mesh: double-curtain vertical bars and horizontal bars with U-bar end caps (`dang05`).
- Drafts opening cutouts with $45^\circ$ diagonal corner crack-control bars.

---

## 5. HPKata.Revit: Commands & 3D Rebar Services

### 5.1 Revit Command Entry Points
1. **`ColumnRebarCommands`**:
   - `[Transaction(TransactionMode.Manual)]`, `[Regeneration(RegenerationOption.Manual)]`.
   - Prompts user to select a `Structural Column` (`OST_StructuralColumns`) or multi-story column stack.
   - Launches `ColumnDetailingWindow` (modal via `RevitWindowHelper.ShowModal`).
   - On commit, dispatches `ColumnRebarGenerator.GenerateColumnRebar(doc, column, layout)`.
2. **`WallRebarCommands`**:
   - Prompts user to select a structural `Wall` (`OST_Walls`).
   - Launches `WallDetailingWindow`.
   - On commit, dispatches `WallRebarGenerator.GenerateWallRebar(doc, wall, layout)`.
3. **`EtabsColumnWallImportCommand`**:
   - Imports ETABS column/wall tables, resolves Revit levels and column/wall instances, and generates 3D rebar.

### 5.2 3D Rebar Generation Mechanics (`ColumnRebarGenerator`)
- **Coordinate Transformation**:
  Transforms column local axes (centerline $Z$, cross-section $X, Y$) to Revit world coordinates.
- **Bar Type Resolution**:
  Maps diameter in mm to project `RebarBarType` ($\pm 0.5\text{ mm}$).
- **Longitudinal Bar Generation**:
  - Uses `Rebar.CreateFromCurves` with vertical curves.
  - When crank is required: creates a 3-segment polyline curve (lower vertical, crank sloped segment $1:6$, upper vertical).
  - When hook is required: sets `RebarHookType` (Standard $90^\circ$).
- **Tie Generation**:
  - Creates closed perimeter hoop curve array at $(Z_i)$ elevations.
  - Creates diamond hoop or cross-tie curves at intermediate bar locations.
  - Sets layout rule: `RebarLayoutRule.MaximumSpacing` or explicit curve arrays for dense ($l_0$) and sparse zones.
- **Idempotency & Tagging**:
  - Rebars are tagged: `Comments` = `"HPKata_Column_{Mark}"`.
  - Re-running the command automatically locates and deletes previous HPKata rebars on that host column before creating the new set.

---

## 6. Comprehensive Test Plan & Acceptance Matrix

### 6.1 Test Suite Breakdown

| Test Class | Project | Test Count | Key Test Scenarios |
|---|---|---|---|
| `ColumnModelTests.cs` | `HPKata.Core.Tests` | 6 | `ColumnStorySegment` validation, `ColumnSectionModel` coordinate calculations, clear spacing checks, total steel area, steel percentage $\rho$. |
| `ColumnCrankBendCalculatorTests.cs` | `HPKata.Core.Tests` | 5 | $\Delta = 0$ straight; $\Delta \le 50\text{ mm}$ crank bend ($1:6$ slope, length $6\Delta$); $\Delta > 50\text{ mm}$ cutoff & starter dowels; negative $\Delta$ handling. |
| `ColumnSpliceCalculatorTests.cs` | `HPKata.Core.Tests` | 5 | Lap splice length ($35d, 45d$); staggered splice elevations ($1.3L_{lap}$, $600\text{ mm}$); 11.7m commercial bar division with 2 stories per bar run. |
| `ColumnConfinementCalculatorTests.cs` | `HPKata.Core.Tests` | 5 | $l_0 = \max(b, h, H_n/6, 500)$; dense spacing $a_1 \le 100\text{ mm}$; sparse spacing $a_2 \le 200\text{ mm}$; tie count estimations. |
| `TcvnColumnRebarOptimizerTests.cs` | `HPKata.Core.Tests` | 4 | Optimizes longitudinal bars from required $A_{st}$; ensures $\rho_{min} = 0.6\%$; symmetric bar arrangement along $b$ and $h$. |
| `EtabsColumnImportTests.cs` | `HPKata.Core.Tests` | 4 | Parses delimited ETABS column design table; maps column mark, story, dimensions, and design forces; generates valid `ColumnRebarLayout`. |
| `ShearWallModelTests.cs` | `HPKata.Core.Tests` | 5 | Planar wall, C-shape core, box core models; boundary element dimensions; double-curtain web reinforcement verification. |
| `WallBoundaryCalculatorTests.cs` | `HPKata.Core.Tests` | 4 | Boundary length $b_{bz} \ge \max(0.15L_w, 400)$; boundary vertical bar layout; boundary confinement tie spacing $\le 100\text{ mm}$. |
| `WallWebMeshCalculatorTests.cs` | `HPKata.Core.Tests` | 4 | Vertical and horizontal mesh arrays; cross-tie distribution ($4\text{ ties}/m^2$); U-bar end closure lengths ($30d$). |
| `WallOpeningTrimmerTests.cs` | `HPKata.Core.Tests` | 4 | $45^\circ$ diagonal corner trimmer bars ($2\phi 14$ - $2\phi 18$); jamb vertical extra bars; header/sill horizontal extra bars. |
| `EtabsWallImportTests.cs` | `HPKata.Core.Tests` | 4 | Parses ETABS pier design table; maps pier dimensions, axial/shear forces, and boundary required areas. |
| `ColumnWallPersistenceTests.cs` | `HPKata.Core.Tests` | 4 | Column & wall schedule aggregation; ClosedXML workbook generation for sheets `Cot` and `Vach`. |
| `ColumnDetailingViewModelTests.cs` | `HPKata.UI.Tests` | 4 | Story addition/removal; property change notifications; auto-size command; layout generation on apply. |
| `WallDetailingViewModelTests.cs` | `HPKata.UI.Tests` | 4 | Wall parameters; opening additions; boundary element toggles; layout generation on apply. |
| `ColumnControllerTests.cs` | `HPKata.AutoCad.Tests` | 4 | Column takeoff registry; layout serialization to DWG dictionary; command method dispatch checks. |
| `WallControllerTests.cs` | `HPKata.AutoCad.Tests` | 4 | Wall takeoff registry; elevator core parameters; command method dispatch checks. |
| `Tier4_Scenario2_MultiStoryColumnTests.cs` | `HPKata.E2E.Tests` | 2 | End-to-end 5-story column detailing ($800\times 800 \to 700\times 700 \to 600\times 600$), crank bends, staggered splices, cutting stock, schedule export. |
| `Tier4_Scenario3_CoreWallTests.cs` | `HPKata.E2E.Tests` | 2 | End-to-end elevator core wall detailing, boundary elements (`ve_loi_vach`), double-curtain mesh, door opening trimmers, schedule export. |

**Total Estimated New Tests**: $\approx 71$ tests, boosting test suite from 437 passing tests to $> 500$ passing tests.

---

## 7. Clean Code Governance & File Partitioning Blueprint

Every new class is strictly partitioned to guarantee $\le 300$ physical lines per file and $\le 50$ physical lines per method, with zero dead code and zero unannotated catch blocks.

### 7.1 Proposed Class & File Manifest

#### 1. `HPKata.Core`
```
HPKata/HPKata.Core/
├── Columns/
│   ├── Calculators/
│   │   ├── ColumnConfinementCalculator.cs       (~110 lines)
│   │   ├── ColumnCrankBendCalculator.cs          (~80 lines)
│   │   ├── ColumnSpliceCalculator.cs            (~130 lines)
│   │   └── TcvnColumnRebarOptimizer.cs          (~140 lines)
│   ├── Etabs/
│   │   ├── EtabsColumnDesign.cs                 (~70 lines)
│   │   ├── EtabsColumnImportService.cs          (~130 lines)
│   │   └── EtabsColumnTableMapper.cs            (~110 lines)
│   ├── Models/
│   │   ├── ColumnBarRunModel.cs                 (~80 lines)
│   │   ├── ColumnRebarLayout.cs                 (~70 lines)
│   │   ├── ColumnSectionModel.cs                (~120 lines)
│   │   ├── ColumnStarterDowelModel.cs           (~60 lines)
│   │   ├── ColumnStorySegment.cs                (~80 lines)
│   │   └── ColumnTieDistributionModel.cs        (~90 lines)
│   └── Persistence/
│       ├── ColumnPlanScheduleWorkbookGenerator.cs (~150 lines)
│       └── ColumnScheduleAggregator.cs          (~120 lines)
│
└── Walls/
    ├── Calculators/
    │   ├── WallBoundaryCalculator.cs            (~120 lines)
    │   ├── WallOpeningTrimmerCalculator.cs      (~110 lines)
    │   └── WallWebMeshCalculator.cs             (~130 lines)
    ├── Etabs/
    │   ├── EtabsPierDesign.cs                   (~70 lines)
    │   ├── EtabsWallImportService.cs            (~130 lines)
    │   └── EtabsWallTableMapper.cs              (~110 lines)
    ├── Models/
    │   ├── ShearWallModel.cs                    (~80 lines)
    │   ├── WallBoundaryElementModel.cs          (~80 lines)
    │   ├── WallOpeningModel.cs                  (~70 lines)
    │   ├── WallStorySegment.cs                  (~70 lines)
    │   └── WallWebReinforcementModel.cs         (~80 lines)
    └── Persistence/
        ├── WallPlanScheduleWorkbookGenerator.cs   (~140 lines)
        └── WallScheduleAggregator.cs            (~110 lines)
```

#### 2. `HPKata.UI`
```
HPKata/HPKata.UI/
├── ViewModels/
│   ├── ColumnDetailingViewModel.cs              (~180 lines)
│   ├── ColumnStoryRowViewModel.cs               (~70 lines)
│   ├── EtabsColumnWallImportViewModel.cs        (~140 lines)
│   ├── WallDetailingViewModel.cs                (~180 lines)
│   └── WallOpeningViewModel.cs                  (~60 lines)
└── Views/
    ├── ColumnDetailingWindow.xaml               (~220 lines)
    ├── ColumnDetailingWindow.xaml.cs            (~40 lines)
    ├── WallDetailingWindow.xaml                 (~220 lines)
    └── WallDetailingWindow.xaml.cs              (~40 lines)
```

#### 3. `HPKata.AutoCad`
```
HPKata/HPKata.AutoCad/
├── Commands/
│   ├── ColumnCommands.cs                        (~60 lines)
│   └── WallCommands.cs                          (~60 lines)
└── Controllers/
    ├── ColumnDimensionController.cs             (~120 lines)
    ├── ColumnDrawController.cs                  (~160 lines)
    ├── ColumnEtabsController.cs                 (~110 lines)
    ├── ColumnPlacementController.cs             (~130 lines)
    ├── ColumnShopController.cs                  (~150 lines)
    ├── ColumnTakeoffController.cs               (~120 lines)
    ├── WallCoreController.cs                    (~140 lines)
    ├── WallDrawController.cs                    (~160 lines)
    ├── WallEtabsController.cs                   (~110 lines)
    └── WallShopController.cs                    (~150 lines)
```

#### 4. `HPKata.Revit`
```
HPKata/HPKata.Revit/
├── Commands/
│   ├── ColumnRebarCommands.cs                   (~80 lines)
│   ├── EtabsColumnWallImportCommand.cs          (~80 lines)
│   └── WallRebarCommands.cs                     (~80 lines)
└── Services/
    ├── ColumnRebarGenerator.cs                  (~180 lines)
    ├── ColumnWallScheduleService.cs             (~130 lines)
    └── WallRebarGenerator.cs                    (~180 lines)
```

---

## 8. Definition of Done & Verification Strategy

1. **Build Verification**:
   - `dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1` builds with `0 warnings` and `0 errors`.
   - No project has circular dependencies; `HPKata.Core` has ZERO AutoCAD or Revit references.
2. **Clean Code Verification**:
   - Every `.cs` file $\le 300$ physical lines.
   - Every method $\le 50$ physical lines.
   - 0 dead code, 0 unannotated catch blocks.
3. **Automated Test Suite**:
   - `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1` runs 100% green without `FailFast` or `Unknown Hard Error` crashes.
4. **CAD & Revit Interop Verification**:
   - All 10 AutoCAD commands registered and callable via aliases (`vc`, `vcs`, `raicot`, `dimcot`, `tkcot`, `loadcolumn`, `vv`, `vvs`, `vlv`, `loadwall`).
   - Revit external commands create valid 3D `Rebar` elements with proper parameters on `OST_StructuralColumns` and `OST_Walls`.

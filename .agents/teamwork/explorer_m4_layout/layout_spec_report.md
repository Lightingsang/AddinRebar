# MASTER SPECIFICATION & CLEAN CODE ARCHITECTURE
## Drawing Layout, Grid & Viewport Subsystem (Milestone M4 / Phase 5)

- **Document ID**: `HPKATA-SPEC-M4-LAYOUT-2026`
- **Version**: `1.0.0-PROD`
- **Author**: `explorer_m4_layout` (Drawing Layout, Grid & Viewport Domain Explorer)
- **Target Deliverable**: `HPKata/HPKata.slnx` (.NET 8 SDK 10.0.300)
- **Status**: AUTHORITATIVE SPECIFICATION & DESIGN BLUEPRINT
- **Governance**: Clean-Room Reverse Engineering Protocol, Full Compliance with `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/autocad-civil.md`, `docs/clean-code/host-appendix/revit.md`

---

## 1. Architectural Overview & Scope Boundary

### 1.1 Mission & Scope
The **Drawing Layout, Grid & Viewport Subsystem** fulfills the drafting automation, grid coordinate system generation, sheet arrangement (bin-packing), layout viewport creation, and drawing register indexing requirements for **Milestone M4 (Phase 5: Quantity Surveying & Drawing Utilities)** of the HPKata ecosystem.

The subsystem bridges standard Vietnamese structural and civil engineering drafting practices (TCVN 5571:2012, TCVN 5574:2018, ISO 128 / ISO 5457) across two CAD/BIM host environments:
1. **Autodesk AutoCAD 2026 (.NET 8.0)**: Interactive and automated drawing commands (`vg`, `sbv`, `ibv`, `taomv`, `taomv1`, `khoav`, `mokhoav`, `danhmuc`).
2. **Autodesk Revit 2026 (.NET 8.0)**: Native `ViewSheet` generation, TitleBlock symbol mapping, collision-free `Viewport.Create` arrangement, and drawing index scheduling.

### 1.2 Architectural Layering & Isolation
In accordance with the repository Clean Code Governance (Rules D1, D4):
- **`HPKata.Core/DrawingLayout/`**: 100% pure C# domain library targeting `netstandard2.0;net8.0`. Contains **ZERO** references to `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`. Operates strictly in millimeter metric units.
- **`HPKata.UI/`**: WPF MVVM presentation library targeting `net8.0-windows` using `CommunityToolkit.Mvvm` and `MahApps.Metro`.
- **`HPKata.AutoCad/`**: AutoCAD 2026 add-in targeting `net8.0-windows` consuming AutoCAD.NET 25.1.0 and bridging Core layout calculators to ModelSpace/PaperSpace entities.
- **`HPKata.Revit/`**: Revit 2026 add-in targeting `net8.0-windows` consuming RevitAPI 2026 and bridging Core layout calculators to `ViewSheet` and `Viewport` elements.

```
+-----------------------------------------------------------------------------------+
|                                  HPKata.UI                                        |
|  DrawingUtilityViewModel  |  DrawingUtilityWindow  |  Theme-aware (Dark/Light)    |
+-----------------------------------------------------------------------------------+
           ▲                                                         ▲
           │ (Binds to)                                              │ (Binds to)
+----------┴------------------------------------+  +-----------------┴--------------+
|             HPKata.AutoCad                    |  |           HPKata.Revit         |
|  - GridDrawController (vg / ve_grid)          |  |  - RevitSheetArrangementService|
|  - SheetLayoutController (sbv / ibv)          |  |  - RevitSheetCommands          |
|  - ViewportController (taomv / khoav)         |  |  - RevitUnitConverter (Rule R5)|
|  - DrawingIndexController (danhmuc)           |  |                                |
|  - DrawingUtilityCommands.cs                  |  |                                |
+-----------------------------------------------+  +--------------------------------+
           │                                                         │
           ▼ (Pure C# Domain DTOs & Calculators)                     ▼
+-----------------------------------------------------------------------------------+
|                             HPKata.Core/DrawingLayout                             |
|  Models:                                                                          |
|    - GridLineModel, GridSystemModel, GridBubbleOrientation                        |
|    - SheetSizeDefinition, SheetFormat, TitleBlockModel, DrawingBlockItem          |
|    - DrawingIndexItem, DrawingIndexTableDto                                       |
|  Calculators:                                                                     |
|    - GridAxisGenerator (Orthogonal X/Y, Skewed, Radial, Labels, Dim strings)      |
|    - SheetArrangerCalculator (2D shelf/bin packing, margin/gutter/titleblock)     |
|    - DrawingIndexAggregator (Natural sorting, gap check, table DTO)           |
+-----------------------------------------------------------------------------------+
```

### 1.3 Strict Clean Code Governance Rules
Every new file and method must strictly conform to:
1. **Rule C2**: File length $\le 300$ physical lines.
2. **Rule M3**: Method length $\le 50$ physical lines.
3. **Rule Q-B1**: Zero dead code, zero commented-out code, zero `NotImplementedException` stubs.
4. **Rule Q-B2**: Zero unannotated empty catch blocks.
5. **Rule R5**: Literal `304.8` must appear **strictly and exclusively** inside `RevitUnitConverter.cs`.
6. **Host Neutrality**: Exactly 0 `using Autodesk.*` references in `HPKata.Core`.

---

## 2. Mathematical & Algorithmic Foundations

### 2.1 Grid Axis Coordinate Geometry & Bounding Box Equations
Given a set of $n$ vertical grid lines ($Y$-axes) at $X$-coordinates $x_0 < x_1 < \dots < x_{n-1}$ and $m$ horizontal grid lines ($X$-axes) at $Y$-coordinates $y_0 < y_1 < \dots < y_{m-1}$:

1. **Intersection Bounding Box**:
   $$X_{\min} = x_0, \quad X_{\max} = x_{n-1}, \quad Y_{\min} = y_0, \quad Y_{\max} = y_{m-1}$$

2. **Axis Extension Offsets**:
   Grid lines must project beyond the perimeter intersections by an extension length $E_{\text{ext}}$ to accommodate bubble callouts and dimension chains:
   $$E_{\text{ext}} = 15.0\text{ mm} \times S_{\text{dwg}}$$
   *(For drawing scale $S_{\text{dwg}} = 100$ (i.e. $1:100$), $E_{\text{ext}} = 1,500\text{ mm}$; for $S_{\text{dwg}} = 50$, $E_{\text{ext}} = 750\text{ mm}$)*.

3. **Coordinate Endpoints**:
   - Vertical axis $i$ at $X = x_i$:
     $$P_{\text{start}, i} = (x_i, Y_{\min} - E_{\text{ext}}, 0), \quad P_{\text{end}, i} = (x_i, Y_{\max} + E_{\text{ext}}, 0)$$
   - Horizontal axis $j$ at $Y = y_j$:
     $$P_{\text{start}, j} = (X_{\min} - E_{\text{ext}}, y_j, 0), \quad P_{\text{end}, j} = (X_{\max} + E_{\text{ext}}, y_j, 0)$$

4. **Bubble Circles**:
   - Bubble radius $R_{\text{bubble}} = 4.5\text{ mm} \times S_{\text{dwg}}$ ($450\text{ mm}$ at $1:100$).
   - Text height $H_{\text{text}} = 3.5\text{ mm} \times S_{\text{dwg}}$ ($350\text{ mm}$ at $1:100$).
   - Bubble centers:
     $$C_{\text{bottom}, i} = (x_i, Y_{\min} - E_{\text{ext}} - R_{\text{bubble}}, 0)$$
     $$C_{\text{top}, i} = (x_i, Y_{\max} + E_{\text{ext}} + R_{\text{bubble}}, 0)$$
     $$C_{\text{left}, j} = (X_{\min} - E_{\text{ext}} - R_{\text{bubble}}, y_j, 0)$$
     $$C_{\text{right}, j} = (X_{\max} + E_{\text{ext}} + R_{\text{bubble}}, y_j, 0)$$

5. **Continuous Dimension Strings**:
   - Distance from bubble edge to span dimension line $D_{\text{dim1}} = 10.0\text{ mm} \times S_{\text{dwg}}$ ($1,000\text{ mm}$ at $1:100$).
   - Distance between span dimension and overall total dimension $D_{\text{dim2}} = 8.0\text{ mm} \times S_{\text{dwg}}$ ($800\text{ mm}$ at $1:100$).
   - Span-to-span dimension chain measures segment intervals:
     $$\Delta x_i = x_{i+1} - x_i \quad (0 \le i < n-1), \quad \Delta y_j = y_{j+1} - y_j \quad (0 \le j < m-1)$$
   - Overall dimension measures total span:
     $$L_{\text{total}, X} = X_{\max} - X_{\min}, \quad L_{\text{total}, Y} = Y_{\max} - Y_{\min}$$

6. **Skewed / Angular Grid Axis Formulation**:
   For an axis oriented at arbitrary angle $\theta \in [0, 2\pi)$ with normal $\vec{n} = (-\sin\theta, \cos\theta)$ and unit direction $\vec{u} = (\cos\theta, \sin\theta)$:
   $$P(t) = P_0 + k \cdot d \cdot \vec{n} + t \cdot \vec{u}$$
   where $d$ is the perpendicular spacing step and $k \in \mathbb{Z}$ is the axis index.

7. **Radial / Polar Grid Axis Formulation**:
   For a radial grid centered at origin $C(x_c, y_c)$:
   - Radial ray at angle $\phi_k$:
     $$P_{\text{ray}}(\rho) = (x_c + \rho\cos\phi_k, \; y_c + \rho\sin\phi_k, \; 0) \quad \text{for } \rho \in [R_{\min} - E_{\text{ext}}, R_{\max} + E_{\text{ext}}]$$
   - Concentric arc rings at radii $R_1, R_2, \dots, R_p$ spanning between $\phi_{\min}$ and $\phi_{\max}$.

### 2.2 Sheet Packing & 2D Arranger Formulation
Let $S$ be a sheet of format $F$ with paper dimensions $(W_{\text{paper}}, H_{\text{paper}})$ in millimeters.

1. **Printable Area Bounds**:
   Taking binding margin $M_{\text{left}}$ (default $25\text{ mm}$) and border margins $M_{\text{top}}, M_{\text{right}}, M_{\text{bottom}}$ (default $10\text{ mm}$):
   $$W_{\text{printable}} = W_{\text{paper}} - M_{\text{left}} - M_{\text{right}}$$
   $$H_{\text{printable}} = H_{\text{paper}} - M_{\text{top}} - M_{\text{bottom}}$$
   Printable domain $\Omega_{\text{print}} = [M_{\text{left}}, W_{\text{paper}} - M_{\text{right}}] \times [M_{\text{bottom}}, H_{\text{paper}} - M_{\text{top}}]$.

2. **Title Block Exclusion Zone**:
   The standard corner title block occupies the bottom-right rectangle:
   $$\Omega_{\text{tb}} = [W_{\text{paper}} - M_{\text{right}} - W_{\text{tb}}, \; W_{\text{paper}} - M_{\text{right}}] \times [M_{\text{bottom}}, \; M_{\text{bottom}} + H_{\text{tb}}]$$
   where typical dimensions are $W_{\text{tb}} = 160.0\text{ mm}$, $H_{\text{tb}} = 60.0\text{ mm}$.
   No drawing detail block may intersect $\Omega_{\text{tb}}$:
   $$\forall B_k \in \text{PlacedBlocks}: \quad \text{Rect}(B_k) \cap \Omega_{\text{tb}} = \emptyset$$

3. **Shelf Packing Algorithm (Row-Oriented Bin Packing)**:
   Structural engineering details (beam elevations, column cross-sections, schedules) require aligned horizontal baselines. The shelf algorithm organizes placement as follows:
   - Sort input blocks $B = \{b_1, b_2, \dots, b_N\}$ in descending order of height:
     $$h(b_1) \ge h(b_2) \ge \dots \ge h(b_N)$$
   - Maintain active sheet list $\mathcal{S} = [S_1]$.
   - For each block $b_i$ with dimensions $(w_i, h_i)$:
     1. Search existing shelves on the active sheet from bottom to top. A shelf at baseline $Y_{\text{shelf}}$ with height $H_{\text{shelf}} \ge h_i$ and current cursor $X_{\text{cursor}}$ can accommodate $b_i$ if:
        $$X_{\text{cursor}} + w_i + G_x \le W_{\text{paper}} - M_{\text{right}}$$
        and if the rectangle $[X_{\text{cursor}}, X_{\text{cursor}} + w_i] \times [Y_{\text{shelf}}, Y_{\text{shelf}} + h_i]$ does not intersect $\Omega_{\text{tb}}$.
     2. If accommodated, place $b_i$ at $(X_{\text{cursor}}, Y_{\text{shelf}})$, advance $X_{\text{cursor}} \leftarrow X_{\text{cursor}} + w_i + G_x$.
     3. If no existing shelf can take $b_i$, open a new shelf above the topmost shelf:
        $$Y_{\text{new}} = Y_{\text{topmost}} + H_{\text{topmost}} + G_y$$
        with height $H_{\text{new}} = h_i$. If $Y_{\text{new}} + h_i \le H_{\text{paper}} - M_{\text{top}}$, add shelf to active sheet and place $b_i$.
     4. If $Y_{\text{new}} + h_i > H_{\text{paper}} - M_{\text{top}}$, current sheet is full. Instantiate new sheet $S_{k+1}$, initialize first shelf at $Y = M_{\text{bottom}}$, and place $b_i$.
   - **Gutter Tolerances**: Horizontal gutter $G_x = 20.0\text{ mm}$, vertical gutter $G_y = 30.0\text{ mm}$ (to provide space for view title annotation strings and underlines).

4. **Sheet Utilization Ratio**:
   $$\eta = \frac{\sum_{k} w(b_k) \cdot h(b_k)}{(W_{\text{printable}} \cdot H_{\text{printable}}) - (W_{\text{tb}} \cdot H_{\text{tb}})} \times 100\%$$

---

## 3. HPKata.Core / DrawingLayout Specification

### 3.1 Folder & File Layout
```
HPKata/HPKata.Core/DrawingLayout/
├── Models/
│   ├── GridLineModel.cs            # Grid line coordinates, direction, label, bubbles
│   ├── GridSystemModel.cs          # Multi-axis grid system container & properties
│   ├── GridBubbleOrientation.cs    # Enum for bubble placement (Start, End, Both, None)
│   ├── SheetSizeDefinition.cs      # ISO A0-A4, extended sizes, margins, printable area
│   ├── SheetFormat.cs              # Enums for SheetFormat and SheetOrientation
│   ├── TitleBlockModel.cs          # Title block metadata, dimensions, attributes
│   ├── DrawingBlockItem.cs         # Detail view bounding box, scale, placement result
│   ├── ArrangedSheetResult.cs      # Multi-sheet arrangement output DTO
│   ├── DrawingIndexItem.cs         # Drawing register row model (number, title, rev, date)
│   └── DrawingIndexTableDto.cs     # Tabular drawing index DTO for CAD & Excel
└── Calculators/
    ├── GridAxisGenerator.cs        # Ortho, skewed, radial grid coordinate math
    ├── SheetArrangerCalculator.cs  # 2D shelf/bin-packing algorithm
    └── DrawingIndexAggregator.cs   # Sheet index sorting, gap check, table builder
```

### 3.2 Exact Model Contracts (`HPKata.Core.DrawingLayout.Models`)

#### 3.2.1 `GridBubbleOrientation.cs`
```csharp
namespace HPKata.Core.DrawingLayout.Models;

public enum GridBubbleOrientation
{
    None = 0,
    StartOnly = 1,
    EndOnly = 2,
    Both = 3
}

public enum GridAxisOrientation
{
    OrthogonalX = 0, // Horizontal grid line (Y constant)
    OrthogonalY = 1, // Vertical grid line (X constant)
    Skewed = 2,      // Angular inclined grid line
    Radial = 3       // Polar radial ray
}
```

#### 3.2.2 `GridLineModel.cs`
```csharp
using HPKata.Core.Models;

namespace HPKata.Core.DrawingLayout.Models;

/// <summary>
/// Immutable specification of a single structural grid axis.
/// Metric units: millimeters (mm).
/// </summary>
public sealed class GridLineModel
{
    public string Label { get; init; } = string.Empty;
    public KataPoint3D StartPointMm { get; init; }
    public KataPoint3D EndPointMm { get; init; }
    public KataVector3D Direction { get; init; }
    public double AngleDeg { get; init; }
    public double LengthMm { get; init; }
    public double ExtensionOffsetStartMm { get; init; } = 1500.0;
    public double ExtensionOffsetEndMm { get; init; } = 1500.0;
    public double BubbleRadiusMm { get; init; } = 450.0;
    public double BubbleTextHeightMm { get; init; } = 350.0;
    public GridBubbleOrientation BubblePlacement { get; init; } = GridBubbleOrientation.Both;
    public GridAxisOrientation Orientation { get; init; } = GridAxisOrientation.OrthogonalY;
    public double CoordinateOffsetMm { get; init; }
}
```

#### 3.2.3 `GridSystemModel.cs`
```csharp
using System.Collections.Generic;
using HPKata.Core.Models;

namespace HPKata.Core.DrawingLayout.Models;

/// <summary>
/// Container model representing a complete multi-axis structural grid coordinate network.
/// </summary>
public sealed class GridSystemModel
{
    public string SystemName { get; init; } = "MainGrid";
    public KataPoint3D OriginMm { get; init; } = new(0, 0, 0);
    public double RotationAngleDeg { get; init; } = 0.0;
    public double DrawingScale { get; init; } = 100.0; // 1:100
    public double DimensionOffsetMm { get; init; } = 1000.0;
    public double OverallDimensionOffsetMm { get; init; } = 800.0;
    public string LayerGrid { get; init; } = "kata_grid";
    public string LayerDim { get; init; } = "kata_dim";
    public string LayerText { get; init; } = "kata_text";
    public List<GridLineModel> XAxes { get; init; } = []; // Horizontal lines
    public List<GridLineModel> YAxes { get; init; } = []; // Vertical lines
    public List<GridLineModel> SkewedAxes { get; init; } = [];
    public List<double> RadialArcRadiiMm { get; init; } = [];
}
```

#### 3.2.4 `SheetFormat.cs` & `SheetSizeDefinition.cs`
```csharp
namespace HPKata.Core.DrawingLayout.Models;

public enum SheetFormat
{
    A0 = 0,
    A1 = 1,
    A2 = 2,
    A3 = 3,
    A4 = 4,
    A1Extended = 5,
    A2Extended = 6,
    A3Extended = 7,
    Custom = 99
}

public enum SheetOrientation
{
    Landscape = 0,
    Portrait = 1
}

public sealed class SheetSizeDefinition
{
    public SheetFormat Format { get; init; } = SheetFormat.A1;
    public SheetOrientation Orientation { get; init; } = SheetOrientation.Landscape;
    public double PaperWidthMm { get; init; } = 841.0;
    public double PaperHeightMm { get; init; } = 594.0;
    public double MarginLeftMm { get; init; } = 25.0;   // Binding margin
    public double MarginTopMm { get; init; } = 10.0;
    public double MarginRightMm { get; init; } = 10.0;
    public double MarginBottomMm { get; init; } = 10.0;

    public double PrintableWidthMm => PaperWidthMm - MarginLeftMm - MarginRightMm;
    public double PrintableHeightMm => PaperHeightMm - MarginTopMm - MarginBottomMm;

    public static SheetSizeDefinition CreateStandard(SheetFormat format, SheetOrientation orientation = SheetOrientation.Landscape)
    {
        var (w, h) = format switch
        {
            SheetFormat.A0 => (1189.0, 841.0),
            SheetFormat.A1 => (841.0, 594.0),
            SheetFormat.A2 => (594.0, 420.0),
            SheetFormat.A3 => (420.0, 297.0),
            SheetFormat.A4 => (297.0, 210.0),
            SheetFormat.A1Extended => (1051.0, 594.0),
            SheetFormat.A2Extended => (804.0, 420.0),
            SheetFormat.A3Extended => (630.0, 297.0),
            _ => (841.0, 594.0)
        };

        if (orientation == SheetOrientation.Portrait)
        {
            (w, h) = (h, w);
        }

        double binding = (format == SheetFormat.A4) ? 20.0 : 25.0;
        double borders = (format == SheetFormat.A3 || format == SheetFormat.A4) ? 5.0 : 10.0;

        return new SheetSizeDefinition
        {
            Format = format,
            Orientation = orientation,
            PaperWidthMm = w,
            PaperHeightMm = h,
            MarginLeftMm = binding,
            MarginTopMm = borders,
            MarginRightMm = borders,
            MarginBottomMm = borders
        };
    }
}
```

#### 3.2.5 `TitleBlockModel.cs`
```csharp
namespace HPKata.Core.DrawingLayout.Models;

public enum TitleBlockPlacement
{
    BottomRightCorner = 0,
    FullHeightRightColumn = 1
}

public sealed class TitleBlockModel
{
    public string BlockName { get; init; } = "khung_ten_A1";
    public TitleBlockPlacement Placement { get; init; } = TitleBlockPlacement.BottomRightCorner;
    public double WidthMm { get; init; } = 160.0;
    public double HeightMm { get; init; } = 60.0;
    public string ProjectName { get; init; } = string.Empty;
    public string BuildingName { get; init; } = string.Empty;
    public string DrawingTitle { get; init; } = string.Empty;
    public string DrawingNumber { get; init; } = "KC-01";
    public string Revision { get; init; } = "00";
    public string Scale { get; init; } = "1/100";
    public string Date { get; init; } = DateTime.Now.ToString("MM/yyyy");
    public string DesignedBy { get; init; } = string.Empty;
    public string CheckedBy { get; init; } = string.Empty;
    public string ApprovedBy { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
}
```

#### 3.2.6 `DrawingBlockItem.cs` & `ArrangedSheetResult.cs`
```csharp
using HPKata.Core.Models;

namespace HPKata.Core.DrawingLayout.Models;

public sealed class DrawingBlockItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = string.Empty;
    public double WidthMm { get; init; }
    public double HeightMm { get; init; }
    public double Scale { get; init; } = 1.0;
    public int Priority { get; init; } = 0;
    public bool AllowRotation { get; init; } = false;
    public KataPoint3D? PlacedPositionMm { get; set; }
    public int PlacedSheetIndex { get; set; } = -1;
}

public sealed class ArrangedSheetResult
{
    public int SheetIndex { get; init; }
    public string SheetNumber { get; init; } = "KC-01";
    public SheetSizeDefinition SheetSize { get; init; } = SheetSizeDefinition.CreateStandard(SheetFormat.A1);
    public TitleBlockModel TitleBlock { get; init; } = new();
    public List<DrawingBlockItem> PlacedBlocks { get; init; } = [];
    public double UtilizationRatio { get; init; }
}
```

#### 3.2.7 `DrawingIndexItem.cs` & `DrawingIndexTableDto.cs`
```csharp
namespace HPKata.Core.DrawingLayout.Models;

public sealed class DrawingIndexItem
{
    public int SequenceNumber { get; init; }
    public string DrawingNumber { get; init; } = string.Empty;
    public string DrawingTitle { get; init; } = string.Empty;
    public string Revision { get; init; } = "00";
    public string Scale { get; init; } = "1/100";
    public string Date { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
    public string LayoutOrSheetName { get; init; } = string.Empty;
    public string DisciplinePrefix { get; init; } = "KC";
}

public sealed class DrawingIndexTableDto
{
    public string TableTitle { get; init; } = "DANH MỤC BẢN VẼ";
    public List<string> Headers { get; init; } = ["STT", "SỐ HIỆU BẢN VẼ", "TÊN BẢN VẼ", "TỶ LỆ", "SỬA ĐỔI", "GHI CHÚ"];
    public List<double> ColumnWidthsMm { get; init; } = [15.0, 40.0, 95.0, 25.0, 25.0, 30.0];
    public List<DrawingIndexItem> Rows { get; init; } = [];
    public double TotalWidthMm => ColumnWidthsMm.Sum();
    public double HeaderRowHeightMm { get; init; } = 10.0;
    public double DataRowHeightMm { get; init; } = 8.0;
}
```

---

### 3.3 Calculator Implementation Specifications (`HPKata.Core.DrawingLayout.Calculators`)

#### 3.3.1 `GridAxisGenerator.cs`
Responsibilities:
1. Generate orthogonal X and Y grid lines from span lists:
   - Method `GenerateOrthogonalSystem(List<double> xSpacings, List<double> ySpacings, double scale = 100.0, string xStartLabel = "1", string yStartLabel = "A")`:
     - Accumulates coordinate points: $x_0 = 0, x_1 = x_0 + \Delta x_0, \dots$
     - Accumulates coordinate points: $y_0 = 0, y_1 = y_0 + \Delta y_0, \dots$
     - Computes bounding box $[X_{\min}, X_{\max}] \times [Y_{\min}, Y_{\max}]$.
     - Extends endpoints by $E_{\text{ext}} = 15.0 \times \text{scale}$.
     - Instantiates `GridLineModel` for each vertical axis ($Y$-axes, numerical labels $1, 2, 3\dots$) and horizontal axis ($X$-axes, alphabetical labels $A, B, C\dots$).
2. Label Generation Engine:
   - Method `GetNextAlphabeticalLabel(int index)`:
     - Implements standard TCVN/ISO 128 character skipping: omits `'I'`, `'O'`, `'Q'` to prevent visual confusion with numerals `1` and `0`.
     - Valid sequence: $A, B, C, D, E, F, G, H, J, K, L, M, N, P, R, S, T, U, V, W, X, Y, Z$.
     - When index $\ge 23$, produces multi-letter notations: $AA, AB, AC\dots$.
3. Skewed / Angular Axis Generator:
   - Method `GenerateSkewedAxis(KataPoint3D origin, double angleDeg, double lengthMm, string label)`:
     - Evaluates $\theta = \text{angleDeg} \times \frac{\pi}{180}$.
     - Computes $P_{\text{end}} = (x_0 + L\cos\theta, y_0 + L\sin\theta, 0)$.
4. Radial Grid Generator:
   - Method `GenerateRadialSystem(KataPoint3D center, List<double> radiiMm, List<double> anglesDeg, double scale = 100.0)`:
     - Generates concentric arc rings at each radius.
     - Generates polar ray lines spanning from $R_{\min} - E_{\text{ext}}$ to $R_{\max} + E_{\text{ext}}$.

#### 3.3.2 `SheetArrangerCalculator.cs`
Responsibilities:
1. Implements 2D Shelf-Packing Algorithm with TitleBlock Collision Avoidance:
   - Method `ArrangeBlocksIntoSheets(List<DrawingBlockItem> blocks, SheetSizeDefinition sheetSize, TitleBlockModel titleBlock, double gutterX = 20.0, double gutterY = 30.0)`:
     - Defines printable boundary:
       $$X_{\text{start}} = M_{\text{left}}, \quad X_{\text{end}} = W_{\text{paper}} - M_{\text{right}}$$
       $$Y_{\text{start}} = M_{\text{bottom}}, \quad Y_{\text{end}} = H_{\text{paper}} - M_{\text{top}}$$
     - Title block forbidden boundary:
       $$X_{\text{tb,min}} = X_{\text{end}} - W_{\text{tb}}, \quad Y_{\text{tb,max}} = Y_{\text{start}} + H_{\text{tb}}$$
     - Sorts blocks descending by height $h_i$.
     - Maintains current sheet list. For each block:
       - Tries existing shelves.
       - If shelf fits within horizontal bounds without colliding with title block, assigns `PlacedPositionMm = new KataPoint3D(currX, currY, 0)`.
       - If shelf full, advances vertically $Y_{\text{shelf}} \leftarrow Y_{\text{shelf}} + H_{\text{prev\_shelf}} + G_y$.
       - If vertical bound exceeded, instantiates new `ArrangedSheetResult` and resets shelf to $Y_{\text{start}}$.
2. Uniform Grid Layout Calculator:
   - Method `ArrangeUniformGrid(List<DrawingBlockItem> blocks, SheetSizeDefinition sheetSize, int columns, int rows)`:
     - Computes uniform cell width $W_{\text{cell}} = \frac{W_{\text{printable}}}{N_{\text{cols}}}$ and height $H_{\text{cell}} = \frac{H_{\text{printable}}}{N_{\text{rows}}}$.
     - Centers each block within its respective grid cell $(c, r)$.

#### 3.3.3 `DrawingIndexAggregator.cs`
Responsibilities:
1. Aggregation and Natural Alphanumeric Sorting:
   - Method `AggregateAndSort(IEnumerable<DrawingIndexItem> items)`:
     - Parses drawing numbers into prefix and numeric component (e.g. `"KC-05"` $\implies$ prefix `"KC"`, number `5`).
     - Sorts primarily by `DisciplinePrefix`, then numerically by integer value. This guarantees natural ordering: `KC-01, KC-02, ..., KC-09, KC-10` (preventing lexicographical sort bug `KC-01, KC-10, KC-02`).
2. Sequence Gap and Duplicate Validation:
   - Method `ValidateSequence(List<DrawingIndexItem> sortedItems, out List<string> errors)`:
     - Flags duplicates: `"[HPKata] Duplicate drawing number detected: KC-03"`.
     - Flags numbering gaps: `"[HPKata] Numbering gap: Missing sheet between KC-02 and KC-04"`.
3. ClosedXML Excel Workbook Generation:
   - Method `ExportToExcelStream(DrawingIndexTableDto tableDto)`:
     - Creates sheet `"DANH_MUC_BAN_VE"` via `ClosedXML.Excel`.
     - Sets header styles (Navy fill, white bold font), borders, auto-fits columns.

---

## 4. HPKata.AutoCad Architecture & Implementation

### 4.1 Controllers Specification (`HPKata.AutoCad.Controllers`)

#### 4.1.1 `GridDrawController.cs`
- **Command Bindings**: `vg`, `ve_grid`, `ve_luoi_truc`.
- **Workflow**:
  1. Prompts user:
     - `GetKeywords("\n[HPKata] Grid generation mode [Orthogonal/Radial/Interactive]: ", "Orthogonal Radial Interactive")`
     - Prompt string for X-spacings: `"Enter X spans (mm, comma-separated) <4000,6000,6000,4000>: "`
     - Prompt string for Y-spacings: `"Enter Y spans (mm, comma-separated) <5000,5000,7000>: "`
     - Prompt for drawing scale: `"Enter scale <100>: "`
  2. Calls `GridAxisGenerator.GenerateOrthogonalSystem(xList, yList, scale)`.
  3. Begins AutoCAD Transaction on active document `BlockTableRecord.ModelSpace`.
  4. Layer Management: Ensures layers exist (`kata_grid` color 8 CENTER, `kata_dim` color 7, `kata_text` color 7).
  5. Entity Drafting:
     - For each `GridLineModel`:
       - Creates `Autodesk.AutoCAD.DatabaseServices.Line` on layer `kata_grid` with linetype `"CENTER"`.
       - If `BubblePlacement` includes Start: creates `Circle` of radius $R_{\text{bubble}}$ at Start and `DBText` centered with `Label` on `kata_text`.
       - If `BubblePlacement` includes End: creates `Circle` of radius $R_{\text{bubble}}$ at End and `DBText` centered with `Label` on `kata_text`.
     - Continuous Dimensions:
       - Creates `RotatedDimension` strings for all individual X-spans and Y-spans.
       - Creates overall `RotatedDimension` strings for total length.
  6. Commits transaction and outputs summary message to `Editor`.

#### 4.1.2 `SheetLayoutController.cs`
- **Command Bindings**:
  - `sbv`, `sap_ban_ve`, `sap_xep_ban_ve`: Batch sheet arrangement.
  - `ibv`, `chen_khung_ten`: Insert titleblock frame.
- **Workflow for `sbv`**:
  1. User selects objects: `Editor.GetSelection()` with prompt: `"\n[HPKata] Select drawing details/blocks to arrange into sheets (sbv): "`
  2. Groups entities by contiguous bounding boxes or block references into a list of `DrawingBlockItem`.
  3. Prompts for target sheet format: `GetKeywords("\nSelect sheet format [A0/A1/A2/A3/A4] <A1>: ", "A0 A1 A2 A3 A4")`.
  4. Prompts for drawing scale: `GetDouble("\nEnter detail scale factor <100>: ")`.
  5. Calls `SheetArrangerCalculator.ArrangeBlocksIntoSheets(...)`.
  6. In AutoCAD ModelSpace:
     - For each arranged sheet:
       - Calculates sheet origin $(X_{\text{origin}}, Y_{\text{origin}})$.
       - Scales sheet border polyline by scale factor (e.g. $841 \times 100 = 84,100\text{ mm}$ for A1 at $1:100$).
       - Draws outer border (`kata_net khung`, lineweight 0.35) and inner printable border.
       - Inserts title block block reference (`khung_ten_A1`) at scaled bottom-right corner.
       - Translates selected entities into their computed target positions via `entity.TransformBy(Matrix3d.Displacement(delta))`.
  7. Commits transaction and reports arranged sheet count.

- **Workflow for `ibv` (`chen_khung_ten`)**:
  1. Prompts for insertion point: `Editor.GetPoint("\nPick insertion point for titleblock (ibv): ")`.
  2. Prompts for Sheet format (A0..A4) and Scale.
  3. Draws standard border and inserts attributed titleblock.

#### 4.1.3 `ViewportController.cs`
- **Command Bindings**:
  - `taomv`, `tao_viewport`: Creates PaperSpace MView scaled to selected ModelSpace window.
  - `taomv1`: Custom scale ratio MView.
  - `khoav`, `khoa_viewport`: Locks display of viewports (`vp.Locked = true`).
  - `mokhoav`, `mo_khoa_viewport`: Unlocks display of viewports (`vp.Locked = false`).
- **Workflow for `taomv`**:
  1. In ModelSpace: User picks two corner points `pt1` and `pt2` framing a detail.
  2. Prompts for standard scale: `GetKeywords("\nSelect scale [1:20/1:25/1:50/1:100] <1:100>: ", "20 25 50 100")`.
  3. User enters target layout name: `GetString("\nEnter target layout name <Layout1>: ")`.
  4. Switches to target layout via `LayoutManager.Current.SetCurrentLayoutId(...)`.
  5. Computes viewport paper dimensions:
     $$W_{\text{paper}} = \frac{|x_2 - x_1|}{\text{scale}}, \quad H_{\text{paper}} = \frac{|y_2 - y_1|}{\text{scale}}$$
  6. Creates `Viewport` entity:
     - `vp.CenterPoint = new Point3d(centerPaperX, centerPaperY, 0);`
     - `vp.Width = W_paper;`
     - `vp.Height = H_paper;`
     - `vp.ViewCenter = new Point2d((x1 + x2)/2, (y1 + y2)/2);`
     - `vp.CustomScale = 1.0 / scale;`
     - `vp.On = true;`
     - `vp.Locked = true;` (auto-lock).

#### 4.1.4 `DrawingIndexController.cs`
- **Command Bindings**: `danhmuc`, `tao_danh_muc_ban_ve`.
- **Workflow**:
  1. Prompts user: `GetKeywords("\nScan titleblocks from [PaperSpace/ModelSpace] <PaperSpace>: ", "PaperSpace ModelSpace")`.
  2. Scans drawing database:
     - In PaperSpace: Iterates all layouts in `LayoutManager`, inspects BlockTableRecord for titleblock references, extracts attributes `SOHIEU`, `TENBV`, `TYLE`, `NGAY`, `REV`.
     - In ModelSpace: Scans block references with names matching `khung_ten*`.
  3. Forwards raw items to `DrawingIndexAggregator.AggregateAndSort(...)`.
  4. Validates sequence completeness and warns of gaps.
  5. Prompts for placement point in ModelSpace:
     `Editor.GetPoint("\nPick insertion point for drawing index table: ")`.
  6. Generates native AutoCAD `Table` entity:
     - Sets TableStyle to `kata_table` (or creates standard table).
     - Assigns column widths and row heights.
     - Populates cell texts with headers and sorted sheet data.
  7. Offers option to export to Excel via ClosedXML.

### 4.2 AutoLISP Command Aliases Integration (`GoiLenh_HPKata.lsp`)
Lines 146–151 of `GoiLenh_HPKata.lsp` already register:
```lisp
(defun c:vg()(command "ve_grid"))
(defun c:danhmuc()(command "danhmuc"))
(defun c:cat()(command "cat_doituong"))
(defun c:taomv()(command "taomv"))
(defun c:taomv1()(command "taomv1"))
(defun c:sbv()(command "sap_ban_ve"))
(defun c:ibv()(command "in_ban_ve"))
```
To support the additional aliases without modifying the 238-line structure or line 152 offset of `GoiLenh_HPKata.lsp`:
- C# `[CommandMethod]` attributes register multiple aliases directly:
  ```csharp
  [CommandMethod("ve_grid")]
  [CommandMethod("vg")]
  [CommandMethod("ve_luoi_truc")]
  public static void VeGrid() => GridDrawController.DrawGridInteractive();

  [CommandMethod("sap_ban_ve")]
  [CommandMethod("sbv")]
  [CommandMethod("sap_xep_ban_ve")]
  public static void SapBanVe() => SheetLayoutController.ArrangeSheetsInteractive();

  [CommandMethod("in_ban_ve")]
  [CommandMethod("ibv")]
  [CommandMethod("chen_khung_ten")]
  public static void ChenKhungTen() => SheetLayoutController.InsertTitleBlockInteractive();

  [CommandMethod("taomv")]
  [CommandMethod("tao_viewport")]
  public static void TaoViewport() => ViewportController.CreateViewportInteractive();

  [CommandMethod("khoav")]
  [CommandMethod("khoa_viewport")]
  public static void KhoaViewport() => ViewportController.LockViewportInteractive(true);

  [CommandMethod("mokhoav")]
  [CommandMethod("mo_khoa_viewport")]
  public static void MoKhoaViewport() => ViewportController.LockViewportInteractive(false);

  [CommandMethod("danhmuc")]
  [CommandMethod("tao_danh_muc_ban_ve")]
  public static void TaoDanhMuc() => DrawingIndexController.GenerateIndexInteractive();
  ```

---

## 5. HPKata.Revit Architecture & Implementation

### 5.1 `RevitSheetArrangementService.cs`
Located at `HPKata/HPKata.Revit/Services/RevitSheetArrangementService.cs`.

#### Core Implementation Architecture:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPKata.Core.DrawingLayout.Calculators;
using HPKata.Core.DrawingLayout.Models;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Units;

namespace HPKata.Revit.Services;

/// <summary>
/// Service automating Revit ViewSheet creation, TitleBlock family placement,
/// and collision-free view placement via Viewport.Create.
/// Complies with Rule R5: literal 304.8 is strictly quarantined in RevitUnitConverter.
/// </summary>
public sealed class RevitSheetArrangementService
{
    private readonly IRevitDocumentContext _context;

    public RevitSheetArrangementService(IRevitDocumentContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Arranges a collection of Revit Views onto newly generated ViewSheets.
    /// </summary>
    public List<ViewSheet> ArrangeViewsOnSheets(
        Document doc,
        List<View> views,
        SheetFormat format = SheetFormat.A1,
        string sheetNumberPrefix = "KC-",
        int startNumber = 1)
    {
        var resultSheets = new List<ViewSheet>();
        if (doc == null || views == null || views.Count == 0) return resultSheets;

        // 1. Resolve TitleBlock FamilySymbol
        FamilySymbol titleBlockSymbol = ResolveTitleBlockSymbol(doc, format);

        // 2. Convert Revit View bounds to Core DrawingBlockItem models (mm)
        var blocks = new List<DrawingBlockItem>();
        var viewMap = new Dictionary<string, View>();

        foreach (var v in views)
        {
            if (v.IsTemplate || !Viewport.CanAddViewToSheet(doc, ElementId.InvalidElementId, v.Id))
                continue;

            // View outline in paper feet
            BoundingBoxXYZ outline = v.Outline;
            double wFt = outline.Max.U - outline.Min.U;
            double hFt = outline.Max.V - outline.Min.V;

            var block = new DrawingBlockItem
            {
                Id = v.Id.ToString(),
                Name = v.Name,
                WidthMm = RevitUnitConverter.FeetToMm(wFt),
                HeightMm = RevitUnitConverter.FeetToMm(hFt)
            };
            blocks.Add(block);
            viewMap[block.Id] = v;
        }

        // 3. Compute 2D Packing via Core Calculator
        var sheetDef = SheetSizeDefinition.CreateStandard(format);
        var titleBlockModel = new TitleBlockModel();
        List<ArrangedSheetResult> arrangedSheets = SheetArrangerCalculator.ArrangeBlocksIntoSheets(
            blocks, sheetDef, titleBlockModel);

        // 4. Create ViewSheets and Viewports inside Revit Transaction
        using var tr = new Transaction(doc, "HPKata: Auto Sheet Arrangement");
        tr.Start();

        int sheetIdx = startNumber;
        foreach (var arranged in arrangedSheets)
        {
            string sheetNum = $"{sheetNumberPrefix}{sheetIdx:D2}";
            var sheet = ViewSheet.Create(doc, titleBlockSymbol.Id);
            sheet.SheetNumber = sheetNum;
            sheet.Name = $"BẢN VẼ CHI TIẾT - PHẦN {sheetIdx}";

            foreach (var placedBlock in arranged.PlacedBlocks)
            {
                if (!viewMap.TryGetValue(placedBlock.Id, out View? targetView)) continue;
                if (!placedBlock.PlacedPositionMm.HasValue) continue;

                // Center position in mm
                double centerXMm = placedBlock.PlacedPositionMm.Value.X + placedBlock.WidthMm / 2.0;
                double centerYMm = placedBlock.PlacedPositionMm.Value.Y + placedBlock.HeightMm / 2.0;

                // Convert to feet via RevitUnitConverter (Rule R5 compliance)
                XYZ centerFt = RevitUnitConverter.PointMmToFeet(centerXMm, centerYMm, 0.0);

                if (Viewport.CanAddViewToSheet(doc, sheet.Id, targetView.Id))
                {
                    Viewport.Create(doc, sheet.Id, targetView.Id, centerFt);
                }
            }

            resultSheets.Add(sheet);
            sheetIdx++;
        }

        tr.Commit();
        return resultSheets;
    }

    private static FamilySymbol ResolveTitleBlockSymbol(Document doc, SheetFormat format)
    {
        string searchToken = format.ToString(); // e.g. "A1"
        var symbol = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_TitleBlocks)
            .WhereElementIsElementType()
            .Cast<FamilySymbol>()
            .FirstOrDefault(s => s.Name.Contains(searchToken) || s.FamilyName.Contains(searchToken))
            ?? new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsElementType()
                .Cast<FamilySymbol>()
                .First();

        if (!symbol.IsActive) symbol.Activate();
        return symbol;
    }
}
```

### 5.2 Revit Drawing Register / Schedule Integration
A companion helper `RevitSheetScheduleService.cs` extracts all `ViewSheet` instances from the document, populates `DrawingIndexItem` records, checks for sequence gaps, and exports a formatted Sheet List Schedule (`ViewSchedule.CreateSheetList(doc)`).

---

## 6. HPKata.UI Integration (`DrawingUtilityViewModel.cs`)

Located at `HPKata/HPKata.UI/ViewModels/DrawingUtilityViewModel.cs`.

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.DrawingLayout.Calculators;
using HPKata.Core.DrawingLayout.Models;

namespace HPKata.UI.ViewModels;

public sealed partial class DrawingUtilityViewModel : ObservableObject
{
    // Grid Generator Properties
    [ObservableProperty] private string _xSpacingsText = "4000, 6000, 6000, 4000";
    [ObservableProperty] private string _ySpacingsText = "5000, 5000, 7000";
    [ObservableProperty] private double _drawingScale = 100.0;
    [ObservableProperty] private string _xStartLabel = "1";
    [ObservableProperty] private string _yStartLabel = "A";
    [ObservableProperty] private double _bubbleRadiusMm = 450.0;
    [ObservableProperty] private double _extensionOffsetMm = 1500.0;
    [ObservableProperty] private bool _hasBubblesBothEnds = true;

    // Sheet Layout Properties
    [ObservableProperty] private SheetFormat _selectedSheetFormat = SheetFormat.A1;
    [ObservableProperty] private SheetOrientation _selectedOrientation = SheetOrientation.Landscape;
    [ObservableProperty] private double _gutterX = 20.0;
    [ObservableProperty] private double _gutterY = 30.0;
    [ObservableProperty] private string _sheetNumberPrefix = "KC-";
    [ObservableProperty] private int _startSheetIndex = 1;

    // Drawing Register Properties
    [ObservableProperty] private ObservableCollection<DrawingIndexItem> _indexItems = [];
    [ObservableProperty] private int _totalSheetCount;
    [ObservableProperty] private string _statusMessage = "Ready";

    public GridSystemModel? GeneratedGridSystem { get; private set; }

    public DrawingUtilityViewModel()
    {
        GenerateGridPreview();
    }

    [RelayCommand]
    public void GenerateGridPreview()
    {
        var xList = ParseSpacings(XSpacingsText);
        var yList = ParseSpacings(YSpacingsText);
        GeneratedGridSystem = GridAxisGenerator.GenerateOrthogonalSystem(
            xList, yList, DrawingScale, XStartLabel, YStartLabel);
        StatusMessage = $"Grid generated: {GeneratedGridSystem.XAxes.Count} X-axes, {GeneratedGridSystem.YAxes.Count} Y-axes.";
    }

    private static List<double> ParseSpacings(string text)
    {
        var result = new List<double>();
        if (string.IsNullOrWhiteSpace(text)) return [5000.0];
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (double.TryParse(part.Trim(), out double val) && val > 0)
                result.Add(val);
        }
        return result.Count > 0 ? result : [5000.0];
    }
}
```

---

## 7. Verification & Testing Strategy

### 7.1 Test Suites Decomposition
To satisfy Acceptance Criterion AC6 without regressions on existing 743 passing tests:

| Test Project | Target Class / File | Test Objective |
|---|---|---|
| `HPKata.Core.Tests` | `GridAxisGeneratorTests.cs` | Orthogonal X/Y spans, bounding box math, bubble coordinates, label skip logic (I/O/Q), multi-letter overflow ('AA'), dimension offsets, skewed and radial grids. |
| `HPKata.Core.Tests` | `SheetArrangerCalculatorTests.cs` | Standard ISO sizes (A0..A4), margins, titleblock exclusion collision avoidance, 2D shelf packing, multi-sheet overflow pagination, non-overlapping validation. |
| `HPKata.Core.Tests` | `DrawingIndexAggregatorTests.cs` | Natural alphanumeric sorting (`KC-01` to `KC-10`), gap detection, duplicate detection, ClosedXML Excel workbook generation. |
| `HPKata.AutoCad.Tests` | `GridDrawControllerTests.cs` | Controller execution, layer creation, entity creation math, takeoff registration. |
| `HPKata.AutoCad.Tests` | `SheetLayoutControllerTests.cs` | Command registration (`vg`, `sbv`, `ibv`, `taomv`, `khoav`, `danhmuc`), bounding box calculation, layout creation. |
| `HPKata.AutoCad.Tests` | `ViewportControllerTests.cs` | Custom scale calculation, PaperSpace viewport dimensions, viewport locked property toggle. |
| `HPKata.Revit.Tests` | `RevitSheetArrangementServiceTests.cs` | Mock document context, View outline to mm conversion, Viewport.Create placement verification, Rule R5 isolation. |
| `HPKata.UI.Tests` | `DrawingUtilityViewModelTests.cs` | ViewModel property changes, relay command recalculations, reactive UI updates. |
| `HPKata.E2E.Tests` | `DrawingLayoutE2ETests.cs` | End-to-end pipeline: Grid generation $\implies$ Sheet arrangement $\implies$ Titleblock insertion $\implies$ Drawing index export. |

### 7.2 Empirical Boundary Test Cases

1. **Grid Axis Boundary Cases**:
   - Single span ($1 \times 1$ grid): verifies no `IndexOutOfRangeException` when list has only 1 element.
   - Large span array (25 spans): verifies label progression past `'Z'` into `'AA'`, `'AB'`.
   - Asymmetric spans ($X = [2000, 8000], Y = [6000]$): verifies bounding box asymmetry.
   - Skewed axes at $45^\circ$: verifies $(\cos 45^\circ, \sin 45^\circ)$ direction unit vector.

2. **Sheet Arranger Boundary Cases**:
   - Giant single detail exceeding A1 sheet size: calculator correctly flags unplaced block or suggests A0.
   - 20 identical small details ($150 \times 100\text{ mm}$): verifies packing density into multiple shelves on a single A1 sheet.
   - Overlap avoidance with TitleBlock: verifies that no block lower-right bounds fall inside $[W - M_r - W_{\text{tb}}, W - M_r] \times [M_b, M_b + H_{\text{tb}}]$.
   - Zero-gap collision check: test asserting $\forall i \ne j, \text{Intersection}(\text{Rect}_i, \text{Rect}_j) == \emptyset$.

3. **Drawing Index Boundary Cases**:
   - Non-standard prefixes: mix of `"KC-01"`, `"KT-05"`, `"ME-02"` correctly grouped by discipline.
   - Missing sequence item: input `["KC-01", "KC-02", "KC-04"]` triggers warning for `"KC-03"`.
   - Duplicate sheet: input `["KC-01", "KC-01"]` triggers duplicate alert.

---

## 8. Implementation Checklist for Worker Agents

- [ ] **Step 1**: Create `HPKata.Core/DrawingLayout/Models/` files:
  - `GridBubbleOrientation.cs`, `GridLineModel.cs`, `GridSystemModel.cs`
  - `SheetFormat.cs`, `SheetSizeDefinition.cs`, `TitleBlockModel.cs`, `DrawingBlockItem.cs`, `ArrangedSheetResult.cs`
  - `DrawingIndexItem.cs`, `DrawingIndexTableDto.cs`
- [ ] **Step 2**: Create `HPKata.Core/DrawingLayout/Calculators/` files:
  - `GridAxisGenerator.cs` (<= 300 lines)
  - `SheetArrangerCalculator.cs` (<= 300 lines)
  - `DrawingIndexAggregator.cs` (<= 300 lines)
- [ ] **Step 3**: Create `HPKata.AutoCad/Controllers/` files:
  - `GridDrawController.cs`
  - `SheetLayoutController.cs`
  - `ViewportController.cs`
  - `DrawingIndexController.cs`
- [ ] **Step 4**: Update `HPKata.AutoCad/Commands/DrawingUtilityCommands.cs` with command bindings for `vg`, `sbv`, `ibv`, `taomv`, `taomv1`, `khoav`, `mokhoav`, `danhmuc`.
- [ ] **Step 5**: Create `HPKata.Revit/Services/RevitSheetArrangementService.cs` adhering to Rule R5.
- [ ] **Step 6**: Create `HPKata.UI/ViewModels/DrawingUtilityViewModel.cs`.
- [ ] **Step 7**: Implement comprehensive unit test suites in `HPKata.Core.Tests/DrawingLayout/`, `HPKata.AutoCad.Tests/`, `HPKata.Revit.Tests/`, and `HPKata.E2E.Tests/`.
- [ ] **Step 8**: Run build and tests with mandatory concurrency flags:
  ```powershell
  dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1
  dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"
  ```

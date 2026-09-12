# Comprehensive Source Codebase Specification Analysis: R02_BeamsRebar

**Repository Target**: `HPRebar` (`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar`)  
**Source Codebase (Reference)**: `F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\RebarAddin-master\RebarAddin-master\R02_BeamsRebar`  
**Mining Agent**: `spec_miner_source_1`  
**Date**: 2026-09-07  
**Status**: Authoritative Architectural & Specification Survey  

---

## Executive Summary

This report delivers an exhaustive reverse-engineering survey and specification of the legacy continuous beam reinforcement module (`R02_BeamsRebar`). The module automates the generation of longitudinal and transverse reinforcement for continuous, multi-span reinforced concrete beams in Autodesk Revit.

The original legacy codebase was developed targeting Revit 2021 on .NET Framework 4.8, relying on monolithic WPF code-behind, coupled external libraries (`WpfCustomControls`, `DSP`), direct UI canvas drawing primitives, and interwoven Revit API calls. 

To migrate `R02_BeamsRebar` into the clean, robust architecture of **HPRebar** (targeting Revit 2025/2026 on .NET 8, plus backward/forward compatibility for R23–R27), this survey extracts:
1. Pure mathematical calculations and geometric abstractions to be isolated in `HPRebar.Core/BeamRebar/` (`netstandard2.0`).
2. Revit-specific adapters, geometry readers, creators, views, and transaction managers in `HPRebar/HPRebar/Beam Rebar/`.
3. Modern WPF MVVM architecture using `CommunityToolkit.Mvvm` and dynamic theme brushes.
4. Business logic bug fixes, numerical guardrails, and unit conversion safeguards.

---

## Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Execution Pipeline | `BeamsRebarCmd` Entry | Selects continuous beam chain, validates geometry, collects supports, displays modal dialog, and coordinates rebar generation. | `ExternalCommandData`, user element picks | `Result.Succeeded` or `Cancelled` | Rolls back entire transaction group on any failure or user cancel. | `Command/BeamsRebarCmd.cs` |
| 2 | Selection Filter | `BeamSelectionFilter` | Restricts user interactive selection strictly to structural framing elements (`OST_StructuralFraming`). | `Reference`, `Element` | `bool` (is structural framing) | Rejects non-framing elements in Revit selection interface. | `Library/Filter/BeamSelectionFilter.cs` |
| 3 | Geometric Validation | `ErrorBeams` Verification | Enforces continuous beam chain rules: collinearity, horizontal orientation, same level, and valid solids. | `IReadOnlyList<Element>` beams | `List<string>` error messages | Throws error dialog and aborts command before transaction group starts. | `Library/Error/ErrorBeams.cs` |
| 4 | Geometry Extraction | `SolidFace` & `ProcessInfoBeamRebar` | Extracts rectangular dimensions ($b$, $h$), centerline curve, end points, and bounding faces for each span. | `Element` framing | `InfoModel` (span length, $b$, $h$, top level, faces) | Throws if beam solid is non-rectangular or has $>1$ solid. | `Library/Orther/ProcessInfoBeamRebar.cs` |
| 5 | Support Discovery | `BeamsBoundBox` Support Finder | Detects intersecting columns (`OST_StructuralColumns`), walls (`OST_Walls`), and girder beams to establish support widths and clear spans. | `Document`, `BoundingBoxXYZ` | `List<NodeModel>` (support center, width $c$, type) | Treats exterior joints without columns as cantilevers or simple end bearings. | `Library/Category/BeamsBoundBox.cs` |
| 6 | Secondary Framing | Intersecting Beam Finder | Discovers incoming secondary beams framing into the continuous primary beam to position hanging ties. | `Document`, `Element` primary beam | `List<SpecialNodeModel>` (location, width $b_s$, depth $h_s$) | Ignores non-intersecting or collinear beams. | `Model/SpecialNodeModel.cs` |
| 7 | Stirrup Layout | Uniform Distribution (`TypeDis = 0`) | Calculates even spacing of closed stirrups across clear span $L_n$ with centering offset. | Clear span $L_n$, spacing $S$, start offset 50mm | `StirrupRun` (count $n$, spacing, start offset $o_1$) | Rejects $S \le 0$ or $n > 1002$. | `Model/DistributeStirrup.cs` |
| 8 | Stirrup Layout | 3-Zone Distribution (`TypeDis = 1, 2`) | Calculates dense-sparse-dense stirrups at support zones ($L_1 = L_n/4$ or $L_n/3$) and midspan zone ($L_2$). | Clear span $L_n$, spacings $S_1, S_2$, zone ratio | 3 $\times$ `StirrupRun` (left, mid, right) | Automatically collapses to uniform if $L_n < 2 \times L_{min}$. | `Model/DistributeStirrup.cs` |
| 9 | Stirrups in Nodes | Column Node Stirrups | Optionally continues stirrup cages through intermediate column joints with dedicated spacing. | Support width $c$, spacing $S_{col}$, toggle `IsStirrupInNode` | `StirrupRun` across support width | Skips support node if toggle is disabled. | `Model/StirrupModel.cs` |
| 10 | Main Bars | Continuous Top Main Bars | Builds continuous longitudinal top bars traversing all spans with 90° downward hooks at exterior ends. | Total length, spans, bar diameter $d_{top}$, count, cover | `BarPolyline` (3D curve vertices) | Anchors into column core; clamps hook length to column depth minus cover. | `Model/MainTopBarModel.cs` |
| 11 | Main Bars | Continuous Bottom Main Bars | Builds continuous longitudinal bottom bars traversing all spans with 90° upward hooks at exterior ends. | Total length, spans, bar diameter $d_{bot}$, count, cover | `BarPolyline` (3D curve vertices) | Steps or terminates at cross-section transitions where depth decreases. | `Model/MainBottomBarModel.cs` |
| 12 | Main Bars Splicing | Staggered Lap Splices | Inserts lap splices ($L_{lap} = 30d \dots 45d$) for spans exceeding stock length (11.7m), staggered 50% in allowable moment zones. | Stock length, lap factor, bar count, stagger toggle | Split `BarPolyline` instances with lap offsets | Prevents top splices at supports and bottom splices at midspans. | `Model/BarsDivisionModel.cs` |
| 13 | Additional Top Bars | Support Over-Reinforcement | Places negative moment top bars centered over intermediate supports with $L/3$ or $L/4$ clear span extensions. | Support width, adjacent $L_{n1}, L_{n2}$, cutoff ratio | `BarPolyline` over support | Supports 2 vertical layers; layer 2 cut shorter (e.g. $L/4$ vs $L/3$). | `Model/AddTopBarModel.cs` |
| 14 | Additional Bottom Bars | Midspan Over-Reinforcement | Places positive moment bottom bars centered in midspan with cutoff boundaries at $L_n/7$ or $L_n/8$ from support faces. | Clear span $L_n$, cutoff ratio, bar count, diameter | `BarPolyline` in midspan | Supports 2 vertical layers with vertical spacer gap. | `Model/AddBottomBarModel.cs` |
| 15 | Deep Beam Skin Bars | Side / Web Reinforcement | Places longitudinal skin bars along lateral faces when beam height $h \ge 700$ mm ($s_{skin} \le 300$ mm). | Beam height $h$, skin spacing limit, bar diameter | Pairs of `BarPolyline` (left and right faces) | Automatically omitted when beam height $h < 700$ mm. | `Model/SideBarModel.cs` |
| 16 | Web Cross-Ties | Anti-Buckling Skin Ties | Places transverse C-ties or cross-ties connecting opposite side bars at spaced intervals along the beam axis. | Side bar positions, tie spacing ($2 \times S_{stirrup}$) | `StirrupRun` of C-ties | Omitted if side bars are absent. | `Model/SideBarModel.cs` |
| 17 | Secondary Beam Joint | Hanging Stirrups (Cốt treo) | Places concentrated closed stirrups flanking incoming secondary beam intersections to resist shear pullout. | Secondary beam width $b_s$, depth $h_s$, count per side | `StirrupRun` on left and right of joint | Merges overlapping hanging zones if adjacent secondary beams are close. | `Model/SpecialBarModel.cs` |
| 18 | Secondary Beam Joint | Diagonal Hanging Bars | Places 45° diagonal bent bars ("thép vai bò") under secondary beam bottom soffit across intersection. | Secondary beam depth $h_s$, bar diameter | Bent `BarPolyline` (45° inclined legs) | Configurable via UI toggle; omitted if secondary depth $\le 200$ mm. | `Model/SpecialBarModel.cs` |
| 19 | UI Presentation | Tabbed Configuration Shell | Provides tabbed dialog: Geometry, Stirrups, MainBars, AddTop, AddBottom, SideBars, SpecialBars, Settings. | User input values, selected beams session | Configured rebar specification | Validates numeric ranges dynamically; disables OK if invalid. | `View/BeamsWindow.xaml` |
| 20 | Interactive Canvas | Continuous Elevation Preview | Draws vector elevation of all spans, supports, stirrup zones, longitudinal bars, cutoffs, and hanging ties. | WPF Canvas dimensions, beam domain model | Rendered Shapes, Path geometries, TextBlocks | Auto-scales $(X, Z)$ coordinates to canvas aspect ratio with margins. | `Library/Draw/DrawMainCanvas.cs` |
| 21 | Interactive Canvas | Cross-Section Preview | Draws cross-section at active span/support showing $b, h$, stirrup legs, main bars, layers, and side bars. | WPF Canvas dimensions, active cross-section model | Rendered cross-section visual | Updates reactively as user edits bar counts and diameters. | `Library/Draw/DrawImageRebar.cs` |
| 22 | Drawing Generation | Detail Elevation & Section Views | Automatically creates longitudinal section view and transverse section views at critical stations. | `Document`, `ViewFamilyType`, `BoundingBoxXYZ` | `ViewSection` instances | Catches naming collisions and suffixes view names. | `Library/Create/DetailBeamView.cs` |
| 23 | Annotations | Dimensions & Rebar Tags | Adds dimension strings to views and places parametric rebar tags with bar mark, diameter, and spacing. | `ViewSection`, rebar elements, reference faces | `Dimension`, `IndependentTag` instances | Converts `SURFACE` references to `LINEAR` for section dimensions. | `Library/Create/CreateViewDimension.cs` |

---

## Edge Cases

| # | Feature | Input | Observed Behavior | Handling in Port |
|---|---------|-------|-------------------|------------------|
| 1 | Continuous Alignment | Beams modeled with reversed direction vectors (e.g. Beam 1 drawn Left-to-Right, Beam 2 drawn Right-to-Left). | Centerline vector dot product is $-1.0$. Point chaining fails. | Normalize span coordinate frame by establishing dominant vector $\vec{U}_X$; invert curve direction if $\vec{V} \cdot \vec{U}_X < 0$. |
| 2 | Continuous Alignment | User picks non-collinear beams (e.g. angled at 45° or offset horizontally by 150 mm). | Axis alignment cross product $> 0.01$ or distance from line $> 10$ mm. | `BeamStackValidator` fails validation with clear message: *"Beams are not collinear"*. |
| 3 | Variable Depth | Adjacent spans have different cross-sections (e.g. Span 1 is $300 \times 600$, Span 2 is $300 \times 400$). | Top faces flush at slab; bottom soffit has a 200 mm vertical step at support column. | Bottom main bars cannot run straight through; anchor bottom bars of each span into the support node with upward hooks. |
| 4 | Support Boundary | Cantilever overhang at exterior end (no column or wall supporting the beam tip). | Exterior node has no column intersection. | Classify as `CantileverLeft` or `CantileverRight`. Top tension bars continue to tip and turn down 90°; bottom bars stop at support. |
| 5 | Short Clear Span | Extremely short link beam ($L_n < 600$ mm, e.g. between elevator core walls). | 3-zone calculation ($L_1 = L_n/4 = 150$ mm) leaves no space for spacing $S_2$. | Fall back automatically to uniform stirrup distribution (`TypeDis = 0`). |
| 6 | Dense Stirrups | Extremely long beam with dense spacing ($L_n = 12$ m @ 50 mm $\to$ 241 stirrups). | Number of bars $< 1002$. Supported by Revit. | Enforce `MaxBarPositions = 1002` guardrail in core calculator before calling Revit API. |
| 7 | Short Curves | Curve segment in polyline bend $< 1.0$ mm (e.g. rounding error on cover offset). | Revit `CreateFromCurves` throws `ArgumentException: Curve is too short`. | Filter and simplify polyline points: merge points where distance $< 1.0$ mm. |
| 8 | Non-Planar Polyline | 3D polyline has minor out-of-plane lateral deviation ($Y$-delta $> 0.001$ mm). | Revit throws `ArgumentException: Curves must be planar`. | Force all vertices of a given bar to have exact identical local $Y$ coordinate. |
| 9 | Multi-layer Crowding | High bar count (e.g. 6 bars $\Phi 25$ in $b = 250$ mm beam). | Clear horizontal distance between bars $< 25$ mm violates standard building codes. | Distribute surplus bars into Layer 2 with vertical offset $\Delta Z = d_{bar} + 30$ mm. |
| 10 | Secondary Beam Framing | Multiple secondary beams frame into primary beam within $< 200$ mm of each other. | Hanging stirrup zones overlap, causing duplicate overlapping stirrups. | Merge overlapping hanging intervals into a single combined hanging zone. |
| 11 | View Dimensioning | Passing `PlanarFace.Reference` directly to `NewDimension` in `ViewSection`. | Revit throws exception or fails silently. | Parse stable representation string and replace `"SURFACE"` with `"LINEAR"`. |
| 12 | Culture Formatting | Host system configured with comma decimal separator (e.g. German/Vietnamese `300,50`). | `double.Parse(UnitFormatUtils.Format(...))` crashes or parses incorrect scale. | Replace string formatting completely with `UnitUtils.ConvertFromInternalUnits(..., UnitTypeId.Millimeters)`. |

---

## 1. Inventory of Classes, Structs & Algorithms in R02_BeamsRebar

The legacy codebase consists of 95 C# source files, 11 XAML view files, and supporting resource dictionaries.

### 1.1 Command & Entry Points
- `Command/BeamsRebarCmd.cs`: Main `IExternalCommand` entry point. Contains picking logic, selection validation, model bootstrapping, UI invocation, and transaction commit.
- `Command/ModifyCmd.cs`: Command for re-editing reinforcement on previously processed beams (via shared parameters or extensible storage).

### 1.2 Geometry & Domain Models (`Model/`)
- `BeamsModel.cs`: Master aggregation model holding continuous spans, support nodes, reinforcement specifications, and cross-section parameters.
- `InfoModel.cs`: Geometric representation of a single beam span (length, width $b$, height $h$, bottom/top elevation, bounding box, planar faces).
- `NodeModel.cs`: Geometric representation of an intermediate or end support (column, wall, girder). Holds support center $X$, width $C$, depth $B$, and support type.
- `SpecialNodeModel.cs`: Intersection node where a secondary beam frames into the main beam. Holds center $X$, width $b_s$, depth $h_s$, and framing angle.
- `StirrupModel.cs`: Specification for stirrups in a span (bar type, cover, distribution type, $S_1, S_2$, $L_1$, hook shape).
- `DistributeStirrup.cs`: Mathematical calculation of stirrup locations, counts, and spacing offsets across clear span zones.
- `MainTopBarModel.cs` & `SingleMainTopBarModel.cs`: Definition of continuous top longitudinal bars, diameters, anchorages, and polylines.
- `MainBottomBarModel.cs`: Definition of continuous bottom longitudinal bars, diameters, anchorages, and bottom-step transitions.
- `AddTopBarModel.cs`: Additional top reinforcement over supports ($L/3$, $L/4$, multi-layer).
- `AddBottomBarModel.cs`: Additional bottom reinforcement at midspans ($L/7$, $L/8$, multi-layer).
- `SideBarModel.cs`: Skin / web reinforcement for deep beams ($h \ge 700$ mm) and anti-buckling cross-ties.
- `SpecialBarModel.cs`: Hanging stirrup cages and diagonal ties at secondary beam intersections.
- `LayerModel.cs` & `ListLayerModel.cs`: Layer offset tracking for multi-layer bar arrangements.
- `LocationBarModel.cs`: 3D vertex generator converting bar parameters into discrete `(X, Y, Z)` coordinate lists.
- `BarsDivisionModel.cs`: Rebar commercial length division (11.7m max) and 50% staggered lap splicing.
- `DrawModel.cs`: Viewport scaling, canvas transformation matrices, and drawing style parameters.

### 1.3 Library Helpers & Revit Interop (`Library/`)
- `Library/Category/BeamsBoundBox.cs`: Bounding box spatial queries to locate underlying columns and intersecting secondary beams.
- `Library/Error/ErrorBeams.cs`: Geometric and category validation checking for horizontal alignment, collinearity, and valid solids.
- `Library/Filter/BeamSelectionFilter.cs`: Revit UI selection filter (`ISelectionFilter`) for `OST_StructuralFraming`.
- `Library/Orther/ProcessInfoBeamRebar.cs`: Geometry engine extracting solid faces, widths, heights, and coordinate transformations.
- `Library/Orther/SolidFace.cs`: Face classification utility (identifying Top, Bottom, South, North, East, West faces of 3D solids).
- `Library/Orther/PointModel.cs` & `LineProcess.cs`: Vector and line arithmetic, project-to-plane math, and distance calculations.
- `Library/Create/CreateRebar.cs`: Revit API calls creating `Rebar` instances (`CreateFromRebarShape`, `CreateFromCurves`, `CreateFreeForm`).
- `Library/Create/CreateViewDimension.cs`, `DetailBeamView.cs`, `SectionBeamView.cs`, `DimensionView.cs`: Drawing automation, section creation, and linear dimensioning.
- `Library/Draw/DrawMainCanvas.cs`, `DrawImageRebar.cs`: Canvas rendering engine projecting beam geometry onto WPF visual elements.

---

## 2. Continuous Beam Span Geometry & Support Modeling

### 2.1 Coordinate Space & Continuous Datum
In continuous beam reinforcement, individual Revit beam elements have their own local coordinate systems based on the direction they were drawn. The legacy codebase solves this by creating a **Unified Continuous Beam Datum**:
1. **Primary Direction Vector ($\vec{U}_X$)**: Established from the start point of Span 1 towards the end point of the last Span.
2. **Vertical Vector ($\vec{U}_Z$)**: World vertical $(0, 0, 1)$.
3. **Transverse Normal ($\vec{U}_Y$)**: $\vec{U}_Y = \vec{U}_Z \times \vec{U}_X$ (perpendicular horizontal vector).
4. **Origin ($P_0$)**: Set at the exterior face or centerline of the initial support (Support 0).

Every span $i$ is positioned along $\vec{U}_X$ with:
- $X_{start, i}$: Starting coordinate along beam axis.
- $X_{end, i}$: Ending coordinate along beam axis.
- Span center length: $L_{center, i} = X_{end, i} - X_{start, i}$.
- Width $b_i$ and Height $h_i$.
- Top elevation $Z_{top, i}$ and Soffit elevation $Z_{bot, i} = Z_{top, i} - h_i$.

```
 Elevation View:
 +--------------------+-----------------------+--------------------+  Z_top
 |      Span 1        |        Span 2         |       Span 3       |
 | (b1 x h1)          | (b2 x h2)             | (b3 x h3)          |
 +---------+----------+-----------+-----------+----------+---------+  Z_bot
           |                      |                      |
      [Support 0]            [Support 1]            [Support 2]   [Support 3]
      Column c0              Column c1              Column c2     Column c3
```

### 2.2 Support Nodes (`NodeModel`)
Support nodes represent physical bearings beneath the continuous beam:
- **Intermediate Supports**: Columns, concrete walls, or cross girders. Support width along beam axis is $C_j$. Clear spans are computed as:
  $$L_{n, i} = L_{center, i} - \frac{C_{left}}{2} - \frac{C_{right}}{2}$$
- **Exterior Supports**: End columns or walls.
- **Cantilever Spans**: When a beam extends past an exterior support without a terminating column ($C_{end} = 0$).

### 2.3 Variable Depths & Vertical Steps
When adjacent spans have different depths ($h_1 \ne h_2$):
- Typically top faces are flush ($Z_{top, 1} = Z_{top, 2}$) to support a continuous floor slab.
- Bottom soffit steps by $\Delta h = |h_1 - h_2|$.
- **Rule for Bottom Bars**: Main bottom bars of the deeper span cannot continue straight into the shallower span. They must anchor into the intermediate support column with 90° upward hooks ($L_a \ge 30d$). Bottom bars of the shallower span anchor into the column similarly.

---

## 3. Stirrup Reinforcement Logic

### 3.1 Distribution Algorithms (`DistributeStirrup.cs`)
The clear span $L_n$ is divided into stirrup zones based on `TypeDis`:

#### Uniform Layout (`TypeDis = 0`):
- Spacing $S$.
- First bar offset from support face: $o_{start} = 50$ mm.
- Available length: $L_{dist} = L_n - 2 \times 50$ mm.
- Stirrup count:
  $$n = \left\lfloor \frac{L_{dist}}{S} \right\rfloor + 1$$
- Centering margin:
  $$\delta = \frac{L_{dist} - (n - 1) \times S}{2}$$
- Positions: $X_k = X_{face} + 50 + \delta + k \times S \quad (k = 0 \dots n-1)$.

#### 3-Zone Layout (`TypeDis = 1`: $L/4 - L/2 - L/4$):
- Zone 1 (Left Support Zone): Length $L_1 = L_n / 4$, dense spacing $S_1$ (e.g. 100 mm).
  $$n_1 = \left\lfloor \frac{L_1 - 50}{S_1} \right\rfloor + 1$$
- Zone 2 (Midspan Zone): Length $L_2 = L_n - 2 \times L_1 = L_n / 2$, sparse spacing $S_2$ (e.g. 200 mm).
  $$n_2 = \left\lfloor \frac{L_2}{S_2} \right\rfloor + 1$$
- Zone 3 (Right Support Zone): Length $L_3 = L_1 = L_n / 4$, dense spacing $S_1$.
  $$n_3 = n_1$$

#### 3-Zone Layout (`TypeDis = 2`: $L/3 - L/3 - L/3$):
- Zone 1: $L_1 = L_n / 3$, spacing $S_1$.
- Zone 2: $L_2 = L_n / 3$, spacing $S_2$.
- Zone 3: $L_3 = L_n / 3$, spacing $S_1$.

### 3.2 Stirrup Sizing & RebarShape
- Shape: Standard rectangular closed hoop with 135° seismic hooks (`M_T1` or `T1`).
- Bounding Dimensions for `RebarShapeDrivenAccessor.ScaleToBox`:
  - Width: $W = b - 2 \times Cover$
  - Height: $H = h - 2 \times Cover$
- Origin: Lower-left corner in transverse plane:
  $$P_{origin} = P_{soffit} + Cover \cdot \vec{U}_Y + Cover \cdot \vec{U}_Z$$

---

## 4. Main Longitudinal Reinforcement Logic

### 4.1 Top Main Bars (`MainTopBarModel`)
- Continuous bars running along the top of the beam from start to end.
- Transverse distribution across beam width $b$:
  $$\Delta Y = \frac{b - 2 \times Cover - 2 \times d_{stirrup} - d_{top}}{n_{top} - 1}$$
  $$Y_i = - \frac{b}{2} + Cover + d_{stirrup} + \frac{d_{top}}{2} + i \cdot \Delta Y \quad (i = 0 \dots n_{top}-1)$$
- Vertical position: $Z = Z_{top} - Cover - d_{stirrup} - d_{top}/2$.
- Exterior Anchorage:
  - 90° downward bend into exterior column core.
  - Bend length: $L_{hook} = h_{beam} - 2 \times Cover$ or user specified anchor length $L_a$ (typically $\ge 30d$).

```
 Top Main Bar Polyline:
 (0, Z_top - h_hook)
        |
        |  (90 deg bend downward into column)
        +-----------------------------------------------------+
 (0, Z_bar)                                            (L_total, Z_bar)
                                                              |
                                                              |  (90 deg bend down)
                                                              v
                                                       (L_total, Z_top - h_hook)
```

### 4.2 Bottom Main Bars (`MainBottomBarModel`)
- Continuous bars along beam soffit.
- Vertical position: $Z = Z_{bot} + Cover + d_{stirrup} + d_{bot}/2$.
- Exterior Anchorage: 90° upward bend into column core ($L_{hook} \ge 30d$).
- At cross-section depth steps: Terminated at support node with 90° upward hooks.

### 4.3 Splicing & Stagger Rules (`BarsDivisionModel`)
Standard rebar stock length is 11.7 m (commercial bar length in Vietnam and international practice). For continuous beams exceeding 11.7 m:
1. **Top Bars**: Spliced strictly in the **midspan region** (between $L/3$ and $2L/3$), where bending moment is positive and top fibers are in compression.
2. **Bottom Bars**: Spliced strictly at **supports** (within $L/4$ from support face), where bending moment is negative and bottom fibers are in compression.
3. **50% Staggered Splicing**:
   - Alternating bars ($i = 0, 2, 4 \dots$) splice at Station $X_A$.
   - Intermediate bars ($i = 1, 3, 5 \dots$) splice at Station $X_B = X_A + 1.3 \times L_{lap}$.
   - Lap length: $L_{lap} = 35d$ to $45d$.

---

## 5. Additional Reinforcement (Gia Cường Gối & Nhịp)

### 5.1 Additional Top Bars over Supports (`AddTopBarModel`)
Designed to resist peak negative bending moments at column supports:
- **Geometry**:
  - Centered over support node $j$ with width $C_j$.
  - Left extension into Span $j$: $L_{ext, left} = L_{n, left} / 3$ (or $L_n / 4$).
  - Right extension into Span $j+1$: $L_{ext, right} = L_{n, right} / 3$ (or $L_n / 4$).
  - Total length: $L_{bar} = L_{ext, left} + C_j + L_{ext, right}$.
- **Multi-layering**:
  - **Layer 1**: Placed in the gaps between top main bars at same vertical elevation.
  - **Layer 2**: Placed directly underneath Layer 1 with clearance:
    $$\Delta Z = - (d_{bar} + 30\text{ mm})$$
  - **Staggered Cutoff**: Layer 1 cut at $L/3$; Layer 2 cut shorter at $L/4$ to match moment gradient.
- **Exterior Support**: Anchors into end column with 90° downward hook, extends $L_n/3$ into first span.

```
 Additional Top Bar Elevation over Column:
               <--- L_n1 / 3 ---> |  C  | <--- L_n2 / 3 --->
 Layer 1:      +------------------+-----+------------------+
 Layer 2:           +-------------+-----+-------------+
                          [Support Column]
```

### 5.2 Additional Bottom Bars in Midspan (`AddBottomBarModel`)
Designed to resist peak positive bending moments at midspan:
- **Geometry**:
  - Starts at $X_{start} = X_{face, left} + L_n / 7$ (or $L_n / 8$).
  - Ends at $X_{end} = X_{face, right} - L_n / 7$ (or $L_n / 8$).
  - Total length: $L_{bar} \approx 0.75 L_n$.
- **Multi-layering**:
  - Layer 1: In line with main bottom bars.
  - Layer 2: Located $\Delta Z = + (d_{bar} + 30\text{ mm})$ above Layer 1.

---

## 6. Deep Beam Side Reinforcement & Hanging Bars

### 6.1 Side / Web Reinforcement (`SideBarModel`)
- **Trigger**: Beam total height $h \ge 700$ mm (per TCVN 5574:2018 §10.3.2 and ACI 318 §9.7.2.3).
- **Diameter**: Typically $\Phi 12$ or $\Phi 14$.
- **Spacing**: Vertical spacing $s_v \le 300$ mm. Number of pairs:
  $$n_{pairs} = \left\lceil \frac{h - 2 \times Cover - 200}{300} \right\rceil$$
- **Positions**: Placed in pairs along left and right side faces:
  $$Y_{left} = - \frac{b}{2} + Cover + d_{stirrup} + \frac{d_{side}}{2}$$
  $$Y_{right} = + \frac{b}{2} - Cover - d_{stirrup} - \frac{d_{side}}{2}$$
- **Anti-Buckling Cross-Ties (C-Ties)**:
  - Connect opposite side bars horizontally.
  - Longitudinal spacing along beam axis: $s_{tie} = 400$ mm (or $2 \times S_{stirrup}$).
  - Hook shape: 135° hook at one end, 90° hook at opposite end for installation ease.

### 6.2 Secondary Beam Hanging Ties (`SpecialBarModel`, `SpecialNodeModel`)
- **Physical Problem**: Concentrated shear from incoming secondary beam introduces diagonal tension in primary beam web.
- **Hanging Stirrups (Cốt treo)**:
  - Placed symmetrically on both sides of secondary beam framing face.
  - Zone width: extends $h_{secondary} / 2$ to left and right of secondary beam.
  - Quantity: User specifies $n$ pairs (typically 2 to 4 closed stirrups each side, $\Phi 8$ or $\Phi 10$, spaced at 50 mm).
- **Diagonal Bars (Thép vai bò)**:
  - 45° bent bars inclined upwards towards the top compression zone of the primary girder.
  - Positioned directly beneath secondary beam soffit.

```
 Secondary Beam Intersection (Hanging Zone):
               |<--- Secondary Beam (bs) --->|
       ||  ||  ||                           ||  ||  ||
       ||  ||  ||                           ||  ||  ||
      [3 stirrups @ 50]                    [3 stirrups @ 50]
```

---

## 7. UI Parameters & Preview Canvas Math

### 7.1 View Models & Input Parameters
The user interface is organized into 8 tabs:
1. **Geometry Tab (`GeometryViewModel`)**: Displays read-only span table (Span name, Length $L$, $b$, $h$, Level) and support table (Support name, Width $C$, Type). Allows editing support widths if not auto-detected.
2. **Stirrups Tab (`StirrupsViewModel`)**:
   - Spacings $S_1$ (support zone) and $S_2$ (midspan zone).
   - Distribution type: Uniform, $L/4 - L/2 - L/4$, or $L/3 - L/3 - L/3$.
   - Stirrup bar type, cover thickness, hook shape (`M_T1`).
   - `IsStirrupInNode` toggle.
3. **Main Bars Tab (`BarsMainViewModel`)**:
   - Top main bar diameter and count ($n \ge 2$).
   - Bottom main bar diameter and count ($n \ge 2$).
   - End anchorage hook lengths and types.
   - Splicing rules: max stock length (11.7 m), lap multiplier ($35d \dots 45d$), 50% stagger toggle.
4. **Additional Top Bars Tab (`AddTopBarViewModel`)**:
   - Per-support configuration matrix.
   - Layer 1 count & diameter; Layer 2 count & diameter.
   - Cutoff ratios (default $L/3$ for Layer 1, $L/4$ for Layer 2).
5. **Additional Bottom Bars Tab (`AddBottomBarViewModel`)**:
   - Per-span configuration matrix.
   - Layer 1 count & diameter; Layer 2 count & diameter.
   - Cutoff ratios (default $L/7$ or $L/8$).
6. **Side Bars Tab (`SideBarViewModel`)**:
   - Beam depth threshold (default 700 mm).
   - Side bar diameter, max vertical spacing (default 300 mm).
   - Cross-tie spacing (default 400 mm).
7. **Special Bars Tab (`SpecialBarViewModel`)**:
   - List of detected secondary framing beams.
   - Hanging stirrup count per side (e.g. 3), diameter, spacing (50 mm).
   - Optional diagonal tie toggle.
8. **Settings Tab (`SettingViewModel`)**:
   - View templates, dimension styles, annotation tags, and section view offsets.

### 7.2 Preview Canvas Scaling & Transformation Math
In `DrawMainCanvas.cs`, the continuous beam must fit into a fixed WPF Canvas ($W_{canvas} \times H_{canvas}$, typically $1100 \times 400$ px) with margins $M = 40$ px:

1. **Available Draw Area**:
   $$W_{draw} = W_{canvas} - 2 \times M$$
   $$H_{draw} = H_{canvas} - 2 \times M$$
2. **Beam Bounding Box**:
   $$L_{total} = \sum_{i=1}^{N} L_{center, i}$$
   $$H_{max} = \max_{i}(h_i) + H_{columns}$$
3. **Uniform Scale Factor**:
   $$S = \min\left(\frac{W_{draw}}{L_{total}}, \frac{H_{draw}}{H_{max}}\right)$$
4. **Coordinate Transformation**:
   For any domain point $(X, Z)$ in millimetres:
   $$X_{canvas} = M + (X - X_{min}) \times S$$
   $$Y_{canvas} = H_{canvas} - M - (Z - Z_{min}) \times S$$
   *(Note: WPF $Y$ increases downwards, hence $H_{canvas} - \dots$)*.

---

## 8. Business Logic Bugs, Edge Cases & Migration Plan

### 8.1 Critical Bugs Discovered in Legacy Source

1. **Culture-Sensitive Unit Parsing (`PointModel.cs`, `InfoModel.cs`)**:
   - Legacy: `double.Parse(UnitFormatUtils.Format(...))`
   - Hazard: On Windows systems with European/Vietnamese number formatting (comma decimal separator), `double.Parse` throws `FormatException` or misparses numbers by a factor of 1000.
   - Fix in Port: Completely replace string round-tripping with `UnitUtils.ConvertFromInternalUnits(val, UnitTypeId.Millimeters)` and vice-versa.
2. **No-Op LINQ Ordering (`ProcessInfoBeamRebar.cs`, `SolidFace.cs`)**:
   - Legacy: `faces.OrderBy(f => f.Origin.Z);` without assigning to a variable.
   - Hazard: LINQ does not mutate lists in place. Faces remain unsorted, causing random assignment of top/bottom faces.
   - Fix in Port: Assign explicitly: `faces = faces.OrderBy(...).ToList();`.
3. **Unchecked Bar Count Limit (`DistributeStirrup.cs`)**:
   - Hazard: Very dense stirrups or long spans exceeding 1002 bar positions crash Revit during `SetLayoutAsNumberWithSpacing`.
   - Fix in Port: Enforce `RequireUsableCount(count, MaxBarPositions = 1002)` in `StirrupDistributionCalculator`.
4. **Self-Assignment Bug (`SingleMainTopBarModel.cs`)**:
   - Legacy: `Bar = Bar;` inside constructor instead of assigning parameter `rebarBarModel`.
   - Fix in Port: Pure C# immutable records eliminating mutable assignment errors.
5. **Zero-Length Polyline Segments (`LocationBarModel.cs`)**:
   - Hazard: Rounding errors create segments $< 1.0$ mm, causing `Rebar.CreateFromCurves` to crash.
   - Fix in Port: Add `Simplify(points, minDistance = 1.0)` in `BarPolylineBuilder`.
6. **Section Dimension Reference Failure (`DimensionView.cs`)**:
   - Hazard: Stable representation tokens of section cuts fail with `SURFACE`.
   - Fix in Port: Port the verified `ToLinearReference` token re-writer from `HPRebar/Column Rebar/DimensionCreator.cs`.

### 8.2 Clean Separation: HPRebar.Core vs HPRebar Add-In

| Component | Target Project | Namespace | Dependencies |
|---|---|---|---|
| Domain Records (`BeamSpan`, `BeamSupportNode`, `BeamStirrupSpec`, `BeamMainBarSpec`, `BeamAdditionalBarSpec`, `BeamSideBarSpec`, `BeamSpecialBarSpec`, `BeamPolyline`) | `HPRebar.Core` | `HPRebar.Core.BeamRebar.Models` | None (`netstandard2.0`) |
| Pure Calculators (`BeamStirrupCalculator`, `BeamMainBarCalculator`, `BeamAdditionalBarCalculator`, `BeamSideBarCalculator`, `BeamSpecialBarCalculator`, `BeamBarPolylineBuilder`, `BeamCanvasScaleCalculator`) | `HPRebar.Core` | `HPRebar.Core.BeamRebar` | None (`netstandard2.0`) |
| Pure Unit Tests (Covering 1-span, multi-span, cantilevers, 3-zone, steps, hanging ties) | `HPRebar.Core.Tests` | `HPRebar.Core.Tests.BeamRebar` | `xunit.v3`, `HPRebar.Core` |
| Revit Readers & Validators (`BeamStackReader`, `BeamSolidFaceReader`, `BeamSupportFinder`, `BeamStackValidator`, `StructuralFramingSelectionFilter`) | `HPRebar` | `HPRebar.BeamRebar` | `Nice3point.Revit.Sdk`, Revit API |
| Revit Rebar Creators (`BeamRebarCreationService`, `BeamStirrupCreator`, `BeamMainBarCreator`, `BeamAdditionalBarCreator`, `BeamSideBarCreator`, `BeamSpecialBarCreator`) | `HPRebar` | `HPRebar.BeamRebar` | `Nice3point.Revit.Sdk`, Revit API |
| Revit Views & Dimensions (`BeamDetailViewCreator`, `BeamSectionViewCreator`, `BeamDimensionCreator`, `BeamTagCreator`) | `HPRebar` | `HPRebar.BeamRebar` | `Nice3point.Revit.Sdk`, Revit API |
| Orchestrator & Command (`BeamRebarOrchestrator`, `BeamRebarCommand`) | `HPRebar` | `HPRebar.BeamRebar` | `Nice3point.Revit.Sdk`, Revit API |
| WPF MVVM UI & ViewModels (`BeamRebarViewModel`, Tab ViewModels, Views, Canvas Preview) | `HPRebar` | `HPRebar.BeamRebar.ViewModels`, `.Views` | `CommunityToolkit.Mvvm`, WPF |

---

## 9. Conclusion

The domain logic, geometric modeling, reinforcement rules, and UI canvas requirements of `R02_BeamsRebar` have been thoroughly analyzed and synthesized. 

The legacy codebase contains rich domain knowledge regarding Vietnamese concrete detailing practices (3-zone stirrups, $L/3-L/4$ top support additions, $L/7$ bottom midspan additions, skin reinforcement for $h \ge 700$ mm, secondary beam hanging ties), but suffers from legacy architectural coupling and fragile Revit API usage.

By porting all pure mathematics into `HPRebar.Core/BeamRebar/` backed by a 100% testable xUnit v3 suite, and wrapping all Revit interactions in `HPRebar/HPRebar/Beam Rebar/` under an atomic `TransactionGroup`, the resulting implementation will be robust, maintainable, and seamlessly compliant with Revit 2023–2027.

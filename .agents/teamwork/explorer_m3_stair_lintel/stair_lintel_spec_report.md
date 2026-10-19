# Stair & Lintel Subsystem Technical Specification & Architecture Design
**Subsystem**: Phase 4 — Stair, Lintel & Generic Section Detailing (`HPKata`)  
**Milestone**: M3 (Feature 12)  
**Author**: `explorer_m3_stair_lintel` (Stair & Lintel Domain Explorer)  
**Date**: 2026-10-17  
**Status**: COMPLETE / READY FOR IMPLEMENTATION  

---

## 1. Executive Summary & Domain Scope

The Staircase & Lintel subsystem represents Feature 12 of Milestone M3 within the clean-room reconstruction of the KataPro suite (`HPKata`). This subsystem provides complete structural modeling, automated reinforcement detailing, DWG CAD drafting, native Revit 3D rebar generation, and ClosedXML schedule aggregation for:
1. **Reinforced Concrete Staircases** (`StairFlightModel`, AutoCAD command `ve_thang` / alias `vethang`, Revit `OST_Stairs` rebar generator): Handles 1-flight, 2-flight parallel (dog-leg), 3-flight open-well, and continuous stairs, computing continuous soffit tension bars, top negative support bars, step nosing transverse bars, and structural **scissor-cranked junction bars** at re-entrant corners to prevent radial tensile concrete breakout.
2. **Masonry Wall Lintels** (`LintelModel`, AutoCAD command `velanhto`): Details concrete lintel beams over window and door wall openings with standard $\ge 200\text{ mm}$ (typically $250\text{ mm}$) wall bearing lengths, top hanger bars, bottom tensile bars with $90^\circ$ hooks, and closed stirrup zones ($\le 150\text{ mm}$ at bearings, $150-200\text{ mm}$ at midspan).
3. **Generic Cross-Section Detailing Engine** (`GenericSectionModel`, AutoCAD command `vemc`): A universal cross-section drafting engine generating section cuts across beams, columns, walls, stairs, and lintels, with concrete outline hatching, longitudinal bar points, stirrup hoops, associative dimensioning, and standard callout tags (title bubble, scale, bar marks).
4. **DWG XRecord Persistence**: Two-way JSON serialization into DWG Extension Dictionaries under keys `KATA_STAIR_DATA` and `KATA_LINTEL_DATA`.
5. **Revit 2026 Native Integration**: Full 3D `Rebar` instantiation in `OST_Stairs` hosts via `StairRebarGenerator.cs` with idempotent update guarantees.
6. **Excel Scheduling**: Aggregates all rebar items into standard `RebarItem` records and generates ClosedXML workbook sheet `Thang` matching TCVN barem weight grouping.
7. **Clean Code Governance**: 100% adherence to Rule C2 ($\le 300$ physical lines per file), Rule M3 ($\le 50$ physical lines per method), zero dead code (Rule Q-B1), zero unannotated catch blocks (Rule Q-B2), and complete isolation of `HPKata.Core` (zero AutoCAD/Revit references).

---

## 2. Staircase Detailing Architecture & Mathematical Models (`StairFlightModel`, `ve_thang`)

### 2.1 Geometric Parameter Definitions & TCVN Ergonomic Rules
A stair flight comprises an inclined reinforced concrete waist slab supporting a series of steps connecting landings or floor beams.

```
       Top Landing / Beam
      +---------------------+
      |                     |
      +-------+             |
       Riser  |             |  t_land2
              +-------+     |
                      |     +-------------------------+
                      +-------+                       |
                              |   Inclined Flight     |
                              +-------+  Waist t_w    |
                                      |               |
                                      +-------+       |
                                              |       |
                                  Bottom      +-------+
                                  Landing    /       /
                            +---------------+       /
                            |                      /
                    t_land1 |                     /
                            +--------------------+
```

#### Primary Flight Parameters:
| Symbol | Parameter Name | Range / Typical Value | Description |
|---|---|---|---|
| $n_r$ | `RiserCount` | $3 \le n_r \le 25$ (typ. 9–13) | Total number of risers in the flight run. |
| $h_r$ | `RiserHeightMm` | $140.0 - 185.0\text{ mm}$ (typ. 160) | Vertical rise of each step. |
| $n_t$ | `TreadCount` | $n_r - 1$ | Number of horizontal tread goings. |
| $b_t$ | `TreadWidthMm` | $250.0 - 320.0\text{ mm}$ (typ. 280) | Horizontal depth of each tread. |
| $L_{flight}$ | `FlightRunLengthMm` | $n_t \times b_t$ | Horizontal projection of the flight run. |
| $H_{flight}$ | `FlightTotalHeightMm`| $n_r \times h_r$ | Total vertical elevation gain of the flight. |
| $\alpha$ | `SlopeAngleRad` | $\arctan(h_r / b_t) \approx 25^\circ - 38^\circ$ | Inclination angle of the flight. |
| $L_{slope}$| `SlopeLengthMm` | $\sqrt{L_{flight}^2 + H_{flight}^2} = L_{flight} / \cos \alpha$ | Inclined travel distance of flight waist. |
| $t_w$ | `ThroatThicknessMm` | $100.0 - 180.0\text{ mm}$ (typ. 120–150) | Perpendicular waist slab thickness. |
| $t_v$ | `VerticalWaistMm` | $t_w / \cos \alpha$ | Vertical projection thickness of waist slab. |
| $L_{land1}$| `BottomLandingLengthMm`| $800.0 - 2500.0\text{ mm}$ (typ. 1200) | Clear length of lower landing / floor slab. |
| $L_{land2}$| `TopLandingLengthMm` | $800.0 - 2500.0\text{ mm}$ (typ. 1200) | Clear length of upper landing / floor slab. |
| $t_{land1}$| `BottomLandingThkMm` | $100.0 - 200.0\text{ mm}$ (typ. 120–150) | Thickness of lower landing slab. |
| $t_{land2}$| `TopLandingThkMm` | $100.0 - 200.0\text{ mm}$ (typ. 120–150) | Thickness of upper landing slab. |
| $W_f$ | `FlightWidthMm` | $800.0 - 2400.0\text{ mm}$ (typ. 1100) | Transverse width of stair flight. |
| $c$ | `ConcreteCoverMm` | $15.0 - 25.0\text{ mm}$ (typ. 15) | Concrete cover to rebar centroid. |
| $h_{first}$| `FirstRiserHeightMm`| $h_r - \Delta_{\text{finish}}$ | Riser adjusted for ground floor architectural finish. |
| $h_{last}$ | `LastRiserHeightMm` | $h_r + \Delta_{\text{finish}}$ | Riser adjusted for landing architectural finish. |

#### Architectural Comfort Validation (TCVN 5574 & Blondel Rule):
$$\text{Ergonomic Metric}: \quad 2 h_r + b_t \in [600\text{ mm}, 640\text{ mm}]$$
The calculator issues a validation warning if $2 h_r + b_t < 580$ or $> 660\text{ mm}$, preventing un-ergonomic stair construction.

---

### 2.2 Concrete Boundary Polygon & Coordinate Transformation

The concrete boundary of the flight in longitudinal section (elevation) is defined as a closed 2D polygon with vertices calculated relative to local flight origin $(0, 0)$ at the bottom-left corner of the lower landing:

1. **Lower Landing Bottom**:
   $$P_0 = (0, 0), \quad P_1 = (L_{land1}, 0)$$
2. **Inclined Soffit Junction (Lower Knee)**:
   The inclined soffit plane equation is:
   $$y - 0 = \tan \alpha \cdot (x - x_{\text{knee\_bot}})$$
   where $x_{\text{knee\_bot}}$ is the intersection between the bottom horizontal soffit $y = 0$ and the inclined soffit $y = \tan \alpha \cdot x - t_v$.
   $$P_{\text{knee\_bot}} = (L_{land1} - t_w \tan(\alpha/2), 0)$$
3. **Inclined Soffit Line to Upper Knee**:
   Extends along the slope up to the top landing soffit $y = H_{flight} + t_{land1} - t_{land2}$:
   $$P_{\text{knee\_top}} = \left( L_{land1} + L_{flight} + t_w \tan(\alpha/2), \; H_{flight} + t_{land1} - t_{land2} \right)$$
4. **Upper Landing Bottom**:
   $$P_{\text{top\_bot}} = \left( L_{land1} + L_{flight} + L_{land2}, \; H_{flight} + t_{land1} - t_{land2} \right)$$
5. **Upper Landing End & Top Surface**:
   $$P_{\text{top\_right}} = \left( L_{land1} + L_{flight} + L_{land2}, \; H_{flight} + t_{land1} \right)$$
   $$P_{\text{top\_nosing}} = \left( L_{land1} + L_{flight}, \; H_{flight} + t_{land1} \right)$$
6. **Stepped Tread/Riser Nosing Profile**:
   For $i = n_r$ down to 1:
   $$x_i = L_{land1} + (i - 1) b_t, \quad y_i = t_{land1} + i \cdot h_r$$
   - Tread horizontal segment: $(x_i + b_t, y_i) \to (x_i, y_i)$
   - Riser vertical segment: $(x_i, y_i) \to (x_i, y_i - h_r)$
7. **Lower Landing Top Surface**:
   From $(L_{land1}, t_{land1})$ horizontally to $(0, t_{land1})$, then vertically down to $P_0(0, 0)$.

---

### 2.3 Reinforcement Layout & The Scissor Crank Rule

Reinforcement in a stair flight experiences high positive bending moment at midspan, negative bending moments over landing supports, and dangerous radial tension forces at re-entrant corners.

```
       Top Landing                        Top Negative Bar (with 90° hook)
       =============================+===================
                                    |     \
                                    |      \
                                    |       \  Bottom bar of flight extends straight
       Scissor Crank Crossing       |        \ into top zone of landing (L_ext >= 30d)
       ---------------------->      X         \
                                   / \         \
       Bottom bar of landing      /   \         \
       extends into top of       /     \         \  Bottom Continuous Tension Bar
       inclined flight          /       \         \ (runs along inclined soffit)
                               /         \         \
                              /           \=========\====================
                                                     Bottom Landing
```

#### Detailing Rules & Bar Classifications:
1. **Bottom Continuous Tension Bars (`BottomTensionBar`)**:
   - Location: Along the inclined soffit, spaced at $a = 150 - 200\text{ mm}$ (diameter $\phi 10 - \phi 14$).
   - Lower Junction (Obtuse angle at soffit $> 180^\circ$): Bar bends continuously from the lower landing soffit into the flight with a bend radius $R \ge 4d$. Anchorage into lower landing beam $L_{anc} \ge 30d$.
   - **Re-entrant Corner Scissor Crank Rule (CRITICAL)**:
     - At the junction between the flight and the upper landing, the soffit angle is obtuse outside, but the interior concrete angle is re-entrant (góc lõm).
     - **Structural Requirement**: Tensile steel MUST NEVER be bent around a re-entrant corner because the tension resultant force kicks outwards, blowing out the concrete cover ($R_{\text{radial}} = 2 T \sin(\theta/2)$).
     - **Implementation**: The bottom bar of the inclined flight continues straight upward across the landing depth into the top compression zone of the landing, extending an anchorage distance $L_{ext} \ge 35d$ past the intersection.
     - The bottom bar of the upper landing continues across into the top zone of the inclined flight, extending $L_{ext} \ge 35d$ down the flight.
     - The two bars cross in an **X-shaped scissor crank** (`ThepNoiVe1Dang04`), providing complete confinement to the corner.
2. **Top Negative Support Bars (`TopNegativeBar`)**:
   - Resists negative support moments at the top and bottom support beams.
   - At upper landing: Starts with a $90^\circ$ hook ($12d$ downward) into the support beam, runs along the top face of the landing, and extends into the inclined flight by a cutoff distance $L_{cut} = \max(L_{slope} / 4, \; 40d)$.
   - Curtailment: Optional cutoff at $L/4$ when `CurtailExtraBars = true` (Kata `cat_thep`).
3. **Step Transverse Distribution Bars (`StepTransverseBar`)**:
   - Continuous horizontal distribution bars ($\phi 6$ or $\phi 8$) running across the width of the flight ($W_f - 2c$).
   - Spaced at $a \approx 200\text{ mm}$ along the soffit and 1 bar positioned at each step nose ($n_r$ bars).
   - In `DangThepBacThangEnum.Dang25` mode: Generates individual triangular stirrups (Shape 25) confining each step nose to prevent edge chipping.

---

## 3. Lintel Detailing Architecture & Mathematical Models (`LintelModel`, `velanhto`)

### 3.1 Clear Span & Bearing Mechanics
Lintels (`velanhto`) span masonry openings for doors and windows, supporting masonry wall weight and floor slab loads above.

```
       +----------------------- L_total = L0 + 2*L_bear -----------------------+
       |                                                                       |
       |  <- L_bear ->                                           <- L_bear ->  |
       +---------------+                                       +---------------+
       | Bearing Zone  |           Clear Opening Span          | Bearing Zone  |
       |  (Masonry)    |                  L_0                  |  (Masonry)    |
       |               |                                       |               |
       |  Masonry      |               OPENING                 |  Masonry      |
       |  Wall Jamb    |             (Door/Window)             |  Wall Jamb    |
       |               |                                       |               |
```

#### Geometric Attributes:
| Parameter | Field Name | Typical Range | Description |
|---|---|---|---|
| $L_0$ | `ClearSpanMm` | $600.0 - 3000.0\text{ mm}$ | Clear wall opening width. |
| $L_{bear}$| `BearingLengthMm` | $\ge 200.0\text{ mm}$ (typ. $250.0$) | Minimum bearing on masonry wall jamb. |
| $L_{total}$| `TotalLengthMm` | $L_0 + 2 \times L_{bear}$ | Out-to-out length of concrete lintel beam. |
| $b$ | `WidthMm` | $110.0\text{ mm}$ or $220.0\text{ mm}$ | Matches brick wall thickness ($100\text{ mm}$ or $200\text{ mm}$). |
| $h$ | `HeightMm` | $100.0 - 300.0\text{ mm}$ | Lintel depth based on span ($h \approx L_0 / 10$). |
| $c$ | `ConcreteCoverMm` | $15.0 - 25.0\text{ mm}$ (typ. $20.0$) | Concrete cover to stirrup outer face. |

---

### 3.2 Longitudinal Bar Detailing & End Hooks
- **Top Longitudinal Hanger Bars**: $2\phi 10$ or $2\phi 12$ running full length with $90^\circ$ downward hooks at both ends:
  $$L_{\text{bar\_top}} = (L_{total} - 2c) + 2 \times (h - 2c)$$
- **Bottom Longitudinal Tensile Bars**: $2\phi 10$, $2\phi 12$, or $2\phi 14$ running full length with $90^\circ$ upward hooks at both ends:
  $$L_{\text{bar\_bot}} = (L_{total} - 2c) + 2 \times (h - 2c)$$
- Hooks provide complete development length into the $250\text{ mm}$ masonry bearing pads.

---

### 3.3 Closed Stirrup Hoops & Zone Distribution
Closed rectangular stirrups (Kata Shape 00 / Shape 11) with $135^\circ$ seismic/anchorage hooks ($6d$ or $50\text{ mm}$ hook extension):
- Stirrup width: $b_{st} = b - 2c$
- Stirrup height: $h_{st} = h - 2c$
- Stirrup cutting length:
  $$L_{st} = 2(b_{st} + h_{st}) + 2 \times 6d_{\text{stirrup}} + 8d_{\text{stirrup}}$$

#### Distribution Zones:
1. **Support Bearing Zones** (left and right, length $L_{bear} + L_0/4$):
   - Spacing: $s_{end} \le 100\text{ mm}$ (dense shear reinforcement over wall bearings).
   - Count: $n_{\text{end}} = \lfloor (L_{bear} + L_0/4 - 50) / s_{end} \rfloor + 1$.
2. **Midspan Zone** (length $L_0 / 2$):
   - Spacing: $s_{mid} \le 150 - 200\text{ mm}$.
   - Count: $n_{mid} = \lfloor (L_0 / 2) / s_{mid} \rfloor + 1$.
3. Total stirrups per lintel: $N_{st} = 2 n_{\text{end}} + n_{mid}$.

---

## 4. Generic Cross-Section Detailing Engine (`GenericSectionModel`, `vemc`)

### 4.1 Cross-Cutting Section Cut Mechanics
The `vemc` engine (`tao_MC_cau_kien1`) provides a universal section-cut generator that operates across beams, columns, shear walls, stairs, and lintels.

```
       +----------------------- Width b -----------------------+
       |                                                       |
       |   +-----------------------------------------------+   |
       |   | o                   o                   o     |   |
       |   |                                               |   |
       |   |                                               |   |
     H |   | |                   |                   |     |   |
     e |   | |                   |                   |     |   |
     i |   |                                               |   |
     g |   |                                               |   |
     h |   | o                   o                   o     |   |
     t |   +-----------------------------------------------+   |
     h |                                                       |
       +-------------------------------------------------------+
                                  1 - 1
                                ( 1 : 20 )
```

#### Domain Model (`GenericSectionModel`):
- `SectionTitle`: String (e.g. "MẶT CẮT 1-1", "MẶT CẮT A-A").
- `ScaleRatio`: Integer denominator (e.g. 20 for 1:20, 25 for 1:25).
- `BoundaryPolygon`: Closed list of 2D points representing the concrete outline.
- `HatchPattern`: ANSI32 or AR-CONC with scale and rotation.
- `LongitudinalBars`: List of `SectionBarPointModel` $(X, Y, \text{DiameterMm}, \text{BarMark}, \text{LayerIndex})$.
- `StirrupHoops`: List of `SectionHoopModel` (ordered vertices, diameter, hook angles $90^\circ/135^\circ$, mark).
- `DimensionChains`: Horizontal and vertical associative dimension offsets.
- `CalloutTags`: Leader line references pointing from bar marks to rebar points.

---

## 5. AutoCAD Commands & DWG XRecord Persistence

### 5.1 Command Signatures & Lisp Aliases
The AutoCAD integration exposes 3 primary commands and their aliases matching `C:\kata_pro\Goi lenh.lsp`:

| Lisp Alias | CommandMethod | Backing Controller | Description |
|---|---|---|---|
| `vethang` | `ve_thang`, `vethang` | `StairDrawController.DrawStairInteractive()` | Prompts user or shows WPF dialog, draws stair flight elevation, section, rebar tags, and dimension lines. |
| `velanhto`| `velanhto` | `LintelDrawController.DrawLintelInteractive()` | Prompts clear span and wall thickness, draws lintel elevation, section, hooks, and stirrups. |
| `vemc` | `vemc` | `SectionCutController.DrawGenericSectionInteractive()` | Universal section cut prompt across beam, column, wall, or stair. |

---

### 5.2 Interactive CAD Controllers & Block Placement
The CAD controllers follow the proven pattern established in `BeamSectionController.cs`:
1. Acquire editor insertion point via `Editor.GetPoint()`.
2. Lock document, open Transaction.
3. Access `BlockTableRecord.ModelSpace`.
4. Append lightweight polylines for concrete outline (Layer `KATA_CONCRETE`, Color ByLayer).
5. Append hatched circles for longitudinal rebars (Layer `KATA_REBAR`, Color Red/Cyan).
6. Append polylines for stirrups and ties (Layer `KATA_STIRRUP`, Color Green).
7. Insert dynamic blocks from `Iso_kata.dwg`:
   - `TAG_THEP` for bar mark bubbles.
   - `KATA_MC` for section title bubbles.
   - Linear dimensions via `RotatedDimension` on Layer `KATA_DIM`.
8. Commit transaction and echo success message.

---

### 5.3 Extension Dictionary Schemas (`KATA_STAIR_DATA`, `KATA_LINTEL_DATA`)
To ensure full two-way round-trip compatibility, the domain models serialize to JSON payloads stored within the DWG object's Extension Dictionary:

```
BlockReference / Polyline (Stair / Lintel entity)
 └── ExtensionDictionary
      ├── XRecord "KATA_STAIR_DATA"   --> JSON payload of StairFlightModel
      └── XRecord "KATA_LINTEL_DATA"  --> JSON payload of LintelModel
```

`StairXRecordSerializer` and `LintelXRecordSerializer` implement:
```csharp
public static string SerializeToJson(StairFlightModel model);
public static StairFlightModel? DeserializeFromJson(string json);
```

---

## 6. Revit 2026 Structural Detailing Services (`OST_Stairs`)

### 6.1 `StairRebarGenerator.cs` API & Geometry Mapping
In Revit 2026 (.NET 8), stairs are represented by the `Stairs` element (Category `OST_Stairs`), containing constituent `StairsRun` and `StairsLanding` objects.

```
Revit Document
 └── Stairs (OST_Stairs)
      ├── StairsRun 1 (Flight 1: Risers, Treads, Throat, Base/Top Elevation)
      ├── StairsLanding (Mid-landing: Thickness, Elevation)
      └── StairsRun 2 (Flight 2)
```

`StairRebarGenerator.cs` provides:
```csharp
public static List<Rebar> GenerateStairRebar(
    Document doc, Element hostStairs, StairFlightModel model);
```

### 6.2 3D Rebar Curve Generation & Hook Resolution
1. **Geometry Extraction**: Reads `ActualRisersNumber`, `ActualRiserHeight`, `ActualTreadDepth`, and `ActualRunWidth` from `StairsRun`.
2. **Coordinate Orientation**: Computes run direction unit vector $\vec{u}$, normal vector $\vec{n} = \vec{u} \times \vec{z}$, and vertical $\vec{z} = (0, 0, 1)$.
3. **Curve Factory**: Generates 3D curves (`Line` segments):
   - Soffit curves with concrete cover offset $c$.
   - Scissor crank crossing lines extending $35d$ across the re-entrant landing corner.
   - Top negative curves with $90^\circ$ downward hooks.
   - Transverse distribution bars spanning across $(W_f - 2c)$ along $\vec{n}$.
4. **Native Instantiation**:
   ```csharp
   Rebar.CreateFromCurves(
       doc,
       RebarStyle.Standard,
       barType,
       startHook,
       endHook,
       hostStairs,
       norm,
       curves,
       RebarHookOrientation.Right,
       RebarHookOrientation.Left,
       true,
       true);
   ```

### 6.3 Idempotency & Clean-up Protocol
Before rebar creation, `StairRebarGenerator.CleanExistingStairRebar(doc, hostStairs, model.StairMark)` queries all existing `Rebar` elements where:
$$\text{Rebar.GetHostId()} == \text{hostStairs.Id} \quad \text{AND} \quad \text{Comments.StartsWith}(\text{"HPKata_Stair\_"})$$
and purges them inside the active `Transaction`, preventing duplicate rebar accumulation.

---

## 7. Excel Rebar Scheduling & ClosedXML Export

### 7.1 `StairScheduleAggregator.cs` Mapping to `RebarItem`
`StairScheduleAggregator.AggregateSchedule(StairFlightModel flight, int memberCount)` maps each stair reinforcement component into standard `RebarItem` instances:

| Component | Shape Code | Description | Cutting Length Formula |
|---|---|---|---|
| Bottom Main Tension | `01` / `02` | Continuous bar along inclined soffit into landing | $L_{slope} + L_{land1} + L_{ext} - 2c$ |
| Scissor Crank Bar | `04` | Hairpin / scissor crank at re-entrant corner | $L_{lap} + t_w / \sin \alpha + L_{ext}$ |
| Top Negative Bar | `02` | Negative moment top bar over support beam | $L_{slope}/4 + L_{land2} + (h_{beam} - 2c)$ |
| Transverse Bars | `01` | Straight distribution bars across flight width | $W_f - 2c$ |
| Step Edge Bars | `01` / `25` | Step nosing crack control bars | $W_f - 2c$ (or triangular hoop) |

### 7.2 ClosedXML Workbook Generator for Sheet `Thang`
`StairPlanScheduleWorkbookGenerator.cs` writes a formatted `.xlsx` workbook containing sheet `Thang`:
- Header block: Project Name, Stair Mark, Story, Scale, Concrete Grade, Steel Grade.
- Table columns: `STT`, `Tên CK`, `Hình dạng`, `Đk (mm)`, `Chiều dài (mm)`, `Số lượng / CK`, `Số CK`, `Tổng số lượng`, `Tổng chiều dài (m)`, `Khối lượng đơn vị (kg/m)`, `Tổng khối lượng (kg)`.
- Barem weight summary grouped by diameter bins:
  - $\phi \le 10\text{ mm}$ (Thép đường kính nhỏ $\le 10$)
  - $10 < \phi \le 18\text{ mm}$ (Thép đường kính $12 - 18$)
  - $\phi > 18\text{ mm}$ (Thép đường kính lớn $\ge 20$)
  - Total weight and percentage.

---

## 8. Clean Code Governance, Class Hierarchy & Line-Budget Enforcing

To comply with `docs/clean-code/HP_CLEAN_CODE_CORE.md`, every file must satisfy:
- **Rule C2**: File line budget $\le 300$ physical lines.
- **Rule M3**: Method line budget $\le 50$ physical lines.
- **Rule Q-B1**: Zero commented-out/dead code.
- **Rule Q-B2**: Zero empty catch blocks without reason comments.
- **Host Neutrality**: Zero AutoCAD or Revit references in `HPKata.Core`.

### Complete Class Layout & Planned Line Budgets:

```
HPKata/
├── HPKata.Core/
│   └── Stairs/
│       ├── Models/
│       │   ├── StairFlightModel.cs            [110 lines] -> Flight geometry & validation
│       │   ├── StairReinforcementModel.cs     [ 95 lines] -> Longitudinal, scissor, step bars
│       │   ├── LintelModel.cs                 [ 90 lines] -> Lintel geometry & rebar layout
│       │   └── GenericSectionModel.cs         [105 lines] -> Cut geometry, bar points, hoops
│       ├── Calculators/
│       │   ├── StairGeometryCalculator.cs     [140 lines] -> Polygon profile, nosings, slope
│       │   ├── StairRebarCalculator.cs        [185 lines] -> Tension, scissor crank, top bars
│       │   ├── LintelRebarCalculator.cs       [125 lines] -> Lintel bar lengths, stirrup zones
│       │   └── GenericSectionCalculator.cs    [135 lines] -> Section cut solver across elements
│       └── Persistence/
│           ├── StairXRecordSerializer.cs      [ 45 lines] -> JSON XRecord "KATA_STAIR_DATA"
│           ├── LintelXRecordSerializer.cs     [ 45 lines] -> JSON XRecord "KATA_LINTEL_DATA"
│           ├── StairScheduleAggregator.cs     [155 lines] -> Maps to List<RebarItem>
│           └── StairPlanScheduleWorkbookGenerator.cs [160 lines] -> ClosedXML sheet "Thang"
│
├── HPKata.AutoCad/
│   ├── Commands/
│   │   └── StairLintelCommands.cs             [ 45 lines] -> [CommandMethod] ve_thang, velanhto, vemc
│   └── Controllers/
│       ├── StairDrawController.cs             [180 lines] -> Interactive stair CAD drafting
│       ├── LintelDrawController.cs            [140 lines] -> Interactive lintel CAD drafting
│       └── SectionCutController.cs            [160 lines] -> Universal vemc CAD drafting
│
├── HPKata.Revit/
│   ├── Commands/
│   │   └── StairRebarCommands.cs              [ 75 lines] -> Revit command for OST_Stairs
│   └── Services/
│       ├── StairRebarGenerator.cs             [210 lines] -> Native Rebar creation in OST_Stairs
│       └── LintelRebarGenerator.cs            [150 lines] -> Native Rebar in lintel beams
│
├── HPKata.UI/
│   ├── ViewModels/
│   │   ├── StairDetailingViewModel.cs         [180 lines] -> Observable flight parameters
│   │   └── LintelDetailingViewModel.cs        [120 lines] -> Observable lintel parameters
│   └── Views/
│       ├── StairDetailingWindow.xaml / .cs    [ 80 lines] -> Dark/Light theme WPF dialog
│       └── LintelDetailingWindow.xaml / .cs   [ 70 lines] -> Dark/Light theme WPF dialog
│
└── HPKata.Core.Tests/
    └── Stairs/
        ├── StairGeometryCalculatorTests.cs    [150 lines] -> Profile & comfort validation
        ├── StairRebarCalculatorTests.cs       [180 lines] -> Scissor crank & tension cuts
        ├── LintelRebarCalculatorTests.cs      [140 lines] -> Span, bearing, stirrup zones
        ├── GenericSectionCalculatorTests.cs   [130 lines] -> Section cut solvers
        └── StairPersistenceTests.cs           [150 lines] -> XRecord & ClosedXML verification
```

**Total Planned Files**: 23 new files across 5 projects.  
**Maximum Planned Lines**: 210 lines (`StairRebarGenerator.cs`), well under the 300-line ceiling (Rule C2).

---

## 9. Comprehensive Test Plan & Verification Strategy

### 9.1 Unit Test Coverage in `HPKata.Core.Tests`
Minimum 50 comprehensive unit tests covering:
1. **Geometry Tests** (`StairGeometryCalculatorTests`):
   - Slope angle and slope length calculations across standard inclinations ($26^\circ - 35^\circ$).
   - Boundary polygon closing and vertex ordering.
   - Blondel ergonomic formula validation ($2h + b \in [600, 640]$).
2. **Stair Rebar Tests** (`StairRebarCalculatorTests`):
   - Continuous bottom tension bar length and cover offsets.
   - Scissor crank bar geometry at re-entrant corner: verifies extension $L_{ext} \ge 35d$ past junction.
   - Top negative bar cutoff lengths ($L/4$ and $L/3$).
   - Step nose transverse bar count equals riser count $n_r$.
3. **Lintel Tests** (`LintelRebarCalculatorTests`):
   - Out-to-out length $L_{total} = L_0 + 2 L_{bear}$ for standard spans ($900, 1200, 1500, 2000\text{ mm}$).
   - Top and bottom bar lengths with $90^\circ$ hooks.
   - Stirrup distribution: dense bearing zones $\le 100\text{ mm}$ vs sparse midspan zone $\le 200\text{ mm}$.
4. **Section Cut Tests** (`GenericSectionCalculatorTests`):
   - Correct bar coordinates and layer spacing in beam, column, wall, and stair section cuts.
   - Associative dimension chain lengths equal section outer dimensions.
5. **Persistence Tests** (`StairPersistenceTests`):
   - Round-trip JSON serialization equality via `StairXRecordSerializer` and `LintelXRecordSerializer`.
   - ClosedXML workbook generation produces valid non-empty byte stream with sheet `Thang` and populated barem weight tables.

### 9.2 Host Verification
- **AutoCAD 2026**: Verification of command methods `vethang`, `velanhto`, `vemc` via `GoiLenh_HPKata.lsp` syntax tests.
- **Revit 2026**: Roslyn/SDK compilation of `StairRebarGenerator.cs` against Revit API reference assemblies.

### 9.3 Concurrency & MTP Execution Rule
To prevent Windows Event ID 1025 / `Unknown Hard Error` popup crashes:
- Always run `dotnet test` with `--disable-build-servers -m:1`.
- When executing tests concurrently, supply `--results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`.

---

## 10. Conclusion & Next Steps
The Stair & Lintel domain model is fully specified and architected. It is ready for the Milestone M3 implementation workers:
- Worker can directly implement `HPKata.Core/Stairs/` models, calculators, and serializers.
- UI worker can bind `StairDetailingViewModel` and `LintelDetailingViewModel`.
- CAD and Revit workers can bind `StairDrawController`, `LintelDrawController`, and `StairRebarGenerator`.
- All test suites are predefined to guarantee 100% green builds.

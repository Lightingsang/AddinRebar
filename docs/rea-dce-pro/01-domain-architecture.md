# Domain Architecture & Algorithm Specification: Reinforced Concrete, Detailing & Geometry Core

**Deliverable Target**: `HPDcePro.Core`  
**Decompiled Baseline**: `C:\Dce Pro\DCE Pro.dll`, `C:\Dce Pro\DceAnalyssiss64.dll`, `C:\Dce Pro\V.2020\DCE Excel.dll`  
**Standard Compliance**: TCVN 5574:2018 (Concrete & Reinforced Concrete Structures), TCVN 10304:2025 (Piles & Deep Foundations)

---

## 1. Reinforced Concrete Beam Detailing & Calculation Architecture

### 1.1 Architectural Overview
The beam detailing ecosystem in DCE Pro bridges AutoCAD drawing primitives, Excel structural calculations, and ETABS finite-element models. It comprises three primary subsystems:
1. **Plan Framing Analysis (`Bea_Design_MatBangKetCau`)**: Extracts geometric framing networks from 2D CAD architectural and structural plans.
2. **Structural Sizing & Code Check (`DCE_Beam_Design` / `DCE_Beam_Function`)**: Computes flexure, shear, crack width, and deflection under TCVN 5574:2018.
3. **Shop Drawing Detailing (`DceShopBeam`, `DceDamMatCatMoi`, `DuAnVeDam`)**: Generates production-ready longitudinal elevations, stirrup zones, bar cutoffs, and cross-sections.

```
┌────────────────────────────────────────────────────────────────────────┐
│                        BEAM DETAILING PIPELINE                         │
│                                                                        │
│   Plan Lines / Polylines         Internal Forces (M, V, N)             │
│   (CAD Architectural Plan)       (ETABS Model or Excel Sheet)          │
│              │                               │                         │
│              ▼                               ▼                         │
│   ┌──────────────────────┐       ┌──────────────────────┐              │
│   │ Bea_Design_          │       │ DCE_Beam_Design      │              │
│   │ MatBangKetCau        │       │ (TCVN 5574:2018)     │              │
│   │ - Grid Intersections │       │ - Mu, Vu Capacity    │              │
│   │ - Span Discretization│       │ - acrc Crack Limits  │              │
│   │ - Column Face Offsets│       │ - Reinforcement As   │              │
│   └──────────┬───────────┘       └──────────┬───────────┘              │
│              │                              │                          │
│              └──────────────┬───────────────┘                          │
│                             ▼                                          │
│                ┌─────────────────────────┐                             │
│                │ DceShopBeam /           │                             │
│                │ DceDamMatCatMoi         │                             │
│                │ - Continuous Top/Bottom │                             │
│                │ - Extra Top L/4 Cutoff  │                             │
│                │ - Dense Stirrup L/4     │                             │
│                │ - 11.7m Lap Splices     │                             │
│                └────────────┬────────────┘                             │
│                             ▼                                          │
│                 CAD Detailing & Schedules                              │
└────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Framing Topology Extraction (`Bea_Design_MatBangKetCau`)
The framing extractor scans architectural and structural line geometry to construct topological continuous beam runs:
- **`BeamSupportData` & `ColumnData`**: Identifies supporting columns and primary intersecting girders. Snapping tolerances default to $\delta = 200\text{ mm}$ (configured via `.autostr` `SaiSoBatDiem`).
- **`UnionFind` & `MergedBeam`**: Employs disjoint-set union algorithms to merge collinear and contiguous beam segments sharing supporting nodes into a continuous structural member.
- **Clear Span Calculation**:
  $$L_0 = L_{center} - \frac{c_{left}}{2} - \frac{c_{right}}{2}$$
  where $c_{left}$ and $c_{right}$ represent column/girder support widths along the beam longitudinal axis.

### 1.3 TCVN 5574:2018 Structural Calculation Algorithms
Engineering equations implemented in `DCE_Beam_Function` and ported to `HPDcePro.Core.Calculations`:

#### Flexural Reinforcement Design ($M_u$)
For a rectangular cross-section $b \times h$ with effective depth $h_0 = h - a$:
1. **Relative Moment Coefficient**:
   $$\alpha_m = \frac{M}{R_b \cdot b \cdot h_0^2}$$
2. **Boundary Compression Zone Depth Ratio**:
   $$\xi_R = \frac{0.8}{1 + \frac{R_s}{\varepsilon_{b2} \cdot E_s}}$$
   where $\varepsilon_{b2} = 0.0035$ for normal-weight concrete, and $\alpha_R = \xi_R (1 - 0.5 \xi_R)$.
3. **Singly Reinforced Section ($\alpha_m \le \alpha_R$)**:
   $$\xi = 1 - \sqrt{1 - 2\alpha_m}$$
   $$A_s = \frac{\xi \cdot R_b \cdot b \cdot h_0}{R_s}$$
4. **Doubly Reinforced Section ($\alpha_m > \alpha_R$)**:
   $$A_{sc} = \frac{M - \alpha_R \cdot R_b \cdot b \cdot h_0^2}{R_{sc} (h_0 - a')}$$
   $$A_s = \frac{\xi_R \cdot R_b \cdot b \cdot h_0}{R_s} + A_{sc} \frac{R_{sc}}{R_s}$$
5. **Reinforcement Ratio Limits**:
   $$\mu = \frac{A_s}{b \cdot h_0} \times 100\% \ge \mu_{\min} = 0.10\%$$

#### Shear Capacity & Stirrup Spacing ($V_u$)
1. **Concrete Shear Strength**:
   $$V_b = \frac{\varphi_{b1} \cdot R_{bt} \cdot b \cdot h_0^2}{c} \quad \text{with } 0.5 R_{bt} b h_0 \le V_b \le 2.5 R_{bt} b h_0$$
2. **Stirrup Linear Resistance**:
   $$q_{sw} = \frac{R_{sw} \cdot A_{sw}}{s}$$
3. **Total Shear Resistance**:
   $$V_u = V_b + V_{sw} = V_b + 0.75 q_{sw} c$$
4. **Spacing Requirements**:
   - Near supports ($L_0 / 4$ zone): $s \le \min(h/2, 150\text{ mm})$
   - Mid-span ($L_0 / 2$ zone): $s \le \min(3h/4, 250\text{ mm}, 300\text{ mm})$

#### Crack Width Verification ($a_{crc}$)
$$a_{crc} = \varphi_1 \cdot \varphi_2 \cdot \varphi_3 \cdot \psi_s \cdot \frac{\sigma_s}{E_s} \cdot 20 \cdot \left(3.5 - 100\mu\right) \cdot \sqrt[3]{d} \le [a_{crc}]$$
where $[a_{crc}] = 0.3\text{ mm}$ for normal environmental exposure (Category 2).

### 1.4 Longitudinal Rebar Detailing Rules (`DceShopBeam`)
- **Continuous Main Bars**: Continuous top bars ($2\phi 18$–$2\phi 25$) and bottom bars ($2\phi 18$–$3\phi 25$) extend along the entire beam length.
- **Top Support Extra Reinforcement**: Extended across support faces by cutoff length:
  $$L_{cutoff, top} = \max\left(\frac{L_{0, left}}{4}, \frac{L_{0, right}}{4}\right) + L_{anchorage}$$
- **Bottom Mid-Span Extra Reinforcement**: Terminated at $0.10 L_0$ to $0.15 L_0$ from the clear column face.
- **Web Skin Reinforcement (Side Bars)**: When $h \ge 700\text{ mm}$, longitudinal torsion/skin reinforcement $\phi 12$–$\phi 14$ is inserted at vertical spacing $s_v \le 400\text{ mm}$.
- **Localized Reinforcement at Girder Intersections (`Beam_DaiGiaCuongDam`)**: Trapezoidal hanger reinforcement (quai treo) and localized dense stirrups placed on both sides of secondary beam intersections to resist vertical shear punch:
  $$A_{sh} = \frac{F_v (1 - h_s / h_m)}{R_{sw}}$$

---

## 2. Column & Shear Wall Detailing Architecture

### 2.1 Rectangular & Circular Columns (`DceColumn`, `Col_DLThietKe`)
- **Interaction Diagram P-M-M (`Col_DesignRecCol`)**: Solves non-linear strain equilibrium across discretised concrete fibers to construct ultimate capacity interaction envelopes under axial force $N$ and biaxial moments $M_x, M_y$.
- **Slender Column Buckling Magnification ($\eta$)**:
  $$\eta = \frac{1}{1 - \frac{N}{N_{cr}}} \quad \text{where } N_{cr} = \frac{\pi^2 D}{l_0^2}$$
  $$D = k_b E_b I_b + k_s E_s I_s$$
- **Multi-Story Detailing Rules**:
  - Longitudinal bar splices located in the middle third of story height ($H_n / 3$ to $2H_n / 3$), staggered across adjacent bars.
  - Crank bend offset ratio for vertical transition between stories: $1:6$ slope.
  - Seismic confinement tie zones: dense stirrup spacing $s \le \min(b/2, 100\text{ mm}, 10d)$ over height $H_c = \max(H_n/6, b, h, 500\text{ mm})$ from top and bottom story faces.

### 2.2 Core Shear Walls (`DceWall`, `DceWallCore`)
- **Boundary Elements (Cột đầu vách)**: Concentrated boundary zone rebar cages placed at wall extremities with confinement ties when compressive stress exceeds $0.2 R_b$.
- **Distributed Web Reinforcement**: Double vertical and horizontal mesh ($\phi 10$–$\phi 14$, $a150$–$a200$) with cross-ties (con bọ) at density $\ge 4\text{ ties/m}^2$.

---

## 3. Floor Slab Detailing Architecture

### 3.1 2-Way Slabs (`DceSlab`, `Sla_Calcsheets`)
- **Wood-Armer Moment Transformation**: Converts 2D plate finite element moments ($M_{xx}, M_{yy}, M_{xy}$) into design reinforcement moments:
  $$M_{dx} = M_{xx} + |M_{xy}| \quad \text{for bottom reinforcement}$$
  $$M_{dy} = M_{yy} + |M_{xy}| \quad \text{for bottom reinforcement}$$
- **Mesh Layout**:
  - Bottom continuous rebar: $\phi 8a150$ to $\phi 10a200$ running edge-to-edge into beam supports ($\ge 15d$ embedment).
  - Top negative rebar (mũ cột/mũ dầm): cantilevered out from beam edges by length $L_0 / 4$ or $L_0 / 3$.
  - Chair bars (thép kê mũ sàn): $\phi 10$ omega-shaped spacer chairs distributed at $1.0\text{ m} \times 1.0\text{ m}$ grid to preserve top concrete cover ($c_{nom} = 15$–$20\text{ mm}$).

---

## 4. Foundation & Pile Capacity Architecture

### 4.1 Pile Bearing Capacity under TCVN 10304:2025 (`DCE_Excel.TinhToanSCTCocSPT2025`)
Implements the revised 2025 Vietnamese deep foundation standard:
1. **Ultimate Axial Compressive Capacity**:
   $$R_{u} = R_b + R_s = q_b \cdot A_b + u \sum f_i \cdot l_i$$
2. **SPT-Correlated Unit Base Resistance ($q_b$)**:
   $$q_b = K_b \cdot N_{60, base} \quad (\text{kPa})$$
   where $K_b$ is the soil-type base coefficient, and $N_{60, base}$ is the average SPT blow count in the zone $3d$ above to $d$ below the pile tip.
3. **SPT-Correlated Unit Shaft Resistance ($f_i$)**:
   - Cohesive soils: $f_i = \alpha_i \cdot c_{u, i}$ or $f_i = 10 \cdot N_{60, i}^{0.6}$
   - Cohesionless soils: $f_i = K_s \cdot N_{60, i} \le 120\text{ kPa}$
4. **Design Capacity**:
   $$R_{c} = \frac{R_u}{\gamma_k \cdot \gamma_n}$$
   with material safety factor $\gamma_k = 1.4$ to $1.6$ and structural importance factor $\gamma_n = 1.15$ to $1.2$.

### 4.2 Pile Cap Detailing (`Mong_VeMongCoc`, `DLMongCoc`)
- Punching shear check of column through pile cap and corner piles through pile cap:
  $$P_{punch} \le 0.75 R_{bt} \cdot b_{pm} \cdot h_0$$
- Pile embedment depth into cap: $100\text{ mm}$ (configured via `.autostr` `MongNgamCoc`).
- Lean concrete blinding: $100\text{ mm}$ thick, extending $50\text{ mm}$ beyond footing perimeter.

---

## 5. Stairs & Lintel Detailing Architecture

### 5.1 Reinforced Concrete Stairs (`DceThangMoi`, `ThangMoi_Lenh`)
- Geometric parameters: Floor height $H$, flight width $B$, riser height $h_r$ ($150$–$180\text{ mm}$), tread width $b_t$ ($250$–$300\text{ mm}$), waist slab thickness $h_w$ ($100$–$150\text{ mm}$).
- Cranked reinforcement detailing at landing joints:
  - Top negative bar bends into landing slab and extends anchor length $L_{anc} \ge 35d$.
  - Bottom bar continues along waist slab, bends upward, and anchors into compression zone to avoid spalling at re-entrant corners.

### 5.2 Lintels (`VeBlockLanhTo`, `VeMatCatLanhTo`)
- Supported spans: Door and window openings from $800\text{ mm}$ to $3200\text{ mm}$.
- Precast or cast-in-place detailing: $2\phi 10$ to $2\phi 14$ main bars, closed stirrups $\phi 6a150$, bearing length on brick walls $\ge 250\text{ mm}$.

---

## 6. Rebar Schedule Aggregation & 1D Cutting Stock Optimization

### 6.1 1D Cutting Stock Optimization Problem (CSP)
In construction fabrication, steel rebars are delivered in standard commercial lengths of:
$$L_{stock} = 11,700\text{ mm} \quad (11.7\text{ m})$$
When a beam or column requires continuous lengths exceeding $11.7\text{ m}$, the optimizer partitions the run and introduces overlapping lap splices:
- Lap length in tension: $L_{lap} = 40 d$
- Lap length in compression: $L_{lap} = 30 d$

#### Mathematical CSP Formulation
Given a set of $m$ cutting demands $\{ (l_i, d_i) \}_{i=1}^m$ where each item requires length $l_i$ with demand quantity $d_i$:
$$\min \sum_{j=1}^n c_j x_j$$
subject to:
$$\sum_{j=1}^n a_{ij} x_j \ge d_i \quad \forall i \in \{1, \dots, m\}$$
$$\sum_{i=1}^m a_{ij} l_i \le L_{stock} \quad \forall j \in \{1, \dots, n\}$$
where $x_j$ is the number of commercial bars cut according to pattern $j$, and $a_{ij}$ is the count of cut piece $i$ yielded by pattern $j$.

### 6.2 Legacy Algorithm Analysis (`Bea_ToiUuCatThepV2`)
The legacy module `Bea_ToiUuCatThepV2` employs a multi-pass hybrid heuristic:
1. **Best-Fit Decreasing (BFD) Pre-Pass**: Sorts demand bars in descending length order.
2. **Dynamic Programming Knapsack Formulation**: For each commercial bar, finds the subset of demands that maximizes used length $\sum l_i \le 11700$.
3. **Scrap Recovery & Re-Use (`XDATA_APP_KHOTHEPDU`)**: Leftover pieces with length $l_{rem} \ge 1500\text{ mm}$ are indexed into a scrap inventory database and matched against short demands (e.g., column ties, footing starters, beam cutoffs).
4. **Target Scrap Rate**: Commercial fabrication efficiency achieves scrap waste ratios $< 3.0\%$.

---

## 7. The 13 CAD Extended Entity Data (XData) Schemas

DCE Pro embeds structured engineering parameters directly into AutoCAD drawing entities (Polylines, Blocks, Leaders, Dimensions) using 13 registered application names (`RegAppTable`).

```
┌────────────────────────────────────────────────────────────┐
│                    CAD ENTITY WITH XDATA                   │
│                                                            │
│   AcDbPolyline / AcDbBlockReference / AcDbDimension        │
│   ├── Standard Geometry (Vertices, Layer, Color)           │
│   └── Extended Entity Data (XData Dictionary)              │
│       ├── (1001 . "RebarXdataDCE")     <-- RegApp Name     │
│       │   ├── (1000 . "D1")            <-- Element Mark    │
│       │   ├── (1000 . "1a")            <-- Bar Mark        │
│       │   ├── (1000 . "20")            <-- Diameter        │
│       │   ├── (1040 . 11700.0)         <-- Total Length    │
│       │   └── ...                                          │
│       ├── (1001 . "DceRebarXDataTKT")  <-- Canonical Str   │
│       └── (1001 . "Links_XDataObject") <-- Entity Links    │
└────────────────────────────────────────────────────────────┘
```

### Detailed Registry & Wire Format Table

| # | RegApp Name | Target Entity Types | Purpose & Lifecycle | Payload Format |
|---|---|---|---|---|
| 1 | **`RebarXdataDCE`** | `AcDbPolyline`, `AcDbBlockReference` | Rebar geometric configuration, detailing mark, and dimensions. | Typed array `o(0)..o(11)` (see §7.1). |
| 2 | **`DceRebarXDataTKT`** | `AcDbPolyline`, `AcDbDimension`, `AcDbBlockReference` | Scheduling and cutting stock aggregation. | Canonical key-value string `SL=..;DK=..;...` |
| 3 | **`Links_XDataObject`** | `AcDbLeader`, `AcDbDimension`, `AcDbText` | Dynamic associative linkage between rebar graphics and annotations. | Pointers `(1005 . Handle)` + Field expressions. |
| 4 | **`Dce_ThepKep`** | `AcDbPolyline` | Bundled / layered rebars (thép kép). | Bar count, bundle spacing, bundle orientation. |
| 5 | **`XDATA_APP_SODOCAT`** | `AcDbPolyline`, `AcDbBlockReference` | 1D cutting stock layout diagrams on drawing. | Pattern ID, stock bar index, segment cut offsets. |
| 6 | **`XDATA_APP_KHOTHEPDU`** | `AcDbTable`, `AcDbBlockReference` | Inventory of reusable scrap bars (đề-xê). | Scrap ID, diameter, remnant length, warehouse tag. |
| 7 | **`XDATA_MAT_BANG_MONG`**| `AcDbPolyline`, `AcDbText` | Footing outline and boundary on foundation plan. | Footing type, top elevation, soil bearing capacity. |
| 8 | **`XDATA_MONG_COC`** | `AcDbBlockReference`, `AcDbCircle` | Pile cap geometry and pile head embedment. | Pile count, pile diameter, embedment depth, load. |
| 9 | **`SYS_XData_ThangMoi`** | `AcDbBlockReference`, `AcDbPolyline` | Staircase flight geometry and structural properties. | Flight number, riser count, riser height, tread width. |
| 10 | **`SYS_XData_App`** | `AcDbBlockReference` (Title Block) | Project-wide DCE versioning and unit standards. | App version, author, date, coordinate system. |
| 11 | **`NHAN_XDATA_COC`** | `AcDbCircle`, `AcDbBlockReference` | Individual pile coordination and design capacity. | Pile coordinates $(X, Y, Z)$, test load, pile number. |
| 12 | **`NHAN_XDATA_DAM_CONG`**| `AcDbArc`, `AcDbPolyline` | Curved and non-prismatic beam detailing parameters. | Arc center, radius, sweep angle, segmentation chord. |
| 13 | **`NHAN_XDATA_MB`** | `AcDbPolyline` (Framing Boundaries) | Floor level association and boundary polygon. | Story index, level name, floor elevation cos. |

### 7.1 Detailed Schema: `RebarXdataDCE`
Stored as an ordered sequence of DXF `TypedValue` records:
```
Index   DXF Code    Data Type   Field Name          Example Value
o(0)    1000        String      TenCauKien          "D1" (Beam Mark)
o(1)    1000        String      SoHieu              "1b" (Bar Number)
o(2)    1000        String      MaThep / LienKet    "/KC" or "a150"
o(3)    1000        String      DuongKinh           "20" (Diameter in mm)
o(4)    1000        String      SoLuong             "3" (Bar Quantity)
o(5)    1000        String      LoaiThep            "CTop" (Continuous Top)
o(6)    1040        Double      DoanA               250.0 (Hook/Leg A in mm)
o(7)    1040        Double      DoanB               5400.0 (Length B in mm)
o(8)    1040        Double      DoanC               250.0 (Hook/Leg C in mm)
o(9)    1040        Double      DoanD               0.0 (Hook/Leg D in mm)
o(10)   1040        Double      ChieuDaiL           5900.0 (Total Length in mm)
o(11)   1070        Short       HinhDang            1 (Standard Shape Code)
```

### 7.2 Detailed Schema: `DceRebarXDataTKT`
Canonical key-value string stored in DXF group code 1000:
```
SL={Quantity};DK={Diameter};KC1={LegA};KC2={LegB};KC3={LegC};KC4={LegD};SH={BarMark};L={Length}
```
*Example string payload*:
`SL=3;DK=20;KC1=250;KC2=5400;KC3=250;KC4=0;SH=1b;L=5900`

### 7.3 Detailed Schema: `Links_XDataObject`
Maintains dynamic associativity between drawing entities:
```
DXF Code    Data Type   Field Name          Description
1005        Handle      HandleThep          SoftPointer handle to rebar Polyline
1005        Handle      HandleTag           Handle to annotation block tag
1005        Handle      HandleDim           Handle to distribution dimension entity
1005        Handle      HandleLeader        Handle to pointing leader
1000        String      LuaChonField        AutoCAD Field formula expression
1070        Short       TrangThaiKhoa       Lock flag (0 = Unlocked, 1 = Locked)
```

---

## 8. Pure C# 2D Geometry Engine: Replacing Native `DceAnalyssiss64.dll`

The native C++ binary `DceAnalyssiss64.dll` exports **exactly 69 functions** using the `cdecl` calling convention. `HPDcePro.Core.Geometry` replaces all 69 native functions with pure, cross-platform, unit-testable C# .NET algorithms.

### 8.1 Functional Mapping Catalog (69 Exports to Pure C#)

#### 1. Computational Geometry (`Geom_*` — 12 Functions)
| Native C++ Export | Pure C# Replacement in `HPDcePro.Core.Geometry` | Algorithm / Description |
|---|---|---|
| `Geom_DistancePointToSegment` | `Segment2D.DistanceTo(Point2D pt)` | Vector projection clamped to segment parameter $t \in [0, 1]$. |
| `Geom_GetCirclePolygon` | `Polygon2D.CreateCircle(Point2D center, double r, int nSides)` | Discretization via $x_i = c_x + r \cos(2\pi i/n), y_i = c_y + r \sin(2\pi i/n)$. |
| `Geom_GetPolygonArea` | `Polygon2D.Area` | Shoelace formula $\frac{1}{2} |\sum (x_i y_{i+1} - x_{i+1} y_i)|$. |
| `Geom_GetPolygonCentroid` | `Polygon2D.Centroid` | Standard polygon moment centroid integration. |
| `Geom_GetSegmentLenInPolygon` | `Polygon2D.ClipSegmentLength(Segment2D seg)` | Sutherland-Hodgman / Liang-Barsky segment clipping against polygon boundary. |
| `Geom_IntersectLinesInfinite` | `Line2D.Intersect(Line2D other)` | Cramer's rule determinant intersection of 2D lines. |
| `Geom_IsPointInsidePolygon` | `Polygon2D.ContainsPoint(Point2D pt)` | Hybrid Ray-Casting & Winding Number point-in-polygon test. |
| `Geom_IsPolygonIntersectLine` | `Polygon2D.IntersectsLine(Line2D line)` | Edge-by-edge segment intersection test. |
| `Geom_IsPolygonOverlap` | `Polygon2D.Overlaps(Polygon2D other)` | Separating Axis Theorem (SAT) for convex/concave decomposition. |
| `Geom_ProjectPointToLine` | `Line2D.ProjectPoint(Point2D pt)` | Orthogonal projection onto line $(p - l_0) \cdot \hat{d}$. |
| `Geom_ProjectPointToLine2Pts` | `Segment2D.ProjectPoint(Point2D pt)` | Orthogonal projection onto segment defined by two endpoints. |
| `Geom_SegmentVsSegment` | `Segment2D.Intersect(Segment2D other, out Point2D pt)` | Strict 2D line segment intersection with collinear overlap detection. |

#### 2. Vector & Math Utilities (`DceMath_*` — 34 Functions)
| Native C++ Export | Pure C# Replacement in `HPDcePro.Core.Geometry` | Algorithm / Description |
|---|---|---|
| `DceMath_CheckBang0` | `MathUtils.IsZero(double val, double tol)` | Floating-point zero check with epsilon tolerance ($10^{-9}$). |
| `DceMath_CheckNamGiua` | `Segment2D.ContainsPointOnLine(Point2D pt)` | Segment bounding box and collinearity validation. |
| `DceMath_CheckNamGiuaDaGiac`| `Polygon2D.IsPointOnBoundary(Point2D pt)` | Boundary distance tolerance check. |
| `DceMath_CheckThangHang` | `MathUtils.AreCollinear(Point2D a, Point2D b, Point2D c)` | Cross product $|(b - a) \times (c - a)| < \epsilon$. |
| `DceMath_ConvexHull` | `Polygon2D.ConvexHull(IEnumerable<Point2D> pts)` | Andrew's monotone chain 2D convex hull algorithm ($O(n \log n)$). |
| `DceMath_DoDaiVector` | `Vector2D.Length` | Euclidean norm $\sqrt{x^2 + y^2}$. |
| `DceMath_DegToRad` | `MathUtils.DegToRad(double deg)` | $\text{deg} \times \pi / 180.0$. |
| `DceMath_RadToDeg` | `MathUtils.RadToDeg(double rad)` | $\text{rad} \times 180.0 / \pi$. |
| `DceMath_Goc2Vector` | `Vector2D.AngleBetween(Vector2D other)` | $\arccos\left(\frac{u \cdot v}{\|u\| \|v\|}\right)$. |
| `DceMath_VectoChiPhuong` | `Vector2D.Normalize()` | Direction unit vector $\hat{v} = v / \|v\|$. |
| `DceMath_VectorAngleOX` | `Vector2D.AngleToOX` | $\text{atan2}(y, x)$ normalized to $[0, 2\pi)$. |
| `DceMath_GiaiHePhuongTrinh` | `MathUtils.Solve2x2(a1, b1, c1, a2, b2, c2)` | Solves $2 \times 2$ linear system via Cramer determinants. |
| `DceMath_GiaiPTBac2` | `MathUtils.SolveQuadratic(a, b, c)` | Real roots of $a x^2 + b x + c = 0$. |
| `DceMath_GiaoDiem2DuongThang`| `Line2D.Intersect(Line2D other)` | 2D line intersection point. |
| `DceMath_GiaoDiemDTvaMP` | `Plane3D.IntersectLine(Line3D line)` | 3D line-plane intersection $t = \frac{-(n \cdot p_0 + d)}{n \cdot v}$. |
| `DceMath_PTMatPhang` | `Plane3D.From3Points(p1, p2, p3)` | 3D plane equation $Ax + By + Cz + D = 0$ via cross product. |
| `DceMath_TichDiemVaMatPhang`| `Plane3D.SignedDistance(Point3D pt)` | Signed distance $(n \cdot p + D)$. |
| `DceMath_KcDiemDenMatPhang` | `Plane3D.AbsoluteDistance(Point3D pt)` | Absolute perpendicular distance $|n \cdot p + D| / \|n\|$. |
| `DceMath_KhoangCach2Diem` | `Point2D.DistanceTo` / `Point3D.DistanceTo` | Euclidean distance between two points $\sqrt{\Delta x^2 + \Delta y^2 + \Delta z^2}$. |
| `DceMath_Goc2MatPhang` | `Plane3D.AngleBetween(Plane3D other)` | Dihedral angle between normal vectors. |
| `DceMath_LocalToGlobal` | `CoordinateTransform.LocalToGlobal` | 3D local-to-WCS affine coordinate transformation. |
| `DceMath_MidPoint` | `Point2D.MidPoint` / `Point3D.MidPoint` | Midpoint calculation between two coordinates $((x_1+x_2)/2, (y_1+y_2)/2)$. |
| `DceMath_NoiSuy1Chieu` | `MathUtils.LinearInterpolate(x[], y[], x0)` | 1D piecewise linear table interpolation. |
| `DceMath_NoiSuy2Chieu` | `MathUtils.BilinearInterpolate(grid, x0, y0)` | 2D bilinear surface interpolation. |
| `DceMath_Offset_x` | `Point2D.OffsetX(double dx)` | Coordinate translation along X axis ($x' = x + dx$). |
| `DceMath_Offset_y` | `Point2D.OffsetY(double dy)` | Coordinate translation along Y axis ($y' = y + dy$). |
| `DceMath_Offset_xy` | `Point2D.Offset(double dx, double dy)` | 2D translation offset vector ($x' = x + dx, y' = y + dy$). |
| `DceMath_PointInPolygonRayCast`| `Polygon2D.RayCastContains(Point2D pt)` | Standard Jordan curve horizontal ray-casting test. |
| `DceMath_PointInPolygonWinding`| `Polygon2D.WindingNumberContains(Point2D pt)` | Winding number summation test (supports self-intersecting loops). |
| `DceMath_PolarPoints` | `Point2D.Polar(Point2D origin, double dist, double ang)` | Polar translation $(x + r\cos\theta, y + r\sin\theta)$. |
| `DceMath_ProjectPointToInfiniteLine`| `Line2D.ProjectPoint(Point2D pt)` | Projection to infinite line. |
| `DceMath_QuayChieuKimDongHo` | `MathUtils.IsClockwise(Point2D a, Point2D b, Point2D c)` | Signed area cross product test (negative = clockwise). |
| `DceMath_TichCoHuong` | `Vector3D.Cross(Vector3D other)` | 3D cross product $u \times v$. |
| `DceMath_TichVoHuong` | `Vector2D.Dot(Vector2D other)` | Dot product $u \cdot v = u_x v_x + u_y v_y$. |

#### 3. Framing Plan Topology (`MBKC_*` — 9 Functions)
| Native C++ Export | Pure C# Replacement in `HPDcePro.Core.Geometry` | Algorithm / Description |
|---|---|---|
| `MBKC_AngleBetweenVectors` | `FramingTopology.AngleBetween(Vector2D v1, Vector2D v2)` | Angular difference between beam alignment axes. |
| `MBKC_AreBeamsConnected` | `FramingTopology.AreConnected(Segment2D b1, Segment2D b2)` | Node proximity within snapping tolerance. |
| `MBKC_CalculateCircumcircle`| `Triangulation.CalculateCircumcircle(p1, p2, p3)` | Triangle circumcenter and circumradius. |
| `MBKC_ContainsInCircumcircle`| `Triangulation.InCircumcircle(p1, p2, p3, testPt)` | Delaunay in-circle condition test. |
| `MBKC_GetBeamGroupDirection`| `FramingTopology.GetGroupDirection(IEnumerable<Segment2D>)` | Principal longitudinal axis of continuous beam spans. |
| `MBKC_GetBeamGroupRefPoint` | `FramingTopology.GetGroupOrigin(IEnumerable<Segment2D>)` | Leftmost/bottom-most anchor node of a continuous run. |
| `MBKC_GetNearestNodeWithinTol`| `SpatialNodeIndex.FindNearest(Point2D pt, double tol)` | $k$-d tree or grid spatial lookup for nearest column node. |
| `MBKC_GetTriangleArea` | `Triangulation.TriangleArea(p1, p2, p3)` | Area via cross product. |
| `MBKC_OrderBeamGroup` | `FramingTopology.SortContinuousSpans(IEnumerable<BeamSpan>)` | Topological ordering along continuous beam run. |

#### 4. Span & Math Parser (`TimSo*`, `KhoangCach*` — 10 Functions)
| Native C++ Export | Pure C# Replacement in `HPDcePro.Core.Geometry` | Algorithm / Description |
|---|---|---|
| `KhoangCachDenDiemTraiGoi` | `BeamSpan.DistanceToLeftSupportFace(Point2D pt)` | Scalar offset from left support column face. |
| `KhoangCachDenDiemPhaiGoi` | `BeamSpan.DistanceToRightSupportFace(Point2D pt)` | Scalar offset from right support column face. |
| `TimSoDoan` | `BeamSpan.ComputeSegmentCount(double length, double maxSeg)` | Segmental discretization of long beam spans. |
| `TimSoNhip` | `BeamContinuousRun.SpanCount` | Total span count of continuous girder. |
| `CalculateExpression` | `MathExpressionEvaluator.Evaluate(string expr)` | Pure C# shunting-yard / recursive descent math parser for algebraic expressions. |
| `CalculateExpression2` | `MathExpressionEvaluator.EvaluateWithContext(string expr, IDictionary<string, double> vars)` | Expression evaluation with variable context dictionary. |
| `CalculateExpression3` | `MathExpressionEvaluator.EvaluateParametric(string expr, double t)` | Parametric formula evaluation for step-dependent expressions. |
| `CalculateRatio` | `MathUtils.Ratio(double a, double b)` | Ratio calculation $a / b$ with divide-by-zero protection. |
| `CalculateRatio2` | `MathUtils.RatioPercent(double a, double b)` | Percentage ratio $(a / b) \times 100\%$ with clamp validation. |
| `HalfP` | `MathUtils.HalfPerimeter(double a, double b, double c)` | Semi-perimeter $s = (a + b + c) / 2$. |

#### 5. Anti-Tamper & Checksum Hooks (4 Functions to Exclude)
| Native C++ Export | Disposition in `HPDcePro` | Forensic Purpose in Legacy Binary |
|---|---|---|
| `CheckAssemblyIntegrity` | **Excluded entirely** | Native MD5 checksum probe validating `DCE Pro.dll` PE header. |
| `GenerateIntegrityFile` | **Excluded entirely** | Writes encrypted license verification artifact (`wnaspi32.dat`). |
| `MyFileD` | **Excluded entirely** | Obfuscated file handle and anti-dump decryption probe. |
| `dgfhfhcxdfg` | **Excluded entirely** | Obfuscated string table decryptor and anti-debugging sentinel. |

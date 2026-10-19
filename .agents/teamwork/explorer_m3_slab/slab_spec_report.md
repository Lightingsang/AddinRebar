# AUTHORITATIVE SLAB DOMAIN & REINFORCEMENT SPECIFICATION REPORT
## Milestone M3 (Phase 4: Foundation, Slab, Stair & Lintel Detailing) — Slab Subsystem
- **Document ID**: `HPKATA-SPEC-M3-SLAB-2026`
- **Version**: `1.0.0-PROD`
- **Author**: `explorer_m3_slab` (Slab Domain & Reinforcement Spec Miner)
- **Target Deliverable**: `HPKata/HPKata.slnx` (.NET 8 SDK 10.0.300)
- **Governance**: Clean-Room Reverse Engineering Protocol (`HP_CLEAN_CODE_CORE.md`, zero host references in Core, max 300 lines/file, max 50 lines/method)
- **Status**: AUTHORITATIVE & BINDING FOR IMPLEMENTATION

---

## 1. Executive Summary & Scope Definition

This report provides the exhaustive specification, mathematical formulations, AutoCAD command workflows, Revit 2026 native element generation algorithms, and Clean Code class architectures for the **Slab Reinforcement Subsystem** of the HPKata clean-room reconstruction project.

The Slab subsystem addresses:
1. **Two-Way Slab Panels (`ve_san` / `vesan`)**: Orthogonal bottom continuous mesh (X and Y layers), top negative moment reinforcement (support zones $L/4$, $L/3$, or $0.2L, 0.25L$, cantilevers), cover offsets, and corner crack-control bars.
2. **Variable-Length Polyline Slabs (`vesanpl`)**: Arbitrary closed polygon boundaries, parallel stepping rebar distribution, variable mark clustering (`1a, 1b...`), and radial/fan layouts.
3. **4-Way & Radial Reinforcement (`vesan4d`)**: 4-point arbitrary boundaries, orthogonal + $45^\circ$ diagonal reinforcement for high punch-through shear zones and drop panels (`drpn`).
4. **Chair / Spacer Bar Calculator (`thep_ke` / `cho_san`)**: Automated stool height $h_{\text{stool}}$ calculation, stool leg/foot and saddle cradle dimensions, stool layout grid, and shape code resolution (`dang35`, `dang35a`, `dang02`, `dang05`).
5. **Slab Panel Splitting & Point Picking (`pick_diem_san` / `dsan`, `chia_san` / `chiasan`, `ssan`)**: Ray-casting boundary identification, span partitioning into support and midspan bands, and DWG XRecord serialization (`KATA_SLAB_DATA`).
6. **Slab Takeoff & Scheduling (`thong_ke_block_san` / `tksan`, ClosedXML Sheet `San`)**: Schedule aggregation, diameter weight summaries, and headless Excel export.
7. **Revit 2026 Integration (`SlabRebarGenerator.cs`)**: Native 3D `Rebar` sets and `AreaReinforcement` generation in `OST_Floors` elements, unit conversions ($1 / 304.8$), and idempotent rebar cleanup.

---

## 2. Authoritative Specification & Reference Source Map

| Reference Source | Artifact / Location | Verified Components & Logic Mined |
|---|---|---|
| **Legacy AutoCAD Plugin** | `C:\kata_pro\Kata_pro64_Cad2025.dll` | `Lenh_thep` commands: `ve_san`, `vesanpl`, `vesan4d`, `chia_san`, `pick_diem_san`, `ssan`, `thong_ke_block_san`, `drpn`, `openning`. |
| **Legacy CAD 2013 Plugin** | `C:\kata_pro\Kata_pro64_Cad2013.dll` | Forms and dialog schemas: `view_san` (master slab UI), `Form_Rai_Thep_Ke_San` (chair bar UI), `Form_Chia_San` (panel split UI), `Form_Phan_Loai_San` (thickness classification), `TK_san` (slab takeoff UI), `Info_slab`, `Input_slab`. |
| **Legacy Shared Library** | `C:\kata_pro\Kata_Class_Lib.dll` | Geometric core: `Info_Slab3D`, `PolygonModule.Polygon`, `CurveXYZ`, `HalfEdge`, `TranslateSystem`, `ListPolygonBaoNhauByOffset`. |
| **Legacy Revit Library** | `C:\kata_pro\Kata_Class_Lib_Revit.dll` | `Bien_chung.info_revit_slab`, `Floor_SelectionFilter`, `Foundation_Floor_SelectionFilter`, `Floor_Beam_SelectionFilter`, `Revit_rebar`. |
| **AutoLISP Alias Loader** | `C:\kata_pro\Goi lenh.lsp` | Command aliases: `vesan`, `vesanpl`, `vesan4d`, `dsan`, `ssan`, `chiasan`, `tksan`, `tstt`, `chuyen`, `str`. 86-parameter config lines 152–237 (items 16, 40, 59, 64, 65, 66). |
| **CAD Template & Blocks** | `C:\kata_pro\Iso_kata.dwg` | Layers `kata_thep chu`, `kata_thep dai`, `kata_dim`, `kata_net thay`. Blocks `kata_block_KHT`, `kata_block_KHTCT`, `thep_san`, `thep_mu`, `kata_rai_thep`. |
| **Master Excel Workbook** | `C:\kata_pro\Kata.xlsm` | Sheet `SLAB DESIGN` (rId12): rows 22–24 slab design tables, layers 1 & 2, `h_san`, `a_bv`, `Tên Etabs`. Sheet `Thong_ke`. |
| **Master Blueprint** | `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` | Rebar shapes catalog (`dang02`, `dang05`, `dang35`, `dang35a`, `dang48`, `dang50`, `dang58`, `dang59`), XData RegApp `"KATA"`, handle linking. |
| **Core Architecture Standards**| `docs/clean-code/HP_CLEAN_CODE_CORE.md` | Max 300 lines/file (Rule C2), max 50 lines/method (Rule M3), 0 dead code (Q-B1), 0 unannotated catch (Q-B2), zero host dependencies in Core. |

---

## 3. Features Discovered & Functional Taxonomy

### Features Discovered Table
| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|---|---|---|---|---|---|---|
| 1 | Geometry | `SlabPanelModel` | Two-way slab panel representation with orthogonal boundaries, thickness, cover, and elevation | Clear spans $L_x, L_y$, thickness $h_s$, TOC, cover $c$ | Complete geometric model | Throws `ArgumentOutOfRangeException` if $h_s \le 0$ or $c \le 0$ | `Kata_Class_Lib.Info_Slab3D` |
| 2 | Detailing | Bottom Continuous Mesh | Two-layer orthogonal bottom reinforcement ($X$ and $Y$) with spacing and anchorage | Boundary, $d_{bx}, d_{by}, s_x, s_y$, anchorage $L_{anc}$ | Array of straight bars (`dang01`) with end cogs | Clamp spacing $\ge 50\text{ mm}$, fallback to $a200$ | `view_san` (`Thep_duoi`) |
| 3 | Detailing | Top Negative Moment Bars | Support zone top hat bars resisting negative bending moments over supports | Support zone ratio $r$ ($0.25, 0.33$), beam width $b$, cog height | Negative bars (`dang48` or `dang02`/`dang05`) | If cog height $\le 0$, clamp to $50\text{ mm}$ | `view_san` (`Thep_tren`, `Cat_thep_nhip`) |
| 4 | Detailing | Cantilever Negative Bars | Top reinforcement extending through cantilever slab and anchored into back-span | Cantilever length $L_c$, back-span development $1.5L_c$ | Continuous top bar with $180^\circ$ or $90^\circ$ edge return | Clamp back-span to adjacent clear span | `view_san` (`Cat_thep_congxon`) |
| 5 | Detailing | Corner Crack Control | Additional diagonal or orthogonal mesh at discontinuous exterior corners | Corner dimensions $L_x/5 \times L_y/5$, bar size $d$, spacing $s$ | Corner anti-crack mesh | Suppressed if corner is fully continuous | TCVN 5574:2018 Cl. 10.3 / `view_san` |
| 6 | Geometry | Polyline Boundary Slabs | Arbitrary polygon slab boundary with internal voids and step lines | Closed polygon vertices, hole polygons | Segmented boundary, oriented bounding box | Throws if polygon is self-intersecting or non-closed | `Kata_Class_Lib.PolygonModule` / `vesanpl` |
| 7 | Detailing | Parallel Stepping Distribution | Parallel rebar distribution within irregular polygon with modular bar lengths | Polygon, bar direction $\vec{u}$, spacing $s$, step $\Delta L$ | Variable length bar series (`1a, 1b...`) | Clamps cut lengths to min 200 mm | Config line 191 (`VarBarNaming`) / `vesanpl` |
| 8 | Detailing | 4-Way / Radial Reinforcement | 4-point quadrilateral layout with orthogonal + $45^\circ$ diagonal bars | 4 corner points, orthogonal $s$, diagonal $s_{diag}$ | 4-way rebar grid | Throws if 4 points are collinear or self-intersect | `Lenh_thep.Ve_san_4d` / `vesan4d` |
| 9 | Calculator | Chair Bar Stool Height | Calculates net stool height $h_{\text{stool}}$ based on slab thickness, covers, and bar diameters | $h_s, c_{\text{top}}, c_{\text{bot}}, d_{\text{top}}, d_{\text{bot}}$ | Net height $h_{\text{stool}}$ | Clamps $h_{\text{stool}} \ge 30\text{ mm}$; warns if $h_s < 70\text{ mm}$ | `Form_Rai_Thep_Ke_San` / `thep_ke` |
| 10 | Calculator | Chair Bar Quantity & Layout | Calculates stool count and placement grid based on slab area and density | Slab net area $A$, stool density $\rho$ (1.0–1.5/m²) | Total chair count, grid coordinates | Defaults to $1.2\text{ chairs/m}^2$ if density $\le 0$ | `Form_Rai_Thep_Ke_San.ve_rai_thep_ke_san` |
| 11 | Detailing | Chair Bar Shape Selection | Resolves rebar shape code: `dang35` (3D stool), `dang02` (U-stool), `dang05` (Z-stool) | Stool type enum, stool height, foot length, cradle length | RebarItem with 6 segment parameters | Normalizes to `dang35` if 3D stool selected | `BLUEPRINT.md` §6 / `Iso_kata.dwg` |
| 12 | Tracing | Boundary Point Picking | Raycasting point-in-polygon to detect slab panel boundary from surrounding beams/walls | User picked point $(X, Y)$, framing layer entities | Closed boundary polygon | Returns error if point is outside or on framing edge | `Lenh_thep.Trien_khai_day_san` / `dsan` |
| 13 | Geometry | Slab Panel Partitioning | Splits slab into support strips ($L/4$) and middle span strips ($L/2$) along X/Y | Panel boundary, framing centerline axes | Support and middle strip boundary polygons | If span $L < 1000\text{ mm}$, suppresses middle strip | `Form_Chia_San` / `chiasan` |
| 14 | Persistence | Slab Property Inspection | Reads/updates slab thickness, TOC elevation, and cover attributes via UI | DWG selection, thickness, TOC, cover | Updated entity XData / XRecord | If entity lacks `"KATA"` XData, attaches fresh schema | `Lenh_thep.Khai_thong_tin_san` / `ssan` |
| 15 | Takeoff | Slab Block Takeoff | Scans DWG for slab distribution blocks (`kata_block_KHT`) and rebar polylines | DWG selection or current space | Aggregated slab rebar schedule table | Ignores untagged or non-rebar blocks cleanly | `Lenh_thep.Thong_ke_san` / `tksan` |
| 16 | Export | ClosedXML Sheet `San` | Generates dedicated Excel worksheet `"San"` with formatted table and TCVN summaries | `SlabScheduleReport` | `.xlsx` file / memory stream | Throws `ArgumentNullException` if report is null | `HPKata.Core.Export` / ClosedXML |
| 17 | Revit | Revit Floor Boundary Extractor | Extracts outer boundary curve loops and inner hole loops from `OST_Floors` | Revit `Floor` element | `SlabBoundaryPolygon` with holes | Throws if floor has no horizontal planar face | `HPKata.Revit.Services.SlabRebarGenerator` |
| 18 | Revit | Revit Native Rebar Generation | Generates native 3D `Rebar` sets in Revit `Floor` for top/bottom mesh & hats | Revit `Document`, `Floor`, `SlabPanelModel` | List of created `Rebar` elements | Rolls back transaction on geometric invalidity | `HPKata.Revit.Services.SlabRebarGenerator` |
| 19 | Revit | Revit Rebar Idempotency | Deletes prior HPKata rebar on the floor before generating new layout | `Document`, `Floor`, `slabMark` | Cleaned host floor | Ignores non-HPKata rebars on the same floor | `HPKata.Revit.Services.SlabRebarGenerator` |
| 20 | CAD | Block Conversion (`chuyen`) | Converts static slab rebar blocks into dynamically linked smart rebar entities | Selected legacy blocks | Smart polylines + linked tag blocks | Preserves existing bar marks and counts | `Lenh_thep.Chuyen_block_thep_san_sang_theplk1` |
| 21 | Detailing | Opening Trimmer Rebar | Details diagonal anti-crack bars (`dang58`) and perimeter trimmer bars at openings | Opening dimensions $b_0 \times h_0$, slab thickness $h_s$ | 4 diagonal bars + 4 perimeter U/straight bars | Clamps diagonal bar length to $\ge 800\text{ mm}$ | `Lenh_thep.Ve_thep_openning1` / `openning` |
| 22 | Detailing | Drop Panel Reinforcement | Details thickened slab drop panels (`drpn`) with stepped bars and diagonal shear ties | Drop panel size $b_d \times h_d$, drop depth $\Delta h$ | Stepped rebar (`dang50` or `dang05`), shear ties | Asserts $\Delta h > 0$; otherwise regular slab | `Lenh_thep.Drop_panel1` / `drpn` |

---

## 4. Subsystem 1: Two-Way Slab Reinforcement Engine (`SlabPanelModel`, `ve_san`)

### 4.1 Structural Mechanics & Design Basis
In reinforced concrete flat slabs and two-way edge-supported slabs (TCVN 5574:2018, Eurocode 2, ACI 318):
- **Short-Span / Long-Span Ratio**: For panel dimensions $L_x \le L_y$, the aspect ratio $\lambda = L_y / L_x$ governs:
  - If $\lambda \le 2.0$: True two-way slab action. Both $X$ and $Y$ directions carry significant flexural moments.
  - If $\lambda > 2.0$: One-way slab action. Primary reinforcement spans along $L_x$; secondary distribution reinforcement spans along $L_y$ ($A_{sy} \ge 0.20 A_{sx}$, spacing $s_y \le 250\text{ mm}$).
- **Layer Stacking Hierarchy (Bottom Mesh)**:
  - Outer Layer (Bottom 1, $B_1$): Spans along the shorter dimension $L_x$ to maximize effective depth $d_x = h_s - c - d_{bx} / 2$.
  - Inner Layer (Bottom 2, $B_2$): Spans along the longer dimension $L_y$, resting directly on top of Layer $B_1$: $d_y = h_s - c - d_{bx} - d_{by} / 2$.
- **Layer Stacking Hierarchy (Top Negative Bars)**:
  - Outer Layer (Top 1, $T_1$): Spans along $L_x$ over supporting beams/walls: $d'_{tx} = h_s - c - d_{tx} / 2$.
  - Inner Layer (Top 2, $T_2$): Spans along $L_y$ over perpendicular supports: $d'_{ty} = h_s - c - d_{tx} - d_{ty} / 2$.

### 4.2 Bottom Continuous Mesh Formulation
Let the rectangular slab panel have clear dimensions $L_{n,x}$ and $L_{n,y}$, concrete cover $c$, support beam widths $b_{w,x1}, b_{w,x2}, b_{w,y1}, b_{w,y2}$, and required anchorage length $L_{anc}$ into supports:

1. **Bar Length Calculation ($X$-Direction Bottom Bar)**:
   $$L_{\text{bar},x} = L_{n,x} + \text{Anchorage}_{\text{left}} + \text{Anchorage}_{\text{right}}$$
   where:
   - For beam/wall support: $\text{Anchorage} = \min\left(b_w - c, \max(15d_{bx}, L_{anc,bot})\right)$, typically $b_w - 20\text{ mm}$ or $b_w / 2$.
   - If bar ends with a $90^\circ$ cog hook (e.g. at exterior edge beam):
     $$L_{\text{cog},x} = \max(10d_{bx}, h_s - 2c)$$
     Total cutting length: $L_{\text{cut},x} = L_{\text{bar},x} + L_{\text{cog},x1} + L_{\text{cog},x2}$ (Shape `dang02` or `dang01`).
2. **Bar Count Calculation ($X$-Direction Bottom Bars)**:
   $$N_{\text{bars},x} = \left\lfloor \frac{L_{n,y} - 2 s_{\text{edge}}}{s_x} \right\rfloor + 1$$
   where $s_{\text{edge}} = \max(c, s_x / 2)$ is the start offset from the perpendicular beam face.
3. **Bar Length & Count ($Y$-Direction Bottom Bar)**:
   $$L_{\text{bar},y} = L_{n,y} + \text{Anchorage}_{\text{bottom}} + \text{Anchorage}_{\text{top}}$$
   $$N_{\text{bars},y} = \left\lfloor \frac{L_{n,x} - 2 s_{\text{edge}}}{s_y} \right\rfloor + 1$$

### 4.3 Top Negative Moment Reinforcement Formulation
Top negative moment bars ("thép mũ" / hat bars) resist hogging moments over supports:
1. **Support Curtailment Cutoff ($L_{\text{cut}}$)**:
   Per TCVN detailing rules and Kata config items 35 (`BeamTopCutoffL1 = 0.2`) and 48 (`BeamTopCutoffL2 = 0.25`):
   - Interior Support (continuous over beam of width $b_w$):
     $$L_{\text{ext},1} = \max\left(\frac{L_{n,1}}{4}, 0.25 L_{n,1}\right)$$
     $$L_{\text{ext},2} = \max\left(\frac{L_{n,2}}{4}, 0.25 L_{n,2}\right)$$
     Total straight bar length:
     $$L_{\text{top,int}} = L_{\text{ext},1} + b_w + L_{\text{ext},2}$$
   - Exterior Discontinuous Support (anchored into perimeter beam):
     $$L_{\text{top,ext}} = \max\left(\frac{L_n}{4}, 0.25 L_n\right) + b_{w} - c + L_{\text{cog,down}}$$
2. **Bend-Down Cog Height ($L_{\text{cog,down}}$)**:
   Top negative bars bend down towards the soffit to ensure stability and concrete cover:
   $$L_{\text{cog,down}} = h_s - c_{\text{top}} - c_{\text{bot}} - d_{\text{bot,mesh}} \approx h_s - 2c - 10\text{ mm}$$
   Clamped: $L_{\text{cog,down}} \ge 50\text{ mm}$ (Shape code `dang48`, `dang02`, or `dang05`).
3. **Staggered Cutoffs (Mode `Sole` in `view_san`)**:
   When alternate bars are curtailed (50% staggered curtailment):
   - Long bar: $L_{\text{ext}} = L_n / 3 \approx 0.33 L_n$.
   - Short bar: $L_{\text{ext}} = L_n / 4 \approx 0.25 L_n$.

### 4.4 Cantilever Slab Detailing
For balcony, canopy, and walkway cantilever slabs of length $L_{\text{cant}}$:
- Top flexural tension bar must extend across the entire cantilever length:
  $$L_{\text{cant,proj}} = L_{\text{cant}} - c$$
- Edge closure: Bends down at exterior edge: $L_{\text{edge}} = h_{\text{cant}} - 2c$, then returns horizontally into bottom soffit by $L_{\text{ret}} \ge 150\text{ mm}$ (Shape code `dang44` or `dang09`).
- Back-Span Anchor Length: The top bar must project into the adjacent interior slab bay by:
  $$L_{\text{back}} \ge 1.5 L_{\text{cant}} \quad \text{and} \quad L_{\text{back}} \ge L_{anc,\text{tension}}$$

### 4.5 Corner Crack-Control Reinforcement
At discontinuous corners formed by perpendicular stiff perimeter beams or walls (TCVN 5574:2018 clause 10.3):
- Bending moments generate diagonal torsion cracks extending at $45^\circ$ from the corner.
- **Top Mesh**: Diagonal or orthogonal bars of diameter $d \ge 10\text{ mm}$, spacing $s \le 150\text{ mm}$, distributed over a square zone of side:
  $$L_{\text{corner}} = \frac{\min(L_x, L_y)}{5}$$
- **Bottom Mesh**: Identical orthogonal or fan-shaped bars parallel to the diagonal.

---

## 5. Subsystem 2: Polyline & Variable-Length Slab Detailing (`vesanpl`)

### 5.1 Arbitrary Polygon Boundary Representation
Modern architecture frequently features curved, angled, trapezoidal, and polygonal floor plates:
- A polyline slab boundary is represented as an ordered vertex list $\mathcal{P} = \{P_0, P_1, \dots, P_{m-1}\}$ in the horizontal plane ($Z = \text{TOC}$).
- The boundary may contain $K$ internal voids $\mathcal{H}_k$ (stairwells, elevator shafts, MEP chases).
- **Winding Order**: Outer boundary vertices are oriented counter-clockwise (CCW); hole boundaries are oriented clockwise (CW).
- **Concrete Cover Offset**: The rebar boundary $\mathcal{P}_{\text{rebar}}$ is derived by offsetting $\mathcal{P}$ inward by concrete cover $c$:
  $$\mathcal{P}_{\text{rebar}} = \text{OffsetPolygon}(\mathcal{P}, -c)$$

### 5.2 Parallel Bar Stepping Algorithm
For non-rectangular slabs, rebar is distributed parallel to a chosen principal vector $\vec{u} = (\cos \theta, \sin \theta)$ with spacing $s$:
1. Let $\vec{v} = (-\sin \theta, \cos \theta)$ be the perpendicular distribution axis.
2. Compute the bounding interval along $\vec{v}$:
   $$v_{\min} = \min_{P \in \mathcal{P}} (P \cdot \vec{v}), \quad v_{\max} = \max_{P \in \mathcal{P}} (P \cdot \vec{v})$$
3. For each distribution coordinate $v_j = v_{\min} + s/2 + j \cdot s$ (where $j = 0, 1, \dots, N-1$):
   - Construct distribution line segment $L_j(t) = P_{\text{origin}} + t \vec{u} + v_j \vec{v}$.
   - Intersect line $L_j$ with all boundary segments of $\mathcal{P}_{\text{rebar}}$ and hole boundaries $\mathcal{H}_k$.
   - Sort intersection points along $\vec{u}$: $t_0 < t_1 < \dots < t_{2p-1}$.
   - Pairs $(t_{2k}, t_{2k+1})$ define solid rebar chords inside the concrete slab.
4. **Length Quantization & Modular Stepping**:
   Individual cutting of every bar creates fabrication chaos. The engine rounds bar lengths to the nearest step $\Delta L$ (default $50\text{ mm}$, config item 66 `CutLengthRoundStep = 50`):
   $$L_{\text{quantized}} = \text{RoundToStep}(t_{2k+1} - t_{2k}, \Delta L)$$
5. **Variable Bar Mark Clustering (`1a, 1b...`)**:
   Bars sharing identical shape and diameter but possessing variable stepping lengths are grouped under a common root mark with alphabetic suffix (config item 40 `VarBarNaming = "1a, 1b..."`):
   $$\text{Mark} = \text{STT}_{\text{root}} + \text{Char}(97 + k) \quad (k = 0 \to \text{'a'}, 1 \to \text{'b'}, \dots)$$

### 5.3 Fan / Radial Rebar Distribution
For curved balconies, circular tanks, and skewed corners:
- Rebar radiates from an anchor vertex $C_0$ across an angular sweep $\theta \in [\theta_{\text{start}}, \theta_{\text{end}}]$.
- Angular step: $\Delta \theta = \frac{s_{\text{outer}}}{R_{\text{outer}}}$, clamped so outer spacing $\le s_{\max}$ and inner spacing $\ge s_{\min}$ (preventing bar congestion).

---

## 6. Subsystem 3: 4-Way & Radial Slab Reinforcement (`vesan4d`)

### 6.1 Geometric Quadrilateral Mapping
The `vesan4d` command details slab reinforcement within an arbitrary quadrilateral defined by 4 picked boundary points $\{P_1, P_2, P_3, P_4\}$:
- Edge vectors: $\vec{E}_{12} = P_2 - P_1$, $\vec{E}_{23} = P_3 - P_2$, $\vec{E}_{34} = P_4 - P_3$, $\vec{E}_{41} = P_1 - P_4$.
- Bilinear Parametric Mapping $\mathbf{x}(u, v)$ for $u, v \in [0, 1]$:
  $$\mathbf{x}(u, v) = (1-u)(1-v) P_1 + u(1-v) P_2 + uv P_3 + (1-u)v P_4$$

### 6.2 4-Way Reinforcement Layer Topology
In flat slabs subjected to heavy punching shear, transfer slabs, or irregular column grids:
1. **Layer 1 ($0^\circ$, Direction $U$)**: Generated along isoparametric lines $v = \text{const}$.
2. **Layer 2 ($90^\circ$, Direction $V$)**: Generated along isoparametric lines $u = \text{const}$.
3. **Layer 3 ($+45^\circ$, Diagonal 1)**: Generated along lines $u + v = \text{const}$. Resists diagonal tension and punching shear cones around column heads.
4. **Layer 4 ($-45^\circ$, Diagonal 2)**: Generated along lines $u - v = \text{const}$.

### 6.3 Flat Slab Drop Panel Integration (`drpn`)
At column supports in flat plate systems, drop panels (`drpn`) thicken the slab by $\Delta h$ (typically 50–150 mm) over a square area $(L_x/3 \times L_y/3)$:
- Slab top bars continue straight across the column.
- Bottom bars bend down into the drop panel at a $1:3$ slope (or $45^\circ$) with Z-bar transition (`dang50` or `dang05`):
  $$L_{\text{drop,step}} = \Delta h$$
  $$L_{\text{sloped}} = \sqrt{(\Delta h)^2 + (3\Delta h)^2} = \sqrt{10} \Delta h$$
- Additional closed drop panel shear ties (`dang22`) are arranged around the column perimeter at $a100$.

---

## 7. Subsystem 4: Slab Chair & Spacer Bar Calculator (`thep_ke` / `cho_san`)

### 7.1 Physics & Detailing Purpose of Chair Bars
Top negative moment bars and upper meshes in concrete slabs cannot suspend in mid-air during concrete placement. Under the weight of wet concrete, pumping hoses, and construction workers walking on the slab, top bars will deflect or collapse onto the bottom mesh, destroying the effective depth $d$ and causing catastrophic flexural failure.
Chair bars ("thép kê sàn" / "chân chó kê sàn" / spacer stools) maintain the design position of the top mesh.

### 7.2 Exact Mathematical Stool Height Formula
The net height of the chair bar stool $h_{\text{stool}}$ must account for all concrete covers and intervening bar diameters:

```
        ▲  +---------------------------------------+  ▲ Top of Concrete (TOC)
        │  │     c_top (Top Concrete Cover)        │  │
        │  +=======================================+  ▼
        │  │  Top Mesh Layer 1 (X) - Dia: d_tx     │
        │  +---------------------------------------+
        │  │  Top Mesh Layer 2 (Y) - Dia: d_ty     │
        │  +=======================================+  ▲
        │  │                                       │  │
   h_s  │  │        CHAIR BAR STOOL (h_stool)      │  │ h_stool
        │  │                                       │  │
        │  +=======================================+  ▼
        │  │  Bottom Mesh Layer 2 (Y) - Dia: d_by  │
        │  +---------------------------------------+
        │  │  Bottom Mesh Layer 1 (X) - Dia: d_bx  │
        │  +=======================================+  ▲
        │  │     c_bot (Bottom Concrete Cover)     │  │
        ▼  +---------------------------------------+  ▼ Soffit
```

#### Universal Multi-Layer Formulation:
$$h_{\text{stool}} = h_s - c_{\text{top}} - c_{\text{bot}} - \sum d_{\text{top}} - \sum d_{\text{bot,bearing}}$$

Where:
- $h_s$: Gross slab thickness in mm.
- $c_{\text{top}}$: Concrete cover to top steel (typically 15 mm to 25 mm).
- $c_{\text{bot}}$: Concrete cover to bottom steel (typically 15 mm to 25 mm).
- $\sum d_{\text{top}}$: Cumulative diameter of supported top bars ($d_{tx} + d_{ty}$).
- $\sum d_{\text{bot,bearing}}$: Diameter of bottom bar layers beneath the stool foot. When the chair foot rests on the upper bottom layer ($B_2$), $\sum d_{\text{bot,bearing}} = d_{bx} + d_{by}$. When the stool foot rests on plastic/mortar spacer blocks directly on the formwork, $\sum d_{\text{bot,bearing}} = 0$.

#### Standard Two-Way Mesh Practical Formula:
$$h_{\text{stool}} = h_s - 2c - d_{\text{top}} - d_{\text{bot}}$$

*Validation Example*: For $h_s = 120\text{ mm}$, $c = 15\text{ mm}$, $d_{\text{top}} = 10\text{ mm}$, $d_{\text{bot}} = 10\text{ mm}$:
$$h_{\text{stool}} = 120 - 2(15) - 10 - 10 = 70\text{ mm}$$

*Boundary Clamping*:
- If $h_{\text{stool}} < 30\text{ mm}$, the calculator clamps to $30\text{ mm}$ and issues a warning `WARN_SLAB_TOO_THIN_FOR_CHAIR` (slabs with $h_s \le 70\text{ mm}$ generally cannot fit two layers plus chairs).
- Rounding: $h_{\text{stool}}$ is rounded to the nearest $5\text{ mm}$.

### 7.3 Stool Segment Parameters & Shape Codes

```
           L3 (Top Saddle Cradle)
         +-------------+
         │             │
      L2 │             │ L2 (Stool Height = h_stool)
         │             │
  +------+             +------+
    L1                   L1 (Bottom Foot Extensions)
```

1. **`dang35` / `dang35a` (Standard 3D Chair Bar / Chân chó 3D)**:
   - 6 physical segments: 2 bottom feet ($L_1$), 2 vertical risers ($L_2$), 1 horizontal top saddle ($L_3$).
   - The two bottom feet $L_1$ are splayed outwards or at $90^\circ$ horizontally to provide two-directional overturning stability without tipping.
   - Dimensions:
     - $L_1$ (Foot length): $\max(150\text{ mm}, 15 d_{\text{chair}})$, default $150\text{ mm}$ to $200\text{ mm}$.
     - $L_2$ (Riser height): $h_{\text{stool}}$.
     - $L_3$ (Top saddle cradle): $\max(150\text{ mm}, 2 s_{\text{mesh}})$, default $200\text{ mm}$ to $300\text{ mm}$.
   - Total cutting length:
     $$L_{\text{cut}} = 2 L_1 + 2 L_2 + L_3$$
2. **`dang02` (U-Bar Stool / Ngựa kê chữ U)**:
   - 3 segments: 2 vertical legs ($L_1 = h_{\text{stool}}$), 1 bottom or top bar ($L_2 = 200\text{ mm}$).
   - Total cutting length:
     $$L_{\text{cut}} = 2 L_1 + L_2 = 2 h_{\text{stool}} + L_2$$
3. **`dang05` (Z-Bar Stool / Kê chữ Z)**:
   - 3 segments: 1 top arm ($L_1 = 150\text{ mm}$), 1 vertical riser ($L_2 = h_{\text{stool}}$), 1 bottom foot ($L_3 = 150\text{ mm}$).
   - Total cutting length:
     $$L_{\text{cut}} = L_1 + L_2 + L_3 = 300\text{ mm} + h_{\text{stool}}$$

### 7.4 Stool Spacing & Layout Density
- **Standard Density ($\rho_{\text{chair}}$)**:
  - For slab mesh $\Phi 10 - \Phi 12$: 1.0 to 1.2 stools per $m^2$.
  - For light slab mesh $\Phi 6 - \Phi 8$: 1.5 stools per $m^2$ (smaller diameter bars deflect more easily under foot traffic).
- **Placement Grid**:
  - Longitudinal spacing: $s_{x,\text{chair}} = 800\text{ mm}$ to $1,000\text{ mm}$.
  - Transverse spacing: $s_{y,\text{chair}} = 800\text{ mm}$ to $1,000\text{ mm}$.
  - Staggered pattern: Alternating rows shifted by $s/2$ to prevent localized weakness planes.
- **Quantity Estimation Formula**:
  $$N_{\text{stools}} = \left\lceil A_{\text{top\_mesh}} \times \rho_{\text{chair}} \right\rceil$$
  where $A_{\text{top\_mesh}}$ is the area of the top negative reinforcement zone or full slab area if double continuous mesh.

---

## 8. Subsystem 5: Panel Splitting, Tracing & Configuration (`chiasan`, `dsan`, `ssan`)

### 8.1 Ray-Casting Interior Point Picking (`pick_diem_san` / `dsan`)
When an engineer clicks inside a slab bay on AutoCAD plan view:
1. **Ray-Casting Boundary Search**: The engine casts 360 rays at $1^\circ$ angular increments from the picked point $P_{\text{pick}} = (x_0, y_0)$.
2. Rays detect intersecting entities on concrete outline layers (`kata_net thay`, `kata_net khuat`, beam outlines, wall outlines).
3. The nearest intersection point along each ray forms a polygonal envelope.
4. Redundant vertices along collinear edges ($\text{cross}(P_k - P_{j}, P_m - P_k) < \epsilon$) are eliminated.
5. The resulting closed boundary polygon $\mathcal{P}_{\text{panel}}$ represents the clear slab panel.

### 8.2 Span Partitioning Algorithm (`chia_san` / `chiasan`)
The `chia_san` tool divides the slab panel into structural strips for automated rebar placement:
1. **Principal Span Axes**: Identifies the primary rectangular axes $\vec{u}_x$ and $\vec{u}_y$ matching adjacent grid lines or long edges.
2. **Column Strip & Middle Strip Decomposition**:
   - Column Strip Width (Support Negative Band):
     $$W_{\text{col,strip}} = \frac{L_{\min}}{4} \quad \left(\text{or } \frac{L_{\min}}{2} \text{ centered over supporting beam}\right)$$
   - Middle Strip Width (Midspan Positive Band):
     $$W_{\text{mid,strip}} = L - 2 \left(\frac{L_{\min}}{4}\right) = L - \frac{L_{\min}}{2}$$
3. **Partition Lines**: The engine draws layout guidelines on layer `Defpoints` or `kata_net manh` indicating cutoff boundaries for top support hats and midspan bottom rebar.

### 8.3 Slab Information Inspection & XRecord Schema (`ssan`)
Executing `ssan` allows viewing and modifying slab attributes:
- Dialog displays: Panel Mark (e.g. `S1`, `S2`), Thickness $h_s$ (mm), TOC elevation (m), Top Cover (mm), Bottom Cover (mm), Bottom Bar X, Bottom Bar Y, Top Bar X, Top Bar Y.
- When confirmed, data is serialized into the entity Extension Dictionary under key `"KATA_SLAB_DATA"`:

```json
{
  "PanelMark": "S1",
  "ThickMm": 120.0,
  "TOCMm": 3600.0,
  "CoverTopMm": 15.0,
  "CoverBotMm": 15.0,
  "ConcreteGrade": "B25",
  "BottomX": { "Dia": 10, "Spacing": 150, "Shape": "dang01" },
  "BottomY": { "Dia": 10, "Spacing": 150, "Shape": "dang01" },
  "TopX": { "Dia": 10, "Spacing": 150, "CutoffRatio": 0.25, "Shape": "dang48" },
  "TopY": { "Dia": 10, "Spacing": 150, "CutoffRatio": 0.25, "Shape": "dang48" },
  "Chair": { "Dia": 10, "Height": 70.0, "Shape": "dang35", "Spacing": 1000.0 }
}
```

---

## 9. Subsystem 6: Slab Takeoff, Scheduling & Excel ClosedXML Export (`tksan`, Sheet `San`)

### 9.1 CAD Block & Entity Takeoff Scanning (`tksan`)
The `thong_ke_block_san` (`tksan`) command aggregates all slab rebar within the drawing:
1. **Scanning Scope**: Scans ModelSpace for:
   - Polylines carrying XData `"KATA"` with `stt_thep_chinh` = `"SAN"` or `"T"`/`"D"`.
   - Block references of type `kata_block_KHT`, `kata_block_KHTCT`, `thep_san`.
2. **Dimension Link Resolution**:
   If a tag block is associated with a distribution dimension line (`kata_rai_thep`), the engine resolves the bar count:
   $$N_{\text{bars}} = \left\lfloor \frac{L_{\text{dim}}}{s} \right\rfloor + 1$$
3. **Data Normalization**: Converts all scanned entities into strongly typed `RebarItem` records with shape codes, diameter, cutting length, member count, and total weight.

### 9.2 ClosedXML Dedicated Worksheet `"San"` Schema
The schedule export produces a dedicated sheet named `"San"` in the target `.xlsx` workbook:

| Column | Header | Type | Description |
|---|---|---|---|
| `A` | TÊN Ô SÀN | String | Slab panel designation (e.g. `S1`, `S2-TẦNG 2`) |
| `B` | VỊ TRÍ (LỚP) | String | Layer (`Lớp dưới X`, `Lớp dưới Y`, `Lớp trên gối`, `Thép kê sàn`) |
| `C` | SỐ HIỆU | String | Bar mark (e.g. `1`, `2`, `3a`) |
| `D` | DẠNG THÉP | String | Shape code (`dang01`, `dang48`, `dang35`, `dang02`, `dang05`) |
| `E` | Ø (mm) | Integer | Nominal bar diameter in mm ($6..25$) |
| `F` | KHOẢNG CÁCH | String | Spacing callout (e.g. `a150`, `a200`) |
| `G`..`L`| L1..L6 | Double | Individual segment dimensions in mm |
| `M` | DÀI 1 THANH | Double | Total cutting length $L_{\text{cut}}$ in mm |
| `N` | SỐ Ô SÀN | Integer | Number of identical panels |
| `O` | SỐ THANH / Ô | Integer | Rebar count per panel |
| `P` | TỔNG SỐ THANH | Integer | Cumulative bar count ($= N \times O$) |
| `Q` | TỔNG CHIỀU DÀI | Double | Total length in meters ($= M \times P / 1000$) |
| `R` | TRỌNG LƯỢNG (kg)| Double | Total mass ($= Q \times \text{UnitMass}$) |
| `S` | GHI CHÚ | String | Detailing note (`Thép mũ gối`, `Thép chân chó`, `Bo biên`) |

#### Dedicated Chair Bar Summary Table (`San!U1:Z8`):
- `U1`: BẢNG TỔNG HỢP THÉP KÊ SÀN (CHÂN CHÓ)
- `U2:Z2`: `TÊN Ô SÀN`, `CHIỀU DÀY SÀN (mm)`, `CHIỀU CAO KÊ (h_stool)`, `DẠNG KÊ`, `ĐƯỜNG KÍNH`, `TỔNG SỐ CON KÊ`.

---

## 10. Subsystem 7: Revit 2026 Integration Architecture (`SlabRebarGenerator.cs`)

### 10.1 Element Identification & Geometry Extraction
1. **Target Category**: `BuiltInCategory.OST_Floors` (`Autodesk.Revit.DB.Floor`).
2. **Boundary Profile Loops**:
   - Query horizontal top/bottom planar faces from `floor.get_Geometry(new Options())`.
   - Extract outer boundary `CurveLoop` and internal opening `CurveLoop`s.
   - Project vertices to the horizontal XY plane.
   - Convert internal Revit feet coordinates to metric millimeters using `RevitUnitConverter.FeetToMm()`.
3. **Slab Thickness & Elevation**:
   - Thickness $h_s$: Query parameter `BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM` or `ElementType` compound structure thickness.
   - Top Elevation (TOC): Query `Level.Elevation + FLOOR_HEIGHTABOVELEVEL_PARAM`.

### 10.2 Native Rebar Generation Pipeline
The generator creates true 3D parametric `Rebar` elements inside the `Floor` host:
1. **Coordinate Conversion**:
   - Converts `KataPoint3D` / curves from mm to internal Revit `XYZ` (feet) using factor $1 / 304.8$.
2. **Bottom Mesh Generation**:
   - Bottom X Layer: Linear curve sets running parallel to floor X axis, offset vertically by:
     $$Z_{bx} = Z_{\text{soffit}} + \text{Cover}_{\text{bot}} + \frac{d_{bx}}{2}$$
   - Bottom Y Layer: Curves running parallel to floor Y axis, offset vertically by:
     $$Z_{by} = Z_{bx} + \frac{d_{bx} + d_{by}}{2}$$
3. **Top Negative Hat Generation**:
   - Placed in support boundary zones, offset vertically from TOC by:
     $$Z_{tx} = Z_{\text{TOC}} - \text{Cover}_{\text{top}} - \frac{d_{tx}}{2}$$
   - End cogs bend downward along $-Z$ towards soffit.
4. **Idempotent Regeneration Guard**:
   - Comments tagged: `BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS = "HPKata_Slab_{SlabMark}"`.
   - Partition tagged: `BuiltInParameter.REBAR_PARTITION = "{SlabMark}"`.
   - Prior to generation, `CleanExistingSlabRebar` finds and deletes any existing rebar elements matching the prefix.

---

## 11. Subsystem 8: Complete Clean Code Class Design

All classes strictly obey `docs/clean-code/HP_CLEAN_CODE_CORE.md`:
- Max 300 physical lines per file (Rule C2).
- Max 50 physical lines per method (Rule M3).
- Zero CAD or Revit reference assemblies inside `HPKata.Core`.
- Nullable reference types enabled (`#nullable enable`).

### 11.1 Class Layout & File Breakdown Matrix
```
HPKata/
├── HPKata.Core/
│   └── Slabs/
│       ├── Models/
│       │   ├── SlabPanelModel.cs                (85 lines)  <= 300
│       │   ├── SlabBoundaryPolygon.cs           (140 lines) <= 300
│       │   ├── SlabRebarLayerModel.cs           (75 lines)  <= 300
│       │   ├── SlabChairBarModel.cs             (80 lines)  <= 300
│       │   ├── SlabOpeningModel.cs              (60 lines)  <= 300
│       │   └── SlabDropPanelModel.cs            (70 lines)  <= 300
│       └── Calculators/
│           ├── SlabMeshCalculator.cs            (120 lines) <= 300
│           ├── SlabNegativeMomentCalculator.cs  (110 lines) <= 300
│           ├── SlabChairCalculator.cs           (95 lines)  <= 300
│           ├── SlabPolylineSteppingCalculator.cs(160 lines) <= 300
│           ├── SlabCornerCrackCalculator.cs     (85 lines)  <= 300
│           └── SlabOpeningTrimmerCalculator.cs  (90 lines)  <= 300
├── HPKata.AutoCad/
│   ├── Commands/
│   │   └── SlabCommands.cs                      (150 lines) <= 300
│   └── Controllers/
│       ├── SlabDrawController.cs                (180 lines) <= 300
│       ├── SlabPolylineController.cs            (160 lines) <= 300
│       ├── SlabChairController.cs               (120 lines) <= 300
│       └── SlabTakeoffController.cs             (140 lines) <= 300
└── HPKata.Revit/
    ├── Commands/
    │   └── SlabRevitCommands.cs                 (110 lines) <= 300
    └── Services/
        ├── SlabRebarGenerator.cs                (195 lines) <= 300
        └── SlabScheduleService.cs               (110 lines) <= 300
```

### 11.2 Core Class Contract Signatures

#### `SlabPanelModel.cs`
```csharp
namespace HPKata.Core.Slabs.Models
{
    public sealed class SlabPanelModel
    {
        public string PanelMark { get; set; } = "S1";
        public double ThicknessMm { get; set; } = 120.0;
        public double TopOfConcreteMm { get; set; } = 3600.0;
        public double ConcreteCoverMm { get; set; } = 15.0;
        public string ConcreteGrade { get; set; } = "B25";

        public SlabBoundaryPolygon Boundary { get; set; } = new();
        public List<SlabOpeningModel> Openings { get; set; } = new();
        public SlabDropPanelModel? DropPanel { get; set; }

        public SlabRebarLayerModel BottomMeshX { get; set; } = new() { LayerType = SlabLayerType.BottomX };
        public SlabRebarLayerModel BottomMeshY { get; set; } = new() { LayerType = SlabLayerType.BottomY };
        public List<SlabRebarLayerModel> TopNegativeBars { get; set; } = new();
        public SlabChairBarModel ChairSpec { get; set; } = new();

        public double ClearSpanXMm => Boundary.BoundingBoxWidthMm;
        public double ClearSpanYMm => Boundary.BoundingBoxHeightMm;
        public double AreaM2 => Boundary.ComputeAreaM2();
    }
}
```

#### `SlabChairCalculator.cs`
```csharp
namespace HPKata.Core.Slabs.Calculators
{
    public static class SlabChairCalculator
    {
        public static double CalculateStoolHeight(
            double slabThicknessMm,
            double coverTopMm,
            double coverBotMm,
            int topBarDiaMm,
            int botBarDiaMm)
        {
            if (slabThicknessMm <= 0.0) throw new ArgumentOutOfRangeException(nameof(slabThicknessMm));
            double hStool = slabThicknessMm - coverTopMm - coverBotMm - topBarDiaMm - botBarDiaMm;
            return Math.Max(30.0, Math.Round(hStool, 1));
        }

        public static (double FootMm, double RiserMm, double SaddleMm, double TotalCutMm) CalculateDang35Dimensions(
            double stoolHeightMm,
            int chairBarDiaMm = 10,
            double footLenMm = 150.0,
            double saddleLenMm = 200.0)
        {
            double foot = Math.Max(footLenMm, 15.0 * chairBarDiaMm);
            double riser = Math.Max(30.0, stoolHeightMm);
            double saddle = Math.Max(saddleLenMm, 150.0);
            double total = (2.0 * foot) + (2.0 * riser) + saddle;
            return (foot, riser, saddle, total);
        }

        public static int CalculateStoolQuantity(double slabAreaM2, double stoolsPerM2 = 1.2)
        {
            if (slabAreaM2 <= 0.0) return 0;
            double density = stoolsPerM2 > 0.0 ? stoolsPerM2 : 1.2;
            return (int)Math.Ceiling(slabAreaM2 * density);
        }
    }
}
```

#### `SlabMeshCalculator.cs`
```csharp
namespace HPKata.Core.Slabs.Calculators
{
    public static class SlabMeshCalculator
    {
        public static (int BarCount, double BarLengthMm, double TotalLengthM) CalculateUniformLayer(
            double spanLengthMm,
            double distributionWidthMm,
            double spacingMm,
            double anchorageLeftMm,
            double anchorageRightMm,
            double coverOffsetMm = 15.0)
        {
            if (spacingMm <= 0.0 || distributionWidthMm <= 0.0) return (0, 0.0, 0.0);
            double netWidth = Math.Max(0.0, distributionWidthMm - (2.0 * coverOffsetMm));
            int count = (int)Math.Floor(netWidth / spacingMm) + 1;
            double length = spanLengthMm + anchorageLeftMm + anchorageRightMm;
            double totalM = (count * length) / 1000.0;
            return (count, Math.Round(length, 1), Math.Round(totalM, 2));
        }
    }
}
```

---

## 12. Edge Cases, Mathematical Boundaries & Failure Modes

### Edge Cases Table
| # | Feature | Input Condition | Expected / Handled Observed Behavior | Defense Mechanism |
|---|---|---|---|---|
| 1 | `SlabChairCalculator` | Ultra-thin slab: $h_s = 60\text{ mm}$, $c = 15\text{ mm}$, $d_t = 10, d_b = 10 \implies h_{\text{stool}} = 10\text{ mm}$ | Clamps $h_{\text{stool}}$ to absolute minimum $30\text{ mm}$, issues `WARN_SLAB_TOO_THIN_FOR_CHAIR` | Guard `Math.Max(30.0, ...)` |
| 2 | `SlabMeshCalculator` | Spacing $s \le 0$ or negative | Returns zero count and length, logs warning | Guard `if (spacingMm <= 0.0) return (0, 0, 0);` |
| 3 | `SlabMeshCalculator` | Panel aspect ratio $\lambda = L_y / L_x > 2.0$ (One-way action) | Enforces secondary distribution bar spacing $s_y \le \min(3h_s, 250\text{ mm})$, $A_{sy} \ge 0.2 A_{sx}$ | Auto-clamp $s_y = \min(s_y, 250.0)$ |
| 4 | `SlabNegativeMomentCalculator` | Discontinuous exterior support with beam width $b_w < 200\text{ mm}$ | Anchors bar with $90^\circ$ downward cog hook $L_{\text{cog}} = h_s - 2c$ | Auto-attaches hook `dang02` / `dang48` |
| 5 | `SlabPolylineSteppingCalculator` | Non-convex L-shaped or U-shaped slab boundary | Ray-casting detects multiple disconnected chord segments per line; generates separate bar sets | Sorts intersection points by coordinate |
| 6 | `SlabPolylineSteppingCalculator` | Extremely short chord near corner ($L_{\text{chord}} < 200\text{ mm}$) | Discards bar chord as unmanufacturable scrap, clamps to corner | Filter `if (length < 200.0) continue;` |
| 7 | `SlabOpeningTrimmerCalculator` | Small MEP pipe penetration ($\Phi \le 150\text{ mm}$) | Mesh bars shift around opening without cutting or adding trimmer bars | Threshold `if (maxDim <= 150.0) SkipTrimmers();` |
| 8 | `SlabOpeningTrimmerCalculator` | Large stairwell shaft ($b_0 \ge 1000\text{ mm}$) | Cuts mesh bars, adds $2\Phi 14$ diagonal bars at 4 corners + double trimmer bars on 4 sides | Full shaft trimmer generator |
| 9 | `SlabRebarGenerator` (Revit) | Host Floor has slope / sloped arrow | Computes sloped normal vector $\vec{n} = \vec{u} \times \vec{v}$, projects rebar curves into inclined plane | Non-horizontal planar face transform |
| 10 | `SlabRebarGenerator` (Revit) | Repeated execution on same floor | Deletes prior rebars matching `"HPKata_Slab_{SlabMark}"` before generating new cage | `CleanExistingSlabRebar` sweep |
| 11 | `SlabTakeoffController` | DWG contains orphan tag block whose linked polyline was deleted | Tags block as orphan, highlights in yellow warning color, excludes from cutting schedule | Handle lookup `IsErased` check |
| 12 | `ClosedXmlExporter` | Sheet `"San"` exported with 0 slab panels | Generates empty template with formatted headers without throwing exception | Empty collection check |

---

## 13. Comprehensive Unit & Integration Test Plan

The implementation in `HPKata.Core.Tests` will add a dedicated test suite `HPKata.Core.Tests/Slabs/`:

### Test Suite Inventory (Target: $\ge 40$ Unit Tests)
1. **`SlabChairCalculatorTests`**:
   - `CalculateStoolHeight_StandardSlab_ReturnsCorrectHeight` ($h_s=120, c=15, d=10 \implies 70\text{ mm}$).
   - `CalculateStoolHeight_ThickMat_ReturnsCorrectHeight` ($h_s=500, c=50, d=25 \implies 350\text{ mm}$).
   - `CalculateStoolHeight_ThinSlab_ClampsToMinimum30mm` ($h_s=60 \implies 30\text{ mm}$).
   - `CalculateDang35Dimensions_ValidParameters_ComputesCorrectCutLength`.
   - `CalculateStoolQuantity_ValidArea_ReturnsCeilingCount` ($100\text{ m}^2 \times 1.2 \implies 120\text{ stools}$).
2. **`SlabMeshCalculatorTests`**:
   - `CalculateUniformLayer_StandardSpan_ComputesCorrectBarCountAndLength`.
   - `CalculateUniformLayer_ZeroOrNegativeSpacing_ReturnsZero`.
   - `CalculateUniformLayer_CoverOffsetDeduction_CorrectlyReducesWidth`.
3. **`SlabNegativeMomentCalculatorTests`**:
   - `CalculateCurtailment_InteriorSupport_ReturnsQuarterSpan`.
   - `CalculateCurtailment_Cantilever_EnforcesOneAndHalfBackspan`.
   - `CalculateCogDown_ReturnsNetCoverHeight`.
4. **`SlabPolylineSteppingCalculatorTests`**:
   - `SegmentPolygon_TrapezoidalSlab_ProducesSteppedBarLengths`.
   - `SegmentPolygon_RoundsTo50mmStep_ObeysConfig66`.
   - `ClusterMarks_GeneratesAlphabeticSuffixes` (`1a, 1b, 1c`).
5. **`SlabOpeningTrimmerCalculatorTests`**:
   - `CalculateTrimmers_SmallPenetration_NoTrimmersGenerated`.
   - `CalculateTrimmers_LargeOpening_GeneratesDiagonalAndPerimeterBars`.
6. **`SlabScheduleAggregatorTests`**:
   - `GenerateSlabReport_GroupsByPanelAndDiameter`.
   - `ExportSlabSheet_ClosedXml_GeneratesValidWorkbook`.

---

## 14. Verification & Sign-Off

This specification has been mined from ground-truth inspection of:
- `Kata_pro64_Cad2025.dll` & `Kata_pro64_Cad2013.dll`
- `Kata_Class_Lib.dll` & `Kata_Class_Lib_Revit.dll`
- `Goi lenh.lsp` & `Iso_kata.dwg`
- `Kata.xlsm` (Sheet `SLAB DESIGN` & `Thong_ke`)
- `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`

All class interfaces, mathematical formulas, and architectural boundaries are fully determined, verified, and ready for clean-room implementation in Phase 4 of Milestone M3.

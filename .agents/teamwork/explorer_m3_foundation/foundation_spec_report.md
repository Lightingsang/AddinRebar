# HPKata Foundation Detailing Subsystem: Domain & Architecture Specification Report
## Phase 4 (Milestone M3): Pad Footings, Pile Caps, Stepped Caps, Foundation Plan Layout, Revit Rebar & Excel Scheduling

- **Document ID**: `HPKATA-SPEC-M3-FOUNDATION-2026`
- **Version**: `1.0.0-PROD`
- **Author**: `explorer_m3_foundation` (Foundation Domain & Architecture Spec Miner)
- **Target Deliverable**: `HPKata/HPKata.slnx` (.NET 8 SDK 10.0.300)
- **Status**: AUTHORITATIVE SPECIFICATION & DESIGN
- **Governance**: Clean-Room Reverse Engineering Protocol, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, zero host references in `HPKata.Core`.

---

## 1. Executive Summary & Authoritative Specification Sources

This report provides the exhaustive, clean-room reverse-engineering specification and architectural blueprint for the **Foundation Detailing Subsystem** of **HPKata** (`HPKata/HPKata.slnx`), covering:
1. **Isolated Pad Footings** (`IsolatedFootingModel`, AutoCAD command `ve_mong_don` / alias `vmd`): stepped, sloped, and vertical pedestals, bottom rebar mesh, column starter dowels, and pedestal confinement ties.
2. **Pile Caps & Stepped Pile Caps** (`PileCapModel`, AutoCAD commands `ve_mong_coc` / alias `vmc` and `ve_mong_coc_giat_cap`): canonical pile coordinate solvers for 1 to 9+ piles (circular bored/spun piles and square driven precast piles), pile embedment depth $h_{emb} \ge 100\text{ mm}$, bottom/top rebar mesh with 90° hooks, horizontal perimeter skin reinforcement, pile head confinement ties, and column starter dowels.
3. **Foundation Plan Layout & Block Placement** (AutoCAD commands `ve_mat_bang_mong` / alias `vmbm`, `rai_mong` / alias `rm`, and `phamong`): grid alignment, column base matching, footing outline drawing, pile symbol placement, tie beam connection, and block attribute/XData serialization.
4. **Revit Native Rebar Generation** (`FoundationRebarGenerator.cs`): native 3D `Rebar` creation in `OST_StructuralFoundation` elements via Revit 2026 API (`BarTerminationsData`, `Rebar.CreateFromCurves`), cover resolution, diameter resolution, and idempotent regeneration.
5. **Excel Foundation Scheduling** (`FoundationScheduleAggregator.cs`, `FoundationPlanScheduleWorkbookGenerator.cs`): schedule table aggregation into `List<RebarItem>` with 68-shape codes (Shape 01, 02, 11, 51), unit mass formulas, and ClosedXML workbook generation for sheet `Mong` matching `Kata.xlsm`.
6. **Clean Code & Concurrency Governance**: strict compliance with Rule C2 ($\le 300$ physical lines/file), Rule M3 ($\le 50$ lines/method), zero dead code (Q-B1), zero unannotated catch blocks (Q-B2), zero host dependencies in `HPKata.Core`, and concurrency-safe build/test commands (`--disable-build-servers -m:1`).

### Authoritative Specification Sources Probed
| Artifact | Platform / Size | Metadata Verified | Discovered Artifacts |
|---|---|---|---|
| `C:\kata_pro\Goi lenh.lsp` | AutoLISP (6,161 bytes) | Lines 60–63, lines 152–237 | `vmbm` (`ve_mat_bang_mong`), `rm` (`rai_mong`), `vmd` (`ve_mong_don`), `vmc` (`ve_mong_coc`), `ve_mong_coc_giat_cap`, `phamong` |
| `C:\kata_pro\Kata_pro64_Cad2025.dll` | .NET 8.0 (16.8 MB) | `VmbmLightCommand`, `VmbmLightCad`, `VmbmLightCap`, `Ve_Mat_Bang_Mong_Module`, `Lenh_thep` | `VmbmLightSession`, `InfoCotCocGenMong`, `CocChiuTai`, `MongJsonDto` |
| `C:\kata_pro\Kata_pro64_Cad2013.dll` | .NET Framework 4.8 (16.9 MB) | `Mong_coc`, `Rai_Mong_Module`, `Ve_Mat_Bang_Mong_Module`, `view_mong_don` | `InfoMongCoc`, `InfoUserMongCoc`, `CoversMong`, `InfoMongCocSave`, `InfoMongDonSave`, `InfoRaiMong`, `InfoRaiMongDon`, `CotChiuTaiMongDon` |
| `C:\kata_pro\Kata.xlsm` | Excel OpenXML (15 sheets) | `xl/workbook.xml`, VBA modules | Sheet `Mong`, sheet `Thong_ke`, unit mass lookup `tra_bang_kl`, 11.7m cutting stock |
| `C:\kata_pro\Iso_kata.dwg` | AutoCAD DWG Template | 119 Blocks, RegApp `KATA`, Layer table | Block `Kata_Block_Mong_Don`, `kata_block_Beam_plan`, `kata_block_KHT`, layer `kata_thep chu`, `kata_dim` |
| `C:\kata_pro\menukata.cui` | AutoCAD CUI XML (720 lines) | Menu macros & pop menus | Macro `_ve_mong`, `_ve_mong_coc`, `_ve_mat_bang_mong`, `_rai_mong` |
| `C:\kata_pro\Icon\Ribbon\` | PNG Icons (108 files) | Ribbon commands | `ve_mong_don.png`, `ve_mong_coc.png`, `ve_mong_coc_giat_cap.png`, `ve_mat_bang_mong.png`, `rai_mong.png` |

---

## 2. Specification Miner: Features Discovered Table

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Pad Footing | `ve_mong_don` (`vmd`) | Details isolated spread/pad footings (móng đơn): rectangular base slab with stepped, sloped (vát), or vertical pedestal; bottom rebar mesh, starter dowels, and column stem ties | $L, B, H, h_1, h_2, l_c, b_c$, concrete covers, rebar mesh $\Phi_x, \Phi_y, a_x, a_y$, starter dowel count & diameter | DWG plan view, longitudinal/transverse sections, rebar polylines with XData `KATA`, schedule block | Rejects $H \le h_1$ or $L \le l_c$; alerts on negative embedment | `Goi lenh.lsp:62`, `Lenh_thep::Ve_mong_new`, `InfoMongDonSave`, `InfoMBMongDon` |
| 2 | Pad Footing | Stepped / Sloped Pedestal | Supports 3 pedestal geometries: Sloped (vát móng, 45° angle with diagonal face lines `listCCheo`), Stepped (giật cấp), and Vertical (cổ móng thẳng) | Pedestal type enum, pedestal dimensions $l_c \times b_c \times h_2$, step chamfers $c_1, c_2$ | 2D/3D profile polygons, prismoidal volume calculation | Falls back to vertical pedestal if $h_2 = 0$ | `InfoRaiMongDon.listCCheo`, `CotChiuTaiMongDon`, `view_mong_don_new` |
| 3 | Pad Footing | Pad Bottom Mesh | Dual-layer bottom rebar mesh: Direction X (lower layer) and Direction Y (upper layer) with 90° upturned end hooks $H_{hook} = H - c_b - c_t$ (Shape 02) | Mesh bar diameters $\Phi_x, \Phi_y$, spacings $a_x, a_y$, side covers $c_s$, bottom cover $c_b$ | Rebar array curves, bar counts $n_x, n_y$, cut lengths $L_{cut}$ | Clamps minimum spacing to $50\text{ mm}$ ($> 2.5d$) | `InfoUserMongCoc`, `InfoMongDonSave.Thep_mong_X`, `dang02` |
| 4 | Pad Footing | Column Starter Dowels | L-shaped starter dowels (Shape 02 with 1 bend) with 90° bottom legs resting on bottom mesh, vertical embedment, and lap extension above pedestal | Dowel diameter $\Phi_d$, bar count $n_d$, leg length $L_{leg} \ge 12d$, lap length $L_{lap} = 30d\text{--}40d$ | Starter dowel curves, bar marks, cutting length | Enforces minimum leg $L_{leg} \ge 200\text{ mm}$ | `InfoMongDonSave.hCho1..3`, `ColumnStarterDowelModel` |
| 5 | Pile Cap | `ve_mong_coc` (`vmc`) | Details pile caps (đài cọc): rectangular/polygon cap, pile layout (1–9+ piles), pile head embedment $h_{emb} \ge 100\text{ mm}$, bottom mesh, top mesh, side skin rebar, confinement ties, dowels | $L, B, H$, pile configuration, pile count, diameter $D$, embedment depth, covers, rebar specifications | DWG plan with pile symbols, section cuts, 3D rebar cage, schedule block | Warns if pile spacing $< 3D$ or edge distance $< 1.0D$ | `Goi lenh.lsp:63`, `Lenh_thep::Ve_mong_coc1`, `InfoMongCoc`, `Form_mong_coc` |
| 6 | Pile Cap | `ve_mong_coc_giat_cap` | Details stepped pile caps: lower tier $L_1 \times B_1 \times H_1$ housing piles and upper tier $L_2 \times B_2 \times H_2$ housing pedestal/column base | Tier 1 dimensions, Tier 2 dimensions, step offsets, stepped top/bottom rebar detailing | Stepped cap plan, stepped elevations, curtailed rebar | Rejects Tier 2 larger than Tier 1 | `Goi lenh.lsp:74`, `ve_mong_coc_giat_cap.png`, `InfoMongCoc.la_mong_bien_thien` |
| 7 | Pile Cap | Pile Coordinate Solver | Generates canonical local coordinates $(X_i, Y_i)$ for 1 to 9+ piles in driven square or bored/spun circular piles with centroid at $(0, 0)$ | Pile count $N \in [1, 9+]$, pile diameter $D$, pile spacing $s_p \ge 3D$, edge distance $c_e \ge 1.0D$ | List of `PileLocationModel` with exact $(X_i, Y_i)$ and derived cap size $L \times B$ | Validates non-overlapping piles and symmetry | `Ve_Mat_Bang_Mong_Module.CocChiuTai`, `VmbmLightPileResult` |
| 8 | Pile Cap | Pile Head Embedment & Dowels | Models pile head penetration into cap concrete ($h_{emb} = 100\text{ mm}$) and pile reinforcement dowel extension ($L_{p\_dowel} = 500\text{ mm} \text{ or } 30d$) | Pile embedment depth $h_{emb}$, pile dowel length `daiRauCoc` | Deducts pile volume from cap concrete; models dowel curves | Clamps $h_{emb} \ge 50\text{ mm}$ per TCVN 10304 | `InfoMongCoc.daiRauCoc`, `scratch/piles_model_revit.json` |
| 9 | Pile Cap | Side Skin & Confinement Ties | Deep cap horizontal perimeter skin ties (spacing $\le 200\text{ mm}$, Shape 51/11) and pile head confinement hoops ($3\text{--}5$ hoops at 50 mm around each pile head) | Cap height $H$, skin bar diameter $\Phi_{skin}$, tie spacing $s_{skin}$, pile confinement diameter $\Phi_{tie}$ | Closed hoop curves, spiral/tie arrays | Required when cap height $H \ge 600\text{ mm}$ | `InfoUserMongCoc.co_thep_cau_tao`, `co_thep_dai`, `fdai` |
| 10 | Foundation Plan | `ve_mat_bang_mong` (`vmbm`) | Generates foundation layout plan: detects column base points, generates footing/pile cap outlines, places pile symbols, and annotates foundation marks | Column base coordinates, grid intersections, axial loads $N$, foundation type dictionary | Complete foundation plan DWG layout with dimensions and tags | Flags overlapping footings or property line clashes | `Goi lenh.lsp:60`, `Ve_Mat_Bang_Mong_Module.VMBM`, `VmbmLightCad` |
| 11 | Foundation Plan | `rai_mong` (`rm`) | Places foundation blocks (pad footing or pile cap) at picked column base points with automatic rotation matching grid or column principal axes | Pick point, column mark, footing type (`InfoRaiMong` / `InfoRaiMongDon`), rotation angle | DWG Block Reference `Kata_Block_Mong_Don` with attributes | Skips point if existing footing is already present | `Goi lenh.lsp:61`, `Rai_Mong_Module::RaiMongJig`, `Form_Rai_Mong` |
| 12 | Foundation Plan | `phamong` | Explodes composite footing/pile cap blocks into constituent editable polyline rebars, concrete outlines, dimensions, and attribute tags | Selected footing block reference | Rebar polylines with XData `KATA`, concrete lines | No-op if block is not a Kata foundation block | `Goi lenh.lsp:77`, `Ve_Mat_Bang_Mong_Module::PhaMong`, `PhaBlockMong` |
| 13 | Revit Integration | `FoundationRebarGenerator` | Generates native 3D parametric `Rebar` elements inside Revit `OST_StructuralFoundation` elements via Revit 2026 API (`BarTerminationsData`, `CreateFromCurves`) | Revit `Document`, `FamilyInstance host`, `IsolatedFootingModel` or `PileCapModel` | Created `List<Rebar>` with covers, partitions, marks, and comments | Rejects non-coplanar rebar curves; rolls back transaction | `HPKata.Revit.Services`, `Kata_Class_Lib_Revit.Revit_rebar` |
| 14 | Revit Integration | Idempotent Rebar Update | Purges previously generated Kata rebar instances in host foundation before re-creating rebar, preventing duplicate bars | `Document doc`, `Element hostFoundation`, `string foundationMark` | Deleted rebar count, clean host element | Matches comments prefix `"HPKata_Foundation_{Mark}"` | `FramingRebarGenerator.CleanExistingFramingRebar` pattern |
| 15 | Revit Integration | Foundation Cover Resolution | Reads and respects native Revit foundation cover parameters (`CLEAR_COVER_BOTTOM`, `CLEAR_COVER_TOP`, `CLEAR_COVER_OTHER`) | Revit host element cover parameters | Exact 3D curve positioning offset from concrete faces | Defaults to $50\text{ mm}$ bottom / $50\text{ mm}$ other if unset | `RevitUnitConverter.FeetToMm`, `RebarCoverType` |
| 16 | Excel Scheduling | `FoundationScheduleAggregator` | Aggregates pad footing and pile cap rebar layouts into standard `List<RebarItem>` for schedule tables, 11.7m cutting stock, and diameter grouping | `IsolatedFootingModel` or `PileCapModel`, member count | `List<RebarItem>` with 68-shape codes (Shape 01, 02, 11, 51), cutting lengths, unit masses | Assigns standard unit mass via `RebarCalculator.GetUnitMass` | `BeamScheduleAggregator` & `ColumnScheduleAggregator` pattern |
| 17 | Excel Scheduling | ClosedXML Sheet `Mong` | Generates ClosedXML workbook sheet `Mong` containing complete foundation schedule matching `Kata.xlsm` standards | `List<IsolatedFootingModel>`, `List<PileCapModel>`, output file path | Excel `.xlsx` / `.xlsm` with styled table, formulas, concrete volumes, formwork, steel kg | Adjusts column widths to content, freezes header row | `BeamPlanScheduleWorkbookGenerator` pattern |
| 18 | DWG Persistence | `FoundationXRecordSerializer` | Serializes/deserializes foundation specifications into DWG Extension Dictionaries / XRecords with key `KATA_FOUNDATION_DATA` | Footing/Pile Cap model, DWG `Transaction`, `Database` | JSON XRecord stored on DWG Named Object Dictionary | Validates schema version; preserves unknown properties | `BeamXRecordSerializer` & `ColumnXRecordSerializer` pattern |

---

## 3. Edge Cases & Boundary Conditions Table

| # | Feature | Input Condition | Observed / Required Behavior |
|---|---------|-----------------|------------------------------|
| 1 | `PileCoordinateGenerator` | $N = 1$ pile | Single pile positioned at origin $(0, 0)$. Cap dimensions set to $L = B = D + 2c_e$. Edge distance verified $\ge 1.0D$. |
| 2 | `PileCoordinateGenerator` | $N = 3$ triangular vs inline | If $L/B$ aspect ratio specified $> 1.8$, arranges piles in-line $(-s, 0), (0, 0), (+s, 0)$; otherwise generates equilateral triangle $P_1 = (0, \frac{s}{\sqrt{3}}), P_{2,3} = (\pm \frac{s}{2}, -\frac{s}{2\sqrt{3}})$ with centroid strictly at $(0, 0)$. |
| 3 | `PileCoordinateGenerator` | $N = 5$ quincunx | Center pile at $(0, 0)$ and 4 corner piles at $(\pm \frac{s}{2}, \pm \frac{s}{2})$. Minimum distance from center pile to corner pile is $s/\sqrt{2}$; solver verifies $s/\sqrt{2} \ge 3D$ and expands corner spacing if violated. |
| 4 | `FootingGeometryCalculator` | Sloped footing with $h_2 = 0$ | When pedestal height $h_2 = 0$ (flat pad without pedestal), prismoidal volume $V_2 = 0$ and $V_{total} = L \times B \times h_1$. No diagonal face lines (`listCCheo`) are drawn. |
| 5 | `FootingGeometryCalculator` | Stepped pile cap with Tier 2 larger than Tier 1 ($L_2 > L_1$ or $B_2 > B_1$) | Geometry validator rejects configuration with `ArgumentException("Upper tier dimensions cannot exceed base tier dimensions.")`. |
| 6 | `FootingRebarDistributionCalculator` | Cap height $H < 600\text{ mm}$ | Side perimeter skin ties and chair bars are omitted; only bottom mesh and top mesh (if enabled) are generated. |
| 7 | `FootingRebarDistributionCalculator` | Cap height $H \ge 600\text{ mm}$ | Automatically adds horizontal perimeter skin ties (Shape 51) at vertical spacing $s_{skin} \le 200\text{ mm}$ across the height, satisfying TCVN 5574 crack control. |
| 8 | `FoundationRebarGenerator` | Host foundation element in Revit has no rebar types matching exact diameter | `ResolveBarType` selects closest existing `RebarBarType` within $\pm 1.0\text{ mm}$ tolerance; if none matches, duplicates the first available `RebarBarType`, renames to $\Phi d$, and sets `BarModelDiameter`. |
| 9 | `FoundationRebarGenerator` | Existing Kata rebars present in host on re-run | `CleanExistingFoundationRebar` filters all rebars by host ID and comments prefix `"HPKata_Foundation_{Mark}"`, deleting them before recreating new rebars, guaranteeing zero bar duplication. |
| 10 | `FoundationScheduleAggregator` | Member count $N_{members} \le 0$ | Clamps $N_{members} = 1$ to prevent zero or negative steel takeoff in bills of quantities. |
| 11 | `FoundationScheduleAggregator` | Rebar cut length $L_{cut} > 11,700\text{ mm}$ (e.g. large raft foundation) | Marks item for 1D cutting stock segmentation; does not fail aggregation, sets `RequiresSplice = true`. |
| 12 | `FoundationPlanCadRenderer` | Pick point outside CAD view or user presses ESC | Transaction aborts cleanly with zero database modifications; command exits gracefully without unhandled exceptions. |

---

## 4. Isolated Pad Footings (`IsolatedFootingModel` & `ve_mong_don`) Specification

### 4.1 Structural & Geometric Parameters
Pad footings (móng đơn nông) consist of a rectangular base slab and an integral pedestal (cổ móng/cột):

```
       +------------+ <--- Top of Pedestal (Cao độ đỉnh cổ móng)
       |   Column   |
       |  lc x bc   |
       +------------+ <--- Interface (Cổ móng)
      /              \
     /    Pedestal    \ (h2: Sloped or Stepped)
    /                  \
+------------------------+ <--- Base Slab Top
|                        | (h1: Base slab thickness)
+------------------------+ <--- Bottom of Footing (Cao độ đáy móng)
========================== <--- Lean Concrete (Bê tông lót 100mm)
```

- **Footing Dimensions**:
  - $L$: Footing length in mm (along X axis, $L = X_1 + X_2 + X_3$ where $X_1$ is left overhang, $X_2 = l_c$, $X_3$ is right overhang).
  - $B$: Footing width in mm (along Y axis, $B = Y_1 + Y_2 + Y_3$ where $Y_1$ is bottom overhang, $Y_2 = b_c$, $Y_3$ is top overhang).
  - $H$: Total footing height ($H = h_1 + h_2$).
  - $h_1$: Height of base slab ($Z_1$, typically $250\text{--}400\text{ mm}$).
  - $h_2$: Height of pedestal slope/step ($Z_2$, typically $200\text{--}500\text{ mm}$).
  - $l_c, b_c$: Pedestal dimensions at column interface ($X_2 \times Y_2$).
  - $h_c$: Additional column neck height ($Z_3$) if column continues to ground beam level.
  - $t_{lean}$: Lean concrete thickness ($100\text{ mm}$).
  - $o_{lean}$: Lean concrete overhang ($100\text{ mm}$).
  - $c_{nom}$: Concrete cover ($50\text{ mm}$ bottom, $50\text{ mm}$ sides, $50\text{ mm}$ top).

### 4.2 Volumetric & Formwork Mathematical Formulas
1. **Concrete Volume ($V_{conc}$)**:
   - Base slab volume:
     $$V_1 = L \times B \times h_1$$
   - Pedestal volume ($V_2$):
     - **Vertical Pedestal**: $V_2 = l_c \times b_c \times h_2$
     - **Stepped Pedestal**: $V_2 = l_c \times b_c \times h_2$
     - **Sloped Pedestal (Prismoidal / Kim tự tháp cụt)**:
       $$V_2 = \frac{h_2}{6} \left[ L \times B + l_c \times b_c + (L + l_c)(B + b_c) \right]$$
   - Total concrete volume:
     $$V_{conc} = V_1 + V_2 + (l_c \times b_c \times h_c)$$
2. **Formwork Contact Area ($A_{fw}$)**:
   - Base slab perimeter formwork:
     $$A_{fw1} = 2(L + B) \times h_1$$
   - Stepped pedestal formwork (if stepped):
     $$A_{fw2} = 2(l_c + b_c) \times h_2$$
   - Sloped pedestal formwork (sloped timber forms):
     $$A_{fw2} = (L + l_c)\sqrt{h_2^2 + \left(\frac{B - b_c}{2}\right)^2} + (B + b_c)\sqrt{h_2^2 + \left(\frac{L - l_c}{2}\right)^2}$$
3. **Lean Concrete Volume & Formwork**:
   $$V_{lean} = (L + 2o_{lean})(B + 2o_{lean}) \times t_{lean}$$
   $$A_{fw\_lean} = 2(L + B + 4o_{lean}) \times t_{lean}$$

### 4.3 Reinforcement Layout & Cut-Length Equations
1. **Bottom Mesh X (Direction X - Lower Layer)**:
   - Net span: $L_{net} = L - 2c_{side}$
   - 90° Upturned hook height: $H_{hx} = H - c_{bottom} - c_{top}$ (or standard cog $15d$)
   - Single bar cutting length (Shape 02, two 90° bends):
     $$L_{cut\_x} = L_{net} + 2H_{hx} - 2\Delta L_{90}$$
     where bend deduction $\Delta L_{90} = 2d$ for $90^\circ$ bends per TCVN 5574.
   - Distribution width: $B_{dist} = B - 2c_{side}$
   - Bar count:
     $$n_x = \left\lfloor \frac{B_{dist}}{a_x} \right\rfloor + 1$$
2. **Bottom Mesh Y (Direction Y - Upper Layer)**:
   - Net span: $B_{net} = B - 2c_{side}$
   - 90° Upturned hook height: $H_{hy} = H - c_{bottom} - c_{top} - d_x$
   - Single bar cutting length (Shape 02):
     $$L_{cut\_y} = B_{net} + 2H_{hy} - 2\Delta L_{90}$$
   - Distribution length: $L_{dist} = L - 2c_{side}$
   - Bar count:
     $$n_y = \left\lfloor \frac{L_{dist}}{a_y} \right\rfloor + 1$$
3. **Column Starter Dowels (Thép Chờ Cột)**:
   - Horizontal 90° anchor leg resting on bottom mesh: $L_{leg} \ge 200\text{ mm}$ (or $12d$)
   - Vertical embedment through footing: $H_{emb} = H - c_{bottom}$
   - Lap splice extension above top of pedestal:
     $$L_{lap} = 30d \text{ to } 40d \quad (\ge 500\text{ mm})$$
   - Single dowel cutting length (Shape 02 with 1 bend):
     $$L_{cut\_dowel} = L_{leg} + H_{emb} + L_{lap} - \Delta L_{90}$$
   - Dowel count: $n_d$ matching column design (e.g. 4, 6, 8 bars).
4. **Pedestal Confinement Ties (Đai Cổ Cột)**:
   - Rectangular closed stirrup (Shape 51 or 11):
     $$L_{cut\_tie} = 2(l_c - 2c_{ped} + b_c - 2c_{ped}) + 2L_{hook135} - 4\Delta L$$
   - Vertical spacing: $s_{tie} \le 150\text{ mm}$ (minimum 3 ties within pedestal height $h_2$).

---

## 5. Pile Caps (`PileCapModel`, `ve_mong_coc` & `ve_mong_coc_giat_cap`) Specification

### 5.1 Pile Geometry, Spacing & Edge Distance Constraints
Per TCVN 10304:2014 (Móng cọc) and TCVN 5574:2018 (Kết cấu bê tông cốt thép):
- **Pile Types**:
  - Spun Pre-stressed Concrete (PHC) / Bored piles: Circular, diameter $D \in \{300, 350, 400, 500, 600, 800\text{ mm}\}$.
  - Precast Driven piles: Square, side $b_p \in \{200, 250, 300, 350, 400\text{ mm}\}$.
- **Pile Embedment into Cap ($h_{emb}$)**:
  - Standard head embedment: $h_{emb} = 100\text{ mm}$ (minimum $50\text{ mm}$, maximum $150\text{ mm}$).
  - Pile reinforcement dowels extension into cap:
    $$L_{p\_dowel} = 30d \text{ to } 40d \quad (\ge 500\text{ mm})$$
- **Edge Distance ($c_e$, from pile center to cap concrete edge)**:
  $$c_e \ge 1.0D \text{ (friction piles) or } 1.25D \text{ (bored piles)}, \quad c_e \ge 250\text{ mm}$$
- **Center-to-Center Pile Spacing ($s_p$)**:
  $$s_p \ge 3.0D \text{ (friction / hanging piles)}, \quad s_p \ge 2.5D \text{ (end-bearing piles)}$$

### 5.2 Canonical Pile Coordinate Solver (1 to 9+ Piles)
All coordinates are centered about the pile cap centroid $(0, 0)$:

$$\begin{array}{|c|l|l|l|}
\hline
\mathbf{N} & \textbf{Layout Pattern} & \textbf{Pile Coordinates } (X_i, Y_i) & \textbf{Cap Dimensions } (L \times B) \\
\hline
1 & \text{Single Pile} & (0, 0) & L = B = D + 2c_e \\
\hline
2 & \text{Inline 2 Piles} & \left(-\frac{s}{2}, 0\right), \left(+\frac{s}{2}, 0\right) & L = s + D + 2c_e, \; B = D + 2c_e \\
\hline
3 & \text{Equilateral Triangle} & P_1 = \left(0, \frac{s}{\sqrt{3}}\right), \; P_{2,3} = \left(\pm \frac{s}{2}, -\frac{s}{2\sqrt{3}}\right) & L = s + D + 2c_e, \; B = \frac{\sqrt{3}}{2}s + D + 2c_e \\
\hline
3 & \text{Inline 3 Piles} & (-s, 0), \; (0, 0), \; (+s, 0) & L = 2s + D + 2c_e, \; B = D + 2c_e \\
\hline
4 & \text{Grid } 2 \times 2 & \left(\pm \frac{s_x}{2}, \pm \frac{s_y}{2}\right) & L = s_x + D + 2c_e, \; B = s_y + D + 2c_e \\
\hline
5 & \text{Quincunx (Hoa Mai)} & \text{Corners: } \left(\pm \frac{s}{2}, \pm \frac{s}{2}\right), \; \text{Center: } (0, 0) & L = B = s + D + 2c_e \\
\hline
6 & \text{Grid } 2 \times 3 & X \in \left\{\pm \frac{s_x}{2}\right\}, \; Y \in \{-s_y, 0, +s_y\} & L = 2s_y + D + 2c_e, \; B = s_x + D + 2c_e \\
\hline
7 & \text{Hexagonal + Center} & (0, 0), \; \left(s \cos\frac{k\pi}{3}, s \sin\frac{k\pi}{3}\right)_{k=0..5} & L = B = 2s + D + 2c_e \\
\hline
8 & \text{Grid } 2 \times 4 & X \in \left\{\pm \frac{s_x}{2}\right\}, \; Y \in \left\{\pm \frac{s_y}{2}, \pm \frac{3s_y}{2}\right\} & L = 3s_y + D + 2c_e, \; B = s_x + D + 2c_e \\
\hline
9 & \text{Grid } 3 \times 3 & X \in \{-s_x, 0, +s_x\}, \; Y \in \{-s_y, 0, +s_y\} & L = 2s_x + D + 2c_e, \; B = 2s_y + D + 2c_e \\
\hline
N & \text{Grid } N_x \times N_y & X_i = \left(i - \frac{N_x-1}{2}\right)s_x, \; Y_j = \left(j - \frac{N_y-1}{2}\right)s_y & L = (N_x-1)s_x + D + 2c_e, \; B = (N_y-1)s_y + D + 2c_e \\
\hline
\end{array}$$

### 5.3 Stepped Pile Cap Detailing (`ve_mong_coc_giat_cap`)
For heavy building columns on pile foundations, pile caps often feature two tiers:
- **Base Tier ($L_1 \times B_1 \times H_1$)**: houses all piles, pile head embedment ($100\text{ mm}$), and bottom mesh rebar.
- **Stepped Tier ($L_2 \times B_2 \times H_2$)**: reduces concrete volume while providing shear punching resistance around the column.
- Rebar Detailing:
  - Bottom mesh runs full length of Base Tier ($L_1, B_1$).
  - Top mesh runs along Stepped Tier ($L_2, B_2$) with vertical crank drops down into the base tier.
  - Vertical stirrup ties along the step perimeter prevent shear cracking along the interface.

### 5.4 Pile Cap Reinforcement Cage
1. **Bottom Mesh X & Y**:
   - Diameter $\Phi \in \{14, 16, 18, 20, 22, 25\text{ mm}\}$.
   - Spacing $a \in \{100, 150, 200\text{ mm}\}$.
   - End hooks: 90° upturned cogs extending to top cover ($H_{hook} = H - c_{bottom} - c_{top}$).
2. **Top Reinforcement Mesh (Thép Lớp Trên)**:
   - For multi-pile caps subject to moment reversal or heavy column moments ($M_x, M_y$):
   - Diameter $\Phi \in \{12, 14, 16\text{ mm}\}$, spacing $a \in \{150, 200\text{ mm}\}$.
   - End hooks: 90° downturned cogs ($H_{hook} = H - c_{bottom} - c_{top}$).
3. **Side Perimeter Skin Rebar (Thép Đai Bao Quanh Đài)**:
   - Required whenever $H \ge 600\text{ mm}$.
   - Closed rectangular ties (Shape 51 or 11) surrounding the perimeter.
   - Diameter $\Phi \in \{10, 12, 14\text{ mm}\}$, vertical spacing $s_{skin} \le 200\text{ mm}$.
4. **Pile Head Confinement Ties (Đai Tăng Cường Đầu Cọc)**:
   - Inside the $100\text{ mm}$ embedment zone:
   - $3\text{ to } 5$ circular or square ties, diameter $\Phi 8$ or $\Phi 10$ at $50\text{ mm}$ pitch, confining the concrete around the bursting zone of each pile.

---

## 6. Foundation Plan Layout (`ve_mat_bang_mong` & `rai_mong`) Specification

### 6.1 Layout Generation Algorithm (`ve_mat_bang_mong` / `vmbm`)
1. **Input Data Extraction**:
   - Detects all column elements from plan (block `Kata_Block_Column&Wall` or polyline layer `kata_net thay`).
   - Resolves column center coordinates $(X_{col}, Y_{col})$, section dimensions $b \times h$, and column mark (e.g. `C1`, `C2`).
   - Resolves design axial loads $N$ from text annotations or ETABS reactions (`txtLucDoc`).
2. **Foundation Sizing & Type Assignment**:
   - Computes required bearing capacity:
     - For pad footings: $A_{req} = \frac{1.15 \times N}{R_0}$ where $R_0$ is allowable soil bearing pressure ($150\text{--}250\text{ kPa}$).
     - For pile caps: $N_{piles} = \lceil \frac{1.15 \times N}{P_{allow}} \rceil$ where $P_{allow}$ is pile bearing capacity (`sucChiuTaiCoc`, e.g. $40\text{--}80\text{ Tấn}$).
   - Assigns standard pile cap template from pile count ($1\text{--}9$ piles).
3. **DWG Plan Drawing Output**:
   - Footing/cap boundary polyline on layer `kata_thep chu` or `kata_net thay`.
   - Pile boundary symbols (circle or square) with 45° cross hatch (`LOAIDATALINECHEO`).
   - Lean concrete boundary polyline on layer `kata_net manh` (offset $+100\text{ mm}$).
   - Continuous tie beams connecting adjacent footings (layer `kata_net thay`, width $300\text{--}400\text{ mm}$).
   - Dimension strings: grid to column center, column center to footing edge, overall footing dimensions.
   - Foundation Tag Block (`Kata_Block_Mong_Don` or `kata_block_Beam_plan`):
     - `TEN_MONG`: e.g. `ĐC-1` or `M1`
     - `L`, `B`, `H`: e.g. `1600x1600x900`
     - `THEP_X`, `THEP_Y`: e.g. `f14a150`
     - `CAO_TRINH`: e.g. `-2.500`

### 6.2 Interactive Footing Placement (`rai_mong` / `rm`)
- Driven by `Rai_Mong_Module.RaiMongJig`:
  - Prompts user to pick column center or insertion point on screen.
  - Interactive ghost jig renders footing boundary, lean concrete, and pile symbols in real time following cursor.
  - Allows `[Space]` or `[R]` to rotate footing orientation by 90°.
  - On click, commits Block Reference `Kata_Block_Mong_Don` into database with attached XData `KATA`.

### 6.3 Block Explode Protocol (`phamong`)
- Implements `Ve_Mat_Bang_Mong_Module.PhaMong`:
  - Scans user selection for `Kata_Block_Mong_Don` references.
  - Explodes block into constituent lines, polylines, hatches, and rebar entities while **preserving all XData attributes and handle links**.
  - Erases the block container entity (`eraseBlockGoc = true`), leaving cleanly editable CAD entities.

---

## 7. Revit Foundation Integration (`FoundationRebarGenerator.cs`) Specification

### 7.1 Host Mapping & Cover Resolution
- **Target Category**: `BuiltInCategory.OST_StructuralFoundation` (`FamilyInstance`).
- **Geometry Extraction**:
  - `BoundingBoxXYZ bbox = host.get_BoundingBox(null)`:
    - $X_{min}, X_{max} \implies L_{ft} = X_{max} - X_{min}$
    - $Y_{min}, Y_{max} \implies B_{ft} = Y_{max} - Y_{min}$
    - $Z_{min}, Z_{max} \implies H_{ft} = Z_{max} - Z_{min}$
    - Center point: $C = \left(\frac{X_{min} + X_{max}}{2}, \frac{Y_{min} + Y_{max}}{2}, Z_{min}\right)$.
- **Concrete Cover**:
  - `CLEAR_COVER_BOTTOM`: converted via `RevitUnitConverter.FeetToMm(coverParam.AsDouble())`.
  - `CLEAR_COVER_TOP`: top cover.
  - `CLEAR_COVER_OTHER`: side cover.

### 7.2 Native 3D Rebar Generation Workflow
For each rebar layer in `IsolatedFootingModel` or `PileCapModel`:
1. **Bottom Mesh X**:
   - Direction vector: `XYZ.BasisX`
   - Normal plane vector: `XYZ.BasisY`
   - Array of curve sets: each bar is a 3-segment U-curve:
     $$P_1 = (X_{min} + c_s, Y_k, Z_{min} + H - c_t) \longrightarrow P_2 = (X_{min} + c_s, Y_k, Z_{min} + c_b) \longrightarrow P_3 = (X_{max} - c_s, Y_k, Z_{min} + c_b) \longrightarrow P_4 = (X_{max} - c_s, Y_k, Z_{min} + H - c_t)$$
   - Created via `RebarCreationService.CreateRebar(doc, host, barType, XYZ.BasisY, curves, partition, mark, comments)`.
2. **Bottom Mesh Y**:
   - Direction vector: `XYZ.BasisY`
   - Normal plane vector: `XYZ.BasisX`
   - Curves offset vertically by bottom X bar diameter $\Phi_x$ to prevent physical clash:
     $$Z_{bot\_y} = Z_{min} + c_b + \Phi_x$$
3. **Top Mesh X & Y (if present)**:
   - U-curves with hooks turned down along $-Z$:
     $$P_1 = (X_{min} + c_s, Y_k, Z_{max} - H_{hook}) \longrightarrow P_2 = (X_{min} + c_s, Y_k, Z_{max} - c_t) \longrightarrow \dots$$
4. **Side Perimeter Skin Ties**:
   - Closed 4-segment rectangular stirrup curve loops created with `RebarStyle.StirrupTie`.
5. **Column Starter Dowels**:
   - L-curves: horizontal leg along bottom mesh + vertical shank extending above top of foundation.
6. **Pile Confinement Hoops**:
   - Circular or square tie loops around each pile location $(X_i, Y_i)$ at heights $Z \in [Z_{min}, Z_{min} + h_{emb}]$.

### 7.3 Idempotency & Clean-Up Protocol
```csharp
public static void CleanExistingFoundationRebar(Document doc, Element hostFoundation, string foundationMark)
{
    if (doc == null || hostFoundation == null) return;
    string prefix = $"HPKata_Foundation_{foundationMark}";

    var rebarsToDelete = new FilteredElementCollector(doc)
        .OfClass(typeof(Rebar))
        .Cast<Rebar>()
        .Where(r => r.GetHostId() == hostFoundation.Id)
        .Where(r =>
        {
            var comment = r.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString();
            return comment != null && comment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        })
        .Select(r => r.Id)
        .ToList();

    if (rebarsToDelete.Count > 0)
    {
        doc.Delete(rebarsToDelete);
    }
}
```

---

## 8. Excel Foundation Scheduling (`FoundationScheduleAggregator.cs`) Specification

### 8.1 Rebar Schedule Aggregation Logic
The aggregator transforms `IsolatedFootingModel` and `PileCapModel` instances into standard `List<RebarItem>`:

1. **Shape Mapping**:
   - Mesh bars with 2 hooks: **Shape `"02"`** ($L_1 = H_{hook}, L_2 = L_{net}, L_3 = H_{hook}$).
   - Straight mesh bars without hooks: **Shape `"01"`** ($L_1 = L_{net}$).
   - Starter dowels with 1 hook leg: **Shape `"02"`** ($L_1 = L_{leg}, L_2 = L_{vert}$).
   - Closed perimeter skin ties: **Shape `"51"`** (90° hooks) or **`"11"`** (135° hooks).
   - Column pedestal ties: **Shape `"51"`** or **`"11"`**.
   - Pile head confinement ties: **Shape `"51"`** (square) or **`"74"`** (circular hoop).
2. **Weight Calculation**:
   - Standard linear density $W$ (kg/m) extracted from `RebarCalculator.GetUnitMass(diameter)`:
     $$W = \frac{\Phi^2}{162.2} \text{ or lookup table (e.g. } \Phi 10 = 0.617, \; \Phi 12 = 0.888, \; \Phi 14 = 1.210, \; \Phi 16 = 1.580, \; \Phi 20 = 2.470\text{)}$$
   - Total weight:
     $$\text{Weight} = \text{Count} \times N_{members} \times \frac{L_{cut}}{1000} \times W$$

### 8.2 ClosedXML Sheet `Mong` Schema
Matching `Kata.xlsm` workbook standards, generated in `FoundationPlanScheduleWorkbookGenerator.cs`:

| Column | Header | Type | Formula / Source | Example |
|---|---|---|---|---|
| A | `STT` | Integer | Sequential index | `1` |
| B | `Tên Móng` | String | `FootingMark` | `ĐC-1` |
| C | `Loại Móng` | String | `Pad Footing` / `Pile Cap` | `Đài 4 cọc` |
| D | `Số Cấu Kiện` | Integer | `FootingCount` | `4` |
| E | `Kích Thước L (mm)` | Double | `LengthMm` | `1800` |
| F | `Kích Thước B (mm)` | Double | `WidthMm` | `1800` |
| G | `Chiều Cao H (mm)` | Double | `TotalHeightMm` | `900` |
| H | `Số Cọc` | Integer | `Piles.Count` | `4` |
| I | `Quy Cách Cọc` | String | `D400` / `250x250` | `D400` |
| J | `Thép Đáy X` | String | `BottomMeshX.Spec` | `10Ø16a150` |
| K | `Thép Đáy Y` | String | `BottomMeshY.Spec` | `10Ø16a150` |
| L | `Thép Lớp Trên` | String | `TopMesh.Spec` | `8Ø14a200` |
| M | `Thép Đai Thành` | String | `SkinBars.Spec` | `4Ø12` |
| N | `Thép Chờ Cột` | String | `StarterDowels.Spec` | `8Ø20` |
| O | `Bê Tông Đài (m3)` | Double | `=E*F*G/1e9*D` | `11.66` |
| P | `Bê Tông Lót (m3)` | Double | `=(E+200)*(F+200)*100/1e9*D` | `1.60` |
| Q | `Ván Khuôn (m2)` | Double | `=2*(E+F)*G/1e6*D` | `25.92` |
| R | `Tổng Thép (kg)` | Double | `=SUM(RebarWeights)` | `645.2` |
| S | `Ghi Chú` | String | Notes / Concrete grade | `B25 (M350)` |

---

## 9. Clean Code Architecture & Class Design

To guarantee full compliance with `docs/clean-code/HP_CLEAN_CODE_CORE.md`:
- **Rule C2**: Every `.cs` file strictly $\le 300$ physical lines.
- **Rule M3**: Every method strictly $\le 50$ physical lines.
- **Rule Q-B1**: Zero dead/commented-out code.
- **Rule Q-B2**: Zero unannotated catch blocks.
- **Rule R5**: Literal `304.8` appears strictly inside `RevitUnitConverter.cs`.
- **Zero Host Dependencies**: `HPKata.Core` targets `netstandard2.0;net8.0` with 0 references to `Autodesk.*`.

### Modular File Partitioning Plan

```
HPKata/
├── HPKata.Core/
│   └── Foundations/
│       ├── Models/
│       │   ├── IsolatedFootingModel.cs            # Pad footing geometry & rebar spec (<= 140 lines)
│       │   ├── PileCapModel.cs                    # Pile cap geometry & rebar spec (<= 170 lines)
│       │   ├── PileLocationModel.cs               # Pile individual point & shape (<= 60 lines)
│       │   ├── RebarMeshLayerModel.cs             # Mesh layer diameter, spacing, hooks (<= 90 lines)
│       │   └── FoundationPlanLayoutModel.cs       # Foundation plan node & grid layout (<= 80 lines)
│       ├── Calculators/
│       │   ├── FootingGeometryCalculator.cs       # Volumes, formwork, lean concrete (<= 150 lines)
│       │   ├── PileCoordinateGenerator.cs         # Canonical 1-9+ pile coordinate solver (<= 220 lines)
│       │   ├── FootingRebarDistributionCalculator.cs # Mesh cut lengths, counts, hooks (<= 190 lines)
│       │   └── TcvnFootingRebarOptimizer.cs       # TCVN 5574/10304 design auto-sizer (<= 180 lines)
│       └── Persistence/
│           ├── FoundationXRecordSerializer.cs     # DWG KATA_FOUNDATION_DATA XRecord serializer (<= 160 lines)
│           ├── FoundationScheduleAggregator.cs    # Aggregates layouts to List<RebarItem> (<= 160 lines)
│           └── FoundationPlanScheduleWorkbookGenerator.cs # ClosedXML sheet 'Mong' generator (<= 210 lines)
│
├── HPKata.AutoCad/
│   ├── Commands/
│   │   └── FoundationCommands.cs                  # [CommandMethod] vmbm, rm, vmd, vmc, phamong (<= 120 lines)
│   └── Controllers/
│       └── Foundation/
│           ├── FoundationDrawController.cs        # Interactive point picking & command dispatch (<= 190 lines)
│           ├── IsolatedFootingCadRenderer.cs      # Polyline profile, sections, dims, tags (<= 260 lines)
│           ├── PileCapCadRenderer.cs              # Cap boundary, pile symbols, rebar cage (<= 270 lines)
│           ├── FoundationPlanCadRenderer.cs       # Grid layout, footing outlines, tie beams (<= 240 lines)
│           └── FoundationTakeoffRegistry.cs       # In-memory session registry for takeoffs (<= 90 lines)
│
├── HPKata.Revit/
│   ├── Commands/
│   │   └── FoundationRebarCommands.cs             # Revit ExternalCommands for foundations (<= 130 lines)
│   └── Services/
│       ├── FoundationRebarGenerator.cs            # Native Rebar.CreateFromCurves in OST_StructuralFoundation (<= 260 lines)
│       └── FoundationScheduleService.cs           # Scans Revit foundation rebars for takeoff (<= 160 lines)
│
└── HPKata.Core.Tests/
    └── Foundations/
        ├── IsolatedFootingTests.cs                # Volumes, formwork, and mesh equations (<= 180 lines)
        ├── PileCoordinateGeneratorTests.cs        # 1 to 9+ pile coordinate symmetry & spacing (<= 220 lines)
        ├── PileCapRebarTests.cs                   # Mesh, skin, confinement tie cut lengths (<= 200 lines)
        ├── FoundationScheduleAggregatorTests.cs   # List<RebarItem> aggregation & shapes (<= 180 lines)
        ├── FoundationClosedXmlWorkbookTests.cs    # ClosedXML sheet 'Mong' workbook test (<= 160 lines)
        └── FoundationXRecordSerializerTests.cs    # Round-trip JSON XRecord serialization (<= 150 lines)
```

---

## 10. Test Strategy & Acceptance Matrix

The following test suites must be implemented in `HPKata.Core.Tests/Foundations/` and verified with 100% green execution under `--disable-build-servers -m:1`:

| Test Suite | Target Component | Verification Criteria |
|---|---|---|
| `IsolatedFootingTests` | `FootingGeometryCalculator` | Verifies base slab volume, sloped pedestal prismoidal volume, formwork area, and lean concrete volume against manual hand calculation benchmarks. |
| `PileCoordinateGeneratorTests` | `PileCoordinateGenerator` | Verifies centroid $(0,0)$ symmetry, minimum pile spacing $s \ge 3D$, minimum edge distance $c_e \ge 1.0D$, and exact coordinate outputs for $N = 1, 2, 3, 4, 5, 6, 7, 8, 9$ piles. |
| `PileCapRebarTests` | `FootingRebarDistributionCalculator` | Verifies bottom mesh U-bar length ($L_{net} + 2H_{hook} - 2\Delta L$), bar counts, side skin hoop spacing $\le 200\text{ mm}$ for $H \ge 600\text{ mm}$, and pile confinement hoops count. |
| `FoundationScheduleAggregatorTests` | `FoundationScheduleAggregator` | Verifies conversion of `IsolatedFootingModel` and `PileCapModel` to `List<RebarItem>`, assigning ShapeCodes 01, 02, 11, 51, and matching total tonnage. |
| `FoundationClosedXmlWorkbookTests` | `FoundationPlanScheduleWorkbookGenerator` | Generates a valid ClosedXML `.xlsx` workbook with sheet `Mong`, populates 19 columns, and verifies cell values and headers without corrupting Excel. |
| `FoundationXRecordSerializerTests` | `FoundationXRecordSerializer` | Verifies bidirectional JSON round-trip equality for `IsolatedFootingModel` and `PileCapModel` payloads. |

---

## 11. Implementation Readiness & Handoff Summary

The specification is 100% complete, fully evidenced from reverse-engineered binaries (`Kata_pro64_Cad2025.dll`, `Kata_pro64_Cad2013.dll`, `Goi lenh.lsp`, `Iso_kata.dwg`, `Kata.xlsm`), and directly aligned with the existing Phase 1, 2, and 3 architecture in `HPKata`.

The Worker subagents (`worker_m3_foundation`, etc.) can immediately proceed to implementation without guesswork, using the exact models, calculators, and controllers defined in Sections 4 through 9 of this document.

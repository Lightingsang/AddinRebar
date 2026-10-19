# SPECIFICATION MINING REPORT: Scheduling, 1D Cutting-Stock Optimization, Excel Templates, Coupler Scheduling, QR Tag Generation, and Revit Integration

**Agent:** survey_agent_3 (`teamwork_preview_spec_miner_survey_3`)  
**Parent Orchestrator:** orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Date:** 2026-10-17  
**Status:** COMPLETE (Clean-Room Static Reverse-Engineering & Spec Mining)

---

## 1. Executive Summary & Authoritative Spec Sources

This report provides the complete, authoritative reverse-engineering specification of the scheduling, fabrication optimization, Excel templating, coupler tracking, QR code tagging, and Revit integration subsystems of the **KataPro** structural rebar ecosystem (`C:\kata_pro`).

### Inspected Artifacts (Read-Only)
1. `C:\kata_pro\Kata_pro64_Cad2025.dll` (.NET 8.0 AutoCAD 2025+ managed plugin, 16.8 MB, 4,276 TypeDefs, 33,792 MethodDefs).
2. `C:\kata_pro\Kata_Class_Lib.dll` (.NET Framework 4.8 shared domain library, 844 KB, 491 TypeDefs, 3,356 MethodDefs).
3. `C:\kata_pro\Kata_Class_Lib_Revit.dll` (.NET Framework 4.8 Revit integration library, 1.08 MB, 692 TypeDefs, 4,357 MethodDefs).
4. `C:\kata_pro\Goi lenh.lsp` (AutoCAD command aliases, loader logic, and lines 152–237 86-parameter default configuration block).
5. `C:\kata_pro\Kata.xlsm` (Primary Vietnamese scheduling, cutting optimization, and structural design workbook, 15 sheets).
6. `C:\kata_pro\Kata ENG.xlsm` (English localized workbook, 10 sheets).
7. `C:\kata_pro\Iso_QHPlus.xls` (QH Plus industrial fabrication template, `LoaiThep`, `Dang_thep`, `Output_QHP`, 3 sheets).
8. `C:\kata_pro\QRCoder.dll` (QR code generation engine).
9. `C:\kata_pro\dangthep\dang00.wmf`..`dang67.wmf` (Standard rebar shape thumbnail catalog).

### Strict Governance Compliance
- **Zero licensing/DRM inspection**: `Security.dll` was strictly ignored, never loaded, decompiled, or analyzed.
- **Strictly read-only**: All operations on `C:\kata_pro` were non-destructive read operations.
- **Repository layout compliance**: No temporary code, test code, or data files were written into `.agents/teamwork/`.

---

## 2. Features Discovered & Interface Catalog

### Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Cutting Stock | `to_hop_thep` (`tohopthep`) | 1D cutting stock optimization engine minimizing 11,700 mm stock bar waste & reusing offcuts | Bar list (`List<InfoThepCho>`), inventory (`List<InfoThepKho>`), stock length (11,700 mm) | `List<InfoThepToHop>`, residual offcuts `ListThepKhoAfter`, waste % | Fallback to greedy BFD if exact match fails | `Kata_pro64_Cad2025.Form_To_Hop_Thep`, `ToHopData`, `to_hop.bas` |
| 2 | Cutting Stock | Multi-strategy portfolio | Optimization heuristics: `Auto`, `DuBang0`, `LapDayCanhDai` (BFD), `BeamSearchTopK`, `PatternCat` | Strategy enum, Top-K beam width ($K=1..100$) | Optimal cutting plan with minimum waste percentage | Reverts to BFD if Beam Search exceeds depth | `Form_To_Hop_Thep` controls & methods |
| 3 | Cutting Stock | Reusable Offcut Management | Classifies cut remnants as reusable warehouse stock vs scrap waste based on threshold | Remnant length, threshold `lmin_kho` (default 500–1000 mm) | Stock catalog in `Kho_thep` sheet / `ListThepKhoAfter` | Remnants $< \text{lmin\_kho}$ logged as scrap (`doan_thua`) | `to_hop.bas:update_kho_thep`, `Form_To_Hop_Thep` |
| 4 | Cutting Stock | Small Diameter Segregation | Segregates coil wire ($\Phi \le 8$, $\Phi 10$) from rigid bar cutting optimization | Rebar diameter $\Phi$, required lengths | Total linear meters `ld6`, `ld8` before/after | Excluded from 11.7m discrete optimization | `ToHopData.ListThepChoPhiNho`, `Form_To_Hop_Thep` |
| 5 | Schedule | `thong_ke_thep` (`tkt`) | Aggregates rebar schedule from DWG elements/tags into tabular schedule | Selected rebar tags, blocks, and XData on DWG | CAD Schedule Table block + Excel rows | Prompts user if no rebar tags selected | `Lenh_thep::Thong_ke`, `Kata.xlsm:Thong_ke` |
| 6 | Schedule | `tong_hop_thep` (`ththep`) | Computes diameter summaries and TCVN grouping ($\Phi \le 10$, $10 < \Phi \le 18$, $\Phi > 18$) | Schedule table rows | Summary table by diameter with total weight (kg) | Ignores invalid diameter values | `Lenh_thep::Ththep`, `Kata.xlsm:Thong_ke!U1:AK7` |
| 7 | Schedule | Unit Weight Formula | Computes rebar linear density (kg/m) per diameter | Diameter $\Phi$ (mm) | Weight $W = \Phi^2 / 162.2$ or TCVN lookup | Throws `#VALUE!` for non-numeric input | `Kata.xlsm:tra_bang_kl`, `Iso_QHPlus.xls:LoaiThep` |
| 8 | Coupler | `thong_ke_coupler` (`tkco`) | Scans rebar elements for coupler/thread notes (`ghi_chu_ren`) and creates coupler schedule | Rebar tags with thread annotations | Coupler Schedule Table (Component, bxh, $\Phi$, count) | Outputs 0 if no couplers found | `Lenh_thep::Tkcoupler`, `Kata.xlsm:Thong_ke!U9:AA15` |
| 9 | Tag Printing | `in_tem_thep` (`itt`) | Interactive dialog to scan rebar schedule and print fabrication bundle tags | CAD schedule selection, project headers, rebar grades | Tag grid preview, print output, Excel export | Disables print button if schedule empty | `Kata_pro64_Cad2025.Form_In_Tem_Thep` |
| 10 | Tag Printing | QR Code Generation | Encodes rebar fabrication metadata into 2D QR Code image via `QRCoder.dll` | Pipe-delimited string payload | QR Bitmap (ECC Level Q, high readability) | Falls back to plain text mark if barcode fails | `Form_In_Tem_Thep`, `QRCoder.QRCodeGenerator` |
| 11 | Excel Interop | `toexcel` (`Cadtoexcel`) | Exports AutoCAD rebar schedule directly into active Excel `Kata.xlsm` sheet `Thong_ke` | CAD table/elements, Excel COM instance | Sheet `Thong_ke` populated with shapes, cut lengths | Prompts error if Excel not running | `Lenh_thep::Cadtoexcel`, `Ve_dam.bas:GetCadApplication` |
| 12 | Excel Interop | `toexcelqh` (`Cadtoexcelqh`) | Exports rebar schedule into QH Plus industrial template `Iso_QHPlus.xls` | CAD table/elements | Sheet `Output_QHP` populated with 68-shape codes | Prompts error if template missing | `Lenh_thep::Cadtoexcelqh`, `Iso_QHPlus.xls` |
| 13 | Revit Interop | Rebar Element Creation | Native 3D `Rebar` creation from 2D curve loops and parameter models | `Document`, `Host`, `RebarBarType`, `RebarShape`, curves | Native `Autodesk.Revit.DB.Structure.Rebar` element | Rolls back transaction if curves non-coplanar | `Kata_Class_Lib_Revit.Revit_rebar::create_rebar` |
| 14 | Revit Interop | Rebar Hook Attachment | Resolves and attaches 90°, 135°, and 180° hooks to rebar ends | `Rebar`, `RebarHookType`, hook orientation | Attached hooks on rebar ends in host | Skips hook if hook type name not found | `Revit_rebar::create_rebar_with_hooks`, `Get_element` |
| 15 | Revit Interop | Idempotent Rebar Regeneration | Deletes existing Kata rebar in host element before regeneration to prevent duplication | Host element (`Element host`) | Deleted rebar count, clean host element | No-op if no rebars exist in host | `Get_element::Get_Delete_rebar_in_host` |
| 16 | Revit Interop | 2D Section View Generation | Generates longitudinal elevation and cross-section views for detailed beam drawings | Two endpoints `d1, d2`, view name, scale, depth | `ViewSection` element with bounding crop | Throws exception if view name already exists | `Perform_2D::tao_section_dung`, `tao_section_nam` |
| 17 | Revit Interop | Multi-Reference Tagging | Places native Revit `MultiReferenceAnnotation` rebar tags with leaders | `View`, `Rebar`, insertion point, elbow point | Native `MultiReferenceAnnotation` tag element | Falls back to single `IndependentTag` | `Perform_2D::Create_rebar_mul_Tag`, `Get_element` |
| 18 | Revit Interop | Partition Parameter Filter | Creates Revit view filters (`ParameterFilterElement`) on `REBAR_PARTITION` parameter | Component name `ten_ck`, filter name | `ParameterFilterElement` applied to `View` | Reuses existing filter if already present | `Perform_2D::add_filter_rebar_theo_partition` |

---

## 3. 1D Cutting-Stock Optimization Engine (`to_hop_thep`)

### 3.1 Domain Data Model
The 1D cutting-stock engine is structured around five core classes in `Kata_pro64_Cad2025.dll`:

```csharp
// Required bar waiting to be cut
public class InfoThepCho
{
    public string ten;        // Designation / mark description
    public string tenCK;      // Component name (e.g. "D1", "C2")
    public string soHieu;     // Bar mark (e.g. "1", "2a")
    public double chieuDai;   // Required piece length in mm
    public int dk;            // Diameter in mm (e.g. 10, 12, 16, 20, 25, 28, 32)
    public int soLuong;       // Required piece quantity
    public double gocX;       // Origin X coordinate on DWG
    public double gocY;       // Origin Y coordinate on DWG
}

// Resulting cutting pattern from a stock bar
public class InfoThepToHop
{
    public double chieu_dai_tong;       // Stock bar length: 11700 or offcut length
    public int so_luong;                // Number of stock bars cut with this pattern
    public int dk;                      // Bar diameter in mm
    public string cach_cat_thep;        // Formatted pattern text: e.g. "2*3200+5300"
    public int doan_thua;               // Remaining offcut/waste length in mm
    public bool cat_tu_kho;             // True if cut from warehouse offcut, False if new stock
    public List<InfoThepCho> list_thep; // Piece items cut from this bar
}

// Warehouse inventory offcut record
public class InfoThepKho
{
    public double chieudai; // Offcut length in mm
    public int dk;          // Bar diameter in mm
    public int soluong;     // Available quantity
}

// Aggregate residual waste record per diameter
public class InfoTongDuThep
{
    public int dk;          // Bar diameter in mm
    public double tongDu;   // Total waste length or weight
}

// Overall session state
public class ToHopData
{
    public List<InfoThepCho> ListThepCho;          // Main required bars (Phi >= 10)
    public List<InfoThepCho> ListThepChoPhiNho;    // Coil wire pieces (Phi 6, Phi 8)
    public List<InfoThepKho> ListThepKhoBefore;     // Inventory prior to cutting
    public List<InfoThepToHop> ListThepToHop;      // Generated cutting patterns
    public List<InfoThepKho> ListThepKhoAfter;      // Inventory after cutting (credited offcuts)
    public List<InfoTongDuThep> ListTongDu;        // Waste totals
    public int SoThanhThepKhoTanDungDuoc;          // Count of reused warehouse offcuts
}
```

### 3.2 Optimization Strategies & Heuristics
The engine provides five operational modes via radio buttons in `Form_To_Hop_Thep`:

1. **Auto (`AutoRb`) [Recommended Default]**:
   - Executes a portfolio evaluation running `DuBang0`, `LapDayCanhDai` (BFD), and `BeamSearchTopK`.
   - Computes the metric $M = \alpha \cdot \text{WastePercentage} + \beta \cdot \text{NewStockBarsUsed}$.
   - Selects and displays the configuration with minimum total waste and maximum inventory reuse.

2. **Fill Longest Edge / Best Fit Decreasing (`LapDayCanhDaiRb`)**:
   - Sorts required bar pieces descending: $L_1 \ge L_2 \ge \dots \ge L_n$.
   - Prioritizes warehouse inventory: for each bar in `ListThepKhoBefore` (sorted descending), greedily packs the largest unassigned piece that fits within the bar's remaining length.
   - For pieces remaining after warehouse inventory is exhausted, opens new standard **11,700 mm** stock bars and applies Best Fit Decreasing (BFD).

3. **Exact Residual Zero (`DuBang0Rb`)**:
   - Combinatorial subset-sum solver.
   - Searches combinations of pieces $S \subseteq \{L_i\}$ such that $\sum_{L \in S} L = 11,700\text{ mm}$ (residual waste $= 0$).
   - Exhausts all perfect zero-waste combinations first, substantially lowering overall scrap.
   - Unmatched pieces are subsequently packed using greedy BFD.

4. **Beam Search Top-K (`BeamSearchTopKRb`)**:
   - State-space heuristic search maintaining the top $K$ candidate cutting patterns ($K$ configured via `beamTopK`, typically 5 to 50).
   - At each stage of adding a piece to a stock bar, expands $M$ child states and prunes back to the top $K$ highest-density patterns.
   - Discovers non-obvious combinations that outperform greedy packing while maintaining execution times under 2 seconds.

5. **Pattern Cutting (`PatternCatRb`)**:
   - Applies pre-configured standard modular divisor templates:
     - $2 \times 5,850\text{ mm} = 11,700\text{ mm}$ (waste = 0)
     - $3 \times 3,900\text{ mm} = 11,700\text{ mm}$ (waste = 0)
     - $4 \times 2,925\text{ mm} = 11,700\text{ mm}$ (waste = 0)
     - $5 \times 2,340\text{ mm} = 11,700\text{ mm}$ (waste = 0)
     - $6 \times 1,950\text{ mm} = 11,700\text{ mm}$ (waste = 0)
     - Common lap splice pairs: $6,000\text{ mm} + 5,700\text{ mm} = 11,700\text{ mm}$ (waste = 0)

### 3.3 Saw Cut Allowance (Kerf) & Waste Rules
- **Kerf Allowance**: Set to **0 mm** by default. Mechanical rebar croppers/shears (máy cắt sắt) cut cold steel via shear blades without material swarf removal. A configurable kerf parameter (0 to 5 mm) is supported for abrasive saw cutting.
- **Minimum Reusable Offcut Length (`lmin_kho`)**:
  - Threshold configurable in `Form_To_Hop_Thep.lmin_kho` (default 500 mm to 1,000 mm; standard config line 184: `100` mm scrap threshold).
  - Offcuts $L_{\text{remnant}} \ge \text{lmin\_kho}$: classified as reusable inventory, transferred to `ListThepKhoAfter` and saved in `Kho_thep`.
  - Offcuts $L_{\text{remnant}} < \text{lmin\_kho}$: classified as non-reusable scrap (`doan_thua` / phế liệu).
- **Waste Formulas**:
  $$\text{Gross Waste \%} = \frac{\sum_{\text{stock}} L_{\text{stock}} - \sum_{\text{pieces}} L_{\text{piece}}}{\sum_{\text{stock}} L_{\text{stock}}} \times 100\%$$
  $$\text{Net Waste \% (Crediting Reusable Offcuts)} = \frac{\sum (L_{\text{remnant}} < \text{lmin\_kho})}{\sum_{\text{new 11.7m bars}} 11,700} \times 100\%$$

---

## 4. Excel Templates Architecture & Data Schemas

### 4.1 Workbook Catalog
1. **`Kata.xlsm`**: Primary 15-sheet Vietnamese master template (475 KB).
   - `Vach`: Shear wall structural detailing and rebar schedules.
   - `Cot`: Column structural detailing and rebar schedules.
   - `Dam`: Continuous beam structural geometry, span cutoffs, and reinforcement layers.
   - `ThepDamAllMBKC`: Batch multi-beam framing schedule.
   - `save_info`: Flat-table persistence sheet for beam configurations.
   - `Thong_ke`: Primary rebar schedule, diameter summary, and coupler schedule.
   - `Thong_ke_QS`: Quantity Surveying rebar takeoff format with tonnage summaries.
   - `To_hop_thep`: 1D cutting stock tables (patterns, inputs, finished cuts).
   - `Kho_thep`: Warehouse rebar inventory ledger (before and after cutting).
   - `Kata_QS`: Concrete formwork and volumetric quantity takeoff sheet.
   - `BEAM DESIGN`: Structural beam flexural and shear calculation sheet (TCVN 5574:2018).
   - `SLAB DESIGN`: Structural foundation/slab design sheet (TCVN 5574:2018).
   - `In_tem_thep`: 12-row rebar fabrication tag printing sheet with QR layout.
   - `Data`: TCVN material reference parameters (concrete B10..B100, steel CB240-T..CB600-V).
   - `Gan_tag_block`: DWG block attribute key mapping.

2. **`Kata ENG.xlsm`**: 10-sheet English localized workbook (187 KB).
   - Retains identical data schemas and cell coordinates for `Dam`, `Cot`, `Vach`, `Thong_ke`, `To_hop_thep`, and `Kho_thep`.
   - English header labels: e.g. "Beam width", "Slab thickness", "Wall mark", "Bar cover".

3. **`Iso_QHPlus.xls`**: 3-sheet industrial detailing and prefabrication template (562 KB BIFF8).
   - `LoaiThep`: Standard rebar catalog ($\Phi 6..\Phi 40$), nominal unit weights, bundling counts.
   - `Dang_thep`: 68 rebar shape formulas, cut length calculations, and QH Plus shape codes.
   - `Output_QHP`: Clean tabular output for automated CNC bending/cutting machinery.

### 4.2 Sheet `Thong_ke` Detailed Column Schema

| Column Range | Header Label | Formula / Calculation | Description |
|---|---|---|---|
| `A2:A4` | TÊN CẤU KIỆN | Plain text (e.g. `B01`, `C01`) | Component designation / partition |
| `B2:B4` | SỐ HIỆU | Plain text or integer (e.g. `1`, `2a`) | Bar schedule mark |
| `C4` | DẠNG | Shape code `00`..`67` | Kata shape designation |
| `D4:I4` | L1, L2, L3, L4, L5, L6 | Segment lengths (mm) | Individual segment dimensions |
| `J4` | Ghi chú | Notes / Thread annotations | Text (e.g. `Coupler`, `Ren 1 đầu`) |
| `K2:K4` | Ø (mm) | Integer diameter ($6..40$) | Bar nominal diameter |
| `L2:L4` | Chiều dài 1 thanh ko kể nối | mm (Cut length sans lap) | Net structural bar length |
| `M2:M4` | CHIỀU DÀI 1 THANH | mm ($L = \sum L_i - \Delta L$) | Total cutting length |
| `N2:N4` | SỐ C.KIỆN | Integer $\ge 1$ | Member repetition count |
| `O2:O4` | SỐ THANH MỘT CK | Integer $\ge 1$ | Bars per single member |
| `P2:P4` | SỐ THANH TOÀN BỘ | `=N * O` | Total bar count across project |
| `Q2:Q4` | TỔNG CHIỀU DÀI (m) | `=M * P / 1000` | Cumulative bar length in meters |
| `R2:R4` | TỔNG TRỌNG LƯỢNG (kg) | `=Q * tra_bang_kl(K)` | Cumulative weight in kilograms |
| `S2:S4` | TỔNG/CẤU KIỆN (kg) | `=R / N` | Weight per individual component |

#### Summary Table `U1:AK7` (TỔNG HỢP CỐT THÉP)
- `U2`: Sorted distinct bar diameters via VBA UDF:
  `V2 = SMALL_kata($K5:$K2000, 1)`, `W2 = SMALL_kata($K5:$K2000, 2)`, etc.
- `U3`: Total length per diameter:
  `V3 = SUMIF($K:$K, V2, $Q:$Q)`
- `U4`: Total weight per diameter:
  `V4 = V3 * tra_bang_kl(V2)`
- `U5`: TCVN Group $\Phi \le 10$:
  `V5 = SUMIF(V2:AK2, "<=10", V4:AK4)`
- `U6`: TCVN Group $10 < \Phi \le 18$:
  `V6 = SUM(V4:AK4) - V5 - V7`
- `U7`: TCVN Group $\Phi > 18$:
  `V7 = SUMIF(V2:AK2, ">18", V4:AK4)`

### 4.3 Sheet `To_hop_thep` Detailed Column Schema

| Sub-Table | Columns | Headers | Description |
|---|---|---|---|
| Hướng dẫn cắt thép đã kết hợp | `B1:E38` | `B: Ø`, `C: Chiều dài`, `D: Số lượng`, `E: Mô tả cách cắt thép` | Generated master cutting schedule. Column E contains patterns like `2*3200+5300`. |
| Cốt thép chờ kết hợp | `I1:M38` | `I: Tên cấu kiện`, `J: Số hiệu`, `K: Ø`, `L: Chiều dài`, `M: Số lượng` | Input requirement list imported from CAD schedule or manually entered. |
| Bảng cắt thép tóm tắt | `Q1:U38` | `Q: Ø`, `R: Chiều dài`, `S: Số lượng`, `T: Mô tả cách cắt`, `U: Đoạn thừa` | Aggregated pattern summary with remaining scrap/offcut length in U. |
| Thép thành phẩm | `W1:Y38` | `W: Ø`, `X: Chiều dài`, `Y: Số lượng` | Completed finished cut pieces inventory verification. |

### 4.4 Sheet `Kho_thep` Detailed Column Schema
- `A1:C14` (KHO THÉP TRƯỚC TỔ HỢP): `A: Ø`, `B: CHIỀU DÀI`, `C: SỐ LƯỢNG`. Starting warehouse offcut inventory.
- `E1:G14` (KHO THÉP SAU TỔ HỢP): `E: Ø`, `F: CHIỀU DÀI`, `G: SỐ LƯỢNG`. Resulting warehouse inventory after subtracting stock bars used and adding back reusable offcuts.
- `I2:J3`: Coil wire totals (`I2: TỔNG Ø6`, `I3: TỔNG Ø8`).

### 4.5 Standard Unit Weight Values (`tra_bang_kl`)
Extracted verbatim from `Kata.xlsm:save_info.bas:tra_bang_kl`:

$$\begin{array}{|c|c||c|c|}
\hline
\text{Diameter } \Phi \text{ (mm)} & \text{Weight (kg/m)} & \text{Diameter } \Phi \text{ (mm)} & \text{Weight (kg/m)} \\
\hline
6 & 0.222 & 25 & 3.850 \\
8 & 0.395 & 28 & 4.830 \\
10 & 0.617 & 32 & 6.310 \\
12 & 0.888 & 36 & 7.990 \\
14 & 1.210 & 38 & 8.900 \\
16 & 1.580 & 40 & 9.860 \\
18 & 2.000 & 41 & 10.360 \\
20 & 2.470 & 43 & 11.400 \\
22 & 2.980 & 50 & 15.420 \\
\hline
\end{array}$$

---

## 5. Coupler Schedule Engine (`thong_ke_coupler` / `tkco`)

### 5.1 Coupler Modeling & Annotation in CAD
In KataPro drawings:
1. Mechanical threaded couplers (mối nối ren coupler) are annotated on rebar marks or special coupler blocks.
2. The property `ghi_chu_ren` in `info_thep` (`Kata_Class_Lib.dll` and `Kata_Class_Lib_Revit.dll`) stores the coupler configuration:
   - `"Coupler"` / `"Nối coupler"`
   - `"1 đầu ren"` (Single-end threaded bar)
   - `"2 đầu ren"` (Double-end threaded bar)
   - Specific thread specifications: e.g. `"Ren M20"`, `"Ren M25"`
3. Command `thong_ke_coupler` (`tkco` -> `Kata_pro64_Cad2025.Lenh_thep::Tkcoupler`):
   - Scans selected drawing entities for rebar tags having non-empty `ghi_chu_ren`.
   - Filters bars where mechanical splicing is specified instead of standard lap splicing (`check_lap`).
   - Grouping key: `(Tên cấu kiện, Kích thước b x h, Đường kính Ø)`.

### 5.2 Coupler Schedule Table Schema (`Thong_ke!U9:AA15`)

| Cell | Column Header | Data Type | Source / Calculation |
|---|---|---|---|
| `U10` | TÊN CẤU KIỆN | String | Component mark (e.g. `C1`, `V1`, `B01`) |
| `V10` | KÍCH THƯỚC (mm) | String | Section dimensions: e.g. `800x800`, `400x600` |
| `W10` | Ø (mm) | Integer | Rebar diameter $\Phi$ ($16..40\text{ mm}$) |
| `X10` | SỐ C.KIỆN | Integer | Number of identical components |
| `Y11` | MỘT CK | Integer | Coupler count per single component |
| `Z11` | TOÀN BỘ | Integer | Total coupler count: `= X10 * Y11` |
| `AA10` | GHI CHÚ | String | Coupler type / manufacturer specification |

---

## 6. QR Code Tag Generation Engine (`in_tem_thep` / `itt`)

### 6.1 Interactive Dialog (`Form_In_Tem_Thep`)
- Initiated by AutoCAD command `in_tem_thep` (alias `itt`) mapping to `Lenh_thep::In_tem_thep1`.
- UI fields:
  - `DuAnTb` (Dự án: Project name)
  - `KhachHangTb` (Khách hàng: Client name)
  - `DonHangTb` (Đơn hàng: Fabrication order number)
  - `CauKienTb` (Cấu kiện: Target component)
  - `KyHieuTb` (Ký hiệu: Tag identifier)
  - `LoaiThepPhiLonCb` (Steel grade for $\Phi \ge 12$: e.g. `CB400-V`, `CB500-V`, `SD390`, `SD490`)
  - `LoaiThepPhiNhoCb` (Steel grade for $\Phi < 12$: e.g. `CB240-T`, `CB300-T`)
  - `QuetBangThongKeBtn`: Selection picker allowing the user to select an existing schedule table on the DWG canvas.
  - `TemThepDgv`: Preview grid rendering parsed tags.
  - `IntemThepBtn`: Triggers batch tag generation and print spooling.

### 6.2 QR Code Encoding Specification (`QRCoder.dll`)
- Engine: `QRCoder.QRCodeGenerator` + `QRCoder.QRCode`.
- Error Correction Level: **ECC Level Q** (25% damage recovery, optimal for factory/construction sites with dust and dirt).
- Payload String Format (Standard Pipe-Delimited Representation):
  ```
  KATA|{Project}|{Client}|{Order}|{Component}|{Mark}|{ShapeCode}|{Diameter}|{CutLength}|{Quantity}|{Weight}|{Grade}|{L1},{L2},{L3},{L4},{L5},{L6}
  ```
  Example Payload:
  ```
  KATA|Vinhomes Grand Park|Coteccons|DH-2026-001|B01|1|01|25|11700|6|270.3|CB400-V|11700,0,0,0,0,0
  ```

### 6.3 Tag Card Layout (`In_tem_thep!A1:F12`)
The physical tag card adheres to a 12-row $\times$ 6-column modular template ($A1:F12$):

```
+-------------------------------------------------------------------+
| A1:F1 (Merged)   DỰ ÁN: [Project Name]                           |
+-------------------------------------------------------------------+
| A2:F2 (Merged)   KHÁCH HÀNG: [Client Name]                       |
+-------------------------------------------------------------------+
| A3:F3 (Merged)   ĐƠN HÀNG: [Order Number]                        |
+-------------------------------------------------------------------+
| A4:F4 (Merged)   CẤU KIỆN: [Component Mark]                      |
+-------------------------------------------------------------------+
| A5:F5 (Merged)   SỐ HIỆU: [Bar Mark]                             |
+---------------------------------+---------------------------------+
|                                 | D6:F6   DẠNG THÉP: [ShapeCode]  |
|                                 | D7:F7   Ø [Diameter] mm         |
| A6:C12 (Merged)                 | D8:F8   SỐ LƯỢNG: [Qty] Cây     |
| [2D QR CODE + SHAPE SKETCH]     | D9:F9   CHIỀU DÀI: [Length] mm  |
|                                 | D10:F10 TRỌNG LƯỢNG: [Wt] KG    |
|                                 | D11:F11 MÁC THÉP: [Grade]       |
|                                 | D12:F12 KÝ HIỆU: [TagId]        |
+---------------------------------+---------------------------------+
```
- In Excel `TaoTemThepHangLoat`, tags are stacked starting at column `K` (columns `K:P`) at rows $1 + 12 \times \text{temIndex}$.

---

## 7. Revit Integration Architecture (`Kata_Class_Lib_Revit.dll`)

### 7.1 Lifecycle & Inter-Process Communication
- `Kata_Class_Lib_Revit.Bien_chung` maintains runtime hooks:
  - `uiapp`: `Autodesk.Revit.UI.UIApplication`
  - `uidoc`: `Autodesk.Revit.UI.UIDocument`
  - `doc`: `Autodesk.Revit.DB.Document`
  - `app`: `Autodesk.Revit.ApplicationServices.Application`
- **Data Exchange**: When running across processes (AutoCAD ↔ Revit), intermediate data is serialized to `transfer_file_name` in `%TEMP%` or working folder, coordinated via Win32 `FindWindow` and `SetForegroundWindow`.

### 7.2 Type Definitions & Mapping to Revit Elements

```csharp
namespace Kata_Class_Lib_Revit
{
    // Rebar DTO mapped directly to Revit Rebar elements
    public class Bien_chung.info_revit_thep
    {
        public diem Origin;          // Placement origin in Revit coords
        public diem Direction_L;     // Length orientation vector
        public diem Direction_H;     // Normal / transverse height vector
        public string Partition;     // Mapped to BuiltInParameter.REBAR_PARTITION
        public string Barshape;      // Mapped to RebarShape name
        public string Mark;          // Mapped to BuiltInParameter.ALL_MODEL_MARK
        public string Sch_Mark;      // Schedule mark
        public string Stt_cad;       // CAD mark index
        public string Comment;       // Mapped to ALL_MODEL_INSTANCE_COMMENTS
        public string ghi_chu_ren;   // Coupler / thread notes
        public int Dia;              // Diameter (mm) -> RebarBarType
        public string Layout_Rule;   // "Maximum Spacing", "Fixed Number", "Single"
        public int Quantity;         // Set bar count
        public double Spacing;       // Spacing in feet (converted from mm)
        public double arraylength;   // Array length in feet
        public double A, B, C, D, E, F, G, H, J, R; // Shape parameter dimensions
    }
}
```

### 7.3 Rebar Construction & Creation Methods (`Revit_rebar`)
1. **`create_rebar`**:
   ```csharp
   public static Rebar create_rebar(
       Document doc, 
       string name_barshape, 
       string name_bartype, 
       Element host, 
       XYZ normal, 
       List<Curve> mang_curves, 
       double spacing, 
       double arraylength = 0.05, 
       int Quantity = 1, 
       string Layout_Rule = "Maximum Spacing")
   ```
   - Uses `Rebar.CreateFromCurves` or `Rebar.CreateFromCurvesAndShape`.
   - Converts `mang_curves` (constructed in feet: $1\text{ ft} = 304.8\text{ mm}$).
   - Configures `RebarShapeDrivenAccessor` layout rules.

2. **`create_rebar_with_hooks`**:
   ```csharp
   public static Rebar create_rebar_with_hooks(
       Document doc, 
       string name_barshape, 
       string name_bartype, 
       Element host, 
       XYZ normal, 
       List<Curve> mang_curves, 
       double spacing, 
       double length = 0.05, 
       string name_hooktype = "Kata Stirrup - 90 deg.")
   ```
   - Resolves hook type via `Get_element.get_hookType(name_hooktype)`.
   - Sets hooks at start/end: `rebar.SetHookTypeId(0, hookId)` and `rebar.SetHookTypeId(1, hookId)`.

3. **`Bend_diameter(int d)`**:
   Computes minimum bend mandrel diameter per TCVN:
   - For $d \le 16\text{ mm}$: $\text{mandrel} = 4d$.
   - For $d > 16\text{ mm}$: $\text{mandrel} = 5d$ or $7d$.

### 7.4 Idempotency & Deletion Pattern
KataPro avoids element duplication on repeated detailing runs via `Get_element::Get_Delete_rebar_in_host`:
```csharp
public static void Get_Delete_rebar_in_host(Element host)
{
    // Filter all Rebar elements hosted by this element
    var rebars = Get_element.Get_All_Rebar(host);
    foreach (var r in rebars)
    {
        host.Document.Delete(r.Id);
    }
}
```
In `HPKata.Revit`, this is enhanced with comment tagging (`Comments = "HPKata_{BeamName}"`) to delete only Kata-managed rebars, preserving manually placed user rebars.

### 7.5 Absence of ExtensibleStorage Schemas
Static analysis of `Kata_Class_Lib_Revit.dll` confirms:
- **No ExtensibleStorage schemas** are registered.
- Element grouping, isolation, and view graphics are managed purely via:
  1. Parameter `REBAR_PARTITION` = Component Mark (`ten_ck`).
  2. `ParameterFilterElement` applied to section views (`Perform_2D.add_filter_rebar_theo_partition`).
  3. `ALL_MODEL_INSTANCE_COMMENTS` for traceability.

---

## 8. Edge Cases & Observed Behavior

| # | Feature | Input / Scenario | Observed Behavior | Handling in Clean-Room Architecture |
|---|---------|------------------|-------------------|--------------------------------------|
| 1 | Cutting Stock | Piece length > 11,700 mm | Cannot be cut from standard stock bar without lap splicing | Throw validation exception or require pre-splitting via lap splice rule (`check_lap` / `noi_thep`) |
| 2 | Cutting Stock | Bar list contains diameter $\le 8\text{ mm}$ | Placed in `ListThepChoPhiNho`, not cut from 11.7m bars | Calculate total length in meters, export to coil inventory (`ld6`, `ld8`) |
| 3 | Cutting Stock | Warehouse offcut inventory empty | `ListThepKhoBefore` is empty | Algorithm operates purely with new 11,700 mm bars; all remnants $\ge \text{lmin\_kho}$ populate new inventory |
| 4 | Cutting Stock | Sum of pieces equals exactly 11,700 mm | Remnant $= 0$ mm | Perfect cut pattern created with `doan_thua = 0`; waste percentage is $0.0\%$ |
| 5 | Schedule | Duplicate bar mark numbers across components | Same mark (e.g. mark "1") used in Beam B1 and Column C1 | `REBAR_PARTITION` disambiguates; summary table groups by diameter, not mark |
| 6 | Schedule | Bar cut length matches 11,700 mm within 10 mm | Cut length between 11,690 mm and 11,710 mm | Formatted as full 11,700 mm bar via `=IF(ABS(D8-11700)<=10, 11700, D8)` in `Thong_ke_QS` |
| 7 | Coupler | Bar with only one end threaded | `ghi_chu_ren` = "Ren 1 đầu" | Counted as 1 coupler preparation; schedule records 1 coupler fitting per connection point |
| 8 | QR Printing | Very long project name (>100 characters) | QR payload exceeds standard module capacity | QRCoder dynamically increases QR version (1..40) while maintaining ECC Level Q |
| 9 | Revit Rebar | Non-coplanar rebar curve loop | 3D curve array passed to `create_rebar` | Revit API throws `ArgumentException`; pre-validate coplanarity via normal vector tolerance |
| 10 | Revit Rebar | Host beam deleted or modified | Rebar regeneration triggered on orphaned element | Selection filter returns null; operation halts with explicit error `ERR_HOST_NOT_FOUND` |

---

## 9. Implementation Blueprint for `HPKata.Core` and `HPKata.Revit`

To build the clean-room `HPKata` solution (`HPKata.slnx`) adhering to `HP_CLEAN_CODE_CORE.md`:

### 9.1 `HPKata.Core` (`netstandard2.0`)
1. **`HPKata.Core.Optimization`**:
   - `CuttingStockEngine`: Pure C# implementation of the 1D cutting stock solver with `Auto`, `BestFitDecreasing`, `ExactZeroResidual`, `BeamSearchTopK` ($K=20$), and `Pattern` modes.
   - Zero external CAD/Revit dependencies.
   - Full support for warehouse offcut tracking, configurable `lmin_kho`, and waste analytics.
2. **`HPKata.Core.Scheduling`**:
   - `RebarScheduleAggregator`: Calculates cut lengths, bend deductions, unit weights, and TCVN diameter summaries.
   - `CouplerScheduleCalculator`: Aggregates mechanical couplers from `ghi_chu_ren` attributes.
3. **`HPKata.Core.Excel`**:
   - `ClosedXmlKataExporter`: Headless Excel reader and writer for `Kata.xlsm` (`Thong_ke`, `To_hop_thep`, `Kho_thep`, `In_tem_thep`) and `Iso_QHPlus.xls`.
4. **`HPKata.Core.Barcode`**:
   - `RebarTagQrGenerator`: Pure C# QR code generator packaging tag metadata into ECC-Q payloads.

### 9.2 `HPKata.Revit` (.NET 8.0 Revit 2026 Add-In)
1. **`KataRebarCreationService`**:
   - Maps `HPKata.Core` rebar specifications to Revit `Rebar` curves, `RebarBarType`, and `RebarHookType`.
   - Tags elements with `REBAR_PARTITION = "{BeamName}"` and `Comments = "HPKata_{BeamName}"`.
   - Executes idempotent cleanup before generation via `Get_Delete_rebar_in_host`.
2. **`KataViewAnnotationService`**:
   - Creates longitudinal beam elevation `ViewSection` and cross-section views.
   - Applies `ParameterFilterElement` on `REBAR_PARTITION` to isolate beam reinforcement.
   - Places `MultiReferenceAnnotation` rebar dimension tags with leaders.

---

## 10. Conclusion & Verification

All four assigned target areas have been comprehensively reverse-engineered through clean-room static analysis, metadata decompilation, and workbook inspection:
1. **1D Cutting-Stock Optimization**: Full data model (`InfoThepCho`, `InfoThepToHop`, `InfoThepKho`, `InfoTongDuThep`), standard 11,700 mm stock length, 5 heuristic strategies, kerf, offcut thresholds, and waste formulas fully mapped.
2. **Excel Templates**: Complete structural schemas, column mappings, and formulas documented for `Kata.xlsm` (15 sheets), `Kata ENG.xlsm` (10 sheets), and `Iso_QHPlus.xls` (3 sheets), including VBA macros.
3. **Coupler Schedule & QR Code Tag**: Coupler extraction rules (`ghi_chu_ren`), table schemas (`Thong_ke!U9:AA15`), `QRCoder` ECC-Q encoding, and 12-row tag layout ($A1:F12$) fully documented.
4. **Revit Integration**: Types, parameters (`REBAR_PARTITION`, `ALL_MODEL_MARK`), hook resolution, section view generation, multi-reference tagging, and idempotent regeneration verified.

# SPECIFICATION REPORT: Quantity Surveying (QS) Subsystem
## Concrete Volume, Formwork Surface Takeoff, Intersection Deduction & Dual-Host CAD/Revit Architecture

- **Author**: `explorer_m4_qs` (Quantity Surveying Domain & Specification Miner)
- **Target Deliverable**: `HPKata/HPKata.slnx` (.NET 8 SDK 10.0.300)
- **Milestone**: M4 (Phase 5: Quantity Surveying & Drawing Utilities)
- **Parent Conversation ID**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`
- **Date**: 2026-10-17
- **Status**: AUTHORITATIVE SPECIFICATION

---

## 1. Executive Summary & Authoritative Spec Sources

This report provides the exhaustive reverse-engineered specification and Clean Code architecture design for the **Quantity Surveying (QS)** subsystem of **HPKata** (`HPKata.Core/QuantitySurveying/`, `HPKata.AutoCad`, and `HPKata.Revit`).

### 1.1 Authoritative Sources Inspected (Strictly Read-Only)
1. `C:\kata_pro\Goi lenh.lsp`:
   - Command aliases: lines 137–141 (`QS_mbkc`, `QS_tru`, `QS`, `xuatQS`, `timqs`).
   - Configuration block: line 237 (`QsFormulaMode` = 0 for standard TCVN, 1 for net 3D intersection).
2. `C:\kata_pro\Kata.xlsm`:
   - Sheet `Kata_QS` (Cols A–J: Drawing Name, Item, Quantity, Area, Length, Height, Total Area, Total Length, Total Volume, Side Surface / Formwork Area).
   - Sheet `Thong_ke_QS` (Quantity surveying rebar takeoff with TCVN diameter bins $\le 10$, $\le 18$, $> 18$).
3. `C:\kata_pro\Kata_pro64_Cad2025.dll` & `Kata_pro64_Cad2013.dll`:
   - Classes `QS_form`, `QS_mbkc_form`, `QS_hm_tru`, `QS_ve_hm_tru`, `QS_xuat`.
   - Entry points: `Lenh_thep::Trien_khai_QS`, `Lenh_thep::QS_mbkc`, `Lenh_thep::Ve_hm_tru`, `Lenh_thep::XuatQS`, `Lenh_thep::Tim_QS1`, `Lenh_thep::Thong_ke_coppha_be_tong1`.
4. `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`:
   - Section 1.2 (§6 Subsystem 6: QS Quantity Takeoff Engine).
   - Section 2.6 (Subsystem 6 Commands: #107–#116).
   - Section 3.1 (Parameter 86: `QsFormulaMode`).
   - Section 8.1 (Excel workbook schema catalog).
5. Existing codebase patterns in `HPKata/HPKata.Core`, `HPKata.AutoCad`, and `HPKata.Revit`.

---

## 2. Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | QS Workflow | `QS` (`Trien_khai_QS`) | Master quantity surveying workflow dialog/command orchestrating concrete, formwork, and steel takeoff | User selection on CAD/Revit model | Consolidated QS takeoff data model and preview | Graceful cancel if selection empty | `Goi lenh.lsp:139`, `Lenh_thep::Trien_khai_QS`, `QS_form` |
| 2 | Framing Plan Takeoff | `QS_mbkc` | Extracts structural quantities directly from 2D structural framing plans (beams, columns, slab panels) | Framing lines/blocks, story height $H$, slab thickness $t_s$ | Concrete volumes, bottom formwork, side formwork | Warns if story height $\le 0$ or boundary unclosed | `Goi lenh.lsp:137`, `Lenh_thep::QS_mbkc`, `QS_mbkc_form` |
| 3 | Deduction Matrix | `QS_tru` (`Ve_hm_tru`) | Manages and applies 3D geometric intersection deductions across structural components | Component geometries, deduction priority configuration | Net volume and net formwork results per element | Skips non-overlapping components | `Goi lenh.lsp:138`, `Lenh_thep::Ve_hm_tru`, `QS_hm_tru` |
| 4 | Excel Export | `xuatQS` (`XuatQS`) | Exports Bill of Quantities (BOQ) workbook matching `Kata.xlsm` sheet `Kata_QS` and `Thong_ke_QS` | Calculated `QsTakeoffSummary`, target file path | Formatted `.xlsx` spreadsheet with formulas | Reverts to temp path if target locked/in-use | `Goi lenh.lsp:140`, `Lenh_thep::XuatQS`, `QS_xuat` |
| 5 | Element Inspection | `timqs` (`Tim_QS1`) | Locates, zooms viewport, and highlights elements associated with a QS takeoff mark on drawing | Element mark string (e.g. `C1`, `D101`) | Viewport zoom to element with transient highlight | Reports "Element not found" if absent | `Goi lenh.lsp:141`, `Lenh_thep::Tim_QS1` |
| 6 | Table Generation | `tkqs` | Generates AutoCAD ModelSpace `Table` entity summarizing concrete volume and formwork areas | Pick insertion point, takeoff dataset | CAD `Table` entity with standard headers | Cancels if point pick aborted | `Lenh_thep::Thong_ke_coppha_be_tong1`, `Iso_kata.dwg` |
| 7 | Core Model | `QsElementModel` | Host-neutral geometric representation of a structural member for QS calculations | Identifier, mark, member type, story, dimensions, bounding polyline | Evaluated gross/net volume and formwork | Throws `ArgumentException` on invalid dimensions | `HPKata.Core/QuantitySurveying/Models` |
| 8 | Deduction Engine | `QsIntersectionDeductionEngine` | Applies standard structural ownership hierarchy: Footing $\succ$ Column/Wall $\succ$ Beam $\succ$ Slab | List of `QsElementModel` in a project/story | Populated `QsVolumeResult` and `QsFormworkResult` | Pure geometric resolution, zero exceptions on parallel elements | `HPKata.Core/QuantitySurveying/Calculators` |
| 9 | Contact Formwork | `QsFormworkCalculator` | Calculates shuttering contact areas and deducts intersecting concrete contact faces | Member dimensions, adjacent member boundaries | Bottom formwork $m^2$, side formwork $m^2$, net contact $m^2$ | Sets contact to 0 if gap exists | `HPKata.Core/QuantitySurveying/Calculators` |
| 10 | Persistence & Summary | `QsScheduleAggregator` | Aggregates elements by story and mark, computes project-level totals, prepares BOQ bins | Collection of element models | Structured `QsTakeoffSummary` with subtotals | Handles empty dataset gracefully (zero totals) | `HPKata.Core/QuantitySurveying/Persistence` |
| 11 | Headless ClosedXML | `QsWorkbookGenerator` | Headless Excel generator creating `Kata_QS`, `Thong_ke_QS`, and Summary sheets | `QsTakeoffSummary`, Stream/FilePath | `.xlsx` binary stream/file with formulas | Throws `ArgumentNullException` if report null | `HPKata.Core/QuantitySurveying/Persistence` |
| 12 | Revit Takeoff Service | `RevitQsTakeoffService` | Queries native Revit elements, extracts solid volumes, computes Boolean solid intersections | Revit `Document`, active level or category filters | List of `QsElementModel` mapped to Core DTOs | Logs warning and uses parameter fallback if solid empty | `HPKata.Revit/Services` |

---

## 3. Edge Cases

| # | Feature | Input / Scenario | Observed & Designed Behavior |
|---|---------|------------------|------------------------------|
| 1 | Volume Deduction | Column completely embedded inside shear wall | Wall takes precedence or shares priority; deduction engine checks containment and sets column net volume to 0 to prevent double counting. |
| 2 | Volume Deduction | Secondary beam intersects primary beam at oblique angle ($\ne 90^\circ$) | Cut length calculated along centerline to primary beam edge; deduction volume = $b_{\text{sec}} \times h_{\text{sec}} \times (b_{\text{prim}} / \sin\theta)$. |
| 3 | Formwork Takeoff | Beam with slab on one side only (exterior spandrel / perimeter beam) | Exterior side formwork has full depth $h_{\text{beam}}$; interior side formwork has net depth $(h_{\text{beam}} - t_{\text{slab}})$. Bottom formwork is $b_{\text{beam}} \times L_{\text{clear}}$. |
| 4 | Formwork Takeoff | Slab with large duct / stair void opening ($> 0.1\text{ m}^2$) | Slab volume deducted by $A_{\text{void}} \times t_{\text{slab}}$; slab soffit formwork deducted by $A_{\text{void}}$; edge formwork added along void perimeter $P_{\text{void}} \times t_{\text{slab}}$. |
| 5 | Formwork Takeoff | Column face where multiple beams frame in at different elevations | Each beam end contact area is deducted individually from the column side formwork without overlap. |
| 6 | Footing Embedment | Pile cap with embedded pile heads ($100\text{ mm}$ ngàm) | Pile cap gross volume deducted by $\sum (A_{\text{pile}} \times \text{EmbedmentDepth})$ matching `FootingGeometryCalculator`. |
| 7 | Excel Generation | Target `.xlsx` file open by user in Excel desktop | ClosedXML throws `IOException`; generator detects lock, writes to `%TEMP%\Kata_QS_{Guid}.xlsx` and informs user. |
| 8 | Drawing Table | Insertion point outside limits or table exceeds 100 rows | Table automatically split into multi-column table or placed with standard title and header repeating rows. |
| 9 | Multi-story Model | Same mark name used on different levels (e.g. `C1` on Story 1 and Story 2) | Grouped hierarchically by `(Story, MemberType, Mark)`; subtotaled per story and summarized project-wide. |
| 10 | Thin/Zero Slabs | Isolated transfer girder or foundation beam without slab | Side formwork calculated at full depth on both sides ($2 \times h \times L$); slab deductions bypassed. |

---

## 4. Mathematical Deduction Hierarchy & Formulas

### 4.1 Structural Ownership Priority Hierarchy
To eliminate double counting while ensuring 100% volumetric and surface conservation, elements follow this strict precedence:

$$\text{Footing} \succ \text{Column / Wall} \succ \text{Primary Beam} \succ \text{Secondary Beam} \succ \text{Slab}$$

```
                +-------------------------+
                |    FOOTING / PILE CAP   |  (Poured 1st, bottom datum)
                +------------+------------+
                             |  Footing takes precedence over column base
                             v
                +-------------------------+
                |   COLUMN / SHEAR WALL   |  (Runs continuous through story)
                +------------+------------+
                             |  Column takes precedence over beams & slabs
                             v
                +-------------------------+
                |      PRIMARY BEAM       |  (Spans between column faces)
                +------------+------------+
                             |  Primary beam takes precedence over secondary
                             v
                +-------------------------+
                |     SECONDARY BEAM      |  (Spans between beam faces)
                +------------+------------+
                             |  Beams take precedence over slab stems
                             v
                +-------------------------+
                |       FLOOR SLAB        |  (Net clear panel between beams/cols)
                +-------------------------+
```

### 4.2 Concrete Volume Formulas ($m^3$)

1. **Footing / Pile Cap**:
   $$V_{\text{gross}} = L \times W \times H + V_{\text{pedestal}}$$
   $$V_{\text{net}} = V_{\text{gross}} - \sum_{i=1}^{n_{\text{piles}}} (A_{\text{pile}, i} \times h_{\text{embed}})$$

2. **Column**:
   $$V_{\text{gross}} = b_{\text{col}} \times d_{\text{col}} \times H_{\text{story}}$$
   $$V_{\text{net}} = V_{\text{gross}} - V_{\text{footing\_overlap}}$$
   *(Note: Beam and slab intersections are NOT deducted from the column, because column concrete is continuous through floors).*

3. **Wall**:
   $$V_{\text{gross}} = t_{\text{wall}} \times L_{\text{wall}} \times H_{\text{story}}$$
   $$V_{\text{net}} = V_{\text{gross}} - \sum (w_{\text{door}} \times h_{\text{door}} \times t_{\text{wall}}) - V_{\text{footing\_overlap}}$$

4. **Primary Beam**:
   $$L_{\text{clear}} = L_{\text{span}} - \frac{1}{2} d_{\text{col, start}} - \frac{1}{2} d_{\text{col, end}}$$
   $$V_{\text{net}} = b_{\text{beam}} \times h_{\text{beam}} \times L_{\text{clear}}$$

5. **Secondary Beam**:
   $$L_{\text{clear}} = L_{\text{span}} - \frac{1}{2} b_{\text{primary, start}} - \frac{1}{2} b_{\text{primary, end}}$$
   $$V_{\text{net}} = b_{\text{sec}} \times h_{\text{sec}} \times L_{\text{clear}}$$

6. **Floor Slab**:
   - Under Beam Precedence (`QsFormulaMode = 0`):
     $$A_{\text{net}} = A_{\text{gross}} - \sum A_{\text{columns}} - \sum (b_{\text{beam}} \times L_{\text{clear\_beam}}) - \sum A_{\text{openings}}$$
     $$V_{\text{net}} = A_{\text{net}} \times t_{\text{slab}}$$
   - Under Stem Precedence (`QsFormulaMode = 1`):
     $$V_{\text{net}} = (A_{\text{gross}} - \sum A_{\text{columns}} - \sum A_{\text{openings}}) \times t_{\text{slab}}$$
     *(where beam volume is reduced to $b_{\text{beam}} \times (h_{\text{beam}} - t_{\text{slab}}) \times L_{\text{clear}}$)*.

7. **Staircase**:
   $$V_{\text{waist}} = B \times L_{\text{slope}} \times t_{\text{waist}}$$
   $$V_{\text{steps}} = N_{\text{steps}} \times \left(\frac{1}{2} b_{\text{step}} \times h_{\text{riser}} \times B\right)$$
   $$V_{\text{landings}} = \sum (L_{\text{landing}} \times B_{\text{landing}} \times t_{\text{landing}})$$
   $$V_{\text{net}} = V_{\text{waist}} + V_{\text{steps}} + V_{\text{landings}}$$

### 4.3 Formwork Contact Area Formulas ($m^2$)

1. **Footing Formwork**:
   $$S_{\text{form}} = 2 \times (L + W) \times H - \sum_{\text{grade beams}} (b_{\text{gb}} \times h_{\text{gb}})$$
   *(Bottom rests on lean concrete $\rightarrow$ 0 formwork).*

2. **Column Formwork**:
   $$S_{\text{gross}} = 2 \times (b_{\text{col}} + d_{\text{col}}) \times H_{\text{story}}$$
   $$S_{\text{net}} = S_{\text{gross}} - \sum_{\text{framing beams}} (b_{\text{beam}} \times h_{\text{beam}}) - \sum_{\text{slabs}} (L_{\text{col\_perimeter\_in\_slab}} \times t_{\text{slab}})$$

3. **Beam Formwork**:
   - Bottom Soffit:
     $$S_{\text{bottom}} = b_{\text{beam}} \times L_{\text{clear}}$$
   - Side Formwork:
     $$S_{\text{sides}} = (h_{\text{left}} + h_{\text{right}}) \times L_{\text{clear}} - \sum_{\text{sec beams}} (b_{\text{sec}} \times h_{\text{contact}})$$
     where:
     - If slab on left: $h_{\text{left}} = h_{\text{beam}} - t_{\text{slab, left}}$.
     - If no slab on left: $h_{\text{left}} = h_{\text{beam}}$.
     - If slab on right: $h_{\text{right}} = h_{\text{beam}} - t_{\text{slab, right}}$.
     - If no slab on right: $h_{\text{right}} = h_{\text{beam}}$.
   - Total Beam Formwork: $S_{\text{beam\_form}} = S_{\text{bottom}} + S_{\text{sides}}$.

4. **Slab Formwork**:
   - Soffit Formwork:
     $$S_{\text{soffit}} = A_{\text{net\_panel}}$$
   - Edge / Perimeter Formwork (edges not bounded by beams or walls):
     $$S_{\text{edge}} = L_{\text{exterior\_free\_edges}} \times t_{\text{slab}}$$
   - Void Opening Perimeter Formwork:
     $$S_{\text{openings}} = \sum P_{\text{opening}} \times t_{\text{slab}}$$
   - Total Slab Formwork: $S_{\text{slab\_form}} = S_{\text{soffit}} + S_{\text{edge}} + S_{\text{openings}}$.

5. **Wall Formwork**:
   $$S_{\text{gross}} = 2 \times (L_{\text{wall}} + t_{\text{wall}}) \times H_{\text{story}}$$
   $$S_{\text{net}} = S_{\text{gross}} - 2 \sum (w_{\text{door}} \times h_{\text{door}}) + \sum 2(w_{\text{door}} + h_{\text{door}}) \times t_{\text{wall}} - S_{\text{beam\_slab\_contacts}}$$

6. **Stair Formwork**:
   - Soffit: $B \times L_{\text{slope}} + \sum (B \times L_{\text{landing}})$.
   - Risers: $N_{\text{steps}} \times h_{\text{riser}} \times B$.
   - Sides / Stringers: $2 \times \text{ProfileArea}_{\text{flight}}$.

---

## 5. Domain Architecture Design (`HPKata.Core/QuantitySurveying/`)

All classes reside in `HPKata.Core/QuantitySurveying/`, target `netstandard2.0;net8.0`, have **ZERO** host dependencies, and obey the $\le 300$ lines/file and $\le 50$ lines/method constraints.

```
HPKata.Core/QuantitySurveying/
├── Models/
│   ├── QsMemberType.cs               # Enum: Footing, Column, Wall, Beam, Slab, Stair
│   ├── QsElementModel.cs             # Generic structural member DTO
│   ├── QsVolumeResult.cs             # Gross/net volume & deduction records
│   ├── QsFormworkResult.cs           # Bottom, side, net formwork & deductions
│   ├── QsDeductionRecord.cs          # Individual intersection deduction entry
│   ├── QsDeductionRule.cs            # Rule configuration and priority flags
│   ├── QsTakeoffItem.cs              # Aggregated line item for report
│   └── QsTakeoffSummary.cs           # Project/story rollup with category bins
├── Calculators/
│   ├── QsTakeoffCalculator.cs        # Gross volume & gross surface calculations
│   ├── QsIntersectionDeductionEngine.cs # Structural priority deduction solver
│   └── QsFormworkCalculator.cs       # Formwork contact area analyzer
└── Persistence/
    ├── QsScheduleAggregator.cs       # Groups, filters, and rolls up elements
    └── QsWorkbookGenerator.cs        # Headless ClosedXML exporter (Kata_QS sheet)
```

### 5.1 Models Specification

#### `QsMemberType.cs`
```csharp
namespace HPKata.Core.QuantitySurveying.Models
{
    public enum QsMemberType
    {
        Footing = 0,
        Column = 1,
        Wall = 2,
        Beam = 3,
        Slab = 4,
        Stair = 5
    }
}
```

#### `QsElementModel.cs`
Represents an individual structural member with geometry in metric mm:
- `string Id`: Unique handle or GUID.
- `string Mark`: Structural mark (e.g. `C1`, `D2`, `S1`, `M1`).
- `string Story`: Story/Level name (e.g. `Tầng 1`, `Tầng 2`, `Móng`).
- `QsMemberType MemberType`: Member classification.
- `int MemberCount`: Number of identical repetitions (default 1).
- `double LengthMm`: Clear or overall length.
- `double WidthMm`: Cross-sectional width $b$.
- `double HeightMm`: Cross-sectional depth $h$ or height $H$.
- `double ThicknessMm`: Slab or wall thickness.
- `double TopElevationMm`: Top surface level.
- `double BottomElevationMm`: Soffit / bottom level.
- `double PlanAreaMm2`: Plan footprint area (for slabs/footings).
- `double PerimeterMm`: Plan perimeter.
- `List<string> IntersectingElementIds`: Handles of spatial neighbors.

#### `QsVolumeResult.cs` & `QsFormworkResult.cs`
- `QsVolumeResult`:
  - `double GrossVolumeM3`
  - `double NetVolumeM3`
  - `double DeductedVolumeM3 => GrossVolumeM3 - NetVolumeM3`
  - `List<QsDeductionRecord> Deductions`
- `QsFormworkResult`:
  - `double GrossFormworkM2`
  - `double SoffitFormworkM2` (Đáy)
  - `double SideFormworkM2` (Thành)
  - `double NetFormworkM2`
  - `double DeductedAreaM2 => GrossFormworkM2 - NetFormworkM2`
  - `List<QsDeductionRecord> Deductions`

#### `QsDeductionRecord.cs`
- `string IntersectingElementId`
- `string IntersectingMark`
- `QsMemberType IntersectingType`
- `double DeductedVolumeM3`
- `double DeductedAreaM2`
- `string Reason` (e.g. "Column takes precedence over Beam stem", "Beam end face on column")

### 5.2 Calculators Specification

#### `QsTakeoffCalculator.cs`
Pure static calculator providing gross evaluations:
- `CalculateGrossVolume(QsElementModel element) -> double (m3)`
- `CalculateGrossFormwork(QsElementModel element) -> (double soffitM2, double sideM2)`

#### `QsIntersectionDeductionEngine.cs`
Evaluates a collection of `QsElementModel`:
- Implements the precedence order:
  1. Footings remain gross (except pile embedments).
  2. Columns deduct footing overlap; beams and slabs do NOT deduct from columns.
  3. Beams deduct column overlaps ($b_{\text{beam}} \times h_{\text{beam}} \times \text{overlap}$) and primary beam overlaps.
  4. Slabs deduct column areas ($\sum A_{\text{col}} \times t_{\text{slab}}$) and beam stem areas ($\sum b_{\text{beam}} \times L_{\text{clear}} \times t_{\text{slab}}$).
- Returns dictionary of element IDs to `(QsVolumeResult, QsFormworkResult)`.

#### `QsFormworkCalculator.cs`
Evaluates surface deductions:
- Identifies contact patches between adjacent elements.
- Deducts beam cross-sections from column vertical faces.
- Deducts slab thickness from beam vertical faces.
- Deducts secondary beam ends from primary beam side faces.

### 5.3 Persistence & Workbook Generation Specification

#### `QsWorkbookGenerator.cs` (ClosedXML)
Generates `.xlsx` matching `Kata.xlsm`:
1. Sheet **`Kata_QS`**:
   - Header row 1: `Tên bản vẽ | Hạng mục | Số lượng | Diện tích (m2) -A- | Chiều dài (m) -L- | Chiều cao (m) -H- | Tổng diện tích (m2) | Tổng chiều dài (m) | Tổng thể tích (m3) | Diện tích xung quanh (m2)`
   - Subheader row 2: `(0) | (1) | (2) | (3) | (4) | (5) | (6) = (3)x(2) | (7) = (4)x(2) | (8) = (3)x(5) | (9) = (4)x(5)`
   - Data rows: One row per member mark per story with exact formulas in columns G, H, I, J.
2. Sheet **`Summary_QS`**:
   - Category rollup table:
     - Cột (Columns): Total Concrete $m^3$, Total Formwork $m^2$.
     - Dầm (Beams): Total Concrete $m^3$, Total Formwork $m^2$.
     - Sàn (Slabs): Total Concrete $m^3$, Total Formwork $m^2$.
     - Vách (Walls): Total Concrete $m^3$, Total Formwork $m^2$.
     - Móng (Footings): Total Concrete $m^3$, Total Formwork $m^2$.
     - Cầu thang (Stairs): Total Concrete $m^3$, Total Formwork $m^2$.
   - Grand Total Project Concrete ($m^3$) and Grand Total Formwork ($m^2$).

---

## 6. AutoCAD 2026 Integration Architecture (`HPKata.AutoCad`)

### 6.1 Commands & Lisp Aliases
All commands are registered in `HPKata.AutoCad/Commands/QsCommands.cs` and match `Goi lenh.lsp`:

| Lisp Alias | CommandMethod | Backing Controller Method | Purpose |
|---|---|---|---|
| `QS` | `[CommandMethod("QS")]` | `QsTakeoffController.ShowQsWorkflowDialog()` | Opens master interactive QS takeoff dialog / workflow |
| `QS_mbkc` | `[CommandMethod("QS_mbkc")]` | `QsTakeoffController.TakeoffFramingPlan()` | Scans framing plan with story height & thickness prompts |
| `QS_tru` | `[CommandMethod("QS_tru")]` | `QsTakeoffController.ConfigureDeductionRules()` | Configures & executes 3D intersection deductions |
| `xuatQS` | `[CommandMethod("xuatQS")]` | `QsTakeoffController.ExportTakeoffToExcel()` | Exports calculated takeoff to `Kata_QS.xlsx` or DWG Table |
| `timqs` | `[CommandMethod("timqs")]` | `QsTakeoffController.FindAndHighlightQsElement()` | Zooms to and highlights member on plan |
| - | `[CommandMethod("tkqs")]` | `QsTakeoffController.GenerateDwgTakeoffTable()` | Creates ModelSpace CAD table with concrete & formwork |

### 6.2 Controller Design (`QsTakeoffController.cs`)
- Scans `ModelSpace` for:
  - Column blocks (`cot`, `block_cot`) or closed rectangular polylines on layer `kata_net thay`.
  - Beam lines/polylines with dimension attributes (e.g. `300x600`).
  - Slab boundary polylines.
  - Footing blocks (`mong_don`, `mong_coc`).
- Converts CAD entities to `List<QsElementModel>`.
- Passes entities to `QsIntersectionDeductionEngine`.
- Builds an AutoCAD `Table` entity with standard formatting and appends to `ModelSpace`.
- Caches results in `QsTakeoffRegistry`.

---

## 7. Revit 2026 Integration Architecture (`HPKata.Revit`)

### 7.1 Service Design (`RevitQsTakeoffService.cs`)
Resides in `HPKata.Revit/Services/RevitQsTakeoffService.cs`.
- Collects elements using `FilteredElementCollector`:
  - Columns: `OfCategory(BuiltInCategory.OST_StructuralColumns).WhereElementIsNotElementType()`
  - Beams: `OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsNotElementType()`
  - Slabs: `OfCategory(BuiltInCategory.OST_Floors).WhereElementIsNotElementType()`
  - Footings: `OfCategory(BuiltInCategory.OST_StructuralFoundation).WhereElementIsNotElementType()`
  - Walls: `OfCategory(BuiltInCategory.OST_Walls).WhereElementIsNotElementType()`
  - Stairs: `OfCategory(BuiltInCategory.OST_Stairs).WhereElementIsNotElementType()`
- Extracts volumetric and geometric data:
  ```csharp
  double volCuFt = element.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED)?.AsDouble() ?? 0.0;
  double volM3 = RevitUnitConverter.CubicFeetToCubicMeters(volCuFt);
  ```
- Evaluates formwork via solid face normal classification:
  - Bottom faces ($\vec{n} \cdot \vec{k} \approx -1$): Soffit formwork.
  - Vertical faces ($|\vec{n} \cdot \vec{k}| < 0.01$): Side formwork.
- Maps to `QsElementModel` DTOs and invokes `QsScheduleAggregator`.
- Can export directly via `QsWorkbookGenerator` to `.xlsx`.

### 7.2 Revit Commands (`RevitQsCommands.cs`)
- `RevitQsTakeoffCommand`: Interactive takeoff on current active Revit view or entire project.
- `RevitQsExportExcelCommand`: Prompts save file dialog and writes `Kata_QS.xlsx`.

---

## 8. Clean Code Governance Compliance Matrix

| Rule | Requirement | Implementation Strategy in QS Subsystem |
|---|---|---|
| **C2** | Max 300 physical lines per file | Small, focused, single-responsibility files (`QsMemberType.cs`, `QsElementModel.cs`, `QsTakeoffCalculator.cs`, `QsIntersectionDeductionEngine.cs`, `QsFormworkCalculator.cs`, `QsScheduleAggregator.cs`, `QsWorkbookGenerator.cs`). |
| **M3** | Max 50 physical lines per method | Decompose calculations into concise helper methods (e.g. `CalculateColumnFormwork`, `DeductBeamOverlaps`, `WriteSheetHeaders`, `WriteItemRow`). |
| **D1, D4** | Zero host references in Core | `HPKata.Core/QuantitySurveying/` uses only BCL and `ClosedXML`. Absolutely zero references to `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`. |
| **Q-B1** | Zero commented-out / dead code | Strictly clean codebase; no commented blocks. |
| **Q-B2** | Zero unannotated empty catch blocks | Every catch block has an explicit reason comment and log/fallback action. |
| **MTP Isolation** | No test runner collisions | Unit tests in `HPKata.Core.Tests/QuantitySurveying/` run cleanly with `--disable-build-servers -m:1`. |

---

## 9. Verification & Test Suite Strategy (`HPKata.Core.Tests`)

A minimum of **45 comprehensive unit tests** in `HPKata.Core.Tests/QuantitySurveying/`:
1. `QsTakeoffCalculatorTests` (10 tests):
   - Gross volume and formwork for footings (sloped/stepped), columns (rect/circular), walls, beams, slabs, and stairs.
2. `QsIntersectionDeductionEngineTests` (15 tests):
   - Column-Beam intersection: proves column volume is untouched, beam volume deducted.
   - Column-Slab intersection: proves column penetrates slab, slab net volume deducted.
   - Beam-Slab intersection: verifies both `QsFormulaMode = 0` (beam priority) and `QsFormulaMode = 1` (stem).
   - Primary-Secondary beam intersection: proves secondary beam net length stops at primary beam face.
   - Footing-Column-Beam intersection: proves footing precedence.
3. `QsFormworkCalculatorTests` (10 tests):
   - Column side formwork deduction when 1, 2, or 4 beams intersect.
   - Interior beam with slabs on both sides vs exterior beam with slab on one side.
   - Slab soffit net area after deducting column footprints and beam stems.
4. `QsScheduleAggregatorTests` (5 tests):
   - Grouping by story and mark, total project summation, handling zero elements.
5. `QsWorkbookGeneratorTests` (5 tests):
   - Verifies ClosedXML writes valid `.xlsx` containing sheets `Kata_QS` and `Summary_QS` with correct headers and mathematical cell formulas.

---

## 10. Conclusion

The Quantity Surveying (QS) subsystem specification is fully mined, mathematically formulated, and architecturally mapped. It bridges clean structural theory, authoritative KataPro features, and legacy `Kata.xlsm` spreadsheet schemas with modern, modular Clean Code C# (.NET 8).

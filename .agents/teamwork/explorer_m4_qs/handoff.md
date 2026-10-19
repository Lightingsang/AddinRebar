# Handoff Report: Quantity Surveying (QS) Subsystem Specification Mining

- **Agent**: `explorer_m4_qs`
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_qs\`
- **Recipient**: `orchestrator_17` (Conversation ID: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`)
- **Milestone**: M4 (Phase 5: Quantity Surveying & Drawing Utilities)
- **Handoff Type**: Hard (Task Complete)
- **Report Path**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_qs\qs_spec_report.md`

---

## 1. Observation
1. **Commands & Lisp Configuration**:
   - `C:\kata_pro\Goi lenh.lsp`:
     - Lines 137–141 define the QS command set:
       ```lisp
       (defun c:QS_mbkc()(command "QS_mbkc"))
       (defun c:QS_tru()(command "QS_tru"))
       (defun c:QS()(command "QS"))
       (defun c:xuatQS()(command "xuatQS"))
       (defun c:timqs()(command "timqs"))
       ```
     - Line 237 defines parameter 86:
       `;0                 <- QsFormulaMode: 0 = TCVN beam priority, 1 = net 3D intersection`
2. **Excel Schema & Sheets in `Kata.xlsm`**:
   - Inspected `C:\kata_pro\Kata.xlsm` via python `openpyxl`:
     - Sheet `Kata_QS` (Rows 1–2):
       `R01: Tên bản vẽ | Hạng mục | Số lượng | Diện tích (m2) -A- | Chiều dài (m) -L- | Chiều cao (m) -H- | Tổng diện tích (m2) | Tổng chiều dài (m) | Tổng thể tích (m3) | Diện tích xung quanh (m2)`
       `R02: (0) | (1) | (2) | (3) | (4) | (5) | (6) = (3)x(2) | (7)=(4)x(2) | (8)=(3)x(5) | (9)=(4)x(5)`
     - Sheet `Thong_ke_QS`: Rebar takeoff sheet with TCVN grouping columns (`D<=10mm`, `D<=18mm`, `D>18mm`).
3. **Legacy Decompiled DLLs**:
   - `C:\kata_pro\Kata_pro64_Cad2025.dll`: Contains classes `QS_form`, `QS_mbkc_form`, `QS_hm_tru`, `QS_ve_hm_tru`, `QS_xuat`.
   - `Lenh_thep` contains command methods `Trien_khai_QS` (`QS`), `QS_mbkc`, `Ve_hm_tru` (`QS_tru`), `XuatQS`, `Tim_QS1`, `Thong_ke_coppha_be_tong1` (`tkqs`).
   - `QS_mbkc_form` controls: `bt_cot` (Column concrete), `H_tang` (Story height), `hm_bt_mbkc` / `bt_mbkc` (Framing concrete), `hm_coppha_day` / `coppha_day` (Soffit formwork), `hm_coppha_thanh` / `coppha_thanh` (Side formwork).
4. **Existing Codebase Patterns**:
   - `HPKata/HPKata.Core`: Clean `Calculators/`, `Models/`, `Persistence/` structure across Beams, Columns, Walls, Foundations, Slabs, Stairs.
   - `HPKata/HPKata.AutoCad/Controllers`: `BeamTakeoffController.cs`, `ColumnTakeoffController.cs`, `FoundationTakeoffController.cs` manage table building and ModelSpace entities.
   - `HPKata/HPKata.Revit/Services`: `FramingScheduleService.cs`, `ColumnWallScheduleService.cs`, `FoundationSlabStairScheduleService.cs` query Revit elements and map to Core DTOs.
   - `ClosedXmlExporter.cs`: Headless ClosedXML generator writing structured `.xlsx` workbooks.

---

## 2. Logic Chain
1. *From Observation 1 and 3*: KataPro implements structural quantity surveying via a dedicated 6-command suite (`QS`, `QS_mbkc`, `QS_tru`, `xuatQS`, `timqs`, `tkqs`). `QS_mbkc` prompts the user for story height and member concrete/formwork parameters, while `QS_tru` deducts 3D component intersections.
2. *From Observation 2*: The authoritative output schema is `Kata.xlsm` sheet `Kata_QS`, which tracks Drawing Name, Member Mark, Quantity, Cross-sectional/Plan Area, Length, Height, and calculates Total Area, Total Length, Total Volume ($m^3$), and Side Formwork Area ($m^2$).
3. *From Structural TCVN Standards & Engineering Practice*: Concrete volumetric and formwork ownership must follow a strict non-conflicting priority hierarchy:
   $$\text{Footing} \succ \text{Column / Wall} \succ \text{Primary Beam} \succ \text{Secondary Beam} \succ \text{Slab}$$
   - Column concrete extends continuously through floor levels (zero beam/slab deductions from column).
   - Beams span between column faces (deducting column overlap from beam stem).
   - Slabs span between beam stems and column faces (deducting column and beam areas from slab volume).
   - Formwork contact areas are deducted where concrete faces touch (beam ends deducted from column sides; slab thickness deducted from beam sides; secondary beam ends deducted from primary beam sides).
4. *From Observation 4 and Clean Code Constraints*:
   - `HPKata.Core/QuantitySurveying/` must be pure C# (`netstandard2.0;net8.0`) with zero references to AutoCAD or Revit.
   - Files are partitioned into `Models/`, `Calculators/`, and `Persistence/` to keep every file $\le 300$ lines (Rule C2) and every method $\le 50$ lines (Rule M3).
   - `HPKata.AutoCad/Controllers/QsTakeoffController.cs` handles CAD entity scanning and DWG Table placement.
   - `HPKata.Revit/Services/RevitQsTakeoffService.cs` extracts Revit solids/volumes and maps them to `QsElementModel`.
   - `QsWorkbookGenerator.cs` handles headless ClosedXML generation matching `Kata.xlsm`.

---

## 3. Caveats
1. Obfuscation in `Kata_pro64_Cad2025.dll`: Certain internal helper methods in the legacy DLL are protected by control-flow flattening. However, all public types, controls, command methods, Lisp bindings, and Excel sheets are 100% visible and documented.
2. Formwork for complex curved geometries: Standard TCVN rules for rectangular columns, beams, slabs, walls, and footings are prioritized. Curved or non-standard profiles are evaluated using planar approximation segments.

---

## 4. Conclusion
The specification and architecture for the Quantity Surveying (QS) subsystem is fully defined and documented in:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_qs\qs_spec_report.md`

It is completely actionable and ready for implementation by Worker subagents in Milestone M4.

---

## 5. Verification Method
1. Inspect report:
   `view_file` on `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_qs\qs_spec_report.md`.
2. Verify reference sources:
   - Check `C:\kata_pro\Goi lenh.lsp` lines 137–141 and line 237.
   - Check `C:\kata_pro\Kata.xlsm` sheet `Kata_QS`.
3. Invalidation Conditions:
   - If any proposed `HPKata.Core` class references `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`.
   - If any proposed file exceeds 300 lines or method exceeds 50 lines.
   - If the volume deduction hierarchy violates $\text{Footing} \succ \text{Column} \succ \text{Beam} \succ \text{Slab}$.

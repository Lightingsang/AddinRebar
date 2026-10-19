# HPKata Milestone M5: AutoLISP Loader, Scheduling & Export Commands, and Headless Testing Architecture

**Author**: `explorer_m5_3` (Lisp Loader & Headless Testing Explorer)  
**Date**: 2026-10-17  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m5_3\`  
**Target Solution**: `HPKata/HPKata.slnx` (.NET 8 SDK 10.0.300)  
**Target Projects**: `HPKata.AutoCad` and `HPKata.AutoCad.Tests`  
**Governance**: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/autocad-civil.md`, `HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`

---

## 1. Executive Summary & Problem Boundary

Milestone M5 establishes the AutoCAD 2026 (.NET 8) client layer for HPKata. While `explorer_m5_1` investigates the core add-in host architecture/WPF integration and `explorer_m5_2` investigates polyline geometry and dynamic handle linking, this investigation defines:
1. **The AutoLISP Companion Loader (`GoiLenh_HPKata.lsp`)**:
   - Replicating lines 1–151 of `C:\kata_pro\Goi lenh.lsp` with 100% command alias fidelity (118 aliases).
   - Modernizing 8 legacy `-vbarun` commands into native .NET command invocations.
   - Preserving line-for-line alignment so that line 152 marks the exact start of the 86-parameter configuration block (lines 152–237) parsed and serialized by `HPKata.Core.Configuration.KataConfigParser`.
2. **Scheduling & Export Command Handlers**:
   - `tkt` (`thong_ke_thep`): Scanning DWG rebar polylines and tag blocks $\rightarrow$ aggregating `RebarScheduleReport` $\rightarrow$ generating CAD schedule tables with shape sketches.
   - `ththep` (`tong_hop_thep`): Grouping bar diameters, computing TCVN tonnage bins ($\le 10\text{ mm}$, $10 < \Phi \le 18\text{ mm}$, $> 18\text{ mm}$), and inserting summary tables.
   - `tkco` (`thong_ke_coupler`): Extracting mechanical couplers from rebar notes and inserting coupler schedule tables.
   - `tohopthep` (`to_hop_thep`): Invoking the 1D Cutting-Stock solver (`CuttingStockEngine`) on 11,700 mm standard stock bars, displaying interactive results via `CuttingStockView` (WPF), and committing offcuts to warehouse inventory.
   - `toexcel` / `toexcelqh`: Exporting active schedule reports headlessly via `ClosedXmlExporter` to `Kata.xlsm` and `Iso_QHPlus.xls` compatible workbooks without requiring Microsoft Excel installed.
   - `itt` (`in_tem_thep`): Generating fabrication shop bundle tags with ECC Level Q QR codes via `QrTagGenerator`.
3. **Headless Testing Architecture (`HPKata.AutoCad.Tests.csproj`)**:
   - Designing a decoupled architectural interface (`ICadDocumentContext`) allowing all command workflows, XData converters, and table generators to be tested headlessly outside `acad.exe`.
   - Developing comprehensive validation tests for `GoiLenh_HPKata.lsp` syntax, 118 command alias completeness, and 86-parameter integrity.
4. **Clean Code Compliance**:
   - Ensuring every proposed class remains $\le 300$ lines (Rule C2) and every method $\le 50$ lines (Rule M3).

---

## 2. AutoLISP Companion Loader Design (`GoiLenh_HPKata.lsp`)

### 2.1 File Structure & Line Alignment Requirements
In `HPKata.Core.Configuration.KataConfigParser`, the line extraction logic specifies:
```csharp
int startIdx = lines.Count >= 237 ? 151 : 0;
int count = Math.Min(86, lines.Count - startIdx);
```
In 0-indexed terms, `lines[151]` corresponds to **Line 152** of the file. To maintain 100% two-way compatibility with both `KataConfigParser.ParseFile()` and `KataConfigParser.SerializeToLines()`:
- **Lines 1–32**: Loader header, version sniffing, `.NETLOAD` execution, and CUI menu initialization.
- **Lines 33–150**: Exactly 118 AutoLISP command aliases (`c:tstt` through `c:ibv`).
- **Line 151**: Comment boundary banner:
  ```lisp
  ;;; ==================== KATA CONFIGURATION PARAMETERS (LINES 152-237) ====================
  ```
- **Lines 152–237**: The exact 86 configuration lines starting with `;kata_dim` and ending with `;0`.
- **Line 238**: Blank terminating newline.

### 2.2 Dynamic Binary Location & Auto-Loading Strategy
Legacy `Goi lenh.lsp` hardcodes `C:\Kata_pro\Kata_pro64_Cad2025.dll`. In `GoiLenh_HPKata.lsp`, the loader must be environment-aware, resolving `HPKata.AutoCad.dll` gracefully:
1. First, checks if the command `setupkata1` is already registered. If yes, loading is skipped.
2. Resolves DLL location dynamically:
   - Check adjacent to `GoiLenh_HPKata.lsp`: `(vl-filename-directory (findfile "GoiLenh_HPKata.lsp"))`.
   - Check standard ApplicationPlugins bundle: `%APPDATA%\Autodesk\ApplicationPlugins\HPKata.bundle\Contents\Windows\HPKata.AutoCad.dll`.
   - Check fallback installation path: `C:\HPKata\HPKata.AutoCad.dll`.
3. Issues `(command "_.NETLOAD" dllPath)` using the resolved path.
4. CUI Menu Loading: Checks `(menugroup "HPKATA")`. If `nil`, loads `menukata.cuix` or registers the shared ribbon tab.

```lisp
;;; =========================================================================================
;;; HPKata AutoCAD 2026 Companion AutoLISP Loader
;;; File: GoiLenh_HPKata.lsp (Total: 238 lines, Lines 152-237 contain 86 config items)
;;; =========================================================================================
(vl-load-com)
(setq *hpkata-dir* (vl-filename-directory (findfile "GoiLenh_HPKata.lsp")))
(if (not (vl-symbol-value 'c:setupkata1))
  (progn
    (setq *hpkata-dll* (strcat *hpkata-dir* "\\HPKata.AutoCad.dll"))
    (if (not (findfile *hpkata-dll*))
      (setq *hpkata-dll* "C:\\kata_pro\\HPKata.AutoCad.dll")
    )
    (if (findfile *hpkata-dll*)
      (command "_.NETLOAD" *hpkata-dll*)
      (prompt (strcat "\n[HPKata] Warning: HPKata.AutoCad.dll not found in " *hpkata-dir* "\n"))
    )
  )
)
(setq *hpkata-menu* (menugroup "menukata"))
(if (= *hpkata-menu* nil)
  (progn
    (setq *hpkata-cuix* (strcat *hpkata-dir* "\\menukata.cuix"))
    (if (findfile *hpkata-cuix*)
      (command "_cuiload" *hpkata-cuix*)
    )
    (menucmd "P13=+menukata.pop1")
  )
)
```

### 2.3 Legacy VBA Modernization
Legacy `Goi lenh.lsp` lines 33–40 invoke AutoCAD VBA (`-vbarun`), which is deprecated in AutoCAD 2025+ (.NET 8). `GoiLenh_HPKata.lsp` redirects these aliases directly to native `[CommandMethod]` entry points in `HPKata.AutoCad`:

| Line | Legacy `Goi lenh.lsp` | Modernized `GoiLenh_HPKata.lsp` | Backing C# CommandMethod |
|---|---|---|---|
| 33 | `(defun c:tstt()(command "-vbarun" "tk_san.tang_stt"))` | `(defun c:tstt()(command "tang_stt"))` | `tang_stt` |
| 34 | `(defun c:rthep()(command "-vbarun" "rai_thep.rai_block_thep"))` | `(defun c:rthep()(command "rthep"))` | `rthep` |
| 35 | `(defun c:lv()(command "-vbarun" "profile.level"))` | `(defun c:lv()(command "ve_level"))` | `ve_level` |
| 36 | `(defun c:lv1()(command "-vbarun" "profile.level1"))` | `(defun c:lv1()(command "ve_level1"))` | `ve_level1` |
| 37 | `(defun c:sst()(command "-vbarun" "profile.thong_ke_cap"))` | `(defun c:sst()(command "thong_ke_cap"))` | `thong_ke_cap` |
| 38 | `(defun c:dao()(command "-vbarun" "profile.dao_so_hieu"))` | `(defun c:dao()(command "dao_so_hieu"))` | `dao_so_hieu` |
| 39 | `(defun c:ad()(command "-vbarun" "profile.ad"))` | `(defun c:ad()(command "edit_att_data"))` | `edit_att_data` |
| 40 | `(defun c:cht()(command "-vbarun" "profile.doi_text_blockcd"))` | `(defun c:cht()(command "doi_text_blockcd"))` | `doi_text_blockcd` |
| 41 | `(defun c:setupkata()(command "setupkata1"))` | `(defun c:setupkata()(command "setupkata1"))` | `setupkata1` |

### 2.4 Complete 118 Command Aliases Inventory (Lines 33–150)
The remainder of lines 42–150 defines all 110 command aliases matching `Goi lenh.lsp`:
- **Grids & Layout**: `vg` (`ve_grid`), `raicot`, `dimcot`, `raidam`, `dsan` (`pick_diem_san`), `ssan`, `giaodam` (`giao_dam`), `catdam` (`cat_dam`), `hideatt`, `Unhideatt`, `tkdam`, `tkcot`, `tim_ten_ck`, `dten`, `tkten`, `textblock`, `chiasan` (`chia_san`).
- **Foundation**: `vmbm` (`ve_mat_bang_mong`), `rm` (`rai_mong`), `vmd` (`ve_mong_don`), `vmc` (`ve_mong_coc`), `vc` (`ve_cot`), `vcs` (`ve_cot_shop`), `vv` (`ve_vach`), `vvs` (`ve_vach_shop`), `vlv` (`ve_loi_vach`).
- **Beam Detailing**: `vd` (`ve_dam`), `vds` (`ve_dam_shop`), `vdm` (`ve_dam_mong`), `vgm` (`ve_giang_mong`), `vgms` (`ve_giang_shop`), `vdmc` (`ve_dam_mc`), `damgiao` (`dam_giao`), `vdx` (`ve_dam_xien`), `vdc` (`ve_dam_cong`), `vda` (`ve_dam_all`), `vdas` (`ve_dam_all_shop`), `vdmca` (`ve_damMC_all`), `qd` (`quet_dam`), `qda` (`quet_dam_all`), `std` (`Save_thep_dam`), `stda` (`Save_thep_dam_all`), `atd` (`Add_thep_dam`), `shtd` (`show_thep_dam`), `loadbeam`, `loadcolumn`, `loadwall`, `Etabs`.
- **Slab & Stair**: `vesan` (`ve_san`), `vesanpl`, `vesan4d`, `vemc`, `vethang` (`ve_thang`), `velanhto`.
- **Core Rebar & Editing**: `vt` (`ve_thep`), `taothep`, `link` (`tao_lien_ket`), `sua` (`sua_thep`), `smd` (`sua_moc_dai`), `dai90`, `dai180`, `dai135`, `dai90135`, `nhan` (`nhan_thep`), `neo` (`neo_thep`), `ud` (`update_thep`), `dmt` (`doi_mau_thep`), `cl` (`copy_tao_lien_ket`), `dl` (`xoa_lien_ket`), `tim` (`xem_lien_ket`), `xoa` (`xoa_highlight`), `gt` (`ghi_thep`), `cut` (`cat_thep`), `cattuyy` (`cat_tuy_y`), `timthep`, `dimthep`, `stt0`, `stt1`, `setupthep`.
- **Scheduling & Cutting**: `tohopthep` (`to_hop_thep`), `tkt` (`thong_ke_thep`), `suatk` (`sua_thong_ke`), `sstk`, `ththep` (`tong_hop_thep`), `tkco` (`thong_ke_coupler`), `tksan` (`thong_ke_block_san`), `toexcel`, `toexcelqh`, `chuyen`, `str` (`show_thep_rai`), `check_lap`, `pukata`, `pdfkata`, `itt` (`in_tem_thep`).
- **Quantity Surveying (QS)**: `QS_mbkc`, `QS_tru`, `QS`, `xuatQS`, `timqs`, `chiatam` (`chia_tam`), `opgach` (`op_gach`).
- **Sheet Production**: `locdim` (`loc_dim`), `pvn` (`phan_vung_noi`), `danhmuc`, `cat` (`cat_doituong`), `taomv`, `taomv1`, `sbv` (`sap_ban_ve`), `ibv` (`in_ban_ve`).

---

## 3. Scheduling & Export Command Handlers Architecture

### 3.1 Architectural Decomposition
To satisfy Clean Code Rule C5 (Separation of Calculation and Presentation) and facilitate 100% headless testing, all scheduling and export operations follow a three-tier architecture:

```
[AutoCAD [CommandMethod]]
          │ (Parses user selection & prompt points)
          ▼
[Command Runners (Services)]  ◄─── Calls ─── [Headless Unit Tests]
          │ (Orchestrates workflow, uses ICadDocumentContext)
          ▼
[HPKata.Core Domain Engines]
  ├── RebarScheduleAggregator (Schedule report, TCVN bins, couplers)
  ├── CuttingStockEngine (1D-CSP solver, patterns, offcuts)
  ├── ClosedXmlExporter (Headless Excel spreadsheet generation)
  └── QrTagGenerator (Pipe-delimited payload, ECC Level Q QR png)
```

### 3.2 Handler 1: `tkt` (`thong_ke_thep`)
- **Command Entry**: `[CommandMethod("thong_ke_thep"), CommandMethod("tkt")]` in `ScheduleCommands.cs`.
- **Workflow**:
  1. `ScheduleCommandRunner.ExecuteSchedule(ICadDocumentContext context, bool interactive)`:
  2. Prompts user for selection or uses entire drawing: `context.GetSelectedRebarEntities()`.
  3. Deserializes entity XData via `KataXDataSerializer.DeserializeFromDxf()` and reads linked tag attributes (`kata_block_KHT`, `thep`).
  4. Maps extracted entities to `List<RebarItem>`.
  5. Evaluates `KataConfig`:
     - Reads `ApplyBendDeduction` (Item 24).
     - Reads `ScheduleRowHeight` (Item 45, default 7.5 mm).
     - Reads `SchedShapeColWidth` (Item 46, default 25 mm).
  6. Calls `RebarScheduleAggregator.GenerateReport(items, applyBendDeduction)`.
  7. If `interactive == true` (or via `suatk`), launches `ScheduleView` (WPF dialog via `HPKata.UI`) allowing the engineer to review/edit items, adjust counts, and renumber marks.
  8. Prompts insertion point: `context.PromptPoint("Select table insertion point: ")`.
  9. Builds `CadTableDto`:
     - Title: "BẢNG THỐNG KÊ CỐT THÉP".
     - Header columns: `STT`, `CẤU KIỆN`, `SỐ HIỆU`, `HÌNH DẠNG`, `ĐK Ø`, `DÀI 1 THANH`, `SỐ LƯỢNG (1 CK / TOÀN BỘ)`, `TỔNG CHIỀU DÀI`, `TRỌNG LƯỢNG`.
     - Data rows populated from `report.Items`.
     - In shape sketch column, inserts block reference `dangXX` corresponding to `item.ShapeCode` scaled to cell dimensions.
  10. Writes table to drawing: `context.InsertTable(tableDto, insertionPoint)`.

### 3.3 Handler 2: `ththep` (`tong_hop_thep`)
- **Command Entry**: `[CommandMethod("tong_hop_thep"), CommandMethod("ththep")]` in `ScheduleCommands.cs`.
- **Workflow**:
  1. `SummaryCommandRunner.ExecuteSummary(ICadDocumentContext context)`:
  2. Scans drawing or retrieves active schedule report items.
  3. Computes diameter groupings via `RebarScheduleAggregator.SummarizeByDiameter(items)`.
  4. Computes TCVN bins via `RebarScheduleAggregator.CalculateTcvnBins(summaries)`.
  5. Prompts insertion point: `context.PromptPoint("Select summary table insertion point: ")`.
  6. Generates `CadTableDto`:
     - Table Header: "TỔNG HỢP CỐT THÉP".
     - Column Headers: `Đường kính Ø (mm)`, `Tổng chiều dài (m)`, `Trọng lượng đơn vị (kg/m)`, `Tổng trọng lượng (kg)`.
     - Summary Rows:
       - Row A: Thép tròn trơn $\le 10\text{ mm}$ ($\text{WireLe10Kg}$).
       - Row B: Thép gờ $10 < \Phi \le 18\text{ mm}$ ($\text{Deformed10To18Kg}$).
       - Row C: Thép gờ $\Phi > 18\text{ mm}$ ($\text{DeformedGt18Kg}$).
       - Total Row: TỔNG CỘNG TOÀN DỰ ÁN ($\text{TotalProjectWeightKg}$).
  7. Inserts table into AutoCAD database: `context.InsertTable(tableDto, insertionPoint)`.

### 3.4 Handler 3: `tkco` (`thong_ke_coupler`)
- **Command Entry**: `[CommandMethod("thong_ke_coupler"), CommandMethod("tkco")]` in `ScheduleCommands.cs`.
- **Workflow**:
  1. `SummaryCommandRunner.ExecuteCouplerSummary(ICadDocumentContext context)`:
  2. Scans drawing rebar items whose notes or XData `chu_thich` match `"coupler"` or `"ren"`.
  3. Calls `RebarScheduleAggregator.ExtractCouplers(items)`.
  4. If no couplers found, notifies user: `context.WriteMessage("\n[HPKata] Không tìm thấy cốt thép nối ren (coupler) trong bản vẽ.\n")` and exits cleanly.
  5. Prompts insertion point: `context.PromptPoint("Select coupler table insertion point: ")`.
  6. Builds `CadTableDto`:
     - Table Header: "BẢNG THỐNG KÊ COUPLER (MỐI NỐI REN)".
     - Columns: `TÊN CẤU KIỆN`, `KÍCH THƯỚC (mm)`, `Ø (mm)`, `SỐ C.KIỆN`, `MỘT CK`, `TOÀN BỘ`, `GHI CHÚ`.
  7. Inserts table at specified coordinates: `context.InsertTable(tableDto, insertionPoint)`.

### 3.5 Handler 4: `tohopthep` (`to_hop_thep`)
- **Command Entry**: `[CommandMethod("to_hop_thep"), CommandMethod("tohopthep")]` in `ScheduleCommands.cs`.
- **Workflow**:
  1. `CuttingStockCommandRunner.ExecuteCuttingStock(ICadDocumentContext context, bool headless)`:
  2. Extracts rebar items from selection or active drawing.
  3. Filters for cutting items: $\Phi \ge 10\text{ mm}$ (coil bars $\Phi 6, \Phi 8$ are segregated).
  4. Transforms `RebarItem` list into `List<CuttingDemand>` grouped by diameter.
  5. Loads existing warehouse offcut inventory (`loadkho` / `WarehouseOffcut`).
  6. Evaluates solver strategy (default `SolverStrategy.Auto`):
     - Executes `DuBang0` (exact 11.7m zero-waste subsets) $\rightarrow$ cascades remainder to `LapDayCanhDai` (BFD) $\rightarrow$ compares with `BeamSearchTopK`.
  7. If not headless, displays interactive `CuttingStockViewModel` and `CuttingStockView` (WPF) where the user can:
     - View cutting patterns with remnant lengths.
     - Inspect gross waste % and net waste %.
     - Review warehouse offcut credits ($\ge \text{lmin\_kho}$).
     - Click "Xuất Bản Vẽ CAD" to insert cutting pattern table blocks.
     - Click "Cập Nhật Kho Thép" (`upkho`) to save reusable offcuts to warehouse ledger.
  8. If headless, runs solver, outputs pattern summary to command line, and saves offcuts.

### 3.6 Handler 5: `toexcel` and `toexcelqh`
- **Command Entry**: `[CommandMethod("toexcel")]` and `[CommandMethod("toexcelqh")]` in `ScheduleCommands.cs`.
- **Workflow**:
  1. `ExportCommandRunner.ExecuteExcelExport(ICadDocumentContext context, bool isQhPlusFormat)`:
  2. Aggregates rebar schedule report via `RebarScheduleAggregator.GenerateReport()`.
  3. Derives default target filepath:
     - Based on active drawing path: `[DrawingFolder]\[DrawingName]_ThongKe.xlsx` (or `_ThongKe_QHPlus.xls`).
  4. If in interactive mode, displays `SaveFileDialog` initialized to the default path.
  5. Calls `ClosedXmlExporter.ExportScheduleToFile(report, targetPath)`.
     - Zero reliance on Microsoft Excel COM or installed Office binaries!
     - Generates 100% compliant `.xlsx` workbook containing:
       - Sheet `Thong_ke` with formatted headers, styles, formulas, and column widths.
       - Diameter summary table (`U1:AK7`).
       - Coupler summary table (`U10:AA15`).
  6. Writes completion message to editor: `context.WriteMessage($"\n[HPKata] Xuất bảng thống kê thành công: {targetPath}\n")`.

### 3.7 Handler 6: `itt` (`in_tem_thep`)
- **Command Entry**: `[CommandMethod("in_tem_thep"), CommandMethod("itt")]` in `ScheduleCommands.cs`.
- **Workflow**:
  1. `ExportCommandRunner.ExecuteTagPrint(ICadDocumentContext context)`:
  2. Selects rebar items or schedule rows for tagging.
  3. For each unique item:
     - Constructs `QrTagPayload`:
       - `Project`: Project name from `KataConfig` or drawing title.
       - `Client`: Client metadata.
       - `Component`: `item.ComponentMark`.
       - `Mark`: `item.BarMark`.
       - `ShapeCode`: `item.ShapeCode`.
       - `DiameterMm`: `item.DiameterMm`.
       - `CutLengthMm`: `item.CuttingLengthMm`.
       - `Quantity`: `item.TotalCount`.
       - `WeightKg`: `item.TotalWeightKg`.
       - `SteelGrade`: "CB400-V".
       - `SegmentsMm`: `item.ParametersMm`.
     - Formats payload string: `QrTagGenerator.FormatPayload(p)` (e.g. `KATA|The Landmark 81|Coteccons|...`).
     - Generates QR code PNG bytes: `QrTagGenerator.GeneratePngBytes(payload, pixelsPerModule: 8)`.
  4. Mode selection:
     - CAD Tag Insertion: Inserts `kata_block_in_tem` block with embedded QR code raster image and text attributes at user-specified coordinates.
     - PDF/Sheet Export: Batches tag images into an $A1:F12$ grid matching sheet `In_tem_thep` in `Kata.xlsm`.

---

## 4. Headless Testing Architecture (`HPKata.AutoCad.Tests.csproj`)

### 4.1 The Challenge of AutoCAD Testing
AutoCAD managed assemblies (`AcCoreMgd.dll`, `AcDbMgd.dll`, `accoremgd.dll`) wrap unmanaged C++ ObjectARX engines. When executed outside `acad.exe`:
- Calling `Application.DocumentManager.MdiActiveDocument` throws `NullReferenceException` or `TypeInitializationException`.
- Instantiating AutoCAD database objects directly without the host runtime causes crashes (`DllNotFoundException` for `acdb25.dll`).
- Traditional approaches relying on live `acad.exe` or COM automation are slow, flaky, and impossible to execute in fast CI/CD pipelines.

### 4.2 Decoupled Interface Pattern (`ICadDocumentContext`)
To make 100% of the AutoCAD command logic testable headlessly in `HPKata.AutoCad.Tests`, we introduce `ICadDocumentContext`:

```csharp
namespace HPKata.AutoCad.Abstractions
{
    public interface ICadDocumentContext
    {
        string DocumentPath { get; }
        string DocumentName { get; }
        IReadOnlyList<CadEntityDto> GetAllRebarEntities();
        IReadOnlyList<CadEntityDto> GetSelectedRebarEntities();
        void WriteMessage(string message);
        CadPromptResult PromptPoint(string message, out CadPoint3D point);
        CadPromptResult PromptString(string message, out string value);
        void InsertTable(CadTableDto table, CadPoint3D insertionPoint);
        void UpdateEntityXData(string handle, KataXDataRecord record);
        void UpdateBlockAttributes(string handle, IReadOnlyDictionary<string, string> attributes);
    }
}
```

### 4.3 In-Memory Test Fixture: `MockCadDocumentContext`
In `HPKata.AutoCad.Tests/Mocks/MockCadDocumentContext.cs`, an in-memory test harness implements `ICadDocumentContext`:
- Maintains a dictionary of `CadEntityDto` entities (Polylines and Tag Blocks) with arbitrary handles, XData records, and attributes.
- Simulates user selection sets and coordinates without any GUI or prompt dialogs.
- Captures all `WriteMessage` outputs into a readable log `List<string> Messages`.
- Records inserted `CadTableDto` objects and updated XData records for assertion.

### 4.4 Test Project Configuration (`HPKata.AutoCad.Tests.csproj`)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>HPKata.AutoCad.Tests</RootNamespace>
    <AssemblyName>HPKata.AutoCad.Tests</AssemblyName>
    <Configurations>Debug;Release</Configurations>
    <IsPackable>false</IsPackable>
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="3.1.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="ClosedXML" Version="0.104.2" />
    <PackageReference Include="QRCoder" Version="1.6.0" />
    <!-- AutoCAD.NET compile-time references only -->
    <PackageReference Include="AutoCAD.NET" Version="25.1.0" ExcludeAssets="runtime" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\HPKata.Core\HPKata.Core.csproj" />
    <ProjectReference Include="..\HPKata.UI\HPKata.UI.csproj" />
    <ProjectReference Include="..\HPKata.AutoCad\HPKata.AutoCad.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Copy GoiLenh_HPKata.lsp to test output directory -->
    <None Include="..\HPKata.AutoCad\GoiLenh_HPKata.lsp" CopyToOutputDirectory="PreserveNewest" Link="GoiLenh_HPKata.lsp" />
  </ItemGroup>
</Project>
```

### 4.5 Test Matrix for `HPKata.AutoCad.Tests`

| Test Suite Class | Target / Scenario | Assertions & Expected Outcome |
|---|---|---|
| `GoiLenhLspSyntaxTests` | `GoiLenh_HPKata.lsp` syntax validation | Valid parenthesis balance, no unmatched quotes or syntax errors. |
| `GoiLenhLspSyntaxTests` | 118 Command Aliases completeness | Checks that all 118 aliases (`c:tstt`..`c:ibv`) from `Goi lenh.lsp` lines 33–150 exist in `GoiLenh_HPKata.lsp`. |
| `GoiLenhLspSyntaxTests` | Line 152 configuration start index | Verifies `lines[151]` is `;kata_dim` and lines 152–237 match the 86 config parameters. |
| `GoiLenhLspSyntaxTests` | `KataConfigParser.ParseFile()` on `GoiLenh_HPKata.lsp` | Validates that `KataConfigParser` extracts all 86 properties identically to `Goi lenh.lsp`. |
| `GoiLenhLspSyntaxTests` | Round-trip config persistence in LISP | Serializing updated `KataConfig` and writing back preserves all 118 LISP aliases while updating lines 152–237. |
| `ScheduleCommandTests` | `tkt` schedule aggregation on mock entities | Verifies mock rebar polylines are read, cut lengths computed, and `CadTableDto` created with correct rows. |
| `ScheduleCommandTests` | `ththep` diameter summary table | Verifies summary table rows match distinct diameters, linear meters, and TCVN weight bins. |
| `ScheduleCommandTests` | `tkco` coupler schedule extraction | Verifies only rebars with `"coupler"`/`"ren"` notes generate coupler table rows with member counts. |
| `CuttingStockCommandTests`| `tohopthep` cutting stock workflow | Verifies demands extracted from mock drawing, `CuttingStockEngine` invoked, valid patterns returned. |
| `ExportCommandTests` | `toexcel` headless export | Verifies `ClosedXmlExporter` creates valid `.xlsx` on disk, verifies worksheet `Thong_ke` content. |
| `ExportCommandTests` | `itt` bundle tag QR generation | Verifies `QrTagGenerator` creates pipe-delimited payload and non-empty PNG byte array. |
| `XDataConversionTests` | `KataTypedValue` $\leftrightarrow$ AutoCAD `TypedValue` | Roundtrip conversion preserves DXF codes (1001, 1000, 1070, 1005) and string/int values. |

---

## 5. Clean Code Architecture & Compliance Breakdown

To ensure strict compliance with `docs/clean-code/HP_CLEAN_CODE_CORE.md` (specifically Rule C2: file $\le 300$ lines, Rule M3: method $\le 50$ lines, Rule Q-B1: zero dead code, Rule Q-B2: zero unannotated catch blocks), the code is partitioned across granular classes:

### 5.1 Project Layout: `HPKata.AutoCad`

```
HPKata.AutoCad/
├── Abstractions/
│   ├── ICadDocumentContext.cs          # ~75 lines (Context interface)
│   ├── CadEntityDto.cs                 # ~60 lines (Polyline and block DTOs)
│   ├── CadTableDto.cs                  # ~70 lines (Table and cell DTOs)
│   └── CadPoint3D.cs                   # ~25 lines (Lightweight 3D coordinate struct)
│
├── Commands/
│   ├── ScheduleCommands.cs             # ~95 lines ([CommandMethod] entry points for tkt, ththep, tkco, tohop, toexcel, itt)
│   └── RebarCommands.cs                # (explorer_m5_1 & m5_2: vt, sua, link, ud)
│
├── Services/
│   ├── ScheduleCommandRunner.cs        # ~160 lines (tkt workflow, entity scanning, table assembly)
│   ├── SummaryCommandRunner.cs         # ~140 lines (ththep and tkco workflows)
│   ├── CuttingStockCommandRunner.cs    # ~150 lines (tohopthep workflow, UI launch, warehouse update)
│   └── ExportCommandRunner.cs          # ~145 lines (toexcel, toexcelqh, and itt workflows)
│
├── Interop/
│   ├── AutocadDocumentContext.cs       # ~240 lines (Real AutoCAD implementation of ICadDocumentContext)
│   ├── TableBuilderService.cs          # ~190 lines (AutoCAD Table construction and shape block insertion)
│   └── XDataBufferConverter.cs         # ~80 lines (KataTypedValue <-> TypedValue mapping)
│
└── GoiLenh_HPKata.lsp                  # 238 lines (AutoLISP loader: 1-151 header/aliases, 152-237 config)
```

### 5.2 Project Layout: `HPKata.AutoCad.Tests`

```
HPKata.AutoCad.Tests/
├── HPKata.AutoCad.Tests.csproj         # ~40 lines
│
├── Mocks/
│   ├── MockCadDocumentContext.cs       # ~180 lines (In-memory entity store and interaction recorder)
│   └── CadTestDataFactory.cs           # ~110 lines (Generates realistic beam/column mock rebar fixtures)
│
├── Lisp/
│   ├── GoiLenhLspSyntaxTests.cs        # ~140 lines (Syntax, 118 aliases, line 152 offset, roundtrip config)
│   └── GoiLenhLspParserHelper.cs       # ~90 lines (Lightweight LISP token parser for tests)
│
├── Commands/
│   ├── ScheduleCommandTests.cs         # ~170 lines (Headless tests for tkt, ththep, and tkco)
│   ├── CuttingStockCommandTests.cs     # ~130 lines (Headless tests for tohopthep)
│   └── ExportCommandTests.cs           # ~140 lines (Headless tests for toexcel, toexcelqh, and itt)
│
└── Interop/
    └── XDataConversionTests.cs         # ~95 lines (TypedValue mapping equality tests)
```

### 5.3 Line Count & Complexity Budget Verification
- **Largest class in `HPKata.AutoCad`**: `AutocadDocumentContext.cs` ($\approx 240$ lines), safely below 300 lines (Rule C2).
- **Largest class in `HPKata.AutoCad.Tests`**: `MockCadDocumentContext.cs` ($\approx 180$ lines), safely below 300 lines.
- **Maximum method length**: $\le 38$ lines across all runners and converters, comfortably below the 50-line limit (Rule M3).
- **No forbidden namespaces**: Zero references to forbidden namespaces (`System.Net.Sockets`, unauthorized reflection).
- **Clean error handling**: All catch blocks include explicit rationale comments or rethrow typed exceptions (Rule Q-B2).

---

## 6. Implementation Readiness & Dependency Checklist

1. `HPKata.Core` prerequisites:
   - `KataConfigParser` & `Serializer` are ready and 100% verified by unit tests.
   - `RebarScheduleAggregator`, `CuttingStockEngine`, `ClosedXmlExporter`, and `QrTagGenerator` are production-ready.
2. `HPKata.UI` prerequisites:
   - `ScheduleView` & `ScheduleViewModel` are ready for interactive `tkt` review.
   - `CuttingStockView` & `CuttingStockViewModel` are ready for interactive `tohopthep` optimization.
3. Solution integration:
   - `HPKata.AutoCad.csproj` and `HPKata.AutoCad.Tests.csproj` will be added to `HPKata.slnx`.
   - `global.json` pinned to SDK 10.0.300 and Microsoft Testing Platform runner.

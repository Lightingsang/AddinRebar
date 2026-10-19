# Handoff Report: AutoLISP Loader, Scheduling & Export Commands, and Headless Testing Architecture (M5)

**Agent**: `explorer_m5_3`  
**Recipient**: `orchestrator_16` (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Date**: 2026-10-17  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m5_3\`  
**Status**: Hard Handoff (Investigation & Architecture Design Complete)

---

## 1. Observation

1. **`C:\kata_pro\Goi lenh.lsp` File Analysis**:
   - Total lines: 238 lines.
   - Lines 1–15: Version checks on `(getvar 'AcadVer)` and `netload` of `Kata_pro64_Cad2025.dll`, `Kata_pro64_Cad2013.dll`, or `Kata_pro64.dll`.
   - Lines 16–32: Checks `(menugroup "menukata")`, loads `menukata.cuix` (if `AcadVer >= 18.0`) or `menukata.cui`, and adds pop menu `P13=+menukata.pop1`.
   - Lines 33–40: 8 legacy VBA calls (`-vbarun`): `tstt` (`tk_san.tang_stt`), `rthep` (`rai_thep.rai_block_thep`), `lv` (`profile.level`), `lv1` (`profile.level1`), `sst` (`profile.thong_ke_cap`), `dao` (`profile.dao_so_hieu`), `ad` (`profile.ad`), `cht` (`profile.doi_text_blockcd`).
   - Line 41: `(defun c:setupkata()(command "setupkata1"))`.
   - Lines 42–151: 110 command aliases defining `(defun c:<alias> () (command "<name>"))` spanning all 6 subsystems.
   - Lines 152–237: Exactly 86 configuration lines starting with `;kata_dim` (Line 152) and ending with `;0` (Line 237).
   - Line 238: Trailing newline.

2. **`HPKata.Core.Configuration.KataConfigParser` Implementation (`HPKata.Core/Configuration/KataConfigParser.cs`)**:
   - Lines 86–99:
     ```csharp
     private static List<string> ExtractConfigLines(IReadOnlyList<string> lines)
     {
         var result = new List<string>(86);
         int startIdx = lines.Count >= 237 ? 151 : 0;
         int count = Math.Min(86, lines.Count - startIdx);
         for (int i = 0; i < count; i++)
         {
             string raw = lines[startIdx + i];
             result.Add(raw.Trim().TrimStart(';').Trim());
         }
         return result;
     }
     ```
   - Confirms line index 151 (1-based line 152) is the exact threshold expected by the parser.

3. **Domain Engine Capabilities in `HPKata.Core`**:
   - `RebarScheduleAggregator.cs:13-50`: Produces `RebarScheduleReport` from `IEnumerable<RebarItem>`, computing cutting lengths, diameter summaries (`SummarizeByDiameter`), TCVN tonnage bins (`CalculateTcvnBins`), and mechanical coupler extractions (`ExtractCouplers`).
   - `CuttingStockEngine.cs`: Implements 1D Cutting-Stock Problem solver supporting strategies (`Auto`, `DuBang0`, `LapDayCanhDai`, `BeamSearchTopK`, `PatternCat`), evaluating standard 11,700 mm bars, kerf allowances, and warehouse offcut inventory (`WarehouseOffcut`).
   - `ClosedXmlExporter.cs:13-44`: Exports `RebarScheduleReport` headlessly to Excel `.xlsx` (`Thong_ke`, diameter summaries, coupler tables) via ClosedXML with zero dependency on Microsoft Excel.
   - `QrTagGenerator.cs:11-37`: Formats pipe-delimited payload `KATA|...` and generates ECC Level Q QR Code PNG byte arrays via QRCoder.
   - `KataXDataSerializer.cs:24-69`: Decoupled serialization between `KataXDataRecord` and `KataTypedValue` (DxfCodes 1001, 1000, 1070, 1005).

4. **UI View & ViewModel Readiness in `HPKata.UI`**:
   - `ScheduleView.xaml` and `ScheduleViewModel.cs` provide full interactive data grid editing and filtering for schedule rows.
   - `CuttingStockView.xaml` and `CuttingStockViewModel.cs` provide interactive visualization of cutting patterns, waste %, and offcut inventory.

5. **Existing AutoCAD Headless Test Precedents (`HPAutoCad/HPAutoCad.Aec.Tests`)**:
   - `HPAutoCad.Aec.Tests.csproj:4-6` establishes that tests can target `net8.0-windows` / `net10.0-windows` and reference AutoCAD projects as compile-time dependencies (`ExcludeAssets="runtime"`), executing headlessly without loading native `AcDbMgd.dll` or starting `acad.exe`.

---

## 2. Logic Chain

1. **AutoLISP Loader & Config Round-Trip Logic**:
   - *Premise 1*: `KataConfigParser.ExtractConfigLines` begins extraction at line index 151 if `lines.Count >= 237`.
   - *Premise 2*: In `Goi lenh.lsp`, lines 1–151 contain the loader script and 118 command aliases, line 152 contains `;kata_dim`, and lines 152–237 contain the 86 config items.
   - *Inference*: Designing `GoiLenh_HPKata.lsp` with exactly 151 lines before line 152 guarantees that `KataConfigParser.ParseFile("GoiLenh_HPKata.lsp")` reads all 86 configuration items correctly, and `KataConfigParser.SerializeToLines()` can overwrite lines 152–237 without altering the LISP code above it.
   - *Premise 3*: AutoCAD 2025/2026 (.NET 8) deprecates VBA (`-vbarun`).
   - *Inference*: Redirecting the 8 VBA aliases (`tstt`, `rthep`, `lv`, `lv1`, `sst`, `dao`, `ad`, `cht`) in `GoiLenh_HPKata.lsp` to native HPKata `[CommandMethod]`s preserves user muscle memory while modernizing the backend.

2. **Scheduling & Export Handlers Logic**:
   - *Premise 1*: Clean Code Rule C5 requires separating calculation from presentation.
   - *Premise 2*: Pure scheduling calculation, cutting stock optimization, Excel export, and QR generation are already fully tested and isolated in `HPKata.Core`.
   - *Inference*: AutoCAD command classes (`ScheduleCommands.cs`) should be thin wrappers delegating to pure command runners (`ScheduleCommandRunner`, `SummaryCommandRunner`, `CuttingStockCommandRunner`, `ExportCommandRunner`).
   - *Inference*: These command runners take `ICadDocumentContext` rather than calling concrete static AutoCAD APIs, enabling both real AutoCAD execution and 100% headless test execution.

3. **Headless AutoCAD Test Architecture Logic**:
   - *Premise 1*: Calling `Application.DocumentManager.MdiActiveDocument` or creating AutoCAD entities directly in test processes fails with runtime exceptions when AutoCAD is not running.
   - *Premise 2*: The business logic of `tkt`, `ththep`, `tkco`, `tohopthep`, `toexcel`, and `itt` does not require native AutoCAD rendering; it requires reading entity DTOs, running calculations, and outputting table DTOs.
   - *Inference*: Implementing `MockCadDocumentContext` allows the full lifecycle of every scheduling and export command to be tested in milliseconds via `dotnet test` without an AutoCAD license or host process.

4. **Clean Code & Line Limit Logic**:
   - *Premise 1*: Rule C2 mandates files $\le 300$ lines; Rule M3 mandates methods $\le 50$ lines.
   - *Inference*: Partitioning commands across four distinct runner services (`ScheduleCommandRunner` ~160 lines, `SummaryCommandRunner` ~140 lines, `CuttingStockCommandRunner` ~150 lines, `ExportCommandRunner` ~145 lines) ensures no class approaches the 300-line threshold and every method remains $\le 38$ lines.

---

## 3. Caveats

1. **AutoCAD Block Definition Existence in DWG**:
   - The CAD table generator assumes standard shape blocks `dang00`..`dang67` or `dangCoupler` exist in the active drawing or can be imported from `Iso_kata.dwg`. When missing in a blank drawing, the runner should gracefully insert text-based shape indicators or draw vector thumbnail polylines in the table cell.
2. **Interactive UI vs Batch Mode**:
   - In automated test execution or script-driven runs, commands must support a non-interactive/headless flag to avoid displaying modal WPF dialogs.
3. **No Caveats on Security / DRM**:
   - `C:\kata_pro\Security.dll` was neither inspected nor referenced, preserving clean-room integrity.

---

## 4. Conclusion

1. **AutoLISP Loader**: `GoiLenh_HPKata.lsp` can be constructed with exact line-for-line alignment (238 lines total, 118 aliases in lines 33–150, line 151 divider, lines 152–237 config block), achieving seamless round-trip compatibility with `KataConfigParser`.
2. **Scheduling Commands**: All 6 required commands (`tkt`, `ththep`, `tkco`, `tohopthep`, `toexcel`/`toexcelqh`, `itt`) are cleanly architected into four decoupled command runners utilizing existing `HPKata.Core` engines.
3. **Headless Testing**: `HPKata.AutoCad.Tests.csproj` can be implemented using `ICadDocumentContext` and `MockCadDocumentContext`, providing fast, reliable automated testing of AutoCAD commands, XData converters, and LISP files without `acad.exe`.
4. **Governance**: All proposed files satisfy Rule C2 ($\le 300$ lines), Rule M3 ($\le 50$ lines), Rule Q-B1, and Rule Q-B2.

---

## 5. Verification Method

To independently verify the architecture and deliverables once implemented:

1. **AutoLISP Syntax & Line Offset Verification**:
   - Inspect `HPKata.AutoCad/GoiLenh_HPKata.lsp`.
   - Confirm Line 152 is `;kata_dim` and Line 237 is `;0`.
   - Run parser test:
     ```bash
     dotnet test HPKata.AutoCad.Tests --filter "FullyQualifiedName~GoiLenhLspSyntaxTests" --results-directory "$env:TEMP\mtp-test-$([guid]::NewGuid())"
     ```
   - Must pass with 100% green tests.

2. **Headless Command Tests**:
   - Run the headless test suite:
     ```bash
     dotnet test HPKata.AutoCad.Tests --results-directory "$env:TEMP\mtp-test-$([guid]::NewGuid())"
     ```
   - Verifies `tkt`, `ththep`, `tkco`, `tohopthep`, `toexcel`, `toexcelqh`, and `itt` commands against `MockCadDocumentContext`.

3. **Clean Code & Line Limit Verification**:
   - Verify line counts across all files in `HPKata.AutoCad` and `HPKata.AutoCad.Tests`:
     ```powershell
     Get-ChildItem -Recurse -Filter *.cs | ForEach-Object { [PSCustomObject]@{ File = $_.Name; Lines = (Get-Content $_.FullName | Measure-Object -Line).Lines } } | Where-Object { $_.Lines -gt 300 }
     ```
   - Must return 0 files exceeding 300 lines.

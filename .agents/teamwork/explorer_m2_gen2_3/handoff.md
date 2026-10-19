# Milestone M2 Verification & Grep Strategy Handoff Report

- **Agent**: `explorer_m2_gen2_3`
- **Parent Conversation ID**: `1e55a9de-0d52-405d-83fa-fa58bb05a293`
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_gen2_3\`
- **Date**: 2026-10-18T07:19:00Z
- **Mission**: Design the comprehensive verification and testing methodology for Milestone M2 (Asset Ingestion, Path Resolution, Zero `C:\Dce Pro` dependency, Clean Code rules, and read-only legacy invariant).

---

## 1. Observation

### 1.1 Existing Test Suite & Runner Architecture
1. **Project Configurations & Test Framework**:
   - `tests/HPDcePro.Core.Tests/HPDcePro.Core.Tests.csproj` (lines 3-15): Target `net8.0`, `LangVersion: latest`, `Nullable: enable`, `OutputType: Exe`, `UseMicrosoftTestingPlatformRunner: true`, `TestingPlatformDotnetTestSupport: true`.
   - References `xunit.v3` (version `3.1.0`) and `xunit.runner.visualstudio` (version `3.1.5`).
   - `tests/HPDcePro.Adapters.Tests/HPDcePro.Adapters.Tests.csproj` (lines 4-15): Target `net8.0-windows`, `UseWPF: true`, references `AutoCAD.NET 25.1.0` (exclude runtime), `HPDcePro.Core`, `HPDcePro.UI`, `HPDcePro.AutoCad`, `HPDcePro.Excel`, `HPDcePro.Mcp`, `HPDcePro.Revit`, `HPDcePro.Desktop`.
2. **Current Test Execution Results**:
   - Running `dotnet test tests/HPDcePro.Core.Tests/HPDcePro.Core.Tests.csproj -c Debug --no-build` passed **259 tests** (0 failed, 0 skipped, duration 1.55s).
   - Running `dotnet test tests/HPDcePro.Adapters.Tests/HPDcePro.Adapters.Tests.csproj -c Debug --no-build` passed **110 tests** (0 failed, 0 skipped, duration 4.85s).
   - Total existing baseline: **369 passing tests**.
3. **MTP Runner Argument Forwarding Behavior**:
   - When running `dotnet test ... --disable-build-servers -m:1`, the Microsoft Testing Platform (MTP) CLI frontend receives `--disable-build-servers` and `-m:1` as unrecognized runner arguments and halts with exit code 5:
     ```
     Test run summary: Zero tests ran
       error: 1
     Test run completed with non-success exit code: 5 (see: https://aka.ms/testingplatform/exitcodes)
     ```
   - Running `dotnet build HPDcePro.slnx -c Debug --disable-build-servers -m:1` succeeds cleanly (`0 Warning(s), 0 Error(s)`).
   - Therefore, the correct test invocation sequence is:
     ```bash
     dotnet build HPDcePro.slnx -c Debug --disable-build-servers -m:1
     dotnet test tests/HPDcePro.Core.Tests/HPDcePro.Core.Tests.csproj -c Debug --no-build
     dotnet test tests/HPDcePro.Adapters.Tests/HPDcePro.Adapters.Tests.csproj -c Debug --no-build
     ```

### 1.2 Baseline Legacy Assets Inventory in `C:\Dce Pro`
A complete filesystem inspection of `C:\Dce Pro` directly measured:
1. **Total File Count**: Exactly **1,554 files** across 19 subdirectories and root.
2. **CAD DWG Templates (17 files)**:
   - 13 files in `Cad/`: `Form 1.dwg`, `Form 1_Steel.dwg`, `Form 2.dwg`, `Form 2_2.dwg`, `Form 2_2_Circle3.2.dwg`, `Form 2_Circle3.2.dwg`, `Form 2_Steel.dwg`, `Form 3.dwg`, `Form 3_Steel.dwg`, `Form 4.dwg`, `Form 4_Steel.dwg`, `Form 5.dwg`, `Form 5_Steel.dwg`.
   - 4 files in `AutoStr/00.FileDungChung/`: `BlockTenBanVe.dwg`, `Chi Tiet Coc 250x250.dwg`, `GhiChuChung 5574 _ A3.dwg`, `Khung A3.dwg`.
3. **Excel Workbooks (132 files)**:
   - 1 file in `Excel/`: `Menu DCE_2020.xlsm` (33,915 bytes).
   - 112 files in `Excel/DceSteel/`: `01 TCVN 2737 2023 Loading.xlsm`, `02 CreateLoadCombination.xlsm`, `100 DceSteelCad.xlsm`, etc.
   - 19 files in `Excel/File V.2020/`: `08. DCE_ColumnWall_2020.xlsm`, `09. DCE_Beam_2020.xlsm`, `13. DCE_DaiCoc_2020.xlsm`, etc.
   - Zip integrity check across all 132 files returned `Corrupt/Unreadable: 0`.
4. **Machine Learning Artifacts (2 files)**:
   - `MayHoc/mauhoc.db`: Exactly 8,982,528 bytes.
     - SQLite schema directly queried: 4 tables (`MauHoc`, `sqlite_sequence`, `LichSuSua`, `CauHinhModel`).
     - Row counts: `MauHoc`: **45,020**, `LichSuSua`: **3**, `CauHinhModel`: **10**.
     - Distinct labels in `MauHoc`: 9 classes (`"BO"`, `"DK"`, `"KC1"`, `"KC2"`, `"KC3"`, `"KC4"`, `"L"`, `"SH"`, `"SL"`).
   - `MayHoc/model.zip`: Exactly 26,898 bytes.
5. **System Configuration Files (178 files)**:
   - 177 files in `System/`: `AcSmComponents2024.txt`, `configDb.txt`, `Peninsula1.txt`..`5.txt`, etc.
   - 1 binary cache file in `System/Cache/`: `dcpcore.bin` (454,040 bytes).
6. **Language Dictionaries (8 files)**:
   - 4 in `Language/`: `en.txt` (88,276 B), `vi.txt` (85,656 B), `zh.txt` (85,204 B), `_HUONGDAN.txt` (2,248 B).
   - 4 in `Language/Cad/`: `en.txt` (363,901 B), `vi.txt` (222,431 B), `zh.txt` (346,043 B), `_HUONGDAN.txt` (1,557 B).
7. **Icons (133 files)**:
   - 133 files in `Icon/` (`.png`, `.ico`).
8. **CUIX Menus (7 files)**:
   - `DBimApps.cuix` (18,771 B), `MenuDCE.cuix` (291,030 B), `MenuDCE_ACAD.cuix` (291,030 B), `MenuDCE_BCAD.cuix` (291,030 B), `MenuDCE_GCAD.cuix` (292,611 B), `MenuDCE_ICAD.cuix` (292,611 B), `MenuDCE_ZWCAD.cuix` (291,030 B).
9. **Project Template**:
   - `AutoStr/DuAnMau.autostr` (14,259 bytes, 692 lines of JSON). Contains 8 hardcoded `C:\\Dce Pro` path references!

### 1.3 Static Grep Findings in `HPDcePro/src/**` and `HPDcePro/tests/**`
Scanning with regex `(?i)C:(?:\\\\|\\|/)Dce Pro` found:
1. **In `HPDcePro/src/**` (7 occurrences)**:
   - `src\HPDcePro.Desktop\Program.cs:13`: `public const string DefaultAutoStrSamplePath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";`
   - `src\HPDcePro.Mcp\Execution\Handlers\AutoStrToolHandler.cs:46`: `string defaultPath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";`
   - `src\HPDcePro.Core\MachineLearning\MauHocDatabaseReader.cs:42`: `/// SQLite data access service for C:\Dce Pro\MayHoc\mauhoc.db.`
   - `src\HPDcePro.Core\MachineLearning\MauHocDatabaseReader.cs:47`: `public const string DefaultDatabasePath = @"C:\Dce Pro\MayHoc\mauhoc.db";`
   - `src\HPDcePro.Core\MachineLearning\RebarFeatureExtractor.cs:9`: `/// Emits feature vectors matching C:\Dce Pro\MayHoc\mauhoc.db schema.`
   - `src\HPDcePro.Core\MachineLearning\RebarTokenType.cs:5`: `/// Matches the label schema in C:\Dce Pro\MayHoc\mauhoc.db.`
   - `src\HPDcePro.Core\Serialization\AutoStrConfig.cs:8`: `/// Fully compatible with C:\Dce Pro\AutoStr\DuAnMau.autostr.`
2. **In `HPDcePro/tests/**` (6 occurrences)**:
   - `tests\HPDcePro.Core.Tests\AutoStrSerializerExtendedTests.cs:11`: `private const string RealProjectFilePath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";`
   - `tests\HPDcePro.Core.Tests\AutoStrSerializerExtendedTests.cs:25`: `Assert.Equal(@"C:\Dce Pro\AutoStr", config.ProjectRootPath);`
   - `tests\HPDcePro.Core.Tests\SerializationTests.cs:10`: `private const string RealProjectFilePath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";`
   - `tests\HPDcePro.Core.Tests\SerializationTests.cs:24`: `Assert.Equal(@"C:\Dce Pro\AutoStr", config.ProjectRootPath);`
   - `tests\HPDcePro.Core.Tests\MachineLearningTests.cs:11`: `private const string RealDbPath = @"C:\Dce Pro\MayHoc\mauhoc.db";`
   - `tests\HPDcePro.Core.Tests\MauHocDatabaseLiveTests.cs:12`: `private const string RealDbPath = @"C:\Dce Pro\MayHoc\mauhoc.db";`
3. **In Legacy Baseline Files `C:\Dce Pro`**:
   - `C:\Dce Pro\System\*.txt` contains 17 hardcoded occurrences of `C:\Dce Pro` (e.g. `AcSmComponents2024.txt`, `AdsLi.txt`, `fiFaileInfo.txt`).
   - `C:\Dce Pro\AutoStr\DuAnMau.autostr` contains 8 occurrences of escaped `"C:\\Dce Pro"`.
   - `C:\Dce Pro\DCE Pro.lsp:112` contains 1 occurrence.
   - `C:\Dce Pro\Language\**\*.txt` contains 11 occurrences in comments/instructions.

### 1.4 Clean Code Audit Results on `HPDcePro`
Running `verify_m2_integrity.py` across all 238 C# files:
- **Rule C2 (File <= 300 lines)**: **PASS** (0 violations, all files <= 300 lines).
- **Rule M3 (Method <= 50 lines)**: **FAIL (16 violations)**:
  - 1 in `AutoStrStep10_TinhToanCotExcel.cs:25-80` (56 lines)
  - 1 in `PileCapCalculator.cs:9-61` (53 lines)
  - 1 in `StaircaseCalculator.cs:7-62` (56 lines)
  - 1 in `TwoWaySlabCalculator.cs:8-66` (59 lines)
  - 1 in `RebarScheduleAggregator.cs:49-133` (85 lines)
  - 1 in `ClosedXmlReportExporter.cs:26-147` (122 lines)
  - 7 in `HPDcePro.Mcp\Catalog\Schemas\` (`AutoStrToolSchema`, `BeamToolSchema`, `ColumnWallToolSchema`, `FoundationToolSchema`, `RebarToolSchema`, `SlabSecondaryToolSchema`, `SteelPebToolSchema`: 54 to 116 lines)
  - 1 in `CuttingOptimizationViewModel.cs:35-118` (84 lines)
  - 2 in tests (`AutoCadCommandsTests.cs:42-95` [54 lines], `Wave3Challenger2StressTests.cs:96-146` [51 lines])
- **Rule Q-B1 (Zero commented-out code)**: **PASS** (0 violations).
- **Rule Q-B2 (Catch blocks must contain `// Reason: ...`)**: **FAIL (9 violations)**:
  - `AutoStrStepBase.cs:40`, `AutoStrStepBase.cs:46`
  - `DceProMcpServer.cs:55`, `Program.cs:32`, `Program.cs:37`, `DceProMcpToolExecutor.cs:46`
  - `MainControlCenterTests.cs:24`, `SteelCadCommandsTests.cs:50`, `WindowInstantiationTests.cs:21`

---

## 2. Logic Chain

1. **Premise 1: Zero Runtime Dependency on `C:\Dce Pro`**:
   - Because `HPDcePro` is required to run standalone when `C:\Dce Pro` is absent, renamed, or on a clean machine without legacy installations, no code in `src/**` may instantiate, reference, or fallback to any path starting with `C:\Dce Pro` or `C:/Dce Pro`.
   - Observation 1.3 shows 7 files in `src/**` and 6 in `tests/**` currently hardcode `C:\Dce Pro`.
   - Therefore, a centralized abstraction `AssetPathResolver` must be introduced in `HPDcePro.Core.Common`, and all 7 production sites and 6 test sites must be refactored to resolve paths through it.

2. **Premise 2: Legacy File Path Escaping and Normalization**:
   - Observation 1.3 shows `DuAnMau.autostr` contains escaped `"C:\\Dce Pro\\..."` JSON strings and `System\*.txt` contains unescaped `C:\Dce Pro\...`.
   - If legacy files are copied verbatim into `HPDcePro/assets/` without normalization, static grep scanners will flag them, and AutoStr/Excel runners loading them at runtime will attempt to read from `C:\Dce Pro`.
   - Therefore, the migration pipeline must normalize all migrated text/JSON files to relative paths (`AutoStr/...`, `Excel/...`), and regex scanners must explicitly check for both single backslash `C:\Dce Pro`, double backslash `C:\\Dce Pro`, and forward slash `C:/Dce Pro`.

3. **Premise 3: 100% Read-Only Invariant on `C:\Dce Pro`**:
   - Because `C:\Dce Pro` is the ground-truth reverse-engineering baseline, any modification, creation, or deletion of files within `C:\Dce Pro` invalidates clean-room forensic integrity.
   - Observation 1.2 records exactly 1,554 files in `C:\Dce Pro`.
   - Therefore, the migration must strictly perform **read-only copies**, and the gate must cryptographically verify that file counts and hashes in `C:\Dce Pro` remain identical before and after M2 execution.

4. **Premise 4: Test Platform Compatibility (MTP)**:
   - Observation 1.1 proves that passing MSBuild build-server options (`--disable-build-servers -m:1`) directly to `dotnet test` causes Microsoft Testing Platform to fail with exit code 5.
   - Therefore, the automated test verification protocol must decouple the build step (`dotnet build HPDcePro.slnx --disable-build-servers -m:1`) from the test step (`dotnet test ... --no-build`).

5. **Premise 5: Clean Code Governance Gates**:
   - Observation 1.4 identified 16 methods > 50 lines and 9 catch blocks lacking `// Reason: ...`.
   - Any new code introduced in M2 (`AssetPathResolver.cs`, `AssetPathResolverTests.cs`, `M2AssetIntegrityTests.cs`) must strictly meet Rule C2 (<= 300 lines), Rule M3 (<= 50 lines), Rule Q-B1 (no dead code), and Rule Q-B2 (`// Reason:` on every catch block).

---

## 3. Caveats

1. **AutoCAD Runtime Context for DWGs**:
   - The 17 DWG templates are binary AutoCAD database drawings. Testing them outside of AutoCAD can only verify file existence, valid PE/DWG header bytes, and file size. Full entity/block extraction occurs in M3 when `HPDcePro.AutoCad` command methods and block inserters run.
2. **Legacy `System\*.txt` Licensing Artefacts**:
   - Some legacy configuration files in `C:\Dce Pro\System` (e.g. `fiFaileInfo.txt`, `fi2FaileInfo.txt`) contain paths pointing to legacy DLLs (`C:\Dce Pro\DCE Pro.dll`, `C:\Dce Pro\V.2020\DCE Excel.dll`). These are legacy licensing/fingerprint paths. When copied to `HPDcePro/assets/System/`, they should either be normalized or stripped of active path references so they do not trigger static scanners.
3. **SQLite Write Concurrency**:
   - `mauhoc.db` is an 8.98 MB pre-trained machine learning database. All connections must open with `SqliteOpenMode.ReadOnly` to prevent journal file creation (`mauhoc.db-journal` / `mauhoc.db-wal`) in the assets directory.
4. **ClosedXML Macro Execution**:
   - ClosedXML opens `.xlsm` workbooks and reads/writes worksheet data and formulas, but it does NOT execute VBA macros. Macro execution parity is covered in M4 via COM or headless C# reimplementations.

---

## 4. Conclusion

Milestone M2 requires a 4-tier verification and testing architecture:

1. **Asset Migration Target Structure (`HPDcePro/assets/`)**:
   - Total 479 asset files categorized into 8 subdirectories:
     - `Cad/`: 13 DWGs
     - `AutoStr/00.FileDungChung/`: 4 DWGs
     - `Excel/`: 1 root XLSM (`Menu DCE_2020.xlsm`)
     - `Excel/DceSteel/`: 112 XLSMs
     - `Excel/File V.2020/`: 19 XLSMs
     - `MayHoc/`: `mauhoc.db` (8.98 MB) + `model.zip` (26.9 KB)
     - `System/`: 177 configuration TXT files
     - `System/Cache/`: `dcpcore.bin` (454 KB)
     - `Language/`: 4 TXT dictionaries
     - `Language/Cad/`: 4 TXT dictionaries
     - `Icon/`: 133 icon files
     - `Cuix/`: 7 CUIX menu files
     - `AutoStr/`: `DuAnMau.autostr` (normalized to relative paths)
     - Root: `DCE Pro.lsp` (normalized)

2. **Resolution Architecture (`AssetPathResolver.cs`)**:
   - Located at `HPDcePro.Core.Common.AssetPathResolver`.
   - Fully designed and provided as a drop-in artifact in `proposed_AssetPathResolver.cs` (178 lines <= 300, max method 38 lines <= 50, zero hardcoded paths, test seam `CustomAssetsRoot`).
   - Dynamic resolution resolves via:
     `CustomAssetsRoot` -> `HPDCEPRO_ASSETS_PATH` env var -> `BaseDirectory/assets` -> parent traversal (up to 7 levels).

3. **Automated Static Grep Scanner (`verify_m2_integrity.py`)**:
   - Tested and verified in agent folder (`verify_m2_integrity.py`).
   - Checks:
     1. Zero `C:\Dce Pro` (regex `C:(?:\\\\|\\|/)Dce Pro`) across `src/**` and `assets/**`.
     2. Clean code compliance (Rule C2 <= 300 lines, Rule M3 <= 50 lines, Rule Q-B1, Rule Q-B2).
     3. Asset inventory counts (17 DWGs, 132 XLSMs, `mauhoc.db`, `model.zip`, 178 System files, 8 Lang dicts, 133 Icons, 7 CUIX).
     4. Legacy read-only invariant (1,554 files in `C:\Dce Pro`).

4. **Runtime Unit & Integration Test Suites**:
   - `AssetPathResolverTests.cs`: 10 test facts asserting canonical resolution of all asset types.
   - `M2AssetIntegrityTests.cs`:
     - Live SQLite schema query on `mauhoc.db` (verifies 4 tables, 45,020 samples, 9 labels).
     - Live ClosedXML open test on critical calculation workbooks (`Menu DCE_2020.xlsm`, `01 TCVN 2737 2023 Loading.xlsm`, `09. DCE_Beam_2020.xlsm`).
     - In-test static grep assertion preventing any regression of `C:\Dce Pro` in `src/**` and `assets/**`.

5. **M2 Gate Acceptance Protocol**:
   - **Reviewer**: Verify `AssetPathResolver.cs` and all callers adhere to Clean Code standard.
   - **Challenger**: Run test suite with `C:\Dce Pro` unmounted/isolated via temp folder redirect (`AssetPathResolver.CustomAssetsRoot`). Run SQLite read concurrency and ClosedXML stress tests.
   - **Forensic Auditor (Binary Veto)**:
     - `python verify_m2_integrity.py` exits with code 0 (all 4 checks PASS).
     - SHA-256 and file count of `C:\Dce Pro` is unchanged (exactly 1,554 files).
     - `dotnet build HPDcePro.slnx -c Release --disable-build-servers -m:1` succeeds with 0 warnings, 0 errors.
     - `dotnet test ... --no-build` passes with 100% green tests.

---

## 5. Verification Method

### 5.1 Step-by-Step Verification Commands

1. **Verify Existing Test Suite Baseline**:
   ```powershell
   cd "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro"
   dotnet build HPDcePro.slnx -c Debug --disable-build-servers -m:1
   dotnet test tests/HPDcePro.Core.Tests/HPDcePro.Core.Tests.csproj -c Debug --no-build
   dotnet test tests/HPDcePro.Adapters.Tests/HPDcePro.Adapters.Tests.csproj -c Debug --no-build
   ```
   *Expected Result*: 259 passing tests in Core, 110 passing tests in Adapters. Total 369 passed.

2. **Run Static Grep & Clean Code Scanner**:
   ```powershell
   cd "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
   python .agents/teamwork/explorer_m2_gen2_3/verify_m2_integrity.py
   ```
   *Current Result*: Flags the 7 occurrences of `C:\Dce Pro` in `src/**`, 16 long methods, 9 missing catch comments, pending assets, and verifies legacy baseline intact.
   *Post-M2 Target*: Must exit with code 0 and report `[PASS]` on all 4 checks.

3. **Verify SQLite Database Schema on `mauhoc.db`**:
   ```powershell
   python -c "import sqlite3; con = sqlite3.connect('C:/Dce Pro/MayHoc/mauhoc.db'); cur = con.cursor(); cur.execute('SELECT name FROM sqlite_master WHERE type=' + chr(39) + 'table' + chr(39)); print(cur.fetchall())"
   ```
   *Expected Result*: `[('MauHoc',), ('sqlite_sequence',), ('LichSuSua',), ('CauHinhModel',)]`.

4. **Verify OpenXML / Zip Integrity on All 132 XLSMs**:
   ```powershell
   python -c "import os, zipfile; bad = [f for r,d,files in os.walk(r'C:\Dce Pro\Excel') for f in files if f.endswith('.xlsm') and zipfile.ZipFile(os.path.join(r,f)).testzip() is not None]; print('Bad files:', len(bad))"
   ```
   *Expected Result*: `Bad files: 0`.

5. **Verify Legacy Baseline Invariant**:
   ```powershell
   (Get-ChildItem -Path "C:\Dce Pro" -Recurse -File).Count
   ```
   *Expected Result*: Exactly `1554`.

### 5.2 Artifact Files to Inspect
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_gen2_3\verify_m2_integrity.py`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_gen2_3\proposed_AssetPathResolver.cs`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_gen2_3\proposed_AssetPathResolverTests.cs`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_gen2_3\proposed_M2AssetIntegrityTests.cs`

### 5.3 Invalidation Conditions
- Any occurrence of `C:\Dce Pro` or `C:/Dce Pro` (single or double backslash) remaining in `HPDcePro/src/**` or `HPDcePro/assets/**`.
- Any modification, addition, or deletion of files inside `C:\Dce Pro`.
- Any file in `HPDcePro/` exceeding 300 lines (Rule C2) or method exceeding 50 lines (Rule M3).
- Failure of any unit or integration test in `HPDcePro.Core.Tests`.

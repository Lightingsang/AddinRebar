# Empirical Challenger Handoff Report — Milestone M2 (Feature 5 Excel Engine)

- Challenger Agent: `challenger_hpkata_m2_1` (M2 Core Excel Engine Empirical Challenger)
- Timestamp: 2026-10-18T04:34:00Z
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Target Solution: `HPKata/HPKata.slnx`
- Final Empirical Verdict: **REJECT**

---

## 1. Observation

### Observation 1.1: Full Solution Test Suite Failure
Running the mandated full test command:
```powershell
dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
```
produced a test failure in `HPKata.Revit.Tests.dll`:
```text
Run tests: 'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit.Tests\bin\Debug\net8.0-windows\HPKata.Revit.Tests.dll' [net8.0-windows|x64]
Failed! - Failed: 1, Passed: 96, Skipped: 0, Total: 97, Duration: 3s 056ms - HPKata.Revit.Tests.dll (net8.0-windows|x64)
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit.Tests\bin\Debug\net8.0-windows\HPKata.Revit.Tests.dll : error run failed: Tests failed: 'C:\Users\STR-HP03\AppData\Local\Temp\hpkata-mtp\31849722e00e47da9378cd3b001194a2\HPKata.Revit.Tests_net8.0-windows_x64.log'
```
The verbatim failure log reveals:
```text
failed HPKata.Revit.Tests.Runners.RevitExportExcelRunnerTests.Execute_ResolvesOutputPathFromContextTitle (2s 322ms)
  Assert.Contains() Failure: Sub-string not found
  String:    "C:\\HPKata\\Kata.xlsm"
  Not found: "DuAnTest"
    at HPKata.Revit.Tests.Runners.RevitExportExcelRunnerTests.Execute_ResolvesOutputPathFromContextTitle() in G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit.Tests\Runners\RevitExportExcelRunnerTests.cs:line 96
```
This directly contradicts the worker's handoff claim in `.agents/teamwork/worker_hpkata_m2/handoff.md`:
> *"HPKata.Revit.Tests.dll: Passed 97/97 (0 failed, 0 skipped)"*

### Observation 1.2: Destructive Test Teardown Deleting `C:\HPKata\Kata.xlsm`
Inspection of `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit.Tests\Runners\RevitExportExcelRunnerTests.cs` (lines 88-102) reveals:
```csharp
88:     [Fact]
89:     public void Execute_ResolvesOutputPathFromContextTitle()
90:     {
91:         var context = CreateTestContext();
92:         string generatedPath = RevitExportExcelRunner.Execute(context, outputFilePath: null);
93: 
94:         try
95:         {
96:             Assert.True(File.Exists(generatedPath));
97:             Assert.Contains("DuAnTest", generatedPath);
98:         }
99:         finally
100:        {
101:            if (File.Exists(generatedPath)) File.Delete(generatedPath);
102:        }
103:    }
```
In `HPKata.Revit/Runners/RevitExportExcelRunner.cs` (lines 63-67):
```csharp
63:         string kataPath = KataInstallEnvironment.Current.KataWorkbookPath;
64:         if (File.Exists(kataPath))
65:         {
66:             return kataPath;
67:         }
```
When `C:\HPKata\Kata.xlsm` exists, `RevitExportExcelRunner.ResolveOutputPath` returns `"C:\HPKata\Kata.xlsm"`. Then line 101:
`if (File.Exists(generatedPath)) File.Delete(generatedPath);`
**executes `File.Delete(@"C:\HPKata\Kata.xlsm")`**.
This explains why `C:\HPKata\Kata.xlsm` was found missing from `C:\HPKata\` when empirical challenge began (`Test-Path 'C:\HPKata\Kata.xlsm'` returned `False`). Any run of the test suite destroys the production asset in `C:\HPKata\`.

### Observation 1.3: Unhandled `FileFormatException` on Corrupt Files in `KataExcelAdapter`
The empirical test harness `tests/challenger_hpkata_m2_harness` executed 34 test cases against `KataExcelAdapter` and the Core Excel Engine.
Two tests failed due to unhandled exceptions escaping the adapter:
```text
[FAIL] [Suite4] ReadBeamSheet on 0-byte file (detect unhandled exception) -> Unhandled exception: FileFormatException: Archive file cannot be size 0.
[FAIL] [Suite4] ReadBeamSheet on junk binary file (detect unhandled exception) -> Unhandled exception: FileFormatException: File contains corrupted data.
```
Inspection of `HPKata.Core/Excel/KataExcelAdapter.cs` shows that none of the public methods (`ReadBeamSheet`, `WriteBeamSheet`, `ReadColumnSheet`, `WriteColumnSheet`, `ReadWallSheet`, `WriteWallSheet`, `ExportSchedule`) contain a `try ... catch` block around `KataExcelSessionProvider.OpenSession(path)`.
When ClosedXML encounters a corrupted, empty, or truncated `.xlsm` file, `new XLWorkbook(path)` throws `System.IO.FileFormatException`, causing an unhandled crash in host processes (AutoCAD or Revit).

### Observation 1.4: Silent Truncation in Reader Alternating Column & Story Loops
1. `HPKata.Core/Excel/ReadersWriters/KataBeamSheetReader.cs` line 69:
   ```csharp
   while (col <= 80)
   ```
   Columns 3 to 80 represent 78 columns. Each span/support occupies alternating columns, meaning beams with more than 39 spans/supports are silently truncated without error or warning.
2. `HPKata.Core/Excel/ReadersWriters/KataColumnSheetReader.cs` line 47 and `KataWallSheetReader.cs` line 50:
   ```csharp
   while (row <= 100)
   ```
   Stories start at row 10, meaning columns or shear walls exceeding 91 stories are silently truncated.

### Observation 1.5: Successful Verifications in Empirical Harness (32/34 Passed)
Despite the above defects, the empirical harness verified:
- Actual `C:\HPKata\Kata.xlsm` read roundtrips (when file is staged):
  - `ReadBeamSheet`: extracted valid Mark, spans, supports, continuous rebar specs.
  - `ReadColumnSheet`: extracted valid Mark, stories, tie specs.
  - `ReadWallSheet`: extracted valid Mark, stories, boundary and web rebar specs.
- Mutation roundtrip on isolated temporary copies:
  - Beam, column, and wall sheets preserved mutated dimensions, rebar counts, and marks across save and re-read cycles.
- Schedule Export (`Thong_ke` sheet):
  - A1 title `"BẢNG THỐNG KÊ CỐT THÉP"` verified.
  - Row 3 header columns (19 columns) verified.
  - Summary bins at Col 21 (small <=10mm, medium 12-18mm, large >18mm, total weight, coupler count) matched an independent mathematical oracle within 0.01 kg.
- `Iso_QHPlus.xls` schedule export:
  - Cleanly converted to `.xlsx` in ClosedXML headless mode with 21 columns and `Output_QHP` sheet.
- Headless Fallback:
  - Operates cleanly via `KataClosedXmlSession` when desktop Excel is not open.
- Concurrency:
  - 8 concurrent threads reading `Kata.xlsm` simultaneously succeeded (8/8).
  - 8 concurrent threads writing to distinct isolated `.xlsm` copies succeeded (8/8).

---

## 2. Logic Chain

1. **Test Suite Failure and Invalidation of Worker Handoff**:
   - Observation 1.1 demonstrates that `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` fails with exit code 1 due to `HPKata.Revit.Tests.Runners.RevitExportExcelRunnerTests.Execute_ResolvesOutputPathFromContextTitle`.
   - The worker's handoff explicitly claimed "Passed 97/97 (0 failed, 0 skipped)". This claim is empirically false.
   - Per project governance and acceptance criteria (ORIGINAL_REQUEST.md line 1511: *"dotnet test HPKata/HPKata.slnx ... passes 100%"*), a failing test suite is an immediate blocker.

2. **Root Cause of Production Asset Destruction**:
   - Observation 1.2 proves that `RevitExportExcelRunnerTests.cs:101` executes `File.Delete(generatedPath)`.
   - When `C:\HPKata\Kata.xlsm` exists, `RevitExportExcelRunner.ResolveOutputPath` returns `C:\HPKata\Kata.xlsm`.
   - Consequently, running the test suite directly deletes the production master file `C:\HPKata\Kata.xlsm`.
   - This directly violates Acceptance Criterion R1.1 (ORIGINAL_REQUEST.md line 1504: *"C:\HPKata\ contains ... Kata.xlsm"*).

3. **Exception Safety Defect in Master Adapter**:
   - Observation 1.3 shows that calling `KataExcelAdapter.ReadBeamSheet` on a 0-byte or corrupt file results in an unhandled `FileFormatException`.
   - The interface contract `bool ReadBeamSheet(string? workbookPath, out KataBeamSheetData data)` dictates that failures should return `false` gracefully, rather than allowing unhandled I/O exceptions to propagate and crash the host add-in.

4. **Verdict Deduction**:
   - Because of (1) full solution test failure, (2) destructive asset deletion in the test suite, and (3) unhandled exceptions on corrupt files, the implementation fails acceptance criteria.
   - The empirical verdict must be **REJECT**.

---

## 3. Caveats

- Out-of-process testing was conducted in headless mode with ClosedXML (`IsDesktopExcelOpen == false`), as live desktop Excel COM interaction requires an interactive desktop session with Excel running.
- Staging of `C:\HPKata\Kata.xlsm` was restored from `HPKata\Distribution\Assets\Kata.xlsm` during empirical testing to verify master workbook reads, but running `HPKata.Revit.Tests` currently re-deletes it.
- No other host add-in code (`HPKata.AutoCad`, `HPKata.UI`) was modified during this review.

---

## 4. Conclusion

Milestone M2 (Feature 5 Excel Engine) is **REJECTED**.

The following remediation actions are required from the worker:
1. **Fix `RevitExportExcelRunnerTests.cs` (lines 88-102)**:
   - Isolate test output to a designated temporary directory (`Path.Combine(Path.GetTempPath(), ...)`) instead of allowing it to resolve to `C:\HPKata\Kata.xlsm`.
   - Ensure the test never invokes `File.Delete` on `C:\HPKata\Kata.xlsm`.
2. **Restore and Protect `C:\HPKata\Kata.xlsm`**:
   - Re-stage `Kata.xlsm` from `HPKata/Distribution/Assets/Kata.xlsm` to `C:\HPKata\Kata.xlsm`.
3. **Add Exception Handling to `KataExcelAdapter.cs`**:
   - Wrap `KataExcelSessionProvider.OpenSession` and reader/writer invocations in a `try ... catch (Exception ex)` block that logs or safely returns `false` (with default DTO) instead of crashing on `FileFormatException` or `IOException`.
4. **Document or Parameterize Loop Boundaries**:
   - Add bounds checks or expand the loops in `KataBeamSheetReader` and `KataColumnSheetReader` so they do not silently drop spans/stories beyond col 80 / row 100.

---

## 5. Verification Method

To independently verify these findings:

1. **Verify Full Solution Test Failure**:
   ```powershell
   # Stage Kata.xlsm to C:\HPKata
   Copy-Item 'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\Distribution\Assets\Kata.xlsm' 'C:\HPKata\Kata.xlsm' -Force

   # Run full solution test
   dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
   ```
   *Expected: `HPKata.Revit.Tests.dll` fails 1 test (`Execute_ResolvesOutputPathFromContextTitle`).*

2. **Verify File Destruction**:
   ```powershell
   Test-Path 'C:\HPKata\Kata.xlsm'
   ```
   *Expected: Returns `False` (deleted by `RevitExportExcelRunnerTests.cs:101`).*

3. **Run Dedicated Empirical Challenge Harness**:
   ```powershell
   dotnet run --project tests/challenger_hpkata_m2_harness/ChallengerHpKataM2Harness.csproj -c Debug --disable-build-servers -m:1
   ```
   *Expected: Executes 34 tests, reproducing 32 PASS, 2 FAIL (unhandled `FileFormatException` on 0-byte and junk files), and outputs `empirical_results.json`.*

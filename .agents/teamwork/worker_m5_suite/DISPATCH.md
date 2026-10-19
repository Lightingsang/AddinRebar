# DISPATCH — worker_m5_suite

## Task Assignment
You are worker_m5_suite, a Teamwork Worker agent.
Your task is to implement Milestone M5 in `HPDcePro/`: Expand the automated test suite in `tests/HPDcePro.Core.Tests` to at least 80 passing tests (currently 64) and verify all clean code and build invariants.

### 1. Requirements & Test Expansion Scope
1. **Target**: At least 80 passing tests under `Microsoft.Testing.Platform` (aim for 85–90 tests).
2. **New Test Suites / Expansion Areas**:
   - `XDataCodecExtendedTests.cs`:
     - Test corrupted / truncated XData byte buffers and verify graceful fallback/recovery.
     - Test boundary cases on all 13 RegApps registered in `XDataRegistry`.
     - Test `RebarXDataCodec` with invalid integer counts and negative diameters.
   - `AutoStrSerializerExtendedTests.cs`:
     - Test real file read from `C:\Dce Pro\AutoStr\DuAnMau.autostr` (strictly read-only) verifying UTF-8 BOM stripping, project title, floor count, and step configuration parsing.
     - Test malformed JSON recovery (missing keys, empty objects, invalid numbers).
   - `MauHocDatabaseLiveTests.cs`:
     - Test live read from `C:\Dce Pro\MayHoc\mauhoc.db` (strictly read-only).
     - Assert total row count is 45,020.
     - Assert distinct labels contain all 9 expected tokens (`KC1`, `L`, `SL`, `BO`, `SH`, `KC2`, `KC3`, `DK`, `KC4`).
     - Test tokenizer against complex Vietnamese rebar expressions (e.g., `2D20+1D18`, `a200/100`, `c/c 150`).
   - `CuttingOptimizationExtendedTests.cs`:
     - Test bars longer than commercial length 11,700 mm (e.g., 15,000 mm, 24,000 mm) verifying automatic lap splice insertion ($30d..40d$) and resulting segment counts.
     - Test zero and negative bar lengths throwing `ArgumentException`.
     - Test scrap minimization and reusable offcut threshold ($\ge 1500$ mm).
   - `BeamCalculationExtendedTests.cs`:
     - Test high-strength concrete B40 with CB500-V rebar.
     - Test crack width under minimal bending moment ($M < M_{crc}$) and high service moment.
     - Test shear design when web crushing criteria governs vs when minimum stirrup governs.
   - `ExcelExportSmokeTests.cs`:
     - Test `ClosedXmlScheduleExporter` creating an `.xlsx` stream in-memory and verifying worksheet headers, row counts, and formula integrity without COM dependencies.
     - Test `ClosedXmlReportExporter` creating beam calculation reports.

### 2. Clean Code Governance & Build Invariants
- Every test file must satisfy: file $\le 300$ lines (Rule C2), method $\le 50$ lines (Rule M3).
- 0 commented-out code (Rule Q-B1).
- 0 unannotated catch blocks (Rule Q-B2).
- Build command: `dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` must complete with 0 warnings (`TreatWarningsAsErrors=true`) and 0 errors.
- Test command: `dotnet test HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` must show $\ge 80$ tests passing 100%.

### 3. Read-Only Invariant
- `C:\Dce Pro` is strictly READ-ONLY. Never write, create, or delete any file in `C:\Dce Pro`.

### 4. Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A forensic auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Write your completion report and handoff to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_suite\handoff.md`.

## 2026-10-17T17:55:53Z
Sender: 4344af72-0cab-4e88-85dc-bb1beed39052
Priority: MESSAGE_PRIORITY_HIGH
Content:
You are worker_m5_suite, a Teamwork Worker agent.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_suite
Your detailed task assignment is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_suite\DISPATCH.md
The authoritative original request is at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Specification dossier: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\

You MUST read ORIGINAL_REQUEST.md and DISPATCH.md before starting work.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A forensic auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your mission:
Implement Milestone M5 in HPDcePro/:
1. Expand the automated test suite in tests/HPDcePro.Core.Tests (and tests/HPDcePro.Adapters.Tests) so that HPDcePro.slnx runs at least 80 passing tests (currently 77; aim for 85–95 tests).
2. Add comprehensive edge case and boundary tests:
   - XDataCodecExtendedTests.cs: test corrupted/truncated buffers, recovery, negative diameters, all 13 RegApps.
   - AutoStrSerializerExtendedTests.cs: read C:\Dce Pro\AutoStr\DuAnMau.autostr (strictly read-only) verifying UTF-8 BOM stripping, project title, floor count, and step configuration parsing.
   - MauHocDatabaseLiveTests.cs: read C:\Dce Pro\MayHoc\mauhoc.db (strictly read-only), assert 45,020 samples, verify 9 distinct labels, test complex Vietnamese rebar expressions with RebarTextTokenizer.
   - CuttingOptimizationExtendedTests.cs: test continuous bars exceeding 11,700 mm (15m, 24m) verifying automatic lap splice insertion (30d..40d), scrap waste %, and reusable offcut threshold (>= 1500 mm).
   - BeamCalculationExtendedTests.cs: high-strength concrete B40, CB500-V, crack width boundary moments, shear web crushing.
   - ExcelExportSmokeTests.cs: in-memory ClosedXml schedule and report export stream assertions.
3. Verify build: run `dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` (must compile cleanly with 0 errors and 0 warnings).
4. Verify tests: run `dotnet test HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` (must show >= 80 passing tests, 100% green).
5. Strictly obey clean code rules: files <= 300 lines, methods <= 50 lines, 0 commented code, 0 unannotated catch blocks.
6. Strictly treat C:\Dce Pro as read-only.

Write your completion report and handoff to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_suite\handoff.md.
When finished, send a message to your parent orchestrator_20 with a concise summary and path to your handoff report.

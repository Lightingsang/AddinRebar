# DISPATCH — Gate M6: Decoupled Context & Command Runners Challenger

**Assigned Agent**: `challenger_m6_2` (`teamwork_preview_challenger`)  
**Parent Orchestrator**: `orchestrator_16` (Conversation ID: `4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m6_2\`  
**Target Projects**: `HPKata/HPKata.Revit/`, `HPKata/HPKata.Revit.Tests/`  
**Date**: 2026-10-17  

---

## 1. Context & Mission
Milestone M6 (`HPKata.Revit` and `HPKata.Revit.Tests`) has been implemented by `worker_m6`.
Your task is to empirically stress-test the decoupled execution context and pure command runners:
1. Run target unit tests under concurrency isolation:
   ```powershell
   dotnet test HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj --filter "FullyQualifiedName~RevitScheduleRunner|FullyQualifiedName~RevitCuttingStockRunner|FullyQualifiedName~RevitExportExcelRunner|FullyQualifiedName~RevitHostTheme" --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal62-$([guid]::NewGuid())"
   ```
2. Empirically verify `RevitScheduleRunner`:
   - Empty document (0 bars) -> clean schedule with 0 rows and 0 total mass.
   - Mixed bars with partitions -> correct aggregation by `ComponentMark`, diameter grouping, and TCVN mass bins.
   - Selection filtering: `GetSelectedRebars()` vs `GetAllRebars()`.
3. Empirically verify `RevitCuttingStockRunner`:
   - Bars with $\Phi < 10\text{ mm}$ (wire coil) -> excluded from 1D-CSP solver.
   - Bars with $\Phi \ge 10\text{ mm}$ -> solved against 11,700 mm stock bars. Conservation of mass verified: sum of cut pieces + scrap = total stock bar length.
   - Waste metric calculations: Gross scrap and net scrap.
4. Empirically verify `RevitExportExcelRunner`:
   - Headless ClosedXML export to disk.
   - Verify generated spreadsheet contains valid sheets (`Thong_ke`), non-empty cells, correct headers, and diameter summaries.
5. Empirically verify full solution test suite:
   ```powershell
   dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal6all-$([guid]::NewGuid())"
   ```
   Verify 287/287 tests pass 100% green.
6. Record empirical evidence in `challenge_report.md` and deliver explicit verdict in `handoff.md`: `APPROVE` or `REQUEST_CHANGES`.


## 2026-10-17T05:04:39Z
You are challenger_m6_2, the Decoupled Context & Command Runners Empirical Challenger for Milestone M6 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m6_2\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m6_2\DISPATCH.md.
3. Read the master specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md (Sections 7, 8, 9, 10).
4. Read worker handoff report at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6\handoff.md.
5. Read docs/clean-code/HP_CLEAN_CODE_CORE.md.

RESOURCE DIRECTIVE (MANDATORY):
- Worker has already built and verified the solution in Release mode.
- If executing tests, you MUST include `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-chal62-$([guid]::NewGuid())"` to prevent log locking collisions!

YOUR TASK:
Empirically stress-test Decoupled Execution Context and Command Runners:
1. Run: dotnet test HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj --filter "FullyQualifiedName~RevitScheduleRunner|FullyQualifiedName~RevitCuttingStockRunner|FullyQualifiedName~RevitExportExcelRunner|FullyQualifiedName~RevitHostTheme" --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal62-$([guid]::NewGuid())".
2. Empirically verify RevitScheduleRunner against MockRevitDocumentContext:
   - Empty document (0 bars) -> clean schedule, 0 total mass.
   - Partition filtering & diameter summaries.
   - Selected bars vs all bars.
3. Empirically verify RevitCuttingStockRunner:
   - Excludes coil wire < 10 mm.
   - Solves bars >= 10 mm against 11,700 mm stock bars.
   - Conservation of mass: cut lengths + scrap == total stock bar length.
4. Empirically verify RevitExportExcelRunner:
   - Headless ClosedXML export to disk.
   - Verify generated spreadsheet contains valid sheets ("Thong_ke"), valid non-empty cells, and accurate formulas.
5. Run full solution test suite:
   dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal6all-$([guid]::NewGuid())"
   Verify all 287/287 tests pass 100% green.
6. Record empirical evidence in challenge_report.md and deliver handoff.md with explicit verdict: APPROVE or REQUEST_CHANGES.

MANDATORY CONSTRAINTS:
- C:\kata_pro is strictly read-only.
- Never inspect, decompile, or touch Security.dll or licensing code.

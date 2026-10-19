# DISPATCH — Gate M6: Revit Interop, Parameter Bridge & Testing Reviewer

**Assigned Agent**: `reviewer_m6_2` (`teamwork_preview_reviewer`)  
**Parent Orchestrator**: `orchestrator_16` (Conversation ID: `4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m6_2\`  
**Target Projects**: `HPKata/HPKata.Revit/`, `HPKata/HPKata.Revit.Tests/`  
**Date**: 2026-10-17  

---

## 1. Context & Mission
Milestone M6 (`HPKata.Revit` and `HPKata.Revit.Tests`) has been implemented by `worker_m6`.
Your task is to review the parameter bridge, shape adapters, unit quarantine, decoupled test architecture, and headless test suite:
1. Review Unit Quarantine (Rule R5):
   - Inspect `HPKata/HPKata.Revit/Units/RevitUnitConverter.cs`: Encapsulates $1\text{ ft} = 304.8\text{ mm}$ and $1\text{ mm} = 1.0 / 304.8\text{ ft}$.
   - Verify that ZERO literal `304.8` factors exist anywhere outside this file across `HPKata/` production source.
2. Review Parameter Bridge & Shape Adapters:
   - `RevitParameterBridge.cs`: Check parameter mapping (`NUMBER_PARTITION_PARAM` <-> `{ten_ck}`, `REBAR_NUMBER` / `ALL_MODEL_MARK` <-> `{stt}`, `Quantity` <-> `{sl}`, `BarNominalDiameter` <-> `{phi}`).
   - Verify single-bar length `REBAR_ELEM_LENGTH` is mapped to `CuttingLengthMm` (NOT `TotalLength`).
   - `RevitShapeAdapter.cs` & `RevitShapeMappingTable.cs`: Disambiguation of symmetric U-bar (`dang02`) vs asymmetric U-bar (`dang04`) via $|A - C| < 1.0\text{ mm}$.
   - `RevitCurveAnalyzer.cs`: Geometric fallback for Freeform and unmapped shapes.
3. Review Decoupled Context & Command Runners:
   - `IRevitDocumentContext.cs`, `RevitRebarDto.cs`, `RevitScheduleTableDto.cs`, `MockRevitDocumentContext.cs`.
   - `RevitScheduleRunner.cs`, `RevitCuttingStockRunner.cs`, `RevitExportExcelRunner.cs`.
4. Review Test Suite Quality (`HPKata.Revit.Tests`):
   - Inspect test classes across `RevitUnitConverterTests`, `RevitShapeAdapterTests`, `RevitParameterBridgeTests`, `RevitScheduleRunnerTests`, `RevitCuttingStockRunnerTests`, `RevitExportExcelRunnerTests`, `RevitHostThemeTests`.
   - Verify 0 tautological tests (tests must exercise production services, not mock self-verifications).
   - Verify tests run 100% headlessly with 0 dependency on a live `Revit.exe`.
5. Resource bounds:
   - Solution already built. If running tests, use `--no-build --disable-build-servers -m:1`.
   - Concurrency isolation: pass `--results-directory "$env:TEMP\mtp-rev62-$([guid]::NewGuid())"`.
6. Deliver findings in `review.md` and explicit verdict in `handoff.md`: `APPROVE` or `REQUEST_CHANGES`.

## 2026-10-17T05:04:39Z
You are reviewer_m6_2, the Revit Interop, Parameter Bridge & Testing Reviewer for Milestone M6 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m6_2\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m6_2\DISPATCH.md.
3. Read the master specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md (Sections 4, 7, 8, 9, 10).
4. Read worker handoff report at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6\handoff.md.
5. Read docs/clean-code/HP_CLEAN_CODE_CORE.md and docs/clean-code/REVITADDINAI_CLEAN_CODE_STANDARD.md.

RESOURCE DIRECTIVE (MANDATORY):
- Worker has already built and verified the solution in Release mode.
- If executing tests, you MUST include `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-rev62-$([guid]::NewGuid())"` to prevent log locking collisions!

YOUR TASK:
Perform an objective review of Revit interop services, parameter mapping, shape adapters, and test suite:
1. Review Unit Quarantine (Rule R5): Inspect RevitUnitConverter.cs (1 ft = 304.8 mm, 1 mm = 1.0 / 304.8 ft). Verify that ZERO conversion factors exist anywhere outside this file across production code.
2. Review Parameter Bridge: RevitParameterBridge.cs (verify mapping of NUMBER_PARTITION_PARAM, REBAR_NUMBER, Quantity, BarNominalDiameter, and verify single-bar length REBAR_ELEM_LENGTH is mapped to CuttingLengthMm, NOT TotalLength).
3. Review Shape Adapters: RevitShapeAdapter.cs & RevitShapeMappingTable.cs (verify U-bar symmetry disambiguation |A - C| < 1.0 mm) and RevitCurveAnalyzer.cs.
4. Review Decoupled Context & Command Runners: IRevitDocumentContext.cs, RevitRebarDto.cs, MockRevitDocumentContext.cs, RevitScheduleRunner.cs, RevitCuttingStockRunner.cs, RevitExportExcelRunner.cs.
5. Review Test Suite Quality: Inspect HPKata.Revit.Tests (verify 54 unit tests are genuine, non-tautological, and run 100% headlessly).
6. Execute test suite: dotnet test HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-rev62-$([guid]::NewGuid())".
7. Record findings in review.md and deliver handoff.md with explicit verdict: APPROVE or REQUEST_CHANGES.

MANDATORY CONSTRAINTS:
- C:\kata_pro is strictly read-only.
- Never inspect, decompile, or touch Security.dll or licensing code.

Send a message to orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6) when complete.


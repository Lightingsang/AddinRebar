# Task Assignment: Test Writer M5 (Test Suite Expansion to >= 330 Tests)

- Subagent Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_test_writer_m5\`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T00:11:55Z`)
- Project Specification: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\PROJECT.md`
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\HPDcePro.slnx`
- Clean Code Standards: `AGENTS.md`, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/CODE_REVIEW_CHECKLIST.md`

## Objectives
1. Expand automated tests in `HPDcePro.Core.Tests` and `HPDcePro.Adapters.Tests` from the 280 baseline to **at least 330 automated tests passing 100%** (0 failed, 0 skipped):
   - Reference `HPDcePro.Mcp.csproj` from `tests/HPDcePro.Adapters.Tests/HPDcePro.Adapters.Tests.csproj` (if not already referenced).
   - In `HPDcePro.Core.Tests/Drafting/`:
     - Add `SteelFrameDraftingGeneratorTests.cs` (<= 300 lines) testing:
       - `GenerateElevationMiddleFrameBT` (primitives, layers, bounding box)
       - `GenerateElevationMiddleFrame3` and `MiddleFrame3BT`
       - `GenerateElevationMiddleFrame5` and `MiddleFrame5a`
       - `GenerateElevationVDS1`
       - `GeneratePlanPurlinRoof`
       - `GeneratePlanPurlinMonitor`
       - `GenerateDetailAnchorBolt`
       - Custom options scaling and layer overrides
   - In `HPDcePro.Adapters.Tests/`:
     - Add `McpToolCatalogAndExecutionTests.cs` (<= 300 lines) testing:
       - `DceProMcpToolCatalog.Tools` contains exactly 8 tools with valid schemas
       - `DceProMcpToolExecutor.ExecuteAsync` for:
         - `dce_design_beam` (single span, continuous, shop drawing)
         - `dce_design_column_wall` (column PMM, shear wall)
         - `dce_design_foundation_pile` (pile capacity, pile cap, pad footing)
         - `dce_design_slab_stair_lintel` (two-way slab, staircase, lintel)
         - `dce_design_steel_peb` (portal frame, moment connection, base plate, wind)
         - `dce_optimize_rebar_cutting` (cutting optimization, schedule aggregation)
         - `dce_query_mayhoc_rebar` (tokenize text, model info)
         - `dce_run_autostr_pipeline` (load config, export boq)
         - Unknown tool handling and invalid argument handling
     - Add `ExcelUdfParityTests.cs` (<= 300 lines) testing all 12 functions via `CalculationUdfBridge`:
       - `TinhCotNeUonXien_PMM`, `TinhVachCung`, `TinhSanHaiPhuong`, `TinhDaiCoc`, `TinhMongDon`, `SucChiuTaiCoc_MaSat`, `BeRongVetNutDam`
       - `TinhDamThep_TCVN5575`, `TinhCotThep_TCVN5575`, `TinhLienKetMoment_TCVN5575`, `TinhBuLongNeo_TCVN5575`, `TinhTaiTrongGio_TCVN2737`
       - Edge cases ($b \le 0$, zero forces, invalid materials, uplift)
     - Add `SteelCadCommandsTests.cs` (<= 300 lines) testing all 10 command methods in `SteelCadCommands` and `GETFORM` in `AutoStrCommands` (safe headless execution with null active document).
     - Add `MainControlCenterTests.cs` (<= 300 lines) testing `MainControlCenterViewModel` properties, KPI summary counts, navigation commands with `Action<Type>` delegate, and `AutoStrProjectSerializer` loading.
2. Verify:
   - `dotnet test HPDcePro/HPDcePro.slnx -c Debug` passes 100% with **>= 330 total tests** (0 failed, 0 skipped).
   - `dotnet build HPDcePro/HPDcePro.slnx -c Debug` builds with 0 errors and 0 warnings.
   - Clean Code compliance: every test file $\le 300$ lines, methods $\le 50$ lines, 0 commented code, 0 unannotated catch blocks.
3. Write your completion report in `handoff.md` and notify the orchestrator.


## 2026-10-18T00:44:38Z
You are Test Writer M5 for HPDcePro Wave 3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_test_writer_m5\
Read your task assignment at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_test_writer_m5\DISPATCH.md
MANDATORY: Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-18T00:11:55Z).
Read PROJECT.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\PROJECT.md
Read Clean Code rules at: docs/clean-code/HP_CLEAN_CODE_CORE.md and AGENTS.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All test implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your task is to expand automated tests across HPDcePro.Core.Tests and HPDcePro.Adapters.Tests from the 280 baseline to at least 330 automated tests passing 100% (0 failed, 0 skipped):
1. In HPDcePro.Core.Tests: Add tests for SteelFrameDraftingGenerator (all elevations, purlin plans, anchor bolt detail).
2. In HPDcePro.Adapters.Tests: Reference HPDcePro.Mcp.csproj. Add tests for DceProMcpToolCatalog and DceProMcpToolExecutor covering all 8 MCP tools.
3. In HPDcePro.Adapters.Tests: Add tests for all 12 UDFs in RcGeotechUdfBridge and SteelWindUdfBridge through CalculationUdfBridge.
4. In HPDcePro.Adapters.Tests: Add tests for SteelCadCommands (10 commands + GETFORM with null document).
5. In HPDcePro.Adapters.Tests: Add tests for MainControlCenterViewModel and MainControlCenterWindow.
6. Verify: `dotnet test HPDcePro/HPDcePro.slnx -c Debug` passes with >= 330 tests (0 failed, 0 skipped) and `dotnet build HPDcePro/HPDcePro.slnx -c Debug` builds with 0 errors, 0 warnings.
7. Keep all test files <= 300 lines, methods <= 50 lines.

Write your completion report in `handoff.md` and send a message back to the orchestrator.


## 2026-10-18T01:12:59Z
**Context**: M5 Test Execution in HPDcePro.Adapters.Tests
**Content**: The background task running `dotnet test tests/HPDcePro.Adapters.Tests/HPDcePro.Adapters.Tests.csproj` is hanging because `cmds.SteelConnectionDialog()` and `cmds.GETFORM()` invoke `AutoCadUiHelper.ShowModal(win)`, which falls back to `window.ShowDialog()` in headless/non-AutoCAD environments. `ShowDialog()` enters a modal Dispatcher loop and blocks execution indefinitely waiting for user interaction.
In `SteelCadCommandsTests.cs`:
1. Do not call `cmds.SteelConnectionDialog()` or `cmds.GETFORM()` with `ShowDialog()`.
2. Verify command registration via metadata/CustomAttributes (as you already do for `SteelCadCommands_RegistersAllTenSteelCommands` and `AutoStrCommands_RegistersGetformCommand`).
3. For window UI testing, verify instantiation on STA thread without calling `ShowModal`/`ShowDialog` (refer to `WindowInstantiationTests.cs` which tests `new Window()`, checks ViewModel, and tests `ThemeManager.Apply`).
**Action**: Kill or cancel task-192 (or let it timeout), update `SteelCadCommandsTests.cs` to remove the blocking `ShowModal`/`ShowDialog` calls, and run `dotnet test HPDcePro/HPDcePro.slnx -c Debug` to verify all >= 330 tests pass cleanly (0 failed, 0 skipped). Then complete `handoff.md` and report back.

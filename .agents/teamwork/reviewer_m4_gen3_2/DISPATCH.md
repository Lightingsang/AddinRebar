## 2026-10-18T10:35:37Z

MANDATORY DIRECTIVE:
You are STRICTLY FORBIDDEN from executing `dotnet build` or `dotnet test`! Concurrent dotnet execution causes host memory exhaustion. Inspect source code, git diffs, and Worker M4 verified test output statically.

MANDATORY READING BEFORE WORK:
- ORIGINAL_REQUEST.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Master Scope: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25_gen3\PROJECT.md
- Truth Matrix: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md
- Worker M4 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_gen3\handoff.md

MISSION: Milestone M4 Excel Dispatcher & UDF Bridge Review
1. Inspect `HPDcePro/src/HPDcePro.Excel/`:
   - Inspect `IDceExcelActionDispatcher.cs`, `ExcelActionContext.cs`, `ExcelActionResult.cs`, and `DceExcelActionDispatcher.cs`.
   - Inspect all 8 domain handlers in `HPDcePro.Excel/ActionHandlers/`:
     `BeamActionHandler`, `ColumnWallActionHandler`, `SlabActionHandler`, `FoundationActionHandler`,
     `RebarCuttingActionHandler`, `CadBridgeActionHandler`, `EtabsSafeActionHandler`, `ApiSystemActionHandler`.
   - Verify all 73 legacy action keys from `danhba_dce_excel.json` are registered and dispatched to real logic.
2. Inspect `DceUdfFunctions.cs`:
   - Verify TCVN calculation methods (TCVN 5574, 10304, 5575, 2737) and `RegisterAll` formula registration with ClosedXML.
3. Verify Truth Matrix update in `docs/rea-dce-pro/TRUTH_MATRIX.md`:
   - Check that Subsystems 2 and 6 are updated to `HOÀN THÀNH`.
4. Verify Clean Code compliance:
   - Rule C2: <= 300 lines per file.
   - Rule M3: <= 50 lines per method.
   - Rule Q-B1: Zero dead code.
   - Rule Q-B2: All catch blocks have `// Reason: ...`.
5. Review Worker M4's verified test execution results (468 passing tests).
6. Write your report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_gen3_2\handoff.md
   with a clear verdict: APPROVE or REQUEST_CHANGES. Then notify parent via send_message.

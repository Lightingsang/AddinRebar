## 2026-10-18T09:58:23Z
You are worker_m4_gen3.
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_gen3\
Parent Conversation ID: 1e55a9de-0d52-405d-83fa-fa58bb05a293

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A Forensic Auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

MANDATORY DIRECT TEST RUNNER RULE:
NEVER run `dotnet test` under any circumstances (it locks TestResults logs, launches NamedPipeServer, and triggers Windows Unknown Hard Error).
To build and test, ALWAYS execute:
```powershell
dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1
& "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Core.Tests\bin\Debug\net8.0\HPDcePro.Core.Tests.exe"
& "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Adapters.Tests\bin\Debug\net8.0-windows\HPDcePro.Adapters.Tests.exe"
```

MANDATORY READING BEFORE WORK:
- ORIGINAL_REQUEST.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Master Scope: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25_gen3\PROJECT.md
- Master Truth Matrix: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md
- Explorer M4.1 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_gen3_1\handoff.md
- Explorer M4.2 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_gen3_2\handoff.md
- Explorer M4.3 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_gen3_3\handoff.md

EXCLUSIVE WRITE OWNERSHIP:
- You own: `HPDcePro/src/HPDcePro.Revit/**`, `HPDcePro/src/HPDcePro.Excel/**`, `HPDcePro/tests/**`, and your working directory.
- STRICT READ-ONLY INVARIANT: Zero modifications to `C:\Dce Pro\`.

MISSION: Implement Milestone M4 (1:1 Revit & Excel Functional Migration)

TASKS:
1. Implement Revit Structural Generators in `HPDcePro/src/HPDcePro.Revit/Generators/`:
   Decouple pure geometry and execution planning (0 references to Autodesk.Revit.*):
   - `RevitGridGenerator.cs`: Grid axis spacing and alphanumeric naming plans.
   - `RevitFramingGenerator.cs`: Beam span curves, cross-section sizing (mm <-> decimal feet).
   - `RevitColumnGenerator.cs`: Column coordinates, section orientation, top/bottom offsets.
   - `RevitSlabWallGenerator.cs`: Slab boundaries, wall curve segments, thickness assignment.
   - `RevitJoinCutPlanner.cs`: Auto-join priorities (Column > Wall > Beam > Slab), coping, cut vectors.
   - `RevitNamingGenerator.cs`: Sequential framing auto-naming along structural grid lines.
   - `RevitAnalyticalGenerator.cs`: Analytical line segments and member connectivity.
   - `RevitTakeoffGenerator.cs`: Volume, formwork area, and rebar tonnage summaries.

2. Implement Revit Commands & Ribbon Application in `HPDcePro/src/HPDcePro.Revit/`:
   - Implement all 23 structural commands in `HPDcePro/src/HPDcePro.Revit/Commands/`:
     `CommandLogin.cs`, `CommandLogout.cs`, `CommandAddBeam.cs`, `CommandAddColumn.cs`,
     `CommandAddGrid.cs`, `CommandAddSlab.cs`, `CommandAddWall.cs`, `CommandAutoCut.cs`,
     `CommandAutoJoin.cs`, `CommandBatchUnjoin.cs`, `CommandBeamDisallowJoin.cs`,
     `CommandCreateCope.cs`, `CommandJoinByOrder.cs`, `DceBeamBreaks.cs`,
     `CommandDatTenDam.cs`, `CommandDceSetup.cs`, `CommandExportBeamToExcel.cs`,
     `CommandExportToAutocad.cs`, `CommandCreateAnalytical.cs`, `CommandEtabsToRevit.cs`,
     `CommandExportToEtabs.cs`, `CommandXuatKhoiLuong.cs`, `CommandAddRebarBeam.cs`.
     Each command implements `IExternalCommand` with `[Transaction(TransactionMode.Manual)]` and `[Regeneration(RegenerationOption.Manual)]`.
     If `commandData?.Application?.ActiveUIDocument == null`, return `Result.Cancelled` gracefully with a clear diagnostic message.
   - Update `DceProRevitApplication.cs`:
     Set RibbonTab `"DCE PRO"`. Create 4 panels ("Modeling", "Join/Cut", "Reinforcement", "Settings").
     Register push buttons for all commands with tooltips and icons resolved via `AssetPathResolver.ResolveIcon(...)` from `HPDcePro/assets/Icon/IconRevit/` and `IconCad/`.

3. Implement Excel Action Dispatcher & Domain Handlers in `HPDcePro/src/HPDcePro.Excel/`:
   - `IDceExcelActionDispatcher.cs`, `ExcelActionContext.cs`, `ExcelActionResult.cs`.
   - `DceExcelActionDispatcher.cs`: Central router registering and dispatching exactly 73 actions.
   - Domain Handlers in `HPDcePro/src/HPDcePro.Excel/ActionHandlers/`:
     * `BeamActionHandler.cs` (15 beam actions + alias `TinhDam`)
     * `ColumnWallActionHandler.cs` (18 column/wall actions + aliases `TinhCot`, `TinhVach`)
     * `SlabActionHandler.cs` (10 slab actions + alias `TinhSan`)
     * `FoundationActionHandler.cs` (3 foundation actions + aliases `TinhMong`, `TinhDaiCoc`, `TinhMongDon`)
     * `RebarCuttingActionHandler.cs` (3 actions: `GopThepDaiToHop`, `ToHop`, `ToHop2`)
     * `CadBridgeActionHandler.cs` (4 actions: `DrawLine`, `DrawPolyLine`, `DrawREC`, `SelectHandle`)
     * `EtabsSafeActionHandler.cs` (7 actions: `ESStiffnessModifiers`, `ESUniformLoadSetsToSafe`, `EtaSTableToEta`, `PointSafeToPointSafe`, `SafeTableToSafe`, `UniformLoadSets`, `DbimPuFile`)
     * `ApiSystemActionHandler.cs` (13 actions: `APIAUTOCAD`, `APICAD`, `APICSI`, `DceAI`, `DceAdmin`, `DceLanguage`, `DceLogin`, `DceLogout`, `DceUpdate`, `licenseissuance`, `MatTienDceExcel.PhienBan`, `MatTienDceExcel.LietKeChucNang`, `MatTienDceExcel.TrangThaiWorkbook`)
     Bind calculation actions to authentic engines in `HPDcePro.Core.Calculations`.

4. Implement ClosedXML TCVN UDF Bridge:
   - In `HPDcePro/src/HPDcePro.Excel/DceUdfFunctions.cs`:
     Standardize UDF methods for `TCVN 5574:2018`, `TCVN 10304:2025`, `TCVN 5575:2024`, `TCVN 2737:2023`.
     Provide `RegisterAll(IXLWorkbook workbook)` hooking formula evaluation.

5. Implement Automated Tests in `tests/`:
   - In `tests/HPDcePro.Adapters.Tests/Revit/`:
     * `RevitCommandsReflectionTests.cs`: Asserts all 23 commands implement `IExternalCommand`, have required attributes, and ribbon registers buttons.
     * `RevitCommandExecutionContextTests.cs`: Asserts null commandData returns Cancelled and diagnostic message.
     * `RevitGeometryExtractionExtendedTests.cs`: Asserts generator geometry, unit conversions, join orders.
     * `RevitRebarPlacementExtendedTests.cs`: Asserts beam, column, wall, footing rebar mapping, tag idempotency.
   - In `tests/HPDcePro.Adapters.Tests/Excel/`:
     * `ExcelActionDispatcherCatalogTests.cs`: Asserts exactly 73 actions registered, `CanExecute` true, unknown key returns NotFound.
     * `ExcelActionExecutionTests.cs`: Asserts execution across beam, column, slab, foundation, and MatTien read actions on ClosedXML workbooks.
   - In `tests/HPDcePro.Core.Tests/Excel/`:
     * `ClosedXmlUdfEvaluationTests.cs`: Asserts TCVN UDF calculations in ClosedXML workbooks.
   - Ensure total automated tests exceed 460 tests (target: >= 466 tests).

6. Clean Code Governance Compliance:
   - Max 300 lines per .cs file (Rule C2).
   - Max 50 lines per method (Rule M3).
   - 0 dead or commented-out code (Rule Q-B1).
   - All catch blocks have `// Reason: ...` comments (Rule Q-B2).
   - `TreatWarningsAsErrors=true` with 0 warnings, 0 errors.

7. Sequential Build & Test Execution:
   - `dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1`
   - `& "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Core.Tests\bin\Debug\net8.0\HPDcePro.Core.Tests.exe"`
   - `& "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Adapters.Tests\bin\Debug\net8.0-windows\HPDcePro.Adapters.Tests.exe"`
   Verify 100% green tests. Save build and test output logs to your working directory.

8. Update TRUTH_MATRIX.md:
   Update Subsystem 2 (Excel actions) and Subsystem 6 (Revit commands) in `docs/rea-dce-pro/TRUTH_MATRIX.md` marking completed endpoints as `HOÀN THÀNH`.

9. Write your detailed handoff report to:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_gen3\handoff.md`
   and notify the parent orchestrator via send_message.

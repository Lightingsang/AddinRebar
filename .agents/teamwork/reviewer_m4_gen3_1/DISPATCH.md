## 2026-10-18T10:35:37Z
You are reviewer_m4_gen3_1.
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_gen3_1\
Parent Conversation ID: 1e55a9de-0d52-405d-83fa-fa58bb05a293

MANDATORY DIRECTIVE:
You are STRICTLY FORBIDDEN from executing `dotnet build` or `dotnet test`! Concurrent dotnet execution causes host memory exhaustion. Inspect source code, git diffs, and Worker M4 verified test output statically.

MANDATORY READING BEFORE WORK:
- ORIGINAL_REQUEST.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Master Scope: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25_gen3\PROJECT.md
- Truth Matrix: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md
- Worker M4 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_gen3\handoff.md

MISSION: Milestone M4 Revit Commands & Generators Review
1. Inspect the 10 decoupled pure generators in `HPDcePro/src/HPDcePro.Revit/Generators/`:
   `RevitGridGenerator`, `RevitFramingGenerator`, `RevitColumnGenerator`, `RevitSlabWallGenerator`,
   `RevitJoinCutPlanner`, `RevitNamingGenerator`, `RevitAnalyticalGenerator`, `RevitTakeoffGenerator`,
   `WallRebarGenerator`, `SlabRebarGenerator`.
   Verify 0 references to Autodesk.Revit.* namespaces and authentic geometric calculations.
2. Inspect the 23 structural Revit commands in `HPDcePro/src/HPDcePro.Revit/Commands/`:
   `CommandAddBeam`..`CommandAddRebarBeam`, `CommandAutoCut`, `CommandAutoJoin`, `CommandDatTenDam`, etc.
   Verify each implements `IExternalCommand`, has `[Transaction(TransactionMode.Manual)]` and `[Regeneration(RegenerationOption.Manual)]`, and gracefully handles null commandData.
3. Inspect `DceProRevitApplication.cs`:
   Verify RibbonTab "DCE PRO" registration with 4 panels ("Modeling", "Join/Cut", "Reinforcement", "Settings"), push buttons, and icon loading via AssetPathResolver.
4. Verify Clean Code compliance:
   - Rule C2: <= 300 lines per file.
   - Rule M3: <= 50 lines per method.
   - Rule Q-B1: Zero dead or commented-out code.
   - Rule Q-B2: All catch blocks annotated with `// Reason: ...`.
5. Review Worker M4's verified test execution results (468 passing tests).
6. Write your report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_gen3_1\handoff.md
   with a clear verdict: APPROVE or REQUEST_CHANGES. Then notify parent via send_message.

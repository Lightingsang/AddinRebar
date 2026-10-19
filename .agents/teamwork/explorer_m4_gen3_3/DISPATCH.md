## 2026-10-18T09:48:31Z

You are explorer_m4_gen3_3.
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_gen3_3\
Parent Conversation ID: 1e55a9de-0d52-405d-83fa-fa58bb05a293

MANDATORY READING BEFORE WORK:
- ORIGINAL_REQUEST.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Master Scope: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25_gen3\PROJECT.md
- Truth Matrix: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md

MISSION: Milestone M4 Verification & Clean Code Strategy Exploration
1. Examine existing tests in HPDcePro/tests/ (HPDcePro.Core.Tests, HPDcePro.Adapters.Tests):
   - Current test count: 422 tests (295 Core + 127 Adapters).
   - Review how adapter tests decouple CAD / UI / STA dependencies.
2. Design testing strategy for Milestone M4:
   - For `HPDcePro.Revit`: Design unit and headless adapter tests covering all 23 commands (geometry extraction, parameter binding, command execution context).
   - For `HPDcePro.Excel`: Design unit and adapter tests covering action dispatching across all 73 actions and ClosedXML UDF evaluation.
   - Estimate target test count (e.g. at least 40+ new tests, total > 460 tests).
3. Clean Code Governance Requirements:
   - Rule C2: Max 300 lines per .cs file.
   - Rule M3: Max 50 lines per method.
   - Rule Q-B1: Zero dead or commented-out code.
   - Rule Q-B2: Zero unannotated empty catch blocks.
   - TreatWarningsAsErrors=true with 0 warnings, 0 errors.
4. Virtual Memory & Runner Protection:
   - Strictly verify direct execution of compiled test runners:
     `& "...\HPDcePro.Core.Tests\bin\Debug\net8.0\HPDcePro.Core.Tests.exe"`
     `& "...\HPDcePro.Adapters.Tests\bin\Debug\net8.0-windows\HPDcePro.Adapters.Tests.exe"`
   - Banned: `dotnet test`.
5. Write your comprehensive report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_gen3_3\handoff.md
   and notify the parent orchestrator via send_message.

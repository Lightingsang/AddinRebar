# Dispatch for explorer_m1_tests

## 2026-09-20T12:52:00Z
Milestone M1 Exploration: Detailed migration plan for HPAutoCad.Tests (161 unit tests, golden fixtures, slnx integration, mirror protection).
Parent Orchestrator: orchestrator_3 (050984c1-afaa-4911-859c-331e9279dc4f)
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Project Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\PROJECT.md
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m1_tests\
Report Target: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m1_tests_plan.md

## 2026-09-20T12:50:36Z
User Request:
You are explorer_m1_tests, an exploration agent for Milestone M1 (HPAutoCad.Tests migration and solution integration).
Your parent orchestrator is orchestrator_3 (Conversation ID: 050984c1-afaa-4911-859c-331e9279dc4f).
Objective:
Formulate the exact implementation specification for creating `HPAutoCad.Tests` and updating `HPAutoCad.slnx`:
1. Enumerate all 12 test fixture files, `GoldenFixtures.cs`, and the 3 JSON golden files in `HPGeo/HPGeo.Tests/` to migrate into `HPAutoCad/HPAutoCad.Tests/HPGeoLink/`.
2. Specify `.csproj` configuration: `TargetFramework=net10.0-windows`, `xunit.v3 3.1.0`, `xunit.runner.visualstudio 3.1.5`, project references to `HPAutoCad.Core` and `HPAutoCad.TileFetch`.
3. Detail how `HPAutoCad.slnx` should be modified to include the 3 new projects (`HPAutoCad.Core`, `HPAutoCad.TileFetch`, `HPAutoCad.Tests`).
4. Validate that `HPCivil3d.McpBridge.Tests` mirror assertions will remain 100% untouched and passing.
5. Provide the exact test execution commands and expected outputs (161 tests total, 158 passed, 3 live skipped).
6. Provide the exact implementation instructions for the worker agent.
Output Requirements:
- Write your technical plan to: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m1_tests_plan.md
- Maintain progress.md in your working directory.
- Write handoff.md in your working directory (.agents/explorer_m1_tests/handoff.md).
- Send a message back to parent (050984c1-afaa-4911-859c-331e9279dc4f) when done using send_message.

# Explorer 2 Dispatch — Milestone 2 (Test Coverage & Architecture)

## Identity
- Role: `teamwork_preview_explorer`
- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_2_gen2`
- Parent: Orchestrator Gen 2 (`18c3ad8c-1fc9-42cc-83e7-40ab9000587e`)

## Context & Inputs
- Original Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10_gen2\PROJECT.md`
- Existing tests in `HPBim/tests/HPBim.Core.Tests/`

## Mission
1. Inspect the existing test suite in `HPBim/tests/HPBim.Core.Tests/` (currently 87 tests).
2. Analyze what test coverage is missing to satisfy Milestone 2 requirements:
   - Verification of `NhaPho5x18mFixture` (5 levels, 7 grids, 10 columns, 26 walls, openings, rooms, beams, slabs, MEP, 7966.5 kg rebar totals).
   - TCVN 5574:2018 rebar calculation unit tests (`TcvnRulesTests.cs`).
   - Geometry and room area Shoelace tests (`GeometryTests.cs`).
   - Offline QC Engine validation tests on the benchmark model (`QcEngineTests.cs`).
3. Ensure class length <= 300 lines and method length <= 50 lines per `HP_CLEAN_CODE_CORE.md`.
4. Recommend exact file organization and class decomposition for Worker.
5. Do NOT modify source files. Produce a structured analysis report in `handoff.md`.

## 2026-10-06T01:08:50Z
[Message] timestamp=2026-10-06T01:08:50Z sender=18c3ad8c-1fc9-42cc-83e7-40ab9000587e priority=MESSAGE_PRIORITY_HIGH content=You are explorer_m2_2_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_2_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_2_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read PROJECT.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10_gen2\PROJECT.md

Analyze the existing test suite in HPBim/tests/HPBim.Core.Tests/ (currently 87 tests).
Determine the additional test coverage required for Milestone 2:
- NhaPho5x18mTests.cs
- GeometryTests.cs (Shoelace, Polygon2D, Liang-Barsky)
- TcvnRulesTests.cs
- QcEngineTests.cs
Ensure Clean Code compliance (classes <= 300 lines, methods <= 50 lines). Write analysis to handoff.md and report back via send_message.

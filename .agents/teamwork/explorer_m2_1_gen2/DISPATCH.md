# Explorer 1 Dispatch — Milestone 2 (Benchmark Survey)

## Identity
- Role: `teamwork_preview_explorer`
- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_1_gen2`
- Parent: Orchestrator Gen 2 (`18c3ad8c-1fc9-42cc-83e7-40ab9000587e`)

## Context & Inputs
- Original Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10_gen2\PROJECT.md`
- Source code in `HPBim/src/HPBim.Core`
- Reference directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\vn-autocad-skill-main`

## Mission
1. Locate the `nha_pho_5x18m` benchmark dataset inside `vn-autocad-skill-main` or project records.
2. Extract the exact numbers: 5 levels (elevations), 7 grids, 10 columns (220x220), 26 walls, openings, rooms, beams, slabs, MEP fixtures, and rebar total weight (7966.5 kg).
3. Design the implementation specification for `NhaPho5x18mFixture.cs` in `HPBim/tests/HPBim.Core.Tests/Fixtures/NhaPho5x18mFixture.cs`.
4. Recommend the test structure for `NhaPho5x18mTests.cs` to verify 100% of the benchmark criteria.
5. Do NOT modify source files. Produce a structured analysis report in `handoff.md`.


## 2026-10-06T01:08:50Z
You are explorer_m2_1_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_1_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_1_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read PROJECT.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10_gen2\PROJECT.md

Investigate the nha_pho_5x18m benchmark dataset in vn-autocad-skill-main or project docs.
Extract the exact data: 5 levels, 7 grids, 10 columns, 26 walls, openings, rooms, beams, slabs, MEP fixtures, and rebar total weight (7966.5 kg).
Design the implementation specification for NhaPho5x18mFixture.cs and write your analysis to handoff.md. Report back via send_message.

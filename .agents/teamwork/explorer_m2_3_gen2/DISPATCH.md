# Explorer 3 Dispatch — Milestone 2 (Data Porting & Verification Strategy)

## Identity
- Role: `teamwork_preview_explorer`
- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_3_gen2`
- Parent: Orchestrator Gen 2 (`18c3ad8c-1fc9-42cc-83e7-40ab9000587e`)

## Context & Inputs
- Original Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10_gen2\PROJECT.md`
- Reference directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\vn-autocad-skill-main`

## Mission
1. Investigate how `nha_pho_5x18m` benchmark entities map to `HPBim.Core.Models` types (`LevelDef`, `AxisGrid`, `ColumnDef`, `WallDef`, `OpeningDef`, `RoomDef`, `BeamDef`, `SlabDef`, `MepFixtureDef`, `PipeRunDef`, `RebarDef`).
2. Verify coordinate system alignment (X, Y in mm, Z in mm/meters, standard orientation).
3. Verify the rebar calculation formulas that yield the exact 7966.5 kg total weight.
4. Formulate the verification methodology so tests execute under 2 seconds without external dependencies.
5. Do NOT modify source files. Produce a structured analysis report in `handoff.md`.

## 2026-10-06T01:08:50Z
You are explorer_m2_3_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_3_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_3_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read PROJECT.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10_gen2\PROJECT.md

Investigate mapping of nha_pho_5x18m entities to HPBim.Core.Models (LevelDef, AxisGrid, ColumnDef, WallDef, OpeningDef, RoomDef, BeamDef, SlabDef, MepFixtureDef, PipeRunDef, RebarDef).
Verify coordinate alignments, units, and rebar total weight computation (7966.5 kg).
Write your analysis to handoff.md and report back via send_message.

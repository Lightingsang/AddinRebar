# DISPATCH — spec_miner_source_1

Role: Source Codebase Specification Miner
Working Directory: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_source_1

## Objective
Read ORIGINAL_REQUEST.md at `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`.
Deeply inspect and mine the legacy source code in `F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\RebarAddin-master\RebarAddin-master\R02_BeamsRebar`.

## Tasks
1. Inventory all classes, structs, enums, math calculations, geometry representations, and algorithms in `R02_BeamsRebar`.
2. Analyze continuous beam span geometry (spans, supports, cross-sections, offsets, levels).
3. Analyze stirrups generation (uniform vs 3-zone gối-nhịp-gối, spacing, cover, hook shapes).
4. Analyze main longitudinal bars (top & bottom, continuous polylines, 90° anchorage hooks, lap splices).
5. Analyze additional reinforcement bars (top bars over supports with L/3, L/4 rules; bottom bars in midspan; multi-layer offsets).
6. Analyze side/web reinforcement bars (for deep beams h > 700mm) and hanging stirrups / diagonal ties at intersections with secondary beams.
7. Detail UI models, user inputs, and preview canvas math.
8. Identify business logic bugs, edge cases, hardcoded values, and non-Revit-dependent mathematical algorithms that should be migrated to `HPRebar.Core`.

## Output
Write your comprehensive findings to `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_source_1\survey_source_analysis.md` and complete `handoff.md`.
Report completion to orchestrator via send_message.

## 2026-09-07T07:25:27Z
You are spec_miner_source_1.
Your working directory is: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_source_1
First, read your task assignment at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_source_1\DISPATCH.md
Also read the authoritative user request at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md

Investigate the source reference codebase at: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\RebarAddin-master\RebarAddin-master\R02_BeamsRebar
Examine all source files, math calculations, geometric models, reinforcement logic (stirrups, 3-zone, main bars, additional top/bottom bars, side bars, hanging bars/ties), UI parameters, and preview canvas logic.
Write a detailed report to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_source_1\survey_source_analysis.md
Write your handoff report to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_source_1\handoff.md
When finished, notify the orchestrator via send_message.


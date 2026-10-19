# DISPATCH — Survey Agent 2 (DWG Schemas & Shapes)

## Task
Extract exact XData RegApp names, DxfCode field orderings, and block attribute dictionaries used on Rebar entities, Rebar Tags, and Dim leaders. Extract full Block definition inventory from `Iso_kata.dwg`. Extract all 68 rebar shape definitions (`dang00` to `dang67`): shape code, description, parameter letters (A, B, C, D, E...), cutting length calculation formula, hook angles, bend deductions.

## Target Files to Inspect (Read-Only)
- `C:\kata_pro\Iso_kata.dwg`
- `C:\kata_pro\dangthep\dang00.wmf` .. `dang67.wmf`
- `C:\kata_pro\Kata_pro64_Cad2025.dll` (reflection / IL / disassembly / REA)
- `C:\kata_pro\Kata_Class_Lib.dll`
- Mandatory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`

## Output
- `survey_dwg_shapes_report.md` in this directory
- `handoff.md`

## Constraints
- Never touch Security.dll or licensing code.
- C:\kata_pro is strictly read-only.

## 2026-10-16T23:59:09Z
You are survey_agent_2 (teamwork_preview_spec_miner_survey_2), a specification mining agent.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_2\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under ## 2026-10-16T23:55:10Z).
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_2\DISPATCH.md.

YOUR TASK:
Clean-room reverse-engineer and document the DWG Data Schemas and Rebar Shapes of KataPro:
1. Inspect C:\kata_pro\Iso_kata.dwg: block definitions, attribute tags, prompt strings, layers, dimension styles, text styles. (You can inspect using Python dwg libraries, PowerShell, or AutoCAD MCP bridge if running, or REA/managed inspection tools).
2. Inspect C:\kata_pro\Kata_pro64_Cad2025.dll and C:\kata_pro\Kata_Class_Lib.dll for XData RegApp names (e.g. KATA_*, etc.), DxfCode field orderings, and block attribute dictionaries on rebar entities, rebar tags, and dimension leaders.
3. Inspect all 68 rebar shape definitions in C:\kata_pro\dangthep\dang00.wmf .. dang67.wmf and corresponding shape calculation classes in Kata_Class_Lib.dll / Kata_pro64_Cad2025.dll:
   - Shape code (00 to 67)
   - Descriptive name / topology
   - Geometric parameters (A, B, C, D, E, F, G...)
   - Formula for total cutting length L
   - Hook rules (90°, 135°, 180°) and bend deductions
   - Lap splice rules.

STRICT CONSTRAINTS:
- NEVER decompile, copy, or circumvent Security.dll, hardware locks, or licensing checks.
- Treat C:\kata_pro as strictly read-only.
- Write your comprehensive findings to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_2\survey_dwg_shapes_report.md
- Write handoff.md in your working directory.
- When finished, send a message to orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

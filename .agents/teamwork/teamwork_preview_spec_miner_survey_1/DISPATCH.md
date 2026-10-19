# DISPATCH — Survey Agent 1 (Commands & Config)

## Task
Map all >110 commands, their Lisp aliases, their ribbon buttons, and classify them across the 6 subsystems:
(1) Core Rebar & Setup, (2) Beam Shop, (3) Column/Wall Shop, (4) Foundation/Slab/Stair, (5) Rebar Schedule/Coupler/1D Cutting Stock, (6) QS Quantity Takeoff.
Document the complete 86-line configuration schema from `Goi lenh.lsp` lines 152–237.

## Target Files to Inspect (Read-Only)
- `C:\kata_pro\Goi lenh.lsp`
- `C:\kata_pro\Icon\Ribbon\Ribbon_Command_Clicks.txt`
- `C:\kata_pro\Kata_pro64_Cad2025.dll` (reflection / IL / disassembly / REA)
- `C:\kata_pro\Kata_Class_Lib.dll`
- Mandatory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`

## Output
- `survey_commands_config_report.md` in this directory
- `handoff.md`

## Constraints
- Never touch Security.dll or licensing code.
- C:\kata_pro is strictly read-only.


## 2026-10-16T23:59:09Z
You are survey_agent_1 (teamwork_preview_spec_miner_survey_1), a specification mining agent.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_1\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under ## 2026-10-16T23:55:10Z).
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_1\DISPATCH.md.

YOUR TASK:
Clean-room reverse-engineer and document the Command Inventory, Lisp Configuration, and Subsystem Decomposition of KataPro:
1. Inspect C:\kata_pro\Goi lenh.lsp (>110 command aliases, and the 86-line default configuration block at lines 152–237).
2. Inspect C:\kata_pro\Icon\Ribbon\Ribbon_Command_Clicks.txt (full ribbon command clicks inventory).
3. Inspect C:\kata_pro\Kata_pro64_Cad2025.dll and C:\kata_pro\Kata_Class_Lib.dll via reflection / REA CLI (C:\Users\STR-HP03\AppData\Roaming\npm\rea.cmd) / IL inspection to identify command methods ([CommandMethod]), command groups, and how commands map across the 6 subsystems:
   - Subsystem 1: Core Rebar & Setup
   - Subsystem 2: Beam Shop
   - Subsystem 3: Column/Wall Shop
   - Subsystem 4: Foundation/Slab/Stair
   - Subsystem 5: Rebar Schedule, Coupler, 1D Cutting Stock
   - Subsystem 6: QS Quantity Takeoff
4. Extract the exact 86-parameter configuration schema from Goi lenh.lsp (lines 152-237): line index, parameter name, default value, data type, unit, physical meaning, and corresponding class/property in Kata_Class_Lib.dll / Kata_pro64_Cad2025.dll.

STRICT CONSTRAINTS:
- NEVER decompile, copy, or circumvent Security.dll, hardware locks, or licensing checks.
- Treat C:\kata_pro as strictly read-only.
- Write your comprehensive findings to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_1\survey_commands_config_report.md
- Write handoff.md in your working directory.
- When finished, send a message to orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

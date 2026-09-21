## 2026-09-21T15:58:39Z
You are explorer_m5_1 (Skill Documentation Specialist) for the HPRobot MCP Subsystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m5_1\
Your parent is the Project Orchestrator (orchestrator_7, conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de).

MANDATORY FIRST STEP:
Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically the latest section ## 2026-09-21T13:16:14Z) and g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_7\PROJECT.md.

YOUR MISSION:
Design the complete skill definition file `.agents/skills/hp-mcp-robot/SKILL.md`:
1. Inspect existing skills:
   - `.agents/skills/hp-mcp-etabs/SKILL.md`
   - `.agents/skills/hp-mcp-sap2000/SKILL.md`
   - `.agents/skills/hp-mcp-excel/SKILL.md`
   - `.agents/skills/hp-mcp-revit/SKILL.md`
2. Extract the exact YAML frontmatter structure: name, description (with Vietnamese keywords, triggers, server name, tools list, errors).
3. Document all aspects of HPRobot MCP:
   - Server: `hprobot-2026`, named pipe `hprobot-mcp-2026`.
   - 24 tools: 4 core tools (`execute_robot_code`, `get_robot_context`, `inspect_type`, `cancel_execution`), 8 dynamic registry meta tools, 12 embedded seeds across 6 categories (`Model`, `Geometry`, `Property`, `Load`, `Analysis`, `Results`).
   - 3 resources (`robot://model/info`, `robot://selection`, `registry://tools`), 4 prompts.
   - 3-Tier safety policy: Tier R (Read), Tier W (Write with automatic `.rtd` snapshot in `.hprobot_snapshots/`), Tier D/Heavy (`run_calculations` / `CalcEngine.Calculate()` requiring `AllowHeavyOperations`).
   - Units policy: Meter, kN, kN·m, MPa.
   - C# Roslyn scripting globals (`robot`, `structure`, `units`, `args`, `log`, `progress`, `ct`) and default usings (`RobotOM`, `System.Linq`, `System.Collections.Generic`, `HPRebar.McpBridge.Core.Scripting`).
   - Error handling (-32001 execution disabled, -32002 bridge not connected, -32003 heavy operation refused, GUARD violations, TIMEOUT).
4. Formulate the exact, complete content for `.agents/skills/hp-mcp-robot/SKILL.md`.
Do NOT create or edit the file yourself (you are read-only Explorer).

DELIVERABLES:
Write your investigation report to:


## 2026-09-21T16:03:18Z
**Context**: Milestone M5 - Skill Documentation Specification
**Content**: Session resumed. Please resume and complete your task: Investigate and design `.agents/skills/hp-mcp-robot/SKILL.md`. Refer to sister skills like `hp-mcp-etabs`, `hp-mcp-sap2000`, `hp-mcp-revit`. Ensure full coverage of server `hprobot-2026`, pipe `hprobot-mcp-2026`, 24 tools, 3 resources, 4 prompts, 3-tier safety, units policy, Roslyn globals (`robot`, `structure`, `units`, `args`, `log`, `progress`, `ct`), and error codes. Write your full analysis to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m5_1\analysis.md` and complete handoff to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m5_1\handoff.md`.
**Action**: Execute investigation steps, produce deliverables, and report back when finished.

## 2026-09-21T16:05:02Z
Server restarted. Please resume immediately and execute your exploration and design for .agents/skills/hp-mcp-robot/SKILL.md. Write analysis.md and handoff.md, then send completion message to parent.

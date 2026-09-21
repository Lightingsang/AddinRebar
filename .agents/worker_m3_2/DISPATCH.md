## 2026-09-21T14:58:08Z

You are worker_m3_2 (HPRobot M3 Remediation Worker) for the HPRobot MCP Subsystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_2\
Your parent is the Project Orchestrator (orchestrator_7, conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de).

MANDATORY FIRST STEP:
Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically the latest section ## 2026-09-21T13:16:14Z) and g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_7\PROJECT.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

READ INVESTIGATION FINDINGS:
Read the comprehensive findings from all 3 Explorers:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m3_r2_1\handoff.md (Roslyn C# Compilation Fixes)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m3_r2_2\handoff.md (12 Schema-Compliant examples.json Payloads)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m3_r2_3\handoff.md (Test Suite Discovery and 5-Step Verification Checklist)

WRITE OWNERSHIP:
You exclusively own:
`HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/**`

YOUR TASKS:
1. Fix Defect 1 (Roslyn compilation errors in 3 seed scripts):
   - `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Load/get_load_definitions/code.cs`:
     Line 36: replace `comb.CaseComponents.Count` with `comb.CaseFactors.Count`.
   - `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Model/get_model_info/code.cs`:
     Lines 19-25: replace untyped loop with `if (cCol.Get(i) is IRobotCase c) { list.Add(new { number = c.Number, name = c.Name, type = c.Type.ToString(), nature = c.Nature.ToString() }); }`.
   - `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Property/get_materials_and_sections/code.cs`:
     Line 19: replace `data.UnitWeight` with `data.RO`.
     Lines 38-41: replace `(short)IRobotBarSectionDataValueType.I_BSDV_*` with `IRobotBarSectionDataValue.I_BSDV_AX`, `IY`, `IZ`, `IX`.
2. Fix Defect 2 (12 examples.json schema violations):
   - Replace all 12 `examples.json` in `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/**/examples.json` with the exact JSON payloads provided in Explorer 2 handoff (Section 4). Ensure >= 2 distinct examples with "args", matching declared parameters.
3. Build and Test Verification (Execute exactly as per Explorer 3 handoff):
   - Build Debug & Release:
     `dotnet build HPRobot/HPRobot.slnx -c Debug`
     `dotnet build HPRobot/HPRobot.slnx -c Release`
     Verify 0 warnings, 0 errors.
   - Test MCP Stdio Handshake:
     `python -X utf8 McpShared/tools/mcp-call.py HPRobot/HPRobot.Mcp.Server/bin/Debug/net10.0/HPRobot.Mcp.Server.exe tools/list`
     `python -X utf8 McpShared/tools/mcp-call.py HPRobot/HPRobot.Mcp.Server/bin/Debug/net10.0/HPRobot.Mcp.Server.exe resources/list`
     `python -X utf8 McpShared/tools/mcp-call.py HPRobot/HPRobot.Mcp.Server/bin/Debug/net10.0/HPRobot.Mcp.Server.exe prompts/list`
     Verify 24 tools, 3 resources, 4 prompts.
   - Run HPRobot Test Suite:
     `dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj`
     Verify all 197 tests pass (197 succeeded, 0 failed, 0 skipped).
   - Run McpShared Regression Tests:
     `dotnet run --project McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj`
     `dotnet run --project McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj`
     Verify 685 tests pass (0 regressions).
4. Documentation:
   - Report genuine, verbatim terminal outputs for all build and test runs in `handoff.md`.
   - Do NOT omit any test failures or assertions.

DELIVERABLES:
Write your implementation report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_2\changes.md`
And write your handoff report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_2\handoff.md`
When finished, send a message to your parent with summary and file paths.

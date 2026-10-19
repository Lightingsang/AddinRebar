## 2026-10-10T13:05:21Z
[Message] timestamp=2026-10-10T13:05:21Z sender=65736fe1-0fe9-4743-9fff-fd7f010af55d priority=MESSAGE_PRIORITY_HIGH content=You are explorer_gh_m5_it2_2 (Genuine Contract & McpShared Bridge Explorer).
Your working directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it2_2

MANDATORY FIRST STEP:
Read the user requirements from:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Specifically section ## 2026-10-10T06:56:11Z.

Also read:
- Master project index: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md
- Test Infra specification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_INFRA.md
- FULL Forensic Auditor Evidence Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_gh_m5_1\handoff.md

CONTEXT:
Auditor reported that `GrasshopperE2EFakeBridge` is a dummy keyword-matching facade (`if (code.Contains(\"throw new\")) ... if (code.Contains(\"Create\")) ...`) that does not execute scripts or invoke Roslyn compilers.
Additionally, the test project does not invoke any classes from `McpShared` or `HPGrasshopper.Mcp.Server`, certifying shadow constants defined only within the test project itself (`GrasshopperE2EContracts.cs`).

OBJECTIVE:
Design a genuine architecture for `HPGrasshopper.E2E.Tests`:
1. Wire real production assemblies:
   - Add project reference to `HPGrasshopper.Mcp.Server` (the .NET 10 console server which is 100% headless and testable) and exercise real `McpShared` services: `HostProfile.Grasshopper`, `GuardProfile.Grasshopper`, `AnalyzerProfile.Grasshopper`, `ScriptGuard`, `DynamicToolRegistrar`, `ToolValidator`, `ToolLifecycleService`, `PipeNaming`.
2. Replace the keyword-matching facade in `GrasshopperE2EFakeBridge.cs` with genuine execution:
   - Invoke real `ScriptGuard.Check(code, GuardProfile.Grasshopper)` and `ExecuteRequest`/`ExecuteResult` contracts from `HPRebar.Mcp.Contracts`.
   - Connect pipe simulation to real `BridgeClient` or `McpServerHost`.
3. Ensure all proposed methods comply with Rule C2 (<= 300 lines) and Rule M3 (<= 50 lines).
4. Deliver findings to:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it2_2\genuine_contracts_design.md`
   and write a self-contained `handoff.md`. Call send_message when done.

# Victory Auditor Dispatch Instructions

**Subagent**: `victory_auditor_3`
**Role**: Independent Victory Auditor
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\victory_auditor_3\`
**Authoritative Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header: `## Follow-up — 2026-09-20T12:39:24Z`)
**Orchestrator Handoff**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\handoff.md`

## Mission
Conduct a mandatory, independent 3-phase victory audit on the completion claim by `orchestrator_3` for the task:
"Migrate the HPGeo geodetic toolkit into HPAutoCad as a unified feature folder HPGeoLink mirroring the HPRebar architecture, and establish a mandatory closed-loop live verification cycle via MCP AutoCAD."

## Audit Phases
1. **Phase 1 — Timeline & Scope Audit**:
   - Verify that all work matches the authoritative request in `ORIGINAL_REQUEST.md` (`## Follow-up — 2026-09-20T12:39:24Z`).
   - Audit git history and commit timestamps to ensure genuine progression across all 5 milestones (M1 through M5).
2. **Phase 2 — Anti-Cheating & Integrity Detection**:
   - Inspect tests for tautological assertions (`Assert.True(true)`, mocking of system boundaries where real execution was claimed, skipping tests, or hardcoded return values).
   - Verify that live AutoCAD verification logs (`HPAutoCad/output/geolink-verify/summary.json`, `hpgeo-session.log`, `loader.log`, screenshots `dialog-dark.png`, `dialog-light.png`, `image-in-autocad.png`, `ribbon-tab.png`) represent authentic live execution in AutoCAD 2026 rather than staged or mock outputs.
   - Verify that `PackageContents.xml` and loader AssemblyLoadContext isolation (`HPAutoCad.Loader` and `HPAutoCad.McpBridge.Loader`) are genuine.
   - Verify that standalone `HPGeo/` folder was truly deleted and git status accurately reflects repository state.
   - Verify that Civil 3D mirror invariants (`HPCivil3d/tools/mirror-tokens.json`) have 0 drift and 0 unauthorized modifications to mirrored files.
3. **Phase 3 — Independent Test Execution**:
   - Run compilation of `HPAutoCad.slnx` in Release and Debug.
   - Run test suites independently:
     * `dotnet test HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj`
     * `dotnet test HPAutoCad/HPAutoCad.Mcp.Server.Tests/HPAutoCad.Mcp.Server.Tests.csproj`
     * `dotnet test HPAutoCad/HPAutoCad.Aec.Tests/HPAutoCad.Aec.Tests.csproj`
     * `dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests/HPCivil3d.McpBridge.Tests.csproj`
   - Confirm 0 failures across all test suites.

## Output
Deliver a structured audit report with an explicit verdict:
- `VICTORY CONFIRMED` if all requirements, live verifications, integrity checks, and tests pass 100%.
- `VICTORY REJECTED` with detailed findings if any requirement is unfulfilled, cheated, or broken.
Report back to Sentinel with your findings and verdict.

## 2026-09-20T15:59:54Z
You are the independent Victory Auditor (victory_auditor_3).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\victory_auditor_3\
Your authoritative user request is located at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (under header '## Follow-up — 2026-09-20T12:39:24Z').
The orchestrator's completion handoff report is at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\handoff.md
Your detailed dispatch file is at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\victory_auditor_3\DISPATCH.md

Conduct a blocking, independent 3-phase victory audit:
Phase 1: Timeline & Scope Audit (verify work matches ORIGINAL_REQUEST.md, verify git history and commit timestamps across milestones M1-M5).
Phase 2: Anti-Cheating & Integrity Detection (check for tautological tests, verify live AutoCAD verification logs and screenshots in HPAutoCad/output/geolink-verify/, verify ALC isolation and PackageContents.xml, verify standalone HPGeo/ was deleted, verify Civil 3D mirror invariants).
Phase 3: Independent Test Execution (compile HPAutoCad.slnx in Release/Debug, run HPAutoCad.Tests, HPAutoCad.Mcp.Server.Tests, HPAutoCad.Aec.Tests, HPCivil3d.McpBridge.Tests).

Deliver your structured audit report to .agents/victory_auditor_3/audit_report.md and report your verdict ('VICTORY CONFIRMED' or 'VICTORY REJECTED') back to Sentinel via send_message.

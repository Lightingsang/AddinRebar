# BRIEFING — 2026-10-08T09:08:40Z

## Mission
Investigate and formulate the exact setup for HPDynamo/HPDynamo.slnx, Directory.Build.props, and global.json for Dynamo 3.7 / Revit 2026.

## 🔒 My Identity
- Archetype: explorer
- Roles: investigation, synthesis
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_1
- Original parent: b20bb4a5-dd75-4c48-9f4d-621e02412393
- Milestone: M2.1

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Analyze exact setup for HPDynamo/HPDynamo.slnx, Directory.Build.props, and global.json
- Reference McpShared/ projects cleanly (HPRebar.Mcp.Contracts, HPRebar.McpBridge.Core, HPRebar.Mcp.Server.Core)
- Resolve Revit 2026 Dynamo paths (C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\) and Dynamo Core 3.7 assemblies
- Target frameworks: net10.0 for HPDynamo.Mcp.Server, net8.0-windows7.0 for HPDynamo.McpBridge
- Ensure clean compilation without circular dependencies
- Output line-by-line implementation guidance to handoff.md

## Current Parent
- Conversation ID: b20bb4a5-dd75-4c48-9f4d-621e02412393
- Updated: not yet

## Investigation State
- **Explored paths**:
  - `HPDynamo/HPDynamo.E2E.Tests/` (already present, builds cleanly in net10.0, 84/84 green tests)
  - `McpShared/` (M1 Dynamo contracts, PipeNaming.DynamoHost, JsonRpcMethods.DynamoPrefix, HostProfile.Dynamo all verified)
  - `HPCivil3d/`, `HPNavis/`, `HPEtabs/`, `HPRobot/`, `HPTekla/` (.slnx, Directory.Build.props, global.json patterns)
  - `C:\Program Files\Autodesk\Revit 2026\` and `AddIns\DynamoForRevit\` (12/12 assemblies verified to exist)
  - Registry `HKLM\SOFTWARE\Autodesk\Revit\2026\REVIT-05:0409` (InstallationLocation verified)
- **Key findings**:
  - Verified presence and paths of all 12 Revit 2026 and Dynamo 3.7 assemblies
  - Formulated exact XML `.slnx` format referencing all 5 projects and 3 McpShared projects
  - Formulated exact `Directory.Build.props` with registry detection, environment overrides, McpShared paths, and API availability flags
  - Formulated exact `global.json` pinning SDK 10.0.300 and Microsoft.Testing.Platform
  - Verified zero circular dependencies and clean dependency DAG: HPDynamo projects -> McpShared
- **Unexplored areas**: None. All requirements fully explored and verified against live filesystem and compiler.

## Key Decisions Made
- Formulate XML `.slnx` solution including all 5 projects (`HPDynamo.Mcp.Server`, `HPDynamo.McpBridge`, `HPDynamo.Mcp.Server.Tests`, `HPDynamo.McpBridge.Tests`, `HPDynamo.E2E.Tests`) plus `/Shared/` folder.
- Formulate `Directory.Build.props` with 4-tier resolution hierarchy for both Revit 2026 and Dynamo 3.7, explicit McpShared paths, and availability booleans.
- Detail minimal project stubs so `dotnet build HPDynamo/HPDynamo.slnx` succeeds immediately upon initialization without missing project errors.

## Artifact Index
- `BRIEFING.md` — Situational awareness
- `progress.md` — Liveness heartbeat and milestone checklist
- `DISPATCH.md` — Received dispatch instructions
- `handoff.md` — 5-component handoff report with exact line-by-line implementation guidance

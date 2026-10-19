# BRIEFING — 2026-10-08T11:46:00Z

## Mission
Design complete test architecture for HPDynamo.Mcp.Server.Tests based on sibling MCP server test projects.

## 🔒 My Identity
- Archetype: explorer
- Roles: teamwork_preview_explorer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_1
- Original parent: b20bb4a5-dd75-4c48-9f4d-621e02412393
- Milestone: M5 (Test Suites & Server Verification)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement production/test code directly in repository.
- Write reports and analysis to working directory only.
- Test framework: net10.0, xUnit v3, Microsoft.Testing.Platform.
- References: Only HPDynamo.Mcp.Server and McpShared projects.
- Clean Code rules: Files <= 300 lines (C2), Methods <= 50 lines (M3), 0 empty catch without reason (Q-B2), no commented code (Q-B1).

## Current Parent
- Conversation ID: b20bb4a5-dd75-4c48-9f4d-621e02412393
- Updated: not yet

## Investigation State
- **Explored paths**:
  - Sibling test projects: `HPNavis.Mcp.Server.Tests`, `HPEtabs.Mcp.Server.Tests`, `HPAutoCad.Mcp.Server.Tests`, `HPCivil3d.Mcp.Server.Tests`, `HPExcel.Mcp.Server.Tests`, `HPRebar.Mcp.Server.Tests`.
  - Engine tests and fakes: `McpShared/HPRebar.Mcp.Server.Core.Tests/Fakes/FakeRevitExecutor.cs`, `HPRebar.McpBridge.Core/Scripting/ScriptQuality.cs`, `GuardProfile.cs`, `AnalyzerProfile.cs`.
  - HPDynamo codebase: `HPDynamo/HPDynamo.Mcp.Server/` (Program.cs, DynamoHostProfile.cs, Tools, SeedLibrary), `HPDynamo/Directory.Build.props`, `HPDynamo.slnx`, `HPDynamo.E2E.Tests/`.
  - M4 audit verification: `scripts/m4_challenge/Program.cs` (verified 12 seeds pass AST and Guard).
- **Key findings**:
  - HPDynamo.Mcp.Server.Tests target framework must be `net10.0` with `UseMicrosoftTestingPlatformRunner = true` and `xunit.v3`.
  - Project references must strictly be `HPDynamo.Mcp.Server.csproj` and `McpShared` projects (`HPRebar.Mcp.Server.Core`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Contracts`), linking `FakeRevitExecutor.cs`.
  - 24-tool surface invariant is preserved via `DynamoHostProfile.EnforceToolSurface`, which suppresses `inspect_type` because `execute_designscript` is the 4th core tool.
  - Seed library contains 36 embedded manifest resources; seed classes must not be compiled into DLL (`Compile Remove="Registry\SeedLibrary\**\*.cs"`).
  - All 12 seeds pass `ScriptQuality.Find` with 0 errors and 0 warnings (clean baseline `[]`).
  - `ScriptGuard.Check` with `GuardProfile.Dynamo` produces 0 violations on all 12 seeds.
  - Registry lifecycle requires `DynamoRegistryFixture` with temp DB/library and `FakeRevitExecutor`.
- **Unexplored areas**: None for server test suite architecture.

## Key Decisions Made
- Partitioned `HPDynamo.Mcp.Server.Tests` into 8 modular files across 4 namespaces/folders (`Surface/`, `Seeds/`, `Registry/`, `Protocol/`) to ensure strict Clean Code compliance (Rule C2: <= 300 lines per file, Rule M3: <= 50 lines per method).
- Formulated complete test architecture blueprint in `handoff.md`.

## Artifact Index
- handoff.md — Comprehensive test architecture blueprint for HPDynamo.Mcp.Server.Tests
- BRIEFING.md — Situational awareness working memory
- DISPATCH.md — Task assignment and dispatch log


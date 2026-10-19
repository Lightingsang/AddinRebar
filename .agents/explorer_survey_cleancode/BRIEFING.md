# BRIEFING — 2026-10-05T23:36:00Z

## Mission
Thorough technical survey of Clean Code governance, solution architecture, and test infrastructure for HPBim deliverable.

## 🔒 My Identity
- Archetype: teamwork_preview_explorer
- Roles: read-only investigation, survey, synthesis
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_cleancode
- Original parent: f21aade0-effc-4aa5-a071-ae17d49dabe3
- Milestone: HPBim Technical Survey & Architecture Design

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- 100% C# .NET for HPBim (no Python runtime)
- HPBim/ MUST ONLY reference McpShared/ (zero cross-references to other MCP folders)
- HPBim.Core target netstandard2.0 with ZERO references to Autodesk.Revit.*
- Strict Clean Code Governance compliance: Methods <= 50 lines, Classes <= 300 lines, no empty catch without comment, no dead code, 3 blocking runtime rules Q-B1..Q-B3
- Global SDK: .NET 10.0.300, Microsoft.Testing.Platform runner, xUnit v3

## Current Parent
- Conversation ID: f21aade0-effc-4aa5-a071-ae17d49dabe3
- Updated: 2026-10-05T23:27:52Z

## Investigation State
- **Explored paths**:
  - `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `host-appendix/revit.md`, `ARCHITECTURE.md`
  - Existing solutions (`HPEtabs.slnx`, `HPExcel.slnx`, `HPRobot.slnx`, `McpShared.slnx`, `HPAutoCad.slnx`)
  - `global.json`, `Directory.Build.props`, project files across repo
  - `McpShared/HPRebar.Mcp.Contracts`, `RevitBridgeClient.cs`, `RegistryCli.cs`
  - `vn-autocad-skill-main` (`layout.py`, `model.py`, `standards.py`, `qc.py`, `estimate.py`, `agents/*.md`)
- **Key findings**:
  - Full blueprint synthesized for `HPBim.slnx` with 3 production projects (`HPBim.Core`, `HPBim.Client`, `HPBim.Cli`) and 2 test projects (`HPBim.Core.Tests`, `HPBim.Client.Tests`).
  - Target frameworks: `HPBim.Core` (`netstandard2.0` with `Polyfill 11.0.1`), `HPBim.Client` (`net8.0;net10.0`), `HPBim.Cli` (`net10.0`).
  - Strict enforcement of Clean Code: Methods <= 50 lines, Classes <= 300 lines, blocking rules Q-B1..Q-B3, `TreatWarningsAsErrors=true`.
  - Zero Revit references in Core; Revit interaction isolated to Client over pipe `\\.\pipe\hprebar-mcp-r2026`.
- **Unexplored areas**: None for survey scope. Implementation phase will be carried out by downstream agents.

## Key Decisions Made
- Solution blueprint designed in full compliance with repository standards. Comprehensive survey report written to `handoff.md`.

## Artifact Index
- `DISPATCH.md` — Inbound message log
- `BRIEFING.md` — Persistent working memory
- `progress.md` — Liveness heartbeat
- `handoff.md` — Comprehensive survey report and architectural blueprint

# BRIEFING — 2026-10-18T01:21:00Z

## Mission
Empirically challenge and stress-test the 8 MCP tools in HPDcePro.Mcp and all 12 Excel UDFs in CalculationUdfBridge under extreme boundary inputs, singularities, and malformed requests.

## 🔒 My Identity
- Archetype: challenger
- Roles: critic, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_1\
- Original parent: d6327505-50dd-47d2-9377-859bd91f34c5
- Milestone: M5 / Wave 3 Challenge
- Instance: 1 of 2

## 🔒 Key Constraints
- Review-only — do NOT modify implementation code
- Report any failures as findings — do NOT fix them yourself
- Empirical proof required: must write and execute tests / stress harnesses
- .agents/teamwork/ holds only agent metadata — NEVER place source code, tests, or data files here

## Current Parent
- Conversation ID: d6327505-50dd-47d2-9377-859bd91f34c5
- Updated: not yet

## Review Scope
- **Files to review**:
  - `HPDcePro/src/HPDcePro.Mcp/**` (Catalog, Execution, Handlers, Schemas, Server)
  - `HPDcePro/src/HPDcePro.Excel/**` (`CalculationUdfBridge.cs`, `RcGeotechUdfBridge.cs`, `SteelWindUdfBridge.cs`)
  - Target solution: `HPDcePro/HPDcePro.slnx`
- **Interface contracts**: `PROJECT.md`, `ORIGINAL_REQUEST.md` (entry dated 2026-10-18T00:11:55Z)
- **Review criteria**: Robustness against extreme boundary inputs, physical singularities, soil uplift, malformed JSON arguments, unknown operation names, zero unhandled crashes.

## Key Decisions Made
- Use compiled test projects or temporary test files in `tests/` or standalone runner to execute empirical stress tests directly against compiled assemblies.

## Artifact Index
- DISPATCH.md — task assignment
- BRIEFING.md — situational awareness
- progress.md — liveness heartbeat
- handoff.md — final challenge verdict and findings

## Attack Surface
- **Hypotheses tested**:
  - MCP tools crash on negative/zero dimensions or enormous loads (10^7 kN).
  - MCP executor throws unhandled exceptions on null/empty tool calls or invalid operation names.
  - Excel UDFs throw divide-by-zero or NaN/Infinity on soil uplift or zero thickness/depth.
- **Vulnerabilities found**: TBD
- **Untested angles**: Concurrency under rapid parallel dispatch, malformed nested JSON.

## Loaded Skills
- Source: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\skills\hp-dce-pro\SKILL.md
- Local copy: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_1\hp-dce-pro-SKILL.md
- Core methodology: TCVN structural calculation and rebar detailing via HPDcePro MCP server and Excel UDFs.

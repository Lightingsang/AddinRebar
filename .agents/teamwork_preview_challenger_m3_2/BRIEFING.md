# BRIEFING — 2026-09-22T01:33:00+07:00

## Mission
Adversarially challenge the 12 embedded seed tools of HPTekla.Mcp.Server for schema validity, example consistency, AST guard compliance, and cross-host contamination.

## 🔒 My Identity
- Archetype: challenger
- Roles: critic, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m3_2
- Original parent: 5d7560ee-5142-428f-a172-e73cf7738ac1
- Milestone: Milestone 3 (HPTekla.Mcp.Server)
- Instance: 2 of 2 (Challenger 2)

## 🔒 Key Constraints
- Review-only — do NOT modify implementation code
- Write only to your folder (`.agents/teamwork_preview_challenger_m3_2/`); read any folder
- Never place source code, tests, or data files in `.agents/`
- Deliver unambiguous verdict: APPROVE or REQUEST_CHANGES
- Send message to parent agent (`5d7560ee-5142-428f-a172-e73cf7738ac1`)

## Current Parent
- Conversation ID: 5d7560ee-5142-428f-a172-e73cf7738ac1
- Updated: 2026-09-22T01:33:00+07:00

## Review Scope
- **Files to review**: `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/**` (all 12 seeds: tool.json, code.cs, examples.json)
- **Interface contracts**: `McpShared/HPRebar.Mcp.Contracts/`, `GuardProfile.Tekla` in `HPRebar.McpBridge.Core`
- **Review criteria**: Schema validity (draft-07), examples validity against inputSchema, AST guard compliance (Roslyn syntax, return statements, forbidden guards: MessageBox, Forms, Process, CommitChanges, #r, #load), cross-host contamination, parameter extraction consistency

## Attack Surface
- **Hypotheses tested**:
  1. JSON Schema draft-07 validity and ToolValidator compliance across all 12 seeds (PASS: 12/12).
  2. InputSchema vs examples payload conformance (PASS: 25/25 examples valid).
  3. AST Guard & ScriptGuard compliance for GuardProfile.Tekla (PASS: 0 violations, 0 directives, 0 CommitChanges).
  4. Cross-host contamination across all seed files (PASS: 0 occurrences).
  5. Parameter extraction consistency between code.cs and tool.json (PASS: 0 undeclared reads).
  6. Roslyn script compilation under .NET Framework 4.8 / Tekla Structures 2025 API (FAIL: 5/12 failed compilation).
- **Vulnerabilities found**:
  1. [CRITICAL] `Math.Clamp` used in 3 seeds (`list_drawings`, `select_objects`, `get_reinforcement_info`) does not exist on .NET Framework 4.8 (CS0117).
  2. [CRITICAL] `ModelObject.ModelObjectEnum.REBAR` used in 2 seeds (`select_objects`, `get_reinforcement_info`) does not exist in Tekla Open API (CS0117).
  3. [CRITICAL] `ProjectInfo.ProjectName` used in `get_model_info` does not exist; actual property is `proj.Name` (CS1061).
  4. [CRITICAL] `Reinforcement.Size` used in `get_reinforcement_info` does not exist on base class `Reinforcement` (CS1061).
  5. [CRITICAL] `export_ifc` uses invalid enums and struct field for `Operation.CreateIFC4ExportFromSelected` (`IFC2X3_COORDINATION_VIEW`, `IFC4_DESIGN_TRANSFER_VIEW`, `BasePointCurrentWorkPlane`, `IFCExportFlags.None`).
- **Untested angles**:
  - Live execution in running TeklaStructures.exe instance (assigned to Live Harness / M2/M4).

## Loaded Skills
- Source: bs:code-review
  Local copy: None
  Core methodology: Adversarial red-team review of code quality, security, and contracts
- Source: bs:test
  Local copy: None
  Core methodology: Empirical test execution and verification harness

## Key Decisions Made
- Executed empirical Roslyn & schema test harness in .NET 10 and .NET Framework 4.8 against Tekla 2025 assemblies.
- Discovered 5 fatal compilation errors in 5 seed scripts when targeting Tekla Structures 2025 / net48.
- Rendered verdict: REQUEST_CHANGES.

## Artifact Index
- report.md — Comprehensive empirical challenge report and compilation stress test results
- handoff.md — 5-component handoff report with explicit REQUEST_CHANGES verdict


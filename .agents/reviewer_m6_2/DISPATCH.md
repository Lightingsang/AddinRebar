# Dispatch Assignment — Reviewer M6-2 (reviewer_m6_2)

## Context
You are Reviewer 2 (reviewer_m6_2) for Milestone M6 (Final Verification & E2E Track) in the HPExcel MCP Ecosystem project.

## Working Directory
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m6_2\`

## Documentation to Review
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (§ 2026-09-21T09:44:29Z)
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_6\PROJECT.md`
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\AGENTS.md`
4. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m6_1\handoff.md`

## Review Objectives
1. Architectural Isolation Check:
   - Ensure `HPExcel/` projects reference ONLY `../McpShared/` and NEVER sibling host deliverables (`HPRebar/`, `HPAutoCad/`, `HPNavis/`, `HPEtabs/`, `HPCivil3d/`, `HPSap2000/`, `HPPowerBi/`).
2. WPF Bridge Structure & MaterialDesign Check:
   - Check `HPExcel.McpBridge` WPF structure, MaterialDesignThemes 5.3.2 integration, Excel green branding (`#107C41` / `#21A366`).
   - STA worker thread safety, `IOleMessageFilter` implementation, ClosedXML headless integration, 3-tier safety engine.
3. Skill & Repository Registration Check:
   - Check `.agents/skills/hp-mcp-excel/SKILL.md` and `.claude/skills/hp-mcp-excel/SKILL.md`.
   - Check `AGENTS.md` repository deliverables table and architecture section.
4. Issue verdict: `APPROVE` or `REQUEST_CHANGES`.
5. Write full review report to `.agents/reviewer_m6_2/handoff.md` and send message with verdict.

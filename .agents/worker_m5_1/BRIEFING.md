# BRIEFING — 2026-09-21T11:37:00Z

## Mission
Create hp-mcp-excel skill documentation and update AGENTS.md for HPExcel MCP Ecosystem (Milestone M5).

## 🔒 My Identity
- Archetype: implementer
- Roles: implementer, qa, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m5_1
- Original parent: a6affb02-3586-4014-be6f-de9dfcd816bd
- Milestone: M5 (Skill & Repository Documentation)

## 🔒 Key Constraints
- All implementations must be genuine. No hardcoding test results, dummy implementations, or shortcuts.
- .agents/ holds only agent metadata and skill definitions.
- Update AGENTS.md with HPExcel/ in Repository Layout table and a dedicated ## HPExcel — Current State & Architecture section.
- Create .agents/skills/hp-mcp-excel/SKILL.md with comprehensive YAML frontmatter, 12 seed tools, core tools, 3-tier safety, ClosedXML/COM guides, troubleshooting.
- Follow minimal change principle when updating AGENTS.md.
- Verify build passes with 0 errors.

## Current Parent
- Conversation ID: a6affb02-3586-4014-be6f-de9dfcd816bd
- Updated: not yet

## Task Summary
- **What to build**: `.agents/skills/hp-mcp-excel/SKILL.md` and update `AGENTS.md`
- **Success criteria**: Skill file created with full catalog and guides, AGENTS.md updated, `dotnet build HPExcel/HPExcel.slnx -c Debug` succeeds with 0 errors.
- **Interface contracts**: McpShared contracts, HPExcel tool schemas.
- **Code layout**: AGENTS.md layout rules.

## Key Decisions Made
- Documented HPExcel ecosystem following established conventions from hp-mcp-etabs and hp-mcp-powerbi.
- Added HPExcel deliverable row to repository layout and created dedicated `## HPExcel — Current State & Architecture` section in `AGENTS.md`.
- Documented complete 12 seed tools catalog across 7 categories (Data, Workbook, Format, Chart, Calculation, Export, Automation) with full parameter schemas and examples.
- Documented 3-tier safety engine (ReadOnly, Write, Destructive) and automatic pre-mutation snapshot backup in `.hpexcel_snapshots/` with 20-file pruning retention.
- Documented hybrid execution modes: live COM automation via STA worker and message filter vs headless OpenXML processing via ClosedXML.

## Artifact Index
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\skills\hp-mcp-excel\SKILL.md` — Complete skill instructions
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.claude\skills\hp-mcp-excel\SKILL.md` — Mirrored skill instructions
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\AGENTS.md` — Repository layout and architecture section update

## Change Tracker
- **Files modified**:
  - `AGENTS.md`: Added HPExcel to repository deliverables table and added `## HPExcel — Current State & Architecture` section.
  - `.agents/skills/hp-mcp-excel/SKILL.md`: Created complete skill instructions.
  - `.claude/skills/hp-mcp-excel/SKILL.md`: Mirrored skill file for Claude Code compatibility.
- **Build status**: pass (0 errors, 0 warnings; 110 bridge tests + 90 server tests pass)
- **Pending issues**: none

## Quality Status
- **Build/test result**: pass (HPExcel.slnx builds clean; tests 200/200 pass)
- **Lint status**: 0 violations
- **Tests added/modified**: n/a (documentation task)

## Loaded Skills
- **Source**: `.agents/skills/hp-mcp-etabs/SKILL.md`
- **Local copy**: `.agents/worker_m5_1/skills/hp-mcp-etabs.md`
- **Core methodology**: Pattern for out-of-process COM desktop bridge MCP skill doc.
- **Source**: `.agents/skills/hp-mcp-powerbi/SKILL.md`
- **Local copy**: `.agents/worker_m5_1/skills/hp-mcp-powerbi.md`
- **Core methodology**: Pattern for standalone WPF bridge + stdio server MCP skill doc.

# BRIEFING — 2026-10-17T00:00:00Z

## Mission
Specification mining and clean-room reverse engineering of KataPro command inventory, Lisp configuration, and 6-subsystem decomposition.

## 🔒 My Identity
- Archetype: specification_miner
- Roles: Specification Miner, Domain Reverse Engineer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_1
- Original parent: 4300a9ce-e3eb-419c-8493-f2001d2d04d6
- Milestone: KataPro Reverse Engineering - Survey Track (Commands & Config)

## 🔒 Key Constraints
- NEVER decompile, copy, or circumvent Security.dll, hardware locks, or licensing checks.
- Treat C:\kata_pro as strictly read-only.
- Prioritize authoritative sources (DLL reflection, Goi lenh.lsp, Ribbon_Command_Clicks.txt) over LLM prior knowledge.
- Do NOT implement anything — read-only specification mining.
- Output comprehensive findings in `survey_commands_config_report.md` and `handoff.md`.

## Current Parent
- Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6
- Updated: 2026-10-16T23:59:09Z

## Task Summary
- **What to build**: Full survey report of KataPro commands (>110 aliases), ribbon clicks, managed [CommandMethod]s, 6 subsystems, and exact 86-parameter Lisp config schema.
- **Success criteria**: Exhaustive mapping of all commands, exact classification across the 6 subsystems, complete 86-parameter schema table with physical units & property mappings.
- **Interface contracts**: `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`, `survey_commands_config_report.md`
- **Code layout**: Report in `.agents/teamwork/teamwork_preview_spec_miner_survey_1/`

## Key Decisions Made
- Use PowerShell and .NET reflection / IL inspection to probe `Kata_pro64_Cad2025.dll` and `Kata_Class_Lib.dll` safely without touching `Security.dll`.

## Artifact Index
- `survey_commands_config_report.md` — Detailed survey report
- `handoff.md` — Self-contained 5-component handoff report

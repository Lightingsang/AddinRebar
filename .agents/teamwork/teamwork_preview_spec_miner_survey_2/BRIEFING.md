# BRIEFING — 2026-10-17T07:22:00Z

## Mission
Clean-room reverse-engineer and document the DWG Data Schemas and all 68 Rebar Shapes of KataPro from Iso_kata.dwg, Kata_pro64_Cad2025.dll, Kata_Class_Lib.dll, and dangthep/dang00.wmf..dang67.wmf.

## 🔒 My Identity
- Archetype: specification miner
- Roles: Specification Miner / Reverse Engineer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_2
- Original parent: 4300a9ce-e3eb-419c-8493-f2001d2d04d6
- Milestone: M0 - Pre-Implementation Clean-Room Specification Mining

## 🔒 Key Constraints
- NEVER decompile, copy, or circumvent Security.dll, hardware locks, or licensing checks.
- Treat C:\kata_pro as strictly read-only.
- Pure specification mining: do NOT implement product code.
- Write survey findings to survey_dwg_shapes_report.md and handoff to handoff.md.

## Current Parent
- Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6
- Updated: 2026-10-17T07:22:00Z

## Task Summary
- **What to build**: Comprehensive reverse engineering specification report on KataPro DWG schemas, XData structures, Block attributes, and 68 rebar shapes (dang00..dang67).
- **Success criteria**: Exhaustive extraction of all 68 rebar shapes (formula, topology, parameters, hook rules, bend deductions), all XData RegApp names + field orderings, and DWG block definitions from Iso_kata.dwg.
- **Interface contracts**: ORIGINAL_REQUEST.md (§ 2026-10-16T23:55:10Z), DISPATCH.md
- **Code layout**: Report written to `.agents/teamwork/teamwork_preview_spec_miner_survey_2/survey_dwg_shapes_report.md`

## Loaded Skills
- Source: reverse-engineer-anything (g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\skills\reverse-engineer-anything\SKILL.md)
  - Core methodology: Reverse engineer managed/.NET targets using runtime or static evidence without modifying original.

## Key Decisions Made
- Use read-only inspection tools: Python / PowerShell / .NET reflection / IL inspection on Kata_pro64_Cad2025.dll, Kata_Class_Lib.dll, and C:\kata_pro\dangthep\ without touching Security.dll.
- All 84 shapes (68 base + 16 variants) fully decompiled and cataloged into JSON and Markdown specification.
- Completed comprehensive report `survey_dwg_shapes_report.md`.

## Artifact Index
- survey_dwg_shapes_report.md — Comprehensive DWG schemas and rebar shapes technical report
- full_shapes_analysis.json — Full attribute & prompt analysis of 84 shape blocks
- wmf_geometries.json — Vector geometry, segments, and text parsed from 136 WMF files
- dwg_blocks_inventory.json — Complete attributes inventory of all 119 blocks in Iso_kata.dwg
- handoff.md — 5-component handoff report

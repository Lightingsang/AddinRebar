---
title: "AEC Automation MCP for AutoCAD (HPAutoCad) — from drawing commands to AEC understanding, rules, QA/QC"
description: "Grow the AutoCAD MCP into an AEC automation layer: drawing context, entity + spatial query, geometry analysis, AEC classification, relationships, batch editing, CAD standards, structural / architecture / MEP checks, clash + opening coordination, change sets. Tools stay seeds (tool.json + code.cs) over the existing execute path; the logic lives in a new HPAutoCad.Aec assembly the bridge references and scripts call."
status: completed
priority: P1
effort: 120h
branch: RebarVersion1
tags: [autocad, mcp, aec, geometry, spatial, structural, architecture, mep, clash, changeset, seeds]
created: 2026-09-16
revised: 2026-09-16
blockedBy: []
blocks: []
---

# AEC Automation MCP — AutoCAD

Design of record for the brief of 2026-09-16 (37 tools in 9 phases). Read [architecture.md](architecture.md) first: it maps the
requested layering onto what `HPAutoCad/` + `McpShared/` already are, and fixes the two decisions everything else hangs on —
[ADR-01](adr/adr-01-tools-as-seeds-over-execute-plus-aec-assembly.md) (tools are seeds, logic is a bridge-side assembly) and
[ADR-02](adr/adr-02-units-tolerance-handles-envelopes.md) (units, tolerance, handles, response envelopes, error codes).

## Phases

| Phase | File | Tools | Status |
|---|---|---|---|
| A — Core | [phase-A](phase-A-core-context-query-spatial-measure-issues.md) | `get_drawing_context`, `query_entities`, `query_entities_spatial`, `measure_geometry`, `detect_geometry_issues` + `HPAutoCad.Aec` foundation (geometry, tolerance, spatial index, entity reader, envelopes) + `HPAutoCad.Aec.Tests` | completed 2026-09-16 — [report](reports/phase-A-core-live.md) |
| B — Semantic | [phase-B](phase-B-semantic-classification-relationships.md) | `classify_aec_entities` (rule config JSON), `get_entity_relationships` | completed 2026-09-16 — [report](reports/phase-B-semantic-live.md) |
| C — Editing | [phase-C](phase-C-editing-batch-blocks-annotations-hatch-xref.md) | `create_entities_batch`, `update_entities_batch`, `manage_blocks_attributes`, `manage_annotations`, `manage_hatches`, `manage_xrefs` | completed 2026-09-16 — [report](reports/phase-C-editing-live.md) |
| D — QA/QC | [phase-D](phase-D-qaqc-standards-audit-markup.md) | `cad_standards_check`, `audit_aec_drawing`, `create_issue_markup` | completed 2026-09-16 — [report](reports/phase-D-qaqc-live.md) |
| E — Structural | [phase-E](phase-E-structural.md) | `structural_detect_grids`, `structural_detect_members`, `structural_member_connectivity_check`, `structural_column_alignment_check`, `structural_opening_conflict_check`, `structural_tag_members`, `structural_generate_member_schedule` | completed 2026-09-17 — [report](reports/phase-E-structural-live.md) (incl. review round 5/10 → fixed) |
| F — Architecture | [phase-F](phase-F-architecture.md) | `arch_detect_rooms`, `arch_room_boundary_check`, `arch_create_room_tags`, `arch_generate_area_schedule`, `arch_auto_dimension_plan` | completed 2026-09-17 — [report](reports/phase-F-architecture-live.md) (incl. review round 5/10 → fixed) |
| G — MEP | [phase-G](phase-G-mep-network.md) | `mep_detect_network`, `mep_connectivity_check`, `mep_endpoint_check` | completed 2026-09-17 — [report](reports/phase-G-mep-live.md) (incl. review round 5.5/10 → fixed) |
| H — Coordination | [phase-H](phase-H-coordination-clash-openings.md) | `aec_clash_check`, `aec_create_opening_requests` | completed 2026-09-17 — [report](reports/phase-H-coordination-live.md) (incl. review round 5.5/10 → fixed) |
| I — Change management | [phase-I](phase-I-change-sets.md) | `begin_change_set`, `preview_change_set`, `get_change_summary`, `commit_change_set`, `rollback_change_set` (+ `changeSetId` on the 12 write tools) | completed 2026-09-17 — [report](reports/phase-I-change-sets-live.md) (incl. review round 6/10 → fixed) |

Each phase ends with: build 0 warnings, xUnit (Aec pure + seed compile), live smoke of every new tool through the stdio server against
AutoCAD 2026 (`tools/harness`), a code review, docs. A tool is "done" only after it ran live.

## Key dependencies / constraints

- `HPAutoCad/` references `../McpShared/` only; `McpShared` changes stay additive (`HostScriptContracts.AutocadImports` + one namespace).
- Seeds contract unchanged: body ending in `return`, `args.X("literal", default)` for every declared input, `tr` is the bridge's,
  read-only tools declare `transaction: none` (the runner aborts and refuses any modification), mm at the API boundary.
- Existing 24 tools untouched (names, schemas). `get_entities` / `get_drawing_info` stay; the new tools supersede them in the docs.
- Output cap 64 KB per run (`MaxOutputBytes`) → every list tool has `limit` + `offset` + summary mode.
- Live verification needs AutoCAD closed for deploy (`dotnet build HPAutoCad.slnx -c Debug`), open for the harness.

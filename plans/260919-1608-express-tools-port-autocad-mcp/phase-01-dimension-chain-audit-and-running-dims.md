---
phase: 1
title: "Dimension chains — audit (gaps, overlaps, overrides, duplicates) and running dimensions"
status: planned
priority: P2
effort: "12h"
dependencies: []
---

# Phase 1: Dimension chains — audit + running dimensions

## Context Links
- [plan.md](plan.md) · [research](research/vinacad-express-tools-analysis.md) (rows `KTD` / `DCD`) · AEC [ADR-01](../260916-1140-aec-automation-mcp-autocad/adr/adr-01-tools-as-seeds-over-execute-plus-aec-assembly.md) / [ADR-02](../260916-1140-aec-automation-mcp-autocad/adr/adr-02-units-tolerance-handles-envelopes.md)
- Existing pieces to reuse: `Issues/AuditIssue` (`Ordered`, `AtLeast`), `Cad/AuditService` (sections), `Cad/EditContext` + `EditContext.Layers` (`EnsureLayer`, two-phase), `Cad/RoomWriteService.Dimensions` (how an aligned dimension is created, `MaxDimensions` 120), `Model/EntityFilter`, `Geometry/GeometryTolerance`, `Cad/ChangeSetRecorder`, `Cad/ChangeSetReplay.WriteToolTable`

## Overview
Vietnamese structural / architectural sheets carry **dimension chains** (chuỗi dim) and, under them, **running dimensions** ("dim cộng
dồn": 0 · 3600 · 7200 · …). Checking them by eye is the classic source of wrong sheets: a chain with a gap, two dims overlapping, a
dim whose text was typed over, a duplicated dim. HPAutoCad has 62 tools and **none reads a dimension** beyond creating one. This phase
adds the audit and the generator.

Tools: `audit_dimension_chains` (Audit, `none`) · `create_running_dimensions` (Annotation, `auto`) · section `dimensions` in
`audit_aec_drawing`.

## Key Insights
- A chain is a *geometric* object, not a drawing one: dims on the same dim line (same station along the chain normal within `lineMm`),
  same direction, sorted along the direction, consecutive when `|start − previousEnd| ≤ gapMm`. Grouping is two-level (station cluster,
  then continuity). Overlap does not break a chain — it is a finding inside it.
- The displayed number is `Measurement × DIMLFAC` unless `DimensionText` is set (empty or `<>` = measured). Compare the parsed text with
  the measured value; a non-numeric text (`TYP`, `EQ`) is an override of a different kind, not an error.
- Aligned dims take their direction from `XLine2Point − XLine1Point`; rotated dims from `Rotation`. Angles equal within 0.03 rad
  (≈ 1.7°) are the same direction; horizontal / vertical are the 0 / 90° buckets, everything else `aligned`.
- Running dims: one zero-length dim with text `0` at the base point plus one dim per segment from the base to the cumulative station,
  `<>` text so it self-measures, same dim line as the source chain. Only clean chains get them (`skipChainsWithIssues` default true).
- OCS: a dimension whose `Normal` is not ±Z is skipped with a warning (the engine's plan geometry is XY).
- Dimensions inside block definitions are never traversed (same rule as every other tool).

## Requirements
### Functional
- `audit_dimension_chains` args: `filter` (handles / layers / space, `EntityFilter.From`), `direction` `all|horizontal|vertical|aligned`,
  `tolerance` `{valueMm 1, lineMm 10, gapMm 1, chainBreakMm 100, angleDeg 1.7}`, `checks[]` (default all: `chain_gap`, `chain_overlap`,
  `dim_text_override`, `dim_text_nonnumeric`, `duplicate_dimension`, `dimlfac_mixed`, `dimstyle_mixed`), `minSeverity`, `includeChains`
  (default true), `limit` ≤ `MaxChainLimit` 50, `offset`, `maxCandidates`.
- Output: `{success, summary{dimensions, chains, issues{critical,warning,info}}, chains[{id "DIMC-nnn", direction, angleDeg, stationMm,
  count, fromMm, toMm, totalMm, dimStyle, layer, handles≤64, handlesTruncated, issueCount}], issues[AuditIssue "DIM-nnnn" with handles +
  locationMm], warnings, errors, truncated}`.
- Severity: `chain_gap` between `gapMm` and `chainBreakMm` → warning (beyond = a new chain, no finding); `chain_overlap` warning;
  `dim_text_override` warning; `dim_text_nonnumeric` info; `duplicate_dimension` warning; `dimlfac_mixed` / `dimstyle_mixed` info.
- `create_running_dimensions` args: same `filter` + `tolerance` (so chain ids agree — the phase-F "shared detection block" rule),
  `chainIds[]` or `handles[]` (one of them required; handles must all belong to one detected chain), `mode` `running|overall`
  (`overall` = one dim spanning first → last), `startFrom` `min|max` (left→right / bottom→top by default), `dimStyle` (must exist — else
  `ArgumentException` listing the drawing's dim styles; default = the chain's own style), `layer` (default `A-ANNO-DIMS-RUN`, created
  on demand via `EnsureLayer`), `colorIndex` (default `byLayer`), `offsetMm` (0 = on the source dim line; positive = shifted outward along
  the chain normal, away from the extension points), `skipChainsWithIssues` (default true), `dryRun`, `changeSetId`.
- Output: `EditResult` — `createdCount`, `affectedHandles`, `items[{chainId, dimensions, totalMm, ok, error}]`, preview lists the plan
  under `dryRun`. Caps `MaxRunningChains` 20 per call, `MaxRunningDimensions` 120 (more → `ArgumentException`).
- `audit_aec_drawing` gains section `dimensions` (ids stay `DIM-`), off by default until the user lists it (keeps existing outputs
  byte-identical).
### Non-functional
- Pure engine testable without AutoCAD; every finding pinned by a unit test with a synthetic chain.
- Envelope under 64 KB at every cap (`EditResultTests` style).

## Architecture
```
seed audit_dimension_chains ──▶ AecTools.Audit.AuditDimensionChains(args)
                                   └─ Cad/DimensionReader.Read(db, tr, filter)  → DimensionRecord[] (mm)
                                   └─ Dimensions/DimensionChainBuilder.Build(records, tol) → DimensionChain[]
                                   └─ Dimensions/DimensionChainChecker.Check(chains, tol, checks) → AuditIssue[]
seed create_running_dimensions ─▶ AecTools.Annotation.CreateRunningDimensions(args)
                                   └─ ChangeSetRecorder.TryRecord … (record instead of apply)
                                   └─ same Read + Build → select chains → Dimensions/RunningDimensionPlanner.Plan(chain, opts) (pure: base point, stations, dim line point)
                                   └─ Cad/DimensionWriteService.Write(cx, plans)  (two-phase: validate style/layer/space, then append RotatedDimension per plan item)
```
- `Dimensions/` is a new pure folder: `DimensionRecord` (handle, kind, p1, p2, dimLinePoint, angleRad, normalZ, measurementMm, dimlfac,
  textRaw, textValue?, dimStyle, layer, space), `DimensionChain`, `DimensionChainBuilder`, `DimensionChainChecker`,
  `RunningDimensionPlanner`, `DimensionTolerance` (record, defaults above, no literals in services).
- `Cad/DimensionReader`: `RotatedDimension` / `AlignedDimension` only (ordinate, angular, radial ignored + counted in a warning);
  `Measurement`, `DimensionText`, `Dimlfac`, `DimLinePoint`, `XLine1Point` / `XLine2Point`, `Rotation`, `Normal`, `DimensionStyleName`.
- `Cad/DimensionWriteService`: appends `RotatedDimension(rotation, p1, p2, dimLinePoint, text, styleId)` in the chain's own space
  (layout or model — chains are per space; the audit never pairs across spaces), sets layer / colour, `RecomputeDimensionBlock(true)`.
- Seeds: `Audit/audit_dimension_chains/{tool.json, code.cs, examples.json}`, `Annotation/create_running_dimensions/…`; shim rule = one
  `AecTools.*` call. `WriteToolTable` gains `create_running_dimensions`.

## Related Code Files
- Create: `HPAutoCad.Aec/Dimensions/*` (6 files), `HPAutoCad.Aec/Cad/DimensionReader.cs`, `HPAutoCad.Aec/Cad/DimensionWriteService.cs`,
  `HPAutoCad.Aec/AecTools.Dimensions.cs`, two seed folders, `HPAutoCad.Aec.Tests/DimensionChainTests.cs`.
- Modify: `AecTools.Audit.cs` (section `dimensions`), `Cad/AuditService.cs` (+ `AuditCategory.Dimensions`), `Cad/ChangeSetReplay.cs`
  (`WriteToolTable`), `Registry/SeedLibrary/_seeds.json` (regenerated), `tools/harness/run-aec-tools-live.ps1` + `run-aec-edit-tools-live.ps1`
  (new step + scene: one clean chain of 4, one chain with a 30 mm gap, one overlap, one typed-over text, one duplicate, one aligned chain at 30°).
- Delete: none.

## Implementation Steps
1. `DimensionTolerance` + `DimensionRecord` + `DimensionChainBuilder` (station cluster → sort → continuity) with unit tests on synthetic records (horizontal, vertical, 30° aligned, reversed order, overlap kept in chain, gap > `chainBreakMm` splits).
2. `DimensionChainChecker`: the 7 checks, `AuditIssue` ids `DIM-nnnn` in `Ordered` severity, `locationMm` = midpoint of the offending extension points; tests per check.
3. `Cad/DimensionReader` + `AecTools.Audit.AuditDimensionChains`; seed + `SeedLibraryTests` (shim, schema ⇔ args, caps); wire section `dimensions` into `AuditService`.
4. `RunningDimensionPlanner` (base point by `startFrom`, cumulative stations from *measured* values ÷ `dimlfac` so an override never propagates, dim line point + `offsetMm` along the normal) + tests incl. `overall` mode.
5. `Cad/DimensionWriteService` two-phase + `AecTools.Annotation.CreateRunningDimensions` + `ChangeSetRecorder` + `WriteToolTable` reader; seed; `ChangeSetTests` still green.
6. Harness steps (read: 8 checks; write: preview → apply → `U` → apply on a chain with issues refused → `skipChainsWithIssues:false` applies → wrong `dimStyle` refused with the style list) — live in AutoCAD 2026.
7. Review round, `CLAUDE.md` / `AGENTS.md`, skill catalog, docs.

## Todo List
- [ ] 1 builder + tests · [ ] 2 checker + tests · [ ] 3 audit seed + section · [ ] 4 planner + tests · [ ] 5 write seed + change set · [ ] 6 live · [ ] 7 review + docs

## Success Criteria
- The harness scene reports exactly: 1 gap, 1 overlap, 1 override, 1 duplicate, 0 findings on the clean chain and the aligned chain; `create_running_dimensions` on the clean chain creates 5 dims (`0` + 4) whose `Measurement` equals the cumulative stations to 0.01 mm; `U` removes them; `audit_aec_drawing` without `sections: ["dimensions"]` is byte-identical to today.

## Risk Assessment
- `Dimension.Measurement` may be stale on a dimension whose block was never recomputed (opened from another CAD) → read after `RecomputeDimensionBlock` is *not* allowed under `none`; fall back to the extension-point distance × `dimlfac` and mark the record `measurementSource: geometry`.
- Text parsing: `"3.600"`, `"3 600"`, `"%%c200"`, `\A1;`-style MText codes in `DimensionText` — there is no plain-text accessor on `Dimension` (the engine reads `MText.Text` elsewhere, nothing strips codes today) → a pure `Dimensions/DimensionTextParser` (strip `\X…;` codes, `%%c/%%d/%%p`, `<>` placeholder, thousands separators, `~` approximations) with its own tests; anything left non-numeric = `dim_text_nonnumeric`.
- Chains on two dim lines 8 mm apart (drafting sloppiness) merge under `lineMm` 10 → the finding lists both stations; the user tunes `lineMm`.

## Security Considerations
- Read-only audit under `none`; writes go through the bridge's transaction, `dryRun`, change sets; no file I/O, no styles imported.

## Next Steps
- Phase 2 is independent; phase 4's `repair_double_line_walls` could later feed `audit_aec_drawing` the same way this section does.

---
phase: 5
title: "Rebar schedule from attributed blocks — mapping-driven totals (optional)"
status: planned (blocked on the user's answer — see plan.md)
priority: P3
effort: "12h"
dependencies: []
---

# Phase 5 (optional): Rebar schedule from attributed blocks (thống kê cốt thép)

## Context Links
- [plan.md](plan.md) · [research](research/vinacad-express-tools-analysis.md) (rows `XLTKCT` / `TKCT` / `TKCK` / `QS` / `QD` / `QF`)
- Reuse: `Cad/BlockService` attributes, `Model/RuleFileLocator` (a JSON mapping file `%AppData%\HPAutoCad\McpServer\rules\rebar-blocks.json`
  or inline `mapping`), `Model/NaturalOrder` (phase 2), `Cad/StructuralWriteService.Table` / `TableWriter`, `ChangeSetRecorder`

## Why optional
The source computes bar lengths and weights from **its own attributed block library** (`TKT_*`, `TKH_*`, `TKS_B*`: tags `DK` diameter,
`SLTB` count, `L1…L5` segments, `DAI`, `CD`, `DT`). Without that convention there is nothing to read. HPRebar (Revit) already
does real rebar; in AutoCAD this only pays off if the office draws schedules with attributed blocks. **Decision needed from the user.**

## Overview
`rebar_schedule_from_blocks` (Structural, `none`; `auto` when `write` is set): read every schedule block matched by a **mapping**, compute
per-mark bar length (segments + hooks + laps), total length, weight by diameter, split totals by diameter class, and optionally write the
computed values back into the blocks' output attributes and a totals block / table.

## Key Insights
- Everything office-specific is data: `mapping {blockNames[], tags{mark, diameter, count, countPerMember?, segments[], shape?, outLength?,
  outTotal?, outWeight?}, shapeHooks{shapeCode → hookCount}}` — inline or a rule file (`RuleFileLocator`, traversal refused).
- Engineering parameters are arguments with **no silent defaults**: `lapFactor {d≤10, 10<d≤16, d>16}` (× d), `hookMm {normal, seismic}`,
  `seismic` bool, `stockLengthMm` 11 700 (laps added per `floor(length / stock)`), `unitWeightKgPerM` table (default the standard
  0.00617 · d² — the only default, documented). A missing mapping key that the computation needs → `ArgumentException`.
- Diameter classes for totals are arguments too (`classes: [10, 18]` → ≤10 / 10–18 / >18).
- Natural sort on marks; duplicate marks reported, never merged silently.

## Requirements
- Output `{success, summary{blocks, marks, totalLengthM, totalWeightKg, byDiameter[{d, lengthM, weightKg}], byClass[…]}, rows[{mark,
  diameter, count, segmentsMm[], hooks, laps, barLengthMm, totalLengthM, weightKg, handle}], warnings, errors[{code, handle}]}`,
  `limit` ≤ 200 + `offset`.
- `write {attributes: true, totalsBlockHandle?, table{locationMm, …}}` → two-phase attribute writes (`ATTRIBUTE_MISSING` per item) + optional table; `dryRun`, `changeSetId`.

## Architecture
```
Rebar/RebarBlockMapping.cs   (JSON, validated on load: required keys, positive factors)
Rebar/RebarSchedule.cs       (pure: length = Σ segments + hooks·hookMm + laps·lapFactor·d; weight = length · kg/m; totals)
Cad/RebarBlockReader.cs      (attributes via BlockService, numeric parsing with the phase-1 text parser)
Cad/RebarBlockWriter.cs      (two-phase attribute updates + TableWriter)
AecTools.Rebar.ScheduleFromBlocks
```

## Implementation Steps
1. Mapping model + validation + tests. 2. `RebarSchedule` pure + tests against a hand-computed sheet. 3. Reader + seed (read). 4. Writer + `write` + change set. 5. Live on a scene built with **our own** attributed block (never the source's). 6. Review + docs.

## Success Criteria
- A 10-mark sample sheet totals match a spreadsheet to 0.01 kg; write-back is idempotent; a wrong mapping key is refused before any read.

## Risk Assessment
- Formula disagreements between offices (hook length by diameter vs fixed; lap per class vs per diameter) → all parameters explicit, the `summary` echoes them.

## Security Considerations
- Rule file read from the registry `rules\` folder only; no other file I/O.

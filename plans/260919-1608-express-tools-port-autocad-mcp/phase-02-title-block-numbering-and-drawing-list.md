---
phase: 2
title: "Title blocks — numbering in reading / layout order, drawing list, drawing-list table"
status: planned
priority: P2
effort: "8h"
dependencies: []
---

# Phase 2: Title-block numbering + drawing list

## Context Links
- [plan.md](plan.md) · [research](research/vinacad-express-tools-analysis.md) (rows `XLDM` / `DSHBV` / `VBDM`)
- Reuse: `Cad/BlockService` (+ `.Attributes`: `ValidateAttributes`, attribute keys checked against the definition), `Structural/MemberTagging`
  (band clustering `DefaultRowBandMm`, reading order), `Cad/StructuralWriteService.Table.WriteTable` (the `Table` API writer, 20 marks per cell
  → here one row per drawing), `Cad/EditContext`, `ChangeSetRecorder`, `WriteToolTable`, natural sort (`ThepDauDam.CompareSoHieuNatural`-like —
  write our own in `Model/NaturalOrder`)

## Overview
A sheet set is dozens of title-block INSERTs (one per layout, or many on one model-space sheet). Numbering them by hand after every
reshuffle is error-prone; the drawing list (danh mục bản vẽ) is typed a second time. Three tools: number the blocks in a chosen order,
read the list back as data, and draw the list as a table.

Tools: `number_title_blocks` (Block, `auto`) · `generate_drawing_list` (Data, `none`) · `create_drawing_list_table` (Data, `auto`).

## Key Insights
- Title blocks live either one per **layout** (order = layout tab order, `LayoutManager` / `Layout.TabOrder`) or many in **model /
  paper space** (order = reading order by band). The tool must offer both; `order: "layoutTabs"` is the default when `space` is a layout
  filter, `rowsThenColumns` otherwise.
- The attribute tags are the office's convention, so they are arguments with defaults (`DRAWINGNO`, `DRAWINGTITLE1`, `DRAWINGTITLE2`,
  `SCALE`, `DATE`); a block lacking the tag is an item error, never a crash.
- Numbering formats: `prefix + number(digits) + suffix` (`KC-01`), or alphabet (`A`…`Z`, `AA`…) when `style: "alphabet"`; `start` may be
  a number or a letter. Existing values are shown in the preview (`oldValue → newValue`); `overwrite` defaults **true** here (the point of
  the tool), `keepExisting: true` reserves numbers already present with the same prefix (phase-E semantics).
- The list reads the same blocks in the same order so the table and the numbering never disagree → one `TitleBlockService.Collect`
  behind all three tools with a shared `selection` block (`blockName`, `tags`, `filter`, `order`, `rowBandMm`).

## Requirements
### Functional
- `number_title_blocks`: `blockName` (wildcards, effective dynamic name — required), `tag` (default `DRAWINGNO`), `filter` (layers /
  space / handles), `order` `layoutTabs|rowsThenColumns|columnsThenRows`, `rowBandMm` (default 500 — title blocks are big; passed to the
  band clustering), `style` `number|alphabet`, `prefix`, `start` (default 1 / `A`), `digits` (default 2), `suffix`, `overwrite` (default
  true), `keepExisting`, `dryRun`, `changeSetId`, `atomic`. Output `EditResult` with `items[{handle, layout, oldValue, newValue,
  changed, error}]`, `modifiedCount`; cap `MaxTitleBlocks` 200.
- `generate_drawing_list`: same `selection` + `tags {no, title1, title2, title3?, scale, date}` → `{success, count, rows[{index, drawingNo,
  title, scale, date, layout, handle}], warnings}`; `sort` `order|drawingNo` (natural order on `drawingNo`); `limit` ≤ 200 + `offset`.
- `create_drawing_list_table`: rows from `generate_drawing_list` (re-collected — never passed back, so the table cannot drift) →
  `Table` at `locationMm` in `space` (default the current layout), columns `STT | Tên bản vẽ | Số hiệu` (+ optional `scale`, `date`),
  `title`, `textHeightMm`, `tableStyle` (must exist), `rowsPerTable` (split into several tables `columnGapMm` apart), `layer`;
  `dryRun`, `changeSetId`. Cap 200 rows.
### Non-functional
- Preview lists every planned change; a refused atomic batch leaves the change counter at 0 (phase-C rule).

## Architecture
```
Cad/TitleBlockService.Collect(db, tr, selection) → TitleBlockRecord[] {handle, position, layout, tabOrder, attributes}
   ├─ order layoutTabs → by Layout.TabOrder then position
   └─ order rows/columns → MemberTagging band clustering (shared)
Sheets/DrawingNumbering.Plan(records, opts) → NumberingPlan[] {handle, old, new}   (pure)
Sheets/DrawingList.Build(records, tags, sort)  → DrawingListRow[]                   (pure, natural sort)
Cad/TitleBlockWriteService.Apply(cx, plans)     (two-phase over BlockService attributes)
Cad/TitleBlockWriteService.WriteTable(cx, rows, …) (reuses the Table writer from StructuralWriteService.Table, generalised to columns)
AecTools.Sheets.{NumberTitleBlocks, GenerateDrawingList, CreateDrawingListTable}
```
- Category **Sheets** is *not* added to the profile — seeds go under `Block` / `Data` (existing categories) to keep `tools/list` stable.

## Related Code Files
- Create: `HPAutoCad.Aec/Sheets/{TitleBlockRecord, DrawingNumbering, DrawingList, AlphabetNumber}.cs`, `Model/NaturalOrder.cs`,
  `Cad/TitleBlockService.cs`, `Cad/TitleBlockWriteService.cs`, `AecTools.Sheets.cs`, 3 seed folders, `HPAutoCad.Aec.Tests/SheetsTests.cs`.
- Modify: `Cad/StructuralWriteService.Table.cs` (extract the generic `TableWriter` so both schedule and list use it), `WriteToolTable`,
  harness scenes (a layout set of 6 title blocks + 4 on one model sheet).

## Implementation Steps
1. `NaturalOrder` + `AlphabetNumber` (A…Z, AA…) + `DrawingNumbering.Plan` with tests (number / alphabet, `keepExisting`, `overwrite:false` keeps and reports `kept_existing`).
2. `TitleBlockService.Collect` (effective name filter reuses `EntityFilter` block matching; layout tab order via `Layout` records; band order via `MemberTagging`).
3. `number_title_blocks` two-phase + change set + seed + tests.
4. `DrawingList.Build` + `generate_drawing_list` seed.
5. Generic `TableWriter` + `create_drawing_list_table` seed.
6. Live: number 6 layouts by tab order, renumber after swapping two tabs, alphabet mode, table of the 6, `U`, wrong tag → item errors, unknown table style → `ArgumentException` naming the styles.
7. Review + docs.

## Todo List
- [ ] 1 · [ ] 2 · [ ] 3 · [ ] 4 · [ ] 5 · [ ] 6 live · [ ] 7 docs

## Success Criteria
- Numbering by layout tab order matches the tab order after a swap; model-space blocks in a 2 × 2 grid number 1–4 row by row with `rowsThenColumns` and column by column with `columnsThenRows`; the table's rows equal `generate_drawing_list` byte for byte; `U` reverts the table and the numbers.

## Risk Assessment
- Title blocks as **xref** or nested inside another block → `UNSUPPORTED_ENTITY` per item (owner must be a layout — `EditContext.OpenForEdit` rule).
- Attribute in a *constant* attribute definition (no `AttributeReference`) → item error `ATTRIBUTE_CONSTANT`.
- Two title blocks in one layout (cover + sheet) → `layoutTabs` order breaks ties by position; the preview shows it.

## Security Considerations
- Writes only attribute text and one table; nothing deleted; change-set aware.

## Next Steps
- The `TableWriter` extraction is a small refactor of phase-E code — run `StructuralReviewTests` after it.

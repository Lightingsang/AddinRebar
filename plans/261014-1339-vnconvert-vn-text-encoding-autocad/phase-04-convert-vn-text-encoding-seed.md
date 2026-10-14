---
phase: 4
title: "convert_vn_text_encoding (write seed + change set)"
status: pending
priority: P1
effort: "10h"
dependencies: [3]
type: AFK
---

# Phase 4: convert seed

## Overview
Seed `convert_vn_text_encoding` (Annotation, `transaction: auto`) converts every legacy text found by phase 3 to Unicode and remaps the
legacy text styles' fonts, two-phase, with `dryRun` and `changeSetId` like every other AEC write seed.

## Requirements
- Args: filter (`handles`, `space`, `layers`, `includeBlocks` default true, `includeTables` default true), `sourceCharset`
  (`auto` default | `tcvn3` | `vni`), `fontMap` (object, overrides defaults), `remapStyles` (default true), `atomic` (default true),
  `changeSetId`.
- Phase 1 (read): resolve targets, decode, validate (locked/frozen layer → `LAYER_LOCKED`/`LAYER_FROZEN`, style without a map target →
  `NO_FONT_MAPPING`, unknown storage form → `UNSUPPORTED_ENCODING`); atomic + any error → `EditResult.Refused`, nothing opened for write.
- Phase 2 (write): `Upgrade`; set `TextString` / `Contents` (rebuilt runs) / `DimensionText` / MLeader MText / table cell text;
  block definitions edited once (references follow); attribute references converted per reference; then styles: `FileName`/`Font`
  set to the mapped Unicode TrueType face (keeps bold/italic/charset 0), applied **after** texts so a failure leaves styles intact.
- Envelope `EditResult` (`createdCount` 0, `modifiedCount`, `affectedHandles` incl. style handles, `items` ≤ 200, `warnings`, `errors`
  via `Settle`); `MaxBatchItems` reused; > cap → `ArgumentException` with paging hint.
- Change set: `ChangeSetRecorder.TryRecord` at the top of the facade; add the tool to `WriteToolTable` (reader mirroring the shim) so
  `commit_change_set` replays it and `rollback_change_set` restores via snapshots (style records included in `Fingerprint`?
  → extend only if snapshots miss `TextStyleTableRecord`; verify in test first).
- Idempotent: second run finds nothing legacy → success, modified 0.

## Related Code Files
- Create: `HPAutoCad.Aec/Cad/VnTextWriteService.cs` (+ partial `.Styles.cs` if > 300 lines), seed folder
  `SeedLibrary/Annotation/convert_vn_text_encoding/`, tests `HPAutoCad.Aec.Tests/VnConvert/VnTextWritePlanTests.cs`
- Modify: `HPAutoCad.Aec/AecTools.VnText.cs`, `HPAutoCad.Aec/Cad/ChangeSetRecorder.cs`, `HPAutoCad.Aec/Cad/ChangeSetReplay.cs`
  (WriteToolTable), `HPAutoCad.Mcp.Server.Tests/SeedLibraryTests.cs`

## Implementation Steps
1. Pure write plan (`VnTextWritePlan`: target → new text / new style font) unit-tested without AutoCAD.
2. Cad writer two-phase; reuse `EditContext` layer guard + `OpenForEdit` (block-definition entities allowed **only** for this service,
   via an explicit opt-in parameter — default AEC rule unchanged).
3. Change set wiring + `SeedLibraryTests` pin (write seeds ⇔ WriteToolTable).
4. Envelope size test at 200 items; build gate; tests.

## Success Criteria
- [ ] Refused atomic batch writes nothing (bridge change counter 0).
- [ ] Style remap happens only after all texts succeeded; idempotent second run.
- [ ] WriteToolTable pin test green; all existing AEC tests green.

## Risk Assessment
- Editing block definitions affects every reference → summary lists block names + reference counts in preview.
- `TextStyleTableRecord` not covered by change-set snapshots → rollback incomplete; test early, extend `ChangeSetSnapshots` if needed (logged).
- Widening `EditContext` for block definitions could leak into other seeds → opt-in parameter, regression test that `update_entities_batch`
  still refuses block-definition handles.

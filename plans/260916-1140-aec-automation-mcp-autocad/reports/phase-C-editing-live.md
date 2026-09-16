# Phase C — Editing: implementation + live verification report (2026-09-16)

## Implemented

| Piece | Files | Notes |
|---|---|---|
| Edit envelope | `HPAutoCad.Aec/Model/EditResult.cs` (`EditResult` success/summary/createdCount/modifiedCount/deletedCount/affectedHandles/items/warnings/errors, `ItemOutcome` index/ok/handle/type/changed/error, `Settle`) | items[i] answers for request.items[i] |
| Write context | `Cad/EditContext.cs` — layer guard (`LayerForCreate`: missing → INVALID_ARGUMENT, locked → LAYER_LOCKED, frozen → created + warning; `LayerAllowsEdit`: locked/frozen entities refused), `OpenForEdit` (handle → entity → layer → owner must be a layout; read-open, never throws) + `Upgrade`, `ValidateProperties` (no mutation), `Space` (current/model/layout name), `Point` {x, y[, z]} mm, `ApplyProperties` (layer, colorIndex 0/1–255/256, color #RRGGBB/ByLayer/ByBlock, linetype loaded, lineweight enum, visible) | every check answers a `ToolError`, no throw |
| Create / update | `Cad/EntityFactory.cs` (line, polyline with bulges, circle, arc, point, text, mtext, blockReference + attributes after append, dimension linear/aligned), `Cad/EntityUpdater.cs` (text, heightMm, rotationDeg, scale, textOverride, style, dimStyle, attributes, geometry per type, move/rotate/scaleBy on any entity), `Cad/BatchEditService.cs` (`MaxBatchItems` 200; two-phase: read-open + validate → refuse whole batch on one invalid item, nothing written, change counter 0; upgrade + apply; write failure → throw → bridge aborts; atomic=false = per-item errors, rest written, `changed` reported on a midway failure) | |
| Blocks | `Cad/BlockService.cs`: listDefinitions, findReferences (through `EntityQueryService`), insert, readAttributes, writeAttributes (unknown tag = warning, none = error), batchUpdateAttributes (items / handles / filter), inspectDynamic, setDynamic (distances mm ↔ drawing units, angles degrees ↔ radians) | |
| Annotations | `Cad/AnnotationService.cs`: create text/mtext/dimension (linear, aligned, angular `Point3AngularDimension`, radial, diameter) / mleader (`AddLeader` + `AddLeaderLine` + first/last vertex, MText content), update (via the updater), batchUpdate, delete (annotation DXF types only — geometry refused with UNSUPPORTED_ENTITY) | measurement in the summary (mm / degrees) |
| Hatches | `Cad/HatchService.cs`: create from boundaryHandles (closed curves, first outer) / points polygon / seedPoint (smallest closed entity containing it), pattern SOLID or acad.pat name, scale/angle/style/associative; NOT_CLOSED refuses the hatch before anything is created; update; delete (hatches only); detectBoundary (geometric: closed entities containing the point, innermost first — works outside a command context) | area from `Hatch.Area`, boundary fallback right after creation |
| Xrefs | `Cad/XrefService.cs`: list / resolveStatus (name, saved path, found path via `HostApplicationServices.FindFile`, status, loaded, overlay, referenceCount, nestedIn), attach (absolute existing .dwg, `AttachXref`/`OverlayXref` + one reference), detach (references erased and counted), reload, unload, bind (only Resolved + loaded; one refused xref refuses the bind; `insertBind`) | |
| Facade + seeds | `AecTools.Editing.cs`; seeds `Drawing/create_entities_batch`, `Drawing/update_entities_batch`, `Block/manage_blocks_attributes`, `Annotation/manage_annotations`, `Drawing/manage_hatches`, `Data/manage_xrefs` (`transaction: auto`, `destructive: true`, descriptions state side effects + dryRun + envelope) | flat facade parameters: every schema key is a literal `args.X` read in the shim (analyzer rule) |
| Tests | `EditResultTests` (9, incl. the envelope-size test at `MaxBatchItems`) + `XrefPathPolicyTests` (4) → `HPAutoCad.Aec.Tests` 122; `SeedLibraryTests` 25 seeds + write-seed shim theory + batch/page caps → `HPAutoCad.Mcp.Server.Tests` 136 | |
| Harness | `tools/harness/aec-edit-tools-live.py` (40 → 62 checks) + `run-aec-edit-tools-live.ps1` (wraps `run-aec-tools-live.ps1 -Script`) | own output folder; writes the xref DWG it attaches via `Wblock` + `SaveAs` |

## Build / tests

| Check | Result |
|---|---|
| `dotnet build HPAutoCad.slnx -c Debug` | 0 warnings; bundle deployed |
| `HPAutoCad.Aec.Tests` | 122/122 |
| `HPAutoCad.Mcp.Server.Tests` | 136/136 (25 seeds compile against AutoCAD.NET 25.1.0 with the bridge's imports, pass the validator/guard, schema ⇔ args, caps pinned to engine constants) |

## Live (AutoCAD 2026, `run-aec-edit-tools-live.ps1`)

Run 1: 33/40 — `Hatch.Area` throws right after `EvaluateHatch` inside the same transaction (summary `areaMm2` null) → boundary-area fallback (curve area / polygon shoelace); the harness's Wblock script used a top-level `using var`, which compiles in the seed test's method wrapper but not as a Roslyn script → `try/finally`. Run 2: **40/40**. Regression `run-aec-tools-live.ps1` (phases A + B): 55/55.

| Step | Verified |
|---|---|
| C create | atomic batch of 5 types (polyline, line, circle, text, arc) → 5 handles in input order, layer/colour/geometry as asked; atomic + locked layer → refused, nothing created; atomic=false → 1 created + LAYER_LOCKED + INVALID_ARGUMENT per item; frozen layer → created with warning; block reference with MARK + aligned dimension; **dryRun** → envelope says 2 created, run `rolledBack`, count unchanged; empty items → ArgumentException |
| U update | shared set (layer + colour) over 2 handles; per-item text/height, line end (length 6000), rotate 45°, move + radius, polyline re-vertexed to 5 points (area 52 000 000); locked → LAYER_LOCKED (other item applied); atomic + locked → refused, valid item untouched; ERASED / INVALID_HANDLE / UNSUPPORTED_ENTITY; missing set → ArgumentException |
| B blocks | listDefinitions (DOOR-TEST, tags [MARK], 2 references); findReferences (position, rotation 90, attributes); insert (attributesSet 1); writeAttributes D09 + unknown-tag warning; readAttributes refuses a polyline; batchUpdateAttributes by filter → 3 references; inspectDynamic on a plain block; setDynamic refused; unknown op lists ops |
| A annotations | text, mtext, aligned 8000 mm, angular 90°, radial 600, diameter 1200, mleader created with measurements; update text/height/rotation; delete refuses a LINE (atomic, nothing deleted), then deletes text + mtext |
| H hatches | ANSI31 from a closed polyline (area 160 000); detectBoundary innermost first (column, then room); SOLID from seedPoint (52 000 000, warning names the boundary); open polyline → NOT_CLOSED, nothing created; update pattern/angle/scale/colour; delete refuses a polyline, then deletes the hatch |
| X xrefs | list 0; DWG written by Wblock; attach + overlay → 2 Resolved, found, referenceCount 1; unload → Unloaded → bind refused (INVALID_ARGUMENT) → reload → Resolved; detach → 1 reference erased; bind insert-style → 0 xrefs, local block; missing file → ArgumentException |
| D undo | after a REGEN boundary, `U` (COM) reverts the whole last batch: count 23 → 26 → 23 |

## Review round (`plans/reports/code-review-2026-09-16-aec-phase-c.md`, 6/10 → every finding fixed)

| Finding | Fix | Pinned by |
|---|---|---|
| H1 hatch `SetDatabaseDefaults` after layer/colour → both discarded | defaults first, then the caller's layer/properties | live "H create … on layer S-COL" + polygon hatch "colour and layer kept" |
| H2 attribute on a locked layer → raw `eOnLockedLayer` (quarantine risk) | `ValidateAttributes` checks each attribute reference's layer before any `UpgradeOpen`; `WriteAttributesOp` two-phase + `AcadException` catch | live "B writeAttributes on an attribute whose own layer is locked → LAYER_LOCKED, changed.modified 0" |
| H3 64 KB breached (findReferences 500, attributes uncapped, error duplication, handles unbounded) | `MaxBatchItems` 200, `MaxReferenceLimit` 100, `MaxDefinitionLimit` 200, `MaxAttributeHandles` 100 / `MaxDynamicHandles` 20 (truncated + warning), attributes cut 8 × 120 chars, `Settle` lists 20 errors + a count, grouped warnings (`WarnItem`) | `A_full_batch_envelope_stays_under_the_result_cap_on_every_path` (3 paths at 200 items < 60 000 B), seed cap theories |
| M1 atomic update validated `set` while writing | `EntityUpdater.Validate` (no mutation) in phase 1 → structural refusal with every item error; `Apply` in phase 2 | live "U atomic + bad key on item 1 → refused structurally, item 0 untouched" |
| M2 phase-1 `UpgradeOpen` counted as modified | `OpenForEdit` (read) in phase 1, `Upgrade` in phase 2 | same check: `run.changed.modified == 0` |
| M3 single-item ops committed partial changes with `success=false` | policy: refuse the whole op (hatch update, setDynamic, detach validate first; a value AutoCAD rejects aborts the run) | live "H update with one bad key → whole update refused, colour unchanged", "X detach with one unknown name → refused whole" |
| M4 single creates threw `ArgumentException`, code lost | `EditResult.Refused` with the structured error (insert, annotation create, hatch create, attach) | live "B insert / A create / H create on a locked layer → structural LAYER_LOCKED refusal" |
| M5 insert dropped unknown tags silently; `attributesSet` counted all | `AppendAttributes` returns the unmatched tags as warnings; `attributesSet` = requested − unmatched | live "B insert with a misspelt tag → attributesSet 0, warning names the tag" |
| M6 `associative` unverified | `Associative = true` before `AppendLoop(ids)` (managed API has no persistent-reactor call) — verified: the hatch follows a moved boundary | live "H associative hatch follows its moved boundary" |
| M7 listDefinitions walked xref records | `HasAttributeDefinitions` gate, `entityCount` null for xrefs, `ct` in the loop | code |
| M8 ATTDEF deletable; block-definition entities editable | `OpenForEdit` refuses entities whose owner is not a layout (`UNSUPPORTED_ENTITY` naming the definition); ATTDEF/TOLERANCE removed from the annotation list | live "A delete of an attribute definition → UNSUPPORTED_ENTITY", "U entity inside a block definition → UNSUPPORTED_ENTITY" |
| M9 path policy | `XrefPathPolicy` (fully qualified, `.dwg`, no `..`, not self; UNC allowed with a warning), `ValidateSymbolName` on `name` | `XrefPathPolicyTests`, live "X invalid name and drive-relative path → ArgumentException each" |
| M10 non-atomic honesty | `changed` accumulated inside `Apply`, reported on failure | code (`BatchEditService.UpdateBatch` catch) |
| L1 names in `affectedHandles` | `modifiedCount` counts block-table records, names only in the summary | live "X unload … affectedHandles == []" |
| L2 unknown `set` keys were warnings; `rotate` without angle | errors in `Validate` | live "U atomic + bad key" (`colour`) |
| L3 area inbound conversion | mm² → drawing² | code |
| L5 hatch delete without per-item errors | `Settle` | code |
| L6 duplicate handles | `DistinctHandles` + warning | live "U duplicate handle → applied once with a warning" |
| L7 ops never run live | added: resolveStatus, polygon + hatchStyle, associative, batchUpdate, `space: Layout1`, dryRun for update / attributes / annotations / hatches / attach | live |
| L8 files > 300 lines | `BlockService` → 3 partials, `EntityUpdater` → 2, `HatchService` → 2 | `wc -l` |
| L9 / L10 / L11 / L12 | fresh warning list; boundary closedness = AutoCAD closed **or** shape closed within tolerance; example #5 reworded; locked-layer move documented in the description | — |

Also: `EntityFilter.From` now defaults `space` to `all` when `handles` are given (a handle is unique; a paper-space entity was invisible to `query_entities {filter: {handles}}`).

Live after the round: **62/62** (`run-aec-edit-tools-live.ps1`; run 1 after the fixes 56/62 — six harness expectations shifted by the new scene block and by summary mode lacking `color`); regression `run-aec-tools-live.ps1` 55/55.

## Known limitations (phase C)

- `setDynamic` is exercised only on the refusal path live (the harness cannot author a dynamic block through the API); the value conversion (mm ↔ drawing units, degrees ↔ radians, mm² ↔ drawing², allowed-value lists) is implemented against the documented `DynamicBlockReferenceProperty` contract but unverified on a real dynamic block.
- An atomic update lists the first refusal per item (`+N more refusal(s) on this item`), not every one.
- `attach` requires the file on disk at call time; a path saved relative by AutoCAD (`.\output\…`) is reported as `path` beside the resolved `foundPath`.
- Error messages that contain the user's profile path are redacted by the bridge (`<path>`), as in every other tool.

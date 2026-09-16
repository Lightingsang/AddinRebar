# ADR-02 — Units, tolerance, handles, response envelopes, error codes

**Status:** accepted 2026-09-16

## Units

- Tool inputs and outputs that are lengths are **millimetres**; areas mm²; angles degrees. This is the contract the 12 existing seeds and the
  bridge's `units` global already implement (`ScriptUnits` from INSUNITS, `AutocadInsunits` labels + note when INSUNITS is Unitless).
  The brief's `DrawingUnitService` = `ScriptUnits`; nothing new. `get_drawing_context` reports `units.label`, `mmPerUnit`, `insunits` so the
  AI can speak the user's unit.
- Deviation from the brief: no `{ value, unit }` objects on inputs in phase A (would double every schema); can be added later as an optional
  top-level `unit` per tool without breaking anything.

## Tolerance

`GeometryTolerance` (mm / degrees), defaults for building plans in mm drawings:

| Member | Default | Used by |
|---|---|---|
| `PointEquality` | 0.5 | point ==, duplicate endpoints |
| `EndpointConnection` | 10 | gap detection, connectivity, room closure |
| `Collinearity` | 1 | overlap of collinear segments, alignment |
| `ParallelAngle` | 0.5° | parallel / perpendicular / aligned |
| `Duplicate` | 1 | near-duplicate geometry |
| `TinySegment` | 5 | tiny / zero-length segments |
| `RoomGap` | 25 | room boundary closure (phase F) |

Tools take an optional `tolerance` object `{ pointEquality, endpointConnection, … }`; missing members fall back to the defaults. Services
receive the resolved record; no literal numbers in service code.

## Handles

- Public identity = AutoCAD **handle** (hex string). `HandleResolver.Resolve(db, tr, handle)` → `ObjectId` with a typed failure
  (`INVALID_HANDLE` unparsable/not found, `ERASED`, `NOT_AN_ENTITY`). Never expose `ObjectId` values.
- Every multi-handle input is resolved first; unresolved handles become `errors[]` entries (code + handle) and the tool proceeds with the rest
  unless `strict: true`.

## Envelopes (camelCase — the repo's JSON policy; the brief's snake_case examples are mapped 1:1)

```json
analysis: { "success": true, "summary": {…}, "items": [...], "count": 0, "truncated": false, "warnings": [], "errors": [] }
edit:     { "success": true, "createdCount": 0, "modifiedCount": 0, "deletedCount": 0, "affectedHandles": [], "warnings": [], "errors": [] }
error:    { "code": "INVALID_HANDLE", "message": "Entity handle A125 was not found.", "handle": "A125" }
```

`ToolErrorCode`: `INVALID_ARGUMENT`, `INVALID_HANDLE`, `ERASED`, `NOT_AN_ENTITY`, `UNSUPPORTED_ENTITY`, `NO_GEOMETRY`, `LAYER_LOCKED`,
`LAYER_FROZEN`, `LIMIT_EXCEEDED`, `NOT_CLOSED`, `INTERNAL`. `success` is true when the requested work was done (possibly with warnings); a
request that cannot start (missing required input, unknown relation) throws `ArgumentException` so the registry's stability window ignores it.

## Read-only and side effects

Query/analysis seeds declare `transaction: "none"`: the runner opens `tr`, always aborts it, and fails the run if the change counter saw a
modification — a query tool cannot modify the DWG even by mistake. Edit seeds declare `auto`, support `dryRun` through the existing request
flag, and document their side effects in the description.

## Output size

`AutocadResultSerializer` caps a result at 64 KB (then returns truncated text). Every list tool therefore has `limit` (default 100, max 500),
`offset`, and a `mode: summary|detail` switch; `truncated` + `count` tell the AI to page.

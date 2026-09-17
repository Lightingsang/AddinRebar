# 2026-09-17 — AEC engine phase I: change sets, and what a bridge cannot tell you

## What landed

- Phase I of `plans/260916-1140-aec-automation-mcp-autocad/`, the last: `begin_change_set`, `preview_change_set`, `get_change_summary`,
  `commit_change_set`, `rollback_change_set` over `HPAutoCad.Aec/ChangeSets/` + `Cad/ChangeSet*.cs`, and `changeSetId` on the 12 write
  tools. 62 tools on the AutoCAD server. Report: `reports/phase-I-change-sets-live.md`. **The plan is complete (A–I).**
- Review round 6/10 → fixed the same day; `ChangeSetTests` 9. Tests 225 + 280, live 109/109 + 90/90.

## Decisions worth remembering

- **Record the call, not a proposed state.** A change set holds write-tool calls (tool + `args.Raw.Clone()`); commit replays them through a
  table of readers that mirror the seed shims. The engine validates at record only what it can: the tool, the `op`, and every handle the
  call names — as an entity.
- **Snapshot at commit, at `ObjectOpenedForModify`.** The bridge's change counter already relies on that event firing before anything
  changes; a non-resident `Entity.Clone()` there is the exact original, and `RXObject.CopyFrom(clone)` puts it back under the same handle
  (a dimension then needs `RecomputeDimensionBlock`, a hatch `EvaluateHatch`). Erased entities come back with `Erase(false)`.
- **The bridge never tells the script it was a dryRun.** The drawing does: a commit whose created handles are all gone (or erased — an
  aborted run leaves what it appended as erased objects whose handles still resolve), whose erased ones are all back and whose modified
  ones all read as their snapshot was undone. Every class must agree (`UndoRule`); the fingerprint must see what the update tools change
  (linetype, hatch pattern, dynamic properties…), or a real commit reads as rolled back and its snapshots are thrown away.
- **Keep the undo until the drawing confirms.** A rollback with `dryRun`, or one that hits a locked layer, must not release the snapshots:
  the set goes back to (or stays) committed with a note, and `keep` is the way to finish a set whose work stays.
- **The managed `Database` wrapper is a new object on every request.** A store keyed by reference restarted on every call; key by the
  native pointer plus the fingerprint GUID, and drop on `DocumentToBeDestroyed` and on the bridge's terminate.
- **A replay abort is the caller's.** `ArgumentException`, naming the op — an engine exception would count against the tool's stability.

## Gotchas

- `query_entities` does not project a hatch's pattern; read it through the API when a harness needs it.
- The harness's detail-mode text height is `textHeightMm`, not `textHeight`.

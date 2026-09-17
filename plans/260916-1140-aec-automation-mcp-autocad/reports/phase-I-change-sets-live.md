# Phase I — Change sets: implementation + live verification report (2026-09-17)

## Implemented

| Piece | Files | Notes |
|---|---|---|
| Model + ledger (pure) | `HPAutoCad.Aec/ChangeSets/ChangeSet.cs` (`ChangeSetState` pending / committed / rolled_back / closed / discarded, `ChangeOp` with the raw args copied, one-line `Summary`, `Describe`; `CommitRecord` = created / modified / deleted handles, layers + block definitions created, the snapshot bag, `SnapshotsComplete`; `ChangeSet` with `Note`), `ChangeSetLedger.cs` (ids `CS-nnn`, caps `MaxSets` 20 live — at the cap the oldest committed set is closed —, `MaxOps` 200, `MaxArgsBytes` 64 KB, `MaxLabelChars` 120; transitions `MarkCommitted`, `RevertToPending`, `MarkRolledBack` (snapshots kept), `ConfirmRolledBack` (released), `RevertToCommitted`, `MarkRollbackIncomplete`, `Close`, `MarkDiscarded`), `ChangeSetEnvelopes.cs` (`UndoRule.WorkIsGone` — every class of handle must agree —, the commit / rollback envelopes grouped and capped through `EditResult.Settle` / first 20 failures + counts by code) | |
| Store | `Cad/ChangeSetStore.cs` — one ledger per open drawing keyed by `Database.UnmanagedObject` (the managed wrapper is new on every request) and checked against `FingerprintGuid` (a recycled address gets a fresh ledger); `Document` refreshed per call; dropped on `DocumentToBeDestroyed`; `DropAll` from the bridge's terminate | |
| Record | `Cad/ChangeSetRecorder.cs` — first line of every write tool: `changeSetId` → `WriteToolTable.Validate` (known tool, valid `op`, `manage_xrefs` attach only) + every handle the call names (`handles`, `items[].handle`, `issues[].handles`, `hatch.boundaryHandles`, `filter.handles` incl. `routes` / `hosts`) opened as an entity → recorded (`args.Raw.Clone()`), else refused; the record summary echoes the set's note and says a dryRun on the request does not undo a record | |
| Replay | `Cad/ChangeSetReplay.cs` — `WriteToolTable` (12 readers mirroring the seed shims, `changeSetId` null), `ChangeSetCommitter`: the ops in order under a `SnapshotBag`; an op that does not succeed → `ArgumentException` naming the op (the bridge aborts; never counts against stability) unless `atomic: false`; an `ArgumentException` from an op is re-thrown naming the op; layer + block tables diffed | |
| Snapshots + verifier | `Cad/ChangeSetSnapshots.cs` — `SnapshotBag` clones (`Entity.Clone()`, non-resident) at `ObjectOpenedForModify`, skipping HANDSEED-new entities, `MaxSnapshots` 2 000, `Unsnapshotted` counted; `Fingerprint` over layer / colour / linetype / lineweight / visibility and the geometry, text, style, hatch pattern, dynamic properties per type; `ChangeSetVerifier` — a committed set whose work is gone (all created gone or erased, all erased back, all modified reading as their snapshot) is pending again; a rolled-back set whose work is back is committed again (snapshots still in hand), one whose undo held releases them | |
| Rollback | `Cad/ChangeSetRollback.cs` — pending → discarded; committed + `keep` → closed; committed → created erased, erased un-erased (`Erase(false)`, same handle), modified restored (`CopyFrom(clone)`, same handle; dimensions `RecomputeDimensionBlock`, hatches `EvaluateHatch`); a failing handle (locked layer) is an error and the set stays committed with its snapshots | |
| Facade + seeds | `AecTools.ChangeSets.cs` (`BeginChangeSet`, `GetChangeSummary`, `PreviewChangeSet` paged `MaxChangeOpLimit` 30 × `MaxOpArgsChars` 500, `CommitChangeSet(atomic)`, `RollbackChangeSet(keep)`); seeds `ChangeSet/begin_change_set`, `preview_change_set`, `get_change_summary` (`none`), `commit_change_set`, `rollback_change_set` (`auto`); the 12 write seeds gained `changeSetId` (schema key + shim line) and their `AecTools` methods `(string? changeSetId, ScriptArgs args)`; `EditResult.CreatedHandles` (`[JsonIgnore]`); category ChangeSet; `BridgeEntry.Dispose` → `ChangeSetStore.DropAll()` | server 62 tools (4 + 8 + 50 seeds) |
| Tests | `ChangeSetTests` (9: ids / labels, the recorded op, caps, the state machine incl. rollback keeping snapshots until confirmed and close, the live cap closing the oldest commit, `UndoRule` theory, envelopes at every cap incl. a 200-op commit with 3 warnings per op + 29 failures and a 2 000-failure rollback) → `HPAutoCad.Aec.Tests` 225; `SeedLibraryTests` 50 seeds + replay table ⇔ write seeds + `changeSetId` on every write seed → `HPAutoCad.Mcp.Server.Tests` 280 | |
| Harness | `aec-edit-tools-live.py` step Z (13 checks) | |

## Build / tests

| Check | Result |
|---|---|
| `dotnet build HPAutoCad.slnx -c Debug` | 0 warnings; bundle deployed |
| `HPAutoCad.Aec.Tests` | 225/225 |
| `HPAutoCad.Mcp.Server.Tests` | 280/280 (50 seeds, 30 read-only) |
| `run-server-smoke.ps1` | 22/22 |

## Live (AutoCAD 2026, 2026-09-17)

`run-aec-edit-tools-live.ps1` **109/109** · `run-aec-tools-live.ps1` **90/90** (regression).

| Step | Verified |
|---|---|
| Z record | `begin_change_set` → `CS-001` pending, the write tools listed, nothing drawn; six write calls with `changeSetId` recorded as ops 1..6 (a create batch, an update of the room text, an mtext, a delete, a hatch pattern change, a dimension text override), nothing written; a bad handle in `items[].handle` and in `hatch.boundaryHandles`, an unknown `op` and `manage_xrefs bind` refused and not recorded |
| Z preview | 6 ops in order with tool, one-line summary, handles, raw args; the set pending with `byTool` |
| Z commit dryRun | the run reports 3 created / 3 modified / 1 deleted, `rolledBack: true`; the next call sees the rollback and the set is pending again with a note; nothing changed |
| Z commit | 3 created, the text `CHANGED BY SET` / 150, the hatch `SOLID`, the dimension `8.0 m`, the text erased (`ERASED` on query); committed, 4 snapshots, items carry `op N`; a second commit refused, a record into it refused |
| Z rollback dryRun | the undo runs (3 erased) then is rolled back; the next call sees it, the set is committed again with a note, `undoAvailable` true, the text still changed |
| Z rollback, layer locked | `A-TEXT` locked → `LAYER_LOCKED` on the modified text and the erased one, the rest undone, `success false`, `failedByCode`; the set stays committed with its snapshots and a note |
| Z rollback | layer unlocked → the created stay erased, the text restored (same handle, original text and height), the hatch back to `ANSI32`, the dimension text back, the erased text back with its handle; `rolled_back`, confirmed on the next call |
| Z keep | a committed set closed with `keep`: its circle stays, state `closed`, `undoAvailable` false, a later rollback refused |
| Z thirty | a 30-op set previewed (one page), committed → 30 entities in one run, one `U` reverts all 30 → the set reads pending again on the next call |
| Z documents | a second drawing (`Documents.Add`) has no sets and cannot see `CS-001`; back in the first drawing the three sets are still there (rolled_back, closed, pending) |
| Z errors | unknown set / empty set → `ArgumentException` naming the known sets / asking for ops |

## Review round (2026-09-17, `plans/reports/code-review-2026-09-17-aec-phase-i.md`, 6/10 → fixed)

| # | Finding | Fix | Pinned by |
|---|---|---|---|
| H1 | no exit from `committed`: the 21st commit refused every `begin_change_set`, 20 × 2 000 clones resident | `closed` state (`rollback_change_set keep`, and automatically for the oldest committed set at the live cap, warned by `begin_change_set`) | `At_the_live_cap_the_oldest_committed_set_is_closed…`; live Z keep |
| H2 | rollback released the snapshots before the undo was confirmed (dryRun, a locked layer) | snapshots kept through `rolled_back` until the drawing confirms; a rolled-back set whose work is back is committed again; a partial rollback leaves the set committed with a note | `The_state_machine…`; live Z (dryRun rollback, locked layer) |
| H3 | the verifier decided by the first class of handle and a fingerprint blind to most updates | `UndoRule`: every class must agree; `Fingerprint` over linetype / lineweight / colour / style / dimStyle / hatch pattern / dynamic properties / geometry per type | `The_undo_rule_needs_every_class…`; live Z (hatch, dimension) |
| M1, M2 | commit / rollback envelopes unbounded (111 KB / 185 KB probes) | `ChangeSetEnvelopes`: warnings grouped by message with op indices, first 20 errors + a count, failures by code | `Envelopes_at_the_caps…` |
| M3 | an atomic abort was an engine exception (stability) | `ArgumentException` naming the op; an op's own `ArgumentException` re-thrown naming the op | seed description pin |
| M4 | `manage_xrefs` detach / bind recorded, "rolled back" with nothing undone | refused at record (attach only); block definitions diffed and listed | live Z (bind refused) |
| M5 | only three handle keys checked at record, `op` typos waited for the commit | `DeclaredHandles` + `WriteToolTable.Validate`, entities only | live Z (boundaryHandles, bogus op) |
| M6 | a recycled database pointer inherited the sets; stale document name | `FingerprintGuid` checked per call; name refreshed | — |
| L1–L10 | literals, an op's own `atomic`, item index, dryRun + changeSetId, note not echoed, `Overflow` mislabel, surrogate cut, wording, dimension / hatch recompute, `DropAll` | constants; documented (the set's `atomic` governs); `op N` in `changed`; warned; echoed; `Unsnapshotted`; surrogate-safe cut; reworded; `RecomputeDimensionBlock` / `EvaluateHatch` + live; `BridgeEntry.Dispose` | tests / seeds / live |
| L11 | plan text "snapshot at record time" | snapshots are taken at commit time (an entity can change between record and commit) — noted in the phase file | — |

## Known limitations (phase I)

- Snapshots restore entities; layers and block definitions a commit added stay (listed). Xref ops other than attach cannot be recorded.
- `CopyFrom` restores an entity's data; a dimension's or hatch's display is recomputed, a dynamic block's definition is the one the snapshot referenced.
- A set that touched nothing (read ops recorded) cannot be told committed from rolled back.
- The record is in-process: a write tool called with `changeSetId` and `dryRun` still records.
- At most 2 000 snapshots per commit; beyond it a rollback erases what was created and warns.

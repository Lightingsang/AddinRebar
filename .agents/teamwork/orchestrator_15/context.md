# Context & Findings — HPGrasshopper Re-audit Remediation

## Findings Summary from Re-audit Report (261013-0600-hpgrasshopper-reaudit.md)

### P1 Findings (Milestone M1)
- **N1**: Seed rebar (`generate_rebar_curves`) creates rebar in a loop; if a subsequent set fails coplanarity check, it returns anonymous `success=false` instead of throwing. `RevitTransactionScope` sees normal return and commits prior created rebars.
- **N2**: `RevitTransactionScope.cs` calls rollback action on `ICanvasUndoScope`, discarding `CanvasRollbackResult.Success/Errors`, and logs "rolled back cleanly". When canvas rollback fails or is partial, outcome must report error and include specific rollback failures.
- **N3**: `GrasshopperExecutionEngine.cs` batch callback has `ct` but `CanvasEditor.ExecuteBatch` does not accept token or undo scope. It solves and commits directly without respecting token cancellation or deferring canvas commit to the Revit transaction boundary.
- **N4**: `CanvasUndoScope` baseline remove -> re-add: an object O existing before the scope, when removed then added back, gets its GUID placed in `_addedGuids`. During rollback, added objects are removed, skipping snapshot restore, causing baseline object O to be permanently deleted.
- **N5**: `McpBridgeWorkRequest.cs` default `RequireTargetIdentity = false`; no production caller sets true. Document paths between coordinator cache, context extractor, and validator are inconsistent regarding empty vs null strings.

### P2 Findings (Milestone M2)
- **N6**: `PlayerDataTreeHelper.cs` ignores boolean return of `AddVolatileData`. Mixed/invalid branches log/continue and return true if any item succeeds. Unsupported/null items are dropped, causing index shifting.
- **N7**: `PlayerOutputExtractor.cs` suffixes second output key, then re-aliases `result[matchedKey]`, overwriting earlier output with matching nickname.
- **N8**: `GrasshopperExecutionEngine.cs` extracts error string and throws, dropping original diagnostic payload (`errors`, `warnings`, `rejectedInputs`, `duration`). Also, `MapToExecuteResult` serializes without `BridgeJson.Options`, leading to PascalCase keys in serialized `JsonElement`.
- **N9**: `CanvasBatchValidator.cs` and `CanvasEditor.cs` return/no-op when a container property is not an array, instead of reporting a preflight validation error.
- **N10**: `GrasshopperContextExtractor.cs` mixes cached flags/units/selection with current coordinator title/path without timestamp or stale marker.

## Governance Directives
- McpShared is immutable (0 edits).
- C2 <= 300 lines, M3 <= 50 lines, Q-B1 0 dead code, Q-B2 0 unannotated catch.
- ADR-0007 12/12 embedded seeds pass quality walker.
- 100% filter-free tests pass.

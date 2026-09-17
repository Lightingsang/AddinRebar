# `execute_autocad_code` — script contract

The bridge compiles the body with Roslyn inside acad.exe (own load context) and runs it on AutoCAD's main thread inside **its own** transaction. What a script gets, what it must not do, and what comes back.

## Globals

| Name | Type | Notes |
|---|---|---|
| `doc` | `Document` | the active document (never `LockDocument` — the bridge holds the lock) |
| `db` | `Database` | `doc.Database` |
| `ed` | `Editor` | **no prompts** (`ed.GetPoint/GetEntity/GetSelection/GetString…` are denied); `ed.SelectAll()`/`SelectImplied()` are fine |
| `app` | `DocumentCollection` | `Application.DocumentManager` |
| `tr` | `Transaction` | the bridge's transaction: `tr.GetObject(id, OpenMode.ForRead\|ForWrite)`, `tr.AddNewlyCreatedDBObject(e, true)`; never `Commit`, `Abort`, `Dispose`, never `db.TransactionManager.StartTransaction()` |
| `units` | `ScriptUnits` | `units.ToDrawing(mm)` → drawing units, `units.ToMm(du)` → mm, `units.Label` — every API coordinate is in **drawing units**, every tool boundary in **mm** |
| `ct` | `CancellationToken` | check `ct.ThrowIfCancellationRequested()` in long loops (timeout 5–120 s is cooperative) |
| `log(string)` | | lines come back in `logs[]` |
| `progress(cur, total, msg)` | | |
| `args` | `ScriptArgs` | `args.Str("key", fallback)`, `Int`, `Double`, `Bool`, `Strings`, `Longs`, `List` (of `ScriptArgs`), `Obj`, `Has`, `Require` — case-insensitive, coercing |

Usings already present: `System`, `System.Linq`, `System.Collections.Generic`, `Autodesk.AutoCAD.ApplicationServices`, `Autodesk.AutoCAD.DatabaseServices`, `Autodesk.AutoCAD.EditorInput`, `Autodesk.AutoCAD.Geometry`, `Autodesk.AutoCAD.Colors`, `HPRebar.McpBridge.Core.Scripting`, `HPAutoCad.Aec` (the AEC engine facade `AecTools` the seeds call — a script may call it too, e.g. `AecTools.MeasureGeometry(db, ed, tr, units, ct, log, …)`).

## Request fields

| Field | Values | Meaning |
|---|---|---|
| `code` | C# body, ≤ 32 KB | must end with `return <value>;` (any serialisable object; anonymous objects are fine) |
| `transaction` | `none` \| `auto` \| `manual` | `none` = read-only: the bridge aborts its transaction after the run and **fails the run if anything changed**; `auto` = commit; `manual` is accepted and runs like `auto` (logged) |
| `dryRun` | bool | run for real, then roll the transaction group back; `rolledBack: true`, `changed` still counted |
| `label` | ≤ 64 chars | the undo entry `MCP: <label>` and the audit line |
| `timeoutSeconds` | 5–120 | a timeout fails the run and rolls back even if the script returned |
| `args` | object | reaches the script as `args` |

## Result envelope (every run: `execute_autocad_code`, `run_tool`, seeds)

```json
{"isError": false, "value": …, "valueType": "…", "message": "…", "logs": [], "diagnostics": [{"id": "GUARD|COMPILE|…", "message": "…"}],
 "changed": {"added": 0, "modified": 0, "deleted": 0}, "rolledBack": false, "timedOut": false, "durationMs": 7, "truncated": false, "runId": 500}
```

`changed` comes from HANDSEED + `ObjectOpenedForModify` + `IsErased` (approximate by construction: a container opened for write to receive an entity is not counted). A JSON-RPC error (`-32001` opt-in, `-32002` busy, `-32003` no drawing) is **not** a run: the tool's stability window ignores it; so does an `ArgumentException:` message.

## Guard (refused before the script runs, diagnostic `GUARD`)

`ed.Get*` prompts, `SendStringToExecute`, `MessageBox`/dialogs, `StartTransaction`/`Commit`/`Abort`/`Dispose` on `tr`, `LockDocument`, `await`/`Task`/`Thread`, `dynamic`, `unsafe`, namespaces `System.IO` / `System.Net` / `System.Reflection` / `System.Diagnostics.Process` / `System.Linq.Expressions` (also `global::`-prefixed), `Expression`/`Delegate`/`CreateDelegate`/`Compile`/`.Method`, `#r` / `#load`. Compile errors come back as diagnostic `COMPILE` with line/column.

## Patterns

**Handle ↔ ObjectId, entity by handle (read).**
```csharp
var id = db.GetObjectId(false, new Handle(Convert.ToInt64(args.Require("handle"), 16)), 0);
if (id.IsNull || id.IsErased) throw new ArgumentException($"handle {args.Str("handle")} is not an entity of this drawing.");
var e = (Entity)tr.GetObject(id, OpenMode.ForRead);
return new { handle = e.Handle.ToString(), type = e.GetType().Name, layer = e.Layer, boundsMm = new { min = units.ToMm(e.GeometricExtents.MinPoint.X), max = units.ToMm(e.GeometricExtents.MaxPoint.X) } };
```

**Walk model space, mm out (transaction none).**
```csharp
var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
var lines = new List<object>();
foreach (ObjectId id in ms)
{
    ct.ThrowIfCancellationRequested();
    if (id.IsErased || tr.GetObject(id, OpenMode.ForRead) is not Line l || l.Layer != args.Str("layer", "0")) continue;
    lines.Add(new { handle = l.Handle.ToString(), lengthMm = Math.Round(units.ToMm(l.Length), 1) });
    if (lines.Count >= args.Int("limit", 200)) break;
}
return new { count = lines.Count, items = lines };
```

**Create on a layer, mm in (transaction auto; dryRun first).**
```csharp
var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
var layer = args.Str("layer", "0");
if (!lt.Has(layer)) throw new ArgumentException($"layer '{layer}' does not exist (create_layer first).");
var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
var line = new Line(new Point3d(units.ToDrawing(args.Double("x1")), units.ToDrawing(args.Double("y1")), 0), new Point3d(units.ToDrawing(args.Double("x2")), units.ToDrawing(args.Double("y2")), 0));
line.SetDatabaseDefaults(db); // before Layer/Color — it resets them
line.Layer = layer;
ms.AppendEntity(line); tr.AddNewlyCreatedDBObject(line, true);
return new { handle = line.Handle.ToString() };
```

**Modify by handle with the layer guard (transaction auto).**
```csharp
var id = db.GetObjectId(false, new Handle(Convert.ToInt64(args.Require("handle"), 16)), 0);
var e = (Entity)tr.GetObject(id, OpenMode.ForRead);
var ltr = (LayerTableRecord)tr.GetObject(e.LayerId, OpenMode.ForRead);
if (ltr.IsLocked || ltr.IsFrozen) throw new ArgumentException($"layer '{ltr.Name}' is locked or frozen.");
e.UpgradeOpen();
e.ColorIndex = args.Int("colorIndex", 1);
return new { handle = e.Handle.ToString(), changed = new[] { "color" } };
```

## Rules of thumb

- Prefer a seed: `query_entities` / `measure_geometry` / `create_entities_batch` / `update_entities_batch` already do the handle resolution, layer guard, mm conversion, paging and structured errors — a script is for what no tool covers.
- Every output field a caller may feed back is a **handle string**; never return `ObjectId` or native pointers.
- Caller mistakes → `throw new ArgumentException("…")` (never counts against stability); engine faults → any other exception.
- Keep results under ~60 KB: `limit` + `offset`, `Take`, counts instead of lists.
- `SetDatabaseDefaults` **before** setting layer/colour; `Hatch.Associative = true` before `AppendLoop`; `Hatch.Area` is not readable in the creating transaction.

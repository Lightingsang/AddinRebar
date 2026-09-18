# `execute_civil3d_code` — script contract

What a C# script sees inside the HPCivil3d MCP Bridge (the bundle loaded in `acad.exe /product C3D`), how the bridge polices it, and what comes back. Source of truth: `HPCivil3d/HPCivil3d.McpBridge/` (`Civil3dScriptRunner`, `MainThreadExecutor`, `Civil3dUnitTable`, `Civil3dResultSerializer`), `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs` (`Civil3d`), `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs` (`Civil3dImports`/`Civil3dGlobals`). The AutoCAD MCP contract applies unchanged; only the deltas below are Civil.

## Environment

| Global | Type | Notes |
|---|---|---|
| `doc` | `Document` | the active drawing (`doc.Name`, `doc.Database`) |
| `db` | `Database` | `db.Filename`, `db.Insunits`, `db.CurrentSpaceId`, block/layer tables |
| `ed` | `Editor` | **only** `WriteMessage`, `SelectImplied`, `SelectAll` — every prompt (`GetPoint`, `GetEntity`, `GetSelection`, `SelectWindow`…) is guard-denied |
| `app` | `Application` | `DocumentManager` — never `Quit`, `ShowModalDialog`, `SendStringToExecute` |
| `tr` | `Transaction` | the bridge's inner transaction: `tr.GetObject(id, OpenMode.ForRead/ForWrite)`, `AppendEntity` + `tr.AddNewlyCreatedDBObject(obj, true)`. **Never** `Commit`/`Abort`/`Dispose` it, never `StartTransaction`/`LockDocument` |
| `civil` | `CivilDocument` | `GetAlignmentIds()`, `GetSurfaceIds()`, `GetPipeNetworkIds()`, `GetPressurePipeNetworkIds()`, `GetSiteIds()`, `GetSitelessAlignmentIds()`, `CorridorCollection`, `CogoPoints`, `PointGroups`, `Settings`, `Styles`. Every drawing opened in Civil 3D has one (Civil settings are created lazily) — the null check in seeds is the defensive path |
| `units` | `ScriptUnits` | the **Civil drawing unit**: `units.Label` = `Meters` \| `Feet`, `units.MmPerUnit` = 1000 \| 304.8, `units.ToDrawing(mm)`, `units.ToMm(du)`, `units.Note` (non-null when INSUNITS disagrees) |
| `ct` | `CancellationToken` | `ct.ThrowIfCancellationRequested()` inside loops; `cancel_execution` and the timeout act at those checks |
| `log(string)` | | lines come back in `logs[]` |
| `progress(int cur, int total, string msg)` | | progress notifications |
| `args` | `ScriptArgs` | the JSON object passed as `args` in the call (see below) |

Default usings: `System`, `System.Linq`, `System.Collections.Generic`, `Autodesk.AutoCAD.ApplicationServices`, `Autodesk.AutoCAD.DatabaseServices`, `Autodesk.AutoCAD.EditorInput`, `Autodesk.AutoCAD.Geometry`, `Autodesk.AutoCAD.Colors`, `Autodesk.Civil`, `Autodesk.Civil.ApplicationServices`, `Autodesk.Civil.DatabaseServices`, `Autodesk.Civil.DatabaseServices.Styles`, `Autodesk.Civil.Settings`, `HPRebar.McpBridge.Core.Scripting`. Language: C# script (Roslyn), max 32 KB, must end with a top-level `return <value>;`.

**Ambiguity rule:** both `Autodesk.AutoCAD.DatabaseServices` and `Autodesk.Civil.DatabaseServices` define `Entity`, `DBObject`, `Surface`. Write the Civil ones in full — `Autodesk.Civil.DatabaseServices.Entity`, `Autodesk.Civil.DatabaseServices.Surface`, `Autodesk.Civil.DatabaseServices.TinSurface` — or the script fails to compile with CS0104. `Alignment`, `Profile`, `Corridor`, `CogoPoint`, `Parcel`, `Site`, `Network`, `Pipe`, `Structure`, `PointGroup` are unambiguous.

### `ScriptArgs` (case-insensitive keys, coercing)

`Str(key, fallback=null)`, `Int(key, fallback=0)`, `Long`, `Double(key, fallback=0)`, `Bool(key, fallback=false)`, `Require(key)` (throws `ArgumentException` when missing/empty), `Has(key)`, `Strings(key)` / `Doubles(key)` / `Longs(key)`, `List(key)` (array of objects — each item is a `ScriptArgs` again: `points[i].Double("x")`), `Obj(key)`. Numbers arrive as JSON numbers or numeric strings; `Int` rounds.

## Units — the rule every script follows

1. **Plan x/y and lengths cross the tool boundary in millimetres.** Convert on the way in (`units.ToDrawing(mm)`) and on the way out (`units.ToMm(du)`).
2. **Stations, elevations, areas stay in the Civil drawing unit** and every envelope carries `drawingUnit = units.Label` (areas as `m²`/`ft²`: say `areaUnit`).
3. The Civil unit comes from the drawing settings (`civil.Settings.DrawingSettings.UnitZoneSettings.DrawingUnits`), **not** INSUNITS. A drawing without Civil settings (opened from `acad.dwt` inside Civil 3D) reports **Feet** whatever INSUNITS says → `get_civil3d_context.civil3d.insunitsMismatch = true`. Scripts follow the Civil unit; warn the user before writing coordinates into such a drawing.
4. US survey feet = feet (relative tolerance 1e-4); coordinate system code `"."` = no zone.

## Transactions and undo

| `transaction` | Behaviour |
|---|---|
| `auto` (default) | the bridge opens `tr`, the script runs, `tr` is committed on return — one undo entry `MCP: <label>` |
| `none` | read-only: `tr` is opened and always aborted; if `changed` sums to anything the run is an error ("The script modified the drawing with transaction=\"none\"") |
| `manual` | ≡ `auto` + a log line (the script never owns a transaction) |
| `dryRun: true` | runs everything, rolls the outer transaction back, still reports `changed` — the preview for any write |

`Abort()`/`U` roll back every Civil object tried so far: `CogoPoints.Add`, `Alignment.Create`, `TinSurface.AddVertex`, even a corridor rebuild (a corridor never disappears). `U` in Civil 3D reverts every AI run since the user's last own command. `changed{added, modified, deleted}` is counted from HANDSEED + `ObjectOpenedForModify` + `IsErased`; a Civil operation that touches many dependent objects reports `modified` > 1 (an alignment create shows its labels).

## Guard (syntax, before compile — diagnostic `GUARD`)

Denied everywhere (base list): namespaces `System.IO` (except `System.IO.Path`), `System.Net`, `System.Reflection`, `System.Diagnostics.Process`, `System.Linq.Expressions`, `System.Windows.Forms`, `Autodesk.AutoCAD.Interop`, `HPRebar.McpBridge.Core.Host` (also as `global::…`); identifiers `File`, `Directory`, `Process`, `Thread`, `Task`, `Parallel`, `Timer`, `Assembly`, `Activator`, `Marshal`, `Expression`, `Delegate`, `HttpClient`, `Registry`, `MessageBox`, `SystemObjects`; keywords `await`, `dynamic`, `unsafe`; directives `#r`, `#load`; member calls `.GetProperty/.GetMethod/.CreateDelegate/.Compile/.Method`. AutoCAD profile: every `ed.Get*`/`Select*`/`Drag`/`DoPrompt`, `SendStringToExecute`, `Command`, `CommandAsync`, `ExecuteInApplicationContext`, `ExecuteInCommandContextAsync`, `ShowModalDialog`/`ShowModalWindow`/`ShowAlertDialog`, `Quit`, `CloseAndDiscard`, `CloseAndSave`, `StartTransaction`, `StartOpenCloseTransaction`, `TopTransaction`, `LockDocument`, `tr.Commit`/`tr.Abort`/`tr.Dispose`.

**Civil profile adds:** `.Rebuild(`, `.RebuildAll`, `.RebuildSnapshot` (corridors, surfaces, networks — a rebuild prompts, can take minutes on a real corridor and its time is unmeasured; `RebuildAutomatic` as a property is fine), `DataShortcuts` and every data-shortcut member (`SetWorkingFolder`, `SetCurrentProjectFolder`, `CreateProjectFolder`, `AssociateDSProject`, `CreateReference`, `CreatePartialReferenceSurface`, `UpdatePartialReferenceSurface`, `RepairBrokenDRef`, `CreateDataShortcutManager`, `SaveDataShortcutManager`), `SurveyProject`/`SurveyProjectCollection`/`SurveyProjects`, `ExportTo*` (`ExportToDEM`…), `CreateFromLandXML`/`CreateFromTin`/`CreateFromDEM`/`CreateFromIMX`, `ImportPoints`/`ExportPoints`, `CreateSolidsAt*ToFile`, namespaces `Autodesk.Civil.DataShortcuts`, `Autodesk.Civil.AeccUiMgd`, `Autodesk.AECC.Interop`. A refusal names the line and column; nothing runs.

## Result

`{isError, value, valueType, message, logs[], diagnostics[{id, line, column, message}], changed{added, modified, deleted}, rolledBack, timedOut, durationMs, truncated, runId, hint}`. `value` is the serialised `return`: anonymous objects, lists, dictionaries, arrays, primitives; `Entity` → `{handle, type, layer, dxfName, name}` (Civil entities carry their `Name`), `CogoPoint` adds `number/x/y/elevation`, `Point3d` → `{x, y, z}` (all **drawing units** — convert with `units.ToMm` yourself when you want mm), `AlignmentEntity`/`SubEntity` and any `StyleBase` by name. Output is capped at 64 KB: past the cap the whole `value` degrades to one string with `truncated: true` — page with `limit`/`offset` instead. `runId` feeds `get_run` and `propose_tool.sourceRunId`.

## Conventions the seeds follow (and reviewers expect)

1. Bad caller input → `throw new ArgumentException("…")` (unknown alignment, duplicate name, layer that does not exist). Never counted against a tool's stability.
2. Resolve `name or handle` uniformly: compare `id.Handle.ToString()` then `((Autodesk.Civil.DatabaseServices.Entity)tr.GetObject(id, OpenMode.ForRead)).Name`, case-insensitive.
3. Per-item problems go to `errors[{code, message, handle}]` or `items[i].ok = false` — one bad point must not fail 499 good ones (`get_surface_elevation`: `OUTSIDE_SURFACE`).
4. Envelope: `{success, summary, count, offset?, truncated?, drawingUnit, lengthUnit: "mm", items, warnings[], errors[]}` for reads; `{success, summary, createdCount, modifiedCount, deletedCount, drawingUnit, items, affectedHandles, warnings, errors}` for writes.
5. Cap output: `limit` ≤ the seed's schema maximum (180 alignments/profiles, 250 parcels, 300 COGO points, 150 alignment entities, 200 samples, 100 parts) — those numbers come from measured bytes per item under the 64 KB cap.
6. `ct.ThrowIfCancellationRequested()` in every loop over drawing objects.
7. Never `Rebuild`, never a file path, never a data shortcut from a stored tool.

## Sample scripts

Read — count COGO points by raw description (`transaction: "none"`, `args: {"description": "GRND"}`):

```csharp
string wanted = args.Require("description");
int count = 0;
foreach (ObjectId id in civil.CogoPoints)
{
    ct.ThrowIfCancellationRequested();
    var p = (CogoPoint)tr.GetObject(id, OpenMode.ForRead);
    if (string.Equals(p.RawDescription, wanted, StringComparison.OrdinalIgnoreCase)) count++;
}
return new { description = wanted, count, drawingUnit = units.Label };
```

Read — alignment length and stations in mm/drawing units (`args: {"alignment": "Centerline (1)"}`):

```csharp
string key = args.Require("alignment");
foreach (ObjectId id in civil.GetAlignmentIds())
{
    var a = (Alignment)tr.GetObject(id, OpenMode.ForRead);
    if (!string.Equals(a.Name, key, StringComparison.OrdinalIgnoreCase) && !string.Equals(id.Handle.ToString(), key, StringComparison.OrdinalIgnoreCase)) continue;
    return new { handle = id.Handle.ToString(), a.Name, lengthMm = units.ToMm(a.Length), startStation = a.StartingStation, endStation = a.EndingStation,
                 startLabel = a.GetStationStringWithEquations(a.StartingStation), drawingUnit = units.Label, entities = a.Entities.Count };
}
throw new ArgumentException($"No alignment '{key}' (name or handle).");
```

Write — add a COGO point at plan mm with an elevation in drawing units (`transaction: "auto"`, preview with `dryRun: true` first):

```csharp
var location = new Point3d(units.ToDrawing(args.Double("xMm")), units.ToDrawing(args.Double("yMm")), args.Double("elevation", 0));
ObjectId id = civil.CogoPoints.Add(location, args.Str("description", "MCP"), true);
var created = (CogoPoint)tr.GetObject(id, OpenMode.ForRead);
log($"COGO point {created.PointNumber} added");
return new { handle = id.Handle.ToString(), number = created.PointNumber, elevation = created.Elevation, drawingUnit = units.Label };
```

Read — surface elevation with the Civil exception handled:

```csharp
var surface = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(civil.GetSurfaceIds()[0], OpenMode.ForRead);
try { return new { surface.Name, elevation = surface.FindElevationAtXY(units.ToDrawing(args.Double("xMm")), units.ToDrawing(args.Double("yMm"))), drawingUnit = units.Label }; }
catch (PointNotOnEntityException) { return new { surface.Name, ok = false, error = "OUTSIDE_SURFACE" }; }
```

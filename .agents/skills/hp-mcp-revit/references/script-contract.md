# `execute_revit_code` — script contract

The bridge (an add-in in its own assembly load context inside Revit.exe) guard-checks and compiles the body with Roslyn on the pipe thread, then runs it on Revit's API thread through an `ExternalEvent`, inside the transaction policy the request asked for. What a script gets, what it must not do, and what comes back.

## Globals

| Name | Type | Notes |
|---|---|---|
| `doc` | `Document` | the active document — the only one a script sees |
| `uidoc` | `UIDocument` | `uidoc.Selection.GetElementIds()` / `SetElementIds(ids)` are fine; **no `PickObject`/`PickPoint`** (nobody is in front of Revit for this run — it hangs until the timeout) |
| `app` | `Autodesk.Revit.ApplicationServices.Application` | `app.VersionNumber`, `app.Documents` |
| `uiapp` | `UIApplication` | |
| `ct` | `CancellationToken` | timeout or `cancel_execution`; check `ct.ThrowIfCancellationRequested()` inside long loops (5–120 s, cooperative) |
| `log(string)` | | lines come back in `logs[]` (200 max; the rest is counted) |
| `progress(cur, total, msg)` | | forwarded to the MCP client as a progress notification |
| `args` | `ScriptArgs` | `args.Str/Double/Int/Long/Bool("key", fallback)`, `DoubleOrNull/IntOrNull/LongOrNull`, `Strings/Doubles/Longs("key")`, `List("key")` (of `ScriptArgs`), `Obj("key")`, `Has`, `Require("key")`, `RequireDouble("key")` — keys case-insensitive, values coerced (`"150"` → 150) |

Usings already present: `System`, `System.Linq`, `System.Collections.Generic`, `Autodesk.Revit.DB`, `Autodesk.Revit.UI`, `Autodesk.Revit.DB.Structure`, `HPRebar.McpBridge.Core.Scripting` (so a helper can take `ScriptArgs` as a parameter — spell it `HPRebar.McpBridge.Core.Scripting.ScriptArgs` in full to stay compatible with a bridge deployed before that import existed). Anything else — `Autodesk.Revit.DB.Architecture` (Room, RoomTag), `.Mechanical`, `.Plumbing`, `.Electrical`, `.Analysis` — needs the full name or a `using` line at the top of the script.

## Request fields

| Field | Values | Meaning |
|---|---|---|
| `code` | C# body, ≤ 32 KB | must end with `return <value>;` (any serialisable object; anonymous objects are fine) |
| `transaction` | `auto` \| `manual` \| `none` | `auto`: `TransactionGroup "MCP: <label>"` + one `Transaction` around the whole script, committed after it returns (Revit **warnings** are dismissed automatically, an **error** fails the commit → "Revit rejected the change"); `manual`: the group only — the script opens/commits its own `Transaction`s (leaving one open fails the run); `none`: no group, no transaction — any modification throws `ModificationOutsideTransactionException` and fails the run |
| `dryRun` | bool | run for real, then `group.RollBack()`; `rolledBack: true`, `changed` still counted (`DocumentChanged` fires at the inner commit) |
| `label` | ≤ 64 chars | the undo entry `MCP: <label>` and the audit line |
| `timeoutSeconds` | 5–120 | a timeout **always fails the run and rolls back**, even when the script returned in time but the token had fired |
| `args` | object | reaches the script as `args` |

Refused **before** the script runs (an `isError` result, not a JSON-RPC error): no document open; document read-only; family document (bridge setting `AllowFamilyDocuments` is off by default); `doc.IsModifiable` — Revit already has a transaction open because the user is inside a command or an edit mode. Guard and compile failures come back as `diagnostics` (`GUARD` / `COMPILE`, with line and column). One script at a time: a second call while one runs is `-32002`.

## Result envelope (every run: `execute_revit_code`, `run_tool`, seeds)

```json
{"isError": false, "value": …, "valueType": "object", "message": null, "logs": [], "diagnostics": [{"line": 3, "column": 5, "id": "CS0103", "message": "…"}],
 "changed": {"added": 0, "modified": 0, "deleted": 0}, "rolledBack": false, "timedOut": false, "durationMs": 120, "truncated": false, "runId": 512}
```

`changed` is what `DocumentChanged` reported during the run (regeneration side effects count too). `value` > 64 KB is cut to a text head with `truncated: true` — return counts and `Take(n)`, not whole collections. A JSON-RPC error (`-32001` opt-in off, `-32002` busy) is **not** a run: the tool's stability window ignores it; so does a message starting with `ArgumentException:`.

## How Revit objects serialise

| Returned | JSON |
|---|---|
| `ElementId` | number (`id.Value`, long) |
| `XYZ` | `{x, y, z}` **in feet** — convert to mm yourself before returning |
| `Element` (Wall, Level, FamilyInstance…) | `{id, name, category, type}` — never its property graph |
| `Category` | `{id, name}` |
| `Parameter` | `{name, value (AsValueString ?? AsString), storageType}` |
| other `Autodesk.Revit.*` object | `{type, text}` from `ToString()` |
| anonymous object / `List<…>` / `Dictionary<string, …>` | plain JSON (depth ≤ 8, cycles ignored) |

## Units

The Revit API works in **feet** (lengths), square feet, cubic feet, radians. Every tool boundary in this MCP is **millimetres**; a script should be too:

```csharp
double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
double Mm(double ft) => Math.Round(UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters), 1);
double M2(double sqFt) => Math.Round(UnitUtils.ConvertFromInternalUnits(sqFt, UnitTypeId.SquareMeters), 3);
```

`get_revit_context.units.length` is only the *display* unit of the project; it never changes what the API expects.

## Guard (refused before the script runs, diagnostic `GUARD`)

Namespaces `System.IO` (except `System.IO.Path`), `System.Net`, `System.Reflection`, `System.Diagnostics.Process`, `System.Runtime.InteropServices`, `System.Runtime.Loader`, `System.Threading.Tasks`, `System.Security`, `System.Linq.Expressions`, `Microsoft.CodeAnalysis`, `Microsoft.Win32` (also `global::`-prefixed, also as string fragments); identifiers `Process`, `Thread`, `ThreadPool`, `Task`, `Parallel`, `Timer`, `AppDomain`, `Assembly`, `Activator`, `Marshal`, `MethodInfo`/`PropertyInfo`/…, `File`, `Directory`, `FileStream`, `StreamReader/Writer`, `Registry`, `HttpClient`, `WebClient`, `Socket`, `Expression`, `Delegate`; members `.GetMethod(s)`, `.GetProperty(ies)`, `.GetField(s)`, `.GetMember(s)`, `.GetConstructor(s)`, `.GetTypes`, `.InvokeMember`, `.DynamicInvoke`, `.CreateInstance`, `.LoadFrom/.LoadFile`, `.Exit`, `.FailFast`, `.Assembly`, `.Module`, `.CreateDelegate`, `.Compile`, `.Method`; `await`, `dynamic`, `unsafe`, `#r`, `#load`. The Revit profile adds nothing on top — Revit-specific care (no interactive picks, no `TaskDialog`, no `doc.Save*`/`Close`) is policy, not guard.

## Patterns

**Count / list with a collector (transaction none).**
```csharp
var walls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).WhereElementIsNotElementType().Cast<Wall>()
    .Where(w => w.LevelId.Value == args.Long("levelId")).ToList();
double Mm(double ft) => Math.Round(UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters), 1);
return new {
    count = walls.Count,
    items = walls.Take(args.Int("limit", 100)).Select(w => new {
        id = w.Id.Value, type = w.WallType.Name,
        lengthMm = Mm(w.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH)?.AsDouble() ?? 0),
        heightMm = Mm(w.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ?? 0) })
};
```

**Read parameters by name, mm out (transaction none).**
```csharp
var ids = args.Longs("elementIds").Select(v => new ElementId(v)).ToList();
var names = args.Strings("parameters");
var rows = new List<object>();
foreach (var id in ids)
{
    ct.ThrowIfCancellationRequested();
    var e = doc.GetElement(id) ?? throw new ArgumentException($"element {id.Value} does not exist.");
    var values = new Dictionary<string, string?>();
    foreach (var n in names)
    {
        var p = e.LookupParameter(n) ?? (e.Document.GetElement(e.GetTypeId()) as Element)?.LookupParameter(n);
        values[n] = p == null ? null : p.StorageType == StorageType.String ? p.AsString() : p.AsValueString();
    }
    rows.Add(new { id = id.Value, name = e.Name, category = e.Category?.Name, values });
}
return rows;
```

**Set a parameter on many elements (transaction auto; dryRun first).**
```csharp
var value = args.Require("value");
var ids = args.Longs("elementIds").Select(v => new ElementId(v)).ToList();
int set = 0, skipped = 0;
foreach (var id in ids)
{
    var p = doc.GetElement(id)?.LookupParameter(args.Str("parameter", "Comments"));
    if (p == null || p.IsReadOnly || p.StorageType != StorageType.String) { skipped++; continue; }
    p.Set(value); set++;
}
log($"set {set}, skipped {skipped}");
return new { set, skipped };
```

**Create with a level lookup, mm in (transaction auto).**
```csharp
double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();
var level = levels.OrderBy(l => Math.Abs(l.Elevation - Ft(args.Double("baseLevel", 0)))).First();
var type = new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().First(t => t.Kind == WallKind.Basic);
var line = Line.CreateBound(new XYZ(Ft(args.Double("x1")), Ft(args.Double("y1")), level.Elevation),
                            new XYZ(Ft(args.Double("x2")), Ft(args.Double("y2")), level.Elevation));
var wall = Wall.Create(doc, line, type.Id, level.Id, Ft(args.Double("heightMm", 3000)), 0, false, false);
return new { id = wall.Id.Value, type = type.Name, level = level.Name };
```

**Two steps that need a regenerate in between (transaction manual).**
```csharp
using (var t = new Transaction(doc, "place instance")) { t.Start(); /* create */ t.Commit(); }
doc.Regenerate();
using (var t = new Transaction(doc, "read back geometry")) { t.Start(); /* uses the regenerated geometry, writes again */ t.Commit(); }
return "done";
```

## Rules of thumb

- Prefer a seed: `ai_element_filter` / `get_available_family_types` / `create_*` / `operate_element` already resolve types, levels, hosts, mm conversion, paging and structured warnings — a script is for what no tool covers.
- Every output id is `e.Id.Value` (long); never return `ElementId` objects inside strings or `UniqueId` unless asked.
- Caller mistakes (id does not exist, wrong category, empty list) → `throw new ArgumentException("…")` (never counts against a tool's stability); engine faults → any other exception.
- Keep results under ~60 KB: `limit` + `Take`, counts instead of lists, `log` for progress.
- `FamilySymbol.Activate()` before the first `NewFamilyInstance` of that type; `doc.Regenerate()` (or a second transaction under `manual`) before reading geometry the same script just created.
- Reading a type parameter: `doc.GetElement(e.GetTypeId()).LookupParameter(name)`; built-ins through `get_Parameter(BuiltInParameter.X)` are locale-independent — prefer them over display names.

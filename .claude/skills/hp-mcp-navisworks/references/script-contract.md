# `execute_navis_code` — script contract

The bridge (a `net48` plugin inside Roamer.exe) guard-checks the body, runs the heavy gate, compiles it with Roslyn on the pipe thread and executes it on Navisworks' main thread from `Application.Idle`, inside the one transaction the bridge owns. What a script gets, what it must not do, and what comes back.

## Globals

| Name | Type | Notes |
|---|---|---|
| `doc` | `Document` | the active document: `doc.Models`, `doc.CurrentSelection`, `doc.SelectionSets`, `doc.SavedViewpoints`, `doc.CurrentViewpoint`, `doc.GetClash()` (Manage), `doc.GetTimeliner()`, `doc.Units`, `doc.Title`, `doc.FileName`, `doc.IsClear` |
| `app` | `NavisApp` | `Year` (2026), `Version` ("23.0"), `HasClashModule`, `IsModified`, `Documents`, `MainDocument`, `IsAutomated` — the static `Application` class is wrapped so `Application.Gui` (dialog parenting) stays out of reach |
| `units` | `ScriptUnits` | `units.ToMm(du)`, `units.ToDrawing(mm)`, `units.Label` — the API works in the **document's units** (`doc.Units`: Meters, Millimeters, Feet…); every tool boundary is mm |
| `ct` | `CancellationToken` | timeout or `cancel_execution`; `ct.ThrowIfCancellationRequested()` inside loops — a script that ignores it **blocks Navisworks** until it returns |
| `log(string)` | | lines come back in `logs[]` (200 max) |
| `progress(cur, total, msg)` | | forwarded to the MCP client |
| `args` | `ScriptArgs` | `args.Str/Double/Int/Long/Bool("key", fallback)`, `Strings/Doubles/Longs("key")`, `List("key")`, `Obj("key")`, `Has`, `Require`, `RequireDouble` — case-insensitive, coercing |

Usings already present: `System`, `System.Linq`, `System.Collections.Generic`, `Autodesk.Navisworks.Api`, `Autodesk.Navisworks.Api.DocumentParts`, `Autodesk.Navisworks.Api.Clash`, `Autodesk.Navisworks.Api.Timeliner`, `HPRebar.McpBridge.Core.Scripting` (spell `HPRebar.McpBridge.Core.Scripting.ScriptArgs` in full when a helper takes it as a parameter).

**Runtime is .NET Framework 4.8** (Roamer hosts CLR 4.8): no `Span<T>`, `Random.Shared`, `async`/`await`, positional `record` (no `IsExternalInit`), `string.Contains(string, StringComparison)`, `Math.Clamp`. Pattern matching, `is not`, local functions, tuples, `?.`, string interpolation are fine (Roslyn 5.9 compiles the language; the BCL is the limit). Use `Math.Min(Math.Max(v, lo), hi)`, `IndexOf(..., StringComparison.OrdinalIgnoreCase) >= 0`.

## Request fields

| Field | Values | Meaning |
|---|---|---|
| `code` | C# body, ≤ 32 KB | must end with `return <value>;` |
| `transaction` | `auto` \| `none` \| `manual` | `auto`: the bridge opens its transaction, the script runs, the transaction is committed as one Undo entry `MCP: <label>` (`(2)`, `(3)` appended when the top entry already carries that label); `none`: read-only — a fingerprint of sets / viewpoints / models / selection / clash tests / `NextUndo` / `IsModified` is compared before and after; a change fails the run (and is rolled back when it left a bridge undo entry); `manual`: accepted, behaves like `auto` (a log line says so) |
| `dryRun` | bool | commit, then `doc.Rollback()` **only if** the top undo entry is the bridge's own and something changed; `rolledBack: true` then. Refused up front when the script names a heavy member |
| `label` | ≤ 64 chars | undo entry + audit; whitespace collapsed, long labels cut with `…` |
| `timeoutSeconds` | 5–120, up to 600 while heavy operations are allowed | above the ceiling → clamped, a log line says so; a timeout fails the run |
| `args` | object | reaches the script as `args` |

Refused before the script runs (an `isError` result): `doc.IsActiveTransaction` ("Navisworks already has a transaction open"); heavy call + `dryRun`. JSON-RPC errors instead of a run: `-32001` execution opt-in off, `-32002` busy after the 8 s grace ("Navisworks is running a command or showing a dialog. Press ESC or close the dialog in Navisworks and retry." — a modal dialog, a file load, a clash run, or another script), `-32003` no model (`doc.IsClear`). Guard / compile / heavy failures come back as `diagnostics` (`GUARD` / `COMPILE` / `HEAVY`, line and column).

## Result envelope (every run: `execute_navis_code`, `run_tool`, seeds)

```json
{"isError": false, "value": …, "valueType": "object", "message": null, "logs": [], "diagnostics": [],
 "changed": {"added": 1, "modified": 0, "deleted": 0}, "rolledBack": false, "timedOut": false, "durationMs": 40, "truncated": false, "runId": 77}
```

`changed` is the fingerprint diff (a new set or viewpoint = `added`, a colour override or a status change = `modified`); it is reported even when the run was rolled back. Output is streamed through a bounded buffer (~64 KB cap, `truncated: true` beyond it); Navisworks collections are cut at **200 items**; `BoundingBox3D` serialises in mm; `ModelItem` as `{displayName, className, instanceGuid, …}`; `VariantData` by its kind.

## Undo semantics worth knowing

- Only an entry the bridge created is ever rolled back — never the user's last edit (`NavisUndoDecision.IsOurs`: committed, top-of-stack label equals ours, and it differs from the top before the run).
- A run that changed nothing under `auto` + `dryRun` answers `rolledBack: false` with the log "dry run: the script produced no undoable change; nothing to roll back." — not an error.
- A change that creates **no** undo entry (moving `doc.CurrentViewpoint`, temporary overrides, `doc.CurrentSelection` in some cases) persists under `dryRun`: `rolledBack: false` + "dry run: … persisted". Avoid such calls under `dryRun`; tell the user Ctrl+Z will not revert them.
- An exception after an edit rolls the bridge's entry back (`rolledBack: true`, `isError: true`).
- Heavy effects (files, clash runs) are outside the undo stack: `rolledBack` is forced to `false` for them even when the test definition itself was undone.

## Heavy gate (diagnostic `HEAVY`, before any opt-in question)

Members: `AppendFile(s)`, `TryAppendFile(s)`, `MergeFile(s)`, `TryMergeFile(s)`, `RemoveFile`, `TryRemoveFile`, `OpenFile`, `TryOpenFile`, `OpenAggregate`, `TryOpenAggregate`, `UpdateFiles`, `SaveFile`, `TrySaveFile`, `ExportToNwd`, `TryExportToNwd`, `PublishFile`, `TryPublishFile`, `ExportAsDwf`, `GenerateImage`, `TestsRunTest`, `TestsRunAllTests`, `TestsCompactAllTests`, `TestsCompactTest`, and `doc.Clear()` (receiver-matched — `selection.Clear()` is fine). Opt-in off → refused naming the checkbox. Opt-in on → string literals are screened: UNC paths (`\\server\…`) and anything under `HPNavis\McpBridge`, `HPNavis\McpServer` or `\Autodesk\Navisworks Manage` are refused; the timeout ceiling becomes 600 s; the audit gets a `started` line and the `[heavy]` tag; `cancel_execution` cannot interrupt the call itself (a cancel arriving after it completed reports "its effect persisted"). `propose_tool` with a heavy member is refused — heavy tools stay seed-only.

## Guard (diagnostic `GUARD`)

Base list (every host): namespaces `System.IO` (except `System.IO.Path`), `System.Net`, `System.Reflection`, `System.Diagnostics.Process`, `System.Runtime.InteropServices`, `System.Runtime.Loader`, `System.Threading.Tasks`, `System.Security`, `System.Linq.Expressions`, `Microsoft.CodeAnalysis`, `Microsoft.Win32`; identifiers `Process`, `Thread`, `Task`, `Parallel`, `Timer`, `AppDomain`, `Assembly`, `Activator`, `Marshal`, `File`, `Directory`, `Registry`, `HttpClient`, `Expression`, `Delegate`…; members `.GetMethod`, `.GetProperty`, `.Invoke*`, `.CreateDelegate`, `.Compile`, `.Method`…; `await`, `dynamic`, `unsafe`, `#r`, `#load`. Navisworks adds: identifiers `MessageBox`, `Transaction`, `NavisworksApplication`, `ComApiBridge`, `NavisworksCommand`, `NavisworksConnection`, `NavisworksDataAdapter`; members `BeginTransaction`, `Undo`, `Redo`, `Rollback`, `TryUndo`, `TryRedo`, `TryRollback`, `StartDisableUndo`, `EndDisableUndo`, `SetModelUnitsAndTransform`, `SetUserDefined`, `Database`, `ToNavisworksConnection`; namespaces `System.Windows.Forms`, `System.Data`, `Autodesk.Navisworks.Api.Automation`, `.Interop`, `.ComApi`, `.Data`. Consequence: **custom (user-defined) properties cannot be written from a script** (COM only) — say so instead of trying.

## Patterns

**Find by property with a Search (transaction none).**
```csharp
var search = new Search();
search.Selection.SelectAll();
search.Locations = SearchLocations.DescendantsAndSelf;
search.PruneBelowMatch = true; // a matching item's descendants are not reported again
search.SearchConditions.Add(SearchCondition.HasPropertyByDisplayName(args.Str("category", "Item"), args.Str("property", "Name")).DisplayStringContains(args.Require("text")));
var found = search.FindAll(doc, false);
return new {
    total = found.Count,
    items = found.Take(args.Int("limit", 50)).Select(i => new { name = i.DisplayName, className = i.ClassDisplayName, guid = i.InstanceGuid,
        path = string.Join(" / ", i.Ancestors.Select(a => a.DisplayName).Reverse()), bboxMm = i.HasGeometry ? i.BoundingBox() : null }).ToList()
};
```

**Read one property of many items, lengths in mm (transaction none).**
```csharp
string Describe(VariantData v)
{
    if (v == null || v.IsNone) return "";
    if (v.IsDisplayString) return v.ToDisplayString();
    if (v.IsIdentifierString) return v.ToIdentifierString();
    if (v.IsDoubleLength) return Math.Round(units.ToMm(v.ToDoubleLength()), 2) + " mm";
    if (v.IsAnyDouble) return v.ToAnyDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
    return v.ToString();
}
var rows = new List<object>();
foreach (var item in doc.CurrentSelection.SelectedItems.Take(args.Int("limit", 20)))
{
    ct.ThrowIfCancellationRequested();
    var prop = item.PropertyCategories.FindPropertyByDisplayName(args.Str("category", "Element"), args.Str("property", "Level")); // null when absent
    rows.Add(new { name = item.DisplayName, guid = item.InstanceGuid, value = prop == null ? null : Describe(prop.Value) });
}
return rows;
```

**Save a search set (transaction auto; dryRun first).**
```csharp
var search = new Search();
search.Selection.SelectAll();
search.Locations = SearchLocations.DescendantsAndSelf;
search.PruneBelowMatch = true;
search.SearchConditions.Add(SearchCondition.HasPropertyByDisplayName("Item", "Type").DisplayStringContains(args.Require("type")));
var set = new SelectionSet(search) { DisplayName = args.Require("name") };
doc.SelectionSets.AddCopy(set);
return new { name = set.DisplayName, resolvesTo = search.FindAll(doc, false).Count };
```

**Hide / show and colour items found by a set (transaction auto).**
```csharp
var set = doc.SelectionSets.Value.OfType<SelectionSet>().FirstOrDefault(s => s.DisplayName == args.Require("set"))
          ?? throw new ArgumentException($"no selection set named '{args.Str("set")}'.");
var items = set.GetSelectedItems(doc);
doc.Models.SetHidden(items, args.Bool("hide", false));
byte Channel(int v) => (byte)Math.Min(255, Math.Max(0, v));
if (args.Has("r")) doc.Models.OverridePermanentColor(items, Color.FromByteRGB(Channel(args.Int("r")), Channel(args.Int("g", 0)), Channel(args.Int("b", 0))));
return new { count = items.Count };
```

**Clash results of one test (transaction none; Manage only).**
```csharp
if (!app.HasClashModule) throw new ArgumentException("this Navisworks has no Clash Detective module.");
var test = doc.GetClash().TestsData.Tests.OfType<ClashTest>().FirstOrDefault(t => t.DisplayName == args.Require("test"))
           ?? throw new ArgumentException($"no clash test named '{args.Str("test")}'.");
IEnumerable<ClashResult> Results(SavedItem item) // results may sit one level down inside result groups
{
    if (item is ClashResult r) return new[] { r };
    if (item is GroupItem g) return g.Children.SelectMany(Results);
    return Enumerable.Empty<ClashResult>();
}
var all = test.Children.SelectMany(Results).ToList();
var results = all.Take(args.Int("limit", 100)).Select(r => new {
    name = r.DisplayName, status = r.Status.ToString(), distanceMm = Math.Round(units.ToMm(r.Distance), 1),
    centerMm = r.Center, // Point3D serialises in mm
    a = r.Item1 == null ? null : r.Item1.DisplayName, b = r.Item2 == null ? null : r.Item2.DisplayName }).ToList();
return new { test = test.DisplayName, status = test.Status.ToString(), total = all.Count,
    byStatus = all.GroupBy(r => r.Status.ToString()).ToDictionary(g => g.Key, g => g.Count()), results };
```

## Rules of thumb

- Prefer a seed: the 12 seeds already do the Search construction, `VariantData` decoding, mm conversion, caps and structured errors.
- Search with conditions instead of `doc.Models.RootItems.Descendants` over the whole model; keep `PruneBelowMatch = true`; cap with `Take`.
- Caller mistakes → `throw new ArgumentException("…")` (never counts against a tool's stability); engine faults → any other exception.
- Keep results small: counts, `Take(n)`, one property per item — a full property dump is ~5 KB per item.
- Do not touch `doc.CurrentViewpoint` or temporary overrides under `dryRun` (not undoable); do not call heavy members without the user's explicit yes in this turn.

# `execute_etabs_code` — script contract

What a C# script sees inside the HPEtabs MCP Bridge, how the bridge classifies it before running it, and what comes back. Source of truth: `HPEtabs/HPEtabs.McpBridge/Service/` (`EtabsTierAnalyzer`, `EtabsPathPolicy`, `EtabsSnapshotManager`, `EtabsScriptRunner`) and `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`.

## Environment

| Global | Type | Notes |
|---|---|---|
| `sapModel` | `ETABSv1.cSapModel` | the attached model; every OAPI call goes through it (`sapModel.FrameObj`, `.PointObj`, `.AreaObj`, `.PropFrame`, `.PropMaterial`, `.LoadPatterns`, `.LoadCases`, `.RespCombo` (a `cCombo`), `.Analyze`, `.Results` (+ `.Results.Setup`), `.Story`, `.GridSys`, `.SelectObj`, `.View`, `.DatabaseTables`, `.File`, `.EditGeneral`…) |
| `etabs` | `ETABSv1.cOAPI` | the application object; its 7 lifecycle members (`ApplicationExit`, `ApplicationStart`, `Hide`, `Unhide`, `SetAsActiveObject`, `UnsetAsActiveObject`, `InternalExec`) are guard-denied; `etabs.Visible()` / `GetOAPIVersionNumber()` are R |
| `units` | `ScriptUnits` | `units.Label` = "mm"; every run is forced to `eUnits.kN_mm_C` and the user's units are restored in `finally` |
| `ct` | `CancellationToken` | check `ct.ThrowIfCancellationRequested()` in loops; `cancel_execution` and the timeout only act between OAPI calls |
| `log(string)` | `Action<string>` | lines come back in `logs[]` |
| `progress(int cur, int total, string msg)` | | progress notifications |
| `args` | `ScriptArgs` | the JSON object passed as `args` in the call |

Default usings: `System`, `System.Linq`, `System.Collections.Generic`, `ETABSv1`, `HPRebar.McpBridge.Core.Scripting`. Language: C# script (Roslyn), max 32 KB, must end with a top-level `return <value>;`. Anonymous objects, lists, dictionaries, arrays and primitives serialise; OAPI proxies are summarised; output is capped (`truncated:true`).

### `ScriptArgs` (case-insensitive keys, coercing)

`Str(key, fallback=null)`, `Int(key, fallback=0)`, `Long`, `Double(key, fallback=0)`, `Bool(key, fallback=false)`, `Require(key)` (throws `ArgumentException` when missing/empty), `Has(key)`, `Strings(key)` / `Doubles(key)` / `Longs(key)` (arrays), `List(key)` (array of objects), `Obj(key)` (nested object). Numbers arrive as JSON numbers or numeric strings; `Int` rounds.

## Conventions the seeds follow (and reviewers expect)

1. Every OAPI member returns an `int` status. Keep it: `int ret = sapModel.X.Y(...); if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from X.Y");`
2. Bad caller input → `throw new ArgumentException("...")`. The registry never counts `Argument…Exception` runs against a tool's stability, so validate names/cases first (a label such as `C1` is not a unique name — resolve through `GetNameFromLabel` or refuse).
3. Arrays come back through `ref`: `int n = 0; string[] names = null; ret = sapModel.FrameObj.GetNameList(ref n, ref names); foreach (var name in names ?? new string[0]) …`
4. Units: lengths **mm**, forces **kN**, moments **kN·mm** (÷ 1000 → kN·m), stresses **kN/mm²** (× 1000 → MPa), distributed loads **kN/mm** (kN/m ÷ 1000). Name output fields with their unit: `lengthMm`, `fzKN`, `m3KNm`, `eMPa`.
5. Read results only after `Analyze.GetCaseStatus` says status 4 (finished) for the case; select the case **or** combo for output first (`Results.Setup.DeselectAllCasesAndCombosForOutput()` then `SetCaseSelectedForOutput(name)` / `SetComboSelectedForOutput(name)`).
6. Cap output: `limit` ≤ 500, `offset` for paging, report `count`, `matched`, `truncated`.
7. Never call `SetPresentUnits` (the bridge owns units), `Helper`, dialogs, or a file-taking member from a stored tool.

## Guard (syntax, before anything else — diagnostic `GUARD`)

Denied anywhere (base list + ETABS profile): namespaces `System.IO` (except `System.IO.Path`), `System.Net`, `System.Reflection`, `System.Diagnostics.Process`, `System.Linq.Expressions`, `System.Windows.Forms`, `HPEtabs.McpBridge`, `HPRebar.McpBridge.Core.Host` (also spelled `global::…`); identifiers `File`, `Directory`, `FileStream`, `Process`, `Thread`, `Task`, `Parallel`, `Timer`, `Assembly`, `Activator`, `Marshal`, `Expression`, `Delegate`, `HttpClient`, `Registry`, `Helper`, `MessageBox`; members `.GetProperty/.GetMethod/.GetField/.GetMembers/.InvokeMember/.CreateInstance/.LoadFrom/.Exit/.CreateDelegate/.Compile/.Method` and the `cOAPI` lifecycle `.ApplicationExit/.ApplicationStart/.Hide/.Unhide/.SetAsActiveObject/.UnsetAsActiveObject/.InternalExec`; keywords `await`, `dynamic`, `unsafe`; directives `#r`, `#load`; string literals containing `System.Reflection` / `System.IO` / `System.Net`. `File` in **member** position (`sapModel.File.Save()`) passes the guard — it is then tiered D. `sapModel.AreaObj.GetProperty(...)` is caught by the reflection rule (known gap) — read area sections through `DatabaseTables.GetTableForDisplayArray` instead.

## Tier (semantic, after compile — decides transaction/opt-in/timeout)

- Every member access that binds to a symbol whose containing type lives in namespace `ETABSv1` is looked up in `Resources/etabs-oapi-tiers.txt` (1 281 rows). Aliases (`var fo = sapModel.FrameObj;`), casts, `?.`, lambdas and method groups bind to the same `cInterface.Member`. Enum fields (`eUnits.kN_mm_C`) and navigation properties (`sapModel.FrameObj`) are skipped.
- **R**: `Get*`, `Is*`, `Has*`, `Verify*`, `Count`, `RefreshView`, `Visible`, everything on `cAnalysisResults`, `cAnalysisResultsSetup`, `cView`, `cSelect`. Runs under `transaction="none"` (also fine under `auto`; dryRun runs it normally).
- **W**: everything else (`Set*`, `Add*`, `EditGeneral.Move`, `Analyze.SetRunCaseFlag`, `PointObj.SetRestraint`…). Needs `transaction="auto"` (`manual` ≡ auto); `none` or `dryRun` = static preview (`isError:true`, `rolledBack:true`, diagnostic `PREVIEW: cFrameObj.SetSection (W)`).
- **D**: `cHelper.*`, `cOAPI` lifecycle, `SetModelIsLocked`, `RunAnalysis`, `DeleteResults`, `CreateAnalysisModel`, `InitializeNewModel`, `ApplyEditedTables`, `File.Save/OpenFile/New*`, prefixes `Start|Modify|Merge|Reset|Clear|Rename|Show|Export|Import|Replicate|Delete`, any member whose by-value parameter is named `FileName|csvFilePath|SourceFileName|FilePath|Path|fullPath`, **and any member not in the table** (fail closed). Needs the second opt-in; otherwise JSON-RPC `-32001` (never a recorded run). Timeout ceiling 600 s.
- The tier of the script = the highest tier of any bound member.

## Path policy (diagnostic `PATH`, before the opt-in question)

A `path=` member's argument must be **either** a string literal **or** `args.Str("key")` / `args.Require("key")` on the bridge's own `args` with a literal key and **no fallback**, exactly one argument. A `ScriptArgs` the script builds or declares, a method group, a concatenation, a variable, a fallback value → refused. `File.Save()` with the optional name omitted saves in place (D, no path). Static screen: UNC, `HPEtabs\McpBridge|McpServer`, `\Computers and Structures\`. Run-time screen (worker, once the model is known): absolute drive path under the model folder or `%LocalAppData%\HPEtabs\`, never `%LocalAppData%\HPEtabs\McpBridge\**`; `..`, `\.\`, doubled separators are normalised first; alternate data streams and drive-root models refused; every path-shaped `args` string is screened, a bare file name for a declared key is refused.

## What a W/D run does, in order

1. Budget clock starts → guard → compile → tier → path → preview check → opt-in check → attached / model checks (`-32003` "No model (.EDB)" when `GetModelFilename` is `(Untitled)` or the file is not a local `.EDB`).
2. Audit `started` line → `presave\` copy when the file on disk was not last written by this bridge → `sapModel.File.Save()` (ret ≠ 0 fails the run before the script) → `prerun\<yyyyMMdd-HHmmss>-<label>.EDB` (buckets keep 5 / 10; `snapshot` in the result = that file name).
3. Fingerprint (names of points/frames/areas/patterns/cases/combos/sections/materials + stories + lock + file) → script → fingerprint → `changed{added, 0, deleted}`.
4. Exception after the first write: `isError:true`, `rolledBack:false`, message "changes … persisted — snapshot <file>" (or "No additions or deletions were recorded" when nothing changed). Restore = the user opens the snapshot in ETABS.

## Examples

Read — frames per section (`transaction: "none"`):

```csharp
int n = 0; string[] names = null;
int ret = sapModel.FrameObj.GetNameList(ref n, ref names);
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from FrameObj.GetNameList");
var bySection = new Dictionary<string, int>();
foreach (var name in names ?? new string[0])
{
    ct.ThrowIfCancellationRequested();
    string prop = null, sauto = null;
    if (sapModel.FrameObj.GetSection(name, ref prop, ref sauto) != 0) continue;
    var key = prop ?? "?";
    bySection[key] = bySection.TryGetValue(key, out var c) ? c + 1 : 1;
}
return new { frames = n, bySection };
```

Read with args — points of one story in mm (`args: {"story": "Story2"}`):

```csharp
string story = args.Require("story");
int n = 0; string[] names = null;
int ret = sapModel.PointObj.GetNameList(ref n, ref names);
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from PointObj.GetNameList");
var items = new List<object>();
foreach (var name in names ?? new string[0])
{
    string label = "", st = "";
    if (sapModel.PointObj.GetLabelFromName(name, ref label, ref st) != 0 || !string.Equals(st, story, StringComparison.OrdinalIgnoreCase)) continue;
    double x = 0, y = 0, z = 0;
    if (sapModel.PointObj.GetCoordCartesian(name, ref x, ref y, ref z) != 0) continue;
    items.Add(new { name, label, xMm = x, yMm = y, zMm = z });
    if (items.Count >= 500) break;
}
return new { story, count = items.Count, items };
```

Write — assign a section (`transaction: "auto"`, preview with `dryRun: true` first; `args: {"frames": ["12","13"], "section": "C40x40"}`):

```csharp
string section = args.Require("section");
int np = 0; string[] props = null;
if (sapModel.PropFrame.GetNameList(ref np, ref props) != 0) throw new InvalidOperationException("ETABS returned non-zero from PropFrame.GetNameList");
var defined = (props ?? new string[0]).FirstOrDefault(p => string.Equals(p, section, StringComparison.OrdinalIgnoreCase));
if (defined == null) throw new ArgumentException($"section '{section}' is not defined in this model");
var done = new List<string>(); var errors = new List<object>();
foreach (var name in args.Strings("frames"))
{
    ct.ThrowIfCancellationRequested();
    int ret = sapModel.FrameObj.SetSection(name, defined);
    if (ret == 0) done.Add(name); else errors.Add(new { frame = name, ret });
}
return new { section = defined, assigned = done.Count, done, errors };
```

Write — restrain the base points (W, needs `auto`):

```csharp
bool[] fixedAll = { true, true, true, true, true, true };
var done = new List<string>();
foreach (var name in args.Strings("points"))
{
    int ret = sapModel.PointObj.SetRestraint(name, ref fixedAll);
    if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from PointObj.SetRestraint({name})");
    done.Add(name);
}
return done;
```

Destructive — run selected cases (D: user ticks "Allow destructive operations"; prefer the `run_analysis` seed):

```csharp
var cases = args.Strings("cases");
int r = sapModel.Analyze.SetRunCaseFlag("", false, true);            // W: clear every flag (persists in the model)
if (r != 0) throw new InvalidOperationException($"ETABS returned {r} from Analyze.SetRunCaseFlag");
foreach (var c in cases) if (sapModel.Analyze.SetRunCaseFlag(c, true) != 0) throw new ArgumentException($"unknown load case '{c}'");
int ret = sapModel.Analyze.RunAnalysis();                            // D, synchronous, cannot be cancelled
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from Analyze.RunAnalysis");
int n = 0; string[] names = null; int[] status = null;
sapModel.Analyze.GetCaseStatus(ref n, ref names, ref status);
return Enumerable.Range(0, n).Select(i => new { name = names[i], status = status[i] }).ToList();   // 1 not run, 2 could not start, 3 not finished, 4 finished
```

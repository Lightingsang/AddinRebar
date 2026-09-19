---
name: mcp-seed-review-gotchas
description: Non-obvious checks for reviewing MCP seed tools (Revit/AutoCAD/Navis) — stability filter only excludes ArgumentException, run_tool never validates inputSchema, analyzer tracks only the `args.` receiver, Navis serializer converts Point3D/BoundingBox3D to mm
metadata:
  type: project
---

When reviewing a seed phase (`Registry/SeedLibrary/**`), check these before anything else — each was missed or nearly missed on 2026-09-15 (Navis phase 4):

- **Stability filter is a string match**: `ToolRegistryDb.Runs.cs` `error NOT LIKE 'Argument%Exception:%'`. Only `ArgumentException` (and subclasses) are treated as the caller's fault. `double.Parse` → `FormatException`, `checked` casts → `OverflowException` count as tool failures and can auto-quarantine a seed. Grep every seed for `Parse(` / `Convert.` and require `TryParse` + `ArgumentException`.
- **`run_tool` does not validate args against `inputSchema`** — `DynamicToolRegistrar` only publishes the schema to the client. `enum`, `minimum`, `maximum`, `required` are advisory; the code's own fallbacks decide. A `switch` with `default: equals` silently swallows a typo'd `op`.
- **`ScriptAnalyzer` records only `args.X("key")` on the literal `args` receiver** (`ScriptAnalyzer.cs:93`). `var side = args.Obj("a"); side.Require("category")` is invisible, so nested sub-schemas are never proven by the "declared ⇔ read" structure test.
- **Navis serializer converts geometry**: `NavisResultSerializer.WritePoint/WriteBox` apply `units.ToMm`, so a seed returning raw `BoundingBox3D`/`Point3D` under a `*Mm` name is correct — do not flag it. Output bound is `BridgeSettings.MaxOutputBytes` = 64 KB; the head is kept as a string + `…[truncated]`, so totals must be emitted before per-item arrays.
- **Navis `doc.IsClear` is refused by the executor** (`NavisMainThreadExecutor.cs:247`) before any script runs; an `if (doc.IsClear)` branch inside a seed is dead.
- Reflection probe pattern that works without Roamer: Windows PowerShell 5.1 (`powershell.exe`, not pwsh) + `Assembly.LoadFrom` on `Autodesk.Navisworks.{Api,Clash,Timeliner}.dll`; `DocumentModels/CurrentSelection/SelectionSets/SavedViewpoints/CurrentViewpoint` live in `Autodesk.Navisworks.Api.DocumentParts`; `GetClash`/`GetTimeliner` are `DocumentExtensions` / `TimelinerDocumentExtensions`.

**Why:** seeds run inside the host with no schema validation and their failures feed the auto-quarantine; the two test projects prove compile + shape, not error classification or output size.

**How to apply:** run the greps above first, then the (a)–(h) checklist the lead sends; cite `ToolRegistryDb.Runs.cs` when rating a `FormatException` finding High.

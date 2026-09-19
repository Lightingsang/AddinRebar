---
name: autocad-script-compile-check-offline
description: How to compile-check AutoCAD MCP scripts/seeds/few-shots without acad.exe (nuget ref DLL paths, wrapper shape) and API facts verified on 2026-09-14 (OpenMode values, Entity.Layer precondition)
metadata:
  type: project
---

Offline compile + guard check of AutoCAD scripts works with Roslyn `MetadataReference.CreateFromFile` on the
nuget DLLs — no `Assembly.LoadFrom`, no AutoCAD needed:
`~/.nuget/packages/autocad.net/25.1.0/lib/net8.0/AcMgd.dll`,
`autocad.net.core/25.1.0/lib/net8.0/AcCoreMgd.dll`, `autocad.net.model/25.1.0/lib/net8.0/AcDbMgd.dll`
(+ `typeof(ScriptArgs).Assembly.Location`, + TPA `System.*`). Wrapper = `using` lines from
`HostScriptContracts.AutocadImports` + a class with the exact `AutocadScriptGlobals` field names
(`doc, db, ed, app, tr, units, ct, log, progress, args`) and `object Run() { #line 1 "code.cs" {code} }`,
`NullableContextOptions.Disable`, errors only. Guard: `ScriptGuard.Check(code, GuardProfile.Autocad)`.
A scratch net8 console referencing `McpShared/HPRebar.McpBridge.Core` is enough (built 2026-09-14, phase-3 review).

**Why:** phase-4 seeds and every prompt few-shot are "API docs the AI copies"; a script that passes here and
fails in acad.exe means the wrapper drifted from the bridge (`BridgeEntry.CompilerReferences` + AutocadImports).

**How to apply:** when reviewing AutoCAD seeds/prompts, run this before trusting a "compiles" claim; also
check runtime preconditions the compiler cannot see — verified facts: `OpenMode.ForRead=0, ForWrite=1,
ForNotify=2` (so `ForRead|ForWrite` silently == ForWrite); `Entity.Layer = "X"` throws `eKeyNotFound` when
the layer is absent (default `acad.dwt` has only layer `0`); `db.Filename` on an unsaved drawing is the
template path, not `Drawing1.dwg`. Harness JSON args through `python $mcp @argv` need pwsh >= 7.3
(`$PSNativeCommandArgumentPassing = Windows`); `powershell.exe` 5.1 strips the embedded quotes.

Added 2026-09-14 (phase-4 review): the bridge's real reference set = `BridgeEntry.CompilerReferences`
(3 AutoCAD DLLs, CoreLib, Linq, Collections, netstandard, System.Runtime, McpBridge.Core, System.Text.Json)
**plus Roslyn scripting's .NET Core defaults** (`ScriptOptions.Default` lazily adds System.Text.RegularExpressions,
System.Threading, System.Runtime.Extensions, … — that is why `Regex` in a seed compiles in acad.exe). The
test wrapper's "every `System.*` on the TPA" is broader (System.Xml/Data/ComponentModel would pass the test and
fail in AutoCAD). Runtime facts checked live by the lead: `LineWeight` is int-backed (`Enum.IsDefined(typeof(LineWeight), 50)`
does not throw); `db.Extmin/Extmax` on an empty drawing are ±1e20 sentinels (min > max); `RXClass.DxfName`
can be null for custom ARX classes; MText line break is `\P` — a tool.json needs `"\\P"`, not `"\\\\P"`
(`\\` in MText is a literal backslash). `ToolManager.RunAsync` does not enforce schema `required`; the script
must check `args.Has(...)` itself.

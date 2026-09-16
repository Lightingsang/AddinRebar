# ADR-01 — AEC tools are seeds over the existing execute path; the logic is a bridge-side assembly `HPAutoCad.Aec`

**Status:** accepted 2026-09-16

## Context

The brief asks for ~37 specialised MCP tools with a Tool / Service / Model split and forbids one wide `run_command`. The AutoCAD MCP already has
a tool mechanism: a **seed** (`tool.json` + `code.cs` + `examples.json`) embedded in the server, validated by the registry, exposed by name in
`tools/list`, executed through `autocad.execute` in the bridge with the guard, the transaction policy, the change counter, audit and the output
cap. ADR-05 of the Revit plan (kept for AutoCAD and Navisworks) says: *never add a native command class to the bridge for a tool*.

Two ways to honour the brief:

| | A. Native tools (server tool classes + new pipe methods + bridge handlers) | B. Seeds + `HPAutoCad.Aec` assembly (chosen) |
|---|---|---|
| Registry (search, versions, quarantine, `tools/list_changed`) | bypassed | native |
| Contracts / dispatcher changes in `McpShared` | one method per tool family | none (one import string) |
| Business logic location | bridge classes | `HPAutoCad.Aec` classes (compiled, unit-tested) |
| Tool = data | no | yes — same shape as the 12 existing seeds and user-approved tools |
| Read-only guarantee | reimplement | `transaction: none` already refuses modification |
| Risk | second execution path to keep in sync | scripts are a 10-line shim; logic is a real assembly |

## Decision

1. Every AEC tool is a seed under `HPAutoCad.Mcp.Server/Registry/SeedLibrary/<Category>/<name>/`. Its `code.cs` only parses `args`,
   calls one service in `HPAutoCad.Aec`, and returns the envelope. New categories are added to `AutocadHostProfile.Categories` as needed.
2. `HPAutoCad.Aec` (net8.0-windows) holds pure geometry/spatial/issue/classification code (no AutoCAD types in signatures) and `Cad/` adapters
   that take the script globals (`db`, `tr`, `ed`, `units`, `ct`). The bridge references it; `BridgeEntry.CompilerReferences` adds its assembly so
   scripts can call it; `HostScriptContracts.AutocadImports` gains `HPAutoCad.Aec` and `HPAutoCad.Aec.Cad`.
3. `HPAutoCad.Aec.Tests` (xUnit v3) tests the pure code; `SeedLibraryTests` keeps compiling every seed with the bridge's exact imports, now
   with the Aec assembly as a metadata reference (the test project targets `net10.0-windows` to reference it).
4. The `execute_autocad_code` description gains the Aec namespaces automatically (it is built from `ScriptImports`), so ad-hoc scripts and
   user-proposed tools can use the same services.

## Consequences

- No change to the 24 existing tools; `get_entities` / `get_drawing_info` are superseded by `query_entities` / `get_drawing_context` in the docs but stay.
- The Aec DLL ships in `Contents\Bridge\` with the bridge (project reference → deps.json → isolated ALC). A stale bundle without it makes every
  AEC seed fail to compile with a clear CS0246; `ScriptingSelfCheck` gets one Aec call so the loader log shows it.
- `test_tool` / `propose_tool` of user tools keep working; the analyzer still sees only `args.X("literal")`, so nested objects (`source`, `target`,
  `filter`) are single declared keys read through `args.Obj("...")`.
- Long-lived state (change sets, phase I) lives in a static store inside `HPAutoCad.Aec` (same ALC, same process); it never holds a
  `Transaction`.

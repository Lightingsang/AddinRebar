# Host appendix — AutoCAD and Civil 3D

> Read with [HP_CLEAN_CODE_CORE.md](../HP_CLEAN_CODE_CORE.md). Scope: `HPAutoCad/` (MCP bridge, `HPAutoCad.Aec` engine, HPGeoLink) and `HPCivil3d/`. Sources below are as of 2026-10-04 (`plans/261004-1005-hp-clean-code-ai-tools/reports/map-02-autocad-civil.md`).

| Id | Rule | Source |
|---|---|---|
| A1 | Scripts never open, commit, abort or dispose a transaction or lock: `tr` is the bridge's (outer group + inner `tr`; `dryRun`/`none` abort the outer). The guard denies `StartTransaction`, `StartOpenCloseTransaction`, `TopTransaction`, `LockDocument`, `tr.Commit/Abort/Dispose` | `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs:14-42` |
| A2 | Wrapper disposal is correctness, not hygiene: every AutoCAD .NET wrapper an add-in creates (Transaction, DocumentLock, DBObject opened outside `tr`) is disposed deterministically (`using var`); an undisposed wrapper finalised on the GC thread crashes acad.exe | `GuardProfile.cs:14-21` (verified live) |
| A3 | The document lock of a script run is the bridge's (`ProtectedAutoWrite`, undo name "HPMCP"); add-in commands that write lock their own document | `HPAutoCad/HPAutoCad.McpBridge/Service/AutocadScriptRunner.cs:70` |
| A4 | No interactive prompts, command-context escapes or modal UI from scripts (`ed.Get*`, `Select*`, `SendStringToExecute`, `Command*`, `ShowModal*`) | `GuardProfile.cs:27-34` |
| A5 | mm at the tool boundary via `units`, whatever INSUNITS is | `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptUnits.cs:11` |
| A6 | Civil 3D: plan x/y in mm via `units`; stations, elevations and areas in the **Civil drawing unit** with `drawingUnit` in every envelope | `HPCivil3d/HPCivil3d.McpBridge/Service/Civil3dUnitTable.cs:8-20,36` |
| A7 | Entities are addressed by **handle** through one resolver (INVALID_HANDLE / ERASED / NOT_AN_ENTITY); never an `ObjectId` across calls | CLAUDE.md AEC engine (ADR-02) |
| A8 | Tolerances come from `GeometryTolerance`; no tolerance literals in services | `HPAutoCad/HPAutoCad.Aec/Geometry/GeometryTolerance.cs:11-24` |
| A9 | Writes are two-phase: validate everything opened for read, then upgrade and apply; an atomic batch writes nothing when one item is refused | `HPAutoCad/HPAutoCad.Aec/Cad/EditContext.cs:97-131` |
| A10 | Results stay under the 64 KB cap: every list is paged (`limit` ≤ engine cap + `offset`), caps pinned by seed tests | `McpShared/HPRebar.McpBridge.Core/Model/BridgeSettings.cs:22` |
| A11 | Seed contract: body ends in `return`; `args.X("literal", default)` for every schema key; schema default = code fallback; caller errors `ArgumentException`; an AEC seed is one `AecTools.*` call | `HPAutoCad/HPAutoCad.Mcp.Server.Tests/SeedLibraryTests.cs:103-144` |
| A12 | Civil guard additions (`Rebuild*`, `DataShortcuts`, `SurveyProject*`, `ExportTo*`, `CreateFrom*`, `ImportPoints/ExportPoints`, `CreateSolids*ToFile`); Civil `Entity`/`DBObject` through aliases, never `using Autodesk.Civil.DatabaseServices` | `GuardProfile.cs:176-192` |
| A13 | A .NET 8 AutoCAD add-in = a loader in the default load context + real code in its own `AssemblyLoadContext`; host prefixes `Ac`/`Ad`/`Autodesk.` resolve from the host | `.claude/rules/development-rules.md` (AutoCAD rules) |
| A14 | One shared ribbon tab `HPAutoCad` (`HPAUTOCAD_MCP_TAB`), one panel per tool, FindTab-or-create; Civil 3D its own tab `HPCIVIL3D_MCP_TAB`, same protocol | `.claude/rules/development-rules.md`; `HPAutoCad/HPAutoCad.McpBridge.Loader/Ribbon/McpRibbonTab.cs` |
| A15 | Drawing-writing commands never use `CommandFlags.NoUndoMarker`; WPF TwoWay properties need setters; `EnterContextualReflection` around `InitializeComponent` | `.claude/rules/development-rules.md` |
| A16 | `HPCivil3d/` mirrors the AutoCAD bridge: an AutoCAD bridge edit is ported (tokens, `civil-only` blocks, re-pin) or the mirror test fails; Civil seeds are changed in `HPCivil3d/tools/generate-seed-library.py`, never by hand | `HPCivil3d/tools/mirror-tokens.json`; `HPCivil3d.McpBridge.Tests/MirrorTests.cs` |

Tests: `HPAutoCad.Mcp.Server.Tests`, `HPAutoCad.Aec.Tests`, `HPCivil3d.Mcp.Server.Tests`, `HPCivil3d.McpBridge.Tests` (mirror). Live: `HPAutoCad/tools/harness/`, `HPCivil3d/tools/harness/`. Builds with the host open: `-p:DeployBundle=false`.

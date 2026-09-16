# Architecture — AEC Automation MCP on the existing HPAutoCad MCP

## 1. What exists (scout 2026-09-16)

```
Claude Code ──stdio──▶ HPAutoCad.Mcp.Server (net10, ModelContextProtocol 2.2.0)
                        ├─ 4 core tools: execute_autocad_code, get_autocad_context, inspect_type, cancel_execution
                        ├─ 8 registry meta tools (search/get/run/get_run/propose/test/publish/manage)
                        └─ N published library tools = seeds (12 embedded) + approved proposals
                              each = tool.json (name, schema, description, transaction, timeout) + code.cs (script body) + examples.json
                                                                    │  autocad.execute over named pipe hpautocad-mcp-2026 (JSON-RPC NDJSON)
                                                                    ▼
HPAutoCad.McpBridge (net8.0-windows, isolated ALC inside acad.exe)
  BridgeEntry → MainThreadExecutor (Idle + IsQuiescent, busy 8 s → -32002, no doc → -32003)
             → ScriptGuard (GuardProfile.Autocad) → ScriptCompiler (Roslyn, imports = HostScriptContracts.AutocadImports,
               references = AcMgd/AcCoreMgd/AcDbMgd + BCL + HPRebar.McpBridge.Core)
             → AutocadScriptRunner: LockDocument + outer/inner Transaction; script sees `tr`; none = abort + refuse modification,
               dryRun = abort, auto = commit; DatabaseChangeCounter → Changed{added,modified,deleted}
             → AutocadResultSerializer: JSON (camelCase, AutoCAD objects summarised), cap MaxOutputBytes 64 KB
  globals: doc, db, ed, app, tr, units (ScriptUnits: ToMm/ToDrawing/MmPerUnit/Label from INSUNITS), ct, log, progress, args (ScriptArgs)
```

| Existing piece | File | Reused by the AEC layer |
|---|---|---|
| Seed = tool contract (schema ⇔ `args.X("literal")`, category ∈ profile, timeout 30–60, `transaction`) | `HPAutoCad.Mcp.Server/Registry/SeedLibrary/**`, `SeedLibraryTests` | every new tool is a seed |
| Execute path, transaction policy, change counting, read-only enforcement (`none`) | `AutocadScriptRunner`, `DatabaseChangeCounter` | every tool; query tools declare `none` |
| Units | `ScriptUnits` (`units` global), `AutocadInsunits` | = `DrawingUnitService` of the brief |
| Args parsing (nested objects/lists, coercion) | `ScriptArgs` | tool inputs |
| Serialization + cap | `AutocadResultSerializer` | responses; forces `limit/offset/summary` |
| Selection filtering | `ed.SelectAll(SelectionFilter)` pattern in `get_entities` | `EntityQueryService` broad phase |
| Handle → ObjectId | `db.GetObjectId(false, new Handle(long), 0)` (not yet wrapped) | `HandleResolver` (new) |
| Context snapshot | `AutocadContextReader` (session-level: docs, units, layout, selection) | `get_drawing_context` extends it, does not replace |
| Live harness | `tools/harness/*` (stdio session, seeds, smoke) | new `run-aec-tools-live.ps1` / `aec-tools-live.py` |

## 2. Mapping the requested layering onto this codebase

| Brief layer | Here | Notes |
|---|---|---|
| Natural language / AI | Claude Code / any MCP client | unchanged |
| MCP Tool Layer (parameters, validation, service call, response) | **seed `tool.json` + thin `code.cs`** | ADR-05 of the Revit plan holds: no native tool classes per feature; a tool is data + a script |
| Drawing Context | `HPAutoCad.Aec.Cad.DrawingContextReader` | `get_drawing_context` |
| AEC Semantic Layer | `HPAutoCad.Aec.Classification` (rules from JSON) + `Cad.AecEntityReader` | phase B |
| Spatial / Geometry Engine | `HPAutoCad.Aec.Geometry` (pure) + `Spatial` (pure index + predicates) + `Cad.MeasureService` (exact AutoCAD curve maths) | phase A |
| AEC Rule Engine | `HPAutoCad.Aec.Rules` (rule sets loaded from JSON: classification, CAD standards, discipline checks) | phase B/D |
| Discipline services | `HPAutoCad.Aec.Structural / Architecture / Mep` | phases E–G |
| ChangeSet / Transaction | `HPAutoCad.Aec.Cad.ChangeSets` (logical plan in bridge memory; real transaction only at commit) | phase I; the bridge already refuses long-lived transactions |
| AutoCAD .NET API | bridge + `HPAutoCad.Aec.Cad` adapters | AutoCAD.NET 25.1.0, no upgrade |

```
HPAutoCad/
├── HPAutoCad.Aec/                    NEW  net8.0-windows, references AutoCAD.NET (compile-only) + HPRebar.McpBridge.Core
│   ├── Geometry/                     pure: Pt, Box, Seg, Shape, GeometryTolerance, GeometryMath (distance, intersection, polygon)
│   ├── Spatial/                      pure: SpatialIndex (uniform grid over boxes), SpatialRelation, SpatialPredicates
│   ├── Issues/                       pure: GeometryIssue, IssueSeverity, GeometryIssueDetector
│   ├── Model/                        pure: AecEntityRecord, response envelopes, ToolErrorCode, ToolError
│   ├── Classification/ Rules/ ...    later phases (pure where possible)
│   └── Cad/                          AutoCAD adapters: EntityShapeReader, EntityQueryService, HandleResolver, DrawingContextReader, MeasureService
├── HPAutoCad.Aec.Tests/              NEW  xUnit v3, pure geometry/spatial/issues/classification — no AutoCAD
├── HPAutoCad.McpBridge/              references HPAutoCad.Aec; CompilerReferences += Aec assembly (scripts call it)
├── HPAutoCad.Mcp.Server/Registry/SeedLibrary/<Category>/<tool>/   NEW seeds per phase (categories: + Geometry, Audit, …)
└── HPAutoCad.Mcp.Server.Tests/       SeedLibraryTests references HPAutoCad.Aec for the compile check; seed count updated per phase
McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs   AutocadImports += "HPAutoCad.Aec", "HPAutoCad.Aec.Cad" (additive)
```

Why a script still sits between the tool and the service: the seed is what the registry validates, versions, quarantines and lists;
the script is ten lines that parse `args` and call one service method, so the "no business logic in the tool" rule of the brief is
kept — the logic is compiled C# in `HPAutoCad.Aec`, reviewed and unit-tested, not text in a JSON file.

## 3. Cross-cutting contracts (ADR-02)

- **Units**: every number that is a length/area crossing the tool boundary is millimetres (mm², mm for tolerances) — the established seed
  contract; the drawing's INSUNITS is applied by `units` (`ScriptUnits`), never assumed. `get_drawing_context` reports the drawing units so the
  AI can convert for the user. Angles in degrees.
- **Tolerance**: `GeometryTolerance` record (mm / degrees) with named members `PointEquality`, `EndpointConnection`, `Collinearity`, `ParallelAngle`,
  `Duplicate`, `RoomGap`, `TinySegment`; tools accept a `tolerance` object overriding any member; no magic numbers in services.
- **Handles**: API uses persistent handles (strings, hex, case-insensitive). `HandleResolver` maps to `ObjectId`, reports `INVALID_HANDLE`,
  `ERASED`, `NOT_AN_ENTITY`; never exposes ObjectId.
- **Envelopes** (camelCase, the repo's JSON convention): analysis `{ success, summary, items, warnings, errors, truncated, count }`; edit
  `{ success, createdCount, modifiedCount, deletedCount, affectedHandles, warnings, errors }`; error `{ code, message, handle?, detail? }` with
  codes from `ToolErrorCode` (INVALID_ARGUMENT, INVALID_HANDLE, ERASED, NOT_AN_ENTITY, UNSUPPORTED_ENTITY, LAYER_LOCKED, LAYER_FROZEN,
  NO_GEOMETRY, LIMIT_EXCEEDED, INTERNAL). Caller errors that make the run meaningless throw `ArgumentException` (excluded from the
  stability window); partial problems are reported in `errors`/`warnings` with `success` reflecting whether the requested work was done.
- **Read-only**: query/analysis tools declare `transaction: none`; the runner aborts the transaction and fails the run if anything changed.
- **Performance**: broad phase by `SelectionFilter` (type, layer, layout) → shape extraction only for candidates (arcs tessellated, splines
  sampled) → `SpatialIndex` (grid) → narrow phase (segment maths, exact AutoCAD `IntersectWith` / `GetClosestPointTo` where it matters);
  `maxCandidates` guard + `truncated` flag rather than silent full-drawing walks.
- **Logging**: each service call logs tool, duration, processed/returned counts through the script `log` (goes into the run's `logs`) and
  Serilog at Debug; never entity payloads.

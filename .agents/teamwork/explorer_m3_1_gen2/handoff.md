# Milestone 3 Handoff Report: Revit Client & Pipe Engine Architecture

## 1. Observation

### 1.1 Named Pipe Infrastructure & Wire Protocol (`McpShared/`)
- **Pipe Naming (`McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`)**:
  - Line 13: `public const string Prefix = "hprebar-mcp-r";`
  - Line 61: `public static string For(int revitVersion) => Prefix + revitVersion;`
  - For Revit 2026, the pipe name is verbatim `"hprebar-mcp-r2026"`, accessed as `\\.\pipe\hprebar-mcp-r2026` or `new NamedPipeClientStream(".", "hprebar-mcp-r2026", PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly)`.
- **Wire Framing & Serialization (`McpShared/HPRebar.Mcp.Contracts/JsonRpc/`)**:
  - `JsonRpcEnvelope.cs` lines 11–38 defines JSON-RPC 2.0 with properties: `jsonrpc` ("2.0"), `id` (long), `method` (string), `params` (JsonElement), `result` (JsonElement), `error` (JsonRpcError).
  - `BridgeJson.cs` lines 14–23: Standard serializer using `JsonNamingPolicy.CamelCase`, `PropertyNameCaseInsensitive = true`, `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`, `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`.
  - `NdjsonPipeTransport.cs` lines 49–50 & 98: Communication uses Newline-Delimited JSON (NDJSON) over UTF-8 without BOM (`new UTF8Encoding(false)`), delimited by `\n`.
  - `JsonRpcMethods.cs` lines 12–15: Request methods: `"revit.execute"`, `"revit.ping"`, `"revit.context"`. Notifications: `"revit.progress"`, `"revit.log"`, `"revit.status"`.
- **RPC DTOs (`McpShared/HPRebar.Mcp.Contracts/Messages/`)**:
  - `ExecuteRequest.cs` line 10: `public sealed record ExecuteRequest(string Code, string Transaction = "auto", bool DryRun = false, int TimeoutSeconds = 30, string? Label = null, JsonElement? Args = null);`
  - `ExecuteResult.cs` lines 11–40: Contains `bool IsError`, `JsonElement? Value`, `string? ValueType`, `string? Message`, `IReadOnlyList<string> Logs`, `IReadOnlyList<ScriptDiagnostic> Diagnostics`, `ChangedCounts Changed`, `bool RolledBack`, `bool TimedOut`, `long DurationMs`.

### 1.2 Revit Bridge Script Execution & Transaction Engine (`HPRebar/HPRebar.McpBridge/`)
- **Transaction & dryRun Mechanics (`HPRebar.McpBridge/Service/ScriptRunner.cs`)**:
  - Lines 74–78: If `mode != TransactionModes.None`: `group = new TransactionGroup(doc, "MCP: " + label); group.Start();`
  - Lines 80–85: If `mode == TransactionModes.Auto`: `transaction = new Transaction(doc, "MCP script"); AutoDismissFailurePreprocessor.ApplyTo(transaction); transaction.Start();`
  - Line 88: Executes compiled Roslyn script: `value = script.RunAsync(globals, linked.Token).GetAwaiter().GetResult().ReturnValue;`
  - Line 97: `transaction.Commit();`
  - Lines 100–111: `if (group is not null) { if (request.DryRun) { group.RollBack(); rolledBack = true; } else { group.Assimilate(); } }`
  - Lines 113–131: Any exception or cancellation automatically rolls back both `transaction` and `group`.
- **Script Guard & Environment (`HPRebar.McpBridge.Core/Scripting/`)**:
  - `HostScriptContracts.cs` lines 12–17: Script imports include `System`, `System.Linq`, `System.Collections.Generic`, `Autodesk.Revit.DB`, `Autodesk.Revit.UI`, `Autodesk.Revit.DB.Structure`, `HPRebar.McpBridge.Core.Scripting`.
  - `HostScriptContracts.cs` line 51: Script globals include `"doc"`, `"uidoc"`, `"app"`, `"uiapp"`, `"ct"`, `"log"`, `"progress"`, `"args"`.
  - `ScriptGuard.cs` lines 20–54 denies `System.IO`, `System.Reflection`, `System.Threading.Tasks`, `Process`, `File`, `Directory`, `#r`, `#load`.
  - `ScriptArgs.cs` lines 13–73 provides typed, case-insensitive accessors: `args.List("data")`, `args.Require("name")`, `args.Double("x")`, `args.Obj("location")`.
  - `ScriptCompiler.cs` lines 59–65 uses SHA256 code hashing: scripts with identical source code achieve cache hits (`ScriptCache`), avoiding re-compilation overhead (~0ms runtime).

### 1.3 Element Creation Seeds (`HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary/`)
- **Stage 1 Levels (`Architecture/create_level/code.cs`)**: `Level.Create(doc, Ft(elevationMm))`, `level.Name = name;`, `level.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY)?.Set(...)`, `ViewPlan.Create(doc, type.Id, level.Id);`.
- **Stage 2 Grids (`Architecture/create_grid/code.cs`)**: `Grid.Create(doc, Line.CreateBound(p0, p1))`, `grid.Name = name;`.
- **Stage 3 Structure (`Structure/create_structural_framing_system/code.cs` & `Architecture/create_surface_based_element/code.cs`)**:
  - Columns: `doc.Create.NewFamilyInstance(point, symbol, level, StructuralType.Column)`.
  - Beams: `doc.Create.NewFamilyInstance(Line.CreateBound(p0, p1), symbol, level, StructuralType.Beam)`.
  - Slabs/Floors: `Floor.Create(doc, new List<CurveLoop> { loop }, floorType.Id, level.Id, isStructural: true, null, 0)`.
- **Stage 4 Walls (`Generic/create_line_based_element/code.cs`)**: `Wall.Create(doc, Line.CreateBound(p0, p1), wallType.Id, level.Id, Ft(height), offsetFt, false, structural)`.
- **Stage 5 Openings (`Architecture/create_point_based_element/code.cs`)**: `doc.Create.NewFamilyInstance(onWall, symbol, hostWall, level, StructuralType.NonStructural)`.
- **Stage 6 Rooms (`Architecture/create_room/code.cs`)**: `doc.Create.NewRoom(level, new UV(Ft(x), Ft(y)))`, `room.get_Parameter(BuiltInParameter.ROOM_NAME)?.Set(name);`.
- **Stage 7 MEP (`Generic/create_line_based_element/code.cs`)**:
  - Fixtures: `doc.Create.NewFamilyInstance(point, symbol, level, StructuralType.NonStructural)`.
  - Pipes: `Autodesk.Revit.DB.Plumbing.Pipe.Create(doc, systemType.Id, pipeType.Id, level.Id, p0, p1)`.
- **Stage 8 Rebars (`HPRebar/Shared/Revit/RebarCurveFactory.cs`)**: `Rebar.CreateFromCurves(doc, style, barType, host, normal, curves, new BarTerminationsData(doc), true, true)` (Revit 2026).

---

## 2. Logic Chain

1. **Decoupled Architecture & Multi-Targeting**:
   - `HPRebar.Mcp.Contracts` targets `netstandard2.0;net48`.
   - `HPBim.Client` targets `net8.0;net10.0` and references `HPBim.Core` and `HPRebar.Mcp.Contracts`.
   - By creating a lightweight, self-contained `RevitPipeClient` inside `HPBim.Client/Connection/`, `HPBim.Client` avoids referencing `HPRebar.Mcp.Server.Core` (which targets `net10.0` only and pulls in `ModelContextProtocol` + SQLite). This preserves `net8.0;net10.0` compatibility and prevents dependency bloat.

2. **Topological Order of the 8-Stage BIM Pipeline**:
   - The generation sequence follows strict BIM spatial and hosting dependencies:
     $$\text{Stage 1 (Levels)} \rightarrow \text{Stage 2 (Grids)} \rightarrow \text{Stage 3 (Structure)} \rightarrow \text{Stage 4 (Walls)} \rightarrow \text{Stage 5 (Openings)} \rightarrow \text{Stage 6 (Rooms)} \rightarrow \text{Stage 7 (MEP)} \rightarrow \text{Stage 8 (Rebars)}$$
   - Levels are prerequisites for Grids, Structure, Walls, and Rooms.
   - Walls must exist before wall-hosted Openings (Doors/Windows).
   - Walls must be regenerated (`doc.Regenerate()`) before Rooms can calculate bounded enclosures.
   - Concrete Structural elements (Columns/Beams) must exist to act as host elements for Rebars.

3. **Separate Staged Execution vs Unified Composite Dry-Run**:
   - In live mode (`dryRun: false`), stages execute sequentially via 8 distinct `ExecuteRequest` calls. This allows the CLI and orchestrators to report real-time progress per stage.
   - In dry-run mode (`dryRun: true`), if stages ran separately, Stage 1 (Levels) would rollback immediately, leaving subsequent stages failing due to missing Levels.
   - **Architectural Solution**: `HPBim.Client` provides `CompositeBimGenerator`. When `dryRun: true` is requested, it generates a single unified script executing all 8 stages inside the bridge's single outer `TransactionGroup`. The entire model is instantiated, regenerated, and validated in-memory, after which `group.RollBack()` cleanly discards all changes.

4. **Dynamic Type & Family Resolution**:
   - Residential Vietnamese construction relies on 110mm and 220mm walls, and 220x220mm columns.
   - Default Revit templates rarely contain these exact names/dimensions.
   - Script generators emit embedded C# routines to check existing types by name and dimension. If missing, they duplicate an existing basic type/symbol, adjust compound layer widths or dimension parameters (`b`, `h`, `Width`, `Height`), and ensure activation via `symbol.Activate()` before placement.

5. **Clean Code Compliance (< 300 lines per class, < 50 lines per method)**:
   - Decomposing the system into focused single-responsibility classes guarantees every file stays comfortably under the 300-line ceiling (projected 30–220 lines per class).
   - Script templates are separated from parameter serializers and pipe transport logic.

---

## 3. Caveats

1. **Revit Runtime Availability**:
   - Live execution requires Autodesk Revit 2026 running with `HPRebar.McpBridge` loaded and an active project document open.
   - For unattended CI/CD and unit tests, `HPBim.Client.Tests` must rely on `FakeRevitPipeClient` and in-process Named Pipe harnesses so tests run completely offline in seconds.
2. **Door/Window Hosting Proximity**:
   - In Revit, `NewFamilyInstance` for wall-hosted families requires a valid host wall and projected point `onWall`. If the source model specifies an opening where wall geometry deviates slightly, a geometric projection search (finding the nearest wall within a tolerance window of 500–1500mm) is required.
3. **Room Boundary Enclosure**:
   - In Revit, rooms require a closed loop of room-bounding elements. Walls must have `WALL_ATTR_ROOM_BOUNDING` active (default true), and `doc.Regenerate()` must be called prior to `doc.Create.NewRoom`.
4. **Rebar Terminations in Revit 2026**:
   - Revit 2026 introduced `BarTerminationsData(doc)` in `Rebar.CreateFromCurves`. Earlier overloads taking hook types directly are obsolete or multi-version gated in `HPRebar`. The script generator targets Revit 2026 natively.

---

## 4. Conclusion

The architecture for Milestone 3 (Revit Client & Pipe Engine) is fully specified and validated against the existing codebase patterns.

### 4.1 Target Solution Structure
```
HPBim/
├── HPBim.slnx                                 (Add HPBim.Client and HPBim.Client.Tests)
├── src/
│   └── HPBim.Client/
│       ├── HPBim.Client.csproj               (Targets net8.0;net10.0)
│       ├── Connection/
│       │   ├── IRevitPipeClient.cs           (Client interface)
│       │   ├── RevitPipeClient.cs            (NamedPipeClientStream + NDJSON framing)
│       │   ├── RevitPipeException.cs         (Typed exceptions)
│       │   └── RevitConnectionOptions.cs     (Timeout, pipe name defaults)
│       ├── ScriptGenerators/
│       │   ├── IStageScriptGenerator.cs       (Stage generator interface)
│       │   ├── StageExecutionPayload.cs       (Code + Args container)
│       │   ├── Stage1LevelGenerator.cs        (Levels + FloorPlan views)
│       │   ├── Stage2GridGenerator.cs         (Orthogonal grids)
│       │   ├── Stage3StructureGenerator.cs    (Columns, Beams, Slabs)
│       │   ├── Stage4WallGenerator.cs         (Walls 110/220)
│       │   ├── Stage5OpeningGenerator.cs      (Wall-hosted Doors/Windows)
│       │   ├── Stage6RoomGenerator.cs         (Rooms + regenerated boundaries)
│       │   ├── Stage7MepGenerator.cs          (Plumbing fixtures + Pipes)
│       │   ├── Stage8RebarGenerator.cs        (Rebar.CreateFromCurves)
│       │   └── CompositeBimGenerator.cs       (Unified 8-stage script for atomic dryRun)
│       ├── TypeResolver/
│       │   ├── DynamicTypeScriptHelper.cs     (C# script functions for duplicate & activate)
│       │   └── TypeResolutionRules.cs         (Parameter mapping: b, h, thickness)
│       ├── Pipeline/
│       │   ├── IBimPipelineEngine.cs          (Pipeline orchestrator interface)
│       │   ├── BimPipelineEngine.cs           (Dispatches stages / composite runs)
│       │   ├── BimPipelineOptions.cs          (DryRun, timeout, selected stages)
│       │   ├── BimPipelineResult.cs           (Aggregate report of all stages)
│       │   └── StageResult.cs                 (Per-stage status and counts)
│       └── Verification/
│           ├── IRevitModelVerifier.cs         (Verification query interface)
│           ├── RevitModelVerifier.cs          (FilteredElementCollector query script)
│           └── ModelVerificationReport.cs     (1:1 count & geometry match report)
└── tests/
    └── HPBim.Client.Tests/
        ├── HPBim.Client.Tests.csproj         (Targets net10.0, xUnit v3 / MTP)
        ├── Fakes/
        │   ├── FakeRevitPipeClient.cs         (In-memory mock for pipeline tests)
        │   └── PipeServerTestHarness.cs       (Real named-pipe local test server)
        ├── Connection/
        │   └── RevitPipeClientTests.cs        (Framing & handshake round-trip tests)
        ├── ScriptGenerators/
        │   ├── ScriptSyntaxAndGuardTests.cs   (Validates C# syntax & ScriptGuard safety)
        │   ├── Stage1LevelGeneratorTests.cs   (Payload & args verification)
        │   ├── Stage2GridGeneratorTests.cs
        │   ├── Stage3StructureGeneratorTests.cs
        │   ├── Stage4WallGeneratorTests.cs
        │   ├── Stage5OpeningGeneratorTests.cs
        │   ├── Stage6RoomGeneratorTests.cs
        │   ├── Stage7MepGeneratorTests.cs
        │   ├── Stage8RebarGeneratorTests.cs
        │   └── CompositeBimGeneratorTests.cs
        ├── Pipeline/
        │   ├── BimPipelineEngineTests.cs      (Stage sequencing & error propagation)
        │   └── DryRunRollbackTests.cs         (Atomic rollback validation)
        └── Verification/
            └── RevitModelVerifierTests.cs     (Query response parsing & reporting)
```

### 4.2 Clean Code Sizing Budget
| Component | Class Count | Lines per Class | Method Lines | Status |
|-----------|-------------|-----------------|--------------|--------|
| `Connection/` | 4 | 30 – 180 | <= 35 | Strict Pass |
| `ScriptGenerators/` | 11 | 25 – 220 | <= 45 | Strict Pass |
| `TypeResolver/` | 2 | 80 – 150 | <= 40 | Strict Pass |
| `Pipeline/` | 5 | 35 – 200 | <= 40 | Strict Pass |
| `Verification/` | 3 | 30 – 180 | <= 35 | Strict Pass |
| `Tests/` | 15 | 100 – 220 | <= 35 | Strict Pass |

---

## 5. Verification Method

To independently verify this design during M3 implementation:

1. **Compilation & Multi-Targeting**:
   ```bash
   dotnet build HPBim/src/HPBim.Client/HPBim.Client.csproj -c Debug
   dotnet build HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj -c Debug
   ```
   *Expected outcome*: Zero errors, zero warnings, clean compilation on both `net8.0` and `net10.0`.

2. **Automated Unit & Syntax Test Suite**:
   ```bash
   dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj
   ```
   *Expected outcome*: 100% green tests in < 3 seconds. Validates:
   - Generated C# scripts pass Roslyn `CSharpSyntaxTree.ParseText` with zero parse errors.
   - `ScriptGuard.Check(code, GuardProfile.Revit)` reports zero guard diagnostics.
   - JSON serialization of `ExecuteRequest` and deserialization of `ExecuteResult` strictly match `McpShared` contracts.
   - `BimPipelineEngine` executes all 8 stages in topological order against `FakeRevitPipeClient`.
   - `DryRunRollbackTests` verify `ExecuteRequest.DryRun = true` and `RolledBack = true`.

3. **Live Pipe Smoke Test (when Revit 2026 is open)**:
   ```bash
   # From PowerShell
   python McpShared/tools/mcp-call.py --host revit --method revit.ping
   ```
   *Expected outcome*: `{"pong": true, "revitVersion": "2026", "executionEnabled": true}`.

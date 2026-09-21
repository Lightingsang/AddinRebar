# Dispatch Assignment: Milestone 3 — HPTekla.Mcp.Server (.NET 10 Console Stdio Server)

## Role: Worker (teamwork_preview_worker)
## Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3`
## Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
## Project Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_8\PROJECT.md`
## Survey Report Reference: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_3\report.md`

---

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

---

## Write Ownership
You have exclusive write access to:
- `HPTekla/HPTekla.Mcp.Server/**` (all files and subdirectories)
- `.agents/teamwork_preview_worker_m3/**` (your working directory)

You MUST NOT modify files in `McpShared/` or `HPTekla/HPTekla.McpBridge/` or any other CAD host folders.

---

## Detailed Task Specification

### 1. Project Configuration (`HPTekla/HPTekla.Mcp.Server/HPTekla.Mcp.Server.csproj`)
- TargetFramework: `net10.0`
- OutputType: `Exe`
- LangVersion: `latest`
- Nullable: `enable`, ImplicitUsings: `enable`
- RootNamespace: `HPTekla.Mcp.Server`
- Configurations: `Debug;Release`
- `<InternalsVisibleTo Include="HPTekla.Mcp.Server.Tests" />`
- ProjectReference to:
  - `..\..\McpShared\HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj`
  - `..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj`
- STRICTLY ZERO references to Tekla Open API assemblies (this is a host-free stdio console server).
- Seed library resource embedding:
  ```xml
  <ItemGroup>
      <Content Include="appsettings.json">
          <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </Content>
      <Compile Remove="Registry\SeedLibrary\**\*.cs" />
      <EmbeddedResource Include="Registry\SeedLibrary\**\*" LogicalName="SeedLibrary/%(RecursiveDir)%(Filename)%(Extension)" />
  </ItemGroup>
  ```

### 2. `Program.cs`
- Standard 6-line entry point:
  ```csharp
  using HPTekla.Mcp.Server.Hosts.Tekla;
  using HPRebar.Mcp.Server.Bootstrap;

  // The Tekla Structures 2025 MCP server:
  // host-neutral bootstrap, pipe client, registry engine, and meta tools live in HPRebar.Mcp.Server.Core.
  // This exe supplies TeklaHostProfile, core tools, prompts, resources, and 12 embedded seeds.
  return await McpServerHost.RunAsync(args, TeklaHostProfile.Instance);
  ```

### 3. `Hosts/Tekla/TeklaHostProfile.cs`
- Implement `IHostProfile`:
  - `ExecuteToolName = "execute_tekla_code"`
  - `ContextToolName = "get_tekla_context"`
  - `HostId = PipeNaming.TeklaHost` ("tekla")
  - `DisplayName = "Tekla Structures"`
  - `ServerName = "HPTekla MCP"`
  - `ProductFolder = "HPTekla"`
  - `EnvPrefix = "HPTEKLA_MCP_"`
  - `DefaultVersion = 2025`
  - `ValidVersions = new[] { 2025 }`
  - `MethodPrefix = JsonRpcMethods.TeklaPrefix` ("tekla.")
  - `ResourceScheme = PipeNaming.TeklaHost` ("tekla")
  - `Categories = new[] { "Model", "Geometry", "Property", "Rebar", "Drawing", "Export", "Generic" }`
  - `CoreToolNames = new[] { ExecuteToolName, ContextToolName, "inspect_type", "cancel_execution" }`
  - `ScriptImports = HostScriptContracts.TeklaImports`
  - `ScriptContractSummary` (accurate summary of globals, mm units, 3-tier safety, atomic rollback via savepoint, heavy ops opt-in)
  - `HostAssembly = typeof(TeklaHostProfile).Assembly`
  - `CliExecutable = "HPTekla.Mcp.Server.exe"`
  - `MaxTimeoutSeconds = HostScriptContracts.TeklaHeavyMaxTimeoutSeconds` (600)
  - `BridgeNotConnectedHint`, `TimeoutSemanticsHint`
  - `PipeName(int version) => PipeNaming.For(HostId, version)`
  - `Method(string suffix) => JsonRpcMethods.For(MethodPrefix, suffix)`

### 4. Core Tools & Services (`Hosts/Tekla/Tools/`)
- `ExecuteTeklaCodeTool.cs`:
  - `[McpServerToolType]`
  - `[McpServerTool(Name = TeklaHostProfile.ExecuteToolName, Title = "Execute C# in Tekla", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]`
  - Delegates to `ExecuteCodeService.ExecuteAsync`
- `GetTeklaContextTool.cs`:
  - `[McpServerToolType]`
  - `[McpServerTool(Name = TeklaHostProfile.ContextToolName, Title = "Get Tekla context", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]`
  - Delegates to `ContextService.GetAsync`

### 5. Resources & Prompts (`Hosts/Tekla/Resources/` & `Hosts/Tekla/Prompts/`)
- `TeklaResourceProvider.cs`:
  - `tekla://model/info` and `tekla://selection` via `ContextService.ReadAsync`
- `TeklaPromptProvider.cs`:
  - `tekla_query_template`, `tekla_modify_template`, `tekla_rebar_template`

### 6. Configuration (`appsettings.json`)
- Configure standard settings matching other MCP servers (e.g. `HPRobot.Mcp.Server/appsettings.json`).

### 7. 12 Embedded Seed Tools (`Registry/SeedLibrary/`)
Each seed directory MUST contain `tool.json`, `examples.json`, and `code.cs`:
1. `Model/get_model_info`:
   - `tool.json`: Category Model, transaction none, timeout 30, destructive false, parameters `includeProjectInfo` (bool), `includePhaseInfo` (bool).
   - `code.cs`: Queries `model.GetInfo()`, `model.GetProjectInfo()`, `model.GetConnectionStatus()`.
   - `examples.json`: 2 examples.
2. `Model/select_objects`:
   - `tool.json`: Category Model, transaction none, timeout 30, destructive false, parameters `typeFilter` (string), `limit` (int 1-200), `setSelection` (bool).
   - `code.cs`: Queries `selector.GetAllObjectsWithType` or `selector.GetAllObjects()`, optionally selects via `UI.ModelObjectSelector`.
   - `examples.json`: 2 examples.
3. `Property/get_part_properties`:
   - `tool.json`: Category Property, transaction none, timeout 30, destructive false, parameters `objectId` (int, required), `includeUserProperties` (bool).
   - `code.cs`: Selects object by ID, reads profile, material, report properties (LENGTH, WEIGHT, VOLUME), and UDAs.
   - `examples.json`: 2 examples.
4. `Geometry/create_beam`:
   - `tool.json`: Category Geometry, transaction auto, timeout 30, destructive false, parameters `startX`, `startY`, `startZ`, `endX`, `endY`, `endZ` (required), `profile`, `material`, `name`, `partClass`.
   - `code.cs`: Inserts `new Beam { StartPoint = ..., EndPoint = ... }`.
   - `examples.json`: 2 examples.
5. `Geometry/create_column`:
   - `tool.json`: Category Geometry, transaction auto, timeout 30, destructive false, parameters `x`, `y` (required), `baseZ`, `topZ`, `profile`, `material`, `name`, `partClass`.
   - `code.cs`: Inserts vertical `Beam` with `Rotation = FRONT`.
   - `examples.json`: 2 examples.
6. `Geometry/create_contour_plate`:
   - `tool.json`: Category Geometry, transaction auto, timeout 30, destructive false, parameters `points` (array of {x,y,z}, required), `thickness`, `material`, `name`, `partClass`.
   - `code.cs`: Inserts `new ContourPlate` with `AddContourPoint`.
   - `examples.json`: 2 examples.
7. `Rebar/create_rebar_group`:
   - `tool.json`: Category Rebar, transaction auto, timeout 30, destructive false, parameters `fatherId`, `shapePoints`, `startX`, `startY`, `startZ`, `endX`, `endY`, `endZ` (required), `spacing`, `size`, `grade`, `name`, `rebarClass`.
   - `code.cs`: Inserts `new RebarGroup` with Polygon and spacing.
   - `examples.json`: 2 examples.
8. `Rebar/create_single_rebar`:
   - `tool.json`: Category Rebar, transaction auto, timeout 30, destructive false, parameters `fatherId`, `points` (required), `size`, `grade`, `name`, `rebarClass`.
   - `code.cs`: Inserts `new SingleRebar` with Polygon.
   - `examples.json`: 2 examples.
9. `Property/modify_user_properties`:
   - `tool.json`: Category Property, transaction auto, timeout 30, destructive false, parameters `objectId`, `properties` (dictionary, required).
   - `code.cs`: Sets UDAs via `mo.SetUserProperty(...)` and calls `mo.Modify()`.
   - `examples.json`: 2 examples.
10. `Rebar/get_reinforcement_info`:
    - `tool.json`: Category Rebar, transaction none, timeout 30, destructive false, parameters `fatherId` (optional), `limit` (default 50).
    - `code.cs`: Enumerates `Reinforcement` objects, reads size, grade, length, weight, quantity.
    - `examples.json`: 2 examples.
11. `Drawing/list_drawings`:
    - `tool.json`: Category Drawing, transaction none, timeout 30, destructive false, parameters `drawingType` (default ALL), `limit` (default 50).
    - `code.cs`: Uses `DrawingHandler.GetDrawings()`, lists mark, title, type, up-to-date status.
    - `examples.json`: 2 examples.
12. `Export/export_ifc`:
    - `tool.json`: Category Export, transaction auto, timeout 600, destructive true, tags `["export", "ifc", "destructive"]`, parameters `outputFilePath` (required), `format` (IFC4/IFC2X3), `exportSelectedOnly` (bool).
    - `code.cs`: Calls `Tekla.Structures.Model.Operations.Operation.CreateIFC4ExportFromSelected(...)`.
    - `examples.json`: 2 examples.

---

## Verification Requirements
1. Build `HPTekla.Mcp.Server.csproj` in both `Debug` and `Release` configurations:
   ```bash
   dotnet build HPTekla/HPTekla.Mcp.Server/HPTekla.Mcp.Server.csproj -c Debug
   dotnet build HPTekla/HPTekla.Mcp.Server/HPTekla.Mcp.Server.csproj -c Release
   ```
   Both must build with 0 errors and 0 warnings.
2. Verify all 12 seed tools are embedded properly and have valid JSON and C# code.
3. Write a comprehensive report in `.agents/teamwork_preview_worker_m3/report.md` and handoff in `.agents/teamwork_preview_worker_m3/handoff.md`.
4. Message the orchestrator upon completion.

## 2026-09-21T18:20:24Z
You are teamwork_preview_worker_m3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3
Your dispatch assignment is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3\DISPATCH.md
The authoritative user request is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md under header ## 2026-09-21T17:20:33Z.
Survey Reference: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_3\report.md
Repo root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

Objective:
Implement Milestone 3 (HPTekla.Mcp.Server .NET 10 Console Stdio Server):
1. Create HPTekla/HPTekla.Mcp.Server/HPTekla.Mcp.Server.csproj
2. Implement Program.cs
3. Implement Hosts/Tekla/TeklaHostProfile.cs
4. Implement Hosts/Tekla/Tools/ExecuteTeklaCodeTool.cs and GetTeklaContextTool.cs
5. Implement Hosts/Tekla/Resources/TeklaResourceProvider.cs and Hosts/Tekla/Prompts/TeklaPromptProvider.cs
6. Implement appsettings.json
7. Implement all 12 Embedded Seed Tools under Registry/SeedLibrary/
8. Verify build: dotnet build in Release and Debug (0 errors, 0 warnings)
9. Write report.md and handoff.md
10. Send message to orchestrator


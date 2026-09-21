# Handoff Report: HPPowerBi MCP Subsystem (R2 Server, R3 Tests, R4 Skill/Docs)

**Agent**: `explorer_server_1`  
**Handoff Type**: Hard (Task Complete)  
**Target Recipient**: `orchestrator_5` (Conversation ID: `4d88b310-8910-4f85-b5a8-50216392bc6b`)  
**Artifact Path**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_server_1\report.md`

---

## 1. Observation

1. **Existing Power BI Hooks in McpShared**:
   - `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs:43,66`:
     ```csharp
     public const string PowerBiHost = "powerbi";
     ...
     PowerBiHost => "hppowerbi-mcp-" + version,
     ```
   - `McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs:41`:
     ```csharp
     public const string PowerBiPrefix = "powerbi.";
     ```
   - `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs:149-163`:
     ```csharp
     public static readonly string[] PowerBiImports = { "System", "System.Linq", "System.Collections.Generic", "System.Data", "Microsoft.AnalysisServices.Tabular", "Microsoft.AnalysisServices.AdomdClient", "HPRebar.McpBridge.Core.Scripting" };
     public static readonly string[] PowerBiGlobals = { "model", "server", "adomd", "ct", "log", "progress", "args" };
     public const int PowerBiHeavyMaxTimeoutSeconds = 600;
     ```
   - `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs:39,169-178`:
     ```csharp
     public PowerBiInfo? PowerBi { get; set; }
     ...
     public sealed record PowerBiInfo(bool IsConnected, int? AttachedPid, int? LocalPort, string? DatabaseName, string? CompatibilityLevel, bool MutationEnabled, int TableCount, int MeasureCount, int RelationshipCount);
     ```
   - `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs:110-119` and `AnalyzerProfile.cs:38-40`:
     `GuardProfile.PowerBi` denies `server.Disconnect()`, `adomd.Close()`, `MessageBox`, and namespaces `System.Windows.Forms`, `HPPowerBi.McpBridge`.
     `AnalyzerProfile.PowerBi` defines empty transaction types/methods (TOM changes are committed via `Model.SaveChanges()`).

2. **McpServerHost Bootstrap and Tool Discovery**:
   - `McpShared/HPRebar.Mcp.Server.Core/Bootstrap/McpServerHost.cs:76-82`:
     ```csharp
     builder.Services
         .AddMcpServer(...)
         .WithStdioServerTransport()
         .WithToolsFromAssembly(engine)
         .WithToolsFromAssembly(profile.HostAssembly)
         .WithResourcesFromAssembly(engine)
         .WithResourcesFromAssembly(profile.HostAssembly)
         .WithPromptsFromAssembly(engine)
         .WithPromptsFromAssembly(profile.HostAssembly);
     ```
     `WithToolsFromAssembly(profile.HostAssembly)` dynamically registers all classes annotated with `[McpServerToolType]` and methods annotated with `[McpServerTool]` found in the server's assembly.

3. **RequestDispatcher Constraints**:
   - `McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs:95-131`:
     Dispatches strictly on 6 suffix methods: `ping`, `cancel`, `inspect`, `analyze`, `context`, `execute`. Any other method returns `BridgeErrorCode.MethodNotFound`.
     Consequently, specialized tools (`powerbi_get_schema`, `powerbi_evaluate_dax`, `powerbi_create_or_update_measure`, etc.) must execute their operations by invoking `ExecuteCodeService.ExecuteAsync` with standardized C# scripts over the existing `execute` method, guaranteeing zero changes are required in `McpShared`.

4. **Standalone Bridge and Server Testing Patterns**:
   - `HPSap2000/HPSap2000.Mcp.Server.Tests/Sap2000HostProfileTests.cs`: Instantiates `McpServerHost.CreateBuilder([], profile).Build()` to verify all registered tools, parameters, descriptions, and schemas without opening any host process.
   - `HPSap2000/HPSap2000.Mcp.Server.Tests/Sap2000ToolsOverPipeTests.cs`: Spawns a real named pipe listener backed by `FakeRevitExecutor` to verify JSON-RPC pipe round-trips, context filtering (`revitVersion` stripped), timeout clamping, and error translations.
   - `HPSap2000/HPSap2000.McpBridge.Tests/SapSnapshotManagerTests.cs`: Tests snapshot directory creation, presave/prerun separation, label sanitization, and pruning in temporary directories without needing a running CAD process.

---

## 2. Logic Chain

1. **Step 1 (Profile & Server Structure)**:
   From Observation 1 and 2, `McpShared` already contains the necessary wire DTOs and guard profiles for Power BI. Implementing `PowerBiHostProfile` following `Sap2000HostProfile` allows `HPPowerBi.Mcp.Server` to reuse `McpServerHost.RunAsync(args, PowerBiHostProfile.Instance)`.
2. **Step 2 (Tool Implementation & Dispatch)**:
   From Observation 3, `RequestDispatcher` is sealed and only supports `execute`, `context`, `inspect`, `cancel`, `ping`, `analyze`. Implementing the 8 Core Local Tools by building and executing safe C# scripts through `ExecuteCodeService.ExecuteAsync` allows them to inherit all bridge-side safety mechanisms (UI opt-in gating, TMDL snapshots, ScriptGuard) with zero changes to `McpShared`.
3. **Step 3 (Cloud REST Architecture)**:
   From `ORIGINAL_REQUEST.md`, Cloud REST tools (`powerbi_cloud_*`) interact with `api.powerbi.com`. Encapsulating this behind `IPowerBiCloudService` allows using MSAL credentials from local configuration while enabling 100% offline mocking via `MockHttpMessageHandler` in unit tests.
4. **Step 4 (Test Reliability in CI)**:
   From Observation 4, following the `HPSap2000.Mcp.Server.Tests` and `HPSap2000.McpBridge.Tests` pattern ensures that port detection (synthetic temp files), DAX serialization (in-memory `DataTable`), snapshots (temp dirs), and pipe communication (`FakeRevitExecutor`) can run in CI with zero real dependencies on `PBIDesktop.exe` or Azure.
5. **Step 5 (Documentation & Skill Alignment)**:
   Following `hp-mcp-sap2000/SKILL.md` and repository standards in `AGENTS.md` guarantees seamless integration with host coding agents (Claude Code, Antigravity) and preserves skill synchronization integrity.

---

## 3. Caveats

1. **Power BI Desktop Process Architecture**: Power BI Desktop runs `msmdsrv.exe` (SQL Server Analysis Services tabular engine) as a child process. Writing to `msmdsrv.port.txt` occurs asynchronously after startup; the port discovery parser must handle file-lock retry semantics gracefully.
2. **DAX Formatter Offline Fallback**: If external internet access is restricted in CI or strict enterprise environments, `powerbi_format_dax` must rely on a local deterministic tokenizer/formatter rather than requiring HTTP connectivity to `api.daxformatter.com`.
3. **Cloud REST Permissions**: Service Principal authentication requires Tenant Admin consent on Power BI Service API settings ("Allow service principals to use Power BI APIs"). The skill documentation must clearly advise users on these tenant settings.

---

## 4. Conclusion

1. `HPPowerBi.Mcp.Server` can be cleanly implemented as a .NET 10 console application reusing `McpShared.McpServerHost` and exposing 12 core/cloud tools + 8 registry meta-tools.
2. The implementation requires **zero modifications to `McpShared`**, ensuring that `HPRebar.Mcp.Server.Core.Tests` continues to pass 100% with no regressions.
3. Automated test suites (`HPPowerBi.McpBridge.Tests` and `HPPowerBi.Mcp.Server.Tests`) can achieve 100% pass rate in offline CI environments using isolated in-memory and filesystem mocks.
4. All detailed contracts, tool schemas, and documentation artifacts are compiled in `report.md`.

---

## 5. Verification Method

1. **Inspect Generated Report**:
   ```bash
   view_file g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_server_1\report.md
   ```
2. **Verify McpShared Baseline Stability**:
   ```bash
   dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests
   ```
3. **Verify HostProfile Invariants**:
   Inspect `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`, `JsonRpcMethods.cs`, and `HostScriptContracts.cs` to confirm exact alignment with `PowerBiHostProfile` specification.

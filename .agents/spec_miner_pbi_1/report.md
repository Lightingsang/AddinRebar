# Architectural Specification & Contract Mining Report: Power BI MCP Subsystem (HPPowerBi)

**Author:** spec_miner_pbi_1 (Teamwork Preview Specification Miner)  
**Date:** 2026-09-21  
**Target Repository:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar`  
**Working Directory:** `.agents/spec_miner_pbi_1/`  
**Authoritative Input:** `ORIGINAL_REQUEST.md` (Section `## 2026-09-21T06:10:48Z`), `AGENTS.md`, and authoritative source code in `McpShared/`, `HPEtabs/`, `HPSap2000/`, `HPNavis/`.

---

## 1. Executive Summary

This report establishes the authoritative architectural contract, component specifications, and boundary rules for building the new **Power BI MCP Subsystem (`HPPowerBi`)** within the AddinRebar repository.

By systematically probing the existing standalone desktop bridge implementations (`HPEtabs` and `HPSap2000`) as well as the shared host-neutral MCP core (`McpShared`), this investigation proves:
1. **Zero Breaking Changes to McpShared:** The wire protocol (`HPRebar.Mcp.Contracts`), named pipe conventions (`PipeNaming.PowerBiHost = "powerbi"`, `"hppowerbi-mcp-2026"`), wire prefixes (`JsonRpcMethods.PowerBiPrefix = "powerbi."`), script contracts (`HostScriptContracts.PowerBiImports`/`PowerBiGlobals`/`PowerBiHeavyMaxTimeoutSeconds`), context snapshots (`ContextResult.PowerBi`/`PowerBiInfo`), guard profiles (`GuardProfile.PowerBi`), and analyzer profiles (`AnalyzerProfile.PowerBi`) **are already present and landed in `McpShared`**.
2. **Standalone Desktop Bridge Pattern:** Unlike the in-process add-ins (Revit, AutoCAD, Civil 3D, Navisworks), Power BI connects to out-of-process local Analysis Services (`msmdsrv.exe`) via AMO-TOM and ADOMD.NET. `HPPowerBi.McpBridge` must follow the pattern of `HPEtabs` and `HPSap2000`: a standalone WPF desktop app (.NET 8.0-windows) running its own process, using loose `MaterialDesignThemes 5.3.2` binaries (zero ILRepack required), persisting settings via `BridgeSettingsStore("HPPowerBi", "McpBridge")`, and listening on named pipe `hppowerbi-mcp-2026`.
3. **NuGet-Based Modern Driver Stack:** While ETABS and SAP2000 depend on locally installed COM wrappers (`ETABSv1.dll`, `SAP2000v1.dll`), Power BI utilizes pure, official NuGet packages (`Microsoft.AnalysisServices.NetCore.retail`, `Microsoft.AnalysisServices.AdomdClient.NetCore.retail`, `Microsoft.Identity.Client`). This enables 100% reliable compilation and headless CI/CD execution without requiring third-party native COM installations.

---

## 2. Architectural Blueprint & Repository Boundaries

### 2.1 Repository Directory Layout
Per `AGENTS.md`, the repository permits dependencies strictly in the direction: `Host Subsystem` → `McpShared`. MCP folders must never reference each other.

```
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\
├── McpShared/                         # Host-neutral engine (zero host references)
│   ├── HPRebar.Mcp.Contracts/         # Wire DTOs, PipeNaming, JsonRpc (netstandard2.0; net48)
│   ├── HPRebar.McpBridge.Core/        # Pipe listener, Roslyn guard/compiler, settings (net8.0; net48)
│   ├── HPRebar.Mcp.Server.Core/       # Server bootstrap, registry engine, CLI (net10.0)
│   └── HPRebar.Mcp.Server.Core.Tests/ # Engine tests (net10.0)
│
├── HPPowerBi/                         # Dedicated Power BI Subsystem (Deliverable #7)
│   ├── HPPowerBi.slnx                 # XML solution file
│   ├── global.json                    # Pins .NET SDK 10.0.300 and Microsoft.Testing.Platform
│   ├── Directory.Build.props          # Subsystem-wide properties and target framework defaults
│   ├── README.md                      # Subsystem documentation & quickstart
│   ├── HPPowerBi.McpBridge/           # Standalone WPF Desktop Bridge (.NET 8.0-windows)
│   │   ├── App.xaml / App.xaml.cs     # WPF Application entry & lifetime management
│   │   ├── BridgeEntry.cs             # Engine bootstrapping, logging, and pipe setup
│   │   ├── Model/                     # Script globals, local instance records
│   │   ├── Service/                   # Port detection, AMO-TOM, ADOMD.NET, Snapshots, Cloud REST
│   │   ├── View/                      # MaterialDesign 5.3.2 MVVM status window
│   │   ├── ViewModel/                 # CommunityToolkit.Mvvm status view model
│   │   └── Resources/                 # Icons, MaterialBridge.xaml, ThemeLight/ThemeDark
│   ├── HPPowerBi.McpBridge.Tests/     # Bridge unit & integration tests (.NET 8.0-windows)
│   ├── HPPowerBi.Mcp.Server/          # Stdio MCP Server Exe (.NET 10.0 Console)
│   │   ├── Program.cs                 # 1-line bootstrap via McpServerHost.RunAsync
│   │   ├── Hosts/PowerBi/             # PowerBiHostProfile implementation
│   │   ├── Tools/                     # Core tabular tools, DAX evaluators, measure tools, cloud tools
│   │   ├── Prompts/                   # Host prompts for DAX & Tabular modeling
│   │   ├── Resources/                 # Scheme `powerbi://` model resources
│   │   └── Seeds/                     # Embedded C# seed tool library
│   └── HPPowerBi.Mcp.Server.Tests/    # Server unit tests (.NET 10.0, xUnit v3 / MTP)
```

### 2.2 Solution File (`HPPowerBi.slnx`)
The solution file must follow the modern XML format (`.slnx`) mirroring `HPEtabs.slnx` and `HPSap2000.slnx`:

```xml
<Solution>
  <Configurations>
    <BuildType Name="Debug" />
    <BuildType Name="Release" />
  </Configurations>
  <Folder Name="/Solution Items/">
    <File Path="global.json" />
    <File Path="Directory.Build.props" />
    <File Path="README.md" />
  </Folder>
  <Project Path="HPPowerBi.McpBridge/HPPowerBi.McpBridge.csproj" />
  <Project Path="HPPowerBi.McpBridge.Tests/HPPowerBi.McpBridge.Tests.csproj" />
  <Project Path="HPPowerBi.Mcp.Server/HPPowerBi.Mcp.Server.csproj" />
  <Project Path="HPPowerBi.Mcp.Server.Tests/HPPowerBi.Mcp.Server.Tests.csproj" />
  <Folder Name="/Shared/">
    <Project Path="../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj" />
    <Project Path="../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj" />
    <Project Path="../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj" />
  </Folder>
</Solution>
```

---

## 3. Authoritative Contract Review in McpShared

Inspection of `McpShared/` reveals that all foundational constants and profiles for Power BI were anticipatorily added and verified:

### 3.1 Named Pipe & Method Conventions (`HPRebar.Mcp.Contracts`)
- **Pipe Name:** Defined in `PipeNaming.cs`:
  - `PipeNaming.PowerBiHost = "powerbi";`
  - `PipeNaming.For("powerbi", 2026)` → `"hppowerbi-mcp-2026"`.
- **Wire Prefixes & Suffixes:** Defined in `JsonRpcMethods.cs`:
  - `JsonRpcMethods.PowerBiPrefix = "powerbi."`
  - Canonical request methods: `powerbi.ping`, `powerbi.context`, `powerbi.inspect`, `powerbi.execute`, `powerbi.cancel`, `powerbi.analyze`.
  - Canonical notification methods: `powerbi.progress`, `powerbi.status`, `powerbi.log`.
  - The bridge dispatches on the suffix (`execute`, `context`), accepting both prefixed and raw suffixes.

### 3.2 Context & Execution Wire Payloads (`HPRebar.Mcp.Contracts.Messages`)
- **Context Result:** `ContextResult.PowerBi` holds `PowerBiInfo`:
  ```csharp
  public sealed record PowerBiInfo(
      bool IsConnected,
      int? AttachedPid,
      int? LocalPort,
      string? DatabaseName,
      string? CompatibilityLevel,
      bool MutationEnabled,
      int TableCount,
      int MeasureCount,
      int RelationshipCount);
  ```
  *Serialization rule:* In `BridgeJson.Options`, `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`. All non-PowerBI fields (`Autocad`, `Navis`, `Etabs`, `Civil3d`, `Sap2000`) remain null and are omitted from serialized output. `ContextService.Shape()` strips Revit-only fields (`RevitVersion`, `IsFamily`) for non-Revit hosts.
- **Execution Request:** `ExecuteRequest(string Code, string Transaction = "auto", bool DryRun = false, int TimeoutSeconds = 30, string? Label = null, JsonElement? Args = null)`
- **Execution Result:** `ExecuteResult` contains `IsError`, `Value`, `ValueType`, `Message`, `Logs`, `Diagnostics`, `DurationMs`, `Truncated`, `RunId`, `Hint`, and `Snapshot`.
  *Snapshot semantics:* For Power BI, `ExecuteResult.Snapshot` carries the filename of the metadata/TMDL backup taken prior to mutation.

### 3.3 Bridge Core Infrastructure (`HPRebar.McpBridge.Core`)
- **Bridge Host:** `McpBridgeHost` provides the bridge state machine:
  ```csharp
  new McpBridgeHost(
      executor, 
      settings, 
      store, 
      hostVersion: "2026", 
      pipeName: PipeNaming.For(PipeNaming.PowerBiHost, 2026), 
      hostName: "Power BI", 
      methodPrefix: JsonRpcMethods.PowerBiPrefix, 
      executionDisabledMessage: "Code execution is disabled. Ask the user to tick 'Allow AI model modification' in the HPPowerBi MCP Bridge window.");
  ```
- **Settings Storage:** Persisted to `%AppData%\HPPowerBi\McpBridge\settings.json` via `new BridgeSettingsStore("HPPowerBi", "McpBridge")`.
  - `ExecutionEnabled` is stripped on save and defaults to `false` on start.

---

## 4. Standalone Desktop Bridge Patterns (HPEtabs & HPSap2000)

### 4.1 Process Model
- `HPPowerBi.McpBridge` is a standalone `WinExe` (.NET 8.0-windows, WPF).
- It runs independently of Power BI Desktop:
  - Users launch `HPPowerBi.McpBridge.exe` directly from Windows Explorer, or
  - Power BI Desktop launches it via an External Tool ribbon button registered in `.pbitool.json`.
- The application manages its own lifecycle:
  - `App.xaml` (`ShutdownMode="OnMainWindowClose"`).
  - Intercepts `Window.Closing`: if an AI script or long query is in-flight, warns the user.
  - Exiting cleanly shuts down the pipe listener and flushes Serilog.

### 4.2 Theming: MaterialDesignThemes 5.3.2 Integration
- **Assembly Isolation:** Because `HPPowerBi.McpBridge` runs in its own process, **ILRepack is NOT required**. `MaterialDesignThemes` ships as standard loose DLLs beside the executable.
- **Theme Dictionary Hierarchy:**
  - `MaterialBridge.xaml`: Merges `md:CustomColorTheme` (`PrimaryColor="#0696D7"`, `SecondaryColor="#E0641E"`) and `MaterialDesign2.Defaults.xaml`. Re-bases legacy button/textbox styles on MaterialDesign styles.
  - `ThemeLight.xaml` & `ThemeDark.xaml`: Define color tokens (`Color.*`, `Brush.*`).
  - `PowerBiTheme.xaml`: Defines geometry, typography, spacing tokens.
  - `WindowsHostTheme.cs`: Implements `IHostTheme`, monitoring `SystemEvents.UserPreferenceChanged` and checking `Theme.GetSystemTheme() == BaseTheme.Dark` (with environment variable override `HPPOWERBI_MCP_BRIDGE_THEME=dark|light`).
  - `MaterialThemeBridge.Attach(window, WindowsHostTheme.Instance)`: Dynamically rebuilds and injects the top-level merged resource overlay on dark/light switch.

### 4.3 3-Layer Safety Architecture
1. **Layer 1 (UI Opt-in Switch):** `ExecutionEnabled` / `MutationEnabled` checkbox. Off on every startup. If unchecked, any state-altering script or tool call fails immediately with JSON-RPC error code `-32001` (`ExecutionDisabled`).
2. **Layer 2 (Syntax & Operation Validation):**
   - DAX validation before evaluating expressions or saving measures.
   - Guard profile inspection: block dangerous methods (`server.Disconnect()`, `adomd.Close()`, `MessageBox.Show()`, reflection, process spawning).
3. **Layer 3 (Metadata / TMDL Snapshot Pre-save):**
   - Before applying mutations to the TOM model via `model.SaveChanges()`, the bridge serializes the complete model metadata/TMSL/TMDL to a timestamped file in `%LocalAppData%\HPPowerBi\McpBridge\snapshots\{databaseName}\prerun\backup_{timestamp}.json`.
   - The filename is returned in `ExecuteResult.Snapshot`.
   - Pruning retention keeps the newest 10 snapshots per model.

---

## 5. Technical Specification: Local Power BI Desktop Integration

### 5.1 Local Instance Discovery & Port Extraction
Power BI Desktop hosts an internal instance of SSAS Tabular (`msmdsrv.exe`).
The bridge automatically detects instances through two coordinated mechanisms:

1. **Process Inspection:**
   - Scan running processes for `PBIDesktop.exe`.
   - Read the window title to extract the report name (e.g. `"SalesDashboard - Power BI Desktop"` → `"SalesDashboard"`).
2. **Port File Discovery:**
   - Locate `%LocalAppData%\Microsoft\Power BI Desktop\AnalysisServicesWorkspaces\`.
   - Iterate over active workspace directories matching `AnalysisServicesWorkspace_*`.
   - Read `Data\msmdsrv.port.txt`.
   - The file contains the TCP port number (stored in UTF-16 LE format, e.g. `54321`).
3. **Multi-Instance Support (Instance Switcher):**
   - When multiple Power BI reports are open, each has its own workspace folder and port.
   - The Bridge Status Window provides a combo box listing all detected instances: `[Port] ReportTitle (PID)`.
   - Selecting an instance connects the bridge to that specific instance's local port.

### 5.2 External Tools Registration (`.pbitool.json`)
Power BI Desktop supports registering third-party tools in its ribbon tab "External Tools".
- **Path:** `%CommonProgramFiles%\Microsoft Shared\Power BI Desktop\External Tools\HPPowerBi.pbitool.json`
- **Schema:**
  ```json
  {
    "version": "1.0",
    "name": "HP Power BI MCP",
    "operationType": "ExternalTool",
    "executable": "<Absolute_Path_To_HPPowerBi.McpBridge.exe>",
    "arguments": "\"%server%\" \"%database%\"",
    "iconData": "data:image/png;base64,...",
    "tooltip": "HP MCP Bridge for Power BI: AI automation, DAX evaluation, and tabular modeling."
  }
  ```
- **Command-line launch arguments:**
  When launched via External Tools, Power BI passes the server host/port and database name. The bridge parses these arguments and connects automatically.

### 5.3 Local Driver Connectivity
- **AMO-TOM (`Microsoft.AnalysisServices.NetCore.retail`):**
  ```csharp
  var server = new Microsoft.AnalysisServices.Tabular.Server();
  server.Connect($"Data Source=localhost:{port};");
  var database = server.Databases[0];
  var model = database.Model;
  ```
- **ADOMD.NET (`Microsoft.AnalysisServices.AdomdClient.NetCore.retail`):**
  ```csharp
  var adomd = new Microsoft.AnalysisServices.AdomdClient.AdomdConnection($"Data Source=localhost:{port};");
  adomd.Open();
  ```

---

## 6. Technical Specification: Power BI Cloud REST API Integration

The subsystem includes a headless client for Power BI Service REST API using MSAL authentication:
- **Authentication Stack:** `Microsoft.Identity.Client` (MSAL.NET).
- **Supported Auth Flows:**
  1. Service Principal (`ConfidentialClientApplicationBuilder` with Client ID + Secret + Tenant ID).
  2. User OAuth 2.0 (`PublicClientApplicationBuilder` with Interactive / Device Code / Cached Token).
- **Scope:** `https://analysis.windows.net/powerbi/api/.default`.
- **Target REST Endpoints:**
  - `GET https://api.powerbi.com/v1.0/myorg/groups` (List workspaces)
  - `GET https://api.powerbi.com/v1.0/myorg/groups/{groupId}/datasets` (List datasets)
  - `POST https://api.powerbi.com/v1.0/myorg/groups/{groupId}/datasets/{datasetId}/refreshes` (Trigger refresh)
  - `POST https://api.powerbi.com/v1.0/myorg/groups/{groupId}/datasets/{datasetId}/executeQueries` (Execute DAX against Power BI Service dataset)

---

## 7. Power BI Scripting Environment Specification

### 7.1 Script Imports (`HostScriptContracts.PowerBiImports`)
Scripts executed via `execute_powerbi_code` automatically receive:
```csharp
"System",
"System.Linq",
"System.Collections.Generic",
"System.Data",
"Microsoft.AnalysisServices.Tabular",
"Microsoft.AnalysisServices.AdomdClient",
"HPRebar.McpBridge.Core.Scripting"
```

### 7.2 Script Globals (`HostScriptContracts.PowerBiGlobals`)
Scripts can access the following globals directly:
| Global | Type | Description |
|---|---|---|
| `model` | `Microsoft.AnalysisServices.Tabular.Model` | The active TOM Tabular Model |
| `server` | `Microsoft.AnalysisServices.Tabular.Server` | Connected Analysis Services Server |
| `adomd` | `Microsoft.AnalysisServices.AdomdClient.AdomdConnection` | Active ADOMD connection for DAX queries |
| `ct` | `CancellationToken` | Cooperative cancellation token |
| `log` | `Action<string>` | Logging delegate, routed to bridge and server stderr |
| `progress` | `Action<int, int?, string?>` | Progress callback (`current`, `total`, `message`) |
| `args` | `ScriptArgs` | Dynamic parameter container passed via `args` property |

### 7.3 Script Guard Profile (`GuardProfile.PowerBi`)
Enforces strict security checks via Roslyn syntax walker:
- **Base Deny-List:** `System.IO`, `System.Net`, `System.Diagnostics.Process`, `System.Reflection`, `System.Runtime.InteropServices`, `System.Threading.Tasks`, `System.Security`, `Microsoft.Win32`, `Microsoft.CodeAnalysis`, `System.Linq.Expressions`.
- **Keywords/Constructs Blocked:** `await`, `async` lambdas, `unsafe`, `dynamic`, `#r`, `#load`.
- **Denied Identifiers:** `MessageBox`.
- **Denied Members:** `Disconnect`, `Dispose`.
- **Scoped Member Denials:**
  - `server.Disconnect()`, `server.Dispose()`
  - `adomd.Close()`, `adomd.Dispose()`
- **Denied Namespaces:** `System.Windows.Forms`, `HPPowerBi.McpBridge`, `HPRebar.McpBridge.Core.Host`.

### 7.4 Script Analyzer Profile (`AnalyzerProfile.PowerBi`)
- `TransactionTypeNames = []`
- `TransactionMethodNames = []`
- Power BI Tabular models do not utilize database transactions in scripts; changes are staged in-memory on TOM objects and persisted atomically with `model.SaveChanges()`.

### 7.5 Execution Timeouts
- Default: 30 seconds.
- Ceiling: 600 seconds (`HostScriptContracts.PowerBiHeavyMaxTimeoutSeconds = 600`).

---

## 8. Authoritative Features Discovered

The following tables document all discovered features and capabilities for the Power BI MCP Subsystem.

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Core MCP | `get_powerbi_context` | Returns full snapshot of local Power BI Desktop instance, port, database, model statistics, and safety status. | None | `ContextResult` (with `PowerBiInfo` details) | Bridge unavailable if bridge is not running. | `McpShared/ContextService.cs` & `ContextMessages.cs` |
| 2 | Core MCP | `execute_powerbi_code` | Executes arbitrary C# Roslyn script with `model`, `server`, and `adomd` globals. | `code` (string), `transaction` (string), `dryRun` (bool), `timeoutSeconds` (int), `args` (object) | `ExecuteResult` containing returned value, logs, diagnostics, and snapshot filename | Guard violations return `GUARD` diagnostics; compile errors return line/col diagnostics; mutation without opt-in returns `-32001`. | `HPRebar.McpBridge.Core.Scripting` & `HostScriptContracts.cs` |
| 3 | Core MCP | `powerbi_get_schema` | Inspects and returns full tabular model schema (tables, columns, measures, relationships, data types). | `includeHidden` (bool, opt), `tableFilter` (string, opt) | JSON schema object with tables, columns, measures, and relationships | Returns error if not connected to Analysis Services. | `ORIGINAL_REQUEST.md` R2 |
| 4 | Core MCP | `powerbi_evaluate_dax` | Runs DAX query locally via ADOMD.NET and formats tabular results. | `query` (string), `maxRows` (int, default 100) | Formatted table text/JSON, row count, execution duration ms | DAX syntax errors return detailed ADOMD error message. | `ORIGINAL_REQUEST.md` R2 |
| 5 | Core MCP | `powerbi_create_or_update_measure` | Creates or updates a measure in a specified table using TOM. | `tableName` (string), `measureName` (string), `daxExpression` (string), `formatString` (opt), `description` (opt), `displayFolder` (opt) | Confirmation message, updated measure metadata, snapshot filename | Throws if table does not exist or if mutation opt-in is disabled. | `ORIGINAL_REQUEST.md` R2 |
| 6 | Core MCP | `powerbi_delete_measure` | Deletes a measure from a specified table. | `tableName` (string), `measureName` (string) | Confirmation message, snapshot filename | Throws if measure not found or mutation opt-in disabled. | `ORIGINAL_REQUEST.md` R2 |
| 7 | Core MCP | `powerbi_manage_relationship` | Creates, updates, or deletes a relationship between tables. | `action` ("create"\|"delete"\|"toggle"), `fromTable`, `fromColumn`, `toTable`, `toColumn`, `isActive` (bool), `crossFiltering` (string) | Updated relationship summary, snapshot filename | Fails if columns/tables invalid or creates circular ambiguity. | `ORIGINAL_REQUEST.md` R2 |
| 8 | Core MCP | `powerbi_format_dax` | Formats and beautifies DAX expression into readable syntax. | `daxExpression` (string) | Formatted DAX string | Returns original DAX with warning if formatting fails. | `ORIGINAL_REQUEST.md` R2 |
| 9 | Cloud MCP | `powerbi_cloud_list_workspaces` | Lists Power BI Service workspaces via REST API. | None | List of workspaces (Id, Name, IsReadOnly, Type) | Fails with 401/403 if unauthenticated. | `ORIGINAL_REQUEST.md` R2 |
| 10 | Cloud MCP | `powerbi_cloud_list_datasets` | Lists datasets in a Power BI Service workspace. | `workspaceId` (string) | List of datasets (Id, Name, ConfiguredBy, IsRefreshable) | Fails if workspace not found or credentials invalid. | `ORIGINAL_REQUEST.md` R2 |
| 11 | Cloud MCP | `powerbi_cloud_trigger_refresh` | Triggers background data refresh for a dataset in Power BI Service. | `workspaceId` (string), `datasetId` (string) | Refresh request status and requestId | Fails if refresh already in progress or permissions missing. | `ORIGINAL_REQUEST.md` R2 |
| 12 | Cloud MCP | `powerbi_cloud_execute_dax` | Executes DAX query against a Power BI Service dataset via REST API. | `workspaceId` (string), `datasetId` (string), `query` (string) | Query results in tabular JSON format | Fails on query syntax error or quota exceeded. | `ORIGINAL_REQUEST.md` R2 |
| 13 | Registry | `list_tools`, `get_tool`, `test_tool`, `run_tool`, `propose_tool`, `review_tool`, `deprecate_tool` | Dynamic MCP tool registry meta-tools. | Varies per tool | Dynamic tool execution and lifecycle management | Validated via `ToolValidator`; shadowed core tools rejected. | `McpShared/HPRebar.Mcp.Server.Core/Registry` |
| 14 | Bridge Host | Local Port Auto-Discovery | Scans `AnalysisServicesWorkspaces` and reads `msmdsrv.port.txt` to find port. | None | TCP port integer | Gracefully reports "PBIDesktop not running or no model loaded". | Investigation of `msmdsrv.port.txt` |
| 15 | Bridge Host | External Tools Auto-Registration | Generates and installs `HPPowerBi.pbitool.json` into External Tools directory. | None | File written to `%CommonProgramFiles%` | Logs warning if directory requires admin privileges. | `ORIGINAL_REQUEST.md` R1 |
| 16 | Bridge Host | Metadata / TMDL Snapshot Manager | Serializes model metadata prior to mutations into snapshot directory. | Database/Model instance | Timestamped JSON snapshot file path | Logs error if disk full; blocks mutation if snapshot fails. | Pattern from `HPEtabs`/`HPSap2000` |
| 17 | Bridge UI | Instance Switcher | Dropdown in bridge UI allowing selection between multiple open Power BI Desktop reports. | User selection | Reconnects TOM & ADOMD to selected instance port | Gracefully updates status if instance closes. | Requirement from R1 |
| 18 | Bridge UI | Windows Theme Synchronization | Dynamically synchronizes WPF theme with Windows OS Light/Dark theme. | `SystemEvents.UserPreferenceChanged` | Updates `MaterialThemeBridge` resource dictionaries | Falls back to Light theme if registry unreadable. | Pattern from `HPEtabs/WindowsHostTheme.cs` |

---

## 9. Edge Cases & Boundary Conditions

| # | Feature | Input / Condition | Observed / Expected Behavior |
|---|---------|-------------------|-----------------------------|
| 1 | Local Port Detection | `PBIDesktop.exe` is running, but user has not opened any PBIX (splash screen only). | `AnalysisServicesWorkspaces` folder has no active workspace or `msmdsrv.port.txt` is missing. Bridge reports `IsConnected = false`, UI displays "Power BI Desktop running, but no report model open." |
| 2 | Multiple Instances | Multiple `PBIDesktop.exe` instances are open with different reports. | Bridge scans all workspace folders, associates PID and report window titles, and populates the Instance Switcher dropdown. The active instance can be switched without restarting the bridge. |
| 3 | Port Recycle | User closes Power BI Desktop and re-opens it (new port assigned). | Connection is dropped. Bridge detects disconnect, periodically re-scans workspace folders, and reconnects to the new port automatically upon user clicking "Reconnect" or auto-refresh. |
| 4 | Safety Gate | AI attempts to call `powerbi_create_or_update_measure` while UI checkbox "Allow AI model modification" is unchecked. | Bridge immediately refuses the request with JSON-RPC error code `-32001` (`ExecutionDisabled`) and message instructing user to check the box in the bridge window. No changes occur. |
| 5 | Mutation Snapshot Failure | Disk is full or permissions error prevents saving metadata snapshot before a write. | The mutation operation is aborted immediately before modifying the TOM model. An error is returned: "Failed to create pre-run metadata snapshot; mutation cancelled." |
| 6 | DAX Evaluation Syntax Error | DAX query passed to `powerbi_evaluate_dax` contains a syntax error (e.g. `EVALUATE NON_EXISTENT_TABLE`). | ADOMD.NET throws `AdomdErrorResponseException`. The tool catches this and returns a clean, formatted error message indicating the syntax failure and line number, without crashing the server. |
| 7 | Large Query Result | DAX query returns 500,000 rows. | Result formatter truncates results to `maxRows` (default 100, configurable up to 10,000) and sets `Truncated = true`, reporting total row count and execution duration. |
| 8 | C# Script Disconnect Attempt | AI script attempts `server.Disconnect()` or `adomd.Close()`. | `ScriptGuard` detects the method access on `server` and `adomd` and rejects the script with `GUARD` diagnostic before compilation or execution. |
| 9 | Malicious Script Constructs | AI script attempts `Process.Start("cmd.exe")` or `File.WriteAllText(...)`. | `ScriptGuard` detects `Process` / `File` identifier and rejects with `GUARD` diagnostic: `Process is not allowed in Power BI scripts.` |
| 10 | Direct `#r` / `#load` | AI script includes `#r "System.IO.dll"` trivia directive. | `ScriptGuard` inspects directive trivia and rejects with `GUARD` diagnostic: `#r is not allowed in Power BI scripts.` |
| 11 | External Tools Launch | User clicks "HP Power BI MCP" in Power BI Desktop ribbon. | PBIDesktop launches `HPPowerBi.McpBridge.exe` passing arguments `"%server%"` (e.g. `localhost:54321`) and `"%database%"`. Bridge starts up, parses args, and connects directly to the specified port. |
| 12 | Cloud Auth Expired | Access token expires during `powerbi_cloud_execute_dax`. | MSAL token cache attempts silent renewal; if refresh token expired, returns clear error indicating re-authentication is required. |

---

## 10. Concrete Recommendations for Implementation Team

1. **Keep McpShared Untouched:** Do not modify any code in `McpShared/`. All required constants, wire types, and profile definitions are already present and verified.
2. **Setup Solution & Projects:**
   - Create `HPPowerBi/global.json` pinning SDK `10.0.300` and `Microsoft.Testing.Platform`.
   - Create `HPPowerBi/Directory.Build.props` setting `LangVersion="latest"`, `Nullable="enable"`, `ImplicitUsings="enable"`.
   - Create `HPPowerBi/HPPowerBi.slnx` with the 4 subsystem projects and the 3 shared projects.
3. **Bridge Implementation (`HPPowerBi.McpBridge`):**
   - Target `net8.0-windows` with `OutputType=WinExe`.
   - Reference `CommunityToolkit.Mvvm 8.4.0`, `MaterialDesignThemes 5.3.2`, `Serilog 4.4.0`, `Serilog.Sinks.File 7.0.0`.
   - Reference `Microsoft.AnalysisServices.NetCore.retail` and `Microsoft.AnalysisServices.AdomdClient.NetCore.retail`.
   - Reference `Microsoft.Identity.Client`.
   - Reference `../McpShared/HPRebar.Mcp.Contracts` and `../McpShared/HPRebar.McpBridge.Core`.
   - Use loose DLLs (no ILRepack target).
   - Replicate the `MaterialThemeBridge` and `WindowsHostTheme` pattern from `HPEtabs`.
4. **Server Implementation (`HPPowerBi.Mcp.Server`):**
   - Target `net10.0` console application.
   - Implement `PowerBiHostProfile` for `IHostProfile`.
   - Implement core tools and cloud tools.
   - Reference `../McpShared/HPRebar.Mcp.Server.Core`.
   - `Program.cs` is simply: `return await McpServerHost.RunAsync(args, PowerBiHostProfile.Instance);`.
5. **Testing & Verification:**
   - Implement `HPPowerBi.McpBridge.Tests` for port detection, DAX serialization, and snapshot retention.
   - Implement `HPPowerBi.Mcp.Server.Tests` for `PowerBiHostProfile`, tool definitions, and registry lifecycle.
   - Ensure `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests` continues to pass 100%.

# Architecture & McpShared Integration Survey for Tekla Structures 2025 (HPTekla MCP)

- **Date**: 2026-09-22
- **Author**: Teamwork Preview Explorer Survey 1 (`teamwork_preview_explorer_survey_1`)
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_1`
- **Repo Root**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar`

---

## 1. Executive Summary

This survey analyzes the architecture of `McpShared`, the core engine that powers all host MCP integrations in this repository (Revit, AutoCAD, Navisworks, ETABS, Civil 3D, SAP2000, Power BI, Excel, Robot Structural Analysis). The goal is to determine the exact additive contracts, profiles, data transfer objects, and test extensions required to integrate **Trimble Tekla Structures 2025.0** (HPTekla MCP) cleanly, with **zero regressions** and **100% backward compatibility** across the existing 9 hosts.

### Key Findings
1. **Architectural Decoupling**: `McpShared` is cleanly split into three libraries:
   - `HPRebar.Mcp.Contracts` (`netstandard2.0;net48`): Protocol, JSON-RPC envelope, DTOs, pipe naming.
   - `HPRebar.McpBridge.Core` (`net8.0;net48`): Bridge host, pipe listener/dispatcher, Roslyn guard/compiler, queue.
   - `HPRebar.Mcp.Server.Core` (`net10.0`): Server bootstrap, pipe client, execution/context services, tool registry.
2. **Strict Host Neutrality**: `McpShared` never references any host API assembly (enforced by automated reflection assertions in `HostNeutralityTests`). Hosts are injected via plain data interfaces (`IHostProfile`, `GuardProfile`, `AnalyzerProfile`, `HostScriptContracts`).
3. **Runtime Precedent for .NET Framework 4.8**: Tekla Structures 2025 runs on CLR v4.0.30319 (.NET Framework 4.8). The repository already has a battle-tested in-process .NET Framework 4.8 host: **HPNavis** (`HPNavis.McpBridge`), which solved all .NET Framework challenges (Roslyn assembly resolution without ALC via `PluginAssemblyResolver`, pipe ACL security via `PipeSecurity`, monotonic timing via `Stopwatch` in `MainThreadQueue`). HPTekla can directly mirror HPNavis's runtime setup.
4. **Verified Baseline Pass State**:
   - `HPRebar.Mcp.Server.Core.Tests` (.NET 10): **613 passed, 0 failed, 0 skipped**.
   - `HPRebar.McpBridge.Core.Net48Tests` (.NET Framework 4.8): **72 passed, 0 failed, 0 skipped**.

---

## 2. McpShared Project Structure & Architectural Boundaries

```
McpShared/
├── HPRebar.Mcp.Contracts/             # TFM: netstandard2.0; net48
│   ├── HostScriptContracts.cs         # Per-host default script usings & globals
│   ├── PipeNaming.cs                  # Pipe name factory (hp{host}-mcp-{version})
│   ├── JsonRpc/
│   │   ├── BridgeErrorCode.cs         # JSON-RPC error codes (-32000 to -32003)
│   │   ├── BridgeJson.cs              # Global JSON serialization options
│   │   ├── JsonRpcEnvelope.cs         # Request / Response / Notification envelope
│   │   └── JsonRpcMethods.cs          # Method prefixes and suffixes
│   └── Messages/
│       ├── ContextMessages.cs         # ContextRequest, ContextResult, per-host Info DTOs
│       ├── ExecuteRequest.cs          # Script payload, transaction mode, dryRun, timeout
│       ├── ExecuteResult.cs           # Result value, error, logs, changed element count
│       └── AnalyzeMessages.cs         # Code analysis request & result
│
├── HPRebar.McpBridge.Core/            # TFM: net8.0; net48
│   ├── Host/
│   │   ├── MainThreadQueue.cs         # Thread marshalling & idle tick processor
│   │   └── McpBridgeHost.cs           # Bridge state machine & lifecycle controller
│   ├── Model/
│   │   ├── BridgeSettings.cs          # Host settings (ExecutionEnabled, AutoStart, etc.)
│   │   └── BridgeSettingsStore.cs     # %AppData%/{Vendor}/{Product}/ persistence
│   ├── Pipe/
│   │   ├── PipeListener.cs            # Named-pipe server (net8 CurrentUserOnly / net48 PipeSecurity)
│   │   ├── RequestDispatcher.cs       # Suffix-based JSON-RPC router (method neutral)
│   │   ├── IBridgeExecutor.cs         # Interface implemented by host-specific bridge
│   │   └── NdjsonPipeWriter.cs        # Thread-safe NDJSON socket writer
│   └── Scripting/
│       ├── GuardProfile.cs            # AST syntax deny-list profiles
│       ├── AnalyzerProfile.cs         # Transaction detection profiles
│       ├── ScriptGuard.cs             # Roslyn syntax tree security validator
│       ├── ScriptAnalyzer.cs          # AST analyzer for transaction & argument usage
│       ├── ScriptCompiler.cs          # Roslyn C# script compiler & cache
│       ├── ScriptArgs.cs              # Type-safe parameter accessor for scripts
│       └── ScriptUnits.cs             # Unit conversion utilities
│
└── HPRebar.Mcp.Server.Core/           # TFM: net10.0
    ├── Bootstrap/
    │   └── McpServerHost.cs           # Application host builder, DI container, CLI router
    ├── Hosts/
    │   ├── IHostProfile.cs            # Polymorphic contract for CAD host metadata
    │   └── HostProfile.cs             # Generic data-driven implementation of IHostProfile
    ├── Models/
    │   └── BridgeOptions.cs           # Named pipe connection & timeout options
    ├── Services/
    │   ├── RevitBridgeClient.cs       # Named pipe client connection manager
    │   ├── ExecuteCodeService.cs      # Script execution orchestrator & timeout clamper
    │   ├── ContextService.cs          # Model context extractor & shape normalizer
    │   └── ResultFormatter.cs         # MCP CallToolResult formatter & path sanitizer
    └── Registry/
        ├── ToolValidator.cs           # Schema, AST, and category validator for dynamic tools
        ├── ToolManager.cs             # Execution coordinator for dynamic registry tools
        ├── DynamicToolRegistrar.cs    # Runtime MCP tool publisher (list_changed events)
        └── ToolLifecycleService.cs    # Tool proposal, review, testing, and lifecycle
```

### Dependency Rules
- **MCP Folders -> `McpShared`**: Any host project (`HPRebar/`, `HPNavis/`, `HPTekla/`) references only projects in `McpShared/`.
- **Zero Cross-Host References**: `HPTekla` must never reference `HPRebar`, `HPNavis`, `HPAutoCad`, etc.
- **Zero Host References in McpShared**: `McpShared` must never reference `Autodesk.*`, `Tekla.*`, `CSI.*`, or any host package.

---

## 3. Comparison of Existing Host Integrations

The repository has integrated 9 hosts across multiple paradigms:

| Host | Host ID | Versioning | Process Model | TFM | Named Pipe | Method Prefix | Transaction / Commit Model | Max Timeout |
|---|---|---|---|---|---|---|---|---|
| **Revit** | `revit` | 2025, 2026 | In-process Add-In | `net8.0` / `net48` | `hprebar-mcp-r{ver}` | `revit.` | Revit API `Transaction` | 120s |
| **AutoCAD** | `autocad` | 2026 | In-process Add-In | `net8.0` | `hpautocad-mcp-{ver}` | `autocad.` | AutoCAD `Transaction` (`tr`) | 120s |
| **Navisworks** | `navis` | 2026 | **In-process Plugin** | **`net48`** | `hpnavis-mcp-{ver}` | `navis.` | Bridge-owned Undo Transaction | 600s |
| **ETABS** | `etabs` | 22 (CSI no.) | Out-of-proc COM app | `net8.0-windows` | `hpetabs-mcp-{ver}` | `etabs.` | None (3-Tier + `.EDB` Snapshot) | 600s |
| **Civil 3D** | `civil3d` | 2026 | In-process Add-In | `net8.0` | `hpcivil3d-mcp-{ver}` | `civil3d.` | AutoCAD `Transaction` (`tr`) | 120s |
| **SAP2000** | `sap2000` | 27 (CSI no.) | Out-of-proc COM app | `net8.0-windows` | `hpsap2000-mcp-{ver}` | `sap2000.` | None (3-Tier + `.SDB` Snapshot) | 600s |
| **Power BI** | `powerbi` | 2026 | Out-of-proc Desktop app | `net8.0-windows` | `hppowerbi-mcp-{ver}` | `powerbi.` | None (AMO-TOM + TMSL Snapshot) | 600s |
| **Excel** | `excel` | 2026 | Out-of-proc Desktop app | `net8.0-windows` | `hpexcel-mcp-{ver}` | `excel.` | None (3-Tier + `.xlsx` Snapshot) | 600s |
| **Robot** | `robot` | 2026 | Out-of-proc COM app | `net8.0-windows` | `hprobot-mcp-{ver}` | `robot.` | None (3-Tier + `.rtd` Snapshot) | 300s |
| **Tekla (Proposed)** | `tekla` | 2025 | **In-process Plugin** | **`net48`** | `hptekla-mcp-{ver}` | `tekla.` | `dryRun` gates `model.CommitChanges()` | 600s |

### Deep Dive: Architectural Parallels Between Tekla and Navisworks
Tekla Structures 2025 and Navisworks Manage 2026 share crucial architectural constraints:
1. **CLR Runtime**: Both run on .NET Framework 4.8 (`net48`).
2. **No AssemblyLoadContext**: .NET Framework does not support isolated ALC. Multiple plugins or versions share the same AppDomain.
   - *Solution in HPNavis*: `PluginAssemblyResolver.cs` intercepts `AppDomain.CurrentDomain.AssemblyResolve` and dynamically binds Roslyn (`Microsoft.CodeAnalysis.dll`, `Microsoft.CodeAnalysis.CSharp.dll`) and `System.Collections.Immutable.dll` from the plugin's own install directory.
   - *Application to Tekla*: `HPTekla.McpBridge` should adopt this exact `PluginAssemblyResolver` mechanism.
3. **Named Pipe Security**: On .NET Framework 4.8, `NamedPipeServerStream` lacks `PipeOptions.CurrentUserOnly`.
   - *Solution in HPNavis & McpBridge.Core*: Uses `PipeSecurity` with the Windows current user's SID granted `PipeAccessRights.FullControl`.
   - *Application to Tekla*: Built-in automatically through `HPRebar.McpBridge.Core` net48 compilation!
4. **Execution Model & Marshalling**:
   - Tekla Structures Open API is single-threaded; calling `Tekla.Structures.Model.Model` from an arbitrary thread causes concurrency errors.
   - *Solution*: Use `MainThreadQueue` from `HPRebar.McpBridge.Core` or dispatch via WPF Dispatcher / Tekla idle loop to ensure thread affinity.

---

## 4. Exact Additive Modifications Required in McpShared

The integration of Tekla Structures into `McpShared` requires edits to **6 existing files**, with **100% additive changes** (zero modifications to existing lines).

### File 1: `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`
- **Location**: Line 53 (after `RobotHost`).
- **Addition**:
  ```csharp
  /// <summary>
  ///     Trimble Tekla Structures; pipe <c>hptekla-mcp-{version}</c> (e.g. 2025).
  /// </summary>
  public const string TeklaHost = "tekla";
  ```
- **In `PipeNaming.For(string host, int version)` switch expression** (around line 79):
  ```csharp
  TeklaHost => "hptekla-mcp-" + version,
  ```

### File 2: `McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs`
- **Location**: Line 44 (after `RobotPrefix`).
- **Addition**:
  ```csharp
  public const string TeklaPrefix = "tekla.";
  ```
- **Impact on Request Dispatching**:
  `RequestDispatcher.cs` handles routing by suffix:
  ```csharp
  switch (JsonRpcMethods.Suffix(method))
  {
      case JsonRpcMethods.PingSuffix: ...
      case JsonRpcMethods.CancelSuffix: ...
      case JsonRpcMethods.InspectSuffix: ...
      case JsonRpcMethods.AnalyzeSuffix: ...
      case JsonRpcMethods.ContextSuffix: ...
      case JsonRpcMethods.ExecuteSuffix: ...
  }
  ```
  Because `JsonRpcMethods.Suffix("tekla.execute") == "execute"` and `JsonRpcMethods.Suffix("tekla.context") == "context"`, `RequestDispatcher` requires **zero code changes** to support Tekla!
  Furthermore, `ProgressMethodFor("tekla.execute")` produces `"tekla.progress"`, ensuring seamless progress notifications.

### File 3: `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`
- **Location**: End of class (around line 209).
- **Addition**:
  ```csharp
  /// <summary>
  ///     Default `using`s of a Tekla Structures script. Covers the core Tekla Open API namespaces:
  ///     general structures, model objects (Beam, Column, ContourPlate, RebarGroup), geometry (Point, Vector),
  ///     and catalog definitions.
  /// </summary>
  public static readonly string[] TeklaImports =
  {
      "System", "System.Linq", "System.Collections.Generic",
      "Tekla.Structures", "Tekla.Structures.Model",
      "Tekla.Structures.Geometry3d", "Tekla.Structures.Catalogs",
      "HPRebar.McpBridge.Core.Scripting",
  };

  /// <summary>
  ///     Global names a Tekla Structures script may use: `model` is the active Tekla Model instance,
  ///     `ct` is cooperative cancellation, `log` writes output, `progress` reports steps, and `args` carries parameters.
  /// </summary>
  public static readonly string[] TeklaGlobals = { "model", "ct", "log", "progress", "args" };

  /// <summary>
  ///     Longest Tekla Structures run allowed when heavy operations are enabled (e.g. batch model updates,
  ///     IFC export, drawing generation).
  /// </summary>
  public const int TeklaHeavyMaxTimeoutSeconds = 600;
  ```

### File 4: `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`
- **Location 1**: Inside `ContextResult` class (after line 45 `RobotInfo? Robot`).
- **Addition**:
  ```csharp
  /// <summary>Tekla Structures-only facts; null for the other hosts (and omitted from the JSON).</summary>
  public TeklaInfo? Tekla { get; set; }
  ```
- **Location 2**: New record definition (after `RobotInfo` around line 222).
- **Addition**:
  ```csharp
  /// <summary>
  ///     What a Tekla Structures script needs to know: connection state, active model name and folder path,
  ///     project name, Tekla major version, whether heavy/destructive operations are enabled, and coarse
  ///     object counts (parts, rebar, drawings).
  /// </summary>
  public sealed record TeklaInfo(
      bool IsConnected,
      string? ModelName,
      string? ModelPath,
      string? ProjectName,
      string? TeklaVersion,
      bool HeavyOperationsEnabled,
      int PartCount,
      int RebarCount,
      int DrawingCount);
  ```
- **JSON Serialization Safety**: `BridgeJson.Options` configures `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`. When `Tekla` is null on Revit, AutoCAD, or Robot, the `"tekla"` property is completely omitted from the JSON payload. Zero wire drift!

### File 5: `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`
- **Location**: Inside `GuardProfile` class (around line 193).
- **Addition**:
  ```csharp
  /// <summary>
  ///     Tekla Structures (in-process .NET Framework 4.8 plugin inside TeklaStructures.exe).
  ///     Denied: Modal dialogs (MessageBox), interactive picking (Picker) that freezes the main thread
  ///     and named-pipe communication, process termination, direct commit bypass if bridge owns commit,
  ///     and bridge internals.
  /// </summary>
  public static readonly GuardProfile Tekla = new GuardProfile(
      "Tekla Structures",
      deniedIdentifiers: new[] { "MessageBox", "Picker" },
      deniedMembers: new[]
      {
          // Interactive UI picking methods that block waiting for mouse clicks
          "PickObject", "PickObjects", "PickPoint", "PickPoints", "PickLine", "PickPolygon",
          // Application shutdown
          "Exit", "Quit",
      },
      deniedMembersOnIdentifier: new Dictionary<string, string[]>(StringComparer.Ordinal)
      {
          // If bridge owns transaction commit and dryRun enforcement:
          ["model"] = new[] { "CommitChanges" },
      },
      deniedNamespaces: new[]
      {
          "System.Windows.Forms",
          "Tekla.Structures.Dialog",
          "Tekla.Structures.Drawing.UI",
          "HPTekla.McpBridge",
          "HPRebar.McpBridge.Core.Host",
      });
  ```
  *(Note on `model.CommitChanges()`: By placing `"CommitChanges"` under `deniedMembersOnIdentifier` for `"model"`, scripts are prevented from prematurely committing changes during `dryRun = true`. The bridge executor takes sole responsibility for calling `model.CommitChanges()` after the script returns successfully, only when `dryRun == false`.)*

### File 6: `McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs`
- **Location**: Inside `AnalyzerProfile` class (around line 55).
- **Addition**:
  ```csharp
  /// <summary>
  ///     Tekla Structures scripts do not open Transaction objects; commit safety is managed by the bridge
  ///     via dryRun and snapshot mechanisms.
  /// </summary>
  public static readonly AnalyzerProfile Tekla = new AnalyzerProfile(
      transactionTypeNames: Array.Empty<string>(),
      transactionMethodNames: new[] { "CommitChanges" });
  ```

---

## 5. HostProfile Definition & Execution Mechanics

### `TeklaHostProfile` Definition in `HPTekla.Mcp.Server`
Every host server exe defines its own `*HostProfile.cs` implementing `IHostProfile`. For `HPTekla.Mcp.Server`, this class is:

```csharp
namespace HPTekla.Mcp.Server.Hosts;

public sealed class TeklaHostProfile : IHostProfile
{
    public static readonly TeklaHostProfile Instance = new();

    public string HostId => PipeNaming.TeklaHost;
    public string DisplayName => "Tekla Structures";
    public string ServerName => "HPTekla MCP Server";
    public string ProductFolder => "HPTekla";
    public string EnvPrefix => "HPTEKLA_MCP_";
    public int DefaultVersion => 2025;
    public IReadOnlyCollection<int> ValidVersions => new[] { 2025 };
    public string MethodPrefix => JsonRpcMethods.TeklaPrefix;
    public string ExecuteToolName => "execute_tekla_code";
    public string ContextToolName => "get_tekla_context";
    public string ResourceScheme => "tekla";
    public IReadOnlyCollection<string> Categories => new[] { "Model", "Geometry", "Reinforcement", "Drawing", "Export", "Generic" };
    public IReadOnlyCollection<string> CoreToolNames => new[] { "execute_tekla_code", "get_tekla_context", "inspect_type", "cancel_execution" };
    public IReadOnlyCollection<string> ScriptImports => HostScriptContracts.TeklaImports;
    public string ScriptContractSummary =>
        "Globals: model (Tekla.Structures.Model.Model), ct (CancellationToken), log(string), progress(cur,total,msg), args (ScriptArgs). " +
        "Coordinates are in millimeters. Modifications are committed by the bridge; dryRun=true rolls back all changes without committing.";
    public Assembly HostAssembly => typeof(TeklaHostProfile).Assembly;
    public string CliExecutable => "HPTekla.Mcp.Server.exe";
    public int MaxTimeoutSeconds => HostScriptContracts.TeklaHeavyMaxTimeoutSeconds; // 600
    public string? BridgeNotConnectedHint => "Open Tekla Structures 2025 and ensure the HPTekla MCP Bridge extension is loaded and listening.";
    public string? TimeoutSemanticsHint => "The script may still be processing in Tekla; check the Tekla status bar before retrying.";

    public string PipeName(int version) => PipeNaming.For(HostId, version);
    public string Method(string suffix) => JsonRpcMethods.For(MethodPrefix, suffix);
}
```

### End-to-End Method Dispatch Trace
```
[AI Agent / MCP Client]
        │
        ▼ (stdio JSON-RPC: "tools/call" -> "execute_tekla_code")
[HPTekla.Mcp.Server]
        │
        ├── ExecuteCodeService.ExecuteAsync()
        │     - Validates source size (< 32KB)
        │     - Clamps timeout: Math.Clamp(timeoutSeconds, 5, 600)
        │     - Calls bridge.Profile.Method("execute") -> "tekla.execute"
        │
        ├── RevitBridgeClient.SendAsync("tekla.execute", payload)
        │
        ▼ (Named Pipe: \\.\pipe\hptekla-mcp-2025)
[HPTekla.McpBridge]
        │
        ├── PipeListener (reads NDJSON line)
        │
        ├── RequestDispatcher.HandleLineAsync()
        │     - JsonRpcMethods.Suffix("tekla.execute") -> "execute"
        │     - Verifies Settings.ExecutionEnabled
        │     - Creates progress notification: "tekla.progress"
        │
        ├── TeklaBridgeExecutor.ExecuteAsync()
        │     - AST Check: ScriptGuard.Check(code, GuardProfile.Tekla)
        │     - Tier Analysis: Read vs Write vs Heavy
        │     - Roslyn Compile: ScriptCompiler.GetOrCompile(code)
        │     - Marshals to Tekla Main Thread (via MainThreadQueue)
        │     - Runs script with globals (model, ct, log, progress, args)
        │     - If dryRun == false and succeeded: model.CommitChanges()
        │     - Returns ExecuteResult
        │
        ▼ (Named Pipe response)
[HPTekla.Mcp.Server]
        │
        ├── ResultFormatter.FromExecute(result)
        │
        ▼ (stdio JSON-RPC response)
[AI Agent / MCP Client]
```

---

## 6. Test Suite Baseline & Non-Breaking Verification Strategy

### Baseline Execution Confirmation
To establish the baseline truth, test suites in `McpShared` were executed in the repository:
1. **`HPRebar.Mcp.Server.Core.Tests`** (.NET 10):
   ```
   dotnet test HPRebar.Mcp.Server.Core.Tests
   Total: 613, Failed: 0, Succeeded: 613, Skipped: 0. Duration: 3.4s.
   ```
2. **`HPRebar.McpBridge.Core.Net48Tests`** (.NET Framework 4.8):
   ```
   dotnet test HPRebar.McpBridge.Core.Net48Tests
   Total: 72, Failed: 0, Succeeded: 72, Skipped: 0. Duration: 2.5s.
   ```

### Strategy for Adding Tekla Host Tests Without Breaking Existing Hosts

Following the established precedent seen in `RobotTestProfile.cs` / `RobotProfileTests.cs` and `NavisProfileTests.cs`:

1. **Create `McpShared/HPRebar.Mcp.Server.Core.Tests/TeklaTestProfile.cs`**:
   - Helper class constructing a mock `HostProfile` for Tekla with `HostId = "tekla"`, `DefaultVersion = 2025`, `ValidVersions = [2025]`, `MethodPrefix = "tekla."`.
   - Helper factory for candidate tools and test pipe names.

2. **Create `McpShared/HPRebar.Mcp.Server.Core.Tests/TeklaProfileTests.cs`**:
   - `Tekla_constants_produce_the_pipe_prefix_imports_and_globals`: Asserts `PipeNaming.For("tekla", 2025) == "hptekla-mcp-2025"`, `JsonRpcMethods.TeklaPrefix == "tekla."`, and verified globals and imports.
   - `Tekla_guard_profile_denies_picker_dialogs_quit_and_base_list`: Verifies `Picker.PickObject()`, `MessageBox.Show()`, `model.CommitChanges()`, and `System.IO.File` are rejected with diagnostic ID `GUARD`.
   - `Tekla_guard_profile_allows_legitimate_model_queries_and_creation`: Verifies scripts querying `model.GetModelInfo()` or creating `Beam` pass the guard cleanly.
   - `Tekla_analyzer_profile_verifies_commit_usage`: Tests `AnalyzerProfile.Tekla`.
   - `Tekla_context_shape_drops_revit_fields_and_serializes_tekla_info`: Verifies `ContextService` shapes output cleanly with `host: "tekla"`, retaining `tekla` block and removing `revitVersion` / `isFamily`.
   - `Tekla_wire_additions_are_invisible_when_unused`: Asserts that serializing `ContextResult` for Revit or AutoCAD never emits `"tekla"`.

3. **Update `McpShared/HPRebar.Mcp.Server.Core.Tests/HostNeutralityTests.cs`**:
   - Add `"Tekla.Structures"` to `HostApiAssemblies` assertion list to guarantee that McpShared never accidentally takes a reference to Tekla binaries.
   - Add assertion `Assert.Equal("hptekla-mcp-2025", PipeNaming.For("tekla", 2025));`.

4. **Update `McpShared/HPRebar.McpBridge.Core.Net48Tests/ScriptCompilerNet48Tests.cs`**:
   - Add unit test `Guard_and_analyzer_with_the_Tekla_profile_work_on_desktop_framework`: Verifies `GuardProfile.Tekla` and `AnalyzerProfile.Tekla` under the .NET Framework 4.8 runtime.

### Why Existing 9 Hosts Will NOT Break
- **Pure Additive Enums/Constants**: No existing string constant or method prefix is modified.
- **Wire Contract Compatibility**: New DTO `TeklaInfo? Tekla` on `ContextResult` is nullable. `BridgeJson` strips nulls during serialization, so outputs for Revit, AutoCAD, Navis, ETABS, Civil 3D, SAP2000, Power BI, Excel, and Robot remain **bit-for-bit identical**.
- **Independent DI Options**: `McpServerHost.ConfigureOptions` configures `BridgeOptions` scoped to each server exe's `IHostProfile`. Tekla options do not leak into other servers.
- **Isolated Pipe Names**: `hptekla-mcp-2025` is unique and will never collide with `hprebar-mcp-r2026`, `hpnavis-mcp-2026`, or any other host.

---

## 7. Recommendations for HPTekla Solution Implementation

When developing the `HPTekla/` folder:
1. **Solution Structure**:
   ```
   HPTekla/
   ├── HPTekla.slnx
   ├── Directory.Build.props
   ├── HPTekla.McpBridge/          # net48 class library / plugin
   ├── HPTekla.McpBridge.Tests/    # net48 xUnit v3 tests
   ├── HPTekla.Mcp.Server/         # net10.0 console application
   ├── HPTekla.Mcp.Server.Tests/   # net10.0 xUnit v3 tests
   └── tools/harness/              # Python & PowerShell live test harness
   ```
2. **Reference `McpShared` Correctly**:
   - `HPTekla.McpBridge.csproj` references `..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj` and `..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj`.
   - `HPTekla.Mcp.Server.csproj` references `..\McpShared\HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj` and `..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj`.
3. **Tekla Open API References in `Directory.Build.props`**:
   - Locate Tekla Structures 2025 assemblies via `C:\Program Files\Trimble\Tekla Structures\2025.0\bin\` or environment variable `TEKLA_DIR`.
   - Assemblies needed: `Tekla.Structures.dll`, `Tekla.Structures.Model.dll`, `Tekla.Structures.Datatype.dll`, `Tekla.Structures.Catalogs.dll`.
4. **Assembly Resolution in Tekla**:
   - Replicate `PluginAssemblyResolver.cs` from `HPNavis.McpBridge` to prevent Roslyn DLL loading collisions inside the Tekla process.

---

## 8. Conclusion

`McpShared` is fully architected to support Trimble Tekla Structures 2025 as its 10th host. The existing patterns provide a complete blueprint, with HPNavis demonstrating the exact .NET Framework 4.8 in-process plugin mechanism needed for Tekla. Implementing the 6 additive modifications in `McpShared` guarantees 100% backward compatibility and sets the foundation for a robust, high-performance HPTekla MCP subsystem.

# Milestone 1 Completion Report: McpShared Additive Integration for Tekla Structures 2025

- **Worker**: `teamwork_preview_worker_m1`
- **Milestone**: Milestone 1 (McpShared Additive Integration)
- **Date**: 2026-09-22
- **Repo Root**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar`

---

## 1. Executive Summary

Milestone 1 has been implemented with **100% additive modifications** to `McpShared`, providing complete host-level contracts, Roslyn guard/analyzer profiles, DTOs, and test suites for **Trimble Tekla Structures 2025.0** (HPTekla MCP).

All existing 9 host integrations (Revit, AutoCAD, Navisworks, ETABS, Civil 3D, SAP2000, Power BI, Excel, Robot Structural Analysis) remain completely untouched in behavior and wire format. Zero breaking changes or regressions were introduced.

### Automated Test Pass Rates
- **`HPRebar.Mcp.Server.Core.Tests`** (.NET 10.0): **643 passed, 0 failed, 0 skipped** (Baseline: 613, +30 test cases added).
- **`HPRebar.McpBridge.Core.Net48Tests`** (.NET Framework 4.8): **73 passed, 0 failed, 0 skipped** (Baseline: 72, +1 test method / 5 assertions added).
- **Overall Suite**: **716 passed, 0 failed, 0 skipped** (100% pass rate).

---

## 2. Detailed File Modifications

### 2.1 `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`
- Added host identifier constant:
  ```csharp
  /// <summary>
  ///     Trimble Tekla Structures; pipe <c>hptekla-mcp-{version}</c> (e.g. 2025).
  /// </summary>
  public const string TeklaHost = "tekla";
  ```
- Added switch branch in `PipeNaming.For(string host, int version)`:
  ```csharp
  TeklaHost => "hptekla-mcp-" + version,
  ```

### 2.2 `McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs`
- Added wire method prefix constant:
  ```csharp
  public const string TeklaPrefix = "tekla.";
  ```

### 2.3 `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`
- Defined Tekla default script imports, globals, and heavy execution timeout ceiling:
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

### 2.4 `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`
- Added `TeklaInfo? Tekla` property to `ContextResult`:
  ```csharp
  /// <summary>Tekla Structures-only facts; null for the other hosts (and omitted from the JSON).</summary>
  public TeklaInfo? Tekla { get; set; }
  ```
- Defined `TeklaInfo` record:
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

### 2.5 `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`
- Defined `GuardProfile.Tekla`:
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

### 2.6 `McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs`
- Defined `AnalyzerProfile.Tekla`:
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

## 3. Test Suites Implemented and Updated

### 3.1 `McpShared/HPRebar.Mcp.Server.Core.Tests/TeklaTestProfile.cs` (New)
- Provides mock `HostProfile` for Tekla (`HostId = "tekla"`, `DefaultVersion = 2025`, `MethodPrefix = "tekla."`, `MaxTimeoutSeconds = 600`, categories, hints).
- Provides `Candidate` tool generator for tool validation tests.

### 3.2 `McpShared/HPRebar.Mcp.Server.Core.Tests/TeklaProfileTests.cs` (New)
- `Tekla_constants_produce_the_pipe_prefix_imports_and_globals`: verifies pipe naming (`hptekla-mcp-2025`), prefixes, globals, imports, and timeout ceiling.
- `Tekla_guard_profile_denies_picker_dialogs_quit_commit_and_the_base_list`: verifies deny rules across 19 syntax scenarios (`CommitChanges`, `Picker.Pick*`, `MessageBox`, `System.Windows.Forms`, `#r`, `#load`, `Process.Start`).
- `Global_alias_does_not_bypass_the_tekla_bridge_namespace_denial`: verifies `global::` namespace prefix cannot evade the guard.
- `Tekla_guard_profile_lets_reads_writes_and_geometry_through`: verifies valid model creation and property queries pass cleanly.
- `Tekla_analyzer_profile_detects_CommitChanges_as_transaction_method`: verifies `CommitChanges` marks script as using transactions under `AnalyzerProfile.Tekla`.
- `Hints_and_timeout_ceiling_survive_WithHostAssembly_and_default_to_null`: verifies host profile cloning and hint defaults.
- `Validator_ceiling_follows_the_tekla_profile`: verifies dynamic tool validator respects 600s ceiling.
- `ConfigureOptions_seeds_HostVersion_from_the_profile_and_configuration_still_wins`: verifies default version 2025 binding.
- `Wire_additions_are_invisible_when_unused`: verifies `tekla` property is omitted from JSON for non-Tekla contexts.
- `Tekla_info_round_trips_in_camel_case_and_is_omitted_when_null`: verifies serialization/deserialization fidelity of `TeklaInfo`.
- `Context_shape_for_tekla_drops_revit_fields_and_keeps_tekla_block`: verifies `ContextService` output removes `revitVersion`/`isFamily` while preserving `host = "tekla"` and `tekla` block.

### 3.3 `McpShared/HPRebar.Mcp.Server.Core.Tests/ContextServiceTests.cs` (New)
- Dedicated integration test verifying that `ContextService` shapes Tekla responses cleanly: stripping Revit-only legacy fields (`revitVersion`, `isFamily`) while preserving all `TeklaInfo` fields in camelCase.

### 3.4 `McpShared/HPRebar.Mcp.Server.Core.Tests/HostNeutralityTests.cs` (Updated)
- Added `"Tekla.Structures"` to `HostApiAssemblies` assertion list.
- Added assertion verifying `PipeNaming.For("tekla", 2025) == "hptekla-mcp-2025"`.

### 3.5 `McpShared/HPRebar.McpBridge.Core.Net48Tests/ScriptCompilerNet48Tests.cs` (Updated)
- Added `Guard_and_analyzer_with_the_Tekla_profile_work_on_desktop_framework` verifying `GuardProfile.Tekla` and `AnalyzerProfile.Tekla` on CLR v4.0.30319 (.NET Framework 4.8).

---

## 4. Verification Evidence

### Build Verification
```
dotnet build McpShared.slnx
Build succeeded.
    0 Error(s)
```

### Server Core Tests (.NET 10.0)
```
dotnet test HPRebar.Mcp.Server.Core.Tests
Running tests from HPRebar.Mcp.Server.Core.Tests.dll (net10.0|x64)
Test run summary: Passed!
  total: 643
  failed: 0
  succeeded: 643
  skipped: 0
  duration: 3s 225ms
```

### Bridge Core Net48 Tests (.NET Framework 4.8)
```
dotnet test HPRebar.McpBridge.Core.Net48Tests
Running tests from HPRebar.McpBridge.Core.Net48Tests.exe (net48|x64)
Test run summary: Passed!
  total: 73
  failed: 0
  succeeded: 73
  skipped: 0
  duration: 2s 730ms
```

---

## 5. Conclusion
Milestone 1 is complete, fully tested, and ready for Milestone 2 (`HPTekla.McpBridge` plugin implementation on `net48`). Zero regressions across all existing hosts.

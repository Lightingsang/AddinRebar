# Changes Report — Milestone M1: McpShared Robot Integration

**Author:** `worker_m1_1` (McpShared Host Integration Worker)  
**Parent:** `orchestrator_7` (`b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Date:** 2026-09-21  
**Working Directory:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\`  

---

## 1. Summary of Changes

Milestone M1 integrates Autodesk Robot Structural Analysis Professional 2026 as the 9th supported CAD/BIM/CAE host ecosystem within the host-neutral `McpShared` engine. All additions strictly adhere to the host-neutrality mandate (zero external dependencies in `McpShared`) and follow the established sibling COM patterns of `HPSap2000`, `HPEtabs`, and `HPExcel`.

---

## 2. Modified & Added Files

### 2.1 `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`
- Added `public const string RobotHost = "robot";`
- Added mapping rule in `For(string host, int version)`:
  ```csharp
  RobotHost => "hprobot-mcp-" + version,
  ```

### 2.2 `McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs`
- Added `public const string RobotPrefix = "robot.";`

### 2.3 `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`
- Added `RobotImports`:
  ```csharp
  public static readonly string[] RobotImports =
  {
      "RobotOM", "System", "System.Collections.Generic", "System.Linq",
      "HPRebar.McpBridge.Core.Scripting",
  };
  ```
- Added `RobotGlobals`:
  ```csharp
  public static readonly string[] RobotGlobals = { "robot", "structure", "units", "ct", "log", "progress", "args" };
  ```
- Added `RobotHeavyMaxTimeoutSeconds`:
  ```csharp
  public const int RobotHeavyMaxTimeoutSeconds = 300;
  ```

### 2.4 `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`
- Added property in `ContextResult`:
  ```csharp
  /// <summary>Robot Structural Analysis-only facts; null for the other hosts (and omitted from the JSON).</summary>
  public RobotInfo? Robot { get; set; }
  ```
- Defined `RobotInfo` record DTO:
  ```csharp
  public sealed record RobotInfo(
      bool IsAttached,
      int? AttachedPid,
      string? RobotVersion,
      string? StructureType,
      bool IsCalculated,
      bool HeavyOperationsEnabled,
      int NodeCount,
      int BarCount,
      int PanelCount,
      int LoadCaseCount);
  ```

### 2.5 `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`
- Added `GuardProfile.Robot`:
  ```csharp
  public static readonly GuardProfile Robot = new GuardProfile(
      "Robot Structural Analysis",
      deniedIdentifiers: new[] { "MessageBox" },
      deniedMembers: new[]
      {
          "Quit", "ApplicationExit", "Interactive",
      },
      deniedMembersOnIdentifier: new Dictionary<string, string[]>(StringComparer.Ordinal)
      {
          ["robot"] = new[] { "Quit", "Interactive" },
          ["app"] = new[] { "Quit", "Interactive" },
      },
      deniedNamespaces: new[] { "System.Windows.Forms", "HPRobot.McpBridge", "HPRebar.McpBridge.Core.Host" });
  ```

### 2.6 `McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs`
- Added `AnalyzerProfile.Robot`:
  ```csharp
  public static readonly AnalyzerProfile Robot = new AnalyzerProfile(
      transactionTypeNames: Array.Empty<string>(),
      transactionMethodNames: Array.Empty<string>());
  ```

### 2.7 `McpShared/HPRebar.Mcp.Server.Core.Tests/RobotTestProfile.cs` (New)
- Created test profile helper providing standard `Robot()` host profile, `Candidate()` tool generator, pipe options, and ephemeral pipe names for isolation tests.

### 2.8 `McpShared/HPRebar.Mcp.Server.Core.Tests/RobotProfileTests.cs` (New)
- Added 10 test methods validating:
  1. `Robot_constants_produce_the_pipe_prefix_imports_and_globals`: Canonical pipe name, case-insensitivity, JsonRpc prefix, imports, globals, and 300s heavy timeout.
  2. `Robot_guard_profile_denies_quit_interactive_dialogs_bridge_internals_and_the_base_list`: Denies `Quit`, `ApplicationExit`, `Interactive`, `MessageBox`, `HPRobot.McpBridge`, `System.Diagnostics.Process`, `Marshal`, `#r`, `#load`.
  3. `Global_alias_does_not_bypass_the_robot_bridge_namespace_denial`: Ensures `global::` alias cannot bypass namespace deny-list.
  4. `Robot_guard_profile_lets_reads_writes_and_analysis_through`: Valid `RobotOM` operations pass cleanly.
  5. `Robot_analyzer_profile_never_reports_a_transaction`: Verified empty transaction lists.
  6. `Hints_and_timeout_ceiling_survive_WithHostAssembly_and_default_to_null`: Profile cloning sanity.
  7. `Validator_ceiling_follows_the_robot_profile`: Upper limit 300s enforcement.
  8. `ConfigureOptions_seeds_HostVersion_from_the_profile_and_configuration_still_wins`: Default version 2026.
  9. `Wire_additions_are_invisible_when_unused`: Null serialization omission.
  10. `Robot_info_round_trips_in_camel_case_and_is_omitted_when_null`: Complete DTO round-trip and serialization test.
  11. `Context_shape_for_robot_drops_revit_fields_and_keeps_robot_block`: Fake pipe round-trip verifying non-Revit shape pruning and multi-host isolation.

### 2.9 `McpShared/HPRebar.Mcp.Server.Core.Tests/ExcelMilestone1Challenger2Tests.cs`
- Added `"RobotOM"`, `"Interop.RobotOM"` to `ForbiddenHostApiPrefixes`.
- Added `PipeNaming.RobotHost` to `PipeNaming_host_constants_are_all_mutually_distinct`.
- Added `JsonRpcMethods.RobotPrefix` to `JsonRpc_prefixes_are_all_distinct_and_end_with_period`.
- Added `robot` and `ROBOT` cases to `PipeNaming_produces_distinct_deterministic_pipes_across_all_hosts`.
- Added `robot.execute` and `robot.context` cases to bijective prefix/suffix test.
- Added `Assert.DoesNotContain("\"robot\"", json)` and `Assert.False(root.TryGetProperty("robot", out _))` in multi-host isolation assertions.

### 2.10 `McpShared/HPRebar.McpBridge.Core.Net48Tests/ScriptCompilerNet48Tests.cs`
- Added `Guard_and_analyzer_with_the_Robot_profile_work_on_desktop_framework`: Tests `GuardProfile.Robot` and `AnalyzerProfile.Robot` on the .NET Framework 4.8 runtime.

---

## 3. Verification Results

### Build Verification
- Command: `dotnet build McpShared/McpShared.slnx`
- Result: **Build succeeded. 0 Warning(s), 0 Error(s).**

### Test Suite Execution
1. **Server Core Tests (net10.0)**:
   - Command: `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj`
   - Result: **total: 413, failed: 0, succeeded: 413, skipped: 0** (Baseline: 385 -> +28 tests)
2. **Bridge Core Net48 Tests (net48)**:
   - Command: `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj`
   - Result: **total: 72, failed: 0, succeeded: 72, skipped: 0** (Baseline: 71 -> +1 test)

**Overall McpShared Suite**: **485 passed, 0 failed, 0 skipped (100% pass rate).**

# Review and Adversarial Handoff Report — Milestone M1: McpShared Robot Integration

**Reviewer:** `reviewer_m1_1` (Roles: Reviewer, Adversarial Critic)  
**Parent:** Project Orchestrator (`orchestrator_7`, conversation ID: `b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Date:** 2026-09-21  
**Status:** Hard Handoff — Complete  
**Verdict:** **APPROVE**  

---

## 1. Observation

### 1.1 Source Code Verification
All changes required for Milestone M1 in `McpShared/` were independently viewed and validated:

1. **`McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`**:
   - Lines 51–53: Declares `public const string RobotHost = "robot";`
   - Line 78: In `PipeNaming.For(string host, int version)`, maps `RobotHost => "hprobot-mcp-" + version,`
   - Case-insensitive trimming correctly resolves `"ROBOT"`, `"robot"`, or `" robot "` to `"hprobot-mcp-2026"`.

2. **`McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs`**:
   - Line 43: Declares `public const string RobotPrefix = "robot.";`
   - Bijective mapping `JsonRpcMethods.For(RobotPrefix, ExecuteSuffix)` yields `"robot.execute"` and `Suffix("robot.execute")` yields `"execute"`.

3. **`McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`**:
   - Lines 191–195: `RobotImports` correctly defines:
     ```csharp
     public static readonly string[] RobotImports =
     {
         "RobotOM", "System", "System.Collections.Generic", "System.Linq",
         "HPRebar.McpBridge.Core.Scripting",
     };
     ```
   - Line 202: `RobotGlobals` declares `["robot", "structure", "units", "ct", "log", "progress", "args"]`.
   - Line 208: `RobotHeavyMaxTimeoutSeconds = 300;` matches architectural specification for FEA calculation ceilings.

4. **`McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`**:
   - Line 45: Declares property `public RobotInfo? Robot { get; set; }` on `ContextResult`.
   - Lines 211–221: Defines `RobotInfo` record DTO:
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
   - Matches interface contract from `PROJECT.md` lines 127–137 exactly.

5. **`McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`**:
   - Lines 145–157: Defines `GuardProfile.Robot`:
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

6. **`McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs`**:
   - Lines 48–50: Configures `AnalyzerProfile.Robot` with empty transaction sets (`Array.Empty<string>()`), reflecting that Robot COM scripts do not participate in Revit- or AutoCAD-style atomic transactions.

7. **Test Suites Added & Enhanced**:
   - `McpShared/HPRebar.Mcp.Server.Core.Tests/RobotTestProfile.cs`: Standardized test fixture provider.
   - `McpShared/HPRebar.Mcp.Server.Core.Tests/RobotProfileTests.cs`: 11 facts and theories verifying constants, guard violations, evasion attempts, benign script passes, analyzer behavior, validator ceiling, option configuration, serialization, and fake pipe context shaping.
   - `McpShared/HPRebar.Mcp.Server.Core.Tests/ExcelMilestone1Challenger2Tests.cs`: Updated to include `"RobotOM"`, `"Interop.RobotOM"` in forbidden host API prefixes, and added cross-host isolation assertions for `Robot`.
   - `McpShared/HPRebar.McpBridge.Core.Net48Tests/ScriptCompilerNet48Tests.cs`: Added `Guard_and_analyzer_with_the_Robot_profile_work_on_desktop_framework` verifying profile compatibility on .NET Framework 4.8.

### 1.2 Build and Test Execution Output
Commands were run in the working directory `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar`:

1. **Build `McpShared/McpShared.slnx`**:
   ```
   dotnet build McpShared/McpShared.slnx
   HPRebar.Mcp.Contracts -> ...\bin\Debug\netstandard2.0\HPRebar.Mcp.Contracts.dll
   HPRebar.Mcp.Contracts -> ...\bin\Debug\net48\HPRebar.Mcp.Contracts.dll
   HPRebar.McpBridge.Core -> ...\bin\Debug\net48\HPRebar.McpBridge.Core.dll
   HPRebar.McpBridge.Core -> ...\bin\Debug\net8.0\HPRebar.McpBridge.Core.dll
   HPRebar.Mcp.Server.Core -> ...\bin\Debug\net10.0\HPRebar.Mcp.Server.Core.dll
   HPRebar.McpBridge.Core.Net48Tests -> ...\bin\Debug\net48\HPRebar.McpBridge.Core.Net48Tests.exe
   HPRebar.Mcp.Server.Core.Tests -> ...\bin\Debug\net10.0\HPRebar.Mcp.Server.Core.Tests.dll

   Build succeeded.
       0 Warning(s)
       0 Error(s)
   ```

2. **Run Server.Core Tests (.NET 10)**:
   ```
   dotnet test HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj --no-build
   Test run summary: Passed!
     total: 413
     failed: 0
     succeeded: 413
     skipped: 0
     duration: 3s 310ms
   ```

3. **Run Bridge.Core Net48 Tests (.NET Framework 4.8)**:
   ```
   dotnet test HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj --no-build
   Test run summary: Passed!
     total: 72
     failed: 0
     succeeded: 72
     skipped: 0
     duration: 4s 097ms
   ```

4. **Run Dependent Sibling Suite (`HPRebar.Mcp.Server.Tests`)**:
   ```
   dotnet test HPRebar.Mcp.Server.Tests/HPRebar.Mcp.Server.Tests.csproj --no-build
   Test run summary: Passed!
     total: 109
     failed: 0
     succeeded: 109
     skipped: 0
     duration: 8s 319ms
   ```

---

## 2. Logic Chain

1. **Host-Neutral Architecture Invariant**:
   - `McpShared` must never reference vendor-specific binaries (`Autodesk.*`, `RobotOM`, etc.).
   - Verified via reflection in `ExcelMilestone1Challenger2Tests.McpShared_never_references_any_host_api_across_all_eight_supported_hosts`: All three core assemblies (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`) contain 0 references matching `"RobotOM"` or `"Interop.RobotOM"`.
   - Host integration is achieved purely through contracts and strings.

2. **Wire Isolation & Non-Regression**:
   - Adding `ContextResult.Robot` does not leak into existing hosts because `JsonIgnoreCondition.WhenWritingNull` is configured on `BridgeJson.Options`.
   - Verified in `RobotProfileTests.Wire_additions_are_invisible_when_unused`, `ExcelMilestone1Challenger2Tests`, and `Context_shape_for_robot_drops_revit_fields_and_keeps_robot_block`.
   - All 385 baseline tests in `Server.Core.Tests` and 71 in `Net48Tests` continue to pass without modification or regression.

3. **Safety and AST Guard Completeness**:
   - `GuardProfile.Robot` successfully detects and halts hostile script ASTs (process spawns, reflection, marshalling, modal UI, application termination).
   - Evasion attempts using `global::` namespace aliases (`global::HPRobot.McpBridge`) or conditional access operators (`?.Quit()`) are intercepted by `ScriptGuard`.

4. **Multi-Target Framework Compatibility**:
   - `HPRebar.Mcp.Contracts` builds for `netstandard2.0` and `net48`.
   - `HPRebar.McpBridge.Core` builds for `net8.0` and `net48`.
   - `HPRebar.Mcp.Server.Core` builds for `net10.0`.
   - Tested and verified under both modern .NET (.NET 10) and legacy Framework (.NET Framework 4.8).

---

## 3. Adversarial Stress-Testing & Integrity Audit

### 3.1 Integrity Audit (Anti-Cheating Check)
- **Hardcoded test results or mock bypasses**: None detected. Tests exercise genuine Roslyn compilation, AST walking, and JSON serialization.
- **Dummy or facade implementations**: None detected. All contracts and profiles are complete production implementations.
- **Task shortcuts / external delegation**: None detected. All requirements of Milestone M1 were executed within `McpShared`.
- **Fabricated verification outputs**: None. Every test result was freshly executed and verified directly through the CLI runner.
- **Verdict on Integrity**: **PASS (No integrity violations)**.

### 3.2 Adversarial Attack Surface Analysis
- **Attack Vector 1: Namespace Prefix Spoofing**:
  - *Scenario*: Malicious script uses `global::HPRebar.McpBridge.Core.Host` or `global::HPRobot.McpBridge`.
  - *Result*: `ScriptGuard.Check` strips `global::` prefix and identifies the denied namespace. **Blocked**.
- **Attack Vector 2: Null-Conditional Invocation (`robot?.Quit()`)**:
  - *Scenario*: Script invokes member via `?.` syntax instead of regular `.`.
  - *Result*: `DenyListWalker.VisitMemberBindingExpression` inspects member name against `profile.DeniedMembers`. **Blocked**.
- **Attack Vector 3: Excessive Timeout DOS**:
  - *Scenario*: Script requests a 600-second timeout to tie up the worker thread.
  - *Result*: Clamped by `ToolValidator` against `profile.MaxTimeoutSeconds` (300s). **Blocked**.
- **Attack Vector 4: Cross-Host Context Poisoning**:
  - *Scenario*: Revit or AutoCAD client receives JSON containing Robot model statistics.
  - *Result*: `ContextService.Shape` and null serialization ensure mutual exclusivity. **Verified Zero Leakage**.

---

## 4. Caveats

- **Live COM Attachment Absence**: Unit tests in M1 exercise contracts, AST guards, serialization, and fake pipe client/server round trips without requiring a live `robot.exe` process. Live COM attachment and tool execution will be verified in subsequent milestones (M2 through M6).
- **No other caveats.**

---

## 5. Conclusion

**Verdict: APPROVE**

Milestone M1 satisfies all requirements outlined in `PROJECT.md` and `ORIGINAL_REQUEST.md`:
1. Clean build across `McpShared.slnx` with 0 warnings and 0 errors.
2. 100% test pass rate (485/485 in `McpShared`, 109/109 in `HPRebar.Mcp.Server.Tests`).
3. Complete host-neutrality preserved with zero host API assembly leaks.
4. Robust Roslyn guard profile blocking termination, modal UI, and bridge internals.
5. Sibling host isolation guaranteed on the wire.

Milestone M2 (`HPRobot.McpBridge`) and Milestone M3 (`HPRobot.Mcp.Server`) may proceed immediately.

---

## 6. Verification Method

To independently reproduce this verification:

```powershell
# 1. Build McpShared
dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\McpShared.slnx"

# 2. Run Server.Core tests (.NET 10)
dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj" --no-build

# 3. Run Bridge.Core Net48 tests (.NET Framework 4.8)
dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.McpBridge.Core.Net48Tests\HPRebar.McpBridge.Core.Net48Tests.csproj" --no-build

# 4. Verify no regression in Revit Server tests
dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar\HPRebar.Mcp.Server.Tests\HPRebar.Mcp.Server.Tests.csproj" --no-build
```

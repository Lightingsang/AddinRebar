# Handoff Report — Milestone 2 Remediation (worker_m2_fix)

- **Author**: worker_m2_fix (teamwork_preview_worker)
- **Roles**: implementer, qa
- **Date**: 2026-09-21T07:22:00Z
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m2_fix`
- **Target Deliverable**: `HPPowerBi.McpBridge` & `McpShared` (Named Pipe Custom Dispatcher & Theme Dictionary Scope)
- **Verdict**: **REMEDIATION_COMPLETE**

---

## 1. Observation

### 1.1 Reviewer 2 Defect Identifications
Reviewer 2 identified two material defects in `reviewer_m2_2/handoff.md`:
1. **Facade Dispatcher**: `PowerBiDispatcher` was instantiated in `BridgeEntry.Start()`, but was disconnected from `McpBridgeHost`'s pipe listener. Incoming custom methods (`powerbi.dax`, `powerbi.schema`, etc.) hit `RequestDispatcher.DispatchAsync`, which rejected them with error code `-32601 MethodNotFound`.
2. **Theming Resource Scope**: In `MaterialThemeBridge.cs:45`, `FindThemeDictionary(window.Resources)` evaluated to `null` because `MaterialBridge.xaml` is registered in `Application.Current.Resources` (via `App.xaml`), not in `window.Resources`. Thus, `overlay.SetTheme(theme)` was never called.

### 1.2 Implemented Changes
1. **`McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs`**:
   - Added optional constructor parameter:
     ```csharp
     public RequestDispatcher(
         IBridgeExecutor executor,
         BridgeSettings settings,
         string hostVersion,
         string hostName = "Revit",
         string? executionDisabledMessage = null,
         Func<long, JsonRpcEnvelope, NdjsonPipeWriter, CancellationToken, Task<JsonRpcEnvelope?>>? customHandler = null)
     ```
   - In `DispatchAsync`, inside the `default:` branch of the switch:
     ```csharp
     default:
         if (_customHandler is not null)
         {
             var customResponse = await _customHandler(id, request, writer, cancellationToken).ConfigureAwait(false);
             if (customResponse is not null)
                 return customResponse;
         }

         return JsonRpcEnvelope.Failure(id, BridgeErrorCode.MethodNotFound, $"Method not found: {method}");
     ```
2. **`McpShared/HPRebar.McpBridge.Core/Host/McpBridgeHost.cs`**:
   - Added optional constructor parameter `customHandler` (defaulting to `null`) and forwarded it directly to `new RequestDispatcher(..., customHandler)`.
3. **`HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiDispatcher.cs`**:
   - Exposed `public async Task<JsonRpcEnvelope?> DispatchCustomAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)`.
   - Refactored high-level custom methods (`dax`, `evaluate_dax`, `dax.format`, `format_dax`, `schema`, `get_schema`, `measure.upsert`, `measure.delete`, `relationship.manage`, `cloud.workspaces`, `cloud.datasets`, `cloud.refresh`, `cloud.dax`) to return `Task<JsonRpcEnvelope>`.
   - In `HandleLineAsync`, delegates custom methods to `DispatchCustomAsync` and writes the resulting envelope, falling back to `_baseDispatcher.HandleLineAsync`.
4. **`HPPowerBi/HPPowerBi.McpBridge/BridgeEntry.cs`**:
   - Passed `dispatcher.DispatchCustomAsync` to `new McpBridgeHost(...)` on line 75:
     ```csharp
     var host = new McpBridgeHost(executor, settings, store, HostVersion, pipe, HostName,
         JsonRpcMethods.PowerBiPrefix, PbiSafetyGuard.ExecutionDisabledMessage, dispatcher.DispatchCustomAsync);
     ```
5. **`HPPowerBi/HPPowerBi.McpBridge/Resources/Themes/MaterialThemeBridge.cs`**:
   - In `Apply`, updated seed discovery to fall back to `Application.Current.Resources`:
     ```csharp
     var seed = FindThemeDictionary(window.Resources)
                ?? (Application.Current is not null ? FindThemeDictionary(Application.Current.Resources) : null);
     if (seed is not null)
     {
         var seedTheme = seed.GetTheme();
         var theme = Theme.Create(dark ? BaseTheme.Dark : BaseTheme.Light, seedTheme.PrimaryMid.Color, seedTheme.SecondaryMid.Color);
         ...
         overlay.SetTheme(theme);
     }
     ```

### 1.3 Verbatim Build and Test Results
1. **Clean Solution Build**:
   ```cmd
   dotnet build HPPowerBi/HPPowerBi.slnx -c Debug
   ```
   Output:
   ```
   Build succeeded.
       0 Warning(s)
       0 Error(s)
   Time Elapsed 00:00:02.78
   ```
2. **Bridge Unit Tests (including new remediation tests)**:
   ```cmd
   dotnet run --project HPPowerBi/HPPowerBi.McpBridge.Tests/HPPowerBi.McpBridge.Tests.csproj
   ```
   Output:
   ```
   Test run summary: Passed! - HPPowerBi.McpBridge.Tests.dll (net8.0|x64)
     total: 183
     failed: 0
     succeeded: 183
     skipped: 0
     duration: 4s 510ms
   ```
3. **McpShared Host & Core Tests (including new customHandler test)**:
   ```cmd
   dotnet run --project McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj
   ```
   Output:
   ```
   Test run summary: Passed! - HPRebar.Mcp.Server.Core.Tests.dll (net10.0|x64)
     total: 228
     failed: 0
     succeeded: 228
     skipped: 0
     duration: 2s 711ms
   ```
4. **McpShared .NET Framework 4.8 Tests**:
   ```cmd
   dotnet run --project McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj
   ```
   Output:
   ```
   Test run summary: Passed! - HPRebar.McpBridge.Core.Net48Tests.exe (.NET Framework 4.8|x64)
     total: 71
     failed: 0
     succeeded: 71
     skipped: 0
     duration: 1s 832ms
   ```
5. **Server Tests**:
   ```cmd
   dotnet run --project HPPowerBi/HPPowerBi.Mcp.Server.Tests/HPPowerBi.Mcp.Server.Tests.csproj
   ```
   Output:
   ```
   Test run summary: Passed! - HPPowerBi.Mcp.Server.Tests.dll (net10.0|x64)
     total: 1
     failed: 0
     succeeded: 1
     skipped: 0
     duration: 255ms
   ```

---

## 2. Logic Chain

1. **Custom Methods Named Pipe Integration**:
   - `McpBridgeHost` owns `_listener` (`PipeListener`), which executes `RequestDispatcher.HandleLineAsync`.
   - With `customHandler` delegate (`Func<long, JsonRpcEnvelope, NdjsonPipeWriter, CancellationToken, Task<JsonRpcEnvelope?>>`) wired into `RequestDispatcher`, any method unrecognized by the base engine falls into `default:` in `DispatchAsync`.
   - `PowerBiDispatcher.DispatchCustomAsync` intercepts method suffixes (`dax`, `evaluate_dax`, `dax.format`, `format_dax`, `schema`, `get_schema`, `measure.upsert`, `measure.delete`, `relationship.manage`, `cloud.*`) and executes genuine business logic, returning a formatted `JsonRpcEnvelope` response.
   - If an unhandled method arrives, `DispatchCustomAsync` returns `null`, and `RequestDispatcher` returns standard `-32601 MethodNotFound`.
   - Empirical test `CustomMethods_RoutedGenuinelyOverNamedPipe_ToPowerBiDispatcher` and `BridgeEntry_WiresDispatcherIntoHostListener` prove over a live Windows named pipe that `powerbi.format_dax`, `powerbi.dax`, and `powerbi.schema` are received, routed, and processed without `-32601 MethodNotFound`.

2. **Theming Dictionary Scope**:
   - `StatusWindow` does not define `md:CustomColorTheme` locally in `window.Resources`; `MaterialBridge.xaml` is merged in `App.xaml` (`Application.Current.Resources`).
   - By updating `MaterialThemeBridge.Apply` to probe `FindThemeDictionary(window.Resources) ?? (Application.Current is not null ? FindThemeDictionary(Application.Current.Resources) : null)`, the seed theme dictionary is successfully located.
   - `overlay.SetTheme(theme)` executes, dynamically deriving and applying the MaterialDesign theme colors (`PrimaryMid`, `SecondaryMid`, `Background`, `Foreground`, `SurfaceElevated`, etc.) for dark and light modes.
   - Empirical test `MaterialThemeBridge_AppliesTheme_FallingBackToApplicationCurrentResources` proves on an STA thread that windows with empty local resources receive fully populated, dynamic dark and light theme overlays.

---

## 3. Caveats

- No caveats. The changes to `McpShared` are 100% additive and non-breaking (parameters default to `null`), verified across both .NET 8, .NET 10, and .NET Framework 4.8 test suites with 0 regressions.

---

## 4. Conclusion

Both defects identified by Reviewer 2 have been thoroughly remediated:
1. `PowerBiDispatcher` is fully connected to the Named Pipe listener and genuinely routes all Power BI high-level methods.
2. `MaterialThemeBridge.Apply` correctly falls back to `Application.Current.Resources`, restoring dynamic theming for `StatusWindow`.

Milestone 2 is complete, verified, and ready for re-review and progression to Milestone 3.

---

## 5. Verification Method

To independently verify the remediations:
```powershell
# 1. Build HPPowerBi solution (confirm 0 warnings, 0 errors)
dotnet build HPPowerBi/HPPowerBi.slnx -c Debug

# 2. Run Bridge test suite (183 tests pass)
dotnet run --project HPPowerBi/HPPowerBi.McpBridge.Tests/HPPowerBi.McpBridge.Tests.csproj

# 3. Run McpShared Core test suite (228 tests pass)
dotnet run --project McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj

# 4. Run McpShared Net48 test suite (71 tests pass)
dotnet run --project McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj

# 5. Run Server test suite (1 test passes)
dotnet run --project HPPowerBi/HPPowerBi.Mcp.Server.Tests/HPPowerBi.Mcp.Server.Tests.csproj
```

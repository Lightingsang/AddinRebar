# Milestone 2 Remediation Report — HPTekla.McpBridge (Iteration 2)

**Agent**: `teamwork_preview_worker_m2_gen2`  
**Role**: Implementer / QA / Specialist  
**Date**: 2026-09-21T18:20:00Z  
**Target Project**: `HPTekla.McpBridge` (.NET Framework 4.8 In-Process Plugin for Tekla Structures 2025.0)  
**Status**: **COMPLETE (All 3 Items Remediated, 100% Tests Passing, 0 Build Errors)**

---

## 1. Executive Summary

In accordance with the dispatch assignment from orchestrator (`DISPATCH.md`) and the findings from Reviewer 2, Challenger 1, and Challenger 2, all 3 critical remediation items in `HPTekla.McpBridge` have been resolved with genuine, non-dummy implementations:

1. **Timeout CTS Linked Token Source in `TeklaBridgeExecutor.cs`**:
   Introduced a cooperative timeout `CancellationTokenSource` initialized from `Math.Clamp(request.TimeoutSeconds, 5, HostScriptContracts.TeklaHeavyMaxTimeoutSeconds)`, linked with the listener `cancellationToken` into `_currentCancel`.
2. **`BridgeRequestException` Rethrow in `TeklaBridgeExecutor.cs`**:
   Added `catch (BridgeRequestException) { throw; }` in `ExecuteAsync` before generic catch blocks so that JSON-RPC error code `-32002` (`BridgeErrorCode.Busy`) correctly propagates to the MCP caller when the Tekla message loop is blocked or busy.
3. **Reflective vs In-Plugin Version Binding in `PluginAssemblyResolver.cs`**:
   Differentiated requests from within the plugin directory (`requester is not null && IsInFolder(requester, folder)`) vs reflective loads (`requester == null`). For in-plugin dependencies (such as `CommunityToolkit.Mvvm 8.4.0` requesting `Microsoft.Bcl.AsyncInterfaces 8.0.0.0`), the resolver permits binding forward across major versions (`requested <= available`, resolving to Version `10.0.0.12`), while strictly preserving same-major equality for foreign/reflective callers.

---

## 2. Detailed Root Cause Analysis & Changes Made

### Item 1: Timeout CTS in `TeklaBridgeExecutor.cs`
- **Root Cause**: Previously, line 164 created `linkedCts` only wrapping `cancellationToken` without incorporating `request.TimeoutSeconds`. Consequently, scripts executing long computations or infinite loops could not time out cooperatively.
- **Fix Applied** (`TeklaBridgeExecutor.cs` lines 164–168):
  ```csharp
  var timeoutSeconds = Math.Clamp(request.TimeoutSeconds, 5, HostScriptContracts.TeklaHeavyMaxTimeoutSeconds);
  using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
  using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
  _currentCancel = linkedCts;
  ```
- **Verification**: `HostScriptContracts.TeklaHeavyMaxTimeoutSeconds` (600s) acts as the upper ceiling, while 5s is the floor. When cancellation or timeout occurs, `OperationCanceledException` is caught and returned cleanly as `ExecuteResult.Failure("Execution timed out or was cancelled.", rolledBack: true, timedOut: true)`.

### Item 2: Rethrow `BridgeRequestException` in `TeklaBridgeExecutor.cs`
- **Root Cause**: Previously, `TeklaBridgeExecutor.ExecuteAsync` caught all non-cancellation exceptions via `catch (Exception ex)` and converted them into standard 200 OK `ExecuteResult.Failure` payloads. When `TeklaThreadDispatcher` timed out or expired due to an open modal dialog, the resulting `BridgeRequestException.Busy` (-32002) was masked into a standard response rather than surfacing as a JSON-RPC `-32002` protocol error. In contrast, `GetContextAsync` did propagate `BridgeRequestException`.
- **Fix Applied** (`TeklaBridgeExecutor.cs` lines 260–263):
  ```csharp
  catch (BridgeRequestException)
  {
      throw;
  }
  ```
- **Verification**: `TeklaBridgeExecutorContractTests.ExecuteAsync_WhenDispatcherTimesOut_RethrowsBridgeRequestExceptionBusy` confirms that when the dispatcher times out, `ExecuteAsync` throws `BridgeRequestException` with `ex.Code == BridgeErrorCode.Busy` (`-32002`).

### Item 3: Differentiate In-Plugin vs Reflective Assembly Requests in `PluginAssemblyResolver.cs`
- **Root Cause**: Line 79 checked `requested.Major != available.Major || requested > available` unconditionally. Because `CommunityToolkit.Mvvm 8.4.0` (compiled against net462) has an explicit assembly reference to `Microsoft.Bcl.AsyncInterfaces, Version=8.0.0.0`, while `System.Text.Json 10.0.12` bundles `Microsoft.Bcl.AsyncInterfaces, Version=10.0.0.12`, the major version mismatch (8 vs 10) caused `Resolve()` to return `null`, risking runtime `FileLoadException`.
- **Fix Applied** (`PluginAssemblyResolver.cs` lines 79–94):
  ```csharp
  var available = AssemblyName.GetAssemblyName(candidate).Version;
  if (name.Version is { } requested && available is not null)
  {
      var isOwnPlugin = requester is not null && IsInFolder(requester, folder);
      if (isOwnPlugin)
      {
          // When requested by our own plugin, allow binding forward across major versions
          // (e.g. CommunityToolkit.Mvvm requesting Microsoft.Bcl.AsyncInterfaces 8.0 while 10.0 is shipped).
          if (requested > available) return null;
      }
      else
      {
          // Reflective loads (requester == null) could come from another plugin; keep the strict same-major gate.
          if (requested.Major != available.Major || requested > available) return null;
      }
  }
  ```
- **Verification**:
  - In-folder request (`requester = typeof(ObservableObject).Assembly`) for Version `8.0.0.0` resolves to `Microsoft.Bcl.AsyncInterfaces 10.0.0.12`.
  - Reflective request (`requester = null`) for Version `8.0.0.0` returns `null`, preventing foreign plugins in the host process from hijacking our assemblies.

---

## 3. Automated Test Suite Updates & Expansion

File: `HPTekla/HPTekla.McpBridge.Tests/PluginAssemblyResolverStressTests.cs`
- Replaced the failing vulnerability-demonstration test with `MicrosoftBclAsyncInterfaces_Version8RequestedByCommunityToolkitMvvm_BindsForwardSuccessfully`, asserting `resolved != null`, `Name == "Microsoft.Bcl.AsyncInterfaces"`, and `Version == 10.0.0.12`.
- Added `ReflectiveRequest_DifferentMajorVersion_ReturnsNullForSafety`, verifying that reflective requests with different major versions return `null`.

File: `HPTekla/HPTekla.McpBridge.Tests/TeklaBridgeExecutorContractTests.cs`
- Updated `ExecuteAsync_WhenDispatcherTimesOut_RethrowsBridgeRequestExceptionBusy` to assert `BridgeRequestException` with `Code == BridgeErrorCode.Busy` is thrown.
- Added `ExecuteAsync_WhenCancelled_ReturnsFailureWithTimedOutTrue`, asserting cooperative cancellation via `CancellationToken` returns `TimedOut = true` and `RolledBack = true`.
- Added `ExecuteAsync_WhenCancelCalledOnExecutor_CancelsAndReturnsTimedOutTrue`, asserting `executor.Cancel()` cancels an in-flight queued request with `result.TimedOut = true`.

---

## 4. Build & Test Verification Results

### A. Solution / Project Builds
```powershell
dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Release
# Result: 0 Warning(s), 0 Error(s). Time Elapsed: 00:00:02.35

dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Debug
# Result: 0 Warning(s), 0 Error(s). Time Elapsed: 00:00:02.35
```

### B. HPTekla Bridge Test Suite
```powershell
dotnet test HPTekla/HPTekla.McpBridge.Tests/HPTekla.McpBridge.Tests.csproj
# Result: Passed! - Failed: 0, Passed: 24, Skipped: 0, Total: 24, Duration: 1 s
```

### C. McpShared Regression Test Suites
```powershell
dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj
# Result: Passed! Total: 113, Failed: 0, Succeeded: 113, Skipped: 0

dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj
# Result: Passed! Total: 742, Failed: 0, Succeeded: 742, Skipped: 0
```

---

## 5. Deliverable Summary Table

| Deliverable | Path | Status | Verification |
|---|---|---|---|
| Timeout CTS | `HPTekla/HPTekla.McpBridge/TeklaBridgeExecutor.cs` | Completed | Clamped `[5, 600]`, linked to `_currentCancel` |
| Busy Rethrow | `HPTekla/HPTekla.McpBridge/TeklaBridgeExecutor.cs` | Completed | Catches & rethrows `BridgeRequestException` |
| In-Plugin Version Binding | `HPTekla/HPTekla.McpBridge/PluginAssemblyResolver.cs` | Completed | Allows `requested <= available` for own folder; blocks reflective |
| Contract Tests | `HPTekla/HPTekla.McpBridge.Tests/TeklaBridgeExecutorContractTests.cs` | Completed | 4 contract tests passing (Busy, Cancel, Queue) |
| Resolver Stress Tests | `HPTekla/HPTekla.McpBridge.Tests/PluginAssemblyResolverStressTests.cs` | Completed | 9 resolver tests passing (Forward bind, Reflective block) |
| Total Tests | `HPTekla/HPTekla.McpBridge.Tests/` | 24 Tests | 100% Pass rate (24/24) |

# Empirical Challenge Report — Milestone 2: HPTekla Runtime & Threading Contracts

- **Author**: Challenger 2 (`teamwork_preview_challenger_m2_2`) — Empirical Challenger & Adversarial Reviewer
- **Target**: Milestone 2 (`HPTekla.McpBridge` on .NET Framework 4.8 / Tekla Structures 2025.0)
- **Repo Root**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar`
- **Execution Runtime**: Windows Desktop CLR `v4.0.30319` (.NET Framework 4.8.9181.0, x64)
- **Date**: 2026-09-22
- **Verdict**: **REQUEST_CHANGES**

---

## 1. Executive Summary & Verdict

**Overall Risk Assessment**: **HIGH**  
**Verdict**: **REQUEST_CHANGES** (2 actionable findings: 1 assembly resolution version blockage, 1 executor error contract asymmetry)

Milestone 2 delivers a functional `HPTekla.McpBridge` plugin targeting `net48` and Tekla Structures 2025.0. Static build sanity checks confirm that all 7 Tekla Open API assemblies resolve cleanly from `C:\Program Files\Tekla Structures\2025.0\bin\` with `CopyLocal = false`. The threading synchronization model in `TeklaThreadDispatcher.cs` with `expireWithoutTicks = true` was empirically stress-tested and proven to handle modal dialog lockouts correctly.

However, empirical stress testing has revealed **two concrete issues**:
1. **[HIGH] Assembly Resolver Major Version Blockage on Internal Dependency**: `PluginAssemblyResolver.cs` applies `requested.Major != available.Major` unconditionally. Because `System.Text.Json 10.0.12` bundles `Microsoft.Bcl.AsyncInterfaces.dll 10.0.0.12`, while `CommunityToolkit.Mvvm 8.4.0` requests `Microsoft.Bcl.AsyncInterfaces, Version=8.0.0.0`, the resolver refuses to resolve `Microsoft.Bcl.AsyncInterfaces` even though the requester is confirmed to reside inside our own plugin directory (`IsInFolder(requester, folder) == true`).
2. **[MEDIUM] `TeklaBridgeExecutor.ExecuteAsync` Swallows `BridgeRequestException`**: When `TeklaThreadDispatcher` times out or fails with `BridgeRequestException.Busy` (-32002), `ExecuteAsync` catches it as generic `Exception` and returns a standard `ExecuteResult.Failure` rather than rethrowing it as in AutoCAD and Navisworks. This breaks JSON-RPC `-32002` protocol error signaling for `execute_tekla_code` while `get_tekla_context` correctly returns `-32002`.

---

## 2. Empirical Verification Test Suite

To independently stress-test the runtime and threading contracts, an automated test suite was constructed at `HPTekla/HPTekla.McpBridge.Tests` (targeting `net48`, running on Desktop CLR `v4.0.30319` via Microsoft.Testing.Platform runner).

### Test Suite Execution Output
```
xUnit.net v3 Microsoft.Testing.Platform Runner v3.1.0+03a071627b (64-bit .NET Framework 4.8.9181.0)
Test run for G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPTekla\HPTekla.McpBridge.Tests\bin\Debug\net48\HPTekla.McpBridge.Tests.exe (.NETFramework,Version=v4.8)

Passed!  - Failed:     0, Passed:    21, Skipped:     0, Total:    21, Duration: 1 s - HPTekla.McpBridge.Tests.exe (net48)
```

The test suite covers:
- **`PluginAssemblyResolverStressTests.cs`** (11 tests): Folder containment detection, prefix isolation, dynamic assemblies, allow-list enforcement, missing candidate handling, corrupted PE file handling, version comparison bounds, culture invariance, and internal dependency version mismatch reproduction.
- **`TeklaThreadDispatcherStressTests.cs`** (6 tests): Enqueue and main-thread execution, `expireWithoutTicks = true` expiration without message pumping, FIFO expiration order, cooperative cancellation, exception propagation, and `IsQuiescent()` stability.
- **`MvvmRuntimeLoadTests.cs`** (2 tests): `CommunityToolkit.Mvvm` runtime instantiation, command execution, and async command binding.
- **`TeklaBridgeExecutorContractTests.cs`** (2 tests): `GetContextAsync` vs `ExecuteAsync` error propagation behavior under dispatcher timeouts.

---

## 3. Detailed Challenge Findings

### [HIGH] Challenge 1: `PluginAssemblyResolver` Rejects In-Plugin Dependency with Mismatched Major Version

- **Target**: `HPTekla/HPTekla.McpBridge/PluginAssemblyResolver.cs` lines 78–79.
- **Code under test**:
  ```csharp
  var available = AssemblyName.GetAssemblyName(candidate).Version;
  if (name.Version is { } requested && available is not null && (requested.Major != available.Major || requested > available)) return null;
  ```
- **Observed Behavior**:
  1. `HPTekla.McpBridge.csproj` references `CommunityToolkit.Mvvm 8.4.0` and `System.Text.Json 10.0.12`.
  2. In the build output directory `HPTekla/HPTekla.McpBridge/bin/Release/net48/`:
     - `Microsoft.Bcl.AsyncInterfaces.dll` has Version `10.0.0.12`.
     - `CommunityToolkit.Mvvm.dll` references `Microsoft.Bcl.AsyncInterfaces, Version=8.0.0.0`.
  3. When `CommunityToolkit.Mvvm` triggers an assembly resolution for `Microsoft.Bcl.AsyncInterfaces, Version=8.0.0.0`:
     - `name.Name` = `"Microsoft.Bcl.AsyncInterfaces"` (in `AllowList`).
     - `args.RequestingAssembly` = `CommunityToolkit.Mvvm` (`IsInFolder` returns `true`).
     - `candidate` = `...\Microsoft.Bcl.AsyncInterfaces.dll` (exists).
     - `requested.Major` = 8, `available.Major` = 10.
     - `(requested.Major != available.Major)` evaluates to `true` (`8 != 10`).
     - `Resolve()` returns `null`!
- **Empirical Proof**:
  Verified by `PluginAssemblyResolverStressTests.CRITICAL_CHALLENGE_MicrosoftBclAsyncInterfaces_Version8RequestedByCommunityToolkitMvvm_FailsUnderMajorVersionGate`:
  ```csharp
  var args = new ResolveEventArgs(
      "Microsoft.Bcl.AsyncInterfaces, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
      mvvmAsm);
  var resolved = InvokeResolve(args);
  Assert.Null(resolved); // PROVEN: Returns null even though requester is inside plugin folder
  ```
- **Blast Radius**:
  In TeklaStructures.exe (an in-process host without binding redirects in `TeklaStructures.exe.config`), any feature in `CommunityToolkit.Mvvm` or external libraries attempting to use `IAsyncDisposable` or async enumerables fails with `System.IO.FileLoadException: Could not load file or assembly 'Microsoft.Bcl.AsyncInterfaces, Version=8.0.0.0'`.
- **Root Cause & Rationale**:
  The developer added `requested.Major != available.Major` to protect against foreign plugins in the same process when `args.RequestingAssembly == null` (reflective loads). However, applying this check when `args.RequestingAssembly != null && IsInFolder(requester, folder)` erroneously rejects our own plugin's dependencies.
- **Recommended Remediation**:
  Differentiate between requests originating from our own plugin folder vs unknown/reflective requests:
  ```csharp
  var available = AssemblyName.GetAssemblyName(candidate).Version;
  if (name.Version is { } requested && available is not null)
  {
      if (requested > available) return null;
      // Enforce identical major version only when requester is unknown, to avoid poisoning foreign plugins:
      if (requester is null && requested.Major != available.Major) return null;
  }
  ```

---

### [MEDIUM] Challenge 2: Error Contract Asymmetry in `TeklaBridgeExecutor.ExecuteAsync` on Dispatcher Timeout

- **Target**: `HPTekla/HPTekla.McpBridge/TeklaBridgeExecutor.cs` lines 262–265 vs `HPAutoCad` / `HPNavis`.
- **Observed Behavior**:
  When `TeklaThreadDispatcher` expires due to timeout (`expireWithoutTicks = true`, throwing `BridgeRequestException.Busy` with code `-32002`):
  1. In `TeklaBridgeExecutor.GetContextAsync` (lines 280–362):
     The exception is unhandled and propagates to `RequestDispatcher`, which produces a JSON-RPC error response with code `-32002`.
  2. In `TeklaBridgeExecutor.ExecuteAsync` (lines 262–265):
     ```csharp
     catch (Exception ex)
     {
         return Finish(request, ExecuteResult.Failure(ex.Message, rolledBack: true), sw, label, rolledBack: true);
     }
     ```
     `BridgeRequestException` is caught as generic `Exception` and converted into a normal 200 OK `ExecuteResult` (`IsError = true`), masking the `-32002` error code.
  3. In comparison, both `HPAutoCad` (`MainThreadExecutor.cs`:123) and `HPNavis` (`NavisMainThreadExecutor.cs`:160) implement:
     ```csharp
     catch (BridgeRequestException exception)
     {
         Finish(request, ExecuteResult.Failure(exception.Message), stopwatch);
         throw;
     }
     ```
- **Empirical Proof**:
  Verified by `TeklaBridgeExecutorContractTests`:
  - `GetContextAsync_WhenDispatcherTimesOut_ThrowsBridgeRequestExceptionBusy`: Throws `BridgeRequestException` with code `-32002`.
  - `ExecuteAsync_WhenDispatcherTimesOut_CatchesBridgeRequestExceptionAndReturnsFailureInsteadOfRethrowing`: Returns `ExecuteResult` instead of throwing `BridgeRequestException`.
- **Blast Radius**:
  The AI client (Claude Code / Antigravity / Gemini CLI) receives a generic failure string for `execute_tekla_code` rather than an actionable `-32002` (Busy) error code, preventing automated retry backoff handlers from recognizing that Tekla is showing a dialog.
- **Recommended Remediation**:
  In `TeklaBridgeExecutor.ExecuteAsync`, catch and rethrow `BridgeRequestException`:
  ```csharp
  catch (BridgeRequestException exception)
  {
      Finish(request, ExecuteResult.Failure(exception.Message, rolledBack: true), sw, label, rolledBack: true);
      throw;
  }
  catch (OperationCanceledException)
  ...
  ```

---

## 4. Verification of MainThreadQueue & Threading Contracts

### Stress Test Matrix for `TeklaThreadDispatcher.cs`

| Scenario | Injected Condition | Expected Behavior | Actual Empirical Result | Status |
|---|---|---|---|---|
| Main Thread Enqueue & Tick | Work item enqueued, simulated idle tick | Executes delegate, returns value, resets pending count to 0 | Returned `84`, pending count `0` | **PASS** |
| Native Modal Dialog Block (`expireWithoutTicks = true`) | Work item enqueued, 0 ticks pumped (message loop blocked) | Times out after `BusyGrace + 50ms`, throws `BridgeRequestException.Busy` (`-32002`) | Threw `BridgeRequestException` with code `-32002` in 345ms | **PASS** |
| Multiple Pending Items Expiry | 2 items enqueued, 0 ticks pumped | Both expire in FIFO order, throwing `-32002`, queue cleans up | Both threw `-32002`, pending count `0` | **PASS** |
| Waiting Item Cancellation | CancellationToken cancelled before tick | Item transitions to cancelled state without running work delegate | Threw `OperationCanceledException`, work did not run | **PASS** |
| Main Thread Exception | Delegate throws `InvalidOperationException` | Exception propagates to caller awaiting `InvokeAsync` | Threw `InvalidOperationException` | **PASS** |
| Quiescence Query (`IsQuiescent()`) | Evaluated outside Tekla process | Safely handles remoting / missing connection without crashing | Returned `true` | **PASS** |

---

## 5. Build and Tekla Assembly Resolution Sanity Checks

### Compilation
- `dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Release`
  - Result: **Succeeded** (0 warnings, 0 errors, 2.48s).
- Configuration evaluation:
  - `TeklaInstallDir` = `C:\Program Files\Tekla Structures\2025.0\bin\`
  - `TeklaApiAvailable` = `true`

### Assembly Reference Resolution Audit
All 7 assemblies declared in `HPTekla.McpBridge.csproj` resolve cleanly from `C:\Program Files\Tekla Structures\2025.0\bin\`:
1. `Tekla.Structures.dll` (FileVersion: `2025.0.48669.0`, `CopyLocal = false`)
2. `Tekla.Structures.Model.dll` (FileVersion: `2025.0.48669.0`, `CopyLocal = false`)
3. `Tekla.Structures.Catalogs.dll` (FileVersion: `2025.0.48669.0`, `CopyLocal = false`)
4. `Tekla.Structures.Datatype.dll` (FileVersion: `2025.0.48669.0`, `CopyLocal = false`)
5. `Tekla.Structures.Drawing.dll` (FileVersion: `2025.0.48669.0`, `CopyLocal = false`)
6. `Tekla.Structures.Plugins.dll` (FileVersion: `2025.0.48669.0`, `CopyLocal = false`)
7. `Tekla.Structures.Dialog.dll` (FileVersion: `2025.0.48669.0`, `CopyLocal = false`)

Zero assemblies copied into the build output (`Private = false`), preventing DLL version collisions inside `TeklaStructures.exe`.

---

## 6. Action Items for Worker M2

1. **Update `PluginAssemblyResolver.cs`**:
   Adjust the version filter on lines 78–80 so that requests originating from within the plugin directory (`requester is not null && IsInFolder(requester, folder)`) can bind to higher-version assemblies in the folder (e.g. `Microsoft.Bcl.AsyncInterfaces 8.0.0.0` -> `10.0.0.12`).
2. **Update `TeklaBridgeExecutor.cs`**:
   In `ExecuteAsync`, catch `BridgeRequestException`, record `Finish()`, and rethrow (`throw;`) to preserve standard JSON-RPC `-32002` error reporting.

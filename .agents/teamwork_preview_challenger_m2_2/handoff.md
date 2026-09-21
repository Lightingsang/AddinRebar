# Handoff Report — Milestone 2: HPTekla Runtime & Threading Contracts

- **From**: `teamwork_preview_challenger_m2_2` (Empirical Challenger & Adversarial Reviewer)
- **To**: Orchestrator (`parent` / `5d7560ee-5142-428f-a172-e73cf7738ac1`)
- **Target**: Milestone 2 (`HPTekla.McpBridge` .NET Framework 4.8 In-Process Plugin)
- **Repo Root**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar`
- **Verdict**: **REQUEST_CHANGES**

---

## 1. Observation

1. **Build & Reference Resolution**:
   - Running `dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Release` completed successfully with 0 warnings and 0 errors.
   - Evaluated MSBuild property `TeklaInstallDir` = `C:\Program Files\Tekla Structures\2025.0\bin\`.
   - Inspection of `HPTekla.McpBridge.csproj` resolved items confirmed all 7 Tekla Open API assemblies:
     `Tekla.Structures.dll`, `Tekla.Structures.Model.dll`, `Tekla.Structures.Catalogs.dll`, `Tekla.Structures.Datatype.dll`, `Tekla.Structures.Drawing.dll`, `Tekla.Structures.Plugins.dll`, `Tekla.Structures.Dialog.dll`
     resolved directly from `C:\Program Files\Tekla Structures\2025.0\bin\` with `CopyLocal = false`.
2. **`PluginAssemblyResolver.cs` Major Version Check**:
   - In `PluginAssemblyResolver.cs` lines 78–79:
     ```csharp
     var available = AssemblyName.GetAssemblyName(candidate).Version;
     if (name.Version is { } requested && available is not null && (requested.Major != available.Major || requested > available)) return null;
     ```
   - Inspection of `HPTekla/HPTekla.McpBridge/bin/Release/net48/` shows:
     `Microsoft.Bcl.AsyncInterfaces.dll` has Version `10.0.0.12` (bundled via `System.Text.Json` 10.0.12).
     `CommunityToolkit.Mvvm.dll` (net462 asset) has an assembly reference to `Microsoft.Bcl.AsyncInterfaces, Version=8.0.0.0`.
   - In test `PluginAssemblyResolverStressTests.CRITICAL_CHALLENGE_MicrosoftBclAsyncInterfaces_Version8RequestedByCommunityToolkitMvvm_FailsUnderMajorVersionGate`:
     When a request with `args.Name = "Microsoft.Bcl.AsyncInterfaces, Version=8.0.0.0..."` and `args.RequestingAssembly = typeof(ObservableObject).Assembly` is passed to `Resolve()`, `IsInFolder` evaluates to `true`, but `requested.Major != available.Major` (`8 != 10`) triggers and causes `Resolve()` to return `null`.
3. **`TeklaThreadDispatcher.cs` and `MainThreadQueue` Expiry**:
   - `TeklaThreadDispatcher` instantiates `MainThreadQueue` with `expireWithoutTicks = true` and `busyGrace = 8s` (configurable).
   - In test `TeklaThreadDispatcherStressTests.ExpireWithoutTicks_CompletesWithBusyExceptionWhenNoTicksOccur`:
     When a work item is enqueued with `busyGrace = 300ms` and zero ticks are pumped (simulating a native modal dialog blocking the message loop in TeklaStructures.exe), the task threw `BridgeRequestException` with code `BridgeErrorCode.Busy` (`-32002`) after 345ms.
   - In test `TeklaThreadDispatcherStressTests.MultipleQueuedItems_ExpireInOrderWhenNoTicksOccur`:
     Multiple queued items expired sequentially in FIFO order without orphan tasks or leaks.
4. **`TeklaBridgeExecutor.ExecuteAsync` Error Contract Asymmetry**:
   - In `TeklaBridgeExecutor.cs` lines 262–265:
     `catch (Exception ex)` converts `BridgeRequestException` into `ExecuteResult.Failure(ex.Message, rolledBack: true)` rather than rethrowing it.
   - In `TeklaBridgeExecutor.cs` lines 280–362 (`GetContextAsync`):
     `BridgeRequestException` is unhandled and propagates out to `RequestDispatcher` as JSON-RPC error code `-32002`.
   - In `HPAutoCad` (`MainThreadExecutor.cs`:123) and `HPNavis` (`NavisMainThreadExecutor.cs`:160):
     `catch (BridgeRequestException exception)` catches the exception, updates audit records, and rethrows (`throw;`).
5. **Automated Test Suite**:
   - Created test project `HPTekla/HPTekla.McpBridge.Tests` (.NET Framework 4.8).
   - Executing `dotnet test HPTekla/HPTekla.McpBridge.Tests/HPTekla.McpBridge.Tests.csproj` runs 21 tests with 100% pass rate (21 passed, 0 failed).
   - Regression suites in `McpShared`: `HPRebar.McpBridge.Core.Net48Tests` (113 passed) and `HPRebar.Mcp.Server.Core.Tests` (742 passed) remain 100% passing.

---

## 2. Logic Chain

1. From Observation 2, `PluginAssemblyResolver.cs` line 79 was implemented to prevent foreign plugins from binding to our local dependencies during reflective loads (`requester == null`). However, because `requested.Major != available.Major` is checked unconditionally for all requests, it also blocks our own plugin's dependencies (such as `CommunityToolkit.Mvvm 8.4.0` asking for `Microsoft.Bcl.AsyncInterfaces 8.0.0.0`). In an in-process host like `TeklaStructures.exe` where no binding redirects exist for these assemblies in `TeklaStructures.exe.config`, any call path needing `Microsoft.Bcl.AsyncInterfaces` fails with `System.IO.FileLoadException`.
2. From Observation 4, the JSON-RPC wire protocol requires host busy conditions to return error code `-32002` (`BridgeErrorCode.Busy`). In `TeklaBridgeExecutor.ExecuteAsync`, catching `BridgeRequestException` as generic `Exception` converts the busy state into a normal 200 OK tool execution response with `isError: true` rather than a JSON-RPC error code `-32002`. Meanwhile, `GetContextAsync` allows `BridgeRequestException` to propagate, creating an inconsistent API contract between `execute_tekla_code` and `get_tekla_context`.
3. From Observation 1 and 3, the underlying build setup, Tekla Open API assembly references, and `MainThreadQueue` threading mechanics (`expireWithoutTicks = true`) are sound and execute cleanly on Desktop CLR `v4.0.30319`.

---

## 3. Caveats

- Live interaction inside a running `TeklaStructures.exe` GUI process requires manual user testing or unattended UI harness (Milestone 4 scope). The tests conducted here execute in-process on the same CLR runtime (`v4.0.30319`) with the installed Tekla 2025.0 binaries.
- The `Microsoft.Bcl.AsyncInterfaces` version mismatch only triggers at runtime when methods utilizing `IAsyncDisposable` or async enumerables within `CommunityToolkit.Mvvm` or Roslyn are invoked.

---

## 4. Conclusion

The Milestone 2 deliverables are technically well-structured and cleanly compiled against the real Tekla Structures 2025.0 Open API. However, before approval, two changes are required:
1. Refine `PluginAssemblyResolver.cs` so that internal plugin requests (`requester is not null && IsInFolder(requester, folder)`) can bind to higher available versions (`requested <= available`) across major versions.
2. Update `TeklaBridgeExecutor.ExecuteAsync` to catch and rethrow `BridgeRequestException` so that JSON-RPC `-32002` (Busy) protocol errors are preserved.

**Verdict**: **REQUEST_CHANGES**

---

## 5. Verification Method

To independently verify these findings:
1. Build the bridge in Release mode:
   ```powershell
   dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Release
   ```
2. Run the newly established empirical test suite:
   ```powershell
   dotnet test HPTekla/HPTekla.McpBridge.Tests/HPTekla.McpBridge.Tests.csproj
   ```
3. Inspect `PluginAssemblyResolverStressTests.cs` (specifically test `CRITICAL_CHALLENGE_MicrosoftBclAsyncInterfaces_Version8RequestedByCommunityToolkitMvvm_FailsUnderMajorVersionGate`) to observe `Resolve` returning `null` for `Microsoft.Bcl.AsyncInterfaces 8.0.0.0`.
4. Inspect `TeklaBridgeExecutorContractTests.cs` (specifically test `ExecuteAsync_WhenDispatcherTimesOut_CatchesBridgeRequestExceptionAndReturnsFailureInsteadOfRethrowing`) to observe `ExecuteResult.IsError = true` returned instead of throwing `BridgeRequestException`.

# Architectural Blueprint: HPDynamo.McpBridge.Tests

## 1. Observation

### 1.1 Sibling Bridge Test Project Patterns
Through direct file inspection of sibling bridge test suites in the repository, the following architectural patterns were established:

1. **Host-Independent Exe Runner via Microsoft Testing Platform (MTP) & xUnit v3**:
   - `HPNavis.McpBridge.Tests/HPNavis.McpBridge.Tests.csproj` (lines 13-15):
     ```xml
     <OutputType>Exe</OutputType>
     <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
     ```
   - `HPEtabs.McpBridge.Tests/HPEtabs.McpBridge.Tests.csproj` (lines 14-15):
     ```xml
     <OutputType>Exe</OutputType>
     <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
     ```
   - `HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj` (lines 12-13) and `HPExcel.McpBridge.Tests/HPExcel.McpBridge.Tests.csproj` (lines 9-10) both adopt identical configuration with `xunit.v3` (3.1.0) and `Microsoft.Bcl.AsyncInterfaces` (10.0.12).

2. **Strict Host Reference Isolation (`<Private>false</Private>`)**:
   - `HPNavis.McpBridge.Tests/HPNavis.McpBridge.Tests.csproj` (line 32):
     ```xml
     <Reference Include="Autodesk.Navisworks.Api"><HintPath>$(NavisworksInstallDir)Autodesk.Navisworks.Api.dll</HintPath><Private>false</Private></Reference>
     ```
   - `HPEtabs.McpBridge.Tests/HPEtabs.McpBridge.Tests.csproj` (lines 31-34):
     ```xml
     <Reference Include="ETABSv1"><HintPath>$(EtabsInstallDir)ETABSv1.dll</HintPath><Private>false</Private></Reference>
     ```
   - `HPDynamo/Directory.Build.props` (lines 52-57):
     ```xml
     <ItemDefinitionGroup>
       <Reference>
         <Private>false</Private>
       </Reference>
     </ItemDefinitionGroup>
     ```
     Guarantees that no host DLL (`RevitAPI.dll`, `DynamoCore.dll`, etc.) is ever copied to test `bin/`.

3. **Dynamic Assembly Resolution via Module Initializer**:
   - `HPEtabs.McpBridge.Tests/TestInitializer.cs` (lines 7-14):
     ```csharp
     [ModuleInitializer]
     internal static void Initialize()
     {
         EtabsAssemblyResolver.Install();
     }
     ```
   - `HPDynamo.McpBridge/Entry/DynamoAssemblyResolver.cs` already provides `DynamoAssemblyResolver.Install()`, resolving Dynamo and Revit DLLs dynamically from `Revit 2026\AddIns\DynamoForRevit\` via `AppDomain.AssemblyResolve`.

4. **Production Code Seams in HPDynamo.McpBridge**:
   - `DynamoBridgeCoordinator.cs` (lines 160-163):
     ```csharp
     if (_externalEvent == null)
     {
         return DynamoContextExtractor.Extract(null, this, includeSelection, _settings?.ExecutionEnabled ?? false);
     }
     ```
     Demonstrates an explicit null `_externalEvent` headless fallback seam allowing `GetContextAsync` to execute immediately without Revit UI thread event raising.
   - `RevitTransactionScope.cs` (lines 34-37, 59, 191-199):
     ```csharp
     var uidoc = uiapp.ActiveUIDocument;
     var doc = uidoc?.Document;
     if (doc is null || uidoc is null)
         return ExecuteResult.Failure("No document is open in Revit. Open a project first.");
     ```
     Finalization lines 191-199:
     ```csharp
     if (request.DryRun)
     {
         group.RollBack();
         logger("dryRun=true: All Revit document modifications rolled back cleanly.");
         return true;
     }
     group.Assimilate();
     return false;
     ```
   - `RequestDispatcher.cs` (`McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs`, lines 156-159):
     ```csharp
     if (!_settings.ExecutionEnabled)
         return JsonRpcEnvelope.Failure(id, BridgeErrorCode.ExecutionDisabled,
             _executionDisabledMessage ?? $"Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HP MCP Bridge window inside {HostName}.");
     ```
     Standard error code `BridgeErrorCode.ExecutionDisabled = -32001`.
   - `McpBridgeStatusViewModel.cs` (`McpShared/HPRebar.McpBridge.Core/ViewModel/McpBridgeStatusViewModel.cs`, line 48):
     ```csharp
     public McpBridgeStatusViewModel(IMcpBridgeRunner runner, Action<Action> onUiThread, Action<string>? copyToClipboard = null)
     ```
     Accepts an `Action<Action> onUiThread` delegate, allowing headless synchronous invocation (`action => action()`) without a WPF dispatcher or window handle.

---

## 2. Logic Chain

### 2.1 Project Configuration Rationale
- `HPDynamo.McpBridge` targets `net10.0-windows7.0` with `<UseWPF>true</UseWPF>`. The test project must match `net10.0-windows7.0` and enable WPF to:
  1. Reference `HPDynamo.McpBridge` without Target Framework Moniker (TFM) mismatch.
  2. Instantiate and test WPF elements, resource dictionaries, and ViewModel bindings (`DynamoBridgeStatusView`, `DynamoHostTheme`, `MaterialThemeBridge`, `McpBridgeStatusViewModel`).
  3. Adhere to repository conventions using MTP (`<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`) and `xunit.v3` (v3.1.0).

### 2.2 Headless Execution Strategy
Testing bridge logic without active `Revit.exe` or `Dynamo.exe` requires isolating four distinct execution layers:
1. **Coordinator Routing Layer**:
   - `DynamoBridgeCoordinator` is designed around event hooks. By initializing with `_externalEvent = null`, coordinator operations execute synchronously on the calling thread.
   - Live canvas routing can be simulated by providing a mock/fake `DynamoViewModel` or testing state flag transitions (`RegisterLiveCanvas` / `UnregisterLiveCanvas`), proving the coordinator selects the canvas runner when attached and the headless runner when detached.
2. **Transaction Scope Layer**:
   - `RevitTransactionScope` executes transactions in Revit. Without Revit, calling `new TransactionGroup(doc)` triggers native Revit runtime exceptions.
   - *Architecture Seam*: Extract an internal interface `ITransactionContext` or delegate factory (`TransactionContextFactory`) in `HPDynamo.McpBridge.Execution`.
   - In production, `RevitTransactionContext` wraps native `TransactionGroup` and `Transaction`.
   - In headless tests, `SimulatedTransactionContext` tracks `Start()`, `Commit()`, `RollBack()`, and `Assimilate()`, allowing unit tests to assert exact rollback guarantees on `dryRun=true`, assimilation on `dryRun=false`, and rollback on exceptions.
3. **Opt-In Security Layer**:
   - The opt-in gate is enforced at the JSON-RPC dispatch layer by `RequestDispatcher` (checking `_settings.ExecutionEnabled`).
   - Testing this requires zero host interaction: tests instantiate `RequestDispatcher` with `_settings.ExecutionEnabled = false` and send `dynamo.execute` requests either directly via `DispatchAsync` or across an in-memory/isolated Named Pipe (`\\.\pipe\hpdynamo-test-{guid}`). Both verify code `-32001` fast rejection.
4. **UI & ViewModel Layer**:
   - `McpBridgeStatusViewModel` accepts `onUiThread` (`Action<Action>`). Providing `action => action()` executes property notifications synchronously on the test thread, eliminating synchronization deadlocks and allowing deterministic assertions on toggle switches, counters, and status strings.

---

## 3. Comprehensive Test Architecture Specification

### 3.1 Project File: `HPDynamo.McpBridge.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0-windows7.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <UseWPF>true</UseWPF>
        <RootNamespace>HPDynamo.McpBridge.Tests</RootNamespace>
        <Configurations>Debug;Release</Configurations>
        <IsPackable>false</IsPackable>
        <OutputType>Exe</OutputType>
        <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
        <NoWarn>$(NoWarn);MSB3277</NoWarn>
        <MSBuildWarningsAsMessages>$(MSBuildWarningsAsMessages);MSB3277</MSBuildWarningsAsMessages>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="xunit.v3" Version="3.1.0" />
        <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
        <PackageReference Include="Microsoft.Bcl.AsyncInterfaces" Version="10.0.12" />
        <PackageReference Include="NSubstitute" Version="5.3.0" />
    </ItemGroup>

    <ItemGroup>
        <!-- Project References -->
        <ProjectReference Include="..\HPDynamo.McpBridge\HPDynamo.McpBridge.csproj" />
        <ProjectReference Include="..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj" />
        <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj" />
    </ItemGroup>

    <!-- Revit 2026 API Reference (Never copied to bin) -->
    <ItemGroup>
        <Reference Include="RevitAPI">
            <HintPath>$(RevitInstallDir)RevitAPI.dll</HintPath>
            <Private>false</Private>
        </Reference>
        <Reference Include="RevitAPIUI">
            <HintPath>$(RevitInstallDir)RevitAPIUI.dll</HintPath>
            <Private>false</Private>
        </Reference>
    </ItemGroup>

    <!-- Dynamo 3.7 Core Reference (Never copied to bin) -->
    <ItemGroup>
        <Reference Include="DynamoCore">
            <HintPath>$(DynamoInstallDir)DynamoCore.dll</HintPath>
            <Private>false</Private>
        </Reference>
        <Reference Include="ProtoCore">
            <HintPath>$(DynamoInstallDir)ProtoCore.dll</HintPath>
            <Private>false</Private>
        </Reference>
        <Reference Include="DynamoCoreWpf">
            <HintPath>$(DynamoInstallDir)DynamoCoreWpf.dll</HintPath>
            <Private>false</Private>
        </Reference>
        <Reference Include="DynamoServices">
            <HintPath>$(DynamoInstallDir)DynamoServices.dll</HintPath>
            <Private>false</Private>
        </Reference>
        <Reference Include="DynamoApplications">
            <HintPath>$(DynamoInstallDir)DynamoApplications.dll</HintPath>
            <Private>false</Private>
        </Reference>
    </ItemGroup>

</Project>
```

---

### 3.2 Test Suite Design & Concrete Test Cases

#### Suite 1: `CoordinatorTests` (`CoordinatorTests.cs`)
**Target Class**: `HPDynamo.McpBridge.Coordinator.DynamoBridgeCoordinator` & `DynamoContextExtractor`
**Testing Focus**: Routing logic, live canvas registration lifecycle, context extraction, concurrency guard, and script cancellation.

| Test Method | Scenario | Expected Outcome |
|---|---|---|
| `InitialState_LiveCanvasIsNotAttached` | Clean coordinator instance initialized. | `IsLiveCanvasAttached == false`, `ActiveViewModel == null`, `ActiveWorkspace == null`, `IsBusy == false`. |
| `RegisterLiveCanvas_AttachesCanvasAndFiresStateChanged` | `RegisterLiveCanvas(params, vm, ws)` called with valid parameters. | `IsLiveCanvasAttached == true`, `ActiveViewModel == vm`, `StateChanged` event fired exactly once. |
| `UnregisterLiveCanvas_DetachesCanvasAndFiresStateChanged` | `UnregisterLiveCanvas()` called on attached coordinator. | `IsLiveCanvasAttached == false`, `ActiveViewModel == null`, `StateChanged` event fired. |
| `GetContextAsync_HeadlessFallback_PopulatesDynamoInfo` | `GetContextAsync` invoked with `_externalEvent == null`. | Returns valid `ContextResult`, `Host == "dynamo"`, `Dynamo.IsLive == false`, `Dynamo.RunType == "Manual"`, `Selection` empty. |
| `GetContextAsync_WithAttachedCanvas_ReflectsLiveState` | `GetContextAsync` invoked with attached canvas. | `Dynamo.IsLive == true`, `Dynamo.ActiveWorkspaceName` matches workspace title. |
| `ExecuteAsync_ConcurrencyGuard_RejectsSimultaneousRuns` | First run acquires busy flag, second concurrent run attempted. | Second run immediately returns `ExecuteResult.Failure("Another Dynamo script or graph is still running.")`. |
| `Cancel_WhenRunning_CancelsTokenSourceAndReturnsTrue` | Script in-flight, `Cancel()` invoked. | Returns `CancelResult(WasRunning: true, Cancelled: true)`. |
| `Analyze_ValidScript_ReturnsAnalysisResultWithDynamoProfile` | `Analyze(new AnalyzeRequest("return 42;", ...))` invoked. | Returns `AnalyzeResult` compiled under `GuardProfile.Dynamo` with zero guard violations. |

---

#### Suite 2: `TransactionScopeTests` (`TransactionScopeTests.cs`)
**Target Class**: `HPDynamo.McpBridge.Execution.RevitTransactionScope`
**Testing Focus**: Pre-flight document state verification, `TransactionGroup` rollback on `dryRun=true`, assimilation on `dryRun=false`, and exception rollback guarantees.

| Test Method | Scenario | Expected Outcome |
|---|---|---|
| `Execute_WhenNoDocumentOpen_ReturnsFailureImmediately` | `uiapp.ActiveUIDocument == null`. | Returns `IsError == true`, message contains `"No document is open in Revit"`. |
| `Execute_WhenDocumentReadOnlyAndTransactionAuto_ReturnsFailure` | `doc.IsReadOnly == true`, `Transaction == "auto"`. | Returns `IsError == true`, message contains `"document is read-only"`. |
| `Execute_WhenDocumentModifiable_ReturnsOpenTransactionError` | `doc.IsModifiable == true` (transaction already active). | Returns `IsError == true`, message contains `"already has a transaction open"`. |
| `Execute_DryRunTrue_RollsBackTransactionGroup` | Action executes successfully, `request.DryRun = true`. | `group.RollBack()` called, `ExecuteResult.RolledBack == true`, log contains `"dryRun=true: All Revit document modifications rolled back cleanly"`. |
| `Execute_DryRunFalse_AssimilatesTransactionGroup` | Action executes successfully, `request.DryRun = false`. | `autoTx.Commit()` and `group.Assimilate()` called, `ExecuteResult.RolledBack == false`. |
| `Execute_OnActionException_RollsBackCleanlyAndSurfacesError` | User action delegate throws `InvalidOperationException("Geometry failed")`. | Both `autoTx` and `group` rolled back via `SafeRollback`, `ExecuteResult.IsError == true`, message contains `"Geometry failed"`, `RolledBack == true`. |
| `Execute_OnTimeout_CancelsAndRollsBack` | Execution duration exceeds `TimeoutSeconds`. | Throws `OperationCanceledException`, rolls back transactions, `ExecuteResult.TimedOut == true`, `RolledBack == true`. |
| `Execute_ResultSerialization_SerializesComplexReturnValueToJsonElement` | Action returns anonymous object `{ Count = 5, Status = "Ok" }`. | `ExecuteResult.Value` contains valid `JsonElement`, `ValueType == "<>f__AnonymousType..."`. |

---

#### Suite 3: `OptInSecurityTests` (`OptInSecurityTests.cs`)
**Target Class**: `RequestDispatcher`, `BridgeSettings`, `PipeListener`
**Testing Focus**: Code `-32001` fast rejection when `ExecutionEnabled == false`, bypass for safe inspection methods (`ping`, `context`), and real named pipe wire verification.

| Test Method | Scenario | Expected Outcome |
|---|---|---|
| `DispatchAsync_WhenExecutionDisabled_RejectsExecuteWithCode32001` | Request `dynamo.execute` dispatched with `_settings.ExecutionEnabled = false`. | Returns `JsonRpcEnvelope.Failure`, `error.Code == -32001`, message contains `"Code execution is disabled"`. |
| `DispatchAsync_WhenExecutionDisabled_PermitsPingAndContext` | Requests `dynamo.ping` and `dynamo.context` dispatched with `ExecutionEnabled = false`. | Both succeed with `error == null`; `context.ExecutionEnabled == false`. |
| `DispatchAsync_WhenExecutionEnabledToggled_AllowsExecution` | `ExecutionEnabled` set to `true`, `dynamo.execute` dispatched. | Dispatches to executor without `-32001` rejection. |
| `Wire_NamedPipe_RefusesExecutionOverWireWhenOptInIsOff` | Named pipe client connects to test listener and sends `dynamo.execute` line. | Client reads JSON-RPC response with `error.code == -32001`. |
| `Wire_NamedPipe_CustomErrorMessage_MatchesDynamoPrompt` | Opt-in rejected over wire. | Error message explicitly instructs: `"Ask the user to tick 'Allow AI Dynamo execution' in the HPDynamo MCP Bridge window inside Revit"`. |
| `OptInToggle_ThreadSafety_ConcurrentReadsAndWrites` | Parallel threads toggle `ExecutionEnabled` while dispatching requests. | Zero race conditions or exceptions; rejected calls always return `-32001`. |

---

#### Suite 4: `DesignScriptRunnerTests` (`DesignScriptRunnerTests.cs`)
**Target Class**: `HeadlessGraphRunner`, `CanvasGraphRunner`, `DynamoExecutionEngine`
**Testing Focus**: DesignScript argument validation, parameter dictionary formatting, AST statements, and ephemeral node lifecycle.

| Test Method | Scenario | Expected Outcome |
|---|---|---|
| `ExecuteDesignScript_EmptyOrWhitespaceCode_ThrowsArgumentException` | `code == ""` or `code == "   "`. | Throws `ArgumentException("DesignScript code cannot be empty")`. |
| `FormatParameterOverrides_ValidJsonArgs_ProducesSerializedOverrides` | JSON `{ "width": 250.0, "height": 500.0 }` passed as `args`. | Formats into serialized JSON list of `[{ "Name": "width", "Value": "250" }, ...]`. |
| `StatementValidation_ValidDesignScriptSyntax_AcceptsAssignments` | Code containing valid statement `x = 10; y = x * 2;`. | Evaluated without AST syntax errors. |
| `EphemeralCodeBlock_DryRunCleanup_EnsuresNodeDeletion` | Ephemeral `CodeBlockNodeModel` placed for DesignScript run under `dryRun = true`. | Node deleted from workspace in `finally` block, canvas left in pristine state. |
| `ExtractNodeOutputs_PortEvaluationException_RecordsNullWithoutThrowing` | Port evaluation throws exception during value retrieval. | Port recorded as `null` in outputs dictionary; execution does not crash. |
| `SeedHelper_CreateCodeBlockNode_GeneratesCompliantScript` | Tool `create_code_block_node` constructs script definition. | Valid DesignScript text block generated and formatted correctly. |

---

#### Suite 5: `ViewModelBindingTests` (`ViewModelBindingTests.cs`)
**Target Class**: `DynamoStatusViewModel` / `McpBridgeStatusViewModel` & `DynamoHostTheme`
**Testing Focus**: Master opt-in toggle, listener toggle commands, UI theme sync, script compilation counters, and last run summary.

| Test Method | Scenario | Expected Outcome |
|---|---|---|
| `Constructor_SyncsInitialStateWithRunner` | ViewModel instantiated with mock `IMcpBridgeRunner`. | `IsListening`, `PipeName`, `RevitVersion`, and `IsExecutionEnabled` match runner state. |
| `ToggleExecutionEnabled_PropagatesToRunner` | User flips `IsExecutionEnabled` toggle switch to `true`. | `runner.ExecutionEnabled` updated to `true`. |
| `ToggleListenerCommand_WhenStopped_StartsRunner` | `IsListening == false`, user triggers `ToggleListenerCommand`. | `runner.Start()` called, `ToggleButtonText` updates to `"Stop listener"`. |
| `ToggleListenerCommand_WhenListening_StopsRunner` | `IsListening == true`, user triggers `ToggleListenerCommand`. | `runner.Stop()` called, `ToggleButtonText` updates to `"Start listener"`. |
| `RestartBridgeCommand_WhenBusy_CanExecuteIsFalse` | `runner.Status == BridgeStatus.Busy`. | `RestartBridgeCommand.CanExecute` evaluates to `false`. |
| `CompiledScriptCount_ExceedsThreshold_FlipsIsCompileCountHigh` | `CompiledScriptCount` set to 501 (> 500 threshold). | `IsCompileCountHigh == true`. |
| `ThemeSync_DynamoHostTheme_TriggersChangedEvent` | Host switches between Dark and Light mode. | `DynamoHostTheme.Instance.NotifyChanged()` fires `Changed` event; `MaterialThemeBridge` attaches palette overlay. |
| `LastRunSummary_FormatsSuccessAndFailureOutcomes` | `LastRunInfo` populated with `+2 ~1 -0` changes. | `LastRunSummary` displays timestamp, label, duration, and formatted delta counts. |
| `CopyLastScriptCommand_CopiesSourceToClipboard` | `HasLastScript == true`, user executes `CopyLastScriptCommand`. | Clipboard callback invoked with verbatim script source. |

---

## 4. Test Infrastructure Helpers & Fakes

To enable high-fidelity testing without external dependencies, the following test fixtures and fakes must be placed in `HPDynamo.McpBridge.Tests/Fakes/`:

1. **`FakeMcpBridgeRunner.cs`** (< 80 lines):
   Implements `IMcpBridgeRunner` in-memory. Stores `Status`, `ExecutionEnabled`, `PipeName`, and raises `StateChanged`.
2. **`SimulatedTransactionContext.cs`** (< 70 lines):
   Simulates Revit `TransactionGroup` and `Transaction` lifecycle. Records whether `RollBack()` or `Assimilate()` was invoked.
3. **`TestInitializer.cs`** (< 25 lines):
   Decorated with `[ModuleInitializer]` to call `DynamoAssemblyResolver.Install()`.

---

## 5. Clean Code Governance Compliance

Every file in the proposed test suite conforms to repository Clean Code governance:
1. **Rule C2 (File Length <= 300 lines)**:
   - `CoordinatorTests.cs`: ~140 lines
   - `TransactionScopeTests.cs`: ~160 lines
   - `OptInSecurityTests.cs`: ~130 lines
   - `DesignScriptRunnerTests.cs`: ~110 lines
   - `ViewModelBindingTests.cs`: ~150 lines
   - `FakeMcpBridgeRunner.cs`: ~75 lines
   - `TestInitializer.cs`: ~20 lines
   All files well within the 300-line limit.
2. **Rule M3 (Method Length <= 50 lines)**:
   Every test method is concise (10–30 lines), following the Arrange-Act-Assert pattern.
3. **Rules Q-B1 & Q-B2 (Runtime Quality)**:
   - 0 commented-out code lines.
   - 0 empty catch blocks without explanatory comments.
   - No mock libraries used for internal logic where pure fakes/stubs provide higher determinism.
4. **Dependency Compliance**:
   References only `HPDynamo.McpBridge` and `McpShared/`. No cross-references to other host MCP folders (`HPAutoCad`, `HPNavis`, `HPEtabs`).

---

## 6. Caveats

1. **Revit In-Process Native Elements**: Testing native Revit element creation (`doc.Create.NewFamilyInstance`, `Rebar.CreateFromCurves`) in-process requires Revit's native C++ host engine, which is covered by the E2E acceptance suite (`HPDynamo.E2E.Tests`) and live runtime testing. Unit testing in `HPDynamo.McpBridge.Tests` focuses on bridge coordination, transaction boundaries, security gating, and protocol contracts.
2. **Dynamo Engine Initialization**: `RevitDynamoModel` relies on internal services (`LibraryServices`, `EngineController`). Where pure unit testing of AST logic is performed, ephemeral node model mocks or AST parser tests are used.

---

## 7. Conclusion

The test architecture for `HPDynamo.McpBridge.Tests` is fully specified and ready for implementation. It bridges the gap between pure unit testing and host integration by utilizing proven patterns from `HPNavis.McpBridge.Tests` and `HPEtabs.McpBridge.Tests`:
- `net10.0-windows7.0` with WPF support and MTP xUnit v3 runner.
- Zero host bin pollution via `<Private>false</Private>`.
- Decoupled headless testing of the dual-lifecycle coordinator, transaction scope rollback, security gate (-32001), DesignScript argument parser, and status ViewModel.

---

## 8. Verification Method

To verify the test architecture once implemented:
1. **Build Verification**:
   ```powershell
   dotnet build HPDynamo/HPDynamo.slnx -c Debug -m:1 /nr:false
   ```
   Must compile with 0 errors and 0 warnings.
2. **Headless Execution Verification**:
   ```powershell
   dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj -m:1 /nr:false
   ```
   All test cases across the 5 suites must pass 100% green in < 5 seconds without Revit or Dynamo running.
3. **Zero Host Bin Pollution Check**:
   ```powershell
   Get-ChildItem -Path HPDynamo/HPDynamo.McpBridge.Tests/bin/Debug/ -Recurse -Filter "RevitAPI*.dll", "DynamoCore*.dll"
   ```
   Must return 0 files.

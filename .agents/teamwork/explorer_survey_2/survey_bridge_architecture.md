# Architectural Survey & Blueprint: In-Process Revit Bridge Architecture (HPGrasshopper MCP)

**Author:** explorer_survey_2 (Bridge Architecture Explorer)  
**Date:** 2026-10-10  
**Target:** `HPGrasshopper` Subsystem (`HPGrasshopper.McpBridge` & `HPGrasshopper.slnx`)  
**Reference Implementations:** `HPDynamo` (Revit 2026 & Dynamo 3.7 In-Process Bridge) & `HPRebar.McpBridge` (Revit 2026 Add-In Bridge)  
**Host Environment:** Autodesk Revit 2026 (.NET 8) + Rhino 8 (`netcore`) + Rhino.Inside.Revit R8 (.NET 8)

---

## Executive Summary

This investigation surveys the in-process Revit bridge architecture used in the repository to establish the definitive architectural blueprint for **HPGrasshopper MCP**.

Unlike standalone out-of-process COM bridges (such as ETABS, SAP2000, Robot, Excel), **HPGrasshopper** operates **in-process inside `Revit.exe` (Revit 2026)** alongside **Rhino.Inside.Revit (Rhino 8)**. This dual-host in-process environment requires:
1. **In-Process Dual-Lifecycle Architecture**: Seamless coordination between the Revit UI/Main thread and the Rhino/Grasshopper canvas lifecycle.
2. **Deterministic Thread Dispatching**: Thread-safe marshalling of incoming Named Pipe requests from background worker threads to the Revit & Grasshopper UI thread via `ExternalEvent` queue synchronization.
3. **Dual-Level Safety Mechanism**:
   - **Level 1 (Revit Document)**: Atomic `TransactionGroup` rollback for `dryRun=true` or error/cancellation.
   - **Level 2 (Grasshopper Canvas)**: `GH_UndoRecord` / canvas reverse-mutation ledger for graph editing rollback.
4. **Isolated Modeless WPF UI**: A standalone status dialog styled with `MaterialDesignThemes 5.3.2`, dynamic Dark/Light theme switching, and ILRepack bundling to prevent BAML naming collisions in Revit.

---

## 1. Architectural Reference: HPDynamo In-Process Bridge

### 1.1 Solution Structure & Project Matrix

`HPDynamo` serves as the primary modern reference in the repository for in-process Revit subsystems. Its solution (`HPDynamo/HPDynamo.slnx`) follows the XML `.slnx` specification and organizes projects cleanly without cross-project pollution:

```
HPDynamo/
├── Directory.Build.props              # Host paths, versions, assembly reference resolution
├── global.json                        # .NET SDK 10.0.300 & Microsoft.Testing.Platform pin
├── README.md
├── HPDynamo.slnx                      # Root solution linking bridge, server, tests & McpShared
├── HPDynamo.McpBridge/                # In-process add-in library (net10.0-windows7.0 / net8.0-windows7.0)
├── HPDynamo.McpBridge.Tests/          # In-memory coordinator, transaction & compiler tests
├── HPDynamo.Mcp.Server/               # Stdio MCP Server executable (net10.0)
├── HPDynamo.Mcp.Server.Tests/         # Registry, stdio protocol & ADR-0007 script quality tests
├── HPDynamo.E2E.Tests/                # 5-Tier integration & stress tests
└── (Shared Links to ../McpShared/)
```

#### Build Configuration & SDK Alignment
- **Configurations**: Standard `Debug` and `Release` (no version suffix like `Debug.R26` needed because target host versions are resolved in `Directory.Build.props`).
- **Target Frameworks**:
  - `McpBridge`: `net8.0-windows7.0` (or `net10.0-windows7.0` with .NET 8 runtime compatibility).
  - `McpBridge.Tests`: `net8.0-windows7.0` (runs tests exercising WPF and Windows APIs).
  - `Mcp.Server`: `net10.0` (stdio process launched by AI coding agent).
  - `Mcp.Server.Tests` & `E2E.Tests`: `net10.0`.
- **Zero Host Bin Pollution**: Host SDK references (`RevitAPI.dll`, `RevitAPIUI.dll`, `RhinoCommon.dll`, `Grasshopper.dll`, `RhinoInside.Revit.dll`) have `<Private>false</Private>` enforced globally via `<ItemDefinitionGroup>` in `Directory.Build.props`.

### 1.2 Host Resolution in `Directory.Build.props`

In `HPDynamo/Directory.Build.props`, reference paths are dynamically located via environment variables, registry lookups, and standard Program Files paths:

```xml
<!-- Revit 2026 Base Directory -->
<RevitInstallDir Condition="'$(RevitInstallDir)' == '' And '$(HPREBAR_REVIT_DIR)' != ''">$(HPREBAR_REVIT_DIR)</RevitInstallDir>
<_RevitRegLocation Condition="'$(RevitInstallDir)' == ''">$([MSBuild]::GetRegistryValueFromView('HKEY_LOCAL_MACHINE\SOFTWARE\Autodesk\Revit\$(RevitYear)\REVIT-05:0409', 'InstallationLocation', null, RegistryView.Registry64))</_RevitRegLocation>
<RevitInstallDir Condition="'$(RevitInstallDir)' == '' And '$(_RevitRegLocation)' != ''">$(_RevitRegLocation)</RevitInstallDir>
<RevitInstallDir Condition="'$(RevitInstallDir)' == ''">$(ProgramW6432)\Autodesk\Revit $(RevitYear)\</RevitInstallDir>

<!-- Shared McpShared Relative Resolution -->
<McpSharedDir Condition="'$(McpSharedDir)' == ''">$([System.IO.Path]::GetFullPath('$(MSBuildThisFileDirectory)..\McpShared\'))</McpSharedDir>
<McpContractsProjectPath>$(McpSharedDir)HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj</McpContractsProjectPath>
<McpBridgeCoreProjectPath>$(McpSharedDir)HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj</McpBridgeCoreProjectPath>
<McpServerCoreProjectPath>$(McpSharedDir)HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj</McpServerCoreProjectPath>
```

### 1.3 Revit In-Process Connection & Lifecycle

#### Revit Addin Manifest (`.addin`)
Deploying an in-process add-in into Revit requires a manifest file located in `%APPDATA%\Autodesk\Revit\Addins\2026\`:
```xml
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>HPGrasshopper MCP Bridge</Name>
    <Assembly>HPGrasshopper.McpBridge\HPGrasshopper.McpBridge.dll</Assembly>
    <AddInId>F6D264A7-876D-4158-9A3E-81DE1234ABCD</AddInId>
    <FullClassName>HPGrasshopper.McpBridge.Entry.GrasshopperRevitApp</FullClassName>
    <VendorId>HPRB</VendorId>
    <VendorDescription>HPRebar Grasshopper MCP Subsystem</VendorDescription>
  </AddIn>
  <ManifestSettings>
    <UseRevitContext>False</UseRevitContext>
    <ContextName>HPGrasshopper.McpBridge</ContextName>
  </ManifestSettings>
</RevitAddIns>
```

#### Application Entry Point (`IExternalApplication`)
The `IExternalApplication` lifecycle in `DynamoRevitApp.cs` demonstrates the exact startup sequence:
1. **Assembly Resolver**: `GrasshopperAssemblyResolver.Install()` intercepts ALC / AppDomain assembly loads before any dependencies are touched.
2. **Serilog File Logging**: Dedicated rolling log directory at `%LOCALAPPDATA%\HPGrasshopper\McpBridge\logs\mcpbridge-.log`.
3. **Settings Store**: `BridgeSettingsStore("HPGrasshopper", "McpBridge")` manages persistence for opt-in execution and listener preferences.
4. **Coordinator & ExternalEvent**:
   - `GrasshopperBridgeCoordinator.Instance` manages work requests and lifecycle hooks.
   - `ExternalEvent.Create(new GrasshopperExternalEventHandler(_coordinator))` binds the Revit UI thread dispatcher.
   - `_coordinator.Initialize(settings, new RevitExternalEventRaiser(externalEvent))`.
5. **Bridge Host Initialization**:
   `McpBridgeHost` is instantiated with:
   - Named Pipe: `hpgrasshopper-mcp-2026`.
   - Host Name: `"Grasshopper"`.
   - Prefix: `JsonRpcMethods.GrasshopperPrefix` (`"grasshopper."`).
   - Opt-in error message: `"Code execution is disabled. Ask the user to tick 'Allow AI Grasshopper execution' in the HPGrasshopper MCP Bridge window inside Revit."`.
   - Custom RPC handler: Handles `grasshopper.show_window`, `grasshopper.activate_canvas`, etc.
6. **Ribbon Panel Registration**:
   - Registers custom Ribbon Tab `"HPGrasshopper"` (or attaches to Rhino.Inside / Add-Ins tab).
   - Adds `PushButtonData` pointing to `ShowStatusDialogCommand`.
   - Attaches theme change listener to swap button icons dynamically on Revit Dark/Light switch.

---

## 2. Threading, Dispatching & Synchronization Mechanics

### 2.1 The In-Process Threading Problem
Revit API and Grasshopper UI Canvas are single-threaded and strictly UI-thread bound:
- Calling Revit DB methods (`doc.Create.*`, `new Transaction(doc)`) from a background thread throws `Autodesk.Revit.Exceptions.InvalidOperationException`.
- Touching Grasshopper Canvas or Document objects (`doc.AddObject`, `canvas.Refresh()`) from a background thread throws cross-thread COM/WPF exceptions or corrupts UI state.
- Conversely, MCP Named Pipe messages arrive asynchronously on thread-pool worker threads in `PipeListener.ServeClientAsync`.

### 2.2 The Proven Solution: `ConcurrentQueue` + `ExternalEvent`

`DynamoBridgeCoordinator` implements `ICorrelatedCancelExecutor` with an asynchronous producer-consumer pattern:

```
[MCP Client (AI Agent)]
        │ (Named Pipe JSON-RPC)
        ▼
[PipeListener (Worker Thread)]
        │ (Fire-and-forget dispatch)
        ▼
[RequestDispatcher] ───> [GrasshopperBridgeCoordinator.ExecuteAsync]
                                │
                                ├── Enqueues McpBridgeWorkRequest to ConcurrentQueue
                                ├── Registers with WorkItemTracker
                                ├── Calls _externalEvent.Raise()
                                └── Awaits work.Completion.Task (Asynchronous Non-Blocking)
                                        │
┌───────────────────────────────────────┘
│ (Revit Idle Loop activates ExternalEvent)
▼
[GrasshopperExternalEventHandler.Execute (Revit Main UI Thread)]
        │
        ▼
[Coordinator.Execute(uiapp)]
        │
        ├── Dequeues McpBridgeWorkRequest
        ├── Checks CancellationToken
        ├── Enters RevitTransactionScope (Level 1)
        ├── Interacts with Grasshopper Canvas / GH_Document (Level 2)
        └── Sets work.Completion.TrySetResult(result)
```

### 2.3 Correlated Cancellation & Non-Blocking Cancellation
A key architectural principle in `HPRebar.McpBridge.Core`:
- `PipeListener` dispatches lines fire-and-forget: `_ = _dispatcher.HandleLineAsync(...)`.
- When an execution is currently waiting in the Revit queue or running on the Revit UI thread, a `grasshopper.cancel` command arrives over the pipe on a separate worker thread.
- `RequestDispatcher` routes `.cancel` directly to `_executor.Cancel(requestId, sessionId)`.
- The coordinator locks `_cancelLock`, verifies the correlated session and request ID, and calls `_currentCancel?.Cancel()`.
- The linked `CancellationToken` triggers an `OperationCanceledException` inside `RevitTransactionScope`, triggering atomic rollback immediately without deadlock.

---

## 3. Dual-Level Safety Architecture (Revit & Grasshopper)

The requirement mandates:
> "Triển khai cơ chế an toàn 2 tầng (Dual-layer Safety): `Revit TransactionGroup` rollback cho `dryRun = true` khi can thiệp Revit elements + Grasshopper `GH_UndoRecord` cho các thao tác can thiệp canvas."

### 3.1 Level 1: Revit TransactionGroup Rollback (`RevitTransactionScope`)

Derived from `HPDynamo.McpBridge.Execution.RevitTransactionScope`:
1. **Transaction Group Boundary**:
   Before executing the user action or Roslyn script, `RevitTransactionScope` begins a `TransactionGroup`:
   ```csharp
   txContext.StartGroup("MCP Grasshopper: " + label);
   ```
2. **Auto-Transaction Management**:
   If `mode == TransactionModes.Auto`, an inner `Transaction` is started:
   ```csharp
   txContext.StartAutoTransaction("MCP Grasshopper auto-step");
   ```
3. **Atomic Rollback on `dryRun = true`**:
   ```csharp
   if (request.DryRun)
   {
       txContext.RollbackGroup();
       logger("dryRun=true: All Revit document modifications rolled back cleanly.");
       return true;
   }
   ```
   Calling `RollbackGroup()` discards every transaction committed within the group, guaranteeing zero modifications are persisted in Revit.
4. **Commit & Assimilation on Production Run**:
   When `dryRun == false` and execution succeeds:
   - `txContext.CommitAutoTransaction()`
   - `txContext.AssimilateGroup()`: flattens the group into a single clean entry in Revit's undo stack.
5. **Change Tracking**:
   `DocumentChangeCounter` captures elements added, modified, and deleted, returning exact metrics in `ExecuteResult.Changed`.

### 3.2 Level 2: Grasshopper Canvas Undo & Mutation Rollback

Grasshopper's canvas has its own state independent of Revit transactions. Modifying components, parameters, and wires modifies the `GH_Document`.

To provide Level 2 safety:
1. **Using `GH_UndoRecord`**:
   When batch editing canvas objects or placing components:
   ```csharp
   var undoRecord = new GH_UndoRecord("MCP Batch Edit");
   doc.UndoServer.PushUndoRecord(undoRecord);
   // Record actions:
   undoRecord.AddObject(new GH_DeleteObjectAction(component));
   // or record parameter mutations
   ```
2. **Reverse Mutation Ledger (Compensating Actions)**:
   In `batch_edit_grasshopper_canvas`, the bridge maintains:
   - `createdComponentGuids`: List of components added during the batch.
   - `replacedWires`: Source/target port tuples that were disconnected.
   - `modifiedParameters`: Original parameter values/expressions before edits.
3. **Rollback Execution**:
   When `dryRun == true` or when an exception/cancellation occurs:
   ```csharp
   void PerformCanvasRollback()
   {
       // 1. Remove newly created components
       foreach (var guid in createdComponentGuids)
       {
           var obj = doc.FindObject(guid, true);
           if (obj != null) doc.RemoveObject(obj, false);
       }

       // 2. Re-establish replaced wires
       foreach (var wire in replacedWires)
       {
           wire.TargetParam.AddSource(wire.SourceParam);
       }

       // 3. Restore modified parameter expressions/values
       foreach (var (param, origVal) in modifiedParameters)
       {
           param.SetPersistentData(origVal);
       }

       // 4. Trigger UI repaint without dirtying document
       doc.NewSolution(false);
   }
   ```
4. **Guaranteed Idempotence**:
   Executing with `dryRun=true` verifies component validity, connections, and execution flow, then restores both Revit DB and Grasshopper Canvas to the exact pre-call state.

---

## 4. Modeless WPF UI & Material Design Styling

Following `HPDynamo.McpBridge/View/` and `MaterialThemeBridge.cs`:

### 4.1 UI Architecture
- **Modeless Window**: `GrasshopperBridgeStatusView` is hosted as a modeless Revit child window via `WindowInteropHelper.Owner = RevitMainWindowHandle`.
- **View Model**: Binds to `McpBridgeStatusViewModel` from `HPRebar.McpBridge.Core.ViewModel`.
  - Pipe connection dot indicator (Stopped / Listening / Connected / Busy / Error).
  - Checkbox: "Allow AI Grasshopper execution" (`ExecutionEnabled`).
  - Checkbox: "Auto-start Named Pipe listener".
  - Real-time audit log stream.
  - Active tool registry catalog.
- **Software Rendering Guard**:
  ```csharp
  if (PresentationSource.FromVisual(this) is HwndSource source && source.CompositionTarget != null)
  {
      source.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
  }
  ```
  Forces software rendering to eliminate hardware acceleration crashes inside `Revit.exe`.

### 4.2 MaterialDesignThemes 5.3.2 & ILRepack Isolation
- **BAML Collision Risk**: In WPF, simple assembly names are bound globally. If multiple Revit add-ins load `MaterialDesignThemes.Wpf.dll`, type conflicts occur.
- **The Solution**: An MSBuild target `RepackMaterialDesign` merges `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, and `Microsoft.Xaml.Behaviors.dll` into `HPGrasshopper.McpBridge.dll` using `ILRepack 2.0.46`:
  ```xml
  <Target Name="RepackMaterialDesign" AfterTargets="CopyFilesToOutputDirectory" Condition="Exists('$(TargetDir)MaterialDesignThemes.Wpf.dll')">
      <Exec Command="&quot;$(PkgILRepack)\tools\ILRepack.exe&quot; /union /parallel /noRepackRes $(_RepackLib) /out:&quot;$(TargetDir)$(AssemblyName).dll&quot; &quot;@(IntermediateAssembly->'%(FullPath)')&quot; &quot;$(TargetDir)MaterialDesignThemes.Wpf.dll&quot; &quot;$(TargetDir)MaterialDesignColors.dll&quot; &quot;$(TargetDir)Microsoft.Xaml.Behaviors.dll&quot;"/>
      <Delete Files="$(TargetDir)MaterialDesignThemes.Wpf.dll;$(TargetDir)MaterialDesignColors.dll;$(TargetDir)Microsoft.Xaml.Behaviors.dll;$(TargetDir)MaterialDesignThemes.Wpf.xml" ContinueOnError="true" />
  </Target>
  ```
- **Contextual Reflection Scope**:
  `GrasshopperBridgeStatusView` initializes within:
  ```csharp
  using (AssemblyLoadContext.GetLoadContext(typeof(GrasshopperBridgeStatusView).Assembly)?.EnterContextualReflection())
  {
      InitializeComponent();
      MaterialThemeBridge.Attach(this, GrasshopperHostTheme.Instance, dark => GrasshopperRibbonIcons.CreateMcpIcon(dark));
  }
  ```

---

## 5. Inspection of Target Directory: `HPGrasshopper`

### 5.1 Current Status
- Directly inspected path `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper`.
- **Finding**: The directory does not exist yet.

### 5.2 Target Solution Blueprint: `HPGrasshopper.slnx`

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
  <!-- HPGrasshopper MCP: In-Process Dual-Lifecycle Bridge running inside Revit.exe / Rhino 8 / Grasshopper,
       stdio MCP Server executable launched by AI coding agents, and unit / E2E acceptance test suites.
       Shared engine, always referenced by relative path, never copied:
         ../McpShared/HPRebar.Mcp.Contracts
         ../McpShared/HPRebar.McpBridge.Core
         ../McpShared/HPRebar.Mcp.Server.Core -->
  <Project Path="HPGrasshopper.McpBridge/HPGrasshopper.McpBridge.csproj" />
  <Project Path="HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj" />
  <Project Path="HPGrasshopper.Mcp.Server/HPGrasshopper.Mcp.Server.csproj" />
  <Project Path="HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj" />
  <Project Path="HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj" />
  <Folder Name="/Shared/">
    <Project Path="../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj" />
    <Project Path="../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj" />
    <Project Path="../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj" />
  </Folder>
</Solution>
```

### 5.3 Proposed Directory Structure

```
HPGrasshopper/
├── Directory.Build.props
├── global.json
├── README.md
├── HPGrasshopper.slnx
│
├── HPGrasshopper.McpBridge/
│   ├── HPGrasshopper.McpBridge.csproj
│   ├── Coordinator/
│   │   ├── GrasshopperBridgeCoordinator.cs
│   │   ├── GrasshopperCompilerReferences.cs
│   │   ├── GrasshopperContextExtractor.cs
│   │   ├── GrasshopperExecutionEngine.cs
│   │   ├── GrasshopperTransactionDetector.cs
│   │   ├── IExternalEventRaiser.cs
│   │   ├── McpBridgeWorkRequest.cs
│   │   └── WorkItemTracker.cs
│   ├── Entry/
│   │   ├── GrasshopperAssemblyResolver.cs
│   │   ├── GrasshopperExternalEventHandler.cs
│   │   ├── GrasshopperRevitApp.cs
│   │   ├── RevitExternalEventRaiser.cs
│   │   └── ShowStatusDialogCommand.cs
│   ├── Execution/
│   │   ├── CanvasGraphRunner.cs
│   │   ├── GrasshopperEvaluationSync.cs
│   │   ├── HeadlessPlayerRunner.cs
│   │   ├── ITransactionContext.cs
│   │   ├── RevitTransactionContext.cs
│   │   └── RevitTransactionScope.cs
│   ├── Manifests/
│   │   └── HPGrasshopper.McpBridge.addin
│   ├── Resources/
│   │   ├── Icons/
│   │   │   └── GrasshopperRibbonIcons.cs
│   │   └── Themes/
│   │       ├── GrasshopperHostTheme.cs
│   │       ├── GrasshopperTheme.xaml
│   │       ├── IHostTheme.cs
│   │       ├── MaterialBridge.xaml
│   │       ├── MaterialThemeBridge.cs
│   │       ├── ThemeDark.xaml
│   │       ├── ThemeInfo.cs
│   │       └── ThemeLight.xaml
│   ├── Support/
│   │   ├── AutoDismissFailurePreprocessor.cs
│   │   ├── DocumentChangeCounter.cs
│   │   └── GrasshopperScriptGlobals.cs
│   └── View/
│       ├── GrasshopperBridgeStatusView.xaml
│       └── GrasshopperBridgeStatusView.xaml.cs
│
├── HPGrasshopper.McpBridge.Tests/
│   ├── HPGrasshopper.McpBridge.Tests.csproj
│   ├── Coordinator/
│   │   ├── CoordinatorTests.cs
│   │   └── GrasshopperExecutionEngineTests.cs
│   ├── Execution/
│   │   ├── TransactionScopeTests.cs
│   │   └── CanvasUndoRollbackTests.cs
│   └── Security/
│       ├── ScriptGuardTests.cs
│       └── OptInSecurityTests.cs
│
├── HPGrasshopper.Mcp.Server/
│   ├── HPGrasshopper.Mcp.Server.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Hosts/
│   │   └── GrasshopperHostProfile.cs
│   ├── Tools/
│   │   ├── ReadGrasshopperCanvasTool.cs
│   │   ├── RunGrasshopperPlayerTool.cs
│   │   ├── BatchEditGrasshopperCanvasTool.cs
│   │   ├── ExecuteGrasshopperCodeTool.cs
│   │   └── ExportGrasshopperCanvasImageTool.cs
│   └── Registry/
│       └── SeedLibrary/ (12 embedded seeds)
│           ├── Canvas/
│           ├── Geometry/
│           ├── RevitSync/
│           └── Analysis/
│
├── HPGrasshopper.Mcp.Server.Tests/
│   ├── HPGrasshopper.Mcp.Server.Tests.csproj
│   ├── Protocol/
│   │   └── GrasshopperToolsOverPipeTests.cs
│   ├── Registry/
│   │   ├── RegistryLifecycleTests.cs
│   │   └── RegistrySearchAndManageTests.cs
│   ├── Seeds/
│   │   ├── SeedCompilationTests.cs
│   │   ├── SeedManifestTests.cs
│   │   ├── SeedSchemaValidationTests.cs
│   │   └── ScriptQualityTests.cs
│   └── Surface/
│       ├── HostProfileBindingTests.cs
│       └── ToolSurfaceTests.cs
│
├── HPGrasshopper.E2E.Tests/
│   ├── HPGrasshopper.E2E.Tests.csproj
│   ├── Contracts/
│   ├── Harness/
│   │   ├── GrasshopperE2EFakeBridge.cs
│   │   └── GrasshopperE2ETestHarness.cs
│   └── Tiers/
│       ├── Tier1_ContractsAndServerTests.cs
│       ├── Tier2_RollbackAndSeedBoundariesTests.cs
│       ├── Tier3_CrossFeatureInteractionTests.cs
│       └── Tier4_RealWorldWorkloadTests.cs
│
└── tools/
    └── harness/
        └── run-live-verify.ps1
```

---

## 6. McpShared Integration Points (Requirements R3)

To integrate Grasshopper as a first-class host across `McpShared`, the following modifications must be documented for the implementation phase:

### 6.1 `HPRebar.Mcp.Contracts`
1. **`PipeNaming.cs`**:
   - Add constant: `public const string GrasshopperHost = "grasshopper";`
   - Route in `PipeNaming.For(string host, int version)`:
     ```csharp
     GrasshopperHost => "hpgrasshopper-mcp-" + version,
     ```
2. **`JsonRpcMethods.cs`**:
   - Add constant: `public const string GrasshopperPrefix = "grasshopper.";`

### 6.2 `HPRebar.McpBridge.Core`
1. **`GuardProfile.cs`**:
   - Define `GuardProfile.Grasshopper`:
     - Denies UI blocking dialogs: `"MessageBox"`, `"TaskDialog"`, `"Eto.Forms.Dialog"`.
     - Denies process killers: `"Exit"`, `"Quit"`, `"CloseApplication"`.
     - Denies bridge internal namespaces: `"HPGrasshopper.McpBridge"`, `"HPRebar.McpBridge.Core.Host"`.
     - Allows: `Rhino.*`, `Grasshopper.*`, `GH_IO.*`, `RhinoInside.Revit.*`, `Autodesk.Revit.*`.
2. **`AnalyzerProfile.cs`**:
   - Define `AnalyzerProfile.Grasshopper`:
     - Recognizes Revit transaction constructors: `"Transaction"`, `"TransactionGroup"`, `"SubTransaction"`.
3. **`HostScriptContracts.cs`**:
   - Define default script imports for Grasshopper:
     - `System`, `System.Collections.Generic`, `System.Linq`
     - `Rhino`, `Rhino.Geometry`, `Rhino.DocObjects`
     - `Grasshopper`, `Grasshopper.Kernel`, `Grasshopper.Kernel.Data`, `Grasshopper.Kernel.Types`
     - `Autodesk.Revit.DB`, `Autodesk.Revit.UI`
     - `RhinoInside.Revit`, `RhinoInside.Revit.Convert.Geometry`

### 6.3 `HPRebar.Mcp.Server.Core`
1. **`HostProfile.cs`**:
   - Define `HostProfile.Grasshopper`:
     - `HostId`: `"grasshopper"`
     - `DisplayName`: `"Grasshopper"`
     - `ServerName`: `"HPGrasshopper MCP"`
     - `ProductFolder`: `"HPGrasshopper"`
     - `EnvPrefix`: `"HPGRASSHOPPER_MCP_"`
     - `DefaultVersion`: `2026`
     - `MethodPrefix`: `JsonRpcMethods.GrasshopperPrefix`
     - `ExecuteToolName`: `"execute_grasshopper_code"`
     - `ContextToolName`: `"read_grasshopper_canvas"`
     - `ResourceScheme`: `"grasshopper"`
     - `CoreToolNames`: `["read_grasshopper_canvas", "run_grasshopper_player", "batch_edit_grasshopper_canvas", "execute_grasshopper_code", "export_grasshopper_canvas_image"]`
     - `Categories`: `["Canvas", "Player", "Geometry", "RevitSync", "Analysis", "Data", "Generic"]`

---

## 7. Concrete Script Contract & Global Environment

For `execute_grasshopper_code`, the Roslyn globals class (`GrasshopperScriptGlobals`) must provide:
```csharp
public sealed class GrasshopperScriptGlobals
{
    public GH_Document? doc { get; }            // Active Grasshopper document
    public GH_Canvas? canvas { get; }            // Active Grasshopper canvas
    public RhinoDoc? rhinoDoc { get; }           // Active Rhino document
    public Document? revitDoc { get; }           // Active Revit DB Document
    public UIDocument? uidoc { get; }            // Active Revit UI Document
    public UIApplication? uiapp { get; }         // Revit UIApplication
    public CancellationToken ct { get; }         // Linked cancellation token
    public Action<string?> log { get; }          // Audit logger
    public Action<int, int, string?> progress { get; } // Progress reporter
    public ScriptArgs args { get; }              // JSON input arguments
    public bool dryRun { get; }                  // dryRun flag
}
```

---

## 8. Verification Strategy & Acceptance Criteria Mapping

| Acceptance Criterion | Verification Method |
|---|---|
| **Zero Build Errors** | `dotnet build HPGrasshopper/HPGrasshopper.slnx -c Release` (0 warnings, 0 errors). |
| **No Copied Libraries** | Solution and project files reference `../McpShared/` only. |
| **Named Pipe Handshake** | Pipe `hpgrasshopper-mcp-2026` tested via `HPGrasshopper.E2E.Tests` / PowerShell. |
| **24 MCP Tools Catalog** | `tools/list` returns 5 Core + 8 Registry Meta + 12 Embedded Seeds = 25 tools total. |
| **Dual-Level Safety** | `RevitTransactionScope` rollback tested for Revit elements; canvas undo tested for GH components. |
| **ADR-0007 Code Quality** | `ScriptQualityTests` walker verifies all 12 embedded seeds against dead code, empty catches, and 300-line limit. |

---
*Report completed by explorer_survey_2 for parent orchestrator review.*

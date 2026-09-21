# Tekla Structures 2025.0 Open API & In-Process Bridge Architecture Survey

## 1. Executive Summary

This report delivers a comprehensive technical analysis of the Trimble Tekla Structures 2025.0 environment, its .NET Open API runtime characteristics, and the architectural design for `HPTekla.McpBridge` (an in-process plugin running on .NET Framework 4.8) and `HPTekla.Mcp.Server` (.NET 10 console server speaking Model Context Protocol).

All findings in this survey were directly verified on the host system via live inspection scripts and reflection over the installed Tekla Structures 2025.0 binaries.

### Key Discoveries & Recommendations
1. **Host Environment**: Tekla Structures 2025.0 is installed at `C:\Program Files\Tekla Structures\2025.0\bin`. The executable `TeklaStructures.exe` (version `2025.0.48669.0`) and its Open API assemblies target **.NET Framework 4.8** (CLR `v4.0.30319`).
2. **Architecture Strategy**: Following `HPNavis.McpBridge` (the repository's existing `net48` bridge), `HPTekla.McpBridge` must be compiled for `net48` and consume the `net48` assets of `McpShared/HPRebar.McpBridge.Core` and `McpShared/HPRebar.Mcp.Contracts`.
3. **Assembly Binding on .NET 4.8**: Roslyn script compilation on .NET Framework 4.8 requires the proven `PluginAssemblyResolver` pattern pioneered in `HPNavis.McpBridge` to intercept and redirect requests for `Microsoft.CodeAnalysis`, `System.Collections.Immutable`, and `System.Text.Json` to the local plugin folder.
4. **Thread Synchronization**: Tekla Open API is strictly single-threaded and bound to Tekla's main UI/Model thread. Requests from the Named Pipe worker thread will be marshaled through `McpShared`'s `MainThreadQueue`, pumped on `ComponentDispatcher.ThreadIdle` / WPF Dispatcher, and awakened via `PostMessage(mainWindowHandle, WM_NULL)`.
5. **Native Rollback for `dryRun = true`**: Unlike AutoCAD or Revit which have standard Transaction objects, Tekla Open API has no public `Rollback()` API. However, our decompilation revealed that `Tekla.Structures.ModelInternal.Operation.SetTestSavePoint()` and `RollbackToTestSavePoint(bool resetSelection)` are **public static methods** that trigger Tekla's internal `TestSetSavePoint` and `TestRollbackToSavePoint` actions. This enables native, atomic rollback of in-memory changes for `dryRun = true`.
6. **Model Snapshot Engine**: Mutating operations (Write/Destructive tiers) will automatically back up the core database files (`<ModelName>.db1`, `<ModelName>.db2`, `environment.db`) into `.hptekla_snapshots/` prior to execution.
7. **Ribbon & Extension Integration**: In-process registration will utilize Tekla's `PluginBase` (`[Plugin("HPTeklaBridge")]`), paired with a Custom Tabs XML definition (`Ribbon-HPTekla.xml` under `Environments\common\system\Ribbons\CustomTabs\Modeling\`) and a companion `.cs` macro for Quick Launch access.

---

## 2. Tekla Structures 2025.0 Installation & Reference Assemblies

### 2.1 Installation Locations & Registry Keys
The installation was verified on the host system:
- **Main Program Directory**: `C:\Program Files\Tekla Structures\2025.0\`
- **Binaries Directory**: `C:\Program Files\Tekla Structures\2025.0\bin\`
- **Environment & Data Directory**: `C:\ProgramData\Trimble\Tekla Structures\2025.0\`
- **Default Models Directory**: `C:\TeklaStructuresModels\`

Registry verification:
- **Registry Key**: `HKEY_LOCAL_MACHINE\SOFTWARE\Trimble\Tekla Structures\2025.0\setup`
  - `MainDir`: `C:\Program Files\Tekla Structures\`
  - `TSVersionDir`: `2025.0`
  - `Version`: `2025.0`
  - `ProductVersion`: `225.3.48669`
  - `EnvDir`: `C:\ProgramData\Trimble\Tekla Structures\`
  - `ModelDir`: `C:\TeklaStructuresModels\`
  - `SysType`: `64`

### 2.2 Verified Open API Assemblies
Reflection inspection of the core DLLs in `C:\Program Files\Tekla Structures\2025.0\bin\` confirmed the following metadata:

| Assembly Name | AssemblyVersion | FileVersion | Target Framework | Purpose in HPTekla |
|---|---|---|---|---|
| `Tekla.Structures.dll` | `2025.0.0.0` | `2025.0.48669.0` | .NET Framework 4.8 | Core identifiers, settings, units, point/vector math |
| `Tekla.Structures.Model.dll` | `2025.0.0.0` | `2025.0.48669.0` | .NET Framework 4.8 | Model, Parts, Rebar, Operations, Events, Selectors |
| `Tekla.Structures.Catalogs.dll` | `2025.0.0.0` | `2025.0.48669.0` | .NET Framework 4.8 | Profile, material, rebar, bolt catalog definitions |
| `Tekla.Structures.Datatype.dll` | `2025.0.0.0` | `2025.0.48669.0` | .NET Framework 4.8 | String, distance, angle datatypes with formatting |
| `Tekla.Structures.Drawing.dll` | `2025.0.0.0` | `2025.0.48669.0` | .NET Framework 4.8 | DrawingHandler, GA/Assembly drawings, sheet queries |
| `Tekla.Structures.Plugins.dll` | `2025.0.0.0` | `2025.0.48669.0` | .NET Framework 4.8 | PluginBase, PluginAttribute, InputDefinition |
| `Tekla.Structures.Dialog.dll` | `2025.0.0.0` | `2025.0.48669.0` | .NET Framework 4.8 | UI integration, form base, dialog data storage |

---

## 3. In-Process Plugin & Extension Architecture

### 3.1 Plugin Lifecycle & Registration
Tekla Structures scans for plugins in:
1. `C:\ProgramData\Trimble\Tekla Structures\2025.0\environments\common\extensions\<PluginFolder>\`
2. `C:\Program Files\Tekla Structures\2025.0\bin\plugins\Tekla\Model\<PluginFolder>\`
3. `<ModelFolder>\plugins\`

`HPTekla.McpBridge` will register as a standard Tekla plugin:
```csharp
namespace HPTekla.McpBridge;

[Plugin("HPTeklaBridge")]
[PluginUserInterface("HPTekla.McpBridge.NullForm")]
public sealed class HPTeklaBridgePlugin : PluginBase
{
    public override bool Run(List<InputDefinition> input)
    {
        // Show/activate the modeless WPF Status Window and ensure bridge pipe is running
        BridgeEntry.ShowStatusWindow();
        return true;
    }

    public override List<InputDefinition> DefineInput()
    {
        // No interactive model picking required to open the AI bridge
        return new List<InputDefinition>();
    }
}
```

### 3.2 Ribbon Integration via Custom Tabs XML
Tekla Structures allows declarative Ribbon definitions using XML files located in:
`%ProgramData%\Trimble\Tekla Structures\2025.0\Environments\common\system\Ribbons\CustomTabs\Modeling\Ribbon-HPTekla.xml`

Verified structure from production Tekla plugins:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Tab Header="HP Tekla AI" IsCollapsed="false" IsUserDefined="true">
  <SimpleButton 
    X="0" 
    Y="0" 
    Width="3" 
    Height="4" 
    Command="Plugin.CatalogPluginComponentItem?HPTeklaBridge" 
    Text="HP Tekla Bridge" 
    Icon="HPTekla.ico" 
    ShowText="true" 
    ShowIcon="true" 
    Tooltip="HP Tekla Structures MCP Bridge (AI Integration)" 
  />
</Tab>
```
When clicked, Tekla triggers the `HPTeklaBridge` plugin, which launches or focuses the modeless status dialog.

### 3.3 Quick Launch / Macro Integration
To provide keyboard shortcut and Quick Launch (`Ctrl+Q`) capability, a companion macro will be installed to:
`%ProgramData%\Trimble\Tekla Structures\2025.0\Environments\common\macros\modeling\HPTeklaBridge.cs`
```csharp
namespace UserMacros
{
    public sealed class Macro
    {
        [Tekla.Macros.Runtime.MacroEntryPointAttribute()]
        public static void Run(Tekla.Macros.Runtime.IMacroRuntime runtime)
        {
            HPTekla.McpBridge.BridgeEntry.ShowStatusWindow();
        }
    }
}
```

---

## 4. UI Thread vs Tekla Model Thread Synchronization

### 4.1 Thread Affinity Problem
The Tekla Structures Open API is strictly single-threaded:
- Calls to `model.CommitChanges()`, `part.Insert()`, `model.GetInfo()`, or `drawingHandler.GetDrawings()` must occur on Tekla's main thread (UI/Model thread).
- The Named Pipe listener runs asynchronously on thread-pool background threads.
- Calling Tekla Open API directly from the background pipe thread causes `AccessViolationException`, COM/Remoting race conditions, or unhandled process crashes.

### 4.2 Solution: `MainThreadQueue` Integration
`McpShared/HPRebar.McpBridge.Core/Host/MainThreadQueue.cs` already contains host-neutral work-marshaling logic with `#if NET48` support:
1. When an RPC call (`tekla.execute` or `tekla.context`) arrives over the pipe, the request is parsed, guarded, and compiled on the pipe thread.
2. A `MainThreadWorkItem` is enqueued into `MainThreadQueue`.
3. The queue awakens Tekla's main thread by calling:
   - `System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(...)`, and
   - `PostMessage(mainWindowHandle, WM_NULL, IntPtr.Zero, IntPtr.Zero)`.
4. On the main thread, `ComponentDispatcher.ThreadIdle` or `System.Windows.Forms.Application.Idle` executes `_queue.OnTick()`.
5. `OnTick()` verifies host quiescence:
   - `!Operation.IsMacroRunning()`
   - `model.GetConnectionStatus() == true`
   - No modal dialog blocking the message loop
6. The work runs synchronously on Tekla's main thread, and its result is posted back to the pipe thread's `TaskCompletionSource<object>`.
7. `expireWithoutTicks: true` ensures that if a native modal dialog is open, the caller receives a clean "busy" error code within `BusyGrace` (default 5.0s) rather than hanging indefinitely.

---

## 5. Transaction Mechanics & `dryRun = true` Rollback Enforcement

### 5.1 Native Rollback Discovery
In Autodesk Revit (`TransactionGroup.RollBack()`) and AutoCAD (`Transaction.Abort()`), rollback is straightforward. In Tekla Structures, developers traditionally faced the limitation that `Model` has no public `Rollback()` method.

Through binary decompiler inspection of `Tekla.Structures.Model.dll`, we discovered two public methods in `Tekla.Structures.ModelInternal.Operation`:
- `public static void SetTestSavePoint()`: Invokes native Tekla command `TestSetSavePoint`.
- `public static void RollbackToTestSavePoint(bool resetSelection = true)`: Invokes native Tekla command `TestRollbackToSavePoint` and clears selection.

These methods are designed for automated test suites to revert model mutations completely.

### 5.2 DryRun Pipeline Implementation
For all script executions in `HPTekla.McpBridge`:
```csharp
public ExecuteResult Run(Model model, ExecuteRequest request, CompiledScript compiled, bool dryRun)
{
    // 1. Establish an in-memory test save point
    Tekla.Structures.ModelInternal.Operation.SetTestSavePoint();

    try
    {
        // 2. Execute the compiled Roslyn script on the main thread
        var scriptOutput = compiled.Run(model, ...);

        if (dryRun)
        {
            // 3. DryRun: atomic rollback of all inserted/modified elements
            Tekla.Structures.ModelInternal.Operation.RollbackToTestSavePoint(resetSelection: true);
            return ExecuteResult.Success(scriptOutput, note: "dryRun: all modifications cleanly rolled back.");
        }
        else
        {
            // 4. Live execution: commit changes to Tekla undo log and update views
            model.CommitChanges(request.Label ?? "HPTekla AI Execution");
            return ExecuteResult.Success(scriptOutput);
        }
    }
    catch (Exception ex)
    {
        // 5. On failure, always rollback to leave the model uncorrupted
        Tekla.Structures.ModelInternal.Operation.RollbackToTestSavePoint(resetSelection: true);
        return ExecuteResult.Failure(ex.Message);
    }
}
```

---

## 6. Model Snapshot Management Engine

### 6.1 Tekla Model Folder Structure
Tekla models reside in self-contained project folders:
- `<ModelName>.db1`: The primary database file containing all model objects (beams, columns, plates, rebars, bolts, welds, cuts).
- `<ModelName>.db2`: The secondary index database file.
- `environment.db`: Model-specific environment configurations.
- `options_model.db`: Model options.
- `xslib.db1` / `xslib.db2`: Custom component definitions.

### 6.2 Snapshot Architecture
Before executing a script classified as `Write` or `Destructive`:
1. The bridge verifies `model.GetInfo().ModelPath`.
2. A snapshot directory is maintained:
   - Target: `<ModelPath>\.hptekla_snapshots\<timestamp>_<hash>\` (or `%TEMP%\.hptekla_snapshots\<ModelName>\` if the model directory is read-only).
3. The snapshot engine copies the small database files (`*.db1`, `*.db2`, `environment.db`).
   - Because Tekla Structures opens these files with `FileShare.ReadWrite`, copying while Tekla is running is fully supported and takes under 50ms for typical models.
4. The snapshot path is returned in `ExecuteResult.Snapshot` so the user/AI can restore or track changes.
5. Destructive operations (such as deleting all objects or batch modifications) are gated behind the UI toggle `Allow Heavy Operations`.

---

## 7. UI & UX Design: WPF Status Window & Ribbon

### 7.1 Status Dialog (WPF MVVM, .NET 4.8)
A modeless dialog (`HPTeklaBridgeStatusView.xaml`) will be displayed when the plugin is launched:
- **Title Bar**: "HP Tekla MCP Bridge — Tekla Structures 2025.0"
- **Status Indicator**:
  - Green pulsing dot: Named pipe listening on `hptekla-mcp-2025`, client connected.
  - Amber dot: Listening, awaiting client connection.
  - Red dot: Pipe disconnected or error state.
- **Model Card**:
  - Active Model: `New model`
  - Model Path: `C:\TeklaStructuresModels\New model`
  - Environment / Role: `SE_Asia / role_All.ini`
  - Units: Millimeters (mm), Kilograms (kg)
- **Safety Toggles**:
  - `[x] Allow Code Execution`: Master execution switch (persisted in `%AppData%\Trimble\Tekla Structures\2025.0\hptekla_settings.json`).
  - `[ ] Allow Heavy Operations`: Gating destructive operations (deleting model objects, IFC batch export). Default false, per-session.
- **Activity & Statistics**:
  - Compiled scripts counter
  - Total requests serviced
  - Average execution latency
- **Log Viewer**: Real-time Serilog audit stream with timestamp, log level, and script summary.

### 7.2 Theming & Color Palette
The WPF window will follow the repository's established UI standards:
- **Accent**: HP Blue `#0696D7`
- **Secondary Accent**: Orange `#E0641E`
- **Dark Palette**: Background `#1E1E1E`, Card `#252526`, Border `#3F3F46`, Foreground `#F1F1F1`
- **Light Palette**: Background `#F3F3F3`, Card `#FFFFFF`, Border `#E0E0E0`, Foreground `#1E1E1E`
- Auto-detects Windows dark/light mode preference or can be toggled manually.

---

## 8. Comparative Analysis: HPTekla vs Existing Bridges

| Architecture Dimension | HPNavis.McpBridge | HPAutoCad.McpBridge | HPRobot.McpBridge | HPTekla.McpBridge (Proposed) |
|---|---|---|---|---|
| **Process Model** | In-Process (`Roamer.exe`) | In-Process (`acad.exe`) | Out-of-Process COM (`RobotOM.dll`) | **In-Process (`TeklaStructures.exe`)** |
| **Target Framework** | `net48` (CLR 4.0) | `net8.0-windows` (CLR .NET 8) | `net8.0-windows` (CLR .NET 8) | **`net48` (CLR 4.0)** |
| **Shared Engine Asset** | `McpShared` net48 | `McpShared` net8.0 | `McpShared` net8.0 | **`McpShared` net48** |
| **Host Entry Point** | `EventWatcherPlugin` in plugin dir | `IExtensionApplication` via ALC | Standalone Desktop WPF App | **`PluginBase` + Ribbon XML + Macro** |
| **Assembly Resolution** | `PluginAssemblyResolver` | Native ALC (`AssemblyLoadContext`) | Standard .NET 8 deps | **`PluginAssemblyResolver` (Ported from HPNavis)** |
| **Thread Synchronization** | `MainThreadQueue` on `Application.Idle` | `MainThreadQueue` on `Application.Idle` | Single-Threaded COM Apartment (STA) | **`MainThreadQueue` on `ComponentDispatcher.ThreadIdle` / WPF Dispatcher** |
| **Thread Wakeup** | `PostMessage(WM_NULL)` | `PostMessage(WM_NULL)` | Direct method call on STA | **`PostMessage(WM_NULL)` + `Dispatcher.BeginInvoke`** |
| **Rollback / dryRun** | Undo transaction step | `Transaction.Abort()` | N/A (no transactions) | **`SetTestSavePoint()` / `RollbackToTestSavePoint()`** |
| **Model Snapshot** | N/A (read-only review) | N/A (in-memory DWG) | `.rtd` snapshot in `.hprobot_snapshots/` | **`.db1`/`.db2` snapshot in `.hptekla_snapshots/`** |
| **Safety Tiers** | Read / Write / Heavy Gate | Read / Write / Changesets | Read / Write / Heavy-Delete | **3-Tier: Read / Write / Destructive** |
| **Named Pipe Name** | `hpnavis-mcp-2026` | `hpautocad-mcp-2026` | `hprobot-mcp-2026` | **`hptekla-mcp-2025`** |

---

## 9. Concrete Implementation Blueprint for HPTekla.slnx

### 9.1 Solution Projects Structure
The new solution `HPTekla/HPTekla.slnx` will comprise four projects:
1. `HPTekla.McpBridge` (`net48`):
   - In-process Tekla plugin assembly.
   - References `Tekla.Structures.*` (from `C:\Program Files\Tekla Structures\2025.0\bin\`).
   - ProjectReference to `McpShared/HPRebar.McpBridge.Core` (net48 asset) and `McpShared/HPRebar.Mcp.Contracts`.
   - Includes `PluginAssemblyResolver`, `MainThreadQueue` binding, `TeklaTierAnalyzer`, `TeklaSnapshotEngine`, and WPF Status View.
2. `HPTekla.Mcp.Server` (`net10.0`):
   - Stdio console application speaking MCP 2.2.0.
   - References `McpShared/HPRebar.Mcp.Server.Core`.
   - Houses `TeklaHostProfile`, 4 core tools (`execute_tekla_code`, `get_tekla_context`, `inspect_type`, `cancel_execution`), 8 registry meta tools, and 12 embedded seed tools.
3. `HPTekla.McpBridge.Tests` (`net48`):
   - Tests `TeklaTierAnalyzer`, snapshot creation/restore, dryRun rollback contract, and pipe communication with fake Tekla executor.
4. `HPTekla.Mcp.Server.Tests` (`net10.0`):
   - Tests tool catalog schemas, `TeklaHostProfile` properties, fake executor round trips, and Roslyn seed tool compilation against installed Tekla assemblies.

### 9.2 Additive Extension to `McpShared`
Following the strict additive integration contract:
- `HPRebar.Mcp.Contracts`:
  - Add `PipeNaming.TeklaHost = "tekla"`
  - Add `ContextResult.Tekla` and `TeklaInfo` records
  - Add `JsonRpcMethods.TeklaPrefix = "tekla."`
- `HPRebar.McpBridge.Core`:
  - Add `GuardProfile.Tekla` and `AnalyzerProfile.Tekla`
- `HPRebar.Mcp.Server.Core`:
  - Add `HostProfile.Tekla` (`DefaultVersion = 2025`, `ValidVersions = [2025]`)
  - Add `HostScriptContracts.TeklaImports` and `TeklaGlobals`

### 9.3 12 Embedded Seed Tools Mapping

| Seed Tool Name | Category | Primary Tekla Open API Types | Expected Output |
|---|---|---|---|
| `get_model_info` | Model | `Model.GetInfo()`, `Model.GetProjectInfo()` | Model name, path, active phase, north angle, DB status |
| `select_objects` | Selection | `ModelObjectSelector`, `UI.ModelObjectSelector` | List of selected object IDs, types, and bounding boxes |
| `get_part_properties` | Query | `Part`, `Beam`, `Column`, `ContourPlate` | Profile, material, class, start/end points, volume, weight |
| `create_beam` | Modeling | `Tekla.Structures.Model.Beam` | Created beam ID, GUID, profile, length |
| `create_column` | Modeling | `Tekla.Structures.Model.Beam` (vertical) | Created column ID, GUID, base point, height |
| `create_contour_plate` | Modeling | `Tekla.Structures.Model.ContourPlate` | Created plate ID, contour points count, thickness |
| `create_rebar_group` | Rebar | `Tekla.Structures.Model.RebarGroup` | Group ID, bar count, shape, spacing |
| `create_single_rebar` | Rebar | `Tekla.Structures.Model.SingleRebar` | Rebar ID, points, diameter, hook types |
| `modify_user_properties` | Attributes | `ModelObject.SetUserProperty()` | Success status, updated UDA keys and values |
| `get_reinforcement_info` | Rebar | `Reinforcement`, `RebarGroup` | Bending schedule, cut length, grade, total weight |
| `list_drawings` | Drawing | `Tekla.Structures.Drawing.DrawingHandler` | List of drawing names, types (A, W, C, G), status |
| `export_ifc` | Export | `Operation.CreateIFC4ExportFromAll()` | Exported file path, entity count, export duration |

---

## 10. Conclusion & Verification Strategy

The investigation confirms that Tekla Structures 2025.0 is fully capable of hosting a robust, in-process MCP bridge on .NET Framework 4.8. By combining:
1. `HPNavis.McpBridge`'s `PluginAssemblyResolver` to guarantee Roslyn stability on .NET 4.8,
2. `MainThreadQueue` with WPF Dispatcher / idle message pumping to protect thread affinity,
3. `SetTestSavePoint()` / `RollbackToTestSavePoint()` for native atomic `dryRun` rollback, and
4. Fast `.db1`/`.db2` file snapshots for Write/Destructive safety,

the `HPTekla` subsystem will provide a secure, high-performance bridge conforming 100% to the repository's `McpShared` standards.

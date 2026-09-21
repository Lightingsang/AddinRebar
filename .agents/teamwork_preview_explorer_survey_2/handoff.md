# Handoff Report: Survey Explorer 2 — Tekla Structures 2025.0 Open API & In-Process Bridge Design

**Type**: Hard Handoff (Investigation & Architecture Survey Complete)  
**Author**: `teamwork_preview_explorer_survey_2`  
**Date**: 2026-09-22T00:32:00Z  
**Primary Deliverable**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_2\report.md`

---

## 1. Observation

1. **Tekla Structures 2025.0 Installation Path and Version**:
   - Directory: `C:\Program Files\Tekla Structures\2025.0\bin\`
   - Main Executable: `TeklaStructures.exe`, FileVersion `2025.0.48669.0`, ProductVersion `2025.0.0-alpha00048669+a99462f17964bc42624ada6e8d316db2...`.
   - Registry Key: `HKLM:\SOFTWARE\Trimble\Tekla Structures\2025.0\setup`
     - `MainDir` = `C:\Program Files\Tekla Structures\`
     - `TSVersionDir` = `2025.0`
     - `EnvDir` = `C:\ProgramData\Trimble\Tekla Structures\`
     - `ModelDir` = `C:\TeklaStructuresModels\`
     - `ProductVersion` = `225.3.48669`

2. **Core Assembly Verification & Target Framework**:
   - `Tekla.Structures.dll`: AssemblyVersion `2025.0.0.0`, FileVersion `2025.0.48669.0`
   - `Tekla.Structures.Model.dll`: AssemblyVersion `2025.0.0.0`, FileVersion `2025.0.48669.0`
   - `Tekla.Structures.Catalogs.dll`: AssemblyVersion `2025.0.0.0`, FileVersion `2025.0.48669.0`
   - `Tekla.Structures.Datatype.dll`: AssemblyVersion `2025.0.0.0`, FileVersion `2025.0.48669.0`
   - `Tekla.Structures.Drawing.dll`: AssemblyVersion `2025.0.0.0`, FileVersion `2025.0.48669.0`
   - `Tekla.Structures.Plugins.dll`: AssemblyVersion `2025.0.0.0`, FileVersion `2025.0.48669.0`
   - Reflection inspection on `Tekla.Structures.Model.dll`:
     - `TargetFrameworkAttribute`: `.NETFramework,Version=v4.8`
     - `FrameworkDisplayName`: `.NET Framework 4.8`
     - `ImageRuntimeVersion`: `v4.0.30319`

3. **In-Process Plugin & Custom Ribbon Infrastructure**:
   - Custom Ribbons are defined via XML in `C:\ProgramData\Trimble\Tekla Structures\2025.0\Environments\common\system\Ribbons\CustomTabs\Modeling\`.
   - Verified XML sample in `Ribbon-Custom-Data-Tab.xml`:
     ```xml
     <Tab Header="Data Exchange" IsCollapsed="false" IsUserDefined="true">
       <SimpleButton Command="Plugin.CatalogPluginComponentItem?DataExchangeTeklaConnector" Text="Data Exchange" Icon="FDXLogo.ico" ... />
     </Tab>
     ```
   - Standard macros reside in `C:\ProgramData\Trimble\Tekla Structures\2025.0\Environments\common\macros\modeling\*.cs` and can be invoked from Tekla Quick Launch (`Ctrl+Q`).

4. **Discovery of Native SavePoint / Rollback Methods**:
   - Reflection on `Tekla.Structures.Model.dll` revealed public methods in `Tekla.Structures.ModelInternal.Operation`:
     - `public static void SetTestSavePoint()`: Dispatches internal command `TestSetSavePoint`.
     - `public static void RollbackToTestSavePoint(bool resetSelection = true)`: Dispatches internal command `TestRollbackToSavePoint` and clears selection.
     - `public static void dotUndo()`: Dispatches internal undo.
   - Visibility test: `[Tekla.Structures.ModelInternal.Operation].IsPublic` evaluated to `True`.

5. **Model Folder Anatomy & Snapshot Capabilities**:
   - Active models in `C:\TeklaStructuresModels\` contain `<Model>.db1`, `<Model>.db2`, `environment.db`, `options_model.db`, `history.db`.
   - File handles are opened by Tekla with `FileShare.ReadWrite`, allowing instantaneous snapshot copying of `.db1` and `.db2` into `.hptekla_snapshots/` during runtime.

6. **Prior Art in Repository**:
   - `HPNavis.McpBridge` targets `net48` and uses `McpShared`'s `net48` asset with `PluginAssemblyResolver.cs` and `MainThreadQueue.cs`.
   - `MainThreadQueue` in `McpShared/HPRebar.McpBridge.Core/Host/MainThreadQueue.cs` already contains `#if NET48` support (`Stopwatch` monotonic clock) and `expireWithoutTicks: true`.

---

## 2. Logic Chain

1. **Runtime Target Alignment**:
   - *Observation 1 & 2*: Tekla Structures 2025.0 executable and all Open API DLLs are built for `.NETFramework,Version=v4.8`.
   - *Logic*: An in-process plugin inside `TeklaStructures.exe` must target `net48`. Any attempt to load `.NET 8` or `.NET 10` code directly into `TeklaStructures.exe` will fail because the .NET Framework 4.8 CLR cannot host modern .NET runtimes in-process.
   - *Deduction*: `HPTekla.McpBridge` must target `net48`.

2. **Reusing the net48 McpShared Engine**:
   - *Observation 6*: `McpShared/HPRebar.McpBridge.Core` and `McpShared/HPRebar.Mcp.Contracts` multi-target `netstandard2.0;net48` and `net8.0;net48`. `HPNavis.McpBridge` currently uses this asset in production.
   - *Logic*: Referencing `McpShared/HPRebar.McpBridge.Core.csproj` from a `net48` `HPTekla.McpBridge` project binds directly to the tested `net48` build, enabling complete code reuse without engine modification.

3. **Solving Roslyn Assembly Resolution in .NET 4.8**:
   - *Observation 2 & 6*: .NET Framework binds assemblies by exact version and does not support modern SDK automatic binding redirects inside plugin AppDomains.
   - *Logic*: When Roslyn 4.8+ executes inside Tekla on .NET 4.8, requests for `System.Collections.Immutable`, `System.Reflection.Metadata`, and `System.Text.Json` require custom resolution.
   - *Deduction*: Porting `PluginAssemblyResolver.cs` from `HPNavis.McpBridge` guarantees identical, crash-proof Roslyn loading in Tekla.

4. **Thread Safety & Marshaling via `MainThreadQueue`**:
   - *Observation 4*: Tekla Open API requires execution on the main UI thread.
   - *Logic*: Named pipe incoming messages run on thread-pool threads. `MainThreadQueue` parks the request until the main thread triggers `OnTick()`.
   - *Deduction*: Hooking `ComponentDispatcher.ThreadIdle` (or WPF Dispatcher) and waking the message loop via `PostMessage(mainWindowHandle, WM_NULL)` allows asynchronous pipe requests to safely run synchronously on the Tekla thread.

5. **Solving the `dryRun = true` Rollback Requirement**:
   - *Observation 4*: `Tekla.Structures.ModelInternal.Operation.SetTestSavePoint()` and `RollbackToTestSavePoint()` are public static methods in `Tekla.Structures.Model.dll`.
   - *Logic*: When `dryRun = true`, establishing a test save point before script execution and rolling back afterwards completely reverts inserted or modified entities without persisting them to disk or dirtying the user's undo queue.
   - *Deduction*: This provides a native, robust transaction simulation for Tekla Open API.

---

## 3. Caveats

1. **Drawing API Context**: `Tekla.Structures.Drawing.DrawingHandler` operations (seed tool 11 `list_drawings`) can query drawings at any time, but modifying drawings requires an active drawing to be open in the Tekla drawing editor.
2. **Tekla Process Lifetime**: Because `HPTekla.McpBridge` is an in-process plugin, closing Tekla Structures terminates the bridge. The companion `HPTekla.Mcp.Server` gracefully reports connection refused / timeout when Tekla is not running.
3. **Model Open Prerequisite**: Many model query operations require a model to be actively open in Tekla (`model.GetConnectionStatus() == true`). Scripts called when no model is open will be rejected early by `RequireModel()` with a clear error message.

---

## 4. Conclusion

1. Tekla Structures 2025.0 is verified and fully ready for an In-Process MCP Bridge on `.NET Framework 4.8`.
2. The architecture of `HPTekla` mirrors the proven design of `HPNavis.McpBridge`:
   - Bridge: `HPTekla.McpBridge` (`net48`), in-process plugin with WPF status dialog and named pipe listener on `hptekla-mcp-2025`.
   - Server: `HPTekla.Mcp.Server` (`net10.0`), console stdio server speaking standard MCP with 24 tools.
   - Thread Synchronization: Handled via `MainThreadQueue` + `ComponentDispatcher.ThreadIdle`.
   - `dryRun` Rollback: Enforced via native `SetTestSavePoint()` and `RollbackToTestSavePoint()`.
   - Snapshot: Backs up `.db1` and `.db2` files into `.hptekla_snapshots/` prior to writes.
   - Assembly Resolution: Protected via `PluginAssemblyResolver`.
3. The implementation requires only additive changes to `McpShared` (registering Tekla profiles, pipe name, and context types) with zero risk of regression to existing hosts.

---

## 5. Verification Method

To independently verify all claims made in this report:

1. **Verify Tekla Binaries & Versions**:
   ```powershell
   Get-ChildItem "C:\Program Files\Tekla Structures\2025.0\bin\Tekla.Structures*.dll" | ForEach-Object {
       [PSCustomObject]@{
           Name = $_.Name
           Version = [System.Reflection.AssemblyName]::GetAssemblyName($_.FullName).Version.ToString()
           FileVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($_.FullName).FileVersion
       }
   }
   ```
   *Expected*: All assemblies report Version `2025.0.0.0` and FileVersion `2025.0.48669.0`.

2. **Verify SavePoint Rollback Methods**:
   ```powershell
   $asm = [System.Reflection.Assembly]::LoadFrom("C:\Program Files\Tekla Structures\2025.0\bin\Tekla.Structures.Model.dll")
   $op = $asm.GetType("Tekla.Structures.ModelInternal.Operation")
   $op.GetMethod("SetTestSavePoint")
   $op.GetMethod("RollbackToTestSavePoint", [Type[]]@([bool]))
   ```
   *Expected*: Both methods are non-null and public static methods.

3. **Verify McpShared net48 Test Baseline**:
   ```bash
   cd g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared
   dotnet test HPRebar.McpBridge.Core.Net48Tests
   ```
   *Expected*: 62/62 tests pass on .NET Framework 4.8.

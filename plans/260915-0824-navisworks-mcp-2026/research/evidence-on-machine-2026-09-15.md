# Evidence log — on-machine verification for the Navisworks 2026 MCP plan

**Date:** 2026-09-15 · **Machine:** dev box, Navisworks Manage 2026 installed at `C:\Program Files\Autodesk\Navisworks Manage 2026\` · **Method:** read-only inspection of the install (XML docs, reflection-only load of DLLs, `Roamer.exe.config`), NuGet cache inspection, and a **throw-away compile/run probe in the session scratchpad** (nothing under the repo was modified). Every ADR cites this file. Items not tested here stay `[chưa xác minh]`.

Legend: **V** = verified by command/file on this machine · **D** = read in vendor docs shipped with the install (`*.xml`) · **U** = `[chưa xác minh]`.

---

## E1 · Runtime of the host — V

- `Roamer.exe.config` lines 8–10: `<startup useLegacyV2RuntimeActivationPolicy="true"><supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8"/>`.
- No `coreclr.dll`, `hostfxr.dll`, `*.runtimeconfig.json` in the install root (486 entries listed).
- `Autodesk.Navisworks.Api.dll` `ImageRuntimeVersion = v4.0.30319`, refs `mscorlib 4.0.0.0, PresentationCore, System.Windows.Forms, System.Data` — a .NET Framework 4.x assembly (reflection-only load via Windows PowerShell 5.1).
- Installed framework: `HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\Release = 533320` (= 4.8.1); probe exe printed `.NET Framework 4.8.9181.0`.

**Consequence:** `McpShared/HPRebar.McpBridge.Core` (`<TargetFramework>net8.0</TargetFramework>`, `HPRebar.McpBridge.Core.csproj:4`) cannot load into `Roamer.exe` as built.

## E2 · Probing paths and binding redirects of Roamer.exe — V

- `Roamer.exe.config:13` `<probing privatePath="internalplugins;plugins;dependencies;stubplugins"/>`; `internalplugins/`, `plugins/`, `dependencies/` exist and are **empty**; `stubplugins/` does not exist.
- Binding redirects exist **only** for `Autodesk.Navisworks.*` 23.0.x → 23.0.0.0 (`Roamer.exe.config:16–99`). None for `System.*`.
- Framework-ish DLLs shipped in the install root (assembly version): `System.Runtime.CompilerServices.Unsafe 6.0.0.0`, `System.Threading.Tasks.Extensions 4.2.0.1`, `System.Reactive 6.0.0.0`, `Newtonsoft.Json 13.0.0.0`, `System.Windows.Interactivity 4.0.0.0`. **No** `System.Collections.Immutable`, `System.Reflection.Metadata`, `System.Text.Json`, `Microsoft.CodeAnalysis*`.

## E3 · Third-party plugin already installed — V

`%AppData%\Autodesk\Navisworks Manage 2026\Plugins\NavisworksMCPPlugin\` holds `NavisworksMCPPlugin.dll` (net4x, refs `Autodesk.Navisworks.Api 23.0.0.0`, `Autodesk.Navisworks.Clash 23.0.0.0`, `AdWindows 5.2.0.2`, `Newtonsoft.Json 13`), `Newtonsoft.Json.dll`, `en-US\NavisworksMCPRibbon.xaml` + `.name`, `Images\*.png`. Reflection-only `CustomAttributeData`:

| Type | Base | Attributes |
|---|---|---|
| `NavisworksMcp.Core.PluginApplication` | `Autodesk.Navisworks.Api.Plugins.EventWatcherPlugin` | `[Plugin("NavisworksMCPPlugin", "DMSTR", DisplayName="Navisworks MCP")]` |
| `NavisworksMcp.Core.RibbonHandler` | `Autodesk.Navisworks.Api.Plugins.CommandHandlerPlugin` | `[Plugin("NavisworksMCP","DMSTR", DisplayName=…)]`, `[Strings("NavisworksMCPRibbon.name")]`, `[RibbonLayout("NavisworksMCPRibbon.xaml")]`, `[RibbonTab("ID_NAVISWORKS_MCP", DisplayName=…, LoadForCanExecute=true)]`, `[Command("ID_MCP_SWITCH", CanToggle=true, …)]`, `[Command("ID_MCP_SETTINGS", …)]` |

**Consequences:** (a) on-machine precedent for the **folder name = assembly name** rule and for the `EventWatcherPlugin` + `CommandHandlerPlugin`/`RibbonLayout` pair; (b) this plugin is a *foreign* MCP experiment — it may open its own IPC endpoint; it must be disabled/removed before live verification so results are attributable (risk R-9 in plan).

## E4 · Plugin API surface — D (`Autodesk.Navisworks.Api.xml`, 1 035 843 bytes, shipped beside the DLL)

- `T:…Plugins.PluginAttribute`: "The standard attribute which must be applied to all plug-ins." ctor `(string name, string developerId)`: "The 4 character ADN developer code or a GUID. The combination of this and Name should make the plugin unique". Props: `Name, DeveloperId, DisplayName, ToolTip, ExtendedToolTip, Options (PluginOptions: None|SupportsControls), SupportsIsSelfEnabled`.
- `T:…Plugins.EventWatcherPlugin`: "Differs from other plugin types in that the plugin is **not delay loaded**. A typical implementation will subscribe to some API events in OnLoaded and unsubscribe in OnUnloading." `OnLoaded`: "Called once immediately after the plugin has been loaded (created)." `OnUnloading`: "…If the plugin implements IDisposable, then Dispose will be called immediately after the plugin is unloaded. Plugins are typically unloaded at application shutdown or when the end user has disabled a plugin."
- `T:…Plugins.AddInPlugin`: "A plugin that can be called either using the GUI, ApplicationAutomation or using the Automation .NET API"; `int Execute(string[])`, `CommandState CanExecute()`. `AddInPluginAttribute(AddInLocation)` optional; `AddInLocation = None|AddIn|Import|Export|Help|CurrentSelectionContextMenu|CurrentSelection2DContextMenu`.
- `T:…Plugins.CommandHandlerPlugin`: `int ExecuteCommand(string, string[])`, `CommandState CanExecuteCommand(string)`, `bool CanExecuteRibbonTab(string)`. `RibbonLayoutAttribute`, `RibbonTabAttribute`, `CommandAttribute`, `StringsAttribute` exist.
- Other plugin kinds present (not needed): `DockPanePlugin`, `ToolPlugin`, `InputPlugin`, `RenderPlugin`, `FileProtocolPlugin`, `ClashResultActionPlugin`, `HomeScreenExtensionPlugin`, `CustomPlugin`.
- `ApplicationParts.ApplicationPlugins`: `PluginRecords, FindPlugin, FindInterfaces, AddPluginAssembly, ExecuteAddInPlugin`; `PluginRecord`: `Id, Name, DeveloperId, IsEnabled, IsLoaded, HasFailedCreate, LoadPlugin, TryLoadPlugin, LoadedPlugin`.

## E5 · Threading hooks — D + reflection V

- `E:Autodesk.Navisworks.Api.Application.Idle`: "Occurs when the application finishes processing and is about to enter the idle state." Handler type `EventHandler<EventArgs>`.
- `P:Application.Gui` → `IApplicationGui` ("Will be null if the application doesn't have a Gui"); `IApplicationGui.MainWindow : System.Windows.Forms.IWin32Window` ("Get the main window of the application. Use as parent window for dialogs and message boxes.") → `Handle` available for `PostMessage(WM_NULL)` exactly as `HPAutoCad.McpBridge/MainThreadExecutor.cs:186–193` (`WakeMainThread` + `PostMessage` P/Invoke) does. `IApplicationGui` lives in namespace `Autodesk.Navisworks.Api.ApplicationParts` (not in the script imports).
- `IApplicationGui.SetRaiseIdle(Action<EventArgs>)`: "Allows Idle to be raised… Will be called with valid Action during GUI creation, and with null during GUI destruction." (internal plumbing — do not call).
- Busy signals: `Application.ProgressBeginning/ProgressEnded/ProgressUpdating/ProgressSubOperationBegan/…` events; `Document.IsActiveTransaction` ("Is there a transaction in progress ?"); `Application.IsAutomated`.
- No `IsQuiescent` equivalent exists → composite quiescence flag needed (ADR-02 §4).
- **U:** whether `Idle` fires while a Navisworks modal dialog is open or during `AppendFile`/`TestsRunTest` — probe scenario S-07/S-08 in phase 1.

## E6 · Transactions and undo — D

- `M:Document.BeginTransaction(System.String)`: "Returns a new Transaction object. This is used to batch together edits. **In the future it may support rollback.**" `displayName`: "String that may be displayed in the GUI for undoing this transaction."
- `T:Transaction`: "This is used to batch together edits. **Currently it is a requirement that you must call Commit, after doing your edits.** In the future rollback may be supported." Members: `Commit()` (throws `InvalidOperationException` "already Commited", `ObjectDisposedException`), `Dispose()`, `IsCommitted`, `IsDisposed`, `DisplayName`, `Document`. Implements only `IDisposable`.
- `M:Document.Rollback()`: "**Rolls back (undoes) the last completed transaction.** It will not be available for Redo." Throws `InvalidOperationException` "No transaction is available to Undo or there is an active transaction in progress."
- `M:Document.Undo()`: "Undoes the last completed transaction and makes it available for Redo." `Redo()`, `NextUndo`, `NextRedo`, `IsActiveTransaction`, `StartDisableUndo/EndDisableUndo`, `IsModified`.
- Events: `Document.TransactionBeginning/TransactionEnded`; `DocumentDatabase.TransactionBeginning/Begun/Committing/RollingBack` (the SQL-ish `Database` part, not the model).
- Reconciled reading (ADR-02): **no in-flight rollback; post-commit undo exists.** dryRun = `Commit()` then `Rollback()` of that very transaction. **U:** which edits actually participate (spike S-05 checks selection set + viewpoint + color override; file ops excluded by construction).

## E7 · Automation API scope — D (`Autodesk.Navisworks.Automation.xml`, 10 036 bytes — complete member list)

`NavisworksApplication`: `.ctor` ("Attempts to start up an instance of Navisworks. This will not make it visible."), `GetRunningInstance`, `TryGetRunningInstance`, `OpenFile(string, string[])`, `AppendFile`, `SaveFile`, `Print(…)`, `CreateCache`, `ExecuteAddInPlugin(string, string[])`, `AddPluginAssembly(string)`, `EnableProgress/DisableProgress`, `StayOpen`, `Visible`, `Dispose`. Summary: "Provides the same interface as Autodesk.Navisworks.Api.ApplicationParts.ApplicationAutomation but via Automation." `ApplicationAutomation` adds only `GenerateThumbnail(ByRayTrace)`.

**Consequence:** no model tree, properties, selection, search, viewpoints, clash access out of process → **ADR-01 option C eliminated**. `ExecuteAddInPlugin` remains useful for the harness (start Roamer + kick the bridge listener without a click).

## E8 · Writable surface (public .NET API) — D + reflection V

| Area | Members (verified) | Undoable via `Transaction`? |
|---|---|---|
| Selection sets | `DocumentSelectionSets.AddCopy, InsertCopy, ReplaceWithCopy, Remove, RemoveAt, Move, Clear, EditDisplayName, AddComment, EditComments, CreateSelectionSource` | U (expected yes — spike S-05) |
| Saved viewpoints | `DocumentSavedViewpoints.AddCopy, InsertCopy, ReplaceWithCopy, ReplaceFromCurrentView, Remove, Move, EditDisplayName, AddComment, EditComments, CaptureRuntimeOverrides` | U (expected yes) |
| Current selection / viewpoint | `DocumentCurrentSelection.Add, AddRange, Remove, Clear, SelectAll, CopyFrom`; `DocumentCurrentViewpoint.CopyFrom` | U |
| Appearance / state | `DocumentModels.OverridePermanentColor, OverridePermanentTransparency, OverrideTemporaryColor/Transparency, OverridePermanentTransform, ResetPermanentMaterials, ResetAllPermanentMaterials, ResetTemporaryMaterials, SetHidden, SetRequired, SetFrozen, ResetAllHidden, MakeVisible, SetModelUnitsAndTransform` | U (expected yes for permanent overrides) |
| Comments | `CreateCommentWithUniqueId`, `*.AddComment/EditComments` on saved items | U |
| Clash (`Autodesk.Navisworks.Clash.dll`, no XML shipped; reflection) | `DocumentClash.TestsData : DocumentClashTests` → `Tests, TestsAddCopy, TestsInsertCopy, TestsReplaceWithCopy, TestsRemove, TestsRemoveAt, TestsClear, TestsClearResults, TestsRunTest, TestsRunAllTests, TestsCompactTest, TestsEditDisplayName, TestsEditResultStatus/AssignedTo/ApprovedBy/Comments/Description/Priority/…, TestsImageForResult, TestsViewpointForResult, TestsSortResults`; `ClashTest {SelectionA, SelectionB, TestType, Tolerance, Status, LastRun, …}`; `ClashResult {Item1, Item2, Distance, Status, Center, BoundingBox, …}`; `DocumentClash.TryCalculateMinimumClearance`; extension `DocumentExtensions.GetClash(doc)` | U — run likely **not** undoable; S-08 |
| Clash report export | only `Autodesk.Navisworks.Api.Interop.LcClClashReport.WriteReport` (namespace `Interop`, unsupported) | — (plan: serialize results to JSON ourselves; **no** `WriteReport`) |
| TimeLiner (`Autodesk.Navisworks.Timeliner.dll`, reflection) | `DocumentTimeliner.Tasks, TasksRoot, TaskAddCopy, TaskInsertCopy, TaskReplaceWithCopy, TaskRemoveAt, TaskEdit, TaskMove, TasksClear, DataSources, DataSourceAddCopy, SimulationAppearances, Settings`; `TimelinerTask {PlannedStartDate, PlannedEndDate, ActualStartDate, …, Selection, TaskStatus}`; extension `TimelinerDocumentExtensions.GetTimeliner(doc)` | U |
| Files | `Document.AppendFile(s), MergeFile(s), RemoveFile, OpenFile, SaveFile, ExportToNwd, PublishFile, ExportAsDwf, GenerateImage, UpdateFiles, Clear`; `Try*` variants; `DocumentFileException` | **not undoable** (docs: "Failed to append file"; no undo mention; forums) → heavy op class |
| Custom properties | `Autodesk.Navisworks.ComApi.ComApiBridge.State/ToInwOaPath/ToModelItem/…` (COM `InwGUIPropertyNode2.SetUserDefined` pattern) | U — ComApi only; **not in MVP seeds** |
| Redlines | no public .NET member found (grep `Redline` in Api.xml → 0) | — out of scope |

Read-only surface confirmed: `Document.Models` (`Model.FileName, SourceFileName, Units, RootItem, Guid`), `ModelItem {DisplayName, ClassDisplayName, ClassName, PropertyCategories, Children, Descendants, Ancestors, BoundingBox, Geometry, HasGeometry, IsHidden, IsRequired, InstanceGuid, Model}`, `Search {SearchConditions, Locations, PruneBelowMatch, FindAll(doc,bool), FindFirst, FindIncremental}`, `SearchCondition.HasPropertyByDisplayName/ByName/ByCombinedName…`, `Document.SelectionSets/SavedViewpoints/CurrentSelection/CurrentViewpoint/ActiveView/Units/Title/FileName/IsModified/IsClear`.

### E8-bis · Members the seed table (phase 4) relies on — D (grep `Api.xml`, all present, counts = XML member entries)

`SelectionSet.#ctor()`, `#ctor(ModelItemCollection)`, `#ctor(Search)`, `#ctor(SelectionSet)`; `SavedViewpoint.#ctor()`, `#ctor(Viewpoint)`; `Color.FromByteRGB`; `SearchCondition.{HasPropertyByDisplayName, HasPropertyByName, HasPropertyByCombinedName, HasCategoryBy*, EqualValue, DisplayStringContains, DisplayStringWildcard, CompareWith, Negate, SameType, IgnoreStringValueCase, …}` — **no `Contains` member**; `SearchLocations` (4 values); `DocumentSelectionSets.Value`, `DocumentSavedViewpoints.Value`; `Search.FindAll(Document, bool)`, `Search.FindAll(bool)`; `Viewpoint.Position`; `DataProperty.Value`; `VariantData.ToDisplayString`, `VariantData.IsDisplayString`; `SelectionSet.HasSearch`; `DocumentModels.RootItems`; `DocumentCurrentSelection.SelectedItems`; `DocumentCurrentViewpoint.ToViewpoint`; `Document.IsClear`; `PropertyCategoryCollection.FindPropertyByDisplayName`; `SavedItem.DisplayName`; `ModelItemCollection.CopyFrom` (2 overloads).

Red-team fact-checks folded in (2026-09-15): `SearchCondition.EqualValue` takes **`VariantData`** (`Api.xml:11410`), so string args need `VariantData.FromDisplayString/FromDouble/…` (`:14225–14250`); `DocumentCurrentSelection.CopyFrom` has **no `Search` overload** — overloads are `ModelItemCollection`, `Selection`, `SelectionSourceCollection`, `IEnumerable<ModelItem>` (`:18512–18528`); `Transaction` has a public ctor `Transaction(Document, string)` (`:13238`) so `new Transaction(doc, "x")` opens a transaction without `BeginTransaction`; `Document.Database : DocumentDatabase` ("embedded database", `:2925`) converts to `Autodesk.Navisworks.Api.Data.NavisworksConnection : DbConnection` (`:17577`) and `NavisworksCommand(string commandText) : DbCommand` (`:17425`) — a SQL surface reachable from scripts unless denied; `AddInLocation.AddIn` = "Display in the Addin menu" (`:20289–20291`).

## E9 · Units — D

`P:Document.Units`: "Units for document. Apply to all geometry, properties, transforms, viewpoints, etc. in the document." `P:Model.Units`: "Units in which dimensions of this model are defined". `M:UnitConversion.ScaleFactor(Units, Units)`: "Return the scale factor between two linear units." `Units` enum: `Meters, Centimeters, Millimeters, Feet, Inches, Yards, Kilometers, Miles, Micrometers, Mils, Microinches`. → `new ScriptUnits(doc.Units.ToString(), UnitConversion.ScaleFactor(doc.Units, Units.Millimeters))` reuses `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptUnits.cs` unchanged.

## E10 · Sample models — V (sizes measured)

| File | Bytes | Use |
|---|---|---|
| `Samples\gatehouse\gatehouse_pub.nwd` | 228 696 | **default harness model** (smallest NWD) |
| `Samples\Getting Started\Architecture.nwc` / `MEP.nwc` / `Structure.nwc` | 2 593 269 / 1 042 375 / 869 277 | append + clash scenario (Architecture vs MEP) |
| `Samples\enviro-dome.nwd`, `ice stadium.nwd`, `snowmobile.nwd` | 1.66 MB / 2.55 MB / 3.64 MB | alternates |
| `Samples\bathcity\*.nwd` + `bathcity.nwf` | 5 × 2.3–5.8 MB | multi-model unit scenario (opt-in) |
| `Samples\Quantification\Autodesk_Hospital_Architectural.nwc` | 30 694 660 | **excluded** from per-run harness (heavy) |

## E10-bis · .NET Framework partial-name load — V

Windows PowerShell 5.1 (CLR 4.0.30319): `[System.Reflection.Assembly]::Load('System.Runtime')` → `FileNotFoundException` (partial names do not probe the GAC); the full name `System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a` loads. Consequence: `ScriptCompilerTests.cs:20–22` / `ScriptArgsAndAnalyzerTests.cs:78,142` (`Assembly.Load("System.Runtime")`, `Assembly.Load("System.Collections")`) cannot be linked verbatim into a net48 test project (ADR-01 §4). `xunit.v3` 3.1.0 ships `lib/net472` (`xunit.v3`, `xunit.v3.core`, `xunit.v3.runner.inproc.console`); `Polyfill 11.0.1` ships no `IReadOnlySet<T>` shim (grep `contentFiles` = 0).

## E11 · NuGet cache TFMs of every McpBridge.Core dependency — V

| Package (version in `HPRebar.McpBridge.Core.csproj`) | TFMs in `~/.nuget/packages` | net48 OK |
|---|---|---|
| Microsoft.CodeAnalysis.CSharp.Scripting 5.9.0 (+ Scripting.Common, CSharp, Common) | `net10.0`, `netstandard2.0` | ✅ via netstandard2.0 |
| CommunityToolkit.Mvvm 8.4.0 | `net8.0`, `net8.0-windows10.0.17763`, `netstandard2.0`, `netstandard2.1` | ✅ |
| Serilog 4.4.0 | `net10.0 net462 net471 net6.0 net8.0 net9.0 netstandard2.0` | ✅ |
| Serilog.Sinks.File 7.0.0 | `net462 net471 net6.0 net8.0 net9.0 netstandard2.0` | ✅ |
| System.Text.Json 10.0.12 (via Contracts) | `net10.0 net462 net8.0 net9.0 netstandard2.0` | ✅ |
| System.Collections.Immutable 10.0.1 / System.Reflection.Metadata 10.0.1 (transitive) | `net10.0 net462 net8.0 net9.0 netstandard2.0` | ✅ |
| Polyfill 11.0.1 (source-only, already used by Contracts) | content files `net461`… | ✅ |
| Microsoft.NETFramework.ReferenceAssemblies.net48 1.0.3 | present | ✅ (offline build possible) |

## E12 · Compile probe: `HPRebar.McpBridge.Core` as `net8.0;net48` — V (scratchpad copy, repo untouched)

1. Plain retarget → 30+ `CS0518 IsExternalInit` (records/`init`) + 4 × `CS0122 IReadOnlySet<T>`.
2. + `Polyfill 11.0.1` (`PrivateAssets=all`, net48 only) → **4 errors left**: `Scripting\GuardProfile.cs(62,12)`, `(65,12)`, `Scripting\AnalyzerProfile.cs(25,12)`, `(28,12)` — `IReadOnlySet<string>` public properties (`System.Collections.Immutable` for net462 carries an *internal* shim, hence "inaccessible"). Polyfill 11.0.1 ships no `IReadOnlySet` (grep of `contentFiles` = 0 hits).
3. Patching those four to a net48-visible type surfaces the **only two body-level gaps**: `Pipe\PipeListener.cs(80,60) CS0117 'PipeOptions' does not contain a definition for 'CurrentUserOnly'` and `Host\MainThreadQueue.cs(67,20)` (`Environment.TickCount64` missing → lambda inference fails). Runtime confirms: `[Enum]::GetNames([System.IO.Pipes.PipeOptions])` on 4.8.1 = `None, Asynchronous, WriteThrough`; ref-assembly `System.Core.dll` v4.8 agrees.
4. With `#if NET48` shims for those two (PipeSecurity ACL for the current user SID; `Stopwatch.GetTimestamp()*1000/Frequency` clock) → **`Build succeeded. 0 Error(s)`** for net48; output folder: 24 DLLs incl. `Microsoft.CodeAnalysis*.dll 5.9.0.0`, `System.Collections.Immutable 10.0.0.1`, `System.Reflection.Metadata 10.0.0.1`, `System.Runtime.CompilerServices.Unsafe 6.0.3.0`, `System.Memory 4.0.5.0`, `System.Text.Json 10.0.0.12`.
5. Everything else compiled **unchanged**: `ScriptGuard.cs` (`string.Contains(string, StringComparison)` ← Polyfill), `ScriptCompiler.cs` (`Convert.ToHexString`, `SHA256.HashData` ← Polyfill), `ScriptArgs.cs` (STJ 10 net462), `AuditLogger.cs`/`BridgeSettingsStore.cs` (records ← Polyfill, STJ), `RequestDispatcher.cs`, `McpBridgeHost.cs`, `McpBridgeStatusViewModel.cs`, `TypeInspector.cs`, `ScriptAnalyzer.cs`, `ScriptCache.cs`, `ScriptUnits.cs`, `AutocadInsunits.cs`. One pre-existing nullable warning (`McpBridgeStatusViewModel.cs(161,31) CS8604`).

## E13 · Runtime probe: Roslyn scripting on .NET Framework 4.8 with the net48 Core — V

Console `Net48RunProbe` (net48, `AutoGenerateBindingRedirects=false` to mimic a plugin that cannot edit `Roamer.exe.config`), referencing the net48 Core build:

- **Without** an `AssemblyResolve` handler → `System.IO.FileLoadException: Could not load file or assembly 'System.Collections.Immutable, Version=10.0.0.0…'. The located assembly's manifest definition does not match the assembly reference.` Cause (reflection): `Microsoft.CodeAnalysis.dll` references `System.Collections.Immutable 10.0.0.0`, `System.Reflection.Metadata 10.0.0.0`, `System.Runtime.CompilerServices.Unsafe 6.0.0.0`, `System.Memory 4.0.2.0`, `System.Threading.Tasks.Extensions 4.2.1.0`, `System.Buffers 4.0.2.0`, `netstandard 2.0.0.0`; the restored files are `10.0.0.1 / 10.0.0.1 / 6.0.3.0 / 4.0.5.0 / 4.2.4.0 / 4.0.5.0` — .NET Framework binds by exact version.
- **With** a same-simple-name-any-version `AppDomain.AssemblyResolve` handler over the plugin folder → run succeeded:
  ```
  resolve System.Collections.Immutable, Version=10.0.0.0 -> System.Collections.Immutable.dll 10.0.0.1
  CLR 4.0.30319.42000  Framework .NET Framework 4.8.9181.0
  resolve System.Runtime.CompilerServices.Unsafe, Version=6.0.0.0 -> ...Unsafe.dll 6.0.3.0
  resolve System.Memory, Version=4.0.2.0 -> System.Memory.dll 4.0.5.0
  guard diagnostics: 1 -> System.IO.File.ReadAllText is not allowed in Revit scripts.
  resolve System.Reflection.Metadata, Version=10.0.0.0 -> System.Reflection.Metadata.dll 10.0.0.1
  script result = 31 (expect 31); compiled=1 cacheHit2=True
  loaded: Microsoft.CodeAnalysis.Scripting 5.9.0.0, System.Collections.Immutable 10.0.0.1, Microsoft.CodeAnalysis 5.9.0.0, Microsoft.CodeAnalysis.CSharp 5.9.0.0, System.Reflection.Metadata 10.0.0.1, Microsoft.CodeAnalysis.CSharp.Scripting 5.9.0.0
  exit=0
  ```
  i.e. `ScriptGuard.Check`, `ScriptCompiler.GetOrCompile` (with `InteractiveAssemblyLoader` — desktop implementation), `Script<object>.RunAsync(globals)`, and the script cache all work on the .NET Framework 4.8 runtime.
- Inside `Roamer.exe` the same four names will need resolving; Roamer's own `System.Runtime.CompilerServices.Unsafe 6.0.0.0` (E2) matches Roslyn's request exactly and may bind first — harmless either way (stateless helpers). **U:** behaviour inside Roamer itself → spike S-02 (`ScriptingSelfCheck` inside the plugin).

## E14 · Repo facts reused verbatim — V

- `PipeNaming.For(host, version)` (`McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs:26–37`) already yields `hp{host}-mcp-{version}` for an unknown host → `PipeNaming.For("navis", 2026) == "hpnavis-mcp-2026"` **without any Contracts change**; a `NavisHost` constant is still added for symmetry.
- `RequestDispatcher` dispatches on `JsonRpcMethods.Suffix(method)` (`JsonRpcMethods.cs:48–55`), so `navis.execute` is accepted once the bridge is built with `methodPrefix = "navis."` (`McpBridgeHost` ctor, `Host/McpBridgeHost.cs:36–52`).
- `ContextService.Shape` (`McpShared/HPRebar.Mcp.Server.Core/Services/ContextService.cs:52–65`) already strips Revit-only fields for **every** non-Revit `HostId` → Navis context is clean with zero change; `ContextResult.Autocad` shows the additive pattern for a `Navis` block (`Contracts/Messages/ContextMessages.cs:24`).
- 120 s is hard-coded in **three** engine sites, not one: `Services/ExecuteCodeService.cs:24,59`, `Registry/ToolManager.cs:173` (`Math.Clamp(record.TimeoutSeconds, 5, 120)` on every `run_tool`/`test_tool`), `Registry/ToolValidator.cs:52` (`timeoutSeconds must be between 5 and 120` at propose); plus the constant description text `Tools/Registry/ToolLifecycleTools.cs:34` and the bridge clamp `AutocadScriptRunner.cs:65`. The heavy-operation timeout of ADR-04 therefore threads an additive `HostProfile.MaxTimeoutSeconds` (default 120) through the three engine sites.
- `RegistryJson.Options = new(JsonSerializerDefaults.Web){ WriteIndented, WhenWritingNull, snake_case enums }` (`Registry/Model/RegistryJson.cs:13–18`): unknown keys are skipped on read and **dropped on rewrite** (`ToolLibraryStore.Write` → `RegistryJson.Serialize(record)`), so a `heavy` marker must be a modelled property — `ToolRecord.Tags : List<string>` (`ToolRecord.cs:34`) already is.
- `BridgeSettingsStore.Options = new JsonSerializerOptions { WriteIndented = true }` (`Model/BridgeSettingsStore.cs:15`, no ignore condition); `Save` serialises a full `BridgeSettings` projection (`:62–75`), `Load` only forces `ExecutionEnabled = false` (`:36–43`) → any new `BridgeSettings` property **is** persisted and **is** honoured on load. `RequireLocalApproval` (`BridgeSettings.cs:16`) has no reader anywhere in the repo.
- Client side of the pipe: `Services/NdjsonPipeTransport.cs:47–50` opens `NamedPipeClientStream` with `PipeOptions.CurrentUserOnly`, which in .NET validates the **owner SID** of the server pipe against `WindowsIdentity.GetCurrent().Owner` → the net48 `PipeSecurity` shim must `SetOwner(identity.Owner)`.
- `RequestDispatcher.cs:94–95` dispatches `cancel` to `_executor.Cancel()` with no request id; `RevitBridgeClient.cs:81–99` sends `cancel` on **every** timeout including the 15 s `ContextService` one (`ContextService.cs:20`); `RequestDispatcher.cs:136` refuses a second `execute` while `IsBusy` but `context` is not gated (`MainThreadExecutor.cs:141–148` enqueues unconditionally).
- `PipeRoundTripTests.cs:3–4,25,33` imports `HPRebar.Mcp.Server.Models/Services` (net10 Server.Core) — not linkable into a net48 test project. Attribute counts: `MainThreadQueueTests` 12, `ScriptGuardTests` 3 (+23 InlineData), `ScriptCompilerTests` 8, `ScriptArgsAndAnalyzerTests` 9, `PipeRoundTripTests` 12.
- `ScriptGuard.cs:104` emits one fixed message for every `profile.DeniedMembers` hit — a heavy-specific message must come from a host-side pre-pass.
- `MainThreadQueue` (`Host/MainThreadQueue.cs`) is host-neutral (isQuiescent + wake + clock injected) — reusable for Navisworks as is (net48 shim only for the default clock).
- `AutocadHostProfile.cs`, `AutocadToolsOverPipeTests.cs`, `SeedLibraryTests.cs`, `tools/harness/*` are the templates to mirror (paths in the phase files).

## E15 · Plugin loads in Roamer.exe, no security prompt — V (2026-09-15, spike S-01/S-02, 6+ starts)

- Log `HPNavis MCP bridge starting from %AppData%\Autodesk\Navisworks Manage 2026\Plugins\HPNavis.McpBridge; Navisworks runtime 23.0 → 2026; CLR 4.0.30319.42000 (.NET Framework 4.8.9181.0)`; `MCP scripting self-check OK in 2762 ms (5 assemblies resolved by the plugin)`. No dialog of any kind for the unsigned DLL.
- Resolver traffic: `Serilog 4.2.0.0→4.4.0.0` (Serilog.Sinks.File 7 references 4.2), `System.Memory 4.0.1.2/4.0.2.0→4.0.5.0`, `System.Collections.Immutable 10.0.0.0→10.0.0.1`, `System.Reflection.Metadata 10.0.0.0→10.0.0.1`; 85/85 lines `requested by <none>` with `NavisworksMCPPlugin` enabled.

## E16 · Discovery reflects every type before the plugin runs — V (probe + Roamer)

- `[Reflection.Assembly]::LoadFrom(plugin).GetTypes()` in Windows PowerShell (after `Add-Type` of the API DLLs) reproduced Roamer's "The Plugin was not found": `ReflectionTypeLoadException … System.Text.Json, Version=10.0.0.0`. `System.Text.Json 10.0.12` package: `lib/netstandard2.0` = assembly **10.0.0.0**, `lib/net462` = **10.0.0.12** (`[Reflection.AssemblyName]::GetAssemblyName`). After `HPRebar.Mcp.Contracts` → `netstandard2.0;net48`: 400 types, 2 `[Plugin]` types, Roamer loads the plugin.

## E17 · `Application.Idle` is silent under a native modal — V (spike S-07)

- Open dialog (Ctrl+O) up → `navis.context` never answered until the dialog closed (first attempt); with `MainThreadQueue(expireWithoutTicks: true)`: `work 'context' refused: Navisworks not quiescent within 8s`, `-32002` at 8 s per request. Main window `IsEnabled=false` while the dialog is up (UIA), so `!IsWindowEnabled(main)` detects it; `GW_ENABLEDPOPUP` also matched the bridge's own modeless window → dropped.

## E18 · Progress events, clash API timing, undo of `CurrentSelection` — V (spike S-05c/S-08)

- Loading `gatehouse_pub.nwd`: `ProgressBeginning: Working…` → `SubOperationBegan` ×n (depth up to 3) → `ProgressEnded` → depth 0 within ~1 s.
- `ClashTest{Hard, Tolerance 0}` between 29/30 root children + `TestsRunTest` → 852 results, `Status Complete`, 61–62 ms; the `ClashTest` wrapper held across `TestsRunTest` throws `ObjectDisposedException (WeakRef)` on `.Children` afterwards — re-resolve from `TestsData.Tests`.
- `doc.CurrentSelection.Clear(); Add(root)` inside `BeginTransaction("MCP: spike current")` → `NextUndo == "MCP: spike current"` (undoable → W1).

## E19 · Automation-started Roamer dies on this machine — V (control run without our plugin)

- `New-Object Autodesk.Navisworks.Api.Automation.NavisworksApplication` (Windows PowerShell 5.1): plugin logs `ready`, then the process is gone within ~15 s, invisible to `Get-Process Roamer`; `OpenFile` → `0x800706BE` (RPC call failed) / `0x800706BA` (RPC server unavailable). Same with `Plugins\HPNavis.McpBridge` renamed away → not caused by the plugin. `Roamer.exe "<model>"` started directly stays up (16–26 s to a titled main window). `ExecuteAddInPlugin` therefore unverified.

## E20 · net10 stdio server ↔ net48 bridge over the named pipe — V (phase 3 smoke, 2026-09-15)

- `HPNavis.Mcp.Server.exe` (published net10, `NdjsonPipeTransport` with `PipeOptions.CurrentUserOnly`) connected to the plugin's `NamedPipeServerStream` created with `PipeSecurity.SetOwner(current user) + FullControl` on .NET Framework 4.8: `MCP server connected on hpnavis-mcp-2026`, `get_navis_context` answered in 0.20 s, no `UnauthorizedAccessException` — the owner-SID check the .NET client performs is satisfied by the explicit owner the net48 listener sets. Both processes unelevated.
- `serverInfo.name` "HPNavis MCP"; `tools/list` 12 names; heavy refusal (`HEAVY`) and dry-run (`rolledBack=true`, `changed.added=1`) survive the server's result mapping unchanged.

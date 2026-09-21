# Handoff Report: UI, Theming, Ribbon, Loader & ILRepack Architecture for Smart Plot Pro in HPAutoCad

## 1. Observation

### 1.1 Existing WPF MVVM and MaterialDesign Setup in `HPAutoCad`
- **Project file & dependencies** (`HPAutoCad/HPAutoCad.csproj` lines 7-31):
  Targets `net8.0-windows` with `UseWPF=true`, `EnableDynamicLoading=true`, referencing:
  - `AutoCAD.NET` (25.1.0, `ExcludeAssets="runtime" PrivateAssets="all"`)
  - `CommunityToolkit.Mvvm` (8.4.0)
  - `MaterialDesignThemes` (5.3.2)
  - `ILRepack` (2.0.46)
- **Theme configuration & assembly attribute** (`HPAutoCad/Resources/Themes/ThemeInfo.cs` line 6):
  `[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]`
  Ensures WPF resolves generic default styles (e.g., `Card`, `PackIcon`) from `HPAutoCad.dll` after ILRepack merge.
- **Theme dictionaries** (`HPAutoCad/Resources/Themes/`):
  - `MaterialBridge.xaml` (lines 12-17): Merges `<md:CustomColorTheme BaseTheme="Dark" PrimaryColor="#0696D7" SecondaryColor="#E0641E"/>` and `MaterialDesign2.Defaults.xaml`. Sets `<FontFamily x:Key="MaterialDesignFont">Segoe UI</FontFamily>`.
  - `Theme.xaml` (lines 1-104): Merges `MaterialBridge.xaml`. Defines spacing tokens (`Spacing.XS`=4, `Spacing.S`=8, `Spacing.M`=16), row/section margins, text styles (`TextBlock`, `Text.Muted`, `Text.Title`, `Text.Section`), button styles (`Button` outlined, `Button.Primary` raised), `Expander`, and `DataGrid` styles with `{DynamicResource Brush.*}` bindings.
  - `ThemeDark.xaml` & `ThemeLight.xaml`: Define color tokens for dark (`COLORTHEME=0`, brightness ~55, `Brush.Background` = `#2B2B2B`) and light (`COLORTHEME=1`, brightness ~245, `Brush.Background` = `#F3F3F3`). Also define `Brush.Surface`, `Brush.Input`, `Brush.Border`, `Brush.Text`, `Brush.TextMuted`, `Brush.Accent` (`#0696D7`), `Brush.AccentText`, `Brush.Danger`, `Brush.Warning`, `Brush.Success`, `Brush.Foreground.Primary`, `Brush.Foreground.Secondary`, `Brush.SurfaceElevated`.
- **Dynamic theme switching** (`HPAutoCad/Resources/Themes/AutocadHostTheme.cs` lines 10-39):
  Singleton `AutocadHostTheme.Instance` implements `IHostTheme`. Listens to `AcadApp.SystemVariableChanged` for `"COLORTHEME"` (0 = dark, 1 = light).
- **Theme bridge attachment** (`HPAutoCad/Resources/Themes/MaterialThemeBridge.cs` lines 32-76):
  `MaterialThemeBridge.Attach(window, AutocadHostTheme.Instance)` applies the palette dynamically, updates top-level merged dictionaries with a new overlay dictionary (`Theme.Create(dark ? BaseTheme.Dark : BaseTheme.Light, ...)`), updates on `host.Changed`, and unbinds on `window.Closed`. Uses `AssemblyLoadContext.GetLoadContext(typeof(MaterialThemeBridge).Assembly)?.EnterContextualReflection()`.
- **Dialog initialization pattern** (`HPAutoCad/HPGeoLink/View/GeoExportWindow.xaml.cs` lines 15-22):
  ```csharp
  using (AssemblyLoadContext.GetLoadContext(typeof(GeoExportWindow).Assembly)!.EnterContextualReflection())
  {
      Resources.MergedDictionaries.Add(ThemeResources.Styles());
      InitializeComponent();
      MaterialThemeBridge.Attach(this, AutocadHostTheme.Instance);
  }
  ```

### 1.2 Modeless Window Architecture in AutoCAD
- **Existing modeless window usage** (`HPAutoCad.McpBridge/BridgeEntry.cs` lines 121-145):
  ```csharp
  private static Window? _window;
  ...
  if (_window is not null)
  {
      _window.Activate();
      return string.Empty;
  }
  ...
  view.Closed += (_, _) => { viewModel.Detach(); _window = null; };
  _window = view;
  AcadApp.ShowModelessWindow(view);
  ```
- **Active document locking requirement**:
  In AutoCAD .NET, modeless windows run outside AutoCAD's document transaction lock. Any interaction with `MdiActiveDocument.Database` (reading block definitions, scanning frames, querying layers, or picking entities) requires `using (doc.LockDocument())`. Without this, AutoCAD throws `DocumentNotLockedException`.

### 1.3 Command Registration and Loader Architecture
- **Autodesk autoloader manifest** (`HPAutoCad.Loader/Bundle/PackageContents.xml` lines 17-41):
  AutoCAD 2026 loads only two component entries on startup:
  1. `Contents/HPAutoCad.McpBridge.Loader.dll`
  2. `Contents/HPAutoCad.Loader.dll`
  `Contents/App/HPAutoCad.dll` is **NOT** loaded directly by AutoCAD's native autoloader.
- **Loader assembly & command discovery** (`HPAutoCad.Loader/HPAutoCadLoaderApplication.cs` lines 13-15):
  `[assembly: ExtensionApplication(typeof(HPAutoCadLoaderApplication))]`
  `[assembly: CommandClass(typeof(HPGeoCommands))]`
  AutoCAD discovers `[CommandMethod]` attributes only on assemblies loaded in the Default ALC.
- **ALC isolation & delegate forwarding** (`HPAutoCad.Loader/AppLoadContext.cs` & `HPGeoCommands.cs`):
  `HPAutoCadLoaderApplication.Initialize()` creates an `AppLoadContext` on `Contents\App\HPAutoCad.dll`, invokes `HPAutoCad.Entry.Start(appDir, log)`, and receives `IReadOnlyDictionary<string, Delegate> App`.
  Commands in `HPAutoCad.Loader` (`HPGeoCommands.cs`) forward execution via `HPGeoCommands.Invoke("key", "COMMAND_NAME")`.
- **Entry points dictionary** (`HPAutoCad/Entry.cs` lines 24-34):
  `HPAutoCad.Entry.Start` returns a dictionary mapping strings to `Action` delegates (`["dialog"] = new Action(HPGeoDialogCommand.Run)`, etc.).

### 1.4 Ribbon Architecture
- **Shared ribbon tab** (`HPAutoCad.Loader/Ribbon/HPGeoLinkRibbonTab.cs` lines 20-76):
  - `TabId = "HPAUTOCAD_MCP_TAB"`, `TabTitle = "HPAutoCad"`.
  - Checks if `tab = ComponentManager.Ribbon.FindTab(TabId)` exists. If not, creates it.
  - Finds or adds its own panel: `PanelId = "HPGEOLINK_PANEL"`, `PanelTitle = "HPGeoLink"`.
  - Re-themes dynamic icons when `COLORTHEME` flips without dropping the shared tab.
  - Rebuilds panels on workspace change (`WSCURRENT`).
  - Handles late ribbon initialization via `ComponentManager.ItemInitialized`.
- **Vector icon drawing** (`HPAutoCad.Loader/Ribbon/RibbonIcons.cs` lines 11-70):
  - All icons are resolution-independent vector graphics drawn via `DrawingGroup` with `Geometry.Parse` in code within a 32×32 box.
  - Zero external PNG or SVG asset files.
  - Dynamic ink: `#E6E6E6` for dark (`COLORTHEME=0`), `#3C3C3C` for light (`COLORTHEME=1`).
  - Accent brush: `#0696D7` (HP Blue).
  - All icons are frozen via `freezable.Freeze()`.

### 1.5 ILRepack and Packaging Target
- **RepackMaterialDesign Target** (`HPAutoCad/HPAutoCad.csproj` lines 61-69):
  ```xml
  <Target Name="RepackMaterialDesign" AfterTargets="CopyFilesToOutputDirectory" Condition="Exists('$(OutDir)MaterialDesignThemes.Wpf.dll')">
      <PropertyGroup>
          <_RepackExe>$(PkgILRepack)\tools\ILRepack.exe</_RepackExe>
          <_RepackLib>@(ReferencePath->'%(RelativeDir)'->Distinct()->'/lib:&quot;%(Identity) &quot;', ' ')</_RepackLib>
      </PropertyGroup>
      <Exec Command="&quot;$(_RepackExe)&quot; /union /parallel /noRepackRes $(_RepackLib) /out:&quot;$(OutDir)$(AssemblyName).dll&quot; &quot;@(IntermediateAssembly->'%(FullPath)')&quot; &quot;$(OutDir)MaterialDesignThemes.Wpf.dll&quot; &quot;$(OutDir)MaterialDesignColors.dll&quot; &quot;$(OutDir)Microsoft.Xaml.Behaviors.dll&quot;"/>
      <Delete Files="$(OutDir)MaterialDesignThemes.Wpf.dll;$(OutDir)MaterialDesignColors.dll;$(OutDir)Microsoft.Xaml.Behaviors.dll;$(OutDir)MaterialDesignThemes.Wpf.xml"/>
      <Message Importance="high" Text="RepackMaterialDesign: toolkit merged into $(AssemblyName).dll"/>
  </Target>
  ```
- **Deployment target** (`HPAutoCad.Loader/HPAutoCad.Loader.csproj` lines 43-81):
  Copies all files from `AppOutDir` (`HPAutoCad/bin/$(Configuration)/$(TargetFramework)/`) into `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\Contents\App\`.
- **NuGet Search for PdfSharp**:
  `dotnet package search PdfSharp --exact-match` confirms `PDFsharp` 6.1.1 is published and targets .NET 6/.NET 8.

---

## 2. Logic Chain

### 2.1 Why `MaterialDesignThemes` is repacked but `PdfSharp` must NOT be repacked
1. *Observation 1.1 & 1.5*: WPF resolves BAML resources and type converters across ALCs using simple assembly names. Spike S0-B proved that when multiple plugins load loose `MaterialDesignThemes.Wpf.dll`, WPF binds BAML types across ALC boundaries to whichever copy loaded last, causing fatal crashes.
2. *Observation 1.5*: The `RepackMaterialDesign` target merges `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, and `Microsoft.Xaml.Behaviors.dll` directly into `HPAutoCad.dll` and deletes the loose DLLs.
3. *Observation 1.3 & 1.5*: In contrast, `PdfSharp.dll` contains pure PDF rendering logic and document object structures. It has **no WPF BAML templates or XAML resources**.
4. *Observation 1.3*: `HPAutoCad.Loader/AppLoadContext.cs` implements an isolated `AssemblyLoadContext` with `AssemblyDependencyResolver` reading `HPAutoCad.deps.json`. When `HPAutoCad.dll` references `PdfSharp`, `AppLoadContext.Load(assemblyName)` intercepts the request and loads `Contents\App\PdfSharp.dll` directly into `AppLoadContext`.
5. *Deduction*: `PdfSharp.dll` is already 100% isolated by `AppLoadContext`. Attempting to ILRepack `PdfSharp.dll` into `HPAutoCad.dll` would add unnecessary IL complexity, risk breaking its internal reflection/metadata, and provide zero architectural benefit. Therefore, `PDFsharp` (v6.1.1) should remain a normal `PackageReference` in `HPAutoCad.csproj` and sit as a loose DLL in `Contents\App\`.

### 2.2 Why Commands must be declared in `HPAutoCad.Loader` and delegated to `HPAutoCad`
1. *Observation 1.3*: AutoCAD's runtime autoloader parses `PackageContents.xml` and loads only `Contents/HPAutoCad.Loader.dll` and `Contents/HPAutoCad.McpBridge.Loader.dll` into the Default ALC.
2. *Observation 1.3*: AutoCAD's command discovery engine scans for `[assembly: CommandClass]` and `[CommandMethod]` only in assemblies loaded by the host autoloader. It cannot inspect assemblies loaded privately inside child `AssemblyLoadContext` instances like `Contents\App\HPAutoCad.dll`.
3. *Deduction*:
   - Commands `HPSMARTPLOT` and alias `HPLOT` must be defined on a command class in `HPAutoCad.Loader` (e.g. `SmartPlotCommands.cs`) decorated with `[assembly: CommandClass(typeof(SmartPlotCommands))]`.
   - The command methods invoke the delegate via `HPGeoCommands.Invoke("smartplot", "HPSMARTPLOT")`.
   - `HPAutoCad/Entry.cs` exposes `["smartplot"] = new Action(SmartPlotCommand.Run)` which launches `SmartPlotWindow.ShowWindow()`.

### 2.3 Modeless Window Lifecycle, Viewport Interaction & Document Locking
1. *Observation 1.2*: Modeless windows in AutoCAD are displayed via `AcadApp.ShowModelessWindow(window)`.
2. *Observation 1.2*: When the window is modeless, the user can pan and zoom in the AutoCAD drawing without closing the dialog.
3. *Observation 1.2*: However, because the dialog runs on the UI thread without holding the active document's command lock:
   - Any database access (e.g. scanning block definitions, reading layers, or querying entities) must explicitly acquire `using (doc.LockDocument())`.
   - When the user clicks "Pick Frame" (picking a block or polyline from the drawing), the dialog must temporarily minimize or hide (`window.WindowState = WindowState.Minimized` or `window.Hide()`), focus must shift to the editor, an entity prompt (`ed.GetEntity(...)` or `ed.GetCorner(...)`) must be executed inside `using (doc.LockDocument())`, and the window must then be restored (`window.WindowState = WindowState.Normal; window.Activate()`).
4. *Observation 1.2*: Modeless dialogs must enforce single-instance semantics (`static SmartPlotWindow? _instance`) to prevent users from opening multiple concurrent plot windows.

### 2.4 UI Architecture: Structure, Cards & ViewModel
1. *Observation 1.1*: All windows in `HPAutoCad` use `Resources.MergedDictionaries.Add(ThemeResources.Styles())`, `InitializeComponent()`, and `MaterialThemeBridge.Attach(this, AutocadHostTheme.Instance)`.
2. *Authoritative Requirements (R4)*:
   - Window: `SmartPlotWindow.xaml` (960×680 min 840×560).
   - Header: Title, Active Document Name, Status badge.
   - Progress bar: Visible during scan/plot, displays `X / Y sheets: {CurrentSheetName}`, supports cooperative cancellation via `CancellationTokenSource`.
   - TabControl:
     1. **Plot Tab**:
        - Frame Source Card: Block / Layer / Layout modes, pick button, attribute tag selector.
        - Printer & Paper Card: PC3/System printer picker, canonical media size selector.
        - Plot Style Card: CTB/STB selector.
        - Orientation & Scale Card: Auto / Portrait / Landscape, Fit to Paper, Center Plot.
        - Output & Naming Card: Output folder picker, SingleFiles vs MergedPdf radio buttons, naming template tokens (`{Prefix}_{Layout}_{Title}_{SheetNo}`).
        - Discovered Frames DataGrid: Checkbox, Order, Layout, Title, SheetNo, Paper Size, Status. Auto-sort and manual reorder controls.
        - Action Footer: Plot Selected, Plot All, Cancel, Close.
     2. **Presets Tab**: Save/Load/Delete presets stored at `%AppData%\HPAutoCad\SmartPlot\presets.json`.
     3. **Settings Tab**: Spatial overlap tolerance band (mm), default output folder, CTB default.
     4. **About Tab**: Version, HPAutoCad ecosystem integration info.
   - ViewModel: `SmartPlotViewModel : ObservableObject` (`CommunityToolkit.Mvvm`), with `[ObservableProperty]` and `[RelayCommand]`.

### 2.5 Ribbon Integration
1. *Observation 1.4*: Tab `HPAUTOCAD_MCP_TAB` ("HPAutoCad") is shared.
2. *Authoritative Requirements (R5)*: A new panel `PanelId = "HPPLOT_PANEL"`, `PanelTitle = "Plot"` must be added.
3. *Observation 1.4*: A new vector icon `Plot` must be added to `RibbonIcons.cs`. It must be resolution-independent (32×32 box), draw a plotter/printer with a paper sheet and accent plot line, and dynamically adapt its ink to dark/light themes.

---

## 3. Detailed Architectural Blueprint

### 3.1 Directory and File Structure
All new files for Smart Plot Pro adhere strictly to the repository conventions:

```
HPAutoCad/
├── HPAutoCad/
│   ├── SmartPlot/
│   │   ├── Commands/
│   │   │   └── SmartPlotCommand.cs          // Launches SmartPlotWindow.ShowWindow()
│   │   ├── UI/
│   │   │   ├── SmartPlotWindow.xaml         // Modeless WPF Window (MaterialDesign + Theme)
│   │   │   ├── SmartPlotWindow.xaml.cs      // Code-behind: ALC reflection, Theme attach
│   │   │   ├── SmartPlotViewModel.cs        // Core MVVM ViewModel (CommunityToolkit.Mvvm)
│   │   │   ├── SmartPlotViewModel.Commands.cs // Relay commands: Plot, Scan, Pick, Presets
│   │   │   ├── Models/
│   │   │   │   └── PlotItemViewModel.cs     // DataGrid row item wrapper
│   │   │   └── Converters/
│   │   │       └── SmartPlotConverters.cs   // BoolToVisibility, EnumToBoolean converters
│   │   ├── Cad/                             // (Engine Layer, handled by peer)
│   │   └── Pdf/                             // (PdfSharp Layer, handled by peer)
│   └── Entry.cs                             // Updated: registers ["smartplot"] delegate
│
├── HPAutoCad.Loader/
│   ├── SmartPlotCommands.cs                 // [CommandMethod("HPSMARTPLOT")], [CommandMethod("HPLOT")]
│   ├── Ribbon/
│   │   ├── SmartPlotRibbonPanel.cs          // Adds "Plot" panel to HPAUTOCAD_MCP_TAB
│   │   └── RibbonIcons.cs                   // Updated: adds vector Plot icon
│   └── HPAutoCadLoaderApplication.cs        // Updated: [assembly: CommandClass(typeof(SmartPlotCommands))], installs panel
```

### 3.2 Modeless Window & Pick Frame Implementation Specification
```csharp
namespace HPAutoCad.SmartPlot.UI;

public partial class SmartPlotWindow : Window
{
    private static SmartPlotWindow? _instance;

    public static void ShowWindow()
    {
        if (_instance is not null)
        {
            if (_instance.WindowState == WindowState.Minimized)
                _instance.WindowState = WindowState.Normal;
            _instance.Activate();
            return;
        }

        var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
        if (doc is null) return;

        var vm = new SmartPlotViewModel(doc);
        var window = new SmartPlotWindow(vm);
        _instance = window;

        window.Closed += (_, _) =>
        {
            vm.Dispose();
            _instance = null;
        };

        Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowModelessWindow(window);
    }

    public SmartPlotWindow(SmartPlotViewModel viewModel)
    {
        using (AssemblyLoadContext.GetLoadContext(typeof(SmartPlotWindow).Assembly)!.EnterContextualReflection())
        {
            Resources.MergedDictionaries.Add(ThemeResources.Styles());
            InitializeComponent();
            MaterialThemeBridge.Attach(this, AutocadHostTheme.Instance);
        }
        DataContext = viewModel;
        viewModel.PickFrameRequested += OnPickFrameRequested;
        viewModel.CloseRequested += Close;
    }

    private void OnPickFrameRequested(Action<ObjectId> onPicked)
    {
        var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
        if (doc is null) return;

        WindowState = WindowState.Minimized;
        try
        {
            using (doc.LockDocument())
            {
                var ed = doc.Editor;
                var peo = new PromptEntityOptions("\nChọn khung bản vẽ (Block hoặc Polyline): ");
                peo.SetRejectMessage("\nĐối tượng phải là BlockReference hoặc Polyline khép kín.");
                peo.AddAllowedClass(typeof(BlockReference), true);
                peo.AddAllowedClass(typeof(Polyline), true);
                peo.AddAllowedClass(typeof(Polyline2d), true);

                var per = ed.GetEntity(peo);
                if (per.Status == PromptStatus.OK)
                {
                    onPicked(per.ObjectId);
                }
            }
        }
        finally
        {
            WindowState = WindowState.Normal;
            Activate();
        }
    }
}
```

### 3.3 Ribbon "Plot" Panel & Vector Icon Specification
In `HPAutoCad.Loader/Ribbon/SmartPlotRibbonPanel.cs`:
```csharp
internal static class SmartPlotRibbonPanel
{
    public const string PanelId = "HPPLOT_PANEL";
    public const string PanelTitle = "Plot";

    public static RibbonPanel BuildPanel(bool isAvailable, RibbonIcons icons)
    {
        var plotButton = new RibbonButton
        {
            Id = "HPSMARTPLOT_BUTTON",
            Text = "Smart Plot\nPro",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = System.Windows.Controls.Orientation.Vertical,
            Image = icons.Plot,
            LargeImage = icons.Plot,
            CommandHandler = new RibbonCommandHandler("HPSMARTPLOT_BUTTON", () => RunCommand("HPSMARTPLOT")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "Smart Plot Pro (HPSMARTPLOT)",
                Content = isAvailable
                    ? "Tự động quét khung tên, nhận diện khổ giấy và in hàng loạt ra PDF (đơn lẻ hoặc gộp file qua PdfSharp)."
                    : "HPAutoCad chưa khởi động.",
                Command = "HPSMARTPLOT",
                IsHelpEnabled = false,
            }
        };

        var panel = new RibbonPanel { Source = new RibbonPanelSource { Id = PanelId, Title = PanelTitle } };
        panel.Source.Items.Add(plotButton);
        return panel;
    }

    private static void RunCommand(string command)
    {
        var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
        doc?.SendStringToExecute("_." + command + " ", true, false, false);
    }
}
```

In `HPAutoCad.Loader/Ribbon/RibbonIcons.cs`:
```csharp
// 6. Plot: Printer chassis with outbound paper sheet and laser/plot accent line
Plot = Glyph(
    (ink, "M4,16 H28 V26 H4 Z M6,18 H26 V24 H6 Z"),                         // Printer base body
    (ink, "M8,6 H20 L24,10 V16 H8 Z M20,6 V10 H24"),                        // Paper sheet feeding out with corner fold
    (Accent, "M10,12 H18 V14 H10 Z M10,20 H22 V22 H10 Z"),                  // Document content line + output feed line
    (Accent, "M22,17 A1.5,1.5 0 1 0 25,17 A1.5,1.5 0 1 0 22,17 Z")          // Status LED indicator
);
```

### 3.4 Packaging & ILRepack Strategy
1. **`HPAutoCad/HPAutoCad.csproj`**:
   - Add `<PackageReference Include="PDFsharp" Version="6.1.1" />`.
   - Keep `RepackMaterialDesign` intact (merging only `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, `Microsoft.Xaml.Behaviors.dll`).
   - Do **NOT** add `PdfSharp.dll` to `RepackMaterialDesign`.
2. **Build and Deployment Output**:
   - `HPAutoCad/bin/Debug/net8.0-windows/`: Contains `HPAutoCad.dll` (with MaterialDesign repacked inside), `PdfSharp.dll`, `CommunityToolkit.Mvvm.dll`, `HPAutoCad.deps.json`.
   - `HPAutoCad.bundle/Contents/App/`: `DeployBundle` copies `PdfSharp.dll` and dependencies alongside `HPAutoCad.dll`.
   - `HPAutoCad.Loader/AppLoadContext.cs`: Resolves `PdfSharp.dll` from `Contents/App/` inside `AppLoadContext`, ensuring 100% isolation from host or other add-ins.

---

## 4. Caveats
1. **Modeless Threading & DocumentLock**:
   AutoCAD commands run on AutoCAD's main STA thread. Modeless WPF windows also run on the main STA thread (sharing the dispatcher), but events fired from WPF controls (such as button clicks) execute outside AutoCAD's active command transaction context. Any database modification or editor prompt MUST be wrapped in `using (doc.LockDocument())`.
2. **AutoCAD Active Document Availability**:
   In AutoCAD, if all documents are closed (zero-document state), `doc` is null. The command should be marked `CommandFlags.Modal | CommandFlags.Session` so it can be invoked safely, but the UI should gracefully disable the "Pick Frame" and "Scan Frames" buttons until a valid document is active.
3. **Dynamic Theme Switching on Modeless Window**:
   When `COLORTHEME` changes in AutoCAD, `AutocadHostTheme.Instance.Changed` fires from AutoCAD's command context. `MaterialThemeBridge.cs` line 36 already dispatches the theme re-apply via `window.Dispatcher.BeginInvoke()`, preventing cross-thread exceptions.
4. **Mirror Test Safety**:
   `HPCivil3d.McpBridge.Tests` verifies mirror tokens between `HPAutoCad` and `HPCivil3d`. Since our changes are scoped to `HPAutoCad` (Add-in/Loader) and do NOT touch the 24 mirrored files in `HPAutoCad.McpBridge` or `HPAutoCad/tools/mirror-tokens.json`, mirror tests will remain 100% green.

---

## 5. Conclusion
- The WPF MVVM and MaterialDesign architecture in `HPAutoCad` is robust, production-tested, and ready for `SmartPlotWindow`.
- Modeless operation via `AcadApp.ShowModelessWindow(window)` enables seamless viewport pan/zoom, with `WindowState.Minimized` / `doc.LockDocument()` supporting interactive "Pick Frame".
- Command registration must bridge from `HPAutoCad.Loader` (`[CommandMethod("HPSMARTPLOT")]`, `[CommandMethod("HPLOT")]`) to `HPAutoCad.Entry.Start` via delegates.
- Ribbon integration cleanly mounts a new "Plot" panel (`HPPLOT_PANEL`) onto the shared `HPAUTOCAD_MCP_TAB` ribbon tab using a crisp, resolution-independent vector glyph in `RibbonIcons.cs`.
- `PdfSharp` v6.1.1 requires no ILRepack merging; it is isolated cleanly within `AppLoadContext` and deployed beside `HPAutoCad.dll`.

---

## 6. Verification Method

### 6.1 Automated Compilation & Static Checks
Run the following build command from repository root or `HPAutoCad/`:
```powershell
dotnet build HPAutoCad/HPAutoCad.slnx -c Debug
```
**Expected outcome**:
- 0 compilation errors.
- `RepackMaterialDesign: toolkit merged into HPAutoCad.dll` executes cleanly.
- `MaterialDesignThemes.Wpf.dll` is deleted from output, while `HPAutoCad.dll` and `PdfSharp.dll` exist in `Contents\App\`.

### 6.2 Unit Test Execution
Run the unit test suites:
```powershell
dotnet test HPAutoCad/HPAutoCad.Tests
dotnet test HPAutoCad/HPAutoCad.Mcp.Server.Tests
dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests
```
**Expected outcome**:
- `HPAutoCad.Tests`: 241 passed, 0 failed.
- `HPAutoCad.Mcp.Server.Tests`: 280 passed, 0 failed.
- `HPCivil3d.McpBridge.Tests`: 60 passed, 0 failed (no mirror drift).

### 6.3 Deployment & Bundle Verification
Inspect bundle directory:
```powershell
Get-ChildItem "$env:APPDATA\Autodesk\ApplicationPlugins\HPAutoCad.bundle\Contents\App"
```
**Verification criteria**:
- `HPAutoCad.dll` is present.
- `PdfSharp.dll` is present.
- No loose `MaterialDesignThemes.Wpf.dll` in `Contents\App\`.
- `HPAutoCad.Loader.dll` is present in `Contents\`.

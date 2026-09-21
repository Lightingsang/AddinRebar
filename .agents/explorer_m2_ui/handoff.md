# Handoff Report: Milestone M2 (WPF MVVM UI & Theming Exploration)

## 1. Observation
- Legacy UI components are located in `HPGeo/HPGeo.AutoCad/UI/` consisting of 25 files (approx. 70 KB total) spanning Windows, UserControls, ViewModels, Model records, and Themes:
  - `GeoExportWindow.xaml` (122 lines) & `GeoExportWindow.xaml.cs` (32 lines)
  - `GeoImportWindow.xaml` (93 lines) & `GeoImportWindow.xaml.cs` (21 lines)
  - `CrsSelectionView.xaml` (63 lines) & `CrsSelectionView.xaml.cs` (10 lines)
  - `MapPanel.xaml` (13 lines) & `MapPanel.xaml.cs` (143 lines)
  - `MapHtml.cs` (67 lines)
  - `ValueConverters.cs` (35 lines)
  - `GeoExportViewModel.cs` (174 lines) & `GeoExportViewModel.Commands.cs` (128 lines)
  - `GeoImportViewModel.cs` (180 lines)
  - `CrsSelectionViewModel.cs` (143 lines)
  - `IGeoExportShell.cs` (23 lines)
  - `GeoExportItems.cs` (47 lines)
  - Theming files: `Theme.xaml` (105 lines), `MaterialBridge.xaml` (19 lines), `ThemeDark.xaml` (21 lines), `ThemeLight.xaml` (21 lines), `MaterialThemeBridge.cs` (111 lines), `IHostTheme.cs` (14 lines), `HPGeoHostTheme.cs` (39 lines), `ThemeInfo.cs` (7 lines), `ThemeResources.cs` (15 lines).
- In `GeoExportWindow.xaml.cs` (lines 13-20):
  ```csharp
  using (AssemblyLoadContext.GetLoadContext(typeof(GeoExportWindow).Assembly)!.EnterContextualReflection())
  {
      Resources.MergedDictionaries.Add(ThemeResources.Styles());
      InitializeComponent();
      MaterialThemeBridge.Attach(this, HPGeoHostTheme.Instance);
      MapView.DarkTheme = HPGeoHostTheme.Instance.IsDark;
  }
  ```
- In `MapPanel.xaml.cs` (lines 22-24, 51-76):
  ```csharp
  private static readonly string UserDataFolder =
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPGeo", "webview2");
  ...
  Directory.CreateDirectory(UserDataFolder);
  var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: UserDataFolder);
  await Browser.EnsureCoreWebView2Async(environment);
  ```
- In `HPGeo.AutoCad.csproj` (lines 60-68):
  `RepackMaterialDesign` target merges `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, and `Microsoft.Xaml.Behaviors.dll` into the main assembly with ILRepack `/union /parallel /noRepackRes`.
- In `HPAutoCad.McpBridge/Service/AutocadHostTheme.cs` (lines 23-37):
  Reads `AcadApp.GetSystemVariable("COLORTHEME")` (0 = Dark, 1 = Light) and listens to `AcadApp.SystemVariableChanged`.

## 2. Logic Chain
1. **Repository Layout Alignment**:
   - `HPRebar` separates domain models, views, viewmodels, and themes into feature folders (`HPRebar/Resources/Themes/`, `HPRebar/<Feature>/View/`, `HPRebar/<Feature>/ViewModel/`, `HPRebar/<Feature>/Model/`).
   - The legacy `HPGeo.AutoCad.UI` dumped all 25 files flat into a single folder.
   - Refactoring them into `HPAutoCad/HPAutoCad/HPGeoLink/View/`, `ViewModel/`, `Model/` and root `HPAutoCad/HPAutoCad/Resources/Themes/` achieves structural parity with `HPRebar` and `HPAutoCad.McpBridge`.
2. **ALC Contextual Reflection Requirement**:
   - In AutoCAD 2026 (.NET 8), `HPAutoCad.dll` runs in an isolated `AssemblyLoadContext` (`AppLoadContext`).
   - WPF BAML loading resolves pack URIs (`/HPAutoCad;component/...`) via `Assembly.Load("HPAutoCad")`, which searches the Default ALC by default.
   - Wrapping `InitializeComponent()` and `ThemeResources.Styles()` within `EnterContextualReflection()` sets `AssemblyLoadContext.CurrentContextualReflectionContext` on the thread, allowing BAML to resolve assemblies inside `AppLoadContext` without `XamlParseException`.
3. **Theming Architecture**:
   - `ThemeInfoAttribute` (`[assembly: ThemeInfo(None, SourceAssembly)]`) is required so WPF knows `generic.baml` was merged into `HPAutoCad.dll`.
   - `MaterialThemeBridge.Apply` swaps the top-level overlay dictionary in `window.Resources.MergedDictionaries`, which is the only mechanism that reliably updates dynamic resources on shown WPF windows in AutoCAD without an `Application.Current`.
   - Replacing `HPGeoHostTheme` with `AutocadHostTheme` ensures uniform AutoCAD integration across MCP Bridge and HPGeoLink.
4. **WebView2 Resilience**:
   - AutoCAD's install directory is read-only. Setting `UserDataFolder` under `%LocalAppData%\HPAutoCad\webview2` prevents crash on launch.
   - Wrapping WebView2 initialization in try-catch with a fallback TextBlock ensures dialogs and commands continue functioning even on machines without the WebView2 runtime or internet access.

## 3. Caveats
- No implementation code was written or modified during this exploration step (strictly adhering to read-only constraint).
- Integration between `HPAutoCad` commands and WPF dialogs depends on CAD selection IDs passed from `DrawingReader` (handled by `explorer_m2_cad`).
- Native `WebView2Loader.dll` bundling and ILRepack MSBuild targets are covered in coordination with `explorer_m2_repack`.

## 4. Conclusion
The implementation specification for Milestone M2 UI is fully formulated, concrete, and published at:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m2_ui_plan.md`.
The plan contains:
- Complete file mapping table (25 files mapped to 27 target files in View, ViewModel, Model, Themes).
- Full namespace migrations from `HPGeo.AutoCad.UI.*` to `HPAutoCad.HPGeoLink.*` and `HPAutoCad.Resources.Themes.*`.
- Exact XAML code, code-behind templates, ViewModels, and shell interfaces.
- Theming setup with `AutocadHostTheme` and `MaterialThemeBridge`.
- Rigorous ALC Contextual Reflection pattern (`EnterContextualReflection`).
- Step-by-step 7-phase implementation instructions and verification matrix.

## 5. Verification Method
- Inspect the plan file:
  `view_file g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m2_ui_plan.md`
- Once implemented by worker:
  `dotnet build HPAutoCad/HPAutoCad.slnx`
  `powershell -File HPAutoCad/tools/harness/run-bridge-unattended.ps1`

# Comprehensive Survey: HPRebar.McpBridge, Packaging, Theme Testing & Build Constraints

**Date:** 2026-10-10  
**Author:** Explorer 3 (McpBridge, Packaging & Test Explorer)  
**Target Solution:** `HPRebar/HPRebar.slnx`  
**Context:** MaterialDesignInXamlToolkit (v5.3.2) Full Adoption & Style Migration  

---

## 1. Executive Summary

This investigation surveys the architecture, current state, packaging mechanisms, test suites, and build constraints of `HPRebar.McpBridge` and related solution infrastructure in preparation for the complete MaterialDesignInXamlToolkit v5.3.2 migration.

### Key Discoveries:
1. **McpBridge UI is a Single Modeless Window (`McpBridgeStatusView.xaml`):**
   - Contains 1 window (`McpBridgeStatusView`) with 3 cards (`Header`, `Safety`, `Last run`), 2 check boxes (`IsExecutionEnabled`, `AutoStartListener`), 4 buttons (`ToggleListener`, `RestartBridge`, `Close`, `CopyScript`/`OpenAudit`), 1 read-only mono text box (`LastScriptPreview`), and a state dot ellipse (`StatusDot`).
   - `HPRebar.McpBridge` **does not own its own theme dictionaries**; it links all theme resources (`MaterialBridge.xaml`, `ThemeDark.xaml`, `ThemeLight.xaml`, `Typography.xaml`, `Spacing.xaml`, `Controls.xaml`, `ThemeIcons.xaml`) directly from `HPRebar/HPRebar/Resources/Themes/` via MSBuild. The only unique file on disk is `Resources/Themes/Theme.xaml` which points pack URIs to `/HPRebar.McpBridge;component/...`.
2. **Theme Synchronization Lifecycle:**
   - Follows Revit live Dark/Light mode (`Options ▸ Colors ▸ UI theme`) via `Application.ThemeChanged` (Revit 2024+) -> `RevitHostTheme.Instance.NotifyChanged()` -> `MaterialThemeBridge.Attach(this, RevitHostTheme.Instance)`.
   - `MaterialThemeBridge.Apply(...)` replaces the top-level overlay dictionary in `window.Resources.MergedDictionaries`. It avoids `PaletteHelper` entirely (since Revit add-ins have no `Application.Current`), ensuring instant theme switching across all open modeless/modal windows.
3. **ThemeGallery Visual Gate:**
   - Standalone tool at `HPRebar/tools/theme-gallery/` (`net8.0-windows;net48`).
   - Automatically scans `HPRebar.dll` and discovers 16 tab views.
   - **Deficiencies identified:** (a) Does not cover `HPRebar.McpBridge.dll` or `McpBridgeStatusView`; (b) Has no dedicated component catalog/showcase for individual MaterialDesign controls (Outlined inputs, ComboBox, CheckBox, DataGrid, etc.); (c) Suffers from a `MissingMethodException` in `KataElevationGallery.cs:149` due to an outdated constructor signature for `KataSupport` in `HPRebar.Core`; (d) Fails off-screen capture in non-interactive sessions due to `Graphics.CopyFromScreen` (requires `RenderTargetBitmap`).
4. **Theme Test Coverage:**
   - Single automated test class: `HPRebar.Core.Tests/Themes/ThemeTokenCoverageTests.cs` (3 fact tests).
   - Validates resource keys, legacy re-based keys, and component pack URIs for `HPRebar/HPRebar`.
   - **Major Gap:** It does **not** scan `HPRebar.McpBridge`.
   - Runs under `Microsoft.Testing.Platform` (MTP) pinned in `global.json`, requiring `--filter-class *ThemeTokenCoverageTests*`.
5. **Packaging & Build Constraints:**
   - `HPRebar.csproj`: Uses SDK-level `<IsRepackable>true</IsRepackable>`, merging `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, and `Microsoft.Xaml.Behaviors.dll` directly into `HPRebar.dll` (+10.5 MB). Supported and verified on `Debug.R25`, `Debug.R26`, `Debug.R27` with `-p:DeployAddin=false`. Pre-existing `net48` compilation error `CS1739` affects `Debug.R23` and `Debug.R24` in `KataExport`/`KataRebar`.
   - `HPRebar.McpBridge.csproj`: Only targets `net8.0-windows7.0` (Revit 2025/2026). Sets `<IsRepackable>false</IsRepackable>` (to keep Roslyn DLLs unmerged) and uses a targeted custom MSBuild target `RepackMaterialDesign` to merge only the toolkit and delete loose assemblies before deployment. Verified 100% clean build on `Debug.R25` and `Debug.R26`.

---

## 2. HPRebar.McpBridge UI Components & Styles Deep Dive

### 2.1 File & Directory Inventory
```
HPRebar/HPRebar.McpBridge/
├── Application.cs                         # Add-in lifecycle, ribbon setup, ThemeChanged hook
├── HPRebar.McpBridge.addin                # Addin manifest (ContextName=HPRebar.McpBridge, UseRevitContext=False)
├── HPRebar.McpBridge.csproj               # Project file with linked resources & RepackMaterialDesign target
├── McpBridgeCommand.cs                    # ExternalCommand opening McpBridgeStatusView modelessly
├── McpBridgeExternalEventHandler.cs       # Revit ExternalEvent API handler
├── McpBridgeRequest.cs                    # Internal execution request model
├── Resources/
│   └── Themes/
│       └── Theme.xaml                     # Master dictionary for McpBridge (assembly pack URIs)
└── View/
    ├── McpBridgeStatusView.xaml           # Modeless bridge status window
    └── McpBridgeStatusView.xaml.cs        # Code-behind attaching MaterialThemeBridge
```

### 2.2 XAML Structure & Controls Used in `McpBridgeStatusView.xaml`
- **Root Element:** `<Window>` (Title: "HPRebar MCP Bridge", 560x640, min 480x520).
  - Background: `{DynamicResource Brush.Background}`
  - Foreground: `{DynamicResource Brush.Foreground.Primary}`
  - Font: `{DynamicResource Font.Family.Default}`, `{DynamicResource Font.Size.Body}`
- **Section 1: Header (Listener Status & Toggle)**
  - Container: `<Border Style="{DynamicResource Card}">`
  - Visual Indicator: `<Ellipse Style="{DynamicResource StatusDot}"/>`
    - Uses local Style with DataTriggers on `StatusKind`:
      - `Listening` -> `Fill="{DynamicResource Brush.Info}"`
      - `Connected` -> `Fill="{DynamicResource Brush.Success}"`
      - `Busy` -> `Fill="{DynamicResource Brush.Warning}"`
      - `Error` -> `Fill="{DynamicResource Brush.Danger}"`
  - Status Texts:
    - `TextBlock Style="{DynamicResource Subheading}" Text="{Binding StatusText}"`
    - `TextBlock Style="{DynamicResource Caption}" Text="{Binding StatusDetail}" Foreground="{DynamicResource Brush.Danger}"`
    - `TextBlock Style="{DynamicResource Caption}" Foreground="{DynamicResource Brush.Foreground.Secondary}"` (Pipe Name & Revit Version)
  - Action: `<Button Style="{DynamicResource PrimaryButton}" Content="{Binding ToggleButtonText}" Command="{Binding ToggleListenerCommand}"/>`
- **Section 2: Safety & Opt-In Settings**
  - Container: `<Border Style="{DynamicResource Card}">`
  - Header: `<TextBlock Style="{DynamicResource BodyStrong}" Text="Safety"/>`
  - CheckBox 1: `IsExecutionEnabled` ("Allow AI code execution in this Revit session")
    - ToolTip explaining permissions and sandbox limits.
    - Currently uses default implicit `CheckBox` style.
  - Explanatory Note: `<TextBlock Style="{DynamicResource Caption}" Text="Off by default. Every script runs in one undo group named 'MCP: <label>' — Ctrl+Z in Revit reverts it. Dry runs never commit."/>`
  - CheckBox 2: `AutoStartListener` ("Start the listener automatically when Revit opens")
    - Currently uses default implicit `CheckBox` style.
- **Section 3: Last Run Preview**
  - Container: `<Border Style="{DynamicResource Card}">`
  - Action Header: `Button Style="{DynamicResource LinkButton}" Content="Copy script" Command="{Binding CopyLastScriptCommand}"` + `TextBlock Style="{DynamicResource BodyStrong}" Text="Last run"`
  - Summary: `TextBlock Style="{DynamicResource Caption}" Text="{Binding LastRunSummary}"`
  - Script Preview Box:
    - `<TextBox Style="{DynamicResource StandardTextBox}" Text="{Binding LastScriptPreview, Mode=OneWay}" IsReadOnly="True" FontFamily="{DynamicResource Font.Family.Mono}" FontSize="{DynamicResource Font.Size.Caption}" AcceptsReturn="True" TextWrapping="NoWrap" VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Auto"/>`
- **Section 4: Footer Actions**
  - Stats: `TextBlock Style="{DynamicResource CompileCountText}"` (triggers on `IsCompileCountHigh` -> `Brush.Warning`)
  - Link: `Button Style="{DynamicResource LinkButton}" Content="Open audit folder" Command="{Binding OpenAuditFolderCommand}"`
  - Buttons:
    - `Button Style="{DynamicResource SecondaryButton}" Content="Restart bridge" Command="{Binding RestartBridgeCommand}"`
    - `Button Style="{DynamicResource PrimaryButton}" Content="Close" Command="{Binding CloseCommand}"`

### 2.3 Style and Resource References in McpBridge
| Category | Resource Key | Source / Definition Location |
|---|---|---|
| Palette Brushes | `Brush.Background`, `Brush.Foreground.Primary`, `Brush.Foreground.Secondary`, `Brush.Foreground.Tertiary`, `Brush.Info`, `Brush.Success`, `Brush.Warning`, `Brush.Danger`, `Brush.Border`, `Brush.SurfaceElevated` | `ThemeDark.xaml` / `ThemeLight.xaml` (linked from `HPRebar`) |
| Typography | `Font.Family.Default`, `Font.Family.Mono`, `Font.Size.Body`, `Font.Size.Caption`, `Subheading`, `Caption`, `BodyStrong` | `Typography.xaml` (linked from `HPRebar`) |
| Spacing | `Spacing.Medium`, `Spacing.SmallHorizontal`, `Spacing.MediumTop`, `Spacing.SmallTop`, `Spacing.XSmall` | `Spacing.xaml` (linked from `HPRebar`) |
| Buttons | `PrimaryButton`, `SecondaryButton`, `LinkButton` | `MaterialBridge.xaml` (linked from `HPRebar`, based on toolkit raised/outlined/flat buttons) |
| Inputs | `StandardTextBox` | `MaterialBridge.xaml` (linked from `HPRebar`, based on `MaterialDesignOutlinedTextBox`) |
| Cards | `Card` | `Controls.xaml` (linked from `HPRebar`, Border with `SurfaceElevated` + `BorderThickness=1` + `CornerRadius=6`) |

### 2.4 Theme Synchronization Architecture
1. **Host Event Binding:**
   - In `HPRebar.McpBridge/Application.cs:91-94`:
     ```csharp
     #if REVIT2024_OR_GREATER
     _onThemeChanged = (_, _) => { ApplyIcon(); Resources.Themes.RevitHostTheme.Instance.NotifyChanged(); };
     Application.ThemeChanged += _onThemeChanged;
     #endif
     ```
   - When Revit changes theme, `RevitHostTheme.Instance.NotifyChanged()` fires.
2. **Window Attachment:**
   - In `McpBridgeStatusView.xaml.cs:15`:
     ```csharp
     MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, dark => new RibbonIcons(dark).McpBridge);
     ```
   - `Attach` calls `Apply(...)` immediately and registers an event handler `host.Changed += OnChanged` marshaled to `window.Dispatcher.BeginInvoke`.
   - On `window.Closed`, it automatically unsubscribes to prevent memory leaks.
3. **Dynamic Resource Replacement:**
   - `MaterialThemeBridge.Apply(Window window, bool dark, Func<bool, ImageSource>? icon)`:
     - Sets `window.Icon` from vector `RibbonIcons` (dark/light adapted).
     - Resolves the palette: `pack://application:,,,/HPRebar.McpBridge;component/Resources/Themes/ThemeDark.xaml` (or `ThemeLight.xaml`).
     - Locates `<md:CustomColorTheme>` inside merged dictionaries.
     - Creates a `MaterialDesignThemes.Wpf.Theme` instance via `Theme.Create(dark ? BaseTheme.Dark : BaseTheme.Light, primaryColor, secondaryColor)`.
     - Maps palette tokens to the toolkit theme:
       - `theme.Background = Token(palette, "Color.Background")`
       - `theme.Foreground = Token(palette, "Color.Foreground.Primary")`
       - `theme.ForegroundLight = Token(palette, "Color.Foreground.Secondary")`
       - `theme.ValidationError = Token(palette, "Color.Danger")`
       - `theme.Cards.Background = Token(palette, "Color.SurfaceElevated")`
       - `theme.Cards.Border = Token(palette, "Color.Border")`
       - `theme.ToolTips.Background = Token(palette, "Color.SurfaceElevated")`
     - Inserts the resulting `overlay` dictionary at the **top level** of `window.Resources.MergedDictionaries`.
     - Because WPF evaluates `{DynamicResource}` lookups from top to bottom, replacing the top dictionary triggers an immediate repaint of all controls in the opened window.

---

## 3. ThemeGallery & Showcase Windows

### 3.1 Overview of `HPRebar/tools/theme-gallery/`
`ThemeGallery` is an off-Revit test harness and screenshot generator located in `HPRebar/tools/theme-gallery/`.
- **Target Frameworks:** `net8.0-windows;net48` (multi-targeting .NET 8 and .NET Framework 4.8).
- **Execution Mode:** Standalone executable running outside Revit. It dynamically loads the compiled `HPRebar.dll` and mocks Revit assemblies (`RevitAPI.dll`, `RevitAPIUI.dll`) via `AppDomain.CurrentDomain.AssemblyResolve` for reflection only.

### 3.2 Current Implementation & Workflow
1. Loads `HPRebar.dll` from specified path (e.g. `HPRebar/HPRebar/bin/Debug.R26/HPRebar.dll`).
2. Scans for all non-abstract types inheriting from `System.Windows.Controls.UserControl` in namespaces containing `.View`.
3. Discovers 16 user controls:
   - BeamRebar: `Beam3DViewport`, `AdditionalBarsTabView`, `GeometryTabView`, `MainBarsTabView`, `StirrupsTabView`, `ViewsTabView`
   - ColumnRebar: `AdditionalStirrupsTabView`, `BarsDivisionTabView`, `BarsTabView`, `BottomDowelsTabView`, `GeometryTabView`, `SettingTabView`, `StirrupsTabView`, `TopDowelsTabView`
   - FoundationRebar: `FoundationGeometryView`, `FoundationSettingView`
4. For each view:
   - Creates a host `Window` merging `pack://application:,,,/HPRebar;component/Resources/Themes/Theme.xaml`.
   - Binds Background, Foreground, Font to theme tokens.
   - Invokes `MaterialThemeBridge.Apply(window, dark: true)` and `Apply(window, dark: false)`.
   - Captures screenshots to `output/theme-gallery/`.
5. Runs `KataElevationGallery.Run(...)` for 5 Kata elevation scenarios.
6. Tests `RibbonIcons` vector glyph conversion to `Window.Icon`.

### 3.3 Critical Deficiencies & Required Upgrades
1. **Missing McpBridge Coverage:**
   - `ThemeGallery` only loads `HPRebar.dll`. It does not test `HPRebar.McpBridge.dll` or `McpBridgeStatusView`.
   - Upgrade requirement: Add support for passing or resolving `HPRebar.McpBridge.dll` to host and capture `McpBridgeStatusView`.
2. **Missing Component / Control Catalog (Requirement R5):**
   - Currently, `ThemeGallery` only loads specific feature `UserControls`. If a feature view doesn't happen to use a specific control (e.g., RadioButton, Expander, Slider, DataGrid with selection), that control is not visually verified.
   - Upgrade requirement: Create a dedicated `ControlGalleryView` or `MaterialComponentCatalogView` displaying all standard controls:
     - Buttons: Raised Primary, Outlined Secondary, Danger, Icon, Link/Flat
     - Inputs: Outlined TextBox (regular, numeric right-aligned, search with hint), Outlined ComboBox with floating hint
     - Toggles: CheckBox, RadioButton, ToggleSwitch
     - Containers: `md:Card`, Expander, TabControl, GroupBox
     - Tables: DataGrid with column headers, cell selections, alternating rows
     - Feedback: ToolTip, Badge, Status dots, Validation error state
3. **Pre-Existing Bug in `KataElevationGallery.cs`:**
   - At line 149, `KataElevationGallery.Support(...)` calls `Activator.CreateInstance(_support, ...)` with 6 arguments.
   - `KataSupport` record in `HPRebar.Core/KataExport/Models/KataRunModels.cs:33` now has 7 positional parameters:
     `(Kind, Extent, ElementKey, SectionText, Upper, CrossingBeamStationMm, CrossingBeamSection)`.
   - Result: `MissingMethodException: Constructor on type 'HPRebar.Core.KataExport.Models.KataSupport' not found`.
4. **Headless Screen Capture Flaw:**
   - Line 146 calls `graphics.CopyFromScreen(x, y, 0, 0, ...)`. In non-interactive or background execution contexts, this throws `Win32Exception: The handle is invalid`.
   - Upgrade recommendation: Use `RenderTargetBitmap` + `PngBitmapEncoder` to render WPF Visuals offscreen without touching the desktop device context.

---

## 4. Theme & Style Test Coverage Across the Solution

### 4.1 Existing Automated Tests
The sole automated theme test class is:
`HPRebar/HPRebar.Core.Tests/Themes/ThemeTokenCoverageTests.cs` (xUnit v3 / net8.0).

| Test Name | What it Asserts | Status |
|---|---|---|
| `EveryResourceKeyUsedByTheAddInXamlIsDefined` | Scans `Theme.xaml` and all merged files. Asserts `ThemeDark.xaml` and `ThemeLight.xaml` define identical key sets. Iterates through all XAML in `HPRebar/HPRebar/` and asserts every `{DynamicResource <key>}` or `{StaticResource <key>}` is defined. | PASS (Verified) |
| `LegacyStyleKeysAreRebasedOnTheToolkitNotRedefinedBesideIt` | Asserts `MaterialBridge.xaml` is merged before `ThemeDark.xaml`. Asserts `Buttons.xaml` and `TextBoxes.xaml` are NOT merged. Asserts 8 legacy keys (`PrimaryButton`, `SecondaryButton`, `DangerButton`, `IconButton`, `LinkButton`, `StandardTextBox`, `NumberTextBox`, `SearchTextBox`) exist in `MaterialBridge.xaml`. Asserts `MaterialDesignFont` is Segoe UI and `{md:MaterialDesignFont}` is absent. | PASS (Verified) |
| `EveryComponentPathReferencedByXamlExistsOnDisk` | Finds every `/HPRebar;component/<path>` pack URI across all XAML in `HPRebar/HPRebar/` and checks that the corresponding file exists on disk. | PASS (Verified) |

### 4.2 Deficiencies & Migration Gaps
1. **McpBridge is completely excluded from test coverage:**
   - In `ThemeTokenCoverageTests.cs:103`:
     ```csharp
     return Path.Combine(directory.FullName, "HPRebar");
     ```
   - It only scans `HPRebar/HPRebar/`.
   - It does **not** scan `HPRebar.McpBridge/View/McpBridgeStatusView.xaml` or `HPRebar.McpBridge/Resources/Themes/Theme.xaml`!
   - Recommendation: Update `ThemeTokenCoverageTests` to scan both `HPRebar/HPRebar` and `HPRebar/HPRebar.McpBridge`, and check `/HPRebar.McpBridge;component/` pack URIs.
2. **Impact of Requirement R1/R2 on `LegacyStyleKeys...` Test:**
   - Requirement R1 specifies:
     *"Thay thế các alias/style HP bằng resource key chính thức của toolkit hoặc implicit styles. Loại bỏ hoàn toàn các style/alias trung gian khi không còn nơi sử dụng."*
   - If legacy style keys (e.g. `PrimaryButton`, `StandardTextBox`) are removed in favor of direct toolkit styles (e.g. `MaterialDesignRaisedButton`, `MaterialDesignOutlinedTextBox`), `LegacyStyleKeysAreRebasedOnTheToolkitNotRedefinedBesideIt` will fail unless updated.
3. **Testing Platform Syntax:**
   - Running `dotnet test` with standard VSTest `--filter` fails because the repository uses `Microsoft.Testing.Platform` (MTP) via `global.json`.
   - The correct command to run theme tests is:
     ```powershell
     dotnet test HPRebar.Core.Tests --filter-class *ThemeTokenCoverageTests*
     ```

---

## 5. Packaging, ILRepack & Build Configurations

### 5.1 Project & Solution Map
| Project | Target Frameworks | ILRepack Strategy | MaterialDesign Packages | Configurations in `HPRebar.slnx` |
|---|---|---|---|---|
| `HPRebar/HPRebar.csproj` | `net48` (R23/R24)<br/>`net8.0-windows7.0` (R25/R26)<br/>`net10.0-windows7.0` (R27) | `<IsRepackable>true</IsRepackable>` (Nice3point SDK auto-merges all copy-local dependencies) | `MaterialDesignThemes` v5.3.2<br/>(transitive: `MaterialDesignColors` v5.3.2) | All 10 configs (`Debug.R23..R27`, `Release.R23..R27`) |
| `HPRebar/HPRebar.McpBridge.csproj` | `net8.0-windows7.0` (R25/R26 only) | `<IsRepackable>false</IsRepackable>`<br/>Custom target `RepackMaterialDesign` merges only toolkit DLLs | `MaterialDesignThemes` v5.3.2<br/>`ILRepack` v2.0.46 | `Debug.R25`, `Debug.R26`, `Release.R25`, `Release.R26`<br/>(Excluded on R23, R24, R27) |
| `HPRebar/HPRebar.Core.csproj` | `netstandard2.0` | None (pure math/geometry, no Revit, no UI) | None | All 10 configs |
| `HPRebar.Core.Tests` | `net8.0` | None (xUnit v3) | None | All 10 configs |
| `HPRebar.Mcp.Server.Tests` | `net10.0` | None (xUnit v3) | None | All 10 configs |
| `HPRebar.Tests` | `net8.0-windows7.0` | None (TUnit) | None | Excluded (`Project="false"`) |

### 5.2 Deep Inspection of ILRepack Configurations
1. **HPRebar (`HPRebar.csproj`):**
   - Uses `Nice3point.Revit.Sdk`'s built-in ILRepack target.
   - ILRepack rewrites all toolkit BAML pack URIs to `pack://application:,,,/HPRebar;component/MaterialDesignThemes.Wpf/...` and merges `generic.baml`.
   - `Resources/Themes/ThemeInfo.cs` supplies:
     `[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]`.
     Without this attribute, default implicit styles for `PackIcon`, `Card`, etc., cannot be located by WPF.
   - `MaterialDesignFont` is pinned to `Segoe UI` in `MaterialBridge.xaml`. ILRepack cannot rewrite the pack URI inside `{md:MaterialDesignFont}`, so `{md:MaterialDesignFont}` must never be used.
2. **HPRebar.McpBridge (`HPRebar.McpBridge.csproj`):**
   - Keeps `<IsRepackable>false</IsRepackable>` so Roslyn compilers (`Microsoft.CodeAnalysis.CSharp.Scripting.dll`, etc., ~15 MB) remain loose copy-local assemblies in the add-in folder, preventing type collisions with other Revit add-ins.
   - Defines a custom post-build target `RepackMaterialDesign`:
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
   - Inputs: Intermediate assembly (`obj/.../HPRebar.McpBridge.dll`), `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, `Microsoft.Xaml.Behaviors.dll`.
   - Deletes loose toolkit assemblies after repacking so they never mix with other add-ins in `Revit.exe`.
   - Links `ThemeInfo.cs` from `HPRebar` so `HPRebar.McpBridge.dll` also gains `[assembly: ThemeInfo]`.

### 5.3 Directory.Build.props Inspection
- `Directory.Build.props` exists in 12 other project folders (`HPAutoCad/`, `HPCivil3d/`, `HPEtabs/`, `HPDynamo/`, etc.).
- There is **no** `Directory.Build.props` in `HPRebar/` or the solution root.
- All package versions and compiler properties are maintained directly in `HPRebar/HPRebar/HPRebar.csproj` and `HPRebar/HPRebar.McpBridge/HPRebar.McpBridge.csproj`.

### 5.4 Build Commands & Live Verification Results
All tests executed with `-p:DeployAddin=false` to prevent file locking by running Revit instances.

| Target / Project | Configuration | Framework | Command | Result | Notes |
|---|---|---|---|---|---|
| Whole Solution | `Debug.R26` | Multi | `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` | **PASS (0 Errors)** | Builds HPRebar, Core, McpBridge, McpServer, Tests. ILRepack succeeds for both DLLs. |
| HPRebar | `Debug.R25` | `net8.0-windows7.0` | `dotnet build HPRebar/HPRebar.csproj -c Debug.R25 -p:DeployAddin=false` | **PASS (0 Errors)** | ILRepack merges toolkit clean. |
| HPRebar | `Debug.R26` | `net8.0-windows7.0` | `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false` | **PASS (0 Errors)** | Primary dev target on local machine. |
| HPRebar | `Debug.R27` | `net10.0-windows7.0` | `dotnet build HPRebar/HPRebar.csproj -c Debug.R27 -p:DeployAddin=false` | **PASS (0 Errors)** | Revit 2027 net10 build verified. |
| HPRebar | `Debug.R24` | `net48` | `dotnet build HPRebar/HPRebar.csproj -c Debug.R24 -p:DeployAddin=false` | **FAIL (102 Errors)** | Pre-existing CS1739 in Polyfill string interpolation (`format` parameter). |
| HPRebar | `Debug.R23` | `net48` | `dotnet build HPRebar/HPRebar.csproj -c Debug.R23 -p:DeployAddin=false` | **FAIL (102 Errors)** | Same pre-existing CS1739 as R24. |
| HPRebar.McpBridge | `Debug.R25` | `net8.0-windows7.0` | `dotnet build HPRebar.McpBridge/HPRebar.McpBridge.csproj -c Debug.R25 -p:DeployAddin=false` | **PASS (0 Errors)** | Toolkit repacked clean. |
| HPRebar.McpBridge | `Debug.R26` | `net8.0-windows7.0` | `dotnet build HPRebar.McpBridge/HPRebar.McpBridge.csproj -c Debug.R26 -p:DeployAddin=false` | **PASS (0 Errors)** | Toolkit repacked clean. |
| HPRebar.Core.Tests | `Debug` | `net8.0` | `dotnet test HPRebar.Core.Tests` | **PASS (1,682/1,682)** | All unit tests green. |
| Theme Token Tests | `Debug` | `net8.0` | `dotnet test HPRebar.Core.Tests --filter-class *ThemeTokenCoverageTests*` | **PASS (3/3)** | Token coverage verified. |

---

## 6. Migration Recommendations for the Full Team

### Recommendation 1: Updating McpBridge Styles & Resources
- **Direct Style Adoption:**
  - For `McpBridgeStatusView.xaml`:
    - Switch CheckBoxes from implicit styling to explicit MaterialDesign styles if required, e.g. `Style="{StaticResource MaterialDesignCheckBox}"`.
    - Ensure `Border Style="{DynamicResource Card}"` maps cleanly to `MaterialDesignCard` or retains the compact `Card` border style.
    - Standardize Buttons: Use `Style="{StaticResource MaterialDesignRaisedButton}"` for primary CTA ("Toggle Listener", "Close"), `Style="{StaticResource MaterialDesignOutlinedButton}"` for secondary ("Restart bridge"), `Style="{StaticResource MaterialDesignFlatButton}"` for inline links ("Copy script", "Open audit folder").
- **Preserve Linked Resource Architecture:**
  - Do NOT create duplicate dictionaries inside `HPRebar.McpBridge`. Keep linking `MaterialBridge.xaml`, `ThemeDark.xaml`, `ThemeLight.xaml`, etc. from `HPRebar/HPRebar/Resources/Themes/`.
  - When updating `HPRebar/HPRebar/Resources/Themes/MaterialBridge.xaml` and `Theme.xaml`, `HPRebar.McpBridge` will automatically inherit all improvements upon compile.

### Recommendation 2: Upgrading ThemeGallery
1. Fix `KataElevationGallery.cs:149`: Add the 7th argument (`null` for `CrossingBeamSection`) to `Activator.CreateInstance(_support, ...)` to resolve the `MissingMethodException`.
2. Add `McpBridgeStatusView` inspection: Add reflection logic to load `HPRebar.McpBridge.dll`, instantiate `McpBridgeStatusView`, and verify its theme switching.
3. Add a dedicated `ControlGalleryView` displaying all core MaterialDesign controls across Dark and Light modes.
4. Replace `CopyFromScreen` with `RenderTargetBitmap` so screenshots render reliably in headless and background environments.

### Recommendation 3: Expanding Theme Test Coverage
1. Expand `ThemeTokenCoverageTests.cs` to iterate over both `HPRebar/HPRebar/` and `HPRebar/HPRebar.McpBridge/`.
2. Add check for `/HPRebar.McpBridge;component/` pack URIs.
3. Update `LegacyStyleKeysAreRebasedOnTheToolkitNotRedefinedBesideIt` if any legacy style aliases are retired or re-aliased during the migration.
4. Add a test fact validating that `MaterialThemeBridge.Apply(...)` successfully modifies `window.Resources.MergedDictionaries` without throwing.

### Recommendation 4: Packaging and Build Hygiene
1. Maintain `<IsRepackable>false</IsRepackable>` on `HPRebar.McpBridge.csproj` and preserve the targeted `RepackMaterialDesign` target.
2. Maintain `[assembly: ThemeInfo]` in both assemblies.
3. Always include `-p:DeployAddin=false` in developer test build commands to avoid DLL lock errors when Revit is open.
4. Address or document the pre-existing CS1739 net48 interpolated string issue affecting `Debug.R23` and `Debug.R24`.

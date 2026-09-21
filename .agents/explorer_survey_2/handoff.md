# Handoff Report: Standalone WPF Bridge Architecture for HPExcel MCP Ecosystem

## 1. Observation

Direct inspection of reference standalone WPF MCP bridges in the repository (`HPPowerBi`, `HPEtabs`, `HPSap2000`) and the shared host-neutral engine (`McpShared`) revealed the following concrete architectural facts:

### 1.1 Solution Layout & Build System
1. **Solution Structure (`HPPowerBi/HPPowerBi.slnx`, lines 11–26; `HPEtabs/HPEtabs.slnx`, lines 1–26)**:
   - Solutions use the modern XML `.slnx` format (not legacy `.sln`).
   - Standard 4-project layout:
     - `<Host>.McpBridge` (`net8.0-windows`, WPF, `WinExe`)
     - `<Host>.McpBridge.Tests` (`net8.0-windows`, WPF, `OutputType=Exe`, MTP runner)
     - `<Host>.Mcp.Server` (`net10.0`, console, `OutputType=Exe`)
     - `<Host>.Mcp.Server.Tests` (`net10.0`, console, `OutputType=Exe`, MTP runner)
   - Shared project references are wired strictly to `../McpShared/`:
     - `HPRebar.Mcp.Contracts.csproj`
     - `HPRebar.McpBridge.Core.csproj`
     - `HPRebar.Mcp.Server.Core.csproj`
   - Zero references exist to sibling host projects (`HPRebar`, `HPAutoCad`, `HPNavis`, `HPEtabs`, `HPCivil3d`, `HPSap2000`, `HPPowerBi`).
2. **Global Configuration (`HPPowerBi/global.json`, lines 1–10)**:
   - Pins .NET SDK: `"version": "10.0.300", "rollForward": "latestMinor", "allowPrerelease": true`.
   - Pins test runner: `"test": { "runner": "Microsoft.Testing.Platform" }`.
3. **Build Properties (`HPPowerBi/Directory.Build.props`, lines 10–18)**:
   - Pins `<PowerBiDefaultVersion>2026</PowerBiDefaultVersion>`, `<LangVersion>latest</LangVersion>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`.
4. **Bridge Project Configuration (`HPPowerBi/HPPowerBi.McpBridge/HPPowerBi.McpBridge.csproj`, lines 1–44)**:
   - `<OutputType>WinExe</OutputType>`
   - `<TargetFramework>net8.0-windows</TargetFramework>`
   - `<UseWPF>true</UseWPF>`
   - `<SatelliteResourceLanguages>en</SatelliteResourceLanguages>`
   - `<PublishSingleFile>false</PublishSingleFile>` (Roslyn script compiler references assemblies by `Assembly.Location`)
   - `<StartupObject>HPPowerBi.McpBridge.Program</StartupObject>`
   - Dependencies: `CommunityToolkit.Mvvm 8.4.0`, `MaterialDesignThemes 5.3.2`, `Serilog 4.4.0`, `Serilog.Sinks.File 7.0.0`.
5. **Test Projects Configuration (`HPPowerBi/HPPowerBi.McpBridge.Tests/HPPowerBi.McpBridge.Tests.csproj`, lines 8–17)**:
   - `<OutputType>Exe</OutputType>`
   - `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`
   - `<PackageReference Include="xunit.v3" Version="3.1.0" />`
   - `<PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />`

### 1.2 UI & Theming Architecture (MaterialDesignThemes 5.3.2)
1. **`ThemeInfo.cs` (`HPPowerBi.McpBridge/Resources/Themes/ThemeInfo.cs`, line 3)**:
   - `[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]` is required so MaterialDesign custom controls (`PackIcon`, `Card`) resolve default styles without throwing runtime XAML resource exceptions.
2. **`WindowsHostTheme.cs` (`HPPowerBi.McpBridge/Resources/Themes/WindowsHostTheme.cs`, lines 12–59)**:
   - Checks `Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")["AppsUseLightTheme"]`.
   - Listens to `SystemEvents.UserPreferenceChanged` when `args.Category == UserPreferenceCategory.General`.
   - Supports environment variable pinning: `HPPOWERBI_MCP_BRIDGE_THEME=dark|light`.
3. **`MaterialThemeBridge.cs` (`HPPowerBi.McpBridge/Resources/Themes/MaterialThemeBridge.cs`, lines 16–65)**:
   - Replaces the theme overlay dictionary at the **top level** of `window.Resources.MergedDictionaries` (nested dictionary replacement fails to repaint rendered WPF windows).
   - Re-derives MaterialDesign brushes (`Background`, `Foreground`, `ValidationError`, `Cards.Background`, `ToolTips.Background`) from the HP palette tokens (`Color.*`).
4. **`MaterialBridge.xaml` (`HPPowerBi.McpBridge/Resources/Themes/MaterialBridge.xaml`, lines 1–53)**:
   - Merges `<md:CustomColorTheme BaseTheme="Dark" PrimaryColor="..." SecondaryColor="..."/>` and `MaterialDesign2.Defaults.xaml`.
   - Re-bases standard button and textbox styles (`PrimaryButton`, `SecondaryButton`, `LinkButton`, `StandardTextBox`) on MaterialDesign while maintaining HP tokens.
5. **App Entry & Theme Application (`HPPowerBi.McpBridge/Views/StatusWindow.xaml.cs`, lines 11–19)**:
   - Modeless/desktop window initializes in code-behind: `MaterialThemeBridge.Attach(this, WindowsHostTheme.Instance)`.

### 1.3 Bridge Lifecycle & Process Management
1. **Single-Instance Enforcement (`HPPowerBi/HPPowerBi.McpBridge/Program.cs`, lines 11–32)**:
   - `using var mutex = new Mutex(true, "Global\\HPPowerBi.McpBridge.SingleInstance", out var isNewInstance);`
   - Exits with message box if another instance is already running.
2. **Application Lifecycle (`HPPowerBi/HPPowerBi.McpBridge/App.xaml.cs`, lines 12–46)**:
   - `OnStartup`: Calls `BridgeEntry.Start()`, constructs ViewModel marshalled via `Dispatcher.CurrentDispatcher.InvokeAsync`, creates and shows Window.
   - `OnExit`: Calls `BridgeEntry.Dispose()`.
3. **Bridge Services Host (`HPPowerBi/HPPowerBi.McpBridge/BridgeEntry.cs`, lines 31–118)**:
   - Configures rolling file Serilog logger (`%LocalAppData%\HPPowerBi\McpBridge\logs\mcpbridge-.log`).
   - Loads persistent settings (`BridgeSettingsStore(VendorFolder, ProductFolder)`).
   - Creates and installs `McpBridgeHost`:
     ```csharp
     var pipe = PipeNaming.For(PipeNaming.PowerBiHost, HostVersionNumber); // hppowerbi-mcp-2026
     var host = new McpBridgeHost(executor, settings, store, HostVersion, pipe, HostName,
         JsonRpcMethods.PowerBiPrefix, PbiSafetyGuard.ExecutionDisabledMessage, dispatcher.DispatchCustomAsync);
     McpBridgeHost.Install(host);
     if (settings.AutoStartListener) host.Start();
     ```
4. **COM STA Worker Threading (`HPEtabs/HPEtabs.McpBridge/EtabsExecutor.Worker.cs`, lines 12–68)**:
   - Dedicated background STA thread (`ApartmentState.STA`) for COM interop calls to avoid RPC threading violations and apartment marshaling deadlocks.
   - Separate control lane for fast Attach/Detach requests ahead of queued scripts.
5. **COM Instance Attachment (`HPSap2000/HPSap2000.McpBridge/Service/SapAttachment.cs`, lines 77–86, 218–223)**:
   - Resolves running instance via P/Invoke `CLSIDFromProgID` and `oleaut32!GetActiveObject`:
     ```csharp
     [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
     private static extern int CLSIDFromProgID(string lpszProgID, out Guid lpclsid);

     [DllImport("oleaut32.dll", PreserveSig = true)]
     private static extern int GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
     ```

### 1.4 3-Tier Safety Engine & Gating
1. **Safety Tiers (`HPEtabs/HPEtabs.McpBridge/Service/EtabsTierTable.cs`, lines 7–17)**:
   - `ReadOnly = 0`: Safe read operations, runs under `transaction: none`, no snapshot needed.
   - `Write = 1`: State/object mutations, requires Write toggle, triggers automatic snapshot backup.
   - `Destructive = 2`: Unlocks, file IO, deletes, macros, requires explicit Destructive toggle.
2. **UI Cascading Gating (`HPEtabs/HPEtabs.McpBridge/ViewModel/EtabsBridgeStatusViewModel.cs`, lines 138–144; `HPPowerBi.McpBridge/Safety/PbiSafetyGuard.cs`, lines 31–50)**:
   - Disabling `IsExecutionEnabled` automatically unchecks and disables `IsWriteEnabled` / `IsDestructiveEnabled`.
   - `CanEnableDestructive` binds to `IsExecutionEnabled` and `IsWriteEnabled`.
   - Destructive toggle is strictly memory-only (`volatile bool`), reset to `false` on every application start.
3. **AST Semantic Analysis (`HPEtabs/HPEtabs.McpBridge/Service/EtabsTierAnalyzer.cs`, lines 46–120)**:
   - Roslyn `SemanticModel` walks syntax tree.
   - Evaluates `MemberAccessExpressionSyntax` against `EtabsTierTable`.
   - Fails closed: unbound or unknown members are classified as `Destructive` (Tier D).
   - If a script declares `transaction: none` or `dryRun: true` on a Tier W/D operation, static `PREVIEW` diagnostics are returned without executing.
4. **Roslyn Guard Profile (`McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`, lines 9–195; `ScriptGuard.cs`, lines 20–75)**:
   - Base deny-list blocks: `System.IO` (except `System.IO.Path`), `System.Net`, `System.Diagnostics.Process`, `System.Reflection`, `System.Runtime.InteropServices`, `System.Threading.Tasks`, `System.Security`, `System.Linq.Expressions`, `Process`, `File`, `Directory`, `Marshal`, `#r`/`#load` directives.
   - Per-host guard profile adds host-specific dangerous members (e.g. `Quit`, `ApplicationExit`, `MessageBox`).

### 1.5 Snapshot Backup Engine
1. **Snapshot Mechanics (`HPEtabs/HPEtabs.McpBridge/Service/EtabsSnapshotManager.cs`, lines 74–104)**:
   - Before executing a Tier W or Tier D operation:
     - If user file has un-tracked edits, capture a `presave` copy.
     - Save/commit state, then capture a timestamped `prerun` copy: `yyyyMMdd-HHmmss_{name}.ext`.
     - Automatically prune snapshot directory to retain newest $N$ copies (e.g. 10 prerun, 5 presave).
2. **Snapshot Wire Contract (`McpShared/HPRebar.Mcp.Contracts/Messages/ExecuteResult.cs`, line 51; `ResultFormatter.cs`, line 31)**:
   - `ExecuteResult.Snapshot`: stores filename of backup snapshot.
   - `ResultFormatter.cs`: sanitizes path to `Path.GetFileName(result.Snapshot)` to avoid leaking machine directory paths over the wire.
3. **Excel Workbook COM Capabilities**:
   - `Workbook.SaveCopyAs(Filename)` saves an exact copy of the workbook to a specified path without affecting the active workbook in memory or altering its saved path.

---

## 2. Logic Chain

From these observations, we establish the following direct design conclusions for `HPExcel`:

1. **Solution & Project Structure**:
   - *Premise*: `HPPowerBi` and `HPEtabs` achieve clean compilation and strict isolation using `.slnx` solutions referencing only `../McpShared/`.
   - *Deduction*: `HPExcel` must replicate this exact layout with `HPExcel.slnx`, `Directory.Build.props`, `global.json`, `HPExcel.McpBridge` (`net8.0-windows`), `HPExcel.Mcp.Server` (`net10.0`), and companion test projects.
2. **WPF & MaterialDesign UI Theming**:
   - *Premise*: `HPPowerBi` and `HPEtabs` provide responsive Dark/Light theming matching Windows app mode by using `MaterialDesignThemes 5.3.2`, `MaterialThemeBridge.cs`, `WindowsHostTheme.cs`, and `[assembly: ThemeInfo]`.
   - *Deduction*: `HPExcel.McpBridge` must adopt this exact theming engine, customized with Microsoft Excel's brand palette: Primary `#107C41` (Deep Excel Green) and Secondary/Accent `#21A366` (Medium Green).
3. **Lifecycle & Out-of-Process COM Bridge**:
   - *Premise*: Microsoft Excel (`EXCEL.EXE`) is an out-of-process COM server. Direct multi-threaded COM calls cause apartment threading crashes, and multiple bridge processes lead to pipe contention.
   - *Deduction*: `HPExcel.McpBridge` must use:
     - Named Mutex (`Global\HPExcel.McpBridge.SingleInstance`) for single-instance enforcement.
     - Dedicated STA worker thread for all Excel COM automation (`Excel.Application`).
     - P/Invoke `oleaut32!GetActiveObject` with ProgID `"Excel.Application"` to detect and attach to running Excel instances.
     - `ClosedXML` (`XLWorkbook`) as a hybrid engine for offline, test, and headless `.xlsx` operations when Excel is not running.
4. **3-Tier Safety Engine**:
   - *Premise*: Excel operations range from harmless reads (`read_range`) to destructive bulk clears (`ClearContents`) and macro executions (`run_macro`).
   - *Deduction*: Implement `ExcelTierAnalyzer`, `ExcelTierTable`, and `ExcelSafetyGuard`:
     - **Tier R (Read)**: `read_range`, `read_worksheet_info`, `find_cells`, `read_table`, `get_excel_context`. Gated only by execution toggle.
     - **Tier W (Write)**: `write_range`, `format_range`, `create_table`, `create_chart`, sheet add/rename. Gated by Write toggle + automatic pre-mutation snapshot.
     - **Tier D (Destructive)**: Sheet deletion, clearing entire sheets, executing macros. Gated by explicit Destructive toggle (off on startup) + automatic snapshot.
     - Static dry-run preview: `transaction: "none"` or `dryRun: true` returns member hit list without mutating the workbook.
5. **Automatic Snapshot Backup Engine**:
   - *Premise*: User workbooks must be safeguarded against unintentional destructive AI modifications.
   - *Deduction*: Implement `ExcelSnapshotManager`:
     - For saved workbooks: backup saved to `.hpexcel_snapshots/` adjacent to the `.xlsx` file.
     - For unsaved workbooks (`Book1`) or protected directories: fallback to `%TEMP%\.hpexcel_snapshots\`.
     - In live COM mode: invoke `workbook.SaveCopyAs(snapshotPath)`.
     - In headless mode: copy `.xlsx` file before `ClosedXML` opens/mutates it.
     - Prune directory to retain the newest 20 snapshots.
     - Set snapshot filename in `ExecuteResult.Snapshot`.

---

## 3. Recommended Project Structure & Complete Code Templates

### 3.1 Solution Layout Recommendation

```
HPExcel/
├── HPExcel.slnx
├── Directory.Build.props
├── global.json
├── README.md
├── HPExcel.McpBridge/
│   ├── HPExcel.McpBridge.csproj
│   ├── Program.cs
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── BridgeEntry.cs
│   ├── Discovery/
│   │   ├── ExcelProcessDetector.cs
│   │   └── ExcelInstanceInfo.cs
│   ├── Com/
│   │   ├── ExcelAttachment.cs
│   │   ├── ExcelStaWorker.cs
│   │   └── ComInteropHelper.cs
│   ├── Headless/
│   │   └── ClosedXmlWorkbookService.cs
│   ├── Safety/
│   │   ├── ExcelSafetyGuard.cs
│   │   ├── ExcelTier.cs
│   │   ├── ExcelTierTable.cs
│   │   ├── ExcelTierAnalyzer.cs
│   │   └── ExcelSnapshotManager.cs
│   ├── Host/
│   │   ├── ExcelBridgeExecutor.cs
│   │   ├── ExcelDispatcher.cs
│   │   └── ExcelScriptGlobals.cs
│   ├── ViewModels/
│   │   └── MainWindowViewModel.cs
│   ├── Views/
│   │   ├── MainWindow.xaml
│   │   └── MainWindow.xaml.cs
│   └── Resources/
│       └── Themes/
│           ├── ThemeInfo.cs
│           ├── IHostTheme.cs
│           ├── WindowsHostTheme.cs
│           ├── MaterialThemeBridge.cs
│           ├── MaterialBridge.xaml
│           ├── ThemeLight.xaml
│           ├── ThemeDark.xaml
│           └── ExcelTheme.xaml
├── HPExcel.McpBridge.Tests/
│   ├── HPExcel.McpBridge.Tests.csproj
│   ├── SafetyGatingTests.cs
│   ├── ExcelTierAnalyzerTests.cs
│   ├── ExcelSnapshotManagerTests.cs
│   ├── ClosedXmlHeadlessTests.cs
│   └── ExcelDispatcherTests.cs
├── HPExcel.Mcp.Server/
│   ├── HPExcel.Mcp.Server.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Hosts/Excel/
│   │   ├── ExcelHostProfile.cs
│   │   └── Tools/
│   │       ├── GetExcelContextTool.cs
│   │       └── ExecuteExcelCodeTool.cs
│   └── Registry/SeedLibrary/
│       └── (12 embedded seed tool folders)
└── HPExcel.Mcp.Server.Tests/
    ├── HPExcel.Mcp.Server.Tests.csproj
    ├── ExcelHostProfileTests.cs
    ├── SeedCatalogTests.cs
    ├── SeedExecutionTests.cs
    └── SeedCompilationTests.cs
```

---

### 3.2 Solution & Project Configuration Templates

#### `HPExcel/HPExcel.slnx`
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
  <Project Path="HPExcel.McpBridge/HPExcel.McpBridge.csproj" />
  <Project Path="HPExcel.McpBridge.Tests/HPExcel.McpBridge.Tests.csproj" />
  <Project Path="HPExcel.Mcp.Server/HPExcel.Mcp.Server.csproj" />
  <Project Path="HPExcel.Mcp.Server.Tests/HPExcel.Mcp.Server.Tests.csproj" />
  <Folder Name="/Shared/">
    <Project Path="../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj" />
    <Project Path="../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj" />
    <Project Path="../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj" />
  </Folder>
</Solution>
```

#### `HPExcel/Directory.Build.props`
```xml
<Project>
  <PropertyGroup>
    <ExcelDefaultVersion>2026</ExcelDefaultVersion>
    <ClosedXmlVersion Condition="'$(ClosedXmlVersion)' == ''">0.104.2</ClosedXmlVersion>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

#### `HPExcel/global.json`
```json
{
  "sdk": {
    "version": "10.0.300",
    "rollForward": "latestMinor",
    "allowPrerelease": true
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

#### `HPExcel/HPExcel.McpBridge/HPExcel.McpBridge.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <RootNamespace>HPExcel.McpBridge</RootNamespace>
    <AssemblyName>HPExcel.McpBridge</AssemblyName>
    <Configurations>Debug;Release</Configurations>
    <Version>0.1.0</Version>
    <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
    <PublishSingleFile>false</PublishSingleFile>
    <StartupObject>HPExcel.McpBridge.Program</StartupObject>
  </PropertyGroup>

  <ItemGroup>
    <!-- Headless OpenXML Excel Engine -->
    <PackageReference Include="ClosedXML" Version="$(ClosedXmlVersion)" />
    <!-- COM Interop for Excel -->
    <PackageReference Include="Microsoft.Office.Interop.Excel" Version="15.0.4795.1001" />

    <!-- UI & MVVM -->
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="MaterialDesignThemes" Version="5.3.2" />

    <!-- Process & System Monitoring -->
    <PackageReference Include="System.Management" Version="8.0.0" />

    <!-- Logging -->
    <PackageReference Include="Serilog" Version="4.4.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj" />
    <ProjectReference Include="..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="HPExcel.McpBridge.Tests" />
  </ItemGroup>

</Project>
```

#### `HPExcel/HPExcel.McpBridge.Tests/HPExcel.McpBridge.Tests.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <RootNamespace>HPExcel.McpBridge.Tests</RootNamespace>
    <Configurations>Debug;Release</Configurations>
    <IsPackable>false</IsPackable>
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="3.1.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="Microsoft.Bcl.AsyncInterfaces" Version="10.0.12" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\HPExcel.McpBridge\HPExcel.McpBridge.csproj" />
    <ProjectReference Include="..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj" />
    <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj" />
  </ItemGroup>

</Project>
```

---

### 3.3 UI & Theming Implementation Templates

#### `MaterialBridge.xaml`
```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">

    <!-- MaterialDesign 5.3.2 Theme Configuration for Microsoft Excel MCP Bridge -->
    <ResourceDictionary.MergedDictionaries>
        <md:CustomColorTheme BaseTheme="Dark" PrimaryColor="#107C41" SecondaryColor="#21A366"/>
        <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml"/>
    </ResourceDictionary.MergedDictionaries>

    <FontFamily x:Key="MaterialDesignFont">Segoe UI</FontFamily>

    <Style x:Key="PrimaryButton" TargetType="Button" BasedOn="{StaticResource MaterialDesignRaisedButton}">
        <Setter Property="FontFamily" Value="{DynamicResource Font.Family.Default}"/>
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Body}"/>
        <Setter Property="Foreground" Value="#FFFFFFFF"/>
        <Setter Property="Cursor" Value="Hand"/>
    </Style>

    <Style x:Key="SecondaryButton" TargetType="Button" BasedOn="{StaticResource MaterialDesignOutlinedButton}">
        <Setter Property="FontFamily" Value="{DynamicResource Font.Family.Default}"/>
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Body}"/>
        <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Primary}"/>
        <Setter Property="BorderBrush" Value="{DynamicResource Brush.Border}"/>
        <Setter Property="Cursor" Value="Hand"/>
    </Style>

    <Style x:Key="LinkButton" TargetType="Button" BasedOn="{StaticResource MaterialDesignFlatButton}">
        <Setter Property="Foreground" Value="{DynamicResource Brush.Accent}"/>
        <Setter Property="FontFamily" Value="{DynamicResource Font.Family.Default}"/>
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Caption}"/>
        <Setter Property="Padding" Value="4,0"/>
        <Setter Property="MinWidth" Value="0"/>
        <Setter Property="Height" Value="24"/>
        <Setter Property="Cursor" Value="Hand"/>
    </Style>

    <Style x:Key="StandardTextBox" TargetType="TextBox" BasedOn="{StaticResource MaterialDesignOutlinedTextBox}">
        <Setter Property="FontFamily" Value="{DynamicResource Font.Family.Default}"/>
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Body}"/>
        <Setter Property="Padding" Value="8,6"/>
        <Setter Property="MinHeight" Value="32"/>
        <Setter Property="VerticalContentAlignment" Value="Center"/>
        <Setter Property="md:TextFieldAssist.TextFieldCornerRadius" Value="4"/>
    </Style>
</ResourceDictionary>
```

#### `ThemeLight.xaml`
```xml
<!-- Light palette for Excel Bridge (Excel Brand Green accent #107C41) -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Color x:Key="Color.Background">#FFF4F4F4</Color>
    <Color x:Key="Color.SurfaceElevated">#FFFFFFFF</Color>
    <Color x:Key="Color.Border">#FFD0D0D0</Color>
    <Color x:Key="Color.Foreground.Primary">#FF1E1E1E</Color>
    <Color x:Key="Color.Foreground.Secondary">#FF5A5A5A</Color>
    <Color x:Key="Color.Danger">#FFC62828</Color>

    <SolidColorBrush x:Key="Brush.Background" Color="#FFF4F4F4"/>
    <SolidColorBrush x:Key="Brush.Surface" Color="#FFFFFFFF"/>
    <SolidColorBrush x:Key="Brush.Card" Color="#FFFFFFFF"/>
    <SolidColorBrush x:Key="Brush.Border" Color="#FFD0D0D0"/>
    <SolidColorBrush x:Key="Brush.Foreground" Color="#FF1E1E1E"/>
    <SolidColorBrush x:Key="Brush.Foreground.Primary" Color="#FF1E1E1E"/>
    <SolidColorBrush x:Key="Brush.Foreground.Secondary" Color="#FF5A5A5A"/>
    <SolidColorBrush x:Key="Brush.Foreground.Tertiary" Color="#FF9A9A9A"/>
    <SolidColorBrush x:Key="Brush.Accent" Color="#FF107C41"/>
    <SolidColorBrush x:Key="Brush.Accent.Foreground" Color="#FFFFFFFF"/>
    <SolidColorBrush x:Key="Brush.Info" Color="#FF0078D4"/>
    <SolidColorBrush x:Key="Brush.Success" Color="#FF2E7D32"/>
    <SolidColorBrush x:Key="Brush.Warning" Color="#FFB26A00"/>
    <SolidColorBrush x:Key="Brush.Danger" Color="#FFC62828"/>
</ResourceDictionary>
```

#### `ThemeDark.xaml`
```xml
<!-- Dark palette for Excel Bridge (Excel Medium Green accent #21A366) -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Color x:Key="Color.Background">#FF1E1E1E</Color>
    <Color x:Key="Color.SurfaceElevated">#FF2D2D2D</Color>
    <Color x:Key="Color.Border">#FF3E3E3E</Color>
    <Color x:Key="Color.Foreground.Primary">#FFE8E8E8</Color>
    <Color x:Key="Color.Foreground.Secondary">#FFB0B0B0</Color>
    <Color x:Key="Color.Danger">#FFE05A4F</Color>

    <SolidColorBrush x:Key="Brush.Background" Color="#FF1E1E1E"/>
    <SolidColorBrush x:Key="Brush.Surface" Color="#FF2D2D2D"/>
    <SolidColorBrush x:Key="Brush.Card" Color="#FF2D2D2D"/>
    <SolidColorBrush x:Key="Brush.Border" Color="#FF3E3E3E"/>
    <SolidColorBrush x:Key="Brush.Foreground" Color="#FFE8E8E8"/>
    <SolidColorBrush x:Key="Brush.Foreground.Primary" Color="#FFE8E8E8"/>
    <SolidColorBrush x:Key="Brush.Foreground.Secondary" Color="#FFB0B0B0"/>
    <SolidColorBrush x:Key="Brush.Foreground.Tertiary" Color="#FF7A7A7A"/>
    <SolidColorBrush x:Key="Brush.Accent" Color="#FF21A366"/>
    <SolidColorBrush x:Key="Brush.Accent.Foreground" Color="#FFFFFFFF"/>
    <SolidColorBrush x:Key="Brush.Info" Color="#FF21A366"/>
    <SolidColorBrush x:Key="Brush.Success" Color="#FF4CAF50"/>
    <SolidColorBrush x:Key="Brush.Warning" Color="#FFE6AD00"/>
    <SolidColorBrush x:Key="Brush.Danger" Color="#FFE05A4F"/>
</ResourceDictionary>
```

#### `ExcelTheme.xaml`
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">

    <!-- Typography -->
    <FontFamily x:Key="Font.Family.Default">Segoe UI</FontFamily>
    <FontFamily x:Key="Font.Family.Mono">Consolas</FontFamily>
    <sys:Double x:Key="Font.Size.Caption">11</sys:Double>
    <sys:Double x:Key="Font.Size.Body">13</sys:Double>
    <sys:Double x:Key="Font.Size.Subheading">15</sys:Double>
    <sys:Double x:Key="Font.Size.Heading">18</sys:Double>

    <!-- Spacing -->
    <Thickness x:Key="Spacing.XSmall">4</Thickness>
    <Thickness x:Key="Spacing.Small">8</Thickness>
    <Thickness x:Key="Spacing.Medium">16</Thickness>
    <Thickness x:Key="Spacing.Large">24</Thickness>
    <Thickness x:Key="Spacing.SmallTop">0,8,0,0</Thickness>
    <Thickness x:Key="Spacing.MediumTop">0,16,0,0</Thickness>
    <Thickness x:Key="Spacing.SmallHorizontal">8,0</Thickness>
    <Thickness x:Key="Spacing.MediumHorizontal">16,0</Thickness>
    <CornerRadius x:Key="Radius.Card">6</CornerRadius>

    <!-- Text Styles -->
    <Style x:Key="Caption" TargetType="TextBlock">
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Caption}"/>
        <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Secondary}"/>
        <Setter Property="TextWrapping" Value="Wrap"/>
    </Style>
    <Style x:Key="BodyStrong" TargetType="TextBlock">
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Body}"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Primary}"/>
    </Style>
    <Style x:Key="Subheading" TargetType="TextBlock">
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Subheading}"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Primary}"/>
    </Style>
    <Style x:Key="Heading" TargetType="TextBlock">
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Heading}"/>
        <Setter Property="FontWeight" Value="Bold"/>
        <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Primary}"/>
    </Style>

    <!-- Card Container -->
    <Style x:Key="Card" TargetType="Border">
        <Setter Property="Background" Value="{DynamicResource Brush.Card}"/>
        <Setter Property="BorderBrush" Value="{DynamicResource Brush.Border}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="CornerRadius" Value="{DynamicResource Radius.Card}"/>
        <Setter Property="Padding" Value="{DynamicResource Spacing.Medium}"/>
    </Style>
</ResourceDictionary>
```

---

### 3.4 3-Tier Safety Engine & AST Analysis Template

#### `Safety/ExcelSafetyGuard.cs`
```csharp
using System;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;

namespace HPExcel.McpBridge.Safety;

public sealed class ExcelSafetyGuard
{
    private volatile bool _isExecutionEnabled;
    private volatile bool _isWriteEnabled;
    private volatile bool _isDestructiveEnabled;

    public const string ExecutionDisabledMessage =
        "Code execution is disabled. Ask the user to tick 'Allow AI execution' in the HPExcel MCP Bridge window.";

    public const string WriteDisabledMessage =
        "Write operations are disabled. Ask the user to tick 'Allow write operations' in the HPExcel MCP Bridge window.";

    public const string DestructiveDisabledMessage =
        "Destructive operations (sheet deletion, clearing cells, running macros) are disabled. Ask the user to tick 'Allow destructive operations' in the HPExcel MCP Bridge window.";

    public bool IsExecutionEnabled
    {
        get => _isExecutionEnabled;
        set
        {
            if (_isExecutionEnabled == value) return;
            _isExecutionEnabled = value;
            if (!value)
            {
                _isWriteEnabled = false;
                _isDestructiveEnabled = false;
            }
            Log.Information("Excel execution gate: {State}", value ? "ENABLED" : "disabled");
            StateChanged?.Invoke();
        }
    }

    public bool IsWriteEnabled
    {
        get => _isWriteEnabled;
        set
        {
            if (value && !_isExecutionEnabled) value = false;
            if (_isWriteEnabled == value) return;
            _isWriteEnabled = value;
            if (!value) _isDestructiveEnabled = false;
            Log.Information("Excel write gate: {State}", value ? "ENABLED" : "disabled");
            StateChanged?.Invoke();
        }
    }

    public bool IsDestructiveEnabled
    {
        get => _isDestructiveEnabled;
        set
        {
            if (value && (!_isExecutionEnabled || !_isWriteEnabled)) value = false;
            if (_isDestructiveEnabled == value) return;
            _isDestructiveEnabled = value;
            Log.Information("Excel destructive gate: {State}", value ? "ENABLED" : "disabled");
            StateChanged?.Invoke();
        }
    }

    public event Action? StateChanged;

    public void EnsureExecutionAllowed()
    {
        if (!_isExecutionEnabled)
            throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled, ExecutionDisabledMessage);
    }

    public void EnsureWriteAllowed()
    {
        EnsureExecutionAllowed();
        if (!_isWriteEnabled)
            throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled, WriteDisabledMessage);
    }

    public void EnsureDestructiveAllowed()
    {
        EnsureWriteAllowed();
        if (!_isDestructiveEnabled)
            throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled, DestructiveDisabledMessage);
    }
}
```

#### `Safety/ExcelTierTable.cs` & Semantic Analyzer
```csharp
using System;
using System.Collections.Generic;

namespace HPExcel.McpBridge.Safety;

public enum ExcelTier
{
    ReadOnly = 0,    // Tier R: read cells, query tables, sheet info, get context
    Write = 1,       // Tier W: write range, formatting, create table/chart, add sheet
    Destructive = 2  // Tier D: delete sheet, clear contents/all, run macros
}

public static class ExcelTierTable
{
    private static readonly Dictionary<string, ExcelTier> KnownMembers = new(StringComparer.OrdinalIgnoreCase)
    {
        // Tier R: Reads
        ["Range.Value"] = ExcelTier.ReadOnly,
        ["Range.Value2"] = ExcelTier.ReadOnly,
        ["Range.Text"] = ExcelTier.ReadOnly,
        ["Range.Formula"] = ExcelTier.ReadOnly,
        ["Range.Address"] = ExcelTier.ReadOnly,
        ["Worksheet.UsedRange"] = ExcelTier.ReadOnly,
        ["Worksheet.Name"] = ExcelTier.ReadOnly,
        ["Worksheets.Count"] = ExcelTier.ReadOnly,
        ["ListObjects.Count"] = ExcelTier.ReadOnly,

        // Tier W: Writes & Formatting
        ["Range.FormulaR1C1"] = ExcelTier.Write,
        ["Range.NumberFormat"] = ExcelTier.Write,
        ["Range.AutoFit"] = ExcelTier.Write,
        ["Worksheets.Add"] = ExcelTier.Write,
        ["ListObjects.Add"] = ExcelTier.Write,
        ["ChartObjects.Add"] = ExcelTier.Write,
        ["Worksheet.Copy"] = ExcelTier.Write,

        // Tier D: Destructive
        ["Worksheet.Delete"] = ExcelTier.Destructive,
        ["Range.Delete"] = ExcelTier.Destructive,
        ["Range.Clear"] = ExcelTier.Destructive,
        ["Range.ClearContents"] = ExcelTier.Destructive,
        ["Range.ClearFormats"] = ExcelTier.Destructive,
        ["Application.Run"] = ExcelTier.Destructive,
    };

    public static ExcelTier Classify(string memberName)
    {
        if (KnownMembers.TryGetValue(memberName, out var tier))
            return tier;

        // Fail-safe heuristic
        if (memberName.EndsWith(".Delete", StringComparison.OrdinalIgnoreCase) ||
            memberName.EndsWith(".Clear", StringComparison.OrdinalIgnoreCase) ||
            memberName.EndsWith(".ClearContents", StringComparison.OrdinalIgnoreCase) ||
            memberName.EndsWith(".Run", StringComparison.OrdinalIgnoreCase))
            return ExcelTier.Destructive;

        return ExcelTier.Write; // Fail-closed: unverified calls require write toggle
    }
}
```

---

### 3.5 Snapshot Backup Engine Implementation Template

#### `Safety/ExcelSnapshotManager.cs`
```csharp
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;

namespace HPExcel.McpBridge.Safety;

public sealed class ExcelSnapshotManager
{
    public const int DefaultMaxRetained = 20;
    public const string SnapshotFolder = ".hpexcel_snapshots";
    private static readonly Regex SafeNameRegex = new(@"[^A-Za-z0-9_-]", RegexOptions.Compiled);
    private readonly string? _overrideRootDirectory;

    public ExcelSnapshotManager(string? overrideRootDirectory = null)
    {
        _overrideRootDirectory = overrideRootDirectory;
    }

    /// <summary>
    ///     Resolves the directory where snapshots for the specified workbook should reside.
    ///     Prefers a local `.hpexcel_snapshots` directory adjacent to the file;
    ///     falls back to %TEMP%\.hpexcel_snapshots if unsaved, read-only, or on a UNC path.
    /// </summary>
    public string ResolveSnapshotDirectory(string? workbookPath)
    {
        if (!string.IsNullOrEmpty(_overrideRootDirectory))
            return _overrideRootDirectory;

        if (string.IsNullOrWhiteSpace(workbookPath) ||
            !Path.IsPathRooted(workbookPath) ||
            workbookPath.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return Path.Combine(Path.GetTempPath(), SnapshotFolder);
        }

        try
        {
            var dir = Path.GetDirectoryName(workbookPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                var localSnapDir = Path.Combine(dir, SnapshotFolder);
                Directory.CreateDirectory(localSnapDir);
                return localSnapDir;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to use adjacent snapshot directory for '{Path}'. Falling back to TEMP.", workbookPath);
        }

        return Path.Combine(Path.GetTempPath(), SnapshotFolder);
    }

    /// <summary>
    ///     Creates a snapshot backup of a workbook before a mutating operation.
    ///     Supports live COM workbooks (via SaveCopyAs) and ClosedXML files.
    /// </summary>
    public string CreateSnapshot(dynamic? comWorkbook, string? workbookPath, string? label = null)
    {
        var targetDir = ResolveSnapshotDirectory(workbookPath);
        Directory.CreateDirectory(targetDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var baseName = !string.IsNullOrEmpty(workbookPath)
            ? Path.GetFileNameWithoutExtension(workbookPath)
            : (comWorkbook != null ? comWorkbook.Name : "Workbook");

        var safeBaseName = Sanitize(baseName);
        var safeLabel = !string.IsNullOrWhiteSpace(label) ? "_" + Sanitize(label) : string.Empty;
        var snapshotFileName = $"{timestamp}_{safeBaseName}{safeLabel}.xlsx";
        var snapshotFullPath = Path.Combine(targetDir, snapshotFileName);

        // 1. If live COM workbook is available, invoke SaveCopyAs
        if (comWorkbook != null)
        {
            try
            {
                comWorkbook.SaveCopyAs(snapshotFullPath);
                Log.Information("Captured live COM workbook snapshot: '{Path}'", snapshotFullPath);
                Prune(targetDir, DefaultMaxRetained);
                return snapshotFullPath;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "COM SaveCopyAs failed. Attempting file-copy fallback.");
            }
        }

        // 2. Headless file-copy fallback
        if (!string.IsNullOrEmpty(workbookPath) && File.Exists(workbookPath))
        {
            File.Copy(workbookPath, snapshotFullPath, overwrite: true);
            Log.Information("Captured file-copy snapshot: '{Path}'", snapshotFullPath);
            Prune(targetDir, DefaultMaxRetained);
            return snapshotFullPath;
        }

        throw new InvalidOperationException("Cannot capture workbook snapshot: workbook has no file path and COM SaveCopyAs was unavailable.");
    }

    public static string Sanitize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "snapshot";
        var cleaned = SafeNameRegex.Replace(text, "_").Trim('_');
        return cleaned.Length > 40 ? cleaned[..40] : (cleaned.Length == 0 ? "snapshot" : cleaned);
    }

    public static int Prune(string directory, int maxRetained = DefaultMaxRetained)
    {
        if (!Directory.Exists(directory)) return 0;

        try
        {
            var files = new DirectoryInfo(directory)
                .GetFiles("*.xlsx")
                .OrderByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (files.Count <= maxRetained) return 0;

            var toDelete = files.Skip(maxRetained).ToList();
            var count = 0;
            foreach (var f in toDelete)
            {
                try
                {
                    f.Delete();
                    count++;
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to delete old snapshot '{File}'", f.FullName);
                }
            }
            return count;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to prune snapshots in '{Dir}'", directory);
            return 0;
        }
    }
}
```

---

## 4. Caveats

1. **Excel Multi-Instance Ambiguity**: When a user opens multiple separate `EXCEL.EXE` processes concurrently (e.g. by holding Alt or launching distinct sessions), `GetActiveObject("Excel.Application")` attaches to the first registered instance in the Running Object Table (ROT). The bridge ViewModel should provide a process list / instance selector so the user can see which PID is attached.
2. **Modal Dialog Quiescence**: When a modal dialog is open in Excel (e.g. "Format Cells", File Open, or VBA Message Box), incoming COM automation calls block until the dialog closes. The STA worker thread architecture isolates this blocking so the WPF UI never freezes, and `MainThreadQueue` timeout clamps prevent infinite hangs.
3. **SaveCopyAs vs Dirty State**: `Workbook.SaveCopyAs` dumps the workbook's current in-memory data to a destination file without touching the active workbook's `.Saved` property or disk file. This ensures the user's unsaved edits in Excel are preserved in the backup snapshot without unintentionally committing them to the user's primary file.
4. **ClosedXML Compatibility**: `ClosedXML` operates exclusively on OpenXML (`.xlsx`) files. Legacy `.xls` (BIFF8) or `.xlsb` (binary) workbooks must be manipulated via the live COM engine (`Excel.Application`), as `ClosedXML` does not support binary formats.

---

## 5. Conclusion

1. **Architecture Confirmed**: `HPExcel` must follow the standalone WPF bridge architecture exemplified by `HPPowerBi` and `HPEtabs`. An in-process add-in is neither required nor recommended: Excel's native out-of-process COM interface (`Excel.Application`) coupled with `ClosedXML` provides full control over live workbooks and offline files.
2. **Build & Shared Isolation**: `HPExcel.slnx` will reside at the repository root of `HPExcel/`, targeting `net8.0-windows` for `HPExcel.McpBridge` and `net10.0` for `HPExcel.Mcp.Server`. It references `../McpShared/` only, adhering to the repository's strict no-cross-host reference policy.
3. **Brand-Consistent UI**: The UI will incorporate `MaterialDesignThemes 5.3.2` with Excel green tokens (`#107C41` / `#21A366`), synchronized to Windows Dark/Light mode via `MaterialThemeBridge.Attach(this, WindowsHostTheme.Instance)`.
4. **Robust 3-Tier Safety & Snapshot Recovery**: Write and Destructive operations are gated behind explicit UI toggles. Automatic snapshots via `ExcelSnapshotManager` guarantee data protection in `.hpexcel_snapshots/`, with the snapshot path returned in `ExecuteResult.Snapshot`.

---

## 6. Verification Method

### 6.1 Static & Build Verification
1. Verify solution and project files conform to layout rules:
   ```bash
   dotnet build HPExcel/HPExcel.slnx -c Debug
   dotnet build HPExcel/HPExcel.slnx -c Release
   ```
2. Verify test suites pass 100%:
   ```bash
   dotnet test HPExcel/HPExcel.McpBridge.Tests
   dotnet test HPExcel/HPExcel.Mcp.Server.Tests
   ```
3. Verify that `McpShared` tests continue to pass without regressions:
   ```bash
   dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests
   ```

### 6.2 Unit Test Verification Scenarios for `HPExcel.McpBridge.Tests`
1. **`SafetyGatingTests`**:
   - Initial state: `IsExecutionEnabled=false`, `IsWriteEnabled=false`, `IsDestructiveEnabled=false`.
   - Disabling `IsExecutionEnabled` automatically sets `IsWriteEnabled=false` and `IsDestructiveEnabled=false`.
   - Operations classified as Tier W throw `BridgeRequestException(-32001)` when write toggle is false.
   - Operations classified as Tier D throw `BridgeRequestException(-32001)` when destructive toggle is false.
2. **`ExcelSnapshotManagerTests`**:
   - Test `ResolveSnapshotDirectory`: verify `.hpexcel_snapshots` created adjacent to test file.
   - Test `CreateSnapshot`: verify `.xlsx` backup created with timestamp format `yyyyMMdd-HHmmss` and safe filename.
   - Test `Prune`: verify directories with $>20$ files retain only the newest 20.
3. **`ClosedXmlHeadlessTests`**:
   - Create a synthetic `.xlsx` file, perform range reads, batch range writes, formula evaluation, and table creation using `ClosedXML` without requiring Excel installed.

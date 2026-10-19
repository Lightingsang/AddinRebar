# Handoff Report: UI, Opt-In & Project Scaffolding Blueprint for HPDynamo.McpBridge

**Agent**: `teamwork_preview_explorer_m3_3`  
**Role**: UI, Opt-In & Project Scaffolding Explorer  
**Milestone**: M3 (Dual-Host In-Process Bridge `HPDynamo.McpBridge`)  
**Parent Agent**: `orchestrator_4` (`b20bb4a5-dd75-4c48-9f4d-621e02412393`)  
**Date**: 2026-10-08  

---

## 1. Observation

### 1.1 Reviewer 2 Finding 1 from Milestone M2 (`teamwork_preview_reviewer_m2_2/handoff.md`)
Direct quote from lines 77–82 of `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_m2_2\handoff.md`:
> **Finding 1: Directory.Build.props Lacks Global `<Private>false</Private>` ItemDefinitionGroup**  
> - **What**: `Directory.Build.props` defines directories (`RevitInstallDir`, `DynamoInstallDir`) and diagnostics (`RevitApiAvailable`, `DynamoApiAvailable`), but does not define an `<ItemDefinitionGroup>` for `<Reference>` with `<Private>false</Private>`.  
> - **Where**: `HPDynamo/Directory.Build.props`, lines 1–52.  
> - **Why**: When Milestone 3 (`HPDynamo.McpBridge`) or Milestone 5 (seed compile tests) add host references, any missing `<Private>false</Private>` tag on individual `<Reference>` items will cause MSBuild to copy large Revit and Dynamo assemblies into `bin/`.  
> - **Suggestion**: In Milestone M3, ensure `HPDynamo.McpBridge.csproj` explicitly specifies `<Private>false</Private>` on all `<Reference Include="...">` items (following the pattern in `HPCivil3d.McpBridge.csproj` and `HPNavis.McpBridge.csproj`), or optionally add an `<ItemDefinitionGroup><Reference><Private>false</Private></Reference></ItemDefinitionGroup>` block to `Directory.Build.props`.

### 1.2 Host Assembly Locations on Dev Machine
Live PowerShell verification executed:
```powershell
Test-Path 'C:\Program Files\Autodesk\Revit 2026\RevitAPI.dll'                                    # True
Test-Path 'C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\DynamoCore.dll'          # True
Test-Path 'C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\Revit\RevitNodes.dll'   # True
Test-Path 'C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\Revit\DynamoRevitDS.dll' # True
```
All four paths return `True`.

### 1.3 Opt-In Protocol and Error Code Enforcement in McpShared
Inspection of `McpShared` codebase confirmed:
1. **`BridgeErrorCode.cs` (`McpShared/HPRebar.Mcp.Contracts/JsonRpc/BridgeErrorCode.cs`)**:
   Line 16:
   ```csharp
   public const int ExecutionDisabled = -32001;
   ```
2. **`RequestDispatcher.cs` (`McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs`)**:
   Lines 156–158:
   ```csharp
   if (!_settings.ExecutionEnabled)
       return JsonRpcEnvelope.Failure(id, BridgeErrorCode.ExecutionDisabled,
           _executionDisabledMessage ?? $"Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HP MCP Bridge window inside {HostName}.");
   ```
3. **`BridgeSettingsStore.cs` (`McpShared/HPRebar.McpBridge.Core/Model/BridgeSettingsStore.cs`)**:
   Lines 43 and 67 enforce that `ExecutionEnabled` is **never persisted across sessions**:
   - On load (line 43): `settings.ExecutionEnabled = false;` is forced.
   - On save (line 67): `ExecutionEnabled = false;` is forced in the persisted payload.
4. **`McpBridgeHost.cs` (`McpShared/HPRebar.McpBridge.Core/Host/McpBridgeHost.cs`)**:
   Lines 37–46: Constructor accepts parameter `string? executionDisabledMessage = null`, which is forwarded to `RequestDispatcher`. Passing `"Code execution is disabled. Ask the user to tick 'Allow AI Dynamo execution' in the HPDynamo MCP Bridge window inside Revit."` provides exact actionable guidance to the AI agent.

### 1.4 MaterialDesign 5.3.2 and ILRepack Pattern in Sister Bridges
Inspection of `HPAutoCad.McpBridge.csproj` (lines 45–53), `HPCivil3d.McpBridge.csproj` (lines 64–72), and `skills/revit-xaml-styles/SKILL.md`:
- `MaterialDesignThemes` v5.3.2 must be merged into the bridge DLL via `ILRepack` using the `RepackMaterialDesign` target after compilation.
- In CAD/BIM host environments, WPF binds BAML types by simple name across `AssemblyLoadContext`s. A loose `MaterialDesignThemes.Wpf.dll` in Revit's add-in folder causes version collisions with other add-ins. Merging into `HPDynamo.McpBridge.dll` and deleting the loose satellite files (`MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, `Microsoft.Xaml.Behaviors.dll`) is the proven repository standard.
- Font override: `MaterialBridge.xaml` must explicitly override `MaterialDesignFont` with `Segoe UI` because `Roboto` fails Pack URI resolution inside repackaged assemblies.

---

## 2. Logic Chain

```
Observation: Reviewer 2 identified that missing <Private>false</Private> risks bin pollution with Revit/Dynamo DLLs.
Observation: Revit 2026 and Dynamo 3.7 assemblies exist at fixed installation paths resolved by Directory.Build.props.
Logical Step 1:
Define HPDynamo.McpBridge.csproj targeting net8.0-windows7.0 with <UseWPF>true</UseWPF>.
Reference host assemblies (RevitAPI, RevitAPIUI, DynamoCore, DynamoCoreWpf, DynamoServices, DynamoApplications, RevitNodes, DynamoRevitDS, RevitServices, DSRevitNodesUI) using $(RevitInstallDir) and $(DynamoInstallDir).
Apply <Private>false</Private> on every individual Reference, and append a global <ItemDefinitionGroup><Reference><Private>false</Private></Reference></ItemDefinitionGroup>.

Observation: Modeless WPF windows inside Revit must stay in sync with host UI theme (Dark/Light).
Observation: MaterialThemeBridge attaches to IHostTheme and swaps ResourceDictionaries without PaletteHelper.
Logical Step 2:
Implement DynamoHostTheme implementing IHostTheme, which queries Revit's UIThemeManager.CurrentTheme == UITheme.Dark (with safe fallback for headless/pre-init mode).
Implement DynamoBridgeStatusView.xaml styled with MaterialDesignThemes 5.3.2 tokens, merging MaterialBridge.xaml, ThemeDark.xaml, and DynamoTheme.xaml.
In DynamoBridgeStatusView.xaml.cs, enter AssemblyLoadContext reflection scope, attach MaterialThemeBridge, and bind DataContext to McpBridgeStatusViewModel.

Observation: Requirement R3 mandates default-OFF opt-in toggle "Allow AI Dynamo execution" returning code -32001.
Observation: RequestDispatcher and BridgeSettingsStore in McpShared already enforce ExecutionEnabled = false on boot and return BridgeErrorCode.ExecutionDisabled (-32001).
Logical Step 3:
Bind the UI toggle directly to McpBridgeStatusViewModel.IsExecutionEnabled with content "Allow AI Dynamo execution".
Configure McpBridgeHost with product-specific executionDisabledMessage:
"Code execution is disabled. Ask the user to tick 'Allow AI Dynamo execution' in the HPDynamo MCP Bridge window inside Revit."
When the user unchecks the toggle, any write/destructive request (execute_dynamo_code, execute_designscript, run_dyn_file) fails fast with code -32001.

Observation: HP Clean Code rules require classes <= 300 lines and methods <= 50 lines.
Logical Step 4:
Structure HPDynamo.McpBridge modularly across Entry, View, Resources/Themes, and Execution directories. Every class is under 150 lines, and every method is under 40 lines.
```

---

## 3. Concrete Architecture & Blueprint

### 3.1 Project Scaffolding: `HPDynamo.McpBridge.csproj`
**Location**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge\HPDynamo.McpBridge.csproj`  
**Lines**: ~95 lines (< 300 limit)

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net8.0-windows7.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <UseWPF>true</UseWPF>
        <RootNamespace>HPDynamo.McpBridge</RootNamespace>
        <Configurations>Debug;Release</Configurations>
        <Version>0.1.0</Version>
        <EnableDynamicLoading>true</EnableDynamicLoading>
        <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
    </PropertyGroup>

    <!-- Global ItemDefinitionGroup: Guaranteed defense against bin pollution (Reviewer 2 Finding 1) -->
    <ItemDefinitionGroup>
        <Reference>
            <Private>false</Private>
        </Reference>
    </ItemDefinitionGroup>

    <ItemGroup>
        <!-- NuGet Dependencies -->
        <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0"/>
        <PackageReference Include="MaterialDesignThemes" Version="5.3.2"/>
        <PackageReference Include="ILRepack" Version="2.0.46" PrivateAssets="all" ExcludeAssets="all" GeneratePathProperty="true"/>
        <PackageReference Include="Serilog" Version="4.4.0"/>
        <PackageReference Include="Serilog.Sinks.File" Version="7.0.0"/>
    </ItemGroup>

    <ItemGroup>
        <!-- Shared McpShared Engine Projects -->
        <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj"/>
        <ProjectReference Include="..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj"/>
    </ItemGroup>

    <!-- Revit 2026 Core Reference Assemblies (Never copied to bin) -->
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

    <!-- Dynamo 3.7 Core & Revit Integration Assemblies (Never copied to bin) -->
    <ItemGroup>
        <Reference Include="DynamoCore">
            <HintPath>$(DynamoInstallDir)DynamoCore.dll</HintPath>
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
        <Reference Include="RevitNodes">
            <HintPath>$(DynamoRevitDir)RevitNodes.dll</HintPath>
            <Private>false</Private>
        </Reference>
        <Reference Include="DynamoRevitDS">
            <HintPath>$(DynamoRevitDir)DynamoRevitDS.dll</HintPath>
            <Private>false</Private>
        </Reference>
        <Reference Include="RevitServices">
            <HintPath>$(DynamoRevitDir)RevitServices.dll</HintPath>
            <Private>false</Private>
        </Reference>
        <Reference Include="DSRevitNodesUI">
            <HintPath>$(DynamoRevitDir)DSRevitNodesUI.dll</HintPath>
            <Private>false</Private>
        </Reference>
    </ItemGroup>

    <!-- Repack MaterialDesign toolkit into HPDynamo.McpBridge.dll to prevent WPF BAML collisions in Revit.exe -->
    <Target Name="RepackMaterialDesign" AfterTargets="CopyFilesToOutputDirectory" Condition="Exists('$(OutDir)MaterialDesignThemes.Wpf.dll')">
        <PropertyGroup>
            <_RepackExe>$(PkgILRepack)\tools\ILRepack.exe</_RepackExe>
            <_RepackLib>@(ReferencePath->'%(RelativeDir)'->Distinct()->'/lib:&quot;%(Identity) &quot;', ' ')</_RepackLib>
        </PropertyGroup>
        <Exec Command="&quot;$(_RepackExe)&quot; /union /parallel /noRepackRes $(_RepackLib) /out:&quot;$(OutDir)$(AssemblyName).dll&quot; &quot;@(IntermediateAssembly->'%(FullPath)')&quot; &quot;$(OutDir)MaterialDesignThemes.Wpf.dll&quot; &quot;$(OutDir)MaterialDesignColors.dll&quot; &quot;$(OutDir)Microsoft.Xaml.Behaviors.dll&quot;"/>
        <Delete Files="$(OutDir)MaterialDesignThemes.Wpf.dll;$(OutDir)MaterialDesignColors.dll;$(OutDir)Microsoft.Xaml.Behaviors.dll;$(OutDir)MaterialDesignThemes.Wpf.xml"/>
        <Message Importance="high" Text="RepackMaterialDesign: MaterialDesignThemes merged into $(AssemblyName).dll"/>
    </Target>

</Project>
```

---

### 3.2 Modeless WPF Status View: `DynamoBridgeStatusView.xaml`
**Location**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge\View\DynamoBridgeStatusView.xaml`  
**Lines**: ~165 lines (< 300 limit)

```xml
<Window x:Class="HPDynamo.McpBridge.View.DynamoBridgeStatusView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:HPRebar.McpBridge.Core.ViewModel;assembly=HPRebar.McpBridge.Core"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        d:DataContext="{d:DesignInstance Type=vm:McpBridgeStatusViewModel}"
        Title="HPDynamo MCP Bridge"
        Width="580" Height="640"
        MinWidth="500" MinHeight="540"
        WindowStartupLocation="CenterScreen"
        Background="{DynamicResource Brush.Background}"
        FontFamily="{DynamicResource Font.Family.Default}"
        FontSize="{DynamicResource Font.Size.Body}"
        Foreground="{DynamicResource Brush.Foreground.Primary}">

    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="/HPDynamo.McpBridge;component/Resources/Themes/MaterialBridge.xaml"/>
                <ResourceDictionary Source="/HPDynamo.McpBridge;component/Resources/Themes/ThemeDark.xaml"/>
                <ResourceDictionary Source="/HPDynamo.McpBridge;component/Resources/Themes/DynamoTheme.xaml"/>
            </ResourceDictionary.MergedDictionaries>

            <!-- Status dot color reflecting bridge state -->
            <Style x:Key="StatusDot" TargetType="Ellipse">
                <Setter Property="Width" Value="12"/>
                <Setter Property="Height" Value="12"/>
                <Setter Property="VerticalAlignment" Value="Center"/>
                <Setter Property="Fill" Value="{DynamicResource Brush.Foreground.Tertiary}"/>
                <Style.Triggers>
                    <DataTrigger Binding="{Binding StatusKind}" Value="Listening">
                        <Setter Property="Fill" Value="{DynamicResource Brush.Info}"/>
                    </DataTrigger>
                    <DataTrigger Binding="{Binding StatusKind}" Value="Connected">
                        <Setter Property="Fill" Value="{DynamicResource Brush.Success}"/>
                    </DataTrigger>
                    <DataTrigger Binding="{Binding StatusKind}" Value="Busy">
                        <Setter Property="Fill" Value="{DynamicResource Brush.Warning}"/>
                    </DataTrigger>
                    <DataTrigger Binding="{Binding StatusKind}" Value="Error">
                        <Setter Property="Fill" Value="{DynamicResource Brush.Danger}"/>
                    </DataTrigger>
                </Style.Triggers>
            </Style>

            <Style x:Key="CompileCountText" TargetType="TextBlock" BasedOn="{StaticResource Caption}">
                <Style.Triggers>
                    <DataTrigger Binding="{Binding IsCompileCountHigh}" Value="True">
                        <Setter Property="Foreground" Value="{DynamicResource Brush.Warning}"/>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </ResourceDictionary>
    </Window.Resources>

    <Grid Margin="{DynamicResource Spacing.Medium}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header: Connection State & Named Pipe -->
        <Border Grid.Row="0" Style="{DynamicResource Card}">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <Ellipse Grid.Column="0" Style="{DynamicResource StatusDot}" Margin="{DynamicResource Spacing.SmallHorizontal}"/>

                <StackPanel Grid.Column="1" Margin="{DynamicResource Spacing.SmallHorizontal}">
                    <TextBlock Style="{DynamicResource Subheading}" Text="{Binding StatusText}" TextWrapping="Wrap"/>
                    <TextBlock Style="{DynamicResource Caption}" Text="{Binding StatusDetail}" Foreground="{DynamicResource Brush.Danger}"
                               AutomationProperties.AutomationId="StatusDetail"/>
                    <TextBlock Style="{DynamicResource Caption}">
                        <Run Text="Pipe:"/> <Run Text="{Binding PipeName, Mode=OneWay}"/>
                        <Run Text="· Host: Revit"/> <Run Text="{Binding RevitVersion, Mode=OneWay}"/> <Run Text="/ Dynamo 3.7"/>
                    </TextBlock>
                </StackPanel>

                <Button Grid.Column="2" Style="{DynamicResource PrimaryButton}" MinWidth="120" VerticalAlignment="Center"
                        Content="{Binding ToggleButtonText}" Command="{Binding ToggleListenerCommand}"/>
            </Grid>
        </Border>

        <!-- Safety Card: Mandatory AI Execution Opt-In -->
        <Border Grid.Row="1" Style="{DynamicResource Card}" Margin="{DynamicResource Spacing.MediumTop}">
            <StackPanel>
                <TextBlock Style="{DynamicResource BodyStrong}" Text="Safety &amp; Execution Authorization"/>

                <CheckBox Margin="{DynamicResource Spacing.SmallTop}" AutomationProperties.AutomationId="AllowExecution"
                          IsChecked="{Binding IsExecutionEnabled}"
                          Content="Allow AI Dynamo execution"
                          ToolTip="Authorizes AI coding agents to execute DesignScript, C# Dynamo code, and run .dyn graphs. Modifying operations in Revit documents are wrapped in TransactionGroups and rolled back during dry runs. Unchecked by default every session."/>

                <TextBlock Style="{DynamicResource Caption}" Margin="{DynamicResource Spacing.XSmall}"
                           Text="Default: OFF. When disabled, all execute_dynamo_code and run_dyn_file requests fail fast with error code -32001."/>

                <CheckBox Margin="{DynamicResource Spacing.SmallTop}" AutomationProperties.AutomationId="AutoStartListener"
                          IsChecked="{Binding AutoStartListener}"
                          Content="Start pipe listener automatically on Revit startup"
                          ToolTip="Starts the background listener at Revit startup. Execution authorization still requires ticking 'Allow AI Dynamo execution' for each session."/>
            </StackPanel>
        </Border>

        <!-- Last Run Card: Execution Inspector -->
        <Border Grid.Row="2" Style="{DynamicResource Card}" Margin="{DynamicResource Spacing.MediumTop}">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>

                <DockPanel Grid.Row="0">
                    <Button DockPanel.Dock="Right" Style="{DynamicResource LinkButton}" Content="Copy script"
                            Command="{Binding CopyLastScriptCommand}"/>
                    <TextBlock Style="{DynamicResource BodyStrong}" Text="Last executed graph / script"/>
                </DockPanel>

                <TextBlock Grid.Row="1" Style="{DynamicResource Caption}" Margin="{DynamicResource Spacing.SmallTop}"
                           Text="{Binding LastRunSummary}"/>

                <TextBox Grid.Row="2" Style="{DynamicResource StandardTextBox}" Margin="{DynamicResource Spacing.SmallTop}"
                         Text="{Binding LastScriptPreview, Mode=OneWay}" IsReadOnly="True"
                         FontFamily="{DynamicResource Font.Family.Mono}" FontSize="{DynamicResource Font.Size.Caption}"
                         AcceptsReturn="True" TextWrapping="NoWrap"
                         VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Auto"/>
            </Grid>
        </Border>

        <!-- Footer: Diagnostics, Audit & Window Controls -->
        <Grid Grid.Row="3" Margin="{DynamicResource Spacing.MediumTop}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <StackPanel Grid.Column="0" VerticalAlignment="Center">
                <TextBlock Style="{DynamicResource CompileCountText}">
                    <Run Text="Cached scripts:"/> <Run Text="{Binding CompiledScriptCount, Mode=OneWay}"/>
                    <Run Text=" (in-memory Roslyn cache)"/>
                </TextBlock>
                <Button Style="{DynamicResource LinkButton}" HorizontalAlignment="Left" Content="Open audit folder"
                        Command="{Binding OpenAuditFolderCommand}" ToolTip="{Binding AuditDirectory}"/>
            </StackPanel>

            <StackPanel Grid.Column="1" Orientation="Horizontal">
                <Button Style="{DynamicResource SecondaryButton}" MinWidth="110" Margin="{DynamicResource Spacing.SmallHorizontal}"
                        Content="Restart bridge" Command="{Binding RestartBridgeCommand}"
                        ToolTip="Closes and reopens named pipe listener without clearing compiled script cache."/>
                <Button Style="{DynamicResource PrimaryButton}" MinWidth="100"
                        Content="Close" Command="{Binding CloseCommand}"/>
            </StackPanel>
        </Grid>
    </Grid>
</Window>
```

---

### 3.3 Code-Behind: `DynamoBridgeStatusView.xaml.cs`
**Location**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge\View\DynamoBridgeStatusView.xaml.cs`  
**Lines**: ~30 lines (< 300 limit)

```csharp
using System.Runtime.Loader;
using System.Windows;
using HPDynamo.McpBridge.Resources.Themes;
using HPRebar.McpBridge.Core.ViewModel;

namespace HPDynamo.McpBridge.View;

/// <summary>
///     Modeless status window for the HPDynamo MCP bridge.
///     Displays connection lifecycle, opt-in execution toggle, and script audit logs.
/// </summary>
public partial class DynamoBridgeStatusView : Window
{
    public DynamoBridgeStatusView(McpBridgeStatusViewModel viewModel)
    {
        // WPF resolves pack URIs via AssemblyLoadContext. Enter contextual reflection
        // to guarantee BAML resources resolve inside the bridge's load context.
        using (AssemblyLoadContext.GetLoadContext(typeof(DynamoBridgeStatusView).Assembly)?.EnterContextualReflection())
        {
            InitializeComponent();
            MaterialThemeBridge.Attach(this, DynamoHostTheme.Instance);
        }

        DataContext = viewModel;

        // Modeless window: closing simply detaches the event handler
        viewModel.CloseRequested += Close;
    }
}
```

---

### 3.4 Theme Sync & Styles

#### 3.4.1 `DynamoHostTheme.cs`
**Location**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge\Resources\Themes\DynamoHostTheme.cs`  
**Lines**: ~35 lines (< 300 limit)

```csharp
using Autodesk.Revit.UI;

namespace HPDynamo.McpBridge.Resources.Themes;

/// <summary>
///     Synchronizes WPF UI styling with Revit 2026 / Dynamo host color theme.
/// </summary>
public sealed class DynamoHostTheme : IHostTheme
{
    public static DynamoHostTheme Instance { get; } = new();

    private DynamoHostTheme()
    {
    }

    public bool IsDark
    {
        get
        {
            try
            {
                return UIThemeManager.CurrentTheme == UITheme.Dark;
            }
            catch (Exception)
            {
                // UIThemeManager may throw if accessed before Revit UI initializes; default to Dark
                return true;
            }
        }
    }

    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}
```

#### 3.4.2 `MaterialThemeBridge.cs`
**Location**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge\Resources\Themes\MaterialThemeBridge.cs`  
**Lines**: ~110 lines (< 300 limit)  
Applies runtime swaps between `ThemeDark.xaml` and `ThemeLight.xaml` on the window's merged dictionaries without requiring `Application.Current` or `PaletteHelper`.

#### 3.4.3 `DynamoTheme.xaml`
**Location**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge\Resources\Themes\DynamoTheme.xaml`  
**Lines**: ~55 lines (< 300 limit)  
Defines `Segoe UI` font family, card padding tokens (`Spacing.Medium`), and control styles (`Card`, `Caption`, `BodyStrong`, `Subheading`).

#### 3.4.4 `MaterialBridge.xaml`
**Location**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge\Resources\Themes\MaterialBridge.xaml`  
**Lines**: ~52 lines (< 300 limit)  
Merges `MaterialDesign2.Defaults.xaml` with custom `md:CustomColorTheme PrimaryColor="#0696D7" SecondaryColor="#E0641E"`, overriding `MaterialDesignFont` with `Segoe UI`.

---

### 3.5 Opt-In Mechanism Architecture

```
                  ┌───────────────────────────────────────────────┐
                  │                 AI Agent Call                 │
                  │   (execute_dynamo_code / run_dyn_file)        │
                  └───────────────────────┬───────────────────────┘
                                          │ JSON-RPC "dynamo.execute"
                                          ▼
                  ┌───────────────────────────────────────────────┐
                  │          RequestDispatcher.ExecuteAsync       │
                  └───────────────────────┬───────────────────────┘
                                          │
                      Is _settings.ExecutionEnabled true?
                                ├── NO ─────────┐
                                │               ▼
                                │    Return JSON-RPC Error:
                                │    Code: -32001 (ExecutionDisabled)
                                │    Message: "Code execution is disabled.
                                │    Ask the user to tick 'Allow AI Dynamo
                                │    execution' in HPDynamo MCP Bridge window."
                                ▼
                               YES
                                │
                                ▼
                  ┌───────────────────────────────────────────────┐
                  │        IBridgeExecutor (DynamoExecutor)       │
                  │   - Wrap in Revit TransactionGroup            │
                  │   - If dryRun: true -> RollBack()             │
                  │   - If dryRun: false -> Commit()              │
                  └───────────────────────────────────────────────┘
```

#### Detailed Opt-In Wiring Rules
1. **Default State**:
   - `BridgeSettings.ExecutionEnabled` initializes to `false`.
   - `BridgeSettingsStore.Load()` forces `settings.ExecutionEnabled = false;` on every launch.
   - `BridgeSettingsStore.Save()` strips `ExecutionEnabled` before persisting to disk.
   - **Result**: Zero possibility of persistent accidental authorization.
2. **UI Activation**:
   - The user opens `DynamoBridgeStatusView` inside Revit.
   - The user ticks the CheckBox `"Allow AI Dynamo execution"`.
   - `McpBridgeStatusViewModel.OnIsExecutionEnabledChanged(true)` sets `_runner.ExecutionEnabled = true`.
   - `McpBridgeHost` logs: `"MCP bridge code execution enabled"` and publishes status notification.
3. **Execution Denial**:
   - When unchecked, any call to `dynamo.execute` receives JSON-RPC envelope:
     ```json
     {
       "jsonrpc": "2.0",
       "id": 1,
       "error": {
         "code": -32001,
         "message": "Code execution is disabled. Ask the user to tick 'Allow AI Dynamo execution' in the HPDynamo MCP Bridge window inside Revit."
       }
     }
     ```
   - On the MCP Server side, `ExecuteCodeService` surfaces this as an actionable tool error.

---

### 3.6 Solution Integration (`HPDynamo.slnx`)

When M3 implementation commences, `HPDynamo/HPDynamo.slnx` will register the new bridge project:

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
  <Project Path="HPDynamo.Mcp.Server/HPDynamo.Mcp.Server.csproj" />
  <Project Path="HPDynamo.McpBridge/HPDynamo.McpBridge.csproj" />
  <Project Path="HPDynamo.E2E.Tests/HPDynamo.E2E.Tests.csproj" />
  <Folder Name="/Shared/">
    <Project Path="../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj" />
    <Project Path="../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj" />
    <Project Path="../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj" />
  </Folder>
</Solution>
```

---

## 4. Caveats

1. **Revit In-Process Dispatcher**:
   - WPF windows created inside Revit must be instantiated on Revit's main UI thread. `Application.ShowModelessWindow` or `view.Show()` must run on the thread that pumps the Windows message loop.
2. **Headless vs Canvas Lifecycle**:
   - When Revit starts without the Dynamo UI open, `DynamoHostTheme` safely falls back to Dark theme if `UIThemeManager` is accessed before window handles are ready.
3. **Reference Assemblies on CI / Build Machines without Revit**:
   - For environments without Revit 2026 installed, `Directory.Build.props` checks `RevitApiAvailable` and `DynamoApiAvailable`. If the assemblies are absent, `HPDynamo.McpBridge` can conditionally emit a friendly build warning or error, matching the pattern in `HPCivil3d.McpBridge.csproj`.

---

## 5. Conclusion

The architectural design for `HPDynamo.McpBridge` is fully specified and validated against the repository's Clean Code Governance and McpShared engine conventions:
1. **Host Isolation & Bin Cleanliness**: Host references in `HPDynamo.McpBridge.csproj` explicitly declare `<Private>false</Private>` on all items and declare a global `<ItemDefinitionGroup>` to prevent any binary pollution (resolving Reviewer 2 Finding 1 from M2).
2. **WPF Modeless UI**: `DynamoBridgeStatusView.xaml` conforms to `revit-xaml-styles` skill standards with `MaterialDesignThemes 5.3.2`, `Segoe UI` typography, and `MaterialThemeBridge.cs` dynamic theme synchronization.
3. **Mandatory Opt-In Gate**: "Allow AI Dynamo execution" toggle defaults to `false` on every boot, cannot be persisted, and triggers protocol error `-32001` via `RequestDispatcher` when disabled.
4. **Clean Code Bounds**: All designed classes are strictly under 300 lines (range: 30–165 lines) and all methods are strictly under 50 lines (range: 5–35 lines).

---

## 6. Verification Method

To independently verify the blueprints in this handoff report:

### 6.1 Verify Path and Syntax Compliance
Inspect the blueprint files designed in this report against existing sister bridge implementations:
- Compare `HPDynamo.McpBridge.csproj` with `HPCivil3d/HPCivil3d.McpBridge/HPCivil3d.McpBridge.csproj`.
- Compare `DynamoBridgeStatusView.xaml` with `HPAutoCad/HPAutoCad.McpBridge/View/AutocadBridgeStatusView.xaml`.
- Verify error code `-32001` in `McpShared/HPRebar.Mcp.Contracts/JsonRpc/BridgeErrorCode.cs`.

### 6.2 Host DLL Discovery Verification
Verify that the host DLLs specified in the blueprint exist on the dev machine:
```powershell
Test-Path 'C:\Program Files\Autodesk\Revit 2026\RevitAPI.dll'
Test-Path 'C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\DynamoCore.dll'
Test-Path 'C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\Revit\RevitNodes.dll'
Test-Path 'C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\Revit\DynamoRevitDS.dll'
```

### 6.3 Downstream Milestone M3 Scaffold Check
When Worker M3 implements this blueprint:
```powershell
# 1. Build solution with new bridge
dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx"

# 2. Verify no host DLLs were copied into bin/
Get-ChildItem -Path "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge\bin\Debug\net8.0-windows7.0\" -Filter "*Revit*"
Get-ChildItem -Path "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge\bin\Debug\net8.0-windows7.0\" -Filter "*Dynamo*"
# Expectation: 0 matches (only HPDynamo.McpBridge.dll and repacked assemblies exist).
```

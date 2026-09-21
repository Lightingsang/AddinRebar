# Dispatch: worker_m2
Role: teamwork_preview_worker
Target: Implement Milestone 2: HPPowerBi.McpBridge WPF MVVM UI & Named Pipe Host
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Project Blueprint: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_5\PROJECT.md
Spec Miner Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\spec_miner_pbi_1\report.md
Bridge Explorer Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_bridge_1\report.md
Output: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m2\handoff.md

## 2026-09-21T06:50:02Z
You are worker_m2, a teamwork_preview_worker.
Your working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m2
Project root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Authoritative user request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically section ## 2026-09-21T06:10:48Z)
Project Blueprint: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_5\PROJECT.md
Worker M1 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1\handoff.md
Worker M1 Fix Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_fix\handoff.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Objective:
Implement Milestone 2: HPPowerBi.McpBridge WPF MVVM UI & Named Pipe Host.

Tasks:
1. Theming Layer in HPPowerBi/HPPowerBi.McpBridge/Resources/Themes/:
   - Examine HPEtabs.McpBridge/Resources/Themes/ and HPSap2000.McpBridge/Resources/Themes/ as direct architectural templates.
   - Implement WindowsHostTheme.cs: listens to Windows theme preference (AppsUseLightTheme) and triggers dynamic theme changes.
   - Implement MaterialThemeBridge.cs: updates window merged dictionaries with palette overlays.
   - Implement ThemeInfo.cs with [assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)].
   - Create MaterialBridge.xaml: CustomColorTheme with PrimaryColor="#F2C811" (Power BI Yellow), SecondaryColor="#E6AD00" (Amber), BaseTheme="Dark", Segoe UI font.
   - Create ThemeDark.xaml and ThemeLight.xaml defining standard brush tokens (Brush.Background, Brush.Foreground, Brush.Card, etc.).
2. ViewModels in HPPowerBi/HPPowerBi.McpBridge/ViewModels/:
   - Implement StatusViewModel.cs using CommunityToolkit.Mvvm (ObservableObject, [ObservableProperty], [RelayCommand]):
     * Connection state: IsConnected, ConnectionStatusText, ActiveReportName, AttachedPid, LocalPort, DatabaseName.
     * Model stats: TableCount, MeasureCount, RelationshipCount.
     * Safety controls: IsExecutionEnabled, IsMutationEnabled (synced with PbiSafetyGuard, disabling execution auto-unchecks mutation).
     * Cloud status: IsCloudConfigured, CloudStatusText.
     * Pipe listener status: PipeStatusText ("Listening on hppowerbi-mcp-2026").
     * Instance selector: AvailableInstances (ObservableCollection<PbiInstanceInfo>), SelectedInstance.
     * Commands: RefreshInstancesCommand, ConnectCommand, DisconnectCommand, RegisterExternalToolCommand, RestoreSnapshotCommand.
3. Views in HPPowerBi/HPPowerBi.McpBridge/Views/:
   - Create StatusWindow.xaml: MaterialDesign 5.3.2 window with Cards for:
     * Header & Status Bar (Title "HP Power BI MCP Bridge", status badge).
     * Instance Switcher (ComboBox for running PBIDesktop instances, Refresh & Connect/Disconnect buttons).
     * Model Information (Display active database, port, and table/measure/relationship counts).
     * Safety & Guarding Controls (Checkboxes for "Allow DAX Execution" and "Allow Model Mutations", explanation of automatic TMSL backups).
     * External Tools & Maintenance (Button to register HPPowerBi.pbitool.json, Button to restore snapshot).
     * Pipe Monitor (Status of hppowerbi-mcp-2026 pipe listener).
   - Create StatusWindow.xaml.cs: code-behind initializing DataContext and attaching WindowsHostTheme.
   - Create App.xaml and App.xaml.cs: WPF application setup merging ResourceDictionaries.
4. Bridge Host Entry & Program.cs:
   - Implement BridgeEntry.cs: Coordinates PbiProcessDetector, PbiConnectionManager, PbiSafetyGuard, PbiSnapshotManager, PowerBiBridgeExecutor, PowerBiDispatcher, and starts McpBridgeHost on pipe "hppowerbi-mcp-2026".
   - Implement Program.cs: [STAThread] Main entry point with single-instance mutex and launching WPF App with StatusWindow.
5. Verification:
   Run:
   dotnet build HPPowerBi/HPPowerBi.slnx -c Debug
   dotnet run --project HPPowerBi/HPPowerBi.McpBridge.Tests/HPPowerBi.McpBridge.Tests.csproj
   Ensure 0 errors, 0 warnings, and 100% tests passing.
   Write your handoff report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m2\handoff.md
   Send a completion message via send_message to orchestrator_5 (conversation ID: 4d88b310-8910-4f85-b5a8-50216392bc6b).

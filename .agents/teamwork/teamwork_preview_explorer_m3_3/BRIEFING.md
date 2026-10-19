# BRIEFING — 2026-10-08T09:52:00Z

## Mission
Design the UI, opt-in mechanism, and project scaffolding for HPDynamo.McpBridge (C# net8.0-windows7.0, WPF, MaterialDesignThemes 5.3.2, Revit 2026 + Dynamo 3.7).

## 🔒 My Identity
- Archetype: explorer
- Roles: UI, Opt-In & Project Scaffolding Explorer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m3_3
- Original parent: b20bb4a5-dd75-4c48-9f4d-621e02412393
- Milestone: M3 (Dual-Host In-Process Bridge HPDynamo.McpBridge)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Target framework net8.0-windows7.0 (matching Revit 2026 .NET 8 runtime)
- Use WPF (<UseWPF>true</UseWPF>)
- ProjectReference to McpShared/HPRebar.McpBridge.Core and McpShared/HPRebar.Mcp.Contracts
- Mandatory: Apply <Private>false</Private> on all host API references (Revit 2026 & Dynamo 3.7) to avoid bin pollution (Reviewer 2 Finding 1 from M2)
- Modeless WPF Status View (DynamoBridgeStatusView.xaml, MaterialDesignThemes 5.3.2, MaterialThemeBridge.cs)
- Mandatory Opt-In toggle: "Allow AI Dynamo execution" (default: false / OFF; error code -32001 when disabled)
- Clean Code compliance (classes <= 300 lines, methods <= 50 lines)

## Current Parent
- Conversation ID: b20bb4a5-dd75-4c48-9f4d-621e02412393
- Updated: 2026-10-08T09:52:00Z

## Investigation State
- **Explored paths**:
  - `HPDynamo/Directory.Build.props`, `HPDynamo/HPDynamo.slnx`
  - `HPDynamo.Mcp.Server/Tools/ExecuteDesignScriptTool.cs`, `ExecuteDynamoCodeTool.cs`
  - `McpShared/HPRebar.McpBridge.Core/Host/McpBridgeHost.cs`, `Pipe/RequestDispatcher.cs`, `ViewModel/McpBridgeStatusViewModel.cs`, `Model/BridgeSettings.cs`, `Model/BridgeSettingsStore.cs`
  - `McpShared/HPRebar.Mcp.Contracts/JsonRpc/BridgeErrorCode.cs`
  - Sister bridges: `HPAutoCad.McpBridge`, `HPCivil3d.McpBridge`, `HPRebar.McpBridge`, `HPNavis.McpBridge`, `HPEtabs.McpBridge`
  - Reviewer 2 Finding 1 report: `teamwork_preview_reviewer_m2_2/handoff.md`
  - Skill standard: `revit-xaml-styles/SKILL.md` and boilerplate files
- **Key findings**:
  - Reviewer 2 Finding 1 from M2: `Directory.Build.props` lacks global `<Private>false</Private>` on Reference items. Add explicit `<Private>false</Private>` on each host Reference in `HPDynamo.McpBridge.csproj` AND declare `<ItemDefinitionGroup><Reference><Private>false</Private></Reference></ItemDefinitionGroup>`.
  - Opt-In toggle: `IsExecutionEnabled` in `McpBridgeStatusViewModel` delegates to `_settings.ExecutionEnabled`. In `BridgeSettingsStore`, `ExecutionEnabled` is stripped on save and always initialized to `false` on load. `RequestDispatcher` intercepts execution when disabled and emits error code `-32001` (`BridgeErrorCode.ExecutionDisabled`).
  - WPF Status View: Styled with `MaterialDesignThemes 5.3.2` and Segoe UI (Roboto overridden). Uses `MaterialThemeBridge.cs` to attach to host theme (`DynamoHostTheme.cs` checking `UIThemeManager.CurrentTheme`). Window is modeless with Close forwarded to ViewModel. Detach on Close.
  - RepackMaterialDesign: ILRepack target merges `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, `Microsoft.Xaml.Behaviors.dll` into `HPDynamo.McpBridge.dll` to prevent ALC type collision in Revit.
- **Unexplored areas**: None for M3.3 scope.

## Key Decisions Made
- Project scaffolding: target `net8.0-windows7.0`, `<UseWPF>true</UseWPF>`, references to `McpShared` contracts and bridge core, 8 host references with `<Private>false</Private>` + global `ItemDefinitionGroup`.
- UI: Complete XAML layout designed with 4 tiered sections (Header, Safety Card, Last Run Card, Footer).
- Safety Opt-In: CheckBox "Allow AI Dynamo execution" binds to `IsExecutionEnabled`, off by default, rejects with code `-32001` and custom message.
- Theme sync: `DynamoHostTheme` queries Revit `UIThemeManager.CurrentTheme` with safe fallback, attached via `MaterialThemeBridge`.

## Artifact Index
- [g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m3_3\handoff.md] — Final handoff report

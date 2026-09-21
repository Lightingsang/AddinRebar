# BRIEFING — 2026-09-21T09:51:00Z

## Mission
Investigate standalone WPF MCP bridge architectures (HPPowerBi, HPEtabs, HPSap2000) to design HPExcel.McpBridge: solution layout, MaterialDesign UI/theme, lifecycle/pipe listener, 3-tier safety engine, and snapshot backup engine.

## 🔒 My Identity
- Archetype: Teamwork explorer
- Roles: Explorer, Synthesizer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_2
- Original parent: a6affb02-3586-4014-be6f-de9dfcd816bd
- Milestone: HPExcel Survey & Architecture Specification

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Produce comprehensive handoff.md with 5 components
- Focus specifically on standalone WPF MCP bridge architecture (HPPowerBi, HPEtabs, HPSap2000) vs in-process addins
- Solution layout, MaterialDesign UI/themes, connection lifecycle, 3-tier safety, snapshot engine

## Current Parent
- Conversation ID: a6affb02-3586-4014-be6f-de9dfcd816bd
- Updated: 2026-09-21T09:51:00Z

## Investigation State
- **Explored paths**:
  - `HPPowerBi/` (`HPPowerBi.slnx`, `Directory.Build.props`, `HPPowerBi.McpBridge.csproj`, `Program.cs`, `App.xaml(.cs)`, `BridgeEntry.cs`, `PowerBiDispatcher.cs`, `PbiSafetyGuard.cs`, `PbiSnapshotManager.cs`, `StatusWindow.xaml(.cs)`, `StatusViewModel.cs`, `MaterialThemeBridge.cs`, `WindowsHostTheme.cs`, `MaterialBridge.xaml`, `ThemeLight.xaml`, `ThemeDark.xaml`)
  - `HPEtabs/` (`HPEtabs.slnx`, `Directory.Build.props`, `HPEtabs.McpBridge.csproj`, `EtabsExecutor.cs`, `EtabsExecutor.Worker.cs`, `EtabsAttachment.cs`, `EtabsTierAnalyzer.cs`, `EtabsTierTable.cs`, `EtabsSnapshotManager.cs`, `EtabsBridgeStatusView.xaml`, `EtabsBridgeStatusViewModel.cs`, `EtabsScriptGlobals.cs`)
  - `HPSap2000/` (`SapAttachment.cs`, `SapExecutor.cs`, P/Invoke `GetActiveObject`)
  - `McpShared/` (`HPRebar.Mcp.Contracts/PipeNaming.cs`, `JsonRpcMethods.cs`, `HostScriptContracts.cs`, `ContextMessages.cs`, `ExecuteResult.cs`, `HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`, `ScriptGuard.cs`, `McpBridgeStatusViewModel.cs`, `ContextService.cs`)
- **Key findings**:
  - Standalone bridge pattern: single-instance mutex in `Program.cs`, STA worker loop for COM interop, WPF MVVM with `CommunityToolkit.Mvvm` 8.4.0, MaterialDesignThemes 5.3.2.
  - Theme architecture: `MaterialThemeBridge.Attach(this, WindowsHostTheme.Instance)` with dynamic overlay dictionary replacing top-level merged dictionaries on `SystemEvents.UserPreferenceChanged` or `HPEXCEL_MCP_BRIDGE_THEME`.
  - Safety tiering: 3 tiers (Tier R, Tier W, Tier D) with UI checkboxes cascading (`IsDestructiveEnabled` requires `IsWriteEnabled`, which requires `IsExecutionEnabled`). Static dryRun / `transaction: none` preview. AST semantic inspection fails closed as Destructive.
  - Snapshot engine: `Workbook.SaveCopyAs(snapshotPath)` for live COM workbooks, `XLWorkbook.SaveAs` / file copy for ClosedXML headless. Target location: `.hpexcel_snapshots/` beside workbook (or `%TEMP%` for unsaved/network workbooks). Timestamped filenames `yyyyMMdd-HHmmss` with automated retention pruning (newest 20-50). Path returned in `ExecuteResult.Snapshot`.
- **Unexplored areas**: None within the scope of Survey 2.

## Key Decisions Made
- Fully analyzed and documented the 5 core focus areas for HPExcel.McpBridge
- Preparing detailed `handoff.md` with complete architecture specifications, code templates, and verification methods.

## Artifact Index
- handoff.md — Comprehensive findings and architecture recommendations

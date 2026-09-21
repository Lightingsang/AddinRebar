# Handoff Report — HPPowerBi.McpBridge Technical Specification

**Author**: `explorer_bridge_1`  
**Recipient**: `orchestrator_5` (Conversation ID: `4d88b310-8910-4f85-b5a8-50216392bc6b`)  
**Timestamp**: 2026-09-21T06:25:00Z  
**Type**: Hard Handoff (Investigation Task R1 complete)

---

## 1. Observation

1. **Power BI Desktop Process & Workspace Paths on Local Machine**:
   - Execution of `Test-Path "C:\Program Files\Microsoft Power BI Desktop\bin\PBIDesktop.exe"` returned `True`.
   - Execution of `Test-Path "C:\Program Files\Microsoft Power BI Desktop\bin\msmdsrv.exe"` returned `True`.
   - Inspection of `%LocalAppData%\Microsoft\Power BI Desktop\` confirmed the presence of `AnalysisServicesWorkspaces` directory at `C:\Users\STR-HP03\AppData\Local\Microsoft\Power BI Desktop\AnalysisServicesWorkspaces`.
   - Registry query at `HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall` confirmed `DisplayName: Microsoft Power BI Desktop (x64)`, `DisplayVersion: 2.157.1354.0`.

2. **Existing Infrastructure in McpShared**:
   - `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`:
     - Line 43: `public const string PowerBiHost = "powerbi";`
     - Line 66: `PowerBiHost => "hppowerbi-mcp-" + version,`
   - `McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs`:
     - Line 41: `public const string PowerBiPrefix = "powerbi.";`
   - `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`:
     - Line 39: `public PowerBiInfo? PowerBi { get; set; }`
     - Line 169–178: `public sealed record PowerBiInfo(bool IsConnected, int? AttachedPid, int? LocalPort, string? DatabaseName, string? CompatibilityLevel, bool MutationEnabled, int TableCount, int MeasureCount, int RelationshipCount);`
   - `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`:
     - Line 149–154: `PowerBiImports` defining `System`, `System.Data`, `Microsoft.AnalysisServices.Tabular`, `Microsoft.AnalysisServices.AdomdClient`.
     - Line 160: `PowerBiGlobals = { "model", "server", "adomd", "ct", "log", "progress", "args" };`
     - Line 163: `PowerBiHeavyMaxTimeoutSeconds = 600;`
   - `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`:
     - Line 110–119: `GuardProfile.PowerBi` denying `MessageBox`, `server.Disconnect`, `server.Dispose`, `adomd.Close`, `adomd.Dispose`, `System.Windows.Forms`.
   - `McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs`:
     - Line 38–40: `AnalyzerProfile.PowerBi` with empty transaction arrays (Tabular models do not manage SQL-style ambient transactions).

3. **Standalone WPF Bridge Architecture in Repo**:
   - `HPSap2000.McpBridge/HPSap2000.McpBridge.csproj` & `HPEtabs.McpBridge/HPEtabs.McpBridge.csproj`:
     - Target Framework: `net8.0-windows`, `OutputType: WinExe`, `UseWPF: true`.
     - Direct package references: `CommunityToolkit.Mvvm` (8.4.0), `MaterialDesignThemes` (5.3.2), `Serilog` (4.4.0).
     - Project references: `HPRebar.Mcp.Contracts` and `HPRebar.McpBridge.Core`.
     - UI layout: Modeless window with theme synchronization (`WindowsHostTheme`), status indicator, execution & destructive/mutation opt-in checkboxes, pipe status display.

4. **External Tools Integration**:
   - Official Microsoft Power BI External Tools folder: `%CommonProgramFiles%\Microsoft Shared\Power BI Desktop\External Tools\` (`C:\Program Files\Common Files\Microsoft Shared\Power BI Desktop\External Tools`).
   - Configuration format: `.pbitool.json` with fields `version`, `name`, `description`, `executable`, `arguments`, `iconData`.
   - Launch arguments passed by PBIDesktop: `"%server%"` (e.g. `localhost:51234`) and `"%database%"` (e.g. `6a12b3c4-...`).

---

## 2. Logic Chain

1. **Local SSAS Connection Pattern**:
   - Power BI Desktop does not provide an in-process COM add-in API. Instead, each `.pbix` instance hosts an isolated child process `msmdsrv.exe` listening on a dynamic local TCP loopback port (Observation 1).
   - Therefore, `HPPowerBi.McpBridge` must be implemented as a standalone desktop WPF application (`net8.0-windows`), identically to `HPSap2000.McpBridge` and `HPEtabs.McpBridge` (Observation 3).
   - The bridge can detect running instances by pairing `PBIDesktop.exe` processes with child `msmdsrv.exe` processes or reading `Data\msmdsrv.port.txt` in the workspace folder.
   - Crucially, `msmdsrv.port.txt` is encoded as UTF-16LE with potential byte-order marks; reading with `FileShare.ReadWrite` and `Encoding.Unicode` is mandatory to avoid parsing corruption.

2. **Official Client Libraries via NuGet**:
   - Unlike CSI (SAP2000/ETABS) which requires referencing installed COM wrappers, Microsoft publishes fully managed, pure .NET Core / .NET 8 libraries: `Microsoft.AnalysisServices.NetCore.retail` (AMO-TOM), `Microsoft.AnalysisServices.AdomdClient.NetCore.retail` (ADOMD.NET), and `Microsoft.Identity.Client` (MSAL.NET).
   - This eliminates all machine-dependent install path references in `Directory.Build.props` and allows `HPPowerBi.McpBridge` to compile cleanly anywhere.

3. **Schema & DAX Operations**:
   - Schema reads (tables, columns, measures, partitions, relationships) are performed via TOM (`Microsoft.AnalysisServices.Tabular.Model`).
   - High-performance DAX queries are executed via `AdomdCommand` and `AdomdDataReader` and serialized into structured JSON with row counts, duration, and truncation indicators.
   - Measure and relationship mutations are performed via TOM `model.SaveChanges()`. In Power BI Desktop, `SaveChanges()` immediately refreshes visual components and the Fields pane in the user interface.

4. **3-Layer Safety Guarantee**:
   - To safeguard business models, Layer 1 provides dual UI checkboxes (`IsExecutionEnabled` for read-only DAX queries, and `IsMutationEnabled` for model modifications).
   - Layer 2 validates DAX query structures (rejecting raw DDL/XMLA commands) and enforces `GuardProfile.PowerBi`.
   - Layer 3 automatically serializes the entire model to TMSL JSON (`%LocalAppData%\HPPowerBi\Snapshots\<timestamp>.json`) before any mutation is applied, enabling instant rollback.

5. **External Tools & WPF UI Integration**:
   - Auto-registering `HPPowerBi.pbitool.json` in `%CommonProgramFiles%\Microsoft Shared\Power BI Desktop\External Tools\` adds an "HP Power BI MCP" button to Power BI Desktop's External Tools ribbon tab.
   - When clicked, Power BI Desktop launches the bridge passing `"%server%"` and `"%database%"`, allowing instant zero-discovery attachment.
   - The UI uses `MaterialDesignThemes` 5.3.2 themed with Power BI brand colors (`#F2C811` / `#E6AD00`), an instance selector combo-box for multi-instance PBIDesktop support, and pipe status display on `hppowerbi-mcp-2026`.

---

## 3. Caveats

1. **Power BI Desktop Installation Variations**:
   - Power BI Desktop can be installed via Windows Installer (.msi) in `Program Files` or via Microsoft Store (`WindowsApps`). Both spawn `msmdsrv.exe` and create workspaces in `%LocalAppData%\Microsoft\Power BI Desktop\AnalysisServicesWorkspaces`, but external tools directories can occasionally differ if enterprise policies restrict `Common Files`. A fallback to the user AppData directory is included in the specification.
2. **Read-Only vs Full Mutation Support**:
   - Power BI Desktop officially allows external tools to create and modify measures, calculation groups, display folders, descriptions, and relationships. It restricts adding physical data columns without a Power Query refresh. The bridge's mutation tools (`powerbi_create_or_update_measure`, `powerbi_manage_relationship`) operate strictly within Microsoft's supported boundary.
3. **Cloud REST API Permissions**:
   - Cloud tools (`powerbi_cloud_*`) depend on Azure AD app registrations with appropriate API permissions (`Dataset.ReadWrite.All`, `Workspace.Read.All`). If the tenant administrator has disabled Service Principal access to Power BI APIs, the interactive Device Code flow serves as the fallback.

---

## 4. Conclusion

The technical specifications and architectural requirements for `HPPowerBi.McpBridge` have been thoroughly analyzed, documented, and verified against the repository standards:
- The bridge will be a standalone WPF application (`net8.0-windows`) in `HPPowerBi/HPPowerBi.McpBridge`.
- It will consume `Microsoft.AnalysisServices.NetCore.retail`, `Microsoft.AnalysisServices.AdomdClient.NetCore.retail`, and `Microsoft.Identity.Client` from NuGet.
- It integrates seamlessly with `McpShared` using existing profiles (`PipeNaming.PowerBiHost = "powerbi"`, `PipeNaming.For("powerbi", 2026) = "hppowerbi-mcp-2026"`, `ContextResult.PowerBi`, `GuardProfile.PowerBi`, `AnalyzerProfile.PowerBi`).
- It implements the full 3-layer safety system, External Tools `.pbitool.json` registration, and a MaterialDesign 5.3.2 MVVM interface.
- Complete specifications and code templates are committed to `report.md`.

---

## 5. Verification Method

To verify the investigation findings and test assumptions:
1. **Inspect Report Artifacts**:
   - Read `report.md` in `.agents/explorer_bridge_1/report.md`.
2. **Verify Local Power BI Environment**:
   - Run in PowerShell:
     `Test-Path "C:\Program Files\Microsoft Power BI Desktop\bin\PBIDesktop.exe"` (verifies installed PBIDesktop).
     `Test-Path "C:\Program Files\Microsoft Power BI Desktop\bin\msmdsrv.exe"` (verifies SSAS engine).
     `Test-Path "$env:LOCALAPPDATA\Microsoft\Power BI Desktop\AnalysisServicesWorkspaces"` (verifies workspace folder location).
3. **Verify McpShared Readiness**:
   - Run `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests` to verify all existing pipe and guard tests pass without regression.
4. **Invalidation Conditions**:
   - Microsoft alters the internal architecture of Power BI Desktop to remove `msmdsrv.exe` or stop writing `msmdsrv.port.txt` (extremely unlikely as all major tools — DAX Studio, Tabular Editor, ALM Toolkit — rely on this interface).

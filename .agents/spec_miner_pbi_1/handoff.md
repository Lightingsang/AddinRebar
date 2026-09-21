# Handoff Report: Specification Mining for Power BI MCP Subsystem (HPPowerBi)

**Author:** spec_miner_pbi_1 (teamwork_preview_spec_miner)  
**Recipient:** orchestrator_5  
**Type:** Hard Handoff (Task Complete)  
**Date:** 2026-09-21  

---

## 1. Observation

Direct code observations from inspecting `McpShared`, `HPEtabs`, `HPSap2000`, and `ORIGINAL_REQUEST.md`:

1. **Pre-existing Power BI Tokens in `McpShared/HPRebar.Mcp.Contracts`:**
   - `PipeNaming.cs` line 43: `public const string PowerBiHost = "powerbi";`
   - `PipeNaming.cs` line 66: `PowerBiHost => "hppowerbi-mcp-" + version,`
   - `JsonRpcMethods.cs` line 41: `public const string PowerBiPrefix = "powerbi.";`
   - `HostScriptContracts.cs` lines 149–163:
     ```csharp
     public static readonly string[] PowerBiImports =
     {
         "System", "System.Linq", "System.Collections.Generic", "System.Data",
         "Microsoft.AnalysisServices.Tabular", "Microsoft.AnalysisServices.AdomdClient",
         "HPRebar.McpBridge.Core.Scripting",
     };
     public static readonly string[] PowerBiGlobals = { "model", "server", "adomd", "ct", "log", "progress", "args" };
     public const int PowerBiHeavyMaxTimeoutSeconds = 600;
     ```
   - `ContextMessages.cs` line 39: `public PowerBiInfo? PowerBi { get; set; }`
   - `ContextMessages.cs` lines 169–178:
     ```csharp
     public sealed record PowerBiInfo(
         bool IsConnected,
         int? AttachedPid,
         int? LocalPort,
         string? DatabaseName,
         string? CompatibilityLevel,
         bool MutationEnabled,
         int TableCount,
         int MeasureCount,
         int RelationshipCount);
     ```

2. **Pre-existing Power BI Guard and Analyzer Profiles in `McpShared/HPRebar.McpBridge.Core`:**
   - `GuardProfile.cs` lines 110–120:
     ```csharp
     public static readonly GuardProfile PowerBi = new GuardProfile(
         "Power BI",
         deniedIdentifiers: new[] { "MessageBox" },
         deniedMembers: new[] { "Disconnect", "Dispose" },
         deniedMembersOnIdentifier: new Dictionary<string, string[]>(StringComparer.Ordinal)
         {
             ["server"] = new[] { "Disconnect", "Dispose" },
             ["adomd"] = new[] { "Close", "Dispose" },
         },
         deniedNamespaces: new[] { "System.Windows.Forms", "HPPowerBi.McpBridge", "HPRebar.McpBridge.Core.Host" });
     ```
   - `AnalyzerProfile.cs` lines 38–40:
     ```csharp
     public static readonly AnalyzerProfile PowerBi = new AnalyzerProfile(
         transactionTypeNames: Array.Empty<string>(),
         transactionMethodNames: Array.Empty<string>());
     ```

3. **Standalone Desktop App Structure in `HPEtabs` & `HPSap2000`:**
   - `HPEtabs.McpBridge/HPEtabs.McpBridge.csproj`:
     - `<TargetFramework>net8.0-windows</TargetFramework>`
     - `<OutputType>WinExe</OutputType>`
     - `<UseWPF>true</UseWPF>`
     - Standard PackageReferences: `CommunityToolkit.Mvvm 8.4.0`, `MaterialDesignThemes 5.3.2`, `Serilog 4.4.0`, `Serilog.Sinks.File 7.0.0`
     - **No ILRepack target:** As a standalone desktop application with its own isolated process, `MaterialDesignThemes` ships as loose DLLs beside the executable without conflicting with CAD host processes.
   - `App.xaml.cs`: Instantiates `BridgeEntry.Start()` yielding `(McpBridgeHost, Executor)`, constructs ViewModel, shows Status Window.
   - `Resources/Themes/`: `WindowsHostTheme.cs` listens to `SystemEvents.UserPreferenceChanged` for dynamic Light/Dark mode; `MaterialThemeBridge.cs` swaps the merged dictionary overlay.
   - 3-Layer Safety: UI Opt-in switch, validation checks, and timestamped pre-run backups in `%LocalAppData%\HPEtabs\McpBridge\snapshots\`.

4. **Server Bootstrap Contract in `McpShared/HPRebar.Mcp.Server.Core`:**
   - `McpServerHost.cs` line 26: `public static HostApplicationBuilder CreateBuilder(string[] args, IHostProfile profile)`
   - `McpServerHost.cs` line 126: `public static async Task<int> RunAsync(string[] args, IHostProfile profile)`
   - A server executable's `Program.cs` is a single line: `return await McpServerHost.RunAsync(args, HostProfile.Instance);`.

---

## 2. Logic Chain

1. **Step 1 (Wire and Interface Stability):** Observations 1 and 2 demonstrate that `HPRebar.Mcp.Contracts` and `HPRebar.McpBridge.Core` already contain full definitions for `PowerBiHost`, `PowerBiPrefix`, `PowerBiImports`, `PowerBiGlobals`, `PowerBiHeavyMaxTimeoutSeconds`, `PowerBiInfo`, `GuardProfile.PowerBi`, and `AnalyzerProfile.PowerBi`. Therefore, implementing the Power BI subsystem requires **zero breaking changes and zero modifications to `McpShared/`**.
2. **Step 2 (Standalone Bridge Architecture):** Observation 3 shows how `HPEtabs` and `HPSap2000` successfully isolate external desktop hosts via a standalone WPF bridge. Power BI Desktop, running `PBIDesktop.exe` with its child `msmdsrv.exe`, mirrors this exact topology. The bridge must be a `.NET 8.0-windows` application with `MaterialDesignThemes 5.3.2` as loose DLLs, connecting via standard NuGet packages (`AMO-TOM` and `ADOMD.NET`) without requiring ILRepack or COM registration.
3. **Step 3 (Safety Architecture):** In Power BI, Tabular models modify database metadata in memory and commit via `model.SaveChanges()`. Following the safety design observed in Observation 3, Power BI operations must enforce: (a) UI checkbox opt-in (`MutationEnabled`), (b) guard denial of connection closures and OS process execution, and (c) automatic metadata/TMDL snapshot persistence to `%LocalAppData%\HPPowerBi\McpBridge\snapshots\` with the filename returned in `ExecuteResult.Snapshot`.
4. **Step 4 (Server & Tools Layer):** Observation 4 shows that `McpServerHost` handles all common server infrastructure (stdio transport, dynamic tool registry, inspection, cancellation). `HPPowerBi.Mcp.Server` only needs to provide `PowerBiHostProfile` and register its core tools (`get_powerbi_context`, `execute_powerbi_code`, `powerbi_get_schema`, `powerbi_evaluate_dax`, `powerbi_create_or_update_measure`, `powerbi_delete_measure`, `powerbi_manage_relationship`, `powerbi_format_dax`) and cloud tools.

---

## 3. Caveats

- **AMO-TOM vs TMDL Support:** AMO-TOM (`Microsoft.AnalysisServices.NetCore.retail`) supports full TMSL JSON serialization (`JsonSerializer.SerializeDatabase`). TMDL (Tabular Model Definition Language) is available in newer versions of the TOM library (`Microsoft.AnalysisServices.Tabular.TmdlSerializer`); if TMDL serializer is not available in the selected package version, JSON TMSL serialization serves as an equivalent, robust metadata snapshot.
- **External Tools Permissions:** Writing to `%CommonProgramFiles%\Microsoft Shared\Power BI Desktop\External Tools\` requires elevated privileges on some corporate Windows installations. The bridge should gracefully catch `UnauthorizedAccessException` and offer fallback installation to `%LocalAppData%\Programs\Microsoft Power BI Desktop\External Tools\`.

---

## 4. Conclusion

The architectural contract for `HPPowerBi` is fully resolved and ready for implementation planning and execution:
- **Zero modification to `McpShared/` required.**
- `HPPowerBi/` must be scaffolded as a self-contained deliverable with `HPPowerBi.slnx`, `global.json`, `Directory.Build.props`, `HPPowerBi.McpBridge` (.NET 8.0-windows WPF), `HPPowerBi.Mcp.Server` (.NET 10.0 stdio), and tests.
- Full details, schemas, feature matrices, and edge cases are documented in `.agents/spec_miner_pbi_1/report.md`.

---

## 5. Verification Method

To independently verify all observations and contracts:

1. **Verify McpShared Tokens:**
   ```powershell
   # Inspect pre-existing Power BI tokens
   Select-String -Path "McpShared\HPRebar.Mcp.Contracts\PipeNaming.cs" -Pattern "PowerBiHost"
   Select-String -Path "McpShared\HPRebar.Mcp.Contracts\HostScriptContracts.cs" -Pattern "PowerBi"
   Select-String -Path "McpShared\HPRebar.Mcp.Contracts\Messages\ContextMessages.cs" -Pattern "PowerBiInfo"
   Select-String -Path "McpShared\HPRebar.McpBridge.Core\Scripting\GuardProfile.cs" -Pattern "PowerBi"
   Select-String -Path "McpShared\HPRebar.McpBridge.Core\Scripting\AnalyzerProfile.cs" -Pattern "PowerBi"
   ```

2. **Verify McpShared Server Core Tests Baseline:**
   ```powershell
   dotnet test McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj
   # Expected: All tests pass with 0 failures.
   ```

3. **Inspect Specification Report:**
   View `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\spec_miner_pbi_1\report.md` for complete feature matrices and technical blueprints.

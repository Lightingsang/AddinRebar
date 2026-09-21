# Handoff Report: Survey Explorer 1 — McpShared Architecture & Extension for HPExcel

## 1. Observation

### 1.1 Existing Host Integration in `McpShared/`
Direct inspection of `McpShared/` revealed how host neutrality and per-host registration are implemented across seven existing deliverables (`HPRebar`, `HPAutoCad`, `HPNavis`, `HPEtabs`, `HPCivil3d`, `HPSap2000`, `HPPowerBi`):

1. **`McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`** (lines 10–70):
   - Defines host ID constants:
     - Line 15: `public const string RevitHost = "revit";`
     - Line 17: `public const string AutocadHost = "autocad";`
     - Line 20: `public const string NavisHost = "navis";`
     - Line 26: `public const string EtabsHost = "etabs";`
     - Line 33: `public const string Civil3dHost = "civil3d";`
     - Line 38: `public const string Sap2000Host = "sap2000";`
     - Line 43: `public const string PowerBiHost = "powerbi";`
   - In `For(string host, int version)` (lines 58–69):
     - Matches `host` key in a switch expression, mapping to `"hp{host}-mcp-" + version` (e.g., `PowerBiHost => "hppowerbi-mcp-" + version`).

2. **`McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs`** (lines 35–42):
   - Defines host wire prefixes:
     - Line 35: `RevitPrefix = "revit.";`
     - Line 36: `AutocadPrefix = "autocad.";`
     - Line 37: `NavisPrefix = "navis.";`
     - Line 38: `EtabsPrefix = "etabs.";`
     - Line 39: `Civil3dPrefix = "civil3d.";`
     - Line 40: `Sap2000Prefix = "sap2000.";`
     - Line 41: `PowerBiPrefix = "powerbi.";`
   - Suffixes (lines 25–33) are host-neutral: `ping`, `context`, `inspect`, `execute`, `cancel`, `analyze`, `progress`, `log`, `status`.
   - `RequestDispatcher` (lines 106–149) dispatches strictly on `JsonRpcMethods.Suffix(method)`.

3. **`McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`** (lines 1–164):
   - Defines default script imports, script globals, and heavy timeout per host:
     - Revit (lines 12–17, 51)
     - AutoCAD (lines 25–33, 54)
     - Navisworks (lines 42–48, 61, 68)
     - ETABS (lines 75–80, 87, 93)
     - Civil 3D (lines 104–112, 119)
     - SAP2000 (lines 125–130, 137, 143)
     - Power BI (lines 149–154, 160, 163):
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

4. **`McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`** (lines 12–179):
   - `ContextResult` (lines 12–65) provides host-specific info records via nullable properties:
     - Line 24: `public AutocadInfo? Autocad { get; set; }`
     - Line 27: `public NavisInfo? Navis { get; set; }`
     - Line 30: `public EtabsInfo? Etabs { get; set; }`
     - Line 33: `public Civil3dInfo? Civil3d { get; set; }`
     - Line 36: `public Sap2000Info? Sap2000 { get; set; }`
     - Line 39: `public PowerBiInfo? PowerBi { get; set; }`
   - `BridgeJson.Options` configures `JsonIgnoreCondition.WhenWritingNull`, so when a property is null, it is completely omitted from the wire JSON.

5. **`McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`** (lines 9–155):
   - Static instances: `Revit`, `Autocad`, `Navis`, `Etabs`, `Sap2000`, `PowerBi`, `Civil3d`.
   - Denies host-specific hazards on top of the base `ScriptGuard` deny-list (`System.Diagnostics.Process`, reflection, threads, `await`, `dynamic`, `#r`/`#load`, raw `System.IO` except `System.IO.Path`).
   - E.g., `GuardProfile.PowerBi` (lines 110–119):
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

6. **`McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs`** (lines 8–46):
   - Manages static transaction recognition.
   - For hosts without script-level transactions (`Etabs`, `Sap2000`, `PowerBi`):
     ```csharp
     public static readonly AnalyzerProfile PowerBi = new AnalyzerProfile(
         transactionTypeNames: Array.Empty<string>(),
         transactionMethodNames: Array.Empty<string>());
     ```

7. **`McpShared/HPRebar.Mcp.Server.Core/` (Engine Services & Registry)**:
   - `ContextService.cs`:
     - Lines 54–59: `Shape()` strips Revit-only fields (`revitVersion`, `isFamily`) for non-Revit hosts.
     - Line 71: Calls `bridge.Profile.Method(JsonRpcMethods.ContextSuffix)` -> routes to `{prefix}.context`.
   - `ExecuteCodeService.cs`:
     - Line 61: Clamps timeout to `bridge.Profile.MaxTimeoutSeconds`.
     - Line 78: Calls `bridge.Profile.Method(JsonRpcMethods.ExecuteSuffix)` -> routes to `{prefix}.execute`.
     - Lines 85–94: Integrates run history with `ToolManager`.
   - `McpServerHost.cs` (lines 22–139):
     - Lines 67–84: Dynamically registers tools, prompts, resources from engine assembly and `profile.HostAssembly`.
     - Lines 103–107: Seeds `BridgeOptions.HostId` and `BridgeOptions.HostVersion = profile.DefaultVersion`.
   - `ToolValidator.cs` (lines 21–172):
     - Validates tool proposals against `profile.HostId`, `profile.Categories`, `profile.MaxTimeoutSeconds`, and `profile.CoreToolNames`.

### 1.2 Baseline Verification Results
Executed via `dotnet test` from `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared`:
- `dotnet test HPRebar.Mcp.Server.Core.Tests`: **Passed! Total: 228, Failed: 0, Succeeded: 228, Skipped: 0, Duration: 4.5s**.
- `dotnet test HPRebar.McpBridge.Core.Net48Tests`: **Passed! Total: 71, Failed: 0, Succeeded: 71, Skipped: 0, Duration: 2.9s**.
- **Total McpShared tests: 299 passed, 0 failures**.

---

## 2. Logic Chain

1. **Step 1 — Host Identity & Pipe Naming**:
   - `PipeNaming` is the single source of named-pipe names across server and bridge.
   - Adding `PipeNaming.ExcelHost = "excel"` and case `ExcelHost => "hpexcel-mcp-" + version` in `PipeNaming.For(...)` guarantees deterministic pipe naming `hpexcel-mcp-2026` for both the bridge listener and server client.
   - Verified by pattern in `PowerBiHost`, `Sap2000Host`, and `EtabsHost`.

2. **Step 2 — JSON-RPC Wire Protocol**:
   - `JsonRpcMethods.ExcelPrefix = "excel."` establishes method names (`excel.execute`, `excel.context`, `excel.ping`, `excel.cancel`, `excel.analyze`, `excel.progress`).
   - `RequestDispatcher` dispatches on method suffixes (`execute`, `context`, etc.), so no changes to dispatcher logic are needed; it natively routes `excel.*` requests.

3. **Step 3 — Scripting Contracts & Safety Guardrails**:
   - Roslyn scripts running in Excel need access to COM Interop (`Microsoft.Office.Interop.Excel`) and ClosedXML (`ClosedXML.Excel`), plus bridge scripting utilities (`HPRebar.McpBridge.Core.Scripting`).
   - `HostScriptContracts.ExcelImports` provides these default usings. `System.IO` is omitted because `ScriptGuard` denies raw file operations; scripts access workbooks through Excel COM or ClosedXML APIs.
   - `HostScriptContracts.ExcelGlobals` defines `excel` (COM `Application`), `workbook` (active `Workbook`), `sheet` (active `Worksheet`), `ct`, `log`, `progress`, `args`.
   - `HostScriptContracts.ExcelHeavyMaxTimeoutSeconds = 600` provides an upper bound for large workbook manipulations and batch calculations.
   - `GuardProfile.Excel`:
     - Denies `excel.Quit()` / `app.Quit()`: Prevents AI scripts from killing the user's running Excel application.
     - Denies `InputBox`, `GetOpenFilename`, `GetSaveAsFilename`: Prevents opening modal dialogs that lock the Excel COM thread and stall the pipe.
     - Denies `MessageBox`: Blocks modal Windows message boxes.
     - Denies `System.Windows.Forms`, `HPExcel.McpBridge`, and `HPRebar.McpBridge.Core.Host`: Blocks modal GUI and bridge internals.
     - The base deny-list in `ScriptGuard` already blocks `System.Diagnostics.Process`, `#r`/`#load` directives, `System.Reflection`, `Marshal`, threads, `await`, `dynamic`, and file deletions outside allowed APIs.
   - `AnalyzerProfile.Excel`:
     - Excel has no script-level transaction keywords (`Transaction`, `StartTransaction`).
     - Setting empty collections in `AnalyzerProfile.Excel` ensures `ScriptAnalyzer` never reports `UsesTransaction = true`, preventing false "set transaction=manual" validator errors.

4. **Step 4 — Context Information Shape**:
   - `ContextResult.Excel` holds `ExcelInfo?`.
   - `ExcelInfo` record captures: `IsAttached`, `AttachedPid`, `ExcelVersion`, `ActiveWorkbookName`, `ActiveWorksheetName`, `SelectionAddress`, `WriteEnabled`, `DestructiveEnabled`, `OpenWorkbookCount`, `WorksheetCount`, `HasActiveWorkbook`.
   - `ContextResult.DocTitle` maps to active workbook name; `DocPath` maps to active workbook full path.
   - In `ContextService`, `Shape()` strips Revit-specific fields and outputs the clean camelCase JSON. When `Excel` is null (in other hosts), `JsonIgnoreCondition.WhenWritingNull` suppresses the field, preserving 100% wire neutrality.

5. **Step 5 — Server Core & Registry Integration**:
   - `ExecuteCodeService`, `ContextService`, `ResultFormatter`, `McpServerHost`, `ToolValidator`, and `DynamicToolRegistrar` are completely profile-driven.
   - Implementing `ExcelHostProfile: IHostProfile` in `HPExcel.Mcp.Server` enables all server capabilities with zero alterations to `Server.Core` service implementations.
   - Snapshot filenames returned from bridge Tier W/D operations are automatically sanitized by `ResultFormatter.FromExecute` (`Path.GetFileName(result.Snapshot)`), preventing path leaks.

6. **Step 6 — Zero-Regression Engine Tests**:
   - Creating `ExcelProfileTests.cs` and `ExcelTestProfile.cs` in `HPRebar.Mcp.Server.Core.Tests` validates Excel profile contracts inside `McpShared` without requiring Microsoft Excel or `HPExcel` assemblies, following the established `Sap2000ProfileTests` and `EtabsProfileTests` pattern.

---

## 3. Caveats

1. **Host-Neutrality Boundary**:
   - `McpShared` must **never** reference `Microsoft.Office.Interop.Excel.dll` or `ClosedXML.dll`. `HostScriptContracts.ExcelImports` contains string literals only; the actual assembly references and Roslyn metadata references belong exclusively to `HPExcel/` (`HPExcel.McpBridge` and `HPExcel.Mcp.Server`).
2. **Dynamic Keyword in Roslyn Scripts**:
   - `ScriptGuard` strictly denies `dynamic` keywords (`dynamic is not allowed in {host} scripts`). Therefore, Excel COM scripts and seed tools must use strongly-typed COM interop interfaces (`Microsoft.Office.Interop.Excel.Application`, `Workbook`, `Worksheet`, `Range`) rather than late-bound `dynamic` variables.
3. **Headless ClosedXML vs Live COM**:
   - When Excel is not running, `get_excel_context` will report `isAttached: false` and `hasActiveWorkbook: false`. ClosedXML operations target closed `.xlsx` files provided via tool parameters or `args`.

---

## 4. Conclusion & Required Code Additions

### 4.1 Exact Code Additions to `McpShared/`

#### A. `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`
Add the host constant (after line 43):
```csharp
    /// <summary>
    ///     Microsoft Excel; pipe <c>hpexcel-mcp-{version}</c> (e.g. 2026).
    /// </summary>
    public const string ExcelHost = "excel";
```
In `For(string host, int version)` switch statement (after line 66):
```csharp
            ExcelHost => "hpexcel-mcp-" + version,
```

#### B. `McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs`
Add the prefix constant (after line 41):
```csharp
    public const string ExcelPrefix = "excel.";
```

#### C. `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`
Add imports, globals, and heavy timeout (at end of class, after line 163):
```csharp
    /// <summary>
    ///     Default `using`s of an Excel script.
    ///     Includes Microsoft.Office.Interop.Excel, ClosedXML.Excel, and bridge scripting.
    /// </summary>
    public static readonly string[] ExcelImports =
    {
        "System", "System.Linq", "System.Collections.Generic",
        "Microsoft.Office.Interop.Excel",
        "ClosedXML.Excel",
        "HPRebar.McpBridge.Core.Scripting",
    };

    /// <summary>
    ///     Global names an Excel script may use: `excel` is the attached Excel.Application,
    ///     `workbook` is the active Workbook (or null), `sheet` is the active Worksheet (or null),
    ///     `ct` is the cancellation token, `log` writes to output, `progress` reports steps, `args` carries parameters.
    /// </summary>
    public static readonly string[] ExcelGlobals = { "excel", "workbook", "sheet", "ct", "log", "progress", "args" };

    /// <summary>Longest Excel run timeout allowed (seconds).</summary>
    public const int ExcelHeavyMaxTimeoutSeconds = 600;
```

#### D. `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`
In `ContextResult` (after line 39):
```csharp
    /// <summary>Excel-only facts; null for the other hosts (and omitted from the JSON).</summary>
    public ExcelInfo? Excel { get; set; }
```
Add record definition (after `PowerBiInfo`, line 179):
```csharp
/// <summary>
///     What an Excel script needs to know: whether attached to a live running Excel process,
///     attached PID, Excel version, active workbook name, active worksheet name, selection address,
///     whether write operations are enabled, whether destructive operations are enabled,
///     open workbook count, worksheet count, and whether there is an active workbook.
/// </summary>
public sealed record ExcelInfo(
    bool IsAttached,
    int? AttachedPid,
    string? ExcelVersion,
    string? ActiveWorkbookName,
    string? ActiveWorksheetName,
    string? SelectionAddress,
    bool WriteEnabled,
    bool DestructiveEnabled,
    int OpenWorkbookCount,
    int WorksheetCount,
    bool HasActiveWorkbook);
```

#### E. `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`
Add static profile (after line 119):
```csharp
    /// <summary>
    ///     Excel guard profile: standalone desktop bridge connected to Microsoft Excel via COM Interop
    ///     and ClosedXML. Denied: modal UI, application termination (Quit), dialog prompts (InputBox,
    ///     GetOpenFilename, GetSaveAsFilename), and bridge internals.
    /// </summary>
    public static readonly GuardProfile Excel = new GuardProfile(
        "Excel",
        deniedIdentifiers: new[] { "MessageBox" },
        deniedMembers: new[]
        {
            "Quit", "ApplicationExit", "InputBox", "GetOpenFilename", "GetSaveAsFilename",
        },
        deniedMembersOnIdentifier: new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["excel"] = new[] { "Quit" },
            ["app"] = new[] { "Quit" },
        },
        deniedNamespaces: new[] { "System.Windows.Forms", "HPExcel.McpBridge", "HPRebar.McpBridge.Core.Host" });
```

#### F. `McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs`
Add static profile (after line 40):
```csharp
    /// <summary>Excel has no transactions in scripts; safety is managed via tiers and file snapshots.</summary>
    public static readonly AnalyzerProfile Excel = new AnalyzerProfile(
        transactionTypeNames: Array.Empty<string>(),
        transactionMethodNames: Array.Empty<string>());
```

#### G. New Test Fixture: `McpShared/HPRebar.Mcp.Server.Core.Tests/ExcelTestProfile.cs`
```csharp
using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;

namespace HPRebar.Mcp.Server.Tests;

internal static class ExcelTestProfile
{
    public const string DisabledText = "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HPExcel MCP Bridge window.";
    public const string NotConnectedHint = "Start HPExcel.McpBridge.exe beside Microsoft Excel, open an active workbook, and tick 'Allow AI code execution' (pipe hpexcel-mcp-2026).";
    public const string TimeoutHint = "Excel may still be executing the script or macro; changes made before timeout persisted — check the snapshot in .hpexcel_snapshots before retrying.";

    public static HostProfile Excel(int maxTimeout = 600, string? notConnected = null, string? timeoutHint = null) => new HostProfile
    {
        HostId = PipeNaming.ExcelHost, DisplayName = "Excel", ServerName = "test", ProductFolder = "HPExcelTest", EnvPrefix = "X_",
        DefaultVersion = 2026, ValidVersions = new[] { 2021, 2024, 2026 }, MethodPrefix = JsonRpcMethods.ExcelPrefix,
        ExecuteToolName = "execute_excel_code", ContextToolName = "get_excel_context", ResourceScheme = "excel",
        Categories = new[] { "Workbook", "Worksheet", "Range", "Table", "Chart", "Formula", "VBA", "Export", "Data", "Generic" },
        CoreToolNames = new[] { "execute_excel_code", "get_excel_context", "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.ExcelImports, ScriptContractSummary = "test", HostAssembly = typeof(ExcelTestProfile).Assembly,
        MaxTimeoutSeconds = maxTimeout, BridgeNotConnectedHint = notConnected, TimeoutSemanticsHint = timeoutHint,
    };

    public static ToolRecord Candidate(int timeoutSeconds, string transaction = "none") => new ToolRecord
    {
        Name = "read_range_sample", Title = "Read Range", Description = "Reads values and formulas from cell range.",
        Category = "Range", Transaction = transaction, TimeoutSeconds = timeoutSeconds, Host = "excel",
        Code = "return excel.ActiveWorkbook.Name;",
        InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
        Examples = [new ToolExample { Title = "default", Args = JsonSerializer.Deserialize<JsonElement>("{}") }],
    };

    public static BridgeOptions PipeOptions(string pipe) => new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 };

    public static string NewPipe() => "hpexcel-mcp-test-" + Guid.NewGuid().ToString("N");
}
```

#### H. New Test Suite: `McpShared/HPRebar.Mcp.Server.Core.Tests/ExcelProfileTests.cs`
Comprehensive unit tests asserting:
1. `Excel_constants_produce_the_pipe_prefix_imports_and_globals`
2. `Excel_guard_profile_denies_quit_dialogs_bridge_internals_and_the_base_list`
3. `Excel_guard_profile_lets_reads_and_writes_through`
4. `Excel_analyzer_profile_never_reports_a_transaction`
5. `Hints_and_timeout_ceiling_survive_WithHostAssembly_and_default_to_null`
6. `Validator_ceiling_follows_the_excel_profile`
7. `ConfigureOptions_seeds_HostVersion_from_the_profile_and_configuration_still_wins`
8. `Wire_additions_are_invisible_when_unused`
9. `Excel_info_round_trips_in_camel_case`

### 4.2 Pattern for Downstream `HPExcel/` Implementation
For implementation in `HPExcel/`:
1. `HPExcel.Mcp.Server`:
   - Target: `net10.0`, OutputType: `Exe`
   - References `..\McpShared\HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj`
   - Defines `ExcelHostProfile.Instance` using `HostProfile`
   - `Program.cs`: `return await McpServerHost.RunAsync(args, ExcelHostProfile.Instance);`
2. `HPExcel.McpBridge`:
   - Target: `net8.0-windows`, UseWPF: `true`
   - References `..\McpShared\HPRebar.Mcp.Contracts` & `..\McpShared\HPRebar.McpBridge.Core`
   - Instantiates `RequestDispatcher(executor, settings, "2026", "Excel", executionDisabledMessage: ...)`
   - Instantiates `PipeListener("hpexcel-mcp-2026", dispatcher)`

---

## 5. Verification Method

### 5.1 Verification Commands
Once the changes above are applied to `McpShared/`:

```powershell
# From McpShared/
cd "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared"

# 1. Build McpShared
dotnet build McpShared.slnx

# 2. Run Engine Tests (including new ExcelProfileTests)
dotnet test HPRebar.Mcp.Server.Core.Tests

# 3. Run Net48 Bridge Tests
dotnet test HPRebar.McpBridge.Core.Net48Tests
```

### 5.2 Acceptance Criteria for McpShared Extension
1. `dotnet build McpShared.slnx` compiles with **0 errors and 0 warnings**.
2. `dotnet test HPRebar.Mcp.Server.Core.Tests` passes **100% of tests** (baseline 228 + new Excel profile tests = ~240 tests).
3. `dotnet test HPRebar.McpBridge.Core.Net48Tests` passes **100% of tests** (71 tests).
4. `HostNeutralityTests.Shared_assemblies_reference_no_host_api` passes without any reference to Office/Excel assemblies in McpShared.
5. Invalidation condition: Any failure in existing test suites or presence of non-nullable or non-wire-compatible changes in `HPRebar.Mcp.Contracts`.

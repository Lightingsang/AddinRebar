using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     The Excel host profile the engine tests use (the real one lives in the HPExcel server exe) plus the
///     message texts that exe is expected to supply, shared by the profile and the bridge-message tests.
/// </summary>
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

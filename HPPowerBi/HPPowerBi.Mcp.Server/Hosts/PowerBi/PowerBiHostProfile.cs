using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;

namespace HPPowerBi.Mcp.Server.Hosts;

/// <summary>
///     The Power BI profile: pipe <c>hppowerbi-mcp-{version}</c>, wire prefix <c>powerbi.</c>,
///     registry root <c>%AppData%\HPPowerBi\McpServer\</c>, timeout ceiling 600s, and this assembly
///     hosting the Power BI tool classes. The bridge is a standalone desktop application connecting to
///     Power BI Desktop's local Analysis Services tabular instance (msmdsrv.exe) and Power BI Service Cloud.
/// </summary>
public static class PowerBiHostProfile
{
    public const string ExecuteToolName = "execute_powerbi_code";
    public const string ContextToolName = "get_powerbi_context";

    public const int Version = 2026;

    public const int HeavyMaxTimeoutSeconds = HostScriptContracts.PowerBiHeavyMaxTimeoutSeconds;

    public const string BridgeExecutable = "HPPowerBi.McpBridge.exe";

    public static readonly HostProfile Instance = new HostProfile
    {
        HostId = PipeNaming.PowerBiHost,
        DisplayName = "Power BI",
        ServerName = "HPPowerBi MCP",
        ProductFolder = "HPPowerBi",
        EnvPrefix = "HPPOWERBI_MCP_",
        DefaultVersion = Version,
        ValidVersions = new[] { 2024, 2025, 2026 },
        MethodPrefix = JsonRpcMethods.PowerBiPrefix,
        ExecuteToolName = ExecuteToolName,
        ContextToolName = ContextToolName,
        ResourceScheme = PipeNaming.PowerBiHost,
        Categories = new[] { "Model", "Schema", "DAX", "Measure", "Relationship", "Cloud", "Data", "Generic" },
        CoreToolNames = new[]
        {
            ExecuteToolName, ContextToolName, "inspect_type", "cancel_execution",
            "powerbi_get_schema", "powerbi_evaluate_dax", "powerbi_create_or_update_measure",
            "powerbi_delete_measure", "powerbi_manage_relationship", "powerbi_format_dax",
            "powerbi_cloud_list_workspaces", "powerbi_cloud_list_datasets", "powerbi_cloud_trigger_refresh",
            "powerbi_cloud_execute_dax"
        },
        ScriptImports = HostScriptContracts.PowerBiImports,
        ScriptContractSummary =
            "Globals: model (active AMO-TOM Model), server (AMO-TOM Server connected to local Analysis Services), " +
            "adomd (active AdomdConnection for DAX queries), ct, log(string), progress(cur,total,msg), args. " +
            "Scripts can inspect tabular metadata, query DAX, or mutate measures/relationships. " +
            "Writing operations call model.SaveChanges() and are gated behind the 'Allow Model Modifications / DAX Execution' checkbox on the bridge UI. " +
            "A snapshot backup is created before mutations.",
        MaxTimeoutSeconds = HeavyMaxTimeoutSeconds,
        HostAssembly = typeof(PowerBiHostProfile).Assembly,
        CliExecutable = "HPPowerBi.Mcp.Server.exe",
        BridgeNotConnectedHint =
            $"Start {BridgeExecutable}, select a Power BI Desktop instance, and tick 'Allow Model Modifications / DAX Execution' (pipe {PipeNaming.For(PipeNaming.PowerBiHost, Version)}).",
        TimeoutSemanticsHint =
            "Power BI Analysis Services may still be executing the query or TOM commit; check the snapshot named in the bridge window before retrying.",
    };
}

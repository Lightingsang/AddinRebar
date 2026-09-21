using System.ComponentModel;
using System.Reflection;
using HPPowerBi.Mcp.Server.Hosts;
using HPPowerBi.Mcp.Server.Tools;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace HPPowerBi.Mcp.Server.Tests;

/// <summary>
///     Verifies that PowerBiHostProfile enforces all required profile invariants, constants,
///     hints, tool definitions, and options seeding.
/// </summary>
public sealed class PowerBiHostProfileTests
{
    [Fact]
    public void Profile_NamesThePowerBiPipe_Prefix_Tools_RegistryRoot_Ceiling_And_Hints()
    {
        var profile = PowerBiHostProfile.Instance;

        Assert.Equal("powerbi", profile.HostId);
        Assert.Equal(PipeNaming.PowerBiHost, profile.HostId);
        Assert.Equal("Power BI", profile.DisplayName);
        Assert.Equal("HPPowerBi MCP", profile.ServerName);
        Assert.Equal("HPPowerBi", profile.ProductFolder);
        Assert.Equal("HPPOWERBI_MCP_", profile.EnvPrefix);
        Assert.Equal(2026, profile.DefaultVersion);
        Assert.Equal([2024, 2025, 2026], profile.ValidVersions);
        Assert.Equal("hppowerbi-mcp-2026", profile.PipeName(2026));
        Assert.Equal(JsonRpcMethods.PowerBiPrefix, profile.MethodPrefix);
        Assert.Equal("powerbi.", profile.MethodPrefix);
        Assert.Equal("powerbi.execute", profile.Method("execute"));
        Assert.Equal("powerbi.dax", profile.Method("dax"));
        Assert.Equal("powerbi.schema", profile.Method("schema"));
        Assert.Equal("execute_powerbi_code", profile.ExecuteToolName);
        Assert.Equal("get_powerbi_context", profile.ContextToolName);
        Assert.Equal(600, profile.MaxTimeoutSeconds);
        Assert.Equal(HostScriptContracts.PowerBiHeavyMaxTimeoutSeconds, profile.MaxTimeoutSeconds);
        Assert.Equal(HostScriptContracts.PowerBiImports, profile.ScriptImports);
        Assert.Contains("Microsoft.AnalysisServices.Tabular", profile.ScriptImports);
        Assert.Contains("Microsoft.AnalysisServices.AdomdClient", profile.ScriptImports);
        Assert.Contains("model", profile.ScriptContractSummary);
        Assert.Contains("adomd", profile.ScriptContractSummary);
        Assert.Contains("SaveChanges", profile.ScriptContractSummary);
        Assert.Same(typeof(PowerBiHostProfile).Assembly, profile.HostAssembly);
        Assert.Equal("HPPowerBi.Mcp.Server.exe", profile.CliExecutable);

        Assert.Contains("Model", profile.Categories);
        Assert.Contains("Schema", profile.Categories);
        Assert.Contains("DAX", profile.Categories);
        Assert.Contains("Measure", profile.Categories);
        Assert.Contains("Relationship", profile.Categories);
        Assert.Contains("Cloud", profile.Categories);
        Assert.DoesNotContain(profile.Categories, c => c is "Architecture" or "Structure" or "Civil" or "Viewpoint" or "Clash");

        Assert.Equal(
            [
                "execute_powerbi_code",
                "get_powerbi_context",
                "inspect_type",
                "cancel_execution",
                "powerbi_get_schema",
                "powerbi_evaluate_dax",
                "powerbi_create_or_update_measure",
                "powerbi_delete_measure",
                "powerbi_manage_relationship",
                "powerbi_format_dax",
                "powerbi_cloud_list_workspaces",
                "powerbi_cloud_list_datasets",
                "powerbi_cloud_trigger_refresh",
                "powerbi_cloud_execute_dax"
            ],
            profile.CoreToolNames);

        Assert.NotNull(profile.BridgeNotConnectedHint);
        Assert.Contains("HPPowerBi.McpBridge.exe", profile.BridgeNotConnectedHint);
        Assert.Contains("hppowerbi-mcp-2026", profile.BridgeNotConnectedHint);
        Assert.Contains("Allow Model Modifications / DAX Execution", profile.BridgeNotConnectedHint);

        Assert.NotNull(profile.TimeoutSemanticsHint);
        Assert.Contains("Power BI Analysis Services", profile.TimeoutSemanticsHint);
        Assert.Contains("snapshot", profile.TimeoutSemanticsHint);
    }

    [Fact]
    public void ContextTool_Description_MentionsPowerBiProperties()
    {
        var method = typeof(GetPowerBiContextTool).GetMethod(nameof(GetPowerBiContextTool.GetContextAsync));
        Assert.NotNull(method);

        var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
        Assert.NotNull(description);

        Assert.Contains("hostVersion (2026)", description);
        Assert.Contains("docTitle", description);
        Assert.Contains("docPath", description);
        Assert.Contains("isModifiable", description);
        Assert.Contains("executionEnabled", description);
        Assert.Contains("powerBi info", description);
        Assert.Contains("isConnected", description);
        Assert.Contains("attachedPid", description);
        Assert.Contains("localPort", description);
        Assert.Contains("databaseName", description);
        Assert.Contains("compatibilityLevel", description);
        Assert.Contains("mutationEnabled", description);
        Assert.Contains("tableCount, measureCount, relationshipCount", description);
    }

    [Fact]
    public void ExecuteTool_Description_CoversGlobals_MutationSafety_And_Timeout()
    {
        var text = ExecutePowerBiCodeTool.ToolDescription;

        Assert.Contains("model (active TOM Model)", text);
        Assert.Contains("server (TOM Server", text);
        Assert.Contains("adomd (active AdomdConnection", text);
        Assert.Contains("ct, log(string), progress", text);
        Assert.Contains("args", text);
        Assert.Contains("model.SaveChanges()", text);
        Assert.Contains("dryRun", text);
        Assert.Contains("600", text);
        Assert.Contains("snapshot backup", text);
    }

    [Fact]
    public void Builder_Seeds_PowerBiProfile_And_HostVersion()
    {
        var builder = McpServerHost.CreateBuilder(["--host-version", "2026"], PowerBiHostProfile.Instance);
        using var host = builder.Build();

        var bridge = host.Services.GetRequiredService<IOptions<BridgeOptions>>().Value;
        Assert.Equal(2026, bridge.HostVersion);
        Assert.Equal("powerbi", bridge.HostId);

        var reg = host.Services.GetRequiredService<IOptions<RegistryOptions>>().Value;
        Assert.Equal("HPPowerBi", reg.ProductFolder);
    }
}

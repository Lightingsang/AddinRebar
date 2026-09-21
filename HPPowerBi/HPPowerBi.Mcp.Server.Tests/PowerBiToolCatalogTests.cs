using HPPowerBi.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Bootstrap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

namespace HPPowerBi.Mcp.Server.Tests;

/// <summary>
///     Verifies that building the MCP server with PowerBiHostProfile discovers and registers
///     all 12 Power BI tools, the 2 core diagnostic tools, the 8 registry meta-tools,
///     the Power BI resources and prompts, and prevents any cross-host contamination.
/// </summary>
public sealed class PowerBiToolCatalogTests
{
    private static readonly string[] RegistryTools =
    [
        "search_tools",
        "get_tool",
        "run_tool",
        "get_run",
        "propose_tool",
        "test_tool",
        "publish_tool",
        "manage_tool"
    ];

    private static readonly string[] PowerBiTools =
    [
        "get_powerbi_context",
        "execute_powerbi_code",
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
    ];

    [Fact]
    public void Server_Registers_All_PowerBi_And_Registry_Tools()
    {
        using var host = McpServerHost.CreateBuilder([], PowerBiHostProfile.Instance).Build();

        var tools = host.Services.GetServices<McpServerTool>().ToArray();
        var toolNames = tools.Select(t => t.ProtocolTool.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        // 12 Power BI tools + 2 engine core tools (inspect_type, cancel_execution) + 8 registry tools = 22 tools
        Assert.Equal(22, toolNames.Length);

        foreach (var pbiTool in PowerBiTools)
        {
            Assert.Contains(pbiTool, toolNames);
        }

        Assert.Contains("inspect_type", toolNames);
        Assert.Contains("cancel_execution", toolNames);

        foreach (var regTool in RegistryTools)
        {
            Assert.Contains(regTool, toolNames);
        }

        // Zero cross-host contamination
        Assert.DoesNotContain(toolNames, n =>
            n.Contains("revit", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("autocad", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("navis", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("etabs", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("sap2000", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("civil3d", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Tools_HaveValidAnnotationsAndDescriptions()
    {
        using var host = McpServerHost.CreateBuilder([], PowerBiHostProfile.Instance).Build();
        var tools = host.Services.GetServices<McpServerTool>().ToDictionary(t => t.ProtocolTool.Name, t => t.ProtocolTool);

        // get_powerbi_context: read-only
        var context = tools["get_powerbi_context"];
        Assert.True(context.Annotations?.ReadOnlyHint);
        Assert.False(context.Annotations?.DestructiveHint ?? false);
        Assert.Contains("hostVersion", context.Description);

        // execute_powerbi_code: destructive
        var execute = tools["execute_powerbi_code"];
        Assert.True(execute.Annotations?.DestructiveHint);
        Assert.False(execute.Annotations?.ReadOnlyHint ?? false);
        Assert.Contains("model", execute.Description);

        // powerbi_get_schema: read-only
        var schema = tools["powerbi_get_schema"];
        Assert.True(schema.Annotations?.ReadOnlyHint);
        Assert.Contains("schema", schema.Description);

        // powerbi_evaluate_dax: read-only
        var dax = tools["powerbi_evaluate_dax"];
        Assert.True(dax.Annotations?.ReadOnlyHint);
        Assert.Contains("DAX", dax.Description);

        // powerbi_create_or_update_measure: destructive
        var upsertMeasure = tools["powerbi_create_or_update_measure"];
        Assert.True(upsertMeasure.Annotations?.DestructiveHint);

        // powerbi_delete_measure: destructive
        var deleteMeasure = tools["powerbi_delete_measure"];
        Assert.True(deleteMeasure.Annotations?.DestructiveHint);

        // powerbi_manage_relationship: destructive
        var relationship = tools["powerbi_manage_relationship"];
        Assert.True(relationship.Annotations?.DestructiveHint);

        // powerbi_format_dax: read-only
        var formatDax = tools["powerbi_format_dax"];
        Assert.True(formatDax.Annotations?.ReadOnlyHint);

        // Cloud tools
        var workspaces = tools["powerbi_cloud_list_workspaces"];
        Assert.True(workspaces.Annotations?.ReadOnlyHint);

        var datasets = tools["powerbi_cloud_list_datasets"];
        Assert.True(datasets.Annotations?.ReadOnlyHint);

        var refresh = tools["powerbi_cloud_trigger_refresh"];
        Assert.True(refresh.Annotations?.DestructiveHint);

        var cloudDax = tools["powerbi_cloud_execute_dax"];
        Assert.True(cloudDax.Annotations?.ReadOnlyHint);
    }

    [Fact]
    public void Resources_And_Prompts_UsePowerBiScheme_And_Names()
    {
        using var host = McpServerHost.CreateBuilder([], PowerBiHostProfile.Instance).Build();

        var resources = host.Services.GetServices<McpServerResource>().Select(r => r.ProtocolResourceTemplate.UriTemplate).ToArray();
        var prompts = host.Services.GetServices<McpServerPrompt>().Select(p => p.ProtocolPrompt.Name).ToArray();

        Assert.Contains("powerbi://schema", resources);
        Assert.Contains("powerbi://document/info", resources);
        Assert.DoesNotContain(resources, r =>
            r.StartsWith("revit://", StringComparison.Ordinal) ||
            r.StartsWith("autocad://", StringComparison.Ordinal) ||
            r.StartsWith("navis://", StringComparison.Ordinal) ||
            r.StartsWith("etabs://", StringComparison.Ordinal) ||
            r.StartsWith("sap2000://", StringComparison.Ordinal) ||
            r.StartsWith("civil3d://", StringComparison.Ordinal));

        Assert.Contains("powerbi_dax_optimize", prompts);
        Assert.Contains("toolify_run", prompts);
        Assert.DoesNotContain(prompts, p =>
            p.StartsWith("revit_", StringComparison.Ordinal) ||
            p.StartsWith("autocad_", StringComparison.Ordinal) ||
            p.StartsWith("navis_", StringComparison.Ordinal) ||
            p.StartsWith("etabs_", StringComparison.Ordinal) ||
            p.StartsWith("sap2000_", StringComparison.Ordinal) ||
            p.StartsWith("civil3d_", StringComparison.Ordinal));
    }

    [Fact]
    public void ServerName_IsHPPowerBiMcp()
    {
        using var host = McpServerHost.CreateBuilder([], PowerBiHostProfile.Instance).Build();

        var options = host.Services.GetRequiredService<IOptions<McpServerOptions>>().Value;

        Assert.Equal("HPPowerBi MCP", options.ServerInfo?.Name);
    }
}

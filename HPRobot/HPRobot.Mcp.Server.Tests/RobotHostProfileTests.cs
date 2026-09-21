using System.ComponentModel;
using System.Reflection;
using HPRobot.Mcp.Server.Hosts.Robot;
using HPRobot.Mcp.Server.Hosts.Robot.Tools;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

namespace HPRobot.Mcp.Server.Tests;

/// <summary>
///     What this exe registers: the Robot pipe (version 2026), prefix, registry root, 300 s ceiling,
///     the two message hints that name the bridge program, and a tool surface that holds the Robot core tools
///     plus the engine's registry tools — nothing named after other hosts.
/// </summary>
public sealed class RobotHostProfileTests
{
    private static readonly string[] ExpectedRegistryTools =
    [
        "search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"
    ];

    [Fact]
    public void Profile_names_the_robot_pipe_prefix_tools_registry_root_ceiling_and_hints()
    {
        var profile = RobotHostProfile.Instance;

        Assert.Equal("robot", profile.HostId);
        Assert.Equal("Robot Structural Analysis", profile.DisplayName);
        Assert.Equal("HPRobot MCP", profile.ServerName);
        Assert.Equal("hprobot-mcp-2026", profile.PipeName(2026));
        Assert.Equal("robot.execute", profile.Method("execute"));
        Assert.Equal("robot.ping", profile.Method("ping"));
        Assert.Equal("robot.context", profile.Method("context"));
        Assert.Equal("robot.cancel", profile.Method("cancel"));
        Assert.Equal("execute_robot_code", ((IHostProfile)profile).ExecuteToolName);
        Assert.Equal("get_robot_context", ((IHostProfile)profile).ContextToolName);
        Assert.Equal("HPRobot", profile.ProductFolder);
        Assert.Equal("HPROBOT_MCP_", profile.EnvPrefix);
        Assert.Equal(2026, profile.DefaultVersion);
        Assert.Equal([2024, 2025, 2026], profile.ValidVersions);
        Assert.Equal(300, profile.MaxTimeoutSeconds);
        Assert.Equal(HostScriptContracts.RobotImports, profile.ScriptImports);
        Assert.Contains("RobotOM", profile.ScriptImports);
        Assert.Contains("HPRebar.McpBridge.Core.Scripting", profile.ScriptImports);
        Assert.Equal<string>(["robot", "structure", "units", "ct", "log", "progress", "args"], HostScriptContracts.RobotGlobals);
        Assert.Same(typeof(RobotHostProfile).Assembly, profile.HostAssembly);
        Assert.Equal("HPRobot.Mcp.Server.exe", profile.CliExecutable);
        Assert.Equal("HPRobot.McpBridge.exe", RobotHostProfile.BridgeExecutable);
        Assert.Contains("Model", profile.Categories);
        Assert.Contains("Geometry", profile.Categories);
        Assert.Contains("Property", profile.Categories);
        Assert.Contains("Load", profile.Categories);
        Assert.Contains("Analysis", profile.Categories);
        Assert.Contains("Results", profile.Categories);
        Assert.Contains("Generic", profile.Categories);
        Assert.DoesNotContain(profile.Categories, c => c is "Layer" or "Block" or "Wall" or "Clash" or "Viewpoint");
        Assert.Equal(["execute_robot_code", "get_robot_context", "inspect_type", "cancel_execution"], profile.CoreToolNames);

        Assert.Contains("kN·m", profile.ScriptContractSummary);
        Assert.Contains("MPa", profile.ScriptContractSummary);
        Assert.Contains("no transaction", profile.ScriptContractSummary);
        Assert.Contains("snapshot", profile.ScriptContractSummary);
        Assert.Contains("Allow heavy/destructive operations", profile.ScriptContractSummary);
        Assert.Contains("PREVIEW", profile.ScriptContractSummary);

        Assert.Contains("HPRobot.McpBridge.exe", profile.BridgeNotConnectedHint);
        Assert.Contains("hprobot-mcp-2026", profile.BridgeNotConnectedHint);
        Assert.Contains("Attach", profile.BridgeNotConnectedHint);
        Assert.Contains("Allow AI code execution", profile.BridgeNotConnectedHint);
        Assert.Contains("persisted (no rollback)", profile.TimeoutSemanticsHint);
        Assert.Contains("snapshot named in the bridge window", profile.TimeoutSemanticsHint);
        Assert.DoesNotContain(@"C:\", profile.BridgeNotConnectedHint + profile.TimeoutSemanticsHint);
        Assert.DoesNotContain(Environment.UserName, profile.BridgeNotConnectedHint);
    }

    [Fact]
    public void Context_tool_description_names_the_robot_properties()
    {
        var description = typeof(GetRobotContextTool).GetMethod(nameof(GetRobotContextTool.GetContextAsync))!
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .Cast<DescriptionAttribute>()
            .Single().Description;

        Assert.Contains("hostVersion (2026)", description);
        Assert.Contains("docTitle", description);
        Assert.Contains("docPath", description);
        Assert.Contains("isModifiable", description);
        Assert.Contains("robot info", description);
        Assert.Contains("isAttached", description);
        Assert.Contains("attachedPid", description);
        Assert.Contains("robotVersion", description);
        Assert.Contains("structureType", description);
        Assert.Contains("isCalculated", description);
        Assert.Contains("heavyOperationsEnabled", description);
        Assert.Contains("nodeCount", description);
        Assert.Contains("barCount", description);
        Assert.Contains("panelCount", description);
        Assert.Contains("loadCaseCount", description);
        Assert.Contains("includeSelection", description);
    }

    [Fact]
    public void Execute_tool_description_covers_tiers_save_and_preview_under_1800_characters()
    {
        var text = ExecuteRobotCodeTool.ToolDescription;

        Assert.True(text.Length < 1800, $"description length {text.Length} exceeds the 1800-char budget");
        Assert.Contains("Robot Structural Analysis Professional 2026", text);
        Assert.Contains("robot (IRobotApplication)", text);
        Assert.Contains("structure (IRobotStructure)", text);
        Assert.Contains("units (IRobotUnitMngr", text);
        Assert.Contains("metric m, kN, kN·m, MPa", text);
        Assert.Contains("no transaction or undo", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".rtd snapshot", text);
        Assert.Contains("Allow heavy/destructive operations", text);
        Assert.Contains("PREVIEW", text);
        Assert.Contains("dryRun", text);
        Assert.Contains("HPRobot MCP Bridge", text);
    }

    [Fact]
    public void Builder_seeds_robot_profile_and_host_version_from_the_profile()
    {
        var builder = McpServerHost.CreateBuilder(["--host-version", "2026"], RobotHostProfile.Instance);
        var host = builder.Build();

        var bridge = host.Services.GetRequiredService<IOptions<BridgeOptions>>().Value;
        Assert.Equal(2026, bridge.HostVersion);
        Assert.Equal("hprobot-mcp-2026", bridge.PipeName);

        var reg = host.Services.GetRequiredService<IOptions<RegistryOptions>>().Value;
        Assert.Equal("manual", reg.PublishPolicy);
        Assert.Contains(Path.Combine("HPRobot", "McpServer"), reg.LibraryPath);
    }

    [Fact]
    public void InvalidHostVersion_IsRefusedByValidation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Bridge:HostVersion"] = "2020" })
            .Build();
        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, RobotHostProfile.Instance);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<BridgeOptions>>().Value);
    }

    [Fact]
    public void ToolSurface_ContainsFourCoreTools_EightRegistryTools_NoForeignHostTools()
    {
        using var host = McpServerHost.CreateBuilder([], RobotHostProfile.Instance).Build();

        var toolNames = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(12, toolNames.Length);
        Assert.Contains("execute_robot_code", toolNames);
        Assert.Contains("get_robot_context", toolNames);
        Assert.Contains("inspect_type", toolNames);
        Assert.Contains("cancel_execution", toolNames);
        Assert.All(ExpectedRegistryTools, regTool => Assert.Contains(regTool, toolNames));
        Assert.DoesNotContain(toolNames, n =>
            n.Contains("revit", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("autocad", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("etabs", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("sap2000", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("navis", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("excel", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("powerbi", StringComparison.OrdinalIgnoreCase));

        var execTool = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "execute_robot_code").ProtocolTool;
        Assert.True(execTool.Annotations?.DestructiveHint);
        Assert.False(execTool.Annotations?.ReadOnlyHint);
        Assert.Equal(ExecuteRobotCodeTool.ToolDescription, execTool.Description);

        var ctxTool = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "get_robot_context").ProtocolTool;
        Assert.True(ctxTool.Annotations?.ReadOnlyHint);
        Assert.True(ctxTool.Annotations?.IdempotentHint);
        Assert.Equal(GetRobotContextTool.ToolDescription, ctxTool.Description);
    }

    [Fact]
    public void ResourcesAndPrompts_UseRobotSchemeAndNames()
    {
        using var host = McpServerHost.CreateBuilder([], RobotHostProfile.Instance).Build();

        var resources = host.Services.GetServices<McpServerResource>()
            .Select(r => r.ProtocolResourceTemplate.UriTemplate)
            .ToArray();
        var prompts = host.Services.GetServices<McpServerPrompt>()
            .Select(p => p.ProtocolPrompt.Name)
            .ToArray();

        Assert.Contains("robot://model/info", resources);
        Assert.Contains("robot://selection", resources);
        Assert.DoesNotContain(resources, r =>
            r.StartsWith("revit://", StringComparison.Ordinal) ||
            r.StartsWith("autocad://", StringComparison.Ordinal) ||
            r.StartsWith("etabs://", StringComparison.Ordinal) ||
            r.StartsWith("sap2000://", StringComparison.Ordinal) ||
            r.StartsWith("navis://", StringComparison.Ordinal));

        Assert.Contains("robot_query_template", prompts);
        Assert.Contains("robot_modify_template", prompts);
        Assert.Contains("robot_analysis_template", prompts);
        Assert.Contains("toolify_run", prompts);
        Assert.DoesNotContain(prompts, p =>
            p.StartsWith("revit_", StringComparison.Ordinal) ||
            p.StartsWith("autocad_", StringComparison.Ordinal) ||
            p.StartsWith("etabs_", StringComparison.Ordinal) ||
            p.StartsWith("sap2000_", StringComparison.Ordinal) ||
            p.StartsWith("navis_", StringComparison.Ordinal));
    }

    [Fact]
    public void Server_name_is_the_robot_one()
    {
        using var host = McpServerHost.CreateBuilder([], RobotHostProfile.Instance).Build();

        var options = host.Services.GetRequiredService<IOptions<McpServerOptions>>().Value;

        Assert.Equal("HPRobot MCP", options.ServerInfo?.Name);
    }
}

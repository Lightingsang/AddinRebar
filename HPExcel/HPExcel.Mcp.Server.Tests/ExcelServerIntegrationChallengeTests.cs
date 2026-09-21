using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using HPExcel.Mcp.Server.Hosts;
using HPExcel.Mcp.Server.Tools;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

namespace HPExcel.Mcp.Server.Tests;

/// <summary>
///     Empirical integration challenge tests for Milestone M3:
///     1. ExcelHostProfile constants, contracts, hints, and invariants.
///     2. Server DI container building, services registration, and tool discovery from host assembly.
///     3. Server CLI and stdio process startup, initialization, and tool catalog responses.
/// </summary>
public sealed class ExcelServerIntegrationChallengeTests
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

    private static readonly string[] ExpectedSeedTools =
    [
        "read_range",
        "read_worksheet_info",
        "find_cells",
        "read_table",
        "write_range",
        "format_range",
        "manage_worksheet",
        "create_table",
        "create_chart",
        "evaluate_formula",
        "export_worksheet",
        "run_macro"
    ];

    // =========================================================================
    // 1. ExcelHostProfile Verification
    // =========================================================================

    [Fact]
    public void ExcelHostProfile_MethodPrefix_Equals_ExcelPrefix()
    {
        var profile = ExcelHostProfile.Instance;
        Assert.Equal("excel.", profile.MethodPrefix);
        Assert.Equal(JsonRpcMethods.ExcelPrefix, profile.MethodPrefix);
    }

    [Fact]
    public void ExcelHostProfile_HostId_Equals_Excel()
    {
        var profile = ExcelHostProfile.Instance;
        Assert.Equal("excel", profile.HostId);
        Assert.Equal(PipeNaming.ExcelHost, profile.HostId);
    }

    [Fact]
    public void ExcelHostProfile_MaxTimeoutSeconds_Equals_600()
    {
        var profile = ExcelHostProfile.Instance;
        Assert.Equal(600, profile.MaxTimeoutSeconds);
        Assert.Equal(HostScriptContracts.ExcelHeavyMaxTimeoutSeconds, profile.MaxTimeoutSeconds);
    }

    [Fact]
    public void ExcelHostProfile_DefaultVersion_Equals_2026()
    {
        var profile = ExcelHostProfile.Instance;
        Assert.Equal(2026, profile.DefaultVersion);
        Assert.Equal(ExcelHostProfile.Version, profile.DefaultVersion);
        Assert.Equal([2026], profile.ValidVersions);
    }

    [Fact]
    public void ExcelHostProfile_CoreToolNames_Contains_All_Four_Expected_Names()
    {
        var profile = ExcelHostProfile.Instance;

        Assert.Equal(4, profile.CoreToolNames.Count);
        Assert.Contains("execute_excel_code", profile.CoreToolNames);
        Assert.Contains("get_excel_context", profile.CoreToolNames);
        Assert.Contains("inspect_type", profile.CoreToolNames);
        Assert.Contains("cancel_execution", profile.CoreToolNames);

        Assert.Equal(ExcelHostProfile.ExecuteToolName, profile.ExecuteToolName);
        Assert.Equal(ExcelHostProfile.ContextToolName, profile.ContextToolName);
    }

    [Fact]
    public void ExcelHostProfile_Additional_Metadata_And_Hints_Are_Valid()
    {
        var profile = ExcelHostProfile.Instance;

        Assert.Equal("Excel", profile.DisplayName);
        Assert.Equal("HPExcel MCP", profile.ServerName);
        Assert.Equal("HPExcel", profile.ProductFolder);
        Assert.Equal("HPEXCEL_MCP_", profile.EnvPrefix);
        Assert.Equal("hpexcel-mcp-2026", profile.PipeName(2026));
        Assert.Equal("excel.execute", profile.Method("execute"));
        Assert.Equal("excel.context", profile.Method("context"));
        Assert.Equal("excel.cancel", profile.Method("cancel"));

        Assert.Same(typeof(ExcelHostProfile).Assembly, profile.HostAssembly);
        Assert.Equal("HPExcel.Mcp.Server.exe", profile.CliExecutable);

        Assert.Contains("Data", profile.Categories);
        Assert.Contains("Workbook", profile.Categories);
        Assert.Contains("Format", profile.Categories);
        Assert.Contains("Chart", profile.Categories);
        Assert.Contains("Calculation", profile.Categories);
        Assert.Contains("Export", profile.Categories);
        Assert.Contains("Automation", profile.Categories);
        Assert.Contains("Generic", profile.Categories);

        Assert.DoesNotContain(profile.Categories, c => c is "Architecture" or "Structure" or "Civil" or "Viewpoint" or "Clash");

        // Hints
        Assert.NotNull(profile.BridgeNotConnectedHint);
        Assert.Contains("HPExcel.McpBridge.exe", profile.BridgeNotConnectedHint);
        Assert.Contains("hpexcel-mcp-2026", profile.BridgeNotConnectedHint);
        Assert.Contains("Allow AI code execution", profile.BridgeNotConnectedHint);

        Assert.NotNull(profile.TimeoutSemanticsHint);
        Assert.Contains("snapshot", profile.TimeoutSemanticsHint);
        Assert.Contains("modal dialogs", profile.TimeoutSemanticsHint);
    }

    // =========================================================================
    // 2. Server DI & Tool Discovery Verification
    // =========================================================================

    [Fact]
    public void McpServerHost_CreateBuilder_Builds_Without_Exceptions()
    {
        var builder = McpServerHost.CreateBuilder([], ExcelHostProfile.Instance);
        Assert.NotNull(builder);

        using var host = builder.Build();
        Assert.NotNull(host);

        // Core singletons resolved from DI
        Assert.NotNull(host.Services.GetRequiredService<IRevitBridgeClient>());
        Assert.NotNull(host.Services.GetRequiredService<ResultFormatter>());
        Assert.NotNull(host.Services.GetRequiredService<ExecuteCodeService>());
        Assert.NotNull(host.Services.GetRequiredService<ContextService>());
        Assert.NotNull(host.Services.GetRequiredService<ToolLibraryStore>());
        Assert.NotNull(host.Services.GetRequiredService<ToolRegistryDb>());
        Assert.NotNull(host.Services.GetRequiredService<ToolManager>());
        Assert.NotNull(host.Services.GetRequiredService<ToolLifecycleService>());
        Assert.NotNull(host.Services.GetRequiredService<DynamicToolRegistrar>());
        Assert.NotNull(host.Services.GetRequiredService<IHostProfile>());
        Assert.Same(ExcelHostProfile.Instance, host.Services.GetRequiredService<IHostProfile>());
    }

    [Fact]
    public void Tool_Discovery_Resolves_Core_Tools_And_Registry_Tools()
    {
        using var host = McpServerHost.CreateBuilder([], ExcelHostProfile.Instance).Build();

        var tools = host.Services.GetServices<McpServerTool>().ToArray();
        var toolNames = tools.Select(t => t.ProtocolTool.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        // 4 core tools + 8 registry meta tools = 12 statically discovered tools from assemblies
        Assert.Equal(12, toolNames.Length);

        // Core host tools (from HPExcel.Mcp.Server assembly)
        Assert.Contains("execute_excel_code", toolNames);
        Assert.Contains("get_excel_context", toolNames);

        // Core utility tools (from HPRebar.Mcp.Server.Core engine assembly)
        Assert.Contains("inspect_type", toolNames);
        Assert.Contains("cancel_execution", toolNames);

        // 8 Registry meta tools (from engine assembly)
        Assert.All(RegistryTools, rt => Assert.Contains(rt, toolNames));

        // Ensure host isolation: no tools from Revit, AutoCAD, Navisworks, ETABS, Civil 3D, SAP2000, Power BI
        Assert.DoesNotContain(toolNames, n => n.Contains("revit", StringComparison.OrdinalIgnoreCase)
                                          || n.Contains("autocad", StringComparison.OrdinalIgnoreCase)
                                          || n.Contains("navis", StringComparison.OrdinalIgnoreCase)
                                          || n.Contains("etabs", StringComparison.OrdinalIgnoreCase)
                                          || n.Contains("civil3d", StringComparison.OrdinalIgnoreCase)
                                          || n.Contains("sap2000", StringComparison.OrdinalIgnoreCase)
                                          || n.Contains("powerbi", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExecuteExcelCodeTool_Has_Correct_Attributes_And_Description()
    {
        using var host = McpServerHost.CreateBuilder([], ExcelHostProfile.Instance).Build();

        var executeTool = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "execute_excel_code").ProtocolTool;
        Assert.True(executeTool.Annotations?.DestructiveHint);
        Assert.False(executeTool.Annotations?.ReadOnlyHint);

        var description = executeTool.Description!;
        Assert.Contains("excel (Excel.Application)", description);
        Assert.Contains("workbook (active Workbook)", description);
        Assert.Contains("sheet (active Worksheet)", description);
        Assert.Contains("ClosedXML.Excel", description);
        Assert.Contains("Three tiers", description);
        Assert.Contains("R read-only", description);
        Assert.Contains("W write", description);
        Assert.Contains("D destructive", description);
        Assert.Contains("snapshot", description);
        Assert.Contains("dryRun", description);
        Assert.Contains("Allow AI code execution", description);
        Assert.Equal(ExecuteExcelCodeTool.ToolDescription, description);
    }

    [Fact]
    public void ExcelContextTool_Has_Correct_Attributes_And_Description()
    {
        using var host = McpServerHost.CreateBuilder([], ExcelHostProfile.Instance).Build();

        var contextTool = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "get_excel_context").ProtocolTool;
        Assert.True(contextTool.Annotations?.ReadOnlyHint);
        Assert.False(contextTool.Annotations?.DestructiveHint);
        Assert.True(contextTool.Annotations?.IdempotentHint);

        var description = contextTool.Description!;
        Assert.Contains("hostVersion (2026)", description);
        Assert.Contains("active workbook", description);
        Assert.Contains("active sheet name", description);
        Assert.Contains("selected cells/range", description);
        Assert.Contains("worksheet count", description);
        Assert.Contains("open workbook count", description);
    }

    [Fact]
    public void Options_Binding_Correctly_Configures_Excel_Defaults()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, ExcelHostProfile.Instance);
        using var provider = services.BuildServiceProvider();

        var bridgeOptions = provider.GetRequiredService<IOptions<BridgeOptions>>().Value;
        Assert.Equal("excel", bridgeOptions.HostId);
        Assert.Equal(2026, bridgeOptions.HostVersion);
        Assert.Equal("hpexcel-mcp-2026", bridgeOptions.PipeName);

        var registryOptions = provider.GetRequiredService<IOptions<RegistryOptions>>().Value;
        Assert.Equal("HPExcel", registryOptions.ProductFolder);
        Assert.Contains(Path.Combine("HPExcel", "McpServer"), registryOptions.LibraryPath);
        Assert.Contains(Path.Combine("HPExcel", "McpServer"), registryOptions.DbPath);
    }

    // =========================================================================
    // 3. Server Startup & CLI Execution
    // =========================================================================

    [Fact]
    public async Task Server_CLI_Registry_Help_Runs_Successfully()
    {
        var builder = McpServerHost.CreateBuilder(["registry", "help"], ExcelHostProfile.Instance);
        using var host = builder.Build();

        var writer = new StringWriter();
        var exitCode = await RegistryCli.RunAsync(host.Services, ["help"], writer);

        Assert.Equal(0, exitCode);
        var output = writer.ToString();
        Assert.Contains("HPExcel.Mcp.Server.exe registry <command> [options]", output);
        Assert.Contains("list", output);
        Assert.Contains("pending", output);
        Assert.Contains("approve", output);
        Assert.Contains("deprecate", output);
    }

    [Fact]
    public async Task Server_Process_Bootstraps_Over_Stdio_And_Responds_To_Initialize_And_ToolsList()
    {
        var exePath = Path.Combine(AppContext.BaseDirectory, "HPExcel.Mcp.Server.exe");
        Assert.True(File.Exists(exePath), $"HPExcel.Mcp.Server.exe not found at {exePath}");

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardInputEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        try
        {
            async Task<JsonDocument> ReadResponseAsync(int expectedId)
            {
                while (true)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    Assert.NotNull(line);
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var doc = JsonDocument.Parse(line);
                    if (doc.RootElement.TryGetProperty("id", out var idProp) && idProp.GetInt32() == expectedId)
                    {
                        return doc;
                    }
                }
            }

            // 1. Send initialize request
            var initJson = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"test-client\",\"version\":\"1.0.0\"}}}";
            await process.StandardInput.WriteLineAsync(initJson);
            await process.StandardInput.FlushAsync();

            using var initDoc = await ReadResponseAsync(1);
            var root = initDoc.RootElement;
            Assert.Equal("2.0", root.GetProperty("jsonrpc").GetString());
            Assert.Equal(1, root.GetProperty("id").GetInt32());

            var serverInfo = root.GetProperty("result").GetProperty("serverInfo");
            Assert.Equal("HPExcel MCP", serverInfo.GetProperty("name").GetString());

            // 2. Send initialized notification
            var initializedNotification = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}";
            await process.StandardInput.WriteLineAsync(initializedNotification);
            await process.StandardInput.FlushAsync();

            // 3. Send tools/list request
            var toolsListRequest = "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\",\"params\":{}}";
            await process.StandardInput.WriteLineAsync(toolsListRequest);
            await process.StandardInput.FlushAsync();

            using var toolsDoc = await ReadResponseAsync(2);
            var toolsRoot = toolsDoc.RootElement;
            Assert.Equal("2.0", toolsRoot.GetProperty("jsonrpc").GetString());
            Assert.Equal(2, toolsRoot.GetProperty("id").GetInt32());

            var toolsArray = toolsRoot.GetProperty("result").GetProperty("tools");
            var toolNames = toolsArray.EnumerateArray()
                .Select(t => t.GetProperty("name").GetString()!)
                .ToHashSet(StringComparer.Ordinal);

            // Total 24 tools (4 core + 8 registry + 12 seed tools)
            Assert.Equal(24, toolNames.Count);

            // Verify core tools
            Assert.Contains("execute_excel_code", toolNames);
            Assert.Contains("get_excel_context", toolNames);
            Assert.Contains("inspect_type", toolNames);
            Assert.Contains("cancel_execution", toolNames);

            // Verify registry tools
            Assert.All(RegistryTools, rt => Assert.Contains(rt, toolNames));

            // Verify 12 seed tools
            Assert.All(ExpectedSeedTools, st => Assert.Contains(st, toolNames));
        }
        finally
        {
            if (!process.HasExited)
            {
                process.StandardInput.Close();
                if (!process.WaitForExit(3000))
                {
                    process.Kill();
                }
            }
        }
    }
}

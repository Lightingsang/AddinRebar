using System.Text.Json;
using HPPowerBi.Mcp.Server.Hosts;
using HPPowerBi.Mcp.Server.Prompts;
using HPPowerBi.Mcp.Server.Resources;
using HPPowerBi.Mcp.Server.Tools;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Xunit;

namespace HPPowerBi.Mcp.Server.Tests;

/// <summary>
///     Empirical challenge test suite for Milestone 3:
///     Stresses argument validation, boundary clamping, format fallbacks, error propagation,
///     and prompt generation across Power BI MCP tools and resources.
/// </summary>
public sealed class PowerBiMilestone3EmpiricalChallengeTests : IAsyncLifetime
{
    private readonly string _pipeName = "hppowerbi-mcp-challenge-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new FakeRevitExecutor();
    private readonly BridgeSettings _settings = new BridgeSettings { ExecutionEnabled = true };

    private PipeListener _listener = null!;
    private RevitBridgeClient _client = null!;
    private ContextService _context = null!;
    private ResultFormatter _formatter = null!;
    private IOptions<BridgeOptions> _options = null!;

    public string? LastCustomMethod { get; private set; }
    public JsonElement? LastCustomParams { get; private set; }

    // Configurable behavior for custom RPC responses
    public Func<long, JsonRpcEnvelope, Task<JsonRpcEnvelope?>>? CustomResponseFactory { get; set; }

    public ValueTask InitializeAsync()
    {
        var dispatcher = new RequestDispatcher(
            _executor,
            _settings,
            "2026",
            "Power BI",
            "Execution disabled",
            customHandler: DispatchCustomAsync);

        _listener = new PipeListener(_pipeName, dispatcher);
        _listener.Start();

        _options = Options.Create(new BridgeOptions
        {
            HostId = "powerbi",
            HostVersion = 2026,
            PipeName = _pipeName,
            ConnectTimeoutMs = 3000,
            PingIntervalSeconds = 60
        });

        _client = new RevitBridgeClient(_options, NullLogger<RevitBridgeClient>.Instance, PowerBiHostProfile.Instance);
        _formatter = new ResultFormatter();
        _context = new ContextService(_client, _formatter);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        await _listener.StopAsync();
        _listener.Dispose();
    }

    private Task<JsonRpcEnvelope?> DispatchCustomAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)
    {
        LastCustomMethod = request.Method;
        LastCustomParams = request.Params;

        if (CustomResponseFactory != null)
        {
            return CustomResponseFactory(id, request);
        }

        var suffix = JsonRpcMethods.Suffix(request.Method ?? string.Empty);
        return suffix switch
        {
            "dax" => Task.FromResult<JsonRpcEnvelope?>(JsonRpcEnvelope.Success(id, "| Col |\n|---|\n| 1 |")),
            "measure.upsert" => Task.FromResult<JsonRpcEnvelope?>(JsonRpcEnvelope.Success(id, new { Success = true, Snapshot = "snap1.json" })),
            "measure.delete" => Task.FromResult<JsonRpcEnvelope?>(JsonRpcEnvelope.Success(id, new { Deleted = true, Snapshot = "snap2.json" })),
            "relationship.manage" => Task.FromResult<JsonRpcEnvelope?>(JsonRpcEnvelope.Success(id, new { Success = true })),
            "schema" => Task.FromResult<JsonRpcEnvelope?>(JsonRpcEnvelope.Success(id, new { Tables = new[] { "Table1" } })),
            _ => Task.FromResult<JsonRpcEnvelope?>(null)
        };
    }

    private static string TextOf(CallToolResult result) => ((TextContentBlock)result.Content[0]).Text;

    #region Task 1: PowerBiEvaluateDaxTool Boundary Clamping & Validation

    [Theory]
    [InlineData(-100, 1)]
    [InlineData(-1, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(500, 500)]
    [InlineData(10000, 10000)]
    [InlineData(10001, 10000)]
    [InlineData(100000, 10000)]
    public async Task EvaluateDax_MaxRows_StrictlyClampedBetween1And10000(int inputMaxRows, int expectedClamped)
    {
        var tool = new PowerBiEvaluateDaxTool(_client, _formatter);
        var result = await tool.EvaluateDaxAsync("EVALUATE DimCustomer", maxRows: inputMaxRows, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.NotNull(LastCustomParams);

        var p = LastCustomParams.Value;
        Assert.Equal(expectedClamped, p.GetProperty("maxRows").GetInt32());
        Assert.Equal(expectedClamped, p.GetProperty("topN").GetInt32());
    }

    [Theory]
    [InlineData("markdown", "markdown")]
    [InlineData("json", "json")]
    [InlineData("MARKDOWN", "MARKDOWN")]
    [InlineData("JSON", "JSON")]
    public async Task EvaluateDax_ValidFormat_PassesThroughToBridge(string inputFormat, string expectedPassed)
    {
        var tool = new PowerBiEvaluateDaxTool(_client, _formatter);
        var result = await tool.EvaluateDaxAsync("EVALUATE DimCustomer", format: inputFormat, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.NotNull(LastCustomParams);
        Assert.Equal(expectedPassed, LastCustomParams.Value.GetProperty("format").GetString());
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("CUSTOM_FORMAT")]
    [InlineData("xml")]
    public async Task EvaluateDax_InvalidFormat_ReturnsCleanToolError(string invalidFormat)
    {
        var tool = new PowerBiEvaluateDaxTool(_client, _formatter);
        var result = await tool.EvaluateDaxAsync("EVALUATE DimCustomer", format: invalidFormat, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains($"Unsupported format '{invalidFormat}'. Supported formats: 'markdown', 'json'.", text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task EvaluateDax_EmptyOrWhitespaceQuery_ReturnsCleanToolError(string? badQuery)
    {
        var tool = new PowerBiEvaluateDaxTool(_client, _formatter);
        var result = await tool.EvaluateDaxAsync(badQuery!, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("DAX query cannot be empty.", text);
    }

    #endregion

    #region Task 2: Measure & Relationship Tools Validation

    [Theory]
    [InlineData("", "Measure1", "SUM(Sales[Amount])")]
    [InlineData("   ", "Measure1", "SUM(Sales[Amount])")]
    [InlineData("FactSales", "", "SUM(Sales[Amount])")]
    [InlineData("FactSales", "   ", "SUM(Sales[Amount])")]
    [InlineData("FactSales", "Measure1", "")]
    [InlineData("FactSales", "Measure1", "   ")]
    public async Task CreateOrUpdateMeasure_EmptyRequiredFields_ReturnsCleanToolError(
        string tableName, string measureName, string expression)
    {
        var tool = new PowerBiCreateOrUpdateMeasureTool(_client, _formatter);
        var result = await tool.CreateOrUpdateMeasureAsync(
            tableName,
            measureName,
            expression,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("cannot be empty.", text);
    }

    [Theory]
    [InlineData("", "Measure1")]
    [InlineData("   ", "Measure1")]
    [InlineData("FactSales", "")]
    [InlineData("FactSales", "   ")]
    public async Task DeleteMeasure_EmptyRequiredFields_ReturnsCleanToolError(string tableName, string measureName)
    {
        var tool = new PowerBiDeleteMeasureTool(_client, _formatter);
        var result = await tool.DeleteMeasureAsync(tableName, measureName, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("cannot be empty.", text);
    }

    [Fact]
    public async Task DeleteMeasure_NonExistentMeasure_BridgeRejectionThrowsMcpException()
    {
        CustomResponseFactory = (id, req) =>
        {
            var p = req.Params;
            var m = p?.TryGetProperty("measureName", out var mProp) == true ? mProp.GetString() : null;
            var t = p?.TryGetProperty("tableName", out var tProp) == true ? tProp.GetString() : null;

            return Task.FromResult<JsonRpcEnvelope?>(
                JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, $"Measure '{m}' was not found in table '{t}'."));
        };

        var tool = new PowerBiDeleteMeasureTool(_client, _formatter);

        var ex = await Assert.ThrowsAsync<McpException>(async () =>
        {
            await tool.DeleteMeasureAsync("FactSales", "NonExistentMeasure", TestContext.Current.CancellationToken);
        });

        Assert.Contains("was not found in table", ex.Message);
        Assert.Contains("-32600", ex.Message);
    }

    [Fact]
    public async Task DeleteMeasure_NonExistentTable_BridgeRejectionThrowsMcpException()
    {
        CustomResponseFactory = (id, req) =>
        {
            return Task.FromResult<JsonRpcEnvelope?>(
                JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, "Table 'NonExistentTable' was not found in the model."));
        };

        var tool = new PowerBiDeleteMeasureTool(_client, _formatter);

        var ex = await Assert.ThrowsAsync<McpException>(async () =>
        {
            await tool.DeleteMeasureAsync("NonExistentTable", "SomeMeasure", TestContext.Current.CancellationToken);
        });

        Assert.Contains("was not found in the model", ex.Message);
        Assert.Contains("-32000", ex.Message);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("delete")]
    [InlineData("set_active")]
    [InlineData("CREATE")]
    [InlineData("Set_Active")]
    public async Task ManageRelationship_AllowedActions_PassThroughToBridge(string action)
    {
        var tool = new PowerBiManageRelationshipTool(_client, _formatter);

        var result = await tool.ManageRelationshipAsync(
            fromTable: "FactSales",
            fromColumn: "CustKey",
            toTable: "DimCustomer",
            toColumn: "CustKey",
            action: action,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.NotNull(LastCustomParams);
        Assert.Equal(action, LastCustomParams.Value.GetProperty("action").GetString());
    }

    [Theory]
    [InlineData("invalid_action")]
    [InlineData("drop")]
    [InlineData("activate")]
    [InlineData("deactivate")]
    public async Task ManageRelationship_UnsupportedActions_ReturnsCleanToolError(string action)
    {
        var tool = new PowerBiManageRelationshipTool(_client, _formatter);

        var result = await tool.ManageRelationshipAsync(
            fromTable: "FactSales",
            fromColumn: "CustKey",
            toTable: "DimCustomer",
            toColumn: "CustKey",
            action: action,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains($"Unknown relationship action '{action}'. Supported actions: 'create', 'delete', 'set_active'.", text);
    }

    [Theory]
    [InlineData("", "CustKey", "DimCustomer", "CustKey", "create")]
    [InlineData("FactSales", "", "DimCustomer", "CustKey", "create")]
    [InlineData("FactSales", "CustKey", "", "CustKey", "create")]
    [InlineData("FactSales", "CustKey", "DimCustomer", "", "create")]
    [InlineData("FactSales", "CustKey", "DimCustomer", "CustKey", "")]
    public async Task ManageRelationship_EmptyRequiredFields_ReturnsCleanToolError(
        string fromTable, string fromColumn, string toTable, string toColumn, string action)
    {
        var tool = new PowerBiManageRelationshipTool(_client, _formatter);

        var result = await tool.ManageRelationshipAsync(
            fromTable: fromTable,
            fromColumn: fromColumn,
            toTable: toTable,
            toColumn: toColumn,
            action: action,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("cannot be empty.", text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task FormatDax_EmptyOrWhitespace_ReturnsCleanToolError(string? badDax)
    {
        var tool = new PowerBiFormatDaxTool(_client, _formatter);
        var result = await tool.FormatDaxAsync(badDax!, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("DAX expression cannot be empty.", text);
    }

    #endregion

    #region Task 3: Prompts & Resources Boundary Conditions

    [Theory]
    [InlineData("", null)]
    [InlineData("   ", "")]
    [InlineData(null, "   ")]
    public async Task DaxOptimizePrompt_EmptyDaxOrContext_DoesNotThrow(string? dax, string? context)
    {
        var messages = PowerBiDaxOptimizePrompt.Optimize(dax!, context);

        Assert.NotNull(messages);
        Assert.Equal(3, messages.Length);

        // System prompt contains persona
        Assert.Contains("expert Power BI", messages[0].Text);

        // User prompt contains template
        Assert.Contains("Please optimize the following DAX expression", messages[1].Text);
        Assert.DoesNotContain("within table ''", messages[1].Text);
    }

    [Fact]
    public async Task DaxOptimizePrompt_WithTableContext_IncludesTableNote()
    {
        var messages = PowerBiDaxOptimizePrompt.Optimize("SUM(Sales[Amount])", "FactSales");
        Assert.Contains("within table 'FactSales'", messages[1].Text);
        Assert.Contains("SUM(Sales[Amount])", messages[1].Text);
    }

    [Fact]
    public void Resources_OnlyPowerBiSchemesRegistered()
    {
        using var host = McpServerHost.CreateBuilder([], PowerBiHostProfile.Instance).Build();
        var resources = host.Services.GetServices<McpServerResource>().ToArray();

        var uris = resources.Select(r => r.ProtocolResourceTemplate.UriTemplate).ToArray();

        Assert.Contains("powerbi://schema", uris);
        Assert.Contains("powerbi://document/info", uris);

        // All host-specific resources must use the powerbi:// scheme
        var hostUris = uris.Where(u => !u.StartsWith("registry://", StringComparison.Ordinal)).ToArray();
        Assert.All(hostUris, uri => Assert.StartsWith("powerbi://", uri, StringComparison.Ordinal));

        // Verify invalid schemes and cross-host contamination are absent
        Assert.DoesNotContain(uris, u => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(uris, u => u.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(uris, u => u.StartsWith("file://", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(uris, u => u.StartsWith("revit://", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(uris, u => u.StartsWith("autocad://", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(uris, u => u.StartsWith("navis://", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(uris, u => u.StartsWith("etabs://", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(uris, u => u.StartsWith("sap2000://", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(uris, u => u.StartsWith("civil3d://", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PowerBiSchemaResource_SendsIncludeFlags_And_SerializesJson()
    {
        var schemaResource = new PowerBiSchemaResource(_client, _context);
        var json = await schemaResource.GetSchemaAsync(TestContext.Current.CancellationToken);

        Assert.Equal("powerbi.schema", LastCustomMethod);
        Assert.NotNull(LastCustomParams);

        var p = LastCustomParams.Value;
        Assert.True(p.GetProperty("includeColumns").GetBoolean());
        Assert.True(p.GetProperty("includeMeasures").GetBoolean());
        Assert.True(p.GetProperty("includeRelationships").GetBoolean());

        Assert.Contains("Table1", json);
    }

    #endregion
}

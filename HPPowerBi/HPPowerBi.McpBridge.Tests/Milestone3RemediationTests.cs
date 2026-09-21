using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using HPPowerBi.McpBridge.Cloud;
using HPPowerBi.McpBridge.Host;
using HPPowerBi.McpBridge.Safety;
using HPPowerBi.McpBridge.Tabular;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using Microsoft.AnalysisServices.Tabular;
using Xunit;

namespace HPPowerBi.McpBridge.Tests;

public sealed class Milestone3RemediationTests : IDisposable
{
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
    private readonly string _tempDir;

    public Milestone3RemediationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HPPowerBi_M3_Remediation_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private static Model CreateInMemoryModel()
    {
        var database = new Database("TestDb");
        var model = new Model();
        database.Model = model;

        var t1 = new Table { Name = "FactSales" };
        t1.Columns.Add(new DataColumn { Name = "CustKey", DataType = DataType.Int64 });
        model.Tables.Add(t1);

        var t2 = new Table { Name = "DimCustomer" };
        t2.Columns.Add(new DataColumn { Name = "CustKey", DataType = DataType.Int64 });
        model.Tables.Add(t2);

        return model;
    }

    private static async Task<JsonRpcEnvelope> CallAsync(NamedPipeClientStream client, long id, string method, object? parameters)
    {
        var line = BridgeJson.Serialize(JsonRpcEnvelope.Request(id, method, parameters)) + "\n";
        var bytes = Utf8NoBom.GetBytes(line);
        await client.WriteAsync(bytes, 0, bytes.Length, TestContext.Current.CancellationToken);
        await client.FlushAsync(TestContext.Current.CancellationToken);

        var reader = new StreamReader(client, Utf8NoBom, false, 65536, leaveOpen: true);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            var text = await reader.ReadLineAsync();
            if (text is null) break;
            var envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(text);
            if (envelope?.Id == id && envelope.Kind == JsonRpcKind.Response) return envelope;
        }

        throw new TimeoutException($"No reply to {method} #{id}");
    }

    #region Finding 1: PbiRelationshipService Unknown Action Fallthrough Prevention

    [Theory]
    [InlineData("invalid_action")]
    [InlineData("remove")]
    [InlineData("drop")]
    [InlineData("update")]
    [InlineData("upsert")]
    public void ManageRelationship_UnknownAction_ThrowsArgumentException(string unsupportedAction)
    {
        var model = CreateInMemoryModel();

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            PbiRelationshipService.ManageRelationship(
                model,
                action: unsupportedAction,
                fromTable: "FactSales",
                fromColumn: "CustKey",
                toTable: "DimCustomer",
                toColumn: "CustKey");
        });

        Assert.Contains($"Unknown relationship action '{unsupportedAction}'. Supported actions: 'create', 'delete', 'set_active'.", ex.Message);
        Assert.Equal("action", ex.ParamName);
        Assert.Empty(model.Relationships);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ManageRelationship_NullOrWhitespaceAction_ThrowsArgumentException(string? badAction)
    {
        var model = CreateInMemoryModel();

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            PbiRelationshipService.ManageRelationship(
                model,
                action: badAction!,
                fromTable: "FactSales",
                fromColumn: "CustKey",
                toTable: "DimCustomer",
                toColumn: "CustKey");
        });

        Assert.Contains("Action cannot be null or whitespace.", ex.Message);
    }

    [Theory]
    [InlineData("", "CustKey", "DimCustomer", "CustKey")]
    [InlineData("FactSales", "", "DimCustomer", "CustKey")]
    [InlineData("FactSales", "CustKey", "", "CustKey")]
    [InlineData("FactSales", "CustKey", "DimCustomer", "")]
    public void ManageRelationship_NullOrWhitespaceTableOrColumn_ThrowsArgumentException(
        string fromTable, string fromColumn, string toTable, string toColumn)
    {
        var model = CreateInMemoryModel();

        Assert.Throws<ArgumentException>(() =>
        {
            PbiRelationshipService.ManageRelationship(
                model,
                action: "create",
                fromTable: fromTable,
                fromColumn: fromColumn,
                toTable: toTable,
                toColumn: toColumn);
        });
    }

    #endregion

    #region Finding 2: PowerBiDispatcher Format Validation

    [Theory]
    [InlineData("xml")]
    [InlineData("csv")]
    [InlineData("invalid")]
    [InlineData("CUSTOM_FORMAT")]
    public async Task PowerBiDispatcher_UnsupportedFormat_ReturnsInvalidRequestError(string unsupportedFormat)
    {
        var ct = TestContext.Current.CancellationToken;
        var uniquePipe = "hppowerbi-test-format-" + Guid.NewGuid().ToString("N")[..8];
        var store = new BridgeSettingsStore("HPPowerBiTest", "McpBridgeTest");
        var settings = store.Load();
        settings.ExecutionEnabled = true;

        using var connectionManager = new PbiConnectionManager();
        var guard = new PbiSafetyGuard { IsExecutionEnabled = true };
        var snapshotManager = new PbiSnapshotManager(_tempDir);
        using var cloudClient = new PowerBiCloudClient();

        var executor = new PowerBiBridgeExecutor(connectionManager, guard, snapshotManager, "2026");
        var dispatcher = new PowerBiDispatcher(executor, cloudClient, settings, "2026");

        using var host = new McpBridgeHost(
            executor,
            settings,
            store,
            "2026",
            uniquePipe,
            "Power BI",
            JsonRpcMethods.PowerBiPrefix,
            PbiSafetyGuard.ExecutionDisabledMessage,
            dispatcher.DispatchCustomAsync);

        host.Start();

        using var client = new NamedPipeClientStream(".", uniquePipe, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000, ct);

        var daxEnvelope = await CallAsync(client, 1, "powerbi.dax", new
        {
            query = "EVALUATE {1}",
            format = unsupportedFormat
        });

        Assert.NotNull(daxEnvelope);
        Assert.Equal(1, daxEnvelope.Id);
        Assert.NotNull(daxEnvelope.Error);
        Assert.Equal(BridgeErrorCode.InvalidRequest, daxEnvelope.Error.Code);
        Assert.Contains($"Unsupported format '{unsupportedFormat}'. Supported formats: 'markdown', 'json'.", daxEnvelope.Error.Message);
    }

    [Theory]
    [InlineData("markdown")]
    [InlineData("json")]
    [InlineData("MARKDOWN")]
    [InlineData("JSON")]
    public async Task PowerBiDispatcher_SupportedFormat_PassesValidation(string supportedFormat)
    {
        var ct = TestContext.Current.CancellationToken;
        var uniquePipe = "hppowerbi-test-valid-fmt-" + Guid.NewGuid().ToString("N")[..8];
        var store = new BridgeSettingsStore("HPPowerBiTest", "McpBridgeTest");
        var settings = store.Load();
        settings.ExecutionEnabled = true;

        using var connectionManager = new PbiConnectionManager();
        var guard = new PbiSafetyGuard { IsExecutionEnabled = true };
        var snapshotManager = new PbiSnapshotManager(_tempDir);
        using var cloudClient = new PowerBiCloudClient();

        var executor = new PowerBiBridgeExecutor(connectionManager, guard, snapshotManager, "2026");
        var dispatcher = new PowerBiDispatcher(executor, cloudClient, settings, "2026");

        using var host = new McpBridgeHost(
            executor,
            settings,
            store,
            "2026",
            uniquePipe,
            "Power BI",
            JsonRpcMethods.PowerBiPrefix,
            PbiSafetyGuard.ExecutionDisabledMessage,
            dispatcher.DispatchCustomAsync);

        host.Start();

        using var client = new NamedPipeClientStream(".", uniquePipe, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000, ct);

        var daxEnvelope = await CallAsync(client, 1, "powerbi.dax", new
        {
            query = "EVALUATE {1}",
            format = supportedFormat
        });

        Assert.NotNull(daxEnvelope);
        Assert.Equal(1, daxEnvelope.Id);
        // Format validation passed, so error is NOT InvalidRequest (-32600).
        // It proceeds to SSAS connection check, returning InternalError ("Not connected to Power BI Desktop.").
        Assert.NotNull(daxEnvelope.Error);
        Assert.Equal(BridgeErrorCode.InternalError, daxEnvelope.Error.Code);
        Assert.Contains("Not connected to Power BI Desktop", daxEnvelope.Error.Message);
    }

    #endregion
}

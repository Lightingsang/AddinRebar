using System.Text.Json;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using static HPRebar.Mcp.Server.Tests.TeklaTestProfile;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Verifies ContextService output shaping across hosts, specifically ensuring that Tekla context results
///     drop Revit-only fields (revitVersion, isFamily) and retain the TeklaInfo block with camelCase properties.
/// </summary>
public sealed class ContextServiceTests
{
    [Fact]
    public async Task ContextService_shaped_json_for_tekla_drops_revit_fields_and_includes_tekla_info()
    {
        var pipe = NewPipe();
        var executor = new FakeRevitExecutor
        {
            ContextHandler = _ => new ContextResult
            {
                RevitVersion = "2025",
                IsFamily = false,
                Host = "tekla",
                HostVersion = "2025",
                DocTitle = "SteelStructure",
                DocPath = @"C:\TeklaStructuresModels\SteelStructure",
                Tekla = new TeklaInfo(
                    IsConnected: true,
                    ModelName: "SteelStructure",
                    ModelPath: @"C:\TeklaStructuresModels\SteelStructure",
                    ProjectName: "WarehouseB",
                    TeklaVersion: "2025.0",
                    HeavyOperationsEnabled: true,
                    PartCount: 120,
                    RebarCount: 450,
                    DrawingCount: 8),
            },
        };

        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, new BridgeSettings(), "2025", "Tekla Structures"));
        listener.Start();

        await using var client = new RevitBridgeClient(
            Options.Create(PipeOptions(pipe)),
            NullLogger<RevitBridgeClient>.Instance, Tekla());

        var service = new ContextService(client, new ResultFormatter());
        var jsonText = await service.ReadAsync(false, TestContext.Current.CancellationToken);

        using var doc = JsonDocument.Parse(jsonText);
        var root = doc.RootElement;

        // Revit-specific fields must be stripped for non-Revit hosts
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));

        // Host metadata
        Assert.Equal("tekla", root.GetProperty("host").GetString());
        Assert.Equal("2025", root.GetProperty("hostVersion").GetString());
        Assert.Equal("SteelStructure", root.GetProperty("docTitle").GetString());

        // TeklaInfo block
        Assert.True(root.TryGetProperty("tekla", out var teklaElem));
        Assert.True(teklaElem.GetProperty("isConnected").GetBoolean());
        Assert.Equal("SteelStructure", teklaElem.GetProperty("modelName").GetString());
        Assert.Equal(@"C:\TeklaStructuresModels\SteelStructure", teklaElem.GetProperty("modelPath").GetString());
        Assert.Equal("WarehouseB", teklaElem.GetProperty("projectName").GetString());
        Assert.Equal("2025.0", teklaElem.GetProperty("teklaVersion").GetString());
        Assert.True(teklaElem.GetProperty("heavyOperationsEnabled").GetBoolean());
        Assert.Equal(120, teklaElem.GetProperty("partCount").GetInt32());
        Assert.Equal(450, teklaElem.GetProperty("rebarCount").GetInt32());
        Assert.Equal(8, teklaElem.GetProperty("drawingCount").GetInt32());

        // Other host blocks must not exist
        Assert.False(root.TryGetProperty("autocad", out _));
        Assert.False(root.TryGetProperty("navis", out _));
        Assert.False(root.TryGetProperty("etabs", out _));
        Assert.False(root.TryGetProperty("civil3d", out _));
        Assert.False(root.TryGetProperty("sap2000", out _));
        Assert.False(root.TryGetProperty("powerbi", out _));
        Assert.False(root.TryGetProperty("excel", out _));
        Assert.False(root.TryGetProperty("robot", out _));

        await listener.StopAsync();
    }
}

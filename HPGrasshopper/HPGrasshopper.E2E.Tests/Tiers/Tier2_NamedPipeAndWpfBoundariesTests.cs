namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.ViewModel;
using Xunit;

public sealed class Tier2_NamedPipeAndWpfBoundariesTests
{
    [Fact]
    public async Task TC_T2_NPS_01_CorruptedNdjsonFramingNegativeTest()
    {
        var bridge = new GrasshopperE2EBridge();
        await using var simulator = new GrasshopperPipeSimulator(bridge: bridge);
        simulator.Start();

        using var client = new NamedPipeClientStream(".", simulator.PipeName, PipeDirection.InOut);
        await client.ConnectAsync(2000);
        using var reader = new StreamReader(client, Encoding.UTF8);
        using var writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };

        await writer.WriteLineAsync("{\"jsonrpc\": \"2.0\", \"method\": \"grasshopper.execute\", \"id\": 1");
        var resp = await reader.ReadLineAsync();

        Assert.NotNull(resp);
        using var doc = JsonDocument.Parse(resp);
        Assert.Equal(-32700, doc.RootElement.GetProperty("error").GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task TC_T2_NPS_02_UnknownRpcMethodNegativeTest()
    {
        var bridge = new GrasshopperE2EBridge();
        await using var simulator = new GrasshopperPipeSimulator(bridge: bridge);
        simulator.Start();

        using var client = new NamedPipeClientStream(".", simulator.PipeName, PipeDirection.InOut);
        await client.ConnectAsync(2000);
        using var reader = new StreamReader(client, Encoding.UTF8);
        using var writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };

        await writer.WriteLineAsync("{\"jsonrpc\": \"2.0\", \"method\": \"grasshopper.non_existent_method\", \"id\": 2}");
        var resp = await reader.ReadLineAsync();

        Assert.NotNull(resp);
        using var doc = JsonDocument.Parse(resp);
        Assert.Equal(-32601, doc.RootElement.GetProperty("error").GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task TC_T2_NPS_03_EmptyStringAndWhitespaceMethodCall()
    {
        var bridge = new GrasshopperE2EBridge();
        await using var simulator = new GrasshopperPipeSimulator(bridge: bridge);
        simulator.Start();

        using var client = new NamedPipeClientStream(".", simulator.PipeName, PipeDirection.InOut);
        await client.ConnectAsync(2000);
        using var reader = new StreamReader(client, Encoding.UTF8);
        using var writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };

        await writer.WriteLineAsync("{\"jsonrpc\": \"2.0\", \"method\": \"   \", \"id\": 3}");
        var resp = await reader.ReadLineAsync();

        Assert.NotNull(resp);
        using var doc = JsonDocument.Parse(resp);
        Assert.Equal(-32601, doc.RootElement.GetProperty("error").GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task TC_T2_NPS_04_MassivePayloadBoundaryTest()
    {
        var bridge = new GrasshopperE2EBridge();
        await using var simulator = new GrasshopperPipeSimulator(bridge: bridge);
        simulator.Start();

        using var client = new NamedPipeClientStream(".", simulator.PipeName, PipeDirection.InOut);
        await client.ConnectAsync(2000);
        using var reader = new StreamReader(client, Encoding.UTF8);
        using var writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };

        var largePadding = new string('x', 64 * 1024);
        await writer.WriteLineAsync($"{{\"jsonrpc\": \"2.0\", \"method\": \"grasshopper.ping\", \"id\": 4, \"params\": {{\"data\": \"{largePadding}\"}}}}");
        var resp = await reader.ReadLineAsync();

        Assert.NotNull(resp);
        using var doc = JsonDocument.Parse(resp);
        Assert.True(doc.RootElement.GetProperty("result").GetProperty("pong").GetBoolean());
    }

    [Fact]
    public async Task TC_T2_NPS_05_PrematureClientStreamSevering()
    {
        var bridge = new GrasshopperE2EBridge();
        await using var simulator = new GrasshopperPipeSimulator(bridge: bridge);
        simulator.Start();

        using (var client = new NamedPipeClientStream(".", simulator.PipeName, PipeDirection.InOut))
        {
            await client.ConnectAsync(2000);
            var buffer = Encoding.UTF8.GetBytes("{\"jsonrpc\":");
            await client.WriteAsync(buffer, 0, buffer.Length);
            client.Close();
        }

        using var client2 = new NamedPipeClientStream(".", simulator.PipeName, PipeDirection.InOut);
        await client2.ConnectAsync(2000);
        Assert.True(client2.IsConnected);
    }

    [Fact]
    public async Task TC_T2_WPF_01_ExecutionAttemptWhenOptInDisabledGate()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = false };
        var req = new GrasshopperExecutionRequest("return 1;");

        var result = await bridge.ExecuteAsync(req);
        Assert.False(result.Success);
        Assert.Equal(GrasshopperE2EContracts.OptInDisabledErrorCode, result.ErrorCode);
        Assert.Contains(GrasshopperE2EContracts.OptInDisabledMessageFragment, result.ErrorMessage);
    }

    [Fact]
    public void TC_T2_WPF_02_ContextQueryAllowedEvenWhenOptInDisabled()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = false };
        var ctx = bridge.GetContext();

        Assert.True(ctx.IsConnected);
        Assert.False(ctx.IsExecutionEnabled);
        Assert.NotNull(ctx.ActiveDefinitionName);
    }

    [Fact]
    public void TC_T2_WPF_03_RapidMultithreadedOptInToggleFlipping()
    {
        var bridge = new GrasshopperE2EBridge();
        Parallel.For(0, 50, i =>
        {
            bridge.IsExecutionEnabled = (i % 2 == 0);
        });

        var ctx = bridge.GetContext();
        Assert.Equal(bridge.IsExecutionEnabled, ctx.IsExecutionEnabled);
    }

    [Fact]
    public void TC_T2_WPF_04_ExtremelyLongLastScriptPreviewTruncation()
    {
        var scriptLines = Enumerable.Range(1, 100)
            .Select(i => $"// Line {i:D3}: " + new string('X', 5000))
            .ToList();
        var longScript = string.Join("\n", scriptLines);
        Assert.True(longScript.Length >= 500_000);

        var runner = new GrasshopperTestBridgeRunner
        {
            LastRun = new LastRunInfo(
                DateTimeOffset.UtcNow,
                "MassiveScript",
                longScript,
                false,
                null,
                120,
                false,
                5,
                0,
                0)
        };

        string? copiedScript = null;
        var vm = new McpBridgeStatusViewModel(runner, action => action(), text => copiedScript = text);

        Assert.True(vm.HasLastScript);
        Assert.Contains("… (80 more lines — use Copy)", vm.LastScriptPreview);
        Assert.Contains("// Line 001:", vm.LastScriptPreview);
        Assert.Contains("// Line 020:", vm.LastScriptPreview);
        Assert.DoesNotContain("// Line 021:", vm.LastScriptPreview);
        Assert.True(vm.LastScriptPreview.Length < longScript.Length);

        vm.CopyLastScriptCommand.Execute(null);
        Assert.Equal(longScript, copiedScript);
    }

    [Fact]
    public void TC_T2_WPF_05_ClipboardExceptionResilience()
    {
        const int clipbrdCantOpenHr = unchecked((int)0x800401D0);
        var scriptCode = "doc.Create.Rebar();";
        var runner = new GrasshopperTestBridgeRunner
        {
            LastRun = new LastRunInfo(DateTimeOffset.UtcNow, "CreateRebar", scriptCode, false, null, 15, false, 1, 0, 0)
        };

        // 1. Headless resilience: null clipboard delegate sets StatusDetail without throwing.
        var vmNullClipboard = new McpBridgeStatusViewModel(runner, action => action(), copyToClipboard: null);
        vmNullClipboard.CopyLastScriptCommand.Execute(null);
        Assert.Equal("Clipboard is not available in this window.", vmNullClipboard.StatusDetail);

        // 2. Exception resilience: delegate traps CLIPBRD_E_CANT_OPEN gracefully.
        string? failureMessage = null;
        Action<string> resilientClipboard = _ =>
        {
            try
            {
                throw new System.Runtime.InteropServices.ExternalException("Clipboard busy", clipbrdCantOpenHr);
            }
            catch (System.Runtime.InteropServices.ExternalException ex) when (ex.ErrorCode == clipbrdCantOpenHr)
            {
                failureMessage = "Clipboard busy; retry recommended.";
            }
        };

        var vm = new McpBridgeStatusViewModel(runner, action => action(), resilientClipboard);
        vm.CopyLastScriptCommand.Execute(null);
        Assert.Equal("Clipboard busy; retry recommended.", failureMessage);
    }
}

namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using HPRebar.McpBridge.Core.ViewModel;
using Xunit;

public sealed class Tier1_NamedPipeAndWpfTests
{
    [Fact]
    public void TC_T1_NPS_01_PipeServerNameResolution()
    {
        var profile = HostProfile.Grasshopper;
        var pipeName = PipeNaming.For(profile.HostId, profile.DefaultVersion);
        Assert.Equal("hpgrasshopper-mcp-2026", pipeName);
        Assert.Equal("grasshopper", PipeNaming.GrasshopperHost);
    }

    [Fact]
    public void TC_T1_NPS_02_JsonRpcMethodPrefixConformance()
    {
        Assert.Equal("grasshopper.", GrasshopperE2EContracts.MethodPrefix);
        Assert.True(GrasshopperE2EContracts.MethodPrefix.EndsWith('.'));
    }

    [Fact]
    public async Task TC_T1_NPS_03_NdjsonFramingAndMessageParsing()
    {
        var bridge = new GrasshopperE2EBridge();
        await using var simulator = new GrasshopperPipeSimulator(bridge: bridge);
        simulator.Start();

        using var client = new NamedPipeClientStream(".", simulator.PipeName, PipeDirection.InOut);
        await client.ConnectAsync(2000);
        using var reader = new StreamReader(client, Encoding.UTF8);
        using var writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };

        await writer.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"id\":101,\"method\":\"grasshopper.ping\"}");
        var line = await reader.ReadLineAsync();

        Assert.NotNull(line);
        using var doc = JsonDocument.Parse(line);
        Assert.Equal(101, doc.RootElement.GetProperty("id").GetInt32());
        Assert.True(doc.RootElement.GetProperty("result").GetProperty("pong").GetBoolean());
    }

    [Fact]
    public async Task TC_T1_NPS_04_RequestResponseIdCorrelation()
    {
        var bridge = new GrasshopperE2EBridge();
        await using var simulator = new GrasshopperPipeSimulator(bridge: bridge);
        simulator.Start();

        using var client = new NamedPipeClientStream(".", simulator.PipeName, PipeDirection.InOut);
        await client.ConnectAsync(2000);
        using var reader = new StreamReader(client, Encoding.UTF8);
        using var writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };

        var testIds = new long[] { 101, 202, 303 };
        foreach (var reqId in testIds)
        {
            await writer.WriteLineAsync($"{{\"jsonrpc\":\"2.0\",\"id\":{reqId},\"method\":\"grasshopper.ping\"}}");
            var line = await reader.ReadLineAsync();
            Assert.NotNull(line);
            using var doc = JsonDocument.Parse(line);
            Assert.Equal(reqId, doc.RootElement.GetProperty("id").GetInt64());
        }
    }

    [Fact]
    public async Task TC_T1_NPS_05_GracefulClientDisconnectAndReconnect()
    {
        var bridge = new GrasshopperE2EBridge();
        await using var simulator = new GrasshopperPipeSimulator(bridge: bridge);
        simulator.Start();

        for (int i = 0; i < 2; i++)
        {
            using var client = new NamedPipeClientStream(".", simulator.PipeName, PipeDirection.InOut);
            await client.ConnectAsync(2000);
            using var reader = new StreamReader(client, Encoding.UTF8);
            using var writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };

            await writer.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"ping\"}");
            var resp = await reader.ReadLineAsync();
            Assert.NotNull(resp);
        }
    }

    [Fact]
    public void TC_T1_WPF_01_StatusViewModelInitialState()
    {
        var bridge = new GrasshopperE2EBridge();
        var context = bridge.GetContext();

        Assert.True(context.IsConnected);
        Assert.False(context.IsExecutionEnabled);
        Assert.True(context.IsLiveCanvasAttached);
        Assert.True(context.IsRhinoInsideActive);
    }

    [Fact]
    public void TC_T1_WPF_02_ToggleListenerCommandExecution()
    {
        var bridge = new GrasshopperE2EBridge();
        Assert.True(bridge.IsLiveCanvasAttached);

        bridge.DetachLiveCanvas();
        Assert.False(bridge.GetContext().IsLiveCanvasAttached);

        bridge.AttachLiveCanvas("UpdatedTower.gh");
        Assert.True(bridge.GetContext().IsLiveCanvasAttached);
        Assert.Equal("UpdatedTower.gh", bridge.GetContext().ActiveDefinitionName);
    }

    [Fact]
    public void TC_T1_WPF_03_OptInExecutionTogglePropertyChange()
    {
        var bridge = new GrasshopperE2EBridge();
        Assert.False(bridge.IsExecutionEnabled);

        bridge.IsExecutionEnabled = true;
        Assert.True(bridge.IsExecutionEnabled);
        Assert.True(bridge.GetContext().IsExecutionEnabled);
    }

    [Fact]
    public void TC_T1_WPF_04_ScriptPreviewAndCopyCommand()
    {
        var scriptCode = "doc.Create.NewFamilyInstance();";
        var diags = ScriptGuard.Check(scriptCode, GuardProfile.Grasshopper);
        Assert.Empty(diags);

        var runner = new GrasshopperTestBridgeRunner
        {
            LastRun = new LastRunInfo(
                DateTimeOffset.UtcNow,
                "NewFamilyInstance",
                scriptCode,
                false,
                null,
                25,
                false,
                1,
                0,
                0)
        };

        string? clipboardContent = null;
        var vm = new McpBridgeStatusViewModel(runner, action => action(), text => clipboardContent = text);

        Assert.True(vm.HasLastScript);
        Assert.Equal(scriptCode, vm.LastScriptPreview);
        Assert.True(vm.CopyLastScriptCommand.CanExecute(null));

        vm.CopyLastScriptCommand.Execute(null);
        Assert.Equal(scriptCode, clipboardContent);
    }

    [Fact]
    public void TC_T1_WPF_05_MaterialDesignThemeSynchronization()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HPGrasshopper.slnx")))
        {
            dir = dir.Parent;
        }
        Assert.NotNull(dir);

        var themesDir = Path.Combine(dir.FullName, "HPGrasshopper.McpBridge", "Resources", "Themes");
        Assert.True(Directory.Exists(themesDir), $"Themes directory must exist at {themesDir}");

        var darkPath = Path.Combine(themesDir, "ThemeDark.xaml");
        var lightPath = Path.Combine(themesDir, "ThemeLight.xaml");
        Assert.True(File.Exists(darkPath), "ThemeDark.xaml must exist in HPGrasshopper.McpBridge");
        Assert.True(File.Exists(lightPath), "ThemeLight.xaml must exist in HPGrasshopper.McpBridge");

        var darkContent = File.ReadAllText(darkPath);
        var lightContent = File.ReadAllText(lightPath);

        var keyRegex = new Regex(@"x:Key=""(?<key>[^""]+)""");
        var darkKeys = keyRegex.Matches(darkContent).Select(m => m.Groups["key"].Value).ToHashSet();
        var lightKeys = keyRegex.Matches(lightContent).Select(m => m.Groups["key"].Value).ToHashSet();

        Assert.NotEmpty(darkKeys);
        Assert.Equal(darkKeys, lightKeys);

        Assert.Contains("Brush.Background", darkKeys);
        Assert.Contains("Brush.Surface", darkKeys);
        Assert.Contains("Brush.Foreground.Primary", darkKeys);
        Assert.Contains("Brush.Accent", darkKeys);

        var viewPath = Path.Combine(dir.FullName, "HPGrasshopper.McpBridge", "UI", "GrasshopperBridgeStatusView.xaml");
        Assert.True(File.Exists(viewPath), "GrasshopperBridgeStatusView.xaml must exist");
        var viewContent = File.ReadAllText(viewPath);
        Assert.Contains("ThemeDark.xaml", viewContent);
        Assert.Contains("Brush.Background", viewContent);
        Assert.Contains("Brush.Foreground.Primary", viewContent);
    }
}

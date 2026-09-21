using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using HPPowerBi.McpBridge;
using HPPowerBi.McpBridge.Cloud;
using HPPowerBi.McpBridge.Host;
using HPPowerBi.McpBridge.Resources.Themes;
using HPPowerBi.McpBridge.Safety;
using HPPowerBi.McpBridge.Tabular;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using MaterialDesignThemes.Wpf;
using Xunit;

namespace HPPowerBi.McpBridge.Tests;

public sealed class Milestone2RemediationTests : IDisposable
{
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
    private readonly string _tempDir;

    public Milestone2RemediationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HPPowerBi_Remediation_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        BridgeEntry.Dispose();
    }

    public void Dispose()
    {
        BridgeEntry.Dispose();
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Ignore cleanup failure in temp dir
        }
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

    [Fact]
    public async Task CustomMethods_RoutedGenuinelyOverNamedPipe_ToPowerBiDispatcher()
    {
        var ct = TestContext.Current.CancellationToken;

        // 1. Arrange a dedicated McpBridgeHost on a unique test pipe with PowerBiDispatcher.DispatchCustomAsync wired
        var uniquePipe = "hppowerbi-remediation-" + Guid.NewGuid().ToString("N")[..8];
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

        // 2. Connect via real NamedPipeClientStream
        using var client = new NamedPipeClientStream(".", uniquePipe, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000, ct);

        // 3. Send custom method: powerbi.format_dax
        var formatEnvelope = await CallAsync(client, 1, "powerbi.format_dax", new { dax = "evaluate filter(tbl, true)" });
        Assert.NotNull(formatEnvelope);
        Assert.Equal(1, formatEnvelope.Id);
        Assert.True(formatEnvelope.Kind == JsonRpcKind.Response);
        Assert.Null(formatEnvelope.Error);

        var formattedText = formatEnvelope.ResultAs<string>();
        Assert.Contains("EVALUATE", formattedText);
        Assert.Contains("FILTER", formattedText);

        // 4. Send custom method: powerbi.dax (without SSAS connection)
        var daxEnvelope = await CallAsync(client, 2, "powerbi.dax", new { query = "EVALUATE {1}" });
        Assert.NotNull(daxEnvelope);
        Assert.Equal(2, daxEnvelope.Id);
        // CRITICAL CHECK: Error must be InternalError ("Not connected to Power BI Desktop."), NOT MethodNotFound (-32601)
        Assert.NotNull(daxEnvelope.Error);
        Assert.NotEqual(BridgeErrorCode.MethodNotFound, daxEnvelope.Error.Code);
        Assert.Equal(BridgeErrorCode.InternalError, daxEnvelope.Error.Code);
        Assert.Contains("Not connected to Power BI Desktop", daxEnvelope.Error.Message);

        // 5. Send custom method: powerbi.schema
        var schemaEnvelope = await CallAsync(client, 3, "powerbi.schema", new { });
        Assert.NotNull(schemaEnvelope);
        Assert.Equal(3, schemaEnvelope.Id);
        Assert.NotNull(schemaEnvelope.Error);
        Assert.NotEqual(BridgeErrorCode.MethodNotFound, schemaEnvelope.Error.Code);
        Assert.Equal(BridgeErrorCode.InternalError, schemaEnvelope.Error.Code);

        // 6. Send standard engine method: powerbi.ping
        var pingEnvelope = await CallAsync(client, 4, "powerbi.ping", null);
        Assert.NotNull(pingEnvelope);
        Assert.Equal(4, pingEnvelope.Id);
        Assert.Null(pingEnvelope.Error);

        var pingResult = pingEnvelope.ResultAs<BridgePingResult>();
        Assert.NotNull(pingResult);
        Assert.True(pingResult.Pong);
        Assert.Equal("2026", pingResult.RevitVersion);

        // 7. Send truly unknown method: powerbi.unknown_arbitrary_method
        var unknownEnvelope = await CallAsync(client, 5, "powerbi.unknown_arbitrary_method", null);
        Assert.NotNull(unknownEnvelope);
        Assert.Equal(5, unknownEnvelope.Id);
        Assert.NotNull(unknownEnvelope.Error);
        Assert.Equal(BridgeErrorCode.MethodNotFound, unknownEnvelope.Error.Code);

        host.Stop();
    }

    [Fact]
    public async Task BridgeEntry_WiresDispatcherIntoHostListener()
    {
        var ct = TestContext.Current.CancellationToken;
        var container = BridgeEntry.Start();
        Assert.NotNull(container);
        Assert.NotNull(container.Host);

        // Ensure host is listening
        container.Host.Start();

        using var client = new NamedPipeClientStream(".", BridgeEntry.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000, ct);

        // Test format_dax over live BridgeEntry named pipe
        var env = await CallAsync(client, 100, "powerbi.format_dax", new { dax = "evaluate customer" });
        Assert.NotNull(env);
        Assert.Equal(100, env.Id);
        Assert.Null(env.Error);
        Assert.Contains("EVALUATE", env.ResultAs<string>());

        BridgeEntry.Dispose();
    }

    [Fact]
    public void MaterialThemeBridge_AppliesTheme_FallingBackToApplicationCurrentResources()
    {
        var thread = new Thread(() =>
        {
            try
            {
                // Ensure Application.Current is initialized with App.xaml dictionaries
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                // Create a window with empty local Resources (simulating StatusWindow where MaterialBridge is in App.xaml)
                var window = new Window();
                Assert.Empty(window.Resources.MergedDictionaries);

                // Apply dark theme
                MaterialThemeBridge.Apply(window, dark: true);

                // Find overlay
                var overlay = window.Resources.MergedDictionaries.FirstOrDefault(d => d.Contains("HPPowerBi.ThemeOverlay"));
                Assert.NotNull(overlay);

                // In the defective version, GetTheme() was never set on overlay when window.Resources lacked the seed.
                // With our fix, overlay has a valid theme derived from Application.Current.Resources!
                var darkTheme = overlay.GetTheme();
                Assert.NotNull(darkTheme);
                Assert.Equal(BaseTheme.Dark, darkTheme.GetBaseTheme());

                // Apply light theme
                MaterialThemeBridge.Apply(window, dark: false);
                var lightOverlay = window.Resources.MergedDictionaries.FirstOrDefault(d => d.Contains("HPPowerBi.ThemeOverlay"));
                Assert.NotNull(lightOverlay);

                var lightTheme = lightOverlay.GetTheme();
                Assert.NotNull(lightTheme);
                Assert.Equal(BaseTheme.Light, lightTheme.GetBaseTheme());
            }
            catch (Exception ex)
            {
                Assert.Fail($"STA Thread Exception: {ex}");
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(5));
    }
}

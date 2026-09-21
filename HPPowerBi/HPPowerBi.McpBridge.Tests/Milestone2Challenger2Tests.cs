using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using HPPowerBi.McpBridge;
using HPPowerBi.McpBridge.Cloud;
using HPPowerBi.McpBridge.Discovery;
using HPPowerBi.McpBridge.Host;
using HPPowerBi.McpBridge.Safety;
using HPPowerBi.McpBridge.Tabular;
using HPPowerBi.McpBridge.ViewModels;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using Xunit;

namespace HPPowerBi.McpBridge.Tests;

/// <summary>
///     Empirical stress tests by challenger_m2_2:
///     1. StatusViewModel: Rapid execution toggle, mutation gating invariants, CanConnect truth table transitions,
///        Disconnect idempotency, and instance selection retention/reset.
///     2. BridgeEntry Lifecycle: Repeated Start()/Dispose() cycles, singleton idempotency, multi-dispose resilience,
///        and PipeNaming resolution contract.
/// </summary>
public sealed class Milestone2Challenger2Tests : IDisposable
{
    private readonly string _tempDir;
    private readonly PbiConnectionManager _connectionManager;
    private readonly PbiSafetyGuard _guard;
    private readonly PbiSnapshotManager _snapshotManager;
    private readonly StatusViewModel _viewModel;

    public Milestone2Challenger2Tests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HPPowerBi_Challenger2_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        BridgeEntry.Dispose();

        _connectionManager = new PbiConnectionManager();
        _guard = new PbiSafetyGuard();
        _snapshotManager = new PbiSnapshotManager(_tempDir);

        _viewModel = new StatusViewModel(
            _connectionManager,
            _guard,
            _snapshotManager,
            host: null,
            cloudClient: null,
            logDirectory: _tempDir,
            onUiThread: action => action());
    }

    public void Dispose()
    {
        BridgeEntry.Dispose();
        _connectionManager.Dispose();
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }

    // =========================================================================
    // Category 1: StatusViewModel Safety Synchronization & Rapid Toggling
    // =========================================================================

    [Fact]
    public void StatusViewModel_RapidExecutionToggling_MaintainsSafetyInvariants()
    {
        // Stress test 500 toggles back and forth
        for (var i = 0; i < 500; i++)
        {
            var targetState = i % 2 == 1;
            _viewModel.IsExecutionEnabled = targetState;

            Assert.Equal(targetState, _viewModel.IsExecutionEnabled);
            Assert.Equal(targetState, _guard.IsExecutionEnabled);
            Assert.Equal(targetState, _viewModel.CanEnableMutation);

            if (!targetState)
            {
                Assert.False(_viewModel.IsMutationEnabled);
                Assert.False(_guard.IsMutationEnabled);
            }
        }

        // Final sanity check: disable execution
        _viewModel.IsExecutionEnabled = false;
        Assert.False(_viewModel.IsExecutionEnabled);
        Assert.False(_guard.IsExecutionEnabled);
        Assert.False(_viewModel.CanEnableMutation);
        Assert.False(_viewModel.IsMutationEnabled);
        Assert.False(_guard.IsMutationEnabled);
    }

    [Fact]
    public void StatusViewModel_MutationAutoDisabledOnExecutionOff_RequiresExplicitReEnable()
    {
        // 1. Enable execution then enable mutation
        _viewModel.IsExecutionEnabled = true;
        _viewModel.IsMutationEnabled = true;

        Assert.True(_viewModel.IsExecutionEnabled);
        Assert.True(_guard.IsExecutionEnabled);
        Assert.True(_viewModel.IsMutationEnabled);
        Assert.True(_guard.IsMutationEnabled);
        Assert.True(_viewModel.CanEnableMutation);

        // 2. Disabling execution MUST automatically disable mutation
        _viewModel.IsExecutionEnabled = false;

        Assert.False(_viewModel.IsExecutionEnabled);
        Assert.False(_guard.IsExecutionEnabled);
        Assert.False(_viewModel.IsMutationEnabled);
        Assert.False(_guard.IsMutationEnabled);
        Assert.False(_viewModel.CanEnableMutation);

        // 3. Re-enabling execution MUST NOT automatically re-enable mutation (no implicit write consent)
        _viewModel.IsExecutionEnabled = true;

        Assert.True(_viewModel.IsExecutionEnabled);
        Assert.True(_guard.IsExecutionEnabled);
        Assert.True(_viewModel.CanEnableMutation);
        Assert.False(_viewModel.IsMutationEnabled);
        Assert.False(_guard.IsMutationEnabled);
    }

    [Fact]
    public void StatusViewModel_AttemptSetMutationWhenExecutionOff_StrictlyRejected()
    {
        Assert.False(_viewModel.IsExecutionEnabled);

        // Direct ViewModel attempt
        _viewModel.IsMutationEnabled = true;
        Assert.False(_viewModel.IsMutationEnabled);
        Assert.False(_guard.IsMutationEnabled);

        // Attempt via Guard directly
        _guard.IsMutationEnabled = true;
        Assert.False(_guard.IsMutationEnabled);
        Assert.False(_viewModel.IsMutationEnabled);
    }

    [Fact]
    public void StatusViewModel_ExternalGuardStateChanges_SynchronizeToViewModel()
    {
        // Modify guard directly: enable execution
        _guard.IsExecutionEnabled = true;
        Assert.True(_viewModel.IsExecutionEnabled);
        Assert.True(_viewModel.CanEnableMutation);

        // Modify guard directly: enable mutation
        _guard.IsMutationEnabled = true;
        Assert.True(_viewModel.IsMutationEnabled);

        // Modify guard directly: disable execution -> VM must disable execution & mutation
        _guard.IsExecutionEnabled = false;
        Assert.False(_viewModel.IsExecutionEnabled);
        Assert.False(_viewModel.IsMutationEnabled);
        Assert.False(_viewModel.CanEnableMutation);
    }

    [Fact]
    public void StatusViewModel_WithMcpBridgeHost_ExecutionSyncAndListenerToggle()
    {
        var store = new BridgeSettingsStore("HPPowerBiTest", "McpBridgeTest");
        var settings = store.Load();
        var executor = new PowerBiBridgeExecutor(_connectionManager, _guard, _snapshotManager, "2026");
        var testPipe = "hppowerbi-mcp-test-vm-" + Guid.NewGuid().ToString("N")[..8];
        var host = new McpBridgeHost(
            executor, settings, store, "2026", testPipe, "Power BI",
            JsonRpcMethods.PowerBiPrefix, PbiSafetyGuard.ExecutionDisabledMessage);

        try
        {
            var vm = new StatusViewModel(
                _connectionManager,
                _guard,
                _snapshotManager,
                host: host,
                cloudClient: null,
                logDirectory: _tempDir,
                onUiThread: action => action());

            // Initially stopped
            Assert.False(vm.IsListening);
            Assert.Contains("stopped", vm.PipeStatusText);

            // Toggle listener on
            vm.ToggleListener();
            Assert.True(vm.IsListening);
            Assert.Contains("Listening", vm.PipeStatusText);

            // Toggle execution through ViewModel -> Host receives it
            vm.IsExecutionEnabled = true;
            Assert.True(host.ExecutionEnabled);
            Assert.True(_guard.IsExecutionEnabled);

            vm.IsExecutionEnabled = false;
            Assert.False(host.ExecutionEnabled);
            Assert.False(_guard.IsExecutionEnabled);

            // Toggle listener off
            vm.ToggleListener();
            // Note: Stop() triggers async task, wait slightly for StateChanged
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (vm.IsListening && sw.ElapsedMilliseconds < 2000)
            {
                System.Threading.Thread.Sleep(20);
            }
            Assert.False(vm.IsListening);
        }
        finally
        {
            host.Dispose();
            executor.Dispose();
        }
    }

    // =========================================================================
    // Category 2: Connection State Transitions, Idempotency & Selection
    // =========================================================================

    [Fact]
    public void StatusViewModel_CanConnectStateTransitions_ExhaustiveMatrix()
    {
        var dummyInstance = new PbiInstanceInfo(
            ProcessId: 4321,
            WindowTitle: "Finance - Power BI Desktop",
            ReportName: "Finance",
            Port: 54321);

        // State 1: SelectedInstance == null, IsConnected == false
        _viewModel.SelectedInstance = null;
        Assert.False(_viewModel.IsConnected);
        Assert.False(_viewModel.ConnectCommand.CanExecute(null));
        Assert.False(_viewModel.DisconnectCommand.CanExecute(null));

        // State 2: SelectedInstance != null, IsConnected == false
        _viewModel.SelectedInstance = dummyInstance;
        Assert.False(_viewModel.IsConnected);
        Assert.True(_viewModel.ConnectCommand.CanExecute(null));
        Assert.False(_viewModel.DisconnectCommand.CanExecute(null));

        // State 3: Clear selection back to null -> CanConnect becomes false again
        _viewModel.SelectedInstance = null;
        Assert.False(_viewModel.ConnectCommand.CanExecute(null));
        Assert.False(_viewModel.DisconnectCommand.CanExecute(null));

        // State 4: Select again -> CanConnect becomes true
        _viewModel.SelectedInstance = dummyInstance;
        Assert.True(_viewModel.ConnectCommand.CanExecute(null));
    }

    [Fact]
    public void StatusViewModel_Disconnect_IdempotentUnderRepeatedCalls()
    {
        // Ensure starting disconnected
        Assert.False(_viewModel.IsConnected);
        Assert.False(_viewModel.DisconnectCommand.CanExecute(null));

        // Call Disconnect 10 times consecutively
        for (var i = 0; i < 10; i++)
        {
            _viewModel.Disconnect();

            Assert.False(_viewModel.IsConnected);
            Assert.Null(_viewModel.ActiveReportName);
            Assert.Null(_viewModel.AttachedPid);
            Assert.Equal(0, _viewModel.LocalPort);
            Assert.Null(_viewModel.DatabaseName);
            Assert.Equal(0, _viewModel.TableCount);
            Assert.Equal(0, _viewModel.MeasureCount);
            Assert.Equal(0, _viewModel.RelationshipCount);
            Assert.Equal("Disconnected", _viewModel.ConnectionStatusText);
            Assert.Equal("Disconnected from Power BI Desktop.", _viewModel.StatusMessage);
            Assert.False(_viewModel.DisconnectCommand.CanExecute(null));
        }
    }

    [Fact]
    public async Task StatusViewModel_ConnectAsync_FailedConnection_GracefullyHandled()
    {
        var unreachableInstance = new PbiInstanceInfo(
            ProcessId: 99999,
            WindowTitle: "Unreachable - Power BI Desktop",
            ReportName: "Unreachable",
            Port: 59999);

        _viewModel.SelectedInstance = unreachableInstance;
        Assert.True(_viewModel.ConnectCommand.CanExecute(null));

        // Attempt connect against closed port 59999
        await _viewModel.ConnectAsync();

        Assert.False(_viewModel.IsConnected);
        Assert.NotNull(_viewModel.ConnectionStatusText);
        Assert.Contains("failed", _viewModel.ConnectionStatusText, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(_viewModel.StatusMessage);
        Assert.Contains("Failed to connect", _viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, _viewModel.LocalPort);
    }

    [Fact]
    public void StatusViewModel_InstanceSelection_RetainsOrResetsGracefully()
    {
        var instA = new PbiInstanceInfo(1001, "Sales - Power BI", "Sales", 50001);
        var instB = new PbiInstanceInfo(1002, "Finance - Power BI", "Finance", 50002);
        var instC = new PbiInstanceInfo(1003, "Inventory - Power BI", "Inventory", 50003);

        _viewModel.AvailableInstances.Clear();
        _viewModel.AvailableInstances.Add(instA);
        _viewModel.AvailableInstances.Add(instB);
        _viewModel.AvailableInstances.Add(instC);

        // Select instB
        _viewModel.SelectedInstance = instB;
        Assert.Same(instB, _viewModel.SelectedInstance);
        Assert.True(_viewModel.ConnectCommand.CanExecute(null));

        // When AvailableInstances is cleared and set to empty
        _viewModel.AvailableInstances.Clear();
        _viewModel.SelectedInstance = _viewModel.AvailableInstances.Count > 0
            ? _viewModel.AvailableInstances[0]
            : null;

        Assert.Null(_viewModel.SelectedInstance);
        Assert.False(_viewModel.ConnectCommand.CanExecute(null));
    }

    [Fact]
    public void StatusViewModel_RefreshInstances_WithZeroAndMultipleInstances_MaintainsState()
    {
        // When RefreshInstances is called on this system (where typically 0 PBIDesktop instances are running)
        _viewModel.RefreshInstances();

        Assert.NotNull(_viewModel.AvailableInstances);
        Assert.NotNull(_viewModel.StatusMessage);

        if (_viewModel.AvailableInstances.Count == 0)
        {
            Assert.Contains("No running Power BI Desktop instances found", _viewModel.ConnectionStatusText);
            Assert.Null(_viewModel.SelectedInstance);
            Assert.False(_viewModel.ConnectCommand.CanExecute(null));
        }
        else
        {
            Assert.NotNull(_viewModel.SelectedInstance);
            Assert.True(_viewModel.ConnectCommand.CanExecute(null));
        }
    }

    [Fact]
    public void StatusViewModel_RestoreSnapshot_CorruptedFile_CatchesGracefully()
    {
        // Create an invalid corrupted snapshot file
        var corruptFile = Path.Combine(_tempDir, "Snapshot_Corrupted.json");
        File.WriteAllText(corruptFile, "{ this is not valid JSON }");

        _viewModel.RestoreSnapshot();

        Assert.NotNull(_viewModel.StatusMessage);
        Assert.Contains("Snapshot restore failed", _viewModel.StatusMessage);
    }

    // =========================================================================
    // Category 3: BridgeEntry Lifecycle & Named Pipe Host Infrastructure
    // =========================================================================

    [Fact]
    public void BridgeEntry_StartAndDispose_RepeatedCycles_CleanTeardownAndRestart()
    {
        for (var cycle = 1; cycle <= 5; cycle++)
        {
            var container = BridgeEntry.Start();

            Assert.NotNull(container);
            Assert.Same(container, BridgeEntry.Current);
            Assert.NotNull(container.Host);
            Assert.Equal("hppowerbi-mcp-2026", container.Host.PipeName);
            Assert.Equal("Power BI", container.Host.HostName);
            Assert.NotNull(container.ConnectionManager);
            Assert.NotNull(container.Guard);
            Assert.NotNull(container.SnapshotManager);
            Assert.NotNull(container.CloudClient);
            Assert.NotNull(container.Dispatcher);

            BridgeEntry.Dispose();
            Assert.Null(BridgeEntry.Current);
        }
    }

    [Fact]
    public void BridgeEntry_MultipleStartCallsWithoutDispose_IdempotentSingleton()
    {
        var container1 = BridgeEntry.Start();
        var container2 = BridgeEntry.Start();

        Assert.NotNull(container1);
        Assert.Same(container1, container2);
        Assert.Same(container1, BridgeEntry.Current);

        BridgeEntry.Dispose();
        Assert.Null(BridgeEntry.Current);
    }

    [Fact]
    public void BridgeEntry_MultipleDisposeCalls_Idempotent()
    {
        BridgeEntry.Start();
        Assert.NotNull(BridgeEntry.Current);

        BridgeEntry.Dispose();
        Assert.Null(BridgeEntry.Current);

        // Subsequent Dispose calls must not throw NullReferenceException or ObjectDisposedException
        BridgeEntry.Dispose();
        BridgeEntry.Dispose();
        Assert.Null(BridgeEntry.Current);
    }

    [Theory]
    [InlineData("powerbi", 2026, "hppowerbi-mcp-2026")]
    [InlineData("PowerBi", 2026, "hppowerbi-mcp-2026")]
    [InlineData("POWERBI", 2026, "hppowerbi-mcp-2026")]
    [InlineData("  powerbi  ", 2026, "hppowerbi-mcp-2026")]
    [InlineData("powerbi", 2025, "hppowerbi-mcp-2025")]
    [InlineData("powerbi", 2027, "hppowerbi-mcp-2027")]
    public void BridgeEntry_PipeNamingConvention_MatchesExpectedContract(string host, int version, string expectedPipe)
    {
        var resolvedPipe = PipeNaming.For(host, version);
        Assert.Equal(expectedPipe, resolvedPipe);
    }

    [Fact]
    public void BridgeEntry_ContractConstants_ExposeExactArchitectureValues()
    {
        Assert.Equal("powerbi", PipeNaming.PowerBiHost);
        Assert.Equal("hppowerbi-mcp-2026", PipeNaming.For(PipeNaming.PowerBiHost, 2026));
        Assert.Equal("hppowerbi-mcp-2026", BridgeEntry.PipeName);
        Assert.Equal("2026", BridgeEntry.HostVersion);
        Assert.Equal(2026, BridgeEntry.HostVersionNumber);
        Assert.Equal("Power BI", BridgeEntry.HostName);
        Assert.Equal("HPPowerBi", BridgeEntry.VendorFolder);
        Assert.Equal("McpBridge", BridgeEntry.ProductFolder);
    }

    [Fact]
    public void BridgeEntry_ContainerExposesAllSubsystems()
    {
        var container = BridgeEntry.Start();

        Assert.NotNull(container.Host);
        Assert.NotNull(container.Executor);
        Assert.NotNull(container.ConnectionManager);
        Assert.NotNull(container.Guard);
        Assert.NotNull(container.SnapshotManager);
        Assert.NotNull(container.CloudClient);
        Assert.NotNull(container.Dispatcher);

        Assert.Equal("hppowerbi-mcp-2026", container.Host.PipeName);
        Assert.Equal("Power BI", container.Host.HostName);
        Assert.Equal("Power BI", PowerBiBridgeExecutor.HostName);
        Assert.Equal("2026", BridgeEntry.HostVersion);

        BridgeEntry.Dispose();
    }
}

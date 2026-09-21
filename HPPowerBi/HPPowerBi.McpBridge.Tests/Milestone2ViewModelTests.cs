using System;
using System.IO;
using HPPowerBi.McpBridge.Cloud;
using HPPowerBi.McpBridge.Discovery;
using HPPowerBi.McpBridge.Safety;
using HPPowerBi.McpBridge.Tabular;
using HPPowerBi.McpBridge.ViewModels;
using Xunit;

namespace HPPowerBi.McpBridge.Tests;

public sealed class Milestone2ViewModelTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PbiConnectionManager _connectionManager;
    private readonly PbiSafetyGuard _guard;
    private readonly PbiSnapshotManager _snapshotManager;
    private readonly StatusViewModel _viewModel;

    public Milestone2ViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HPPowerBi_VmTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

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

    [Fact]
    public void InitialState_IsDisconnectedAndSafetyOff()
    {
        Assert.False(_viewModel.IsConnected);
        Assert.Contains("Disconnected", _viewModel.ConnectionStatusText);
        Assert.False(_viewModel.IsExecutionEnabled);
        Assert.False(_viewModel.IsMutationEnabled);
        Assert.False(_viewModel.CanEnableMutation);
        Assert.Equal(0, _viewModel.LocalPort);
        Assert.Equal(0, _viewModel.TableCount);
        Assert.Equal(0, _viewModel.MeasureCount);
        Assert.Equal(0, _viewModel.RelationshipCount);
        Assert.Null(_viewModel.ActiveReportName);
        Assert.Null(_viewModel.AttachedPid);
        Assert.Null(_viewModel.DatabaseName);
    }

    [Fact]
    public void ExecutionToggle_ControlsMutationAndGuard()
    {
        // 1. Enable execution -> CanEnableMutation becomes true
        _viewModel.IsExecutionEnabled = true;
        Assert.True(_guard.IsExecutionEnabled);
        Assert.True(_viewModel.CanEnableMutation);

        // 2. Enable mutation -> guard mutation becomes true
        _viewModel.IsMutationEnabled = true;
        Assert.True(_guard.IsMutationEnabled);
        Assert.True(_viewModel.IsMutationEnabled);

        // 3. Disable execution -> mutation automatically disabled
        _viewModel.IsExecutionEnabled = false;
        Assert.False(_guard.IsExecutionEnabled);
        Assert.False(_guard.IsMutationEnabled);
        Assert.False(_viewModel.IsMutationEnabled);
        Assert.False(_viewModel.CanEnableMutation);
    }

    [Fact]
    public void MutationCannotBeEnabledWithoutExecution()
    {
        Assert.False(_viewModel.IsExecutionEnabled);

        _viewModel.IsMutationEnabled = true;

        Assert.False(_viewModel.IsMutationEnabled);
        Assert.False(_guard.IsMutationEnabled);
    }

    [Fact]
    public void ConnectCommand_CanExecute_FollowsSelectionAndConnection()
    {
        // 1. No instance selected -> CanExecute is false
        _viewModel.SelectedInstance = null;
        Assert.False(_viewModel.ConnectCommand.CanExecute(null));
        Assert.False(_viewModel.DisconnectCommand.CanExecute(null));

        // 2. Instance selected -> CanExecute is true
        var dummyInstance = new PbiInstanceInfo(
            ProcessId: 1234,
            WindowTitle: "Sales - Power BI Desktop",
            ReportName: "Sales",
            Port: 54321);

        _viewModel.SelectedInstance = dummyInstance;
        Assert.True(_viewModel.ConnectCommand.CanExecute(null));
        Assert.False(_viewModel.DisconnectCommand.CanExecute(null));
    }

    [Fact]
    public void Disconnect_ResetsModelStatistics()
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
    }

    [Fact]
    public void RefreshInstances_ExecutesWithoutException()
    {
        _viewModel.RefreshInstances();

        Assert.NotNull(_viewModel.AvailableInstances);
        Assert.NotNull(_viewModel.StatusMessage);
    }

    [Fact]
    public void RegisterExternalTool_ExecutesAndSetsStatusMessage()
    {
        _viewModel.RegisterExternalTool();

        Assert.NotNull(_viewModel.StatusMessage);
        Assert.NotEmpty(_viewModel.StatusMessage);
    }

    [Fact]
    public void RestoreSnapshot_EmptyDirectory_SetsInformativeStatusMessage()
    {
        _viewModel.RestoreSnapshot();

        Assert.NotNull(_viewModel.StatusMessage);
        Assert.Contains("No snapshot files found", _viewModel.StatusMessage);
    }

    [Fact]
    public void CloudStatus_TextIsPopulated()
    {
        Assert.NotNull(_viewModel.CloudStatusText);
        Assert.NotEmpty(_viewModel.CloudStatusText);
    }

    public void Dispose()
    {
        _connectionManager.Dispose();
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }
}

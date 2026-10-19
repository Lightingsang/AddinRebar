namespace HPDynamo.McpBridge.Tests.ViewModel;

using System;
using HPDynamo.McpBridge.Resources.Themes;
using HPDynamo.McpBridge.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.ViewModel;
using Xunit;

public sealed class ViewModelBindingTests
{
    private static McpBridgeStatusViewModel CreateViewModel(
        FakeMcpBridgeRunner runner,
        Action<string>? clipboard = null)
    {
        return new McpBridgeStatusViewModel(runner, action => action(), clipboard);
    }

    [Fact]
    public void Constructor_SyncsInitialStateWithRunner()
    {
        var runner = new FakeMcpBridgeRunner
        {
            PipeName = "hpdynamo-mcp-2026",
            RevitVersion = "2026",
            ExecutionEnabled = false,
            Status = BridgeStatus.Stopped
        };

        var vm = CreateViewModel(runner);

        Assert.False(vm.IsListening);
        Assert.False(vm.IsExecutionEnabled);
        Assert.Equal("hpdynamo-mcp-2026", vm.PipeName);
        Assert.Equal("2026", vm.RevitVersion);
        Assert.Equal("Start listener", vm.ToggleButtonText);
    }

    [Fact]
    public void ToggleExecutionEnabled_PropagatesToRunner()
    {
        var runner = new FakeMcpBridgeRunner { ExecutionEnabled = false };
        var vm = CreateViewModel(runner);

        vm.IsExecutionEnabled = true;

        Assert.True(runner.ExecutionEnabled);
    }

    [Fact]
    public void ToggleListenerCommand_WhenStopped_StartsRunner()
    {
        var runner = new FakeMcpBridgeRunner { Status = BridgeStatus.Stopped };
        var vm = CreateViewModel(runner);

        vm.ToggleListenerCommand.Execute(null);

        Assert.Equal(1, runner.StartCalls);
        Assert.True(vm.IsListening);
        Assert.Equal("Stop listener", vm.ToggleButtonText);
    }

    [Fact]
    public void ToggleListenerCommand_WhenListening_StopsRunner()
    {
        var runner = new FakeMcpBridgeRunner { Status = BridgeStatus.Listening };
        var vm = CreateViewModel(runner);

        vm.ToggleListenerCommand.Execute(null);

        Assert.Equal(1, runner.StopCalls);
        Assert.False(vm.IsListening);
        Assert.Equal("Start listener", vm.ToggleButtonText);
    }

    [Fact]
    public void RestartBridgeCommand_WhenBusy_CanExecuteIsFalse()
    {
        var runner = new FakeMcpBridgeRunner { Status = BridgeStatus.Busy };
        var vm = CreateViewModel(runner);

        Assert.True(vm.IsBusy);
        Assert.False(vm.RestartBridgeCommand.CanExecute(null));
    }

    [Fact]
    public void CompiledScriptCount_ExceedsThreshold_FlipsIsCompileCountHigh()
    {
        var runner = new FakeMcpBridgeRunner { CompiledScriptCount = 501 };
        var vm = CreateViewModel(runner);

        Assert.Equal(501, vm.CompiledScriptCount);
        Assert.True(vm.IsCompileCountHigh);
    }

    [Fact]
    public void ThemeSync_DynamoHostTheme_TriggersChangedEvent()
    {
        var theme = DynamoHostTheme.Instance;
        var eventFired = false;
        theme.Changed += () => eventFired = true;

        theme.NotifyChanged();

        Assert.True(eventFired);
    }

    [Fact]
    public void LastRunSummary_FormatsSuccessAndFailureOutcomes()
    {
        var runner = new FakeMcpBridgeRunner
        {
            LastRun = new LastRunInfo(
                Timestamp: DateTime.Now,
                Label: "CreateNodes",
                DurationMs: 45,
                Added: 3,
                Modified: 1,
                Deleted: 0,
                RolledBack: true,
                IsError: false,
                Message: null,
                Source: "return 1;")
        };

        var vm = CreateViewModel(runner);

        Assert.True(vm.HasLastScript);
        Assert.Contains("CreateNodes", vm.LastRunSummary);
        Assert.Contains("+3 ~1 -0", vm.LastRunSummary);
        Assert.Contains("rolled back", vm.LastRunSummary);
    }

    [Fact]
    public void CopyLastScriptCommand_CopiesSourceToClipboard()
    {
        string? copied = null;
        var runner = new FakeMcpBridgeRunner
        {
            LastRun = new LastRunInfo(
                Timestamp: DateTime.Now,
                Label: "Test",
                DurationMs: 10,
                Added: 0,
                Modified: 0,
                Deleted: 0,
                RolledBack: false,
                IsError: false,
                Message: null,
                Source: "var x = 123; return x;")
        };

        var vm = CreateViewModel(runner, text => copied = text);

        vm.CopyLastScriptCommand.Execute(null);

        Assert.Equal("var x = 123; return x;", copied);
    }
}

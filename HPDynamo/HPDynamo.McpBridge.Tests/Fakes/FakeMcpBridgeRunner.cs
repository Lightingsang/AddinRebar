namespace HPDynamo.McpBridge.Tests.Fakes;

using System;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.ViewModel;

public sealed class FakeMcpBridgeRunner : IMcpBridgeRunner
{
    public string PipeName { get; set; } = "hpdynamo-mcp-2026";
    public string RevitVersion { get; set; } = "2026";
    public string HostName { get; set; } = "Dynamo";
    public string AuditDirectory { get; set; } = @"C:\Temp\Audit";
    public BridgeStatus Status { get; set; } = BridgeStatus.Stopped;
    public string? StatusMessage { get; set; }
    public bool HasClient { get; set; }
    public bool ExecutionEnabled { get; set; }
    public bool AutoStartListener { get; set; }
    public LastRunInfo? LastRun { get; set; }
    public int CompiledScriptCount { get; set; }

    public int StartCalls { get; private set; }
    public int StopCalls { get; private set; }
    public int RestartCalls { get; private set; }

    public event Action? StateChanged;

    public void RaiseStateChanged() => StateChanged?.Invoke();

    public void Start()
    {
        StartCalls++;
        Status = BridgeStatus.Listening;
        RaiseStateChanged();
    }

    public void Stop()
    {
        StopCalls++;
        Status = BridgeStatus.Stopped;
        RaiseStateChanged();
    }

    public void Restart()
    {
        RestartCalls++;
        Status = BridgeStatus.Listening;
        RaiseStateChanged();
    }
}

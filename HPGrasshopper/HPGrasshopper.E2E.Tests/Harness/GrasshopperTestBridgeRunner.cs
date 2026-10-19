namespace HPGrasshopper.E2E.Tests.Harness;

using System;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.ViewModel;

/// <summary>
///     Lightweight test runner implementing IMcpBridgeRunner for WPF and StatusViewModel testing.
/// </summary>
public sealed class GrasshopperTestBridgeRunner : IMcpBridgeRunner
{
    public string PipeName { get; set; } = "hpgrasshopper-mcp-2026";
    public string RevitVersion { get; set; } = "2026";
    public string HostName { get; set; } = "Grasshopper";
    public string AuditDirectory { get; set; } = @"C:\Temp\GrasshopperAudit";
    public BridgeStatus Status { get; set; } = BridgeStatus.Listening;
    public string? StatusMessage { get; set; }
    public bool HasClient { get; set; } = true;
    public bool ExecutionEnabled { get; set; } = true;
    public bool AutoStartListener { get; set; } = true;
    public LastRunInfo? LastRun { get; set; }
    public int CompiledScriptCount { get; set; }

    public event Action? StateChanged;

    public void RaiseStateChanged() => StateChanged?.Invoke();

    public void Start()
    {
        Status = BridgeStatus.Listening;
        RaiseStateChanged();
    }

    public void Stop()
    {
        Status = BridgeStatus.Stopped;
        RaiseStateChanged();
    }

    public void Restart()
    {
        Status = BridgeStatus.Listening;
        RaiseStateChanged();
    }
}

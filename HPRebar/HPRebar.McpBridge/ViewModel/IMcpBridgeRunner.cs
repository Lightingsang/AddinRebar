using HPRebar.McpBridge.Core.Model;

namespace HPRebar.McpBridge.ViewModel;

/// <summary>
///     Everything the status window may read or toggle. No Revit types cross this line, so the view model
///     can be exercised without Revit and the host can change how it talks to Revit without touching the UI.
/// </summary>
public interface IMcpBridgeRunner
{
    string PipeName { get; }

    string RevitVersion { get; }

    string AuditDirectory { get; }

    BridgeStatus Status { get; }

    /// <summary>Human-readable detail for the current status; the reason when <see cref="BridgeStatus.Error"/>.</summary>
    string? StatusMessage { get; }

    bool HasClient { get; }

    /// <summary>The per-session opt-in for execute_revit_code. Never persisted.</summary>
    bool ExecutionEnabled { get; set; }

    bool AutoStartListener { get; set; }

    LastRunInfo? LastRun { get; }

    int CompiledScriptCount { get; }

    /// <summary>Any property above may have changed. Raised on an arbitrary thread; marshal before touching UI.</summary>
    event Action? StateChanged;

    void Start();

    void Stop();

    void Restart();
}

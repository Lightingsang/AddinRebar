using HPRebar.McpBridge.Core.Model;

namespace HPRebar.McpBridge.Core.ViewModel;

/// <summary>
///     Everything the status window may read or toggle. No host API types cross this line, so the view model
///     can be exercised without the host and each bridge can change how it talks to its host without touching the UI.
/// </summary>
public interface IMcpBridgeRunner
{
    string PipeName { get; }

    /// <summary>Host major version; historical name kept for the Revit XAML binding.</summary>
    string RevitVersion { get; }

    /// <summary>Display name of the host application: "Revit", "AutoCAD".</summary>
    string HostName { get; }

    string AuditDirectory { get; }

    BridgeStatus Status { get; }

    /// <summary>Human-readable detail for the current status; the reason when <see cref="BridgeStatus.Error"/>.</summary>
    string? StatusMessage { get; }

    bool HasClient { get; }

    /// <summary>The per-session opt-in for AI code execution. Never persisted.</summary>
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

namespace HPRebar.McpBridge.Core.Model;

/// <summary>
///     Everything the user or the executor can tune. <see cref="ExecutionEnabled"/> is deliberately
///     not persisted: allowing AI code to run is a per-session decision the user makes each time Revit starts.
/// </summary>
public sealed class BridgeSettings
{
    /// <summary>Master switch for execute_revit_code. On by default via BridgeSettingsStore.Load and persisted.</summary>
    public bool ExecutionEnabled { get; set; }

    /// <summary>Start the pipe listener as soon as Revit loads the add-in. On by default via BridgeSettingsStore.Load and persisted.</summary>
    public bool AutoStartListener { get; set; }

    /// <summary>Ask in Revit before each script on top of the host AI's own confirmation. Persisted.</summary>
    public bool RequireLocalApproval { get; set; }

    public int DefaultTimeoutSeconds { get; set; } = 30;

    public int MaxSourceBytes { get; set; } = 32 * 1024;

    public int MaxOutputBytes { get; set; } = 64 * 1024;

    public int MaxLogLines { get; set; } = 200;

    public int ScriptCacheSize { get; set; } = 50;

    /// <summary>Scripts may modify a family document. Off by default: families are usually shared libraries.</summary>
    public bool AllowFamilyDocuments { get; set; }
}

/// <summary>Progress reported by a running script: `progress(current, total, message)` in script terms.</summary>
public sealed record ScriptProgress(int Current, int? Total, string? Message);

/// <summary>What the status window shows about the most recent script.</summary>
public sealed record LastRunInfo(
    DateTimeOffset Timestamp,
    string Label,
    string Source,
    bool IsError,
    string? Message,
    long DurationMs,
    bool RolledBack,
    int Added,
    int Modified,
    int Deleted);

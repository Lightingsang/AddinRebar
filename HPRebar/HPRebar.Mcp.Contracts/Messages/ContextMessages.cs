using System.Collections.Generic;

namespace HPRebar.Mcp.Contracts.Messages;

/// <summary>Parameters of `revit.context`.</summary>
public sealed record ContextRequest(bool IncludeSelection = false);

/// <summary>
///     Snapshot of the Revit session the AI is about to script against. Enough to write correct code
///     (units, active view, what is selected) without a round trip per question.
/// </summary>
public sealed class ContextResult
{
    public string RevitVersion { get; set; } = string.Empty;

    public string? DocTitle { get; set; }

    public string? DocPath { get; set; }

    public bool IsFamily { get; set; }

    public bool IsReadOnly { get; set; }

    public bool IsModifiable { get; set; }

    public UnitsInfo Units { get; set; } = new UnitsInfo("mm");

    public ViewInfo? ActiveView { get; set; }

    public IReadOnlyList<ElementInfo> Selection { get; set; } = System.Array.Empty<ElementInfo>();

    public IReadOnlyList<string> OpenDocs { get; set; } = System.Array.Empty<string>();

    public bool ExecutionEnabled { get; set; }
}

/// <summary>Display unit for lengths in the document; the Revit API itself always works in feet.</summary>
public sealed record UnitsInfo(string Length);

public sealed record ViewInfo(long Id, string Name, string Type);

public sealed record ElementInfo(long Id, string? Category, string? Name);

/// <summary>Parameters of `revit.inspect`: reflect over a Revit API type so the AI can discover members instead of guessing.</summary>
public sealed record InspectRequest(string TypeName, string? MemberFilter = null, int MaxMembers = 100);

public sealed class InspectResult
{
    public string TypeName { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public string? BaseType { get; set; }

    public IReadOnlyList<MemberSignature> Members { get; set; } = System.Array.Empty<MemberSignature>();

    public bool Truncated { get; set; }

    /// <summary>Set when the type was not found; lists close matches when there are any.</summary>
    public string? Message { get; set; }
}

/// <summary>Kind is `property` / `method` / `field` / `event`; signature is C#-like text.</summary>
public sealed record MemberSignature(string Kind, string Signature);

public sealed record CancelResult(bool Cancelled, bool WasRunning);

public sealed record BridgePingResult(bool Pong, string RevitVersion, bool ExecutionEnabled, bool Busy);

/// <summary>Payload of the `revit.progress` notification; <see cref="Id"/> is the request it belongs to.</summary>
public sealed record ProgressParams(long Id, int Progress, int? Total, string? Message);

/// <summary>Payload of the `revit.status` notification, sent whenever the bridge's state changes.</summary>
public sealed record StatusParams(bool Listening, bool ExecutionEnabled, bool Busy, string? DocTitle, string RevitVersion);

/// <summary>Payload of the `revit.log` notification: a bridge log line the server forwards to stderr.</summary>
public sealed record LogParams(string Level, string Message);

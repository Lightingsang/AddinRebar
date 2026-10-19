namespace HPBim.Client.Connection;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using HPBim.Core.Execution;

/// <summary>
/// Immutable record capturing operation intent and its lifecycle state in the local journal.
/// Serialized as a single JSON line in the append-only JSONL journal file.
/// </summary>
public sealed record IntentJournalRecord
{
    /// <summary>Unique operation batch ID generated before dispatch.</summary>
    public Guid OperationId { get; init; } = Guid.NewGuid();

    /// <summary>Deterministic SHA-256 hash of the execution plan.</summary>
    public string PlanHash { get; init; } = string.Empty;

    /// <summary>Timestamp (UTC) when this record or state transition was recorded.</summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Current lifecycle status of the operation.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IntentStatus Status { get; init; } = IntentStatus.Pending;

    /// <summary>Total number of element items targeted in this operation.</summary>
    public int ItemCount { get; init; }

    /// <summary>Targeted composite source keys for exact keyset verification.</summary>
    public IReadOnlyList<string> SourceKeys { get; init; } = Array.Empty<string>();

    /// <summary>Identity and version snapshot of the target Revit document.</summary>
    public DocumentIdentitySnapshot TargetDoc { get; init; } = new();

    private readonly string? _targetDocId;

    /// <summary>Canonical identifier for target document (Title, VersionGuid, or explicitly assigned ID).</summary>
    public string TargetDocId
    {
        get => !string.IsNullOrEmpty(_targetDocId)
            ? _targetDocId!
            : (!string.IsNullOrEmpty(TargetDoc.Title)
                ? TargetDoc.Title
                : (TargetDoc.VersionGuid != Guid.Empty ? TargetDoc.VersionGuid.ToString("D") : string.Empty));
        init => _targetDocId = value;
    }

    /// <summary>Optional error message or diagnostic detail if status is Failed or OutcomeUnknown.</summary>
    public string? ErrorReason { get; init; }

    /// <summary>Indicates whether this intent has reached a final state (Committed or Failed).</summary>
    [JsonIgnore]
    public bool IsTerminal => Status is IntentStatus.Committed or IntentStatus.Failed;

    /// <summary>Indicates whether this intent requires recovery or reconciliation.</summary>
    [JsonIgnore]
    public bool IsIncomplete => Status is IntentStatus.Pending or IntentStatus.InFlight or IntentStatus.OutcomeUnknown;
}

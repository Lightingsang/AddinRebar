namespace HPBim.Client.Connection;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Persistent write-ahead journal recording operation intent before network dispatch
/// across Revit Named Pipe, enabling crash recovery and idempotent reconciliation.
/// </summary>
public interface IIntentJournal : IAsyncDisposable, IDisposable
{
    /// <summary>Gets the absolute file path of the underlying JSONL journal.</summary>
    string JournalFilePath { get; }

    /// <summary>
    /// Records a new operation intent with status InFlight or Pending and flushes to disk.
    /// MUST be awaited before sending the request across the pipe.
    /// </summary>
    Task RecordIntentAsync(IntentJournalRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a status update for an existing operation (e.g. Committed, Failed, OutcomeUnknown).
    /// </summary>
    Task UpdateStatusAsync(
        Guid operationId,
        IntentStatus status,
        string? errorReason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all operations whose latest known status is Incomplete (Pending, InFlight, or OutcomeUnknown).
    /// </summary>
    Task<IReadOnlyList<IntentJournalRecord>> GetIncompleteIntentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent record for a specific operation ID, if present.
    /// </summary>
    Task<IntentJournalRecord?> GetIntentAsync(Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compacts the journal by removing completed entries older than the specified retention window,
    /// while strictly preserving all incomplete entries.
    /// </summary>
    Task<int> PruneOldEntriesAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}

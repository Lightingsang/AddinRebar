namespace HPBim.Client.Connection;

/// <summary>
/// Lifecycle status of an operation intent recorded in the local write-ahead journal.
/// </summary>
public enum IntentStatus
{
    /// <summary>Intent recorded locally, preparing for network dispatch.</summary>
    Pending = 0,

    /// <summary>Dispatched across Named Pipe; actively awaiting execution response from Revit.</summary>
    InFlight = 1,

    /// <summary>Execution confirmed successful and committed in Revit.</summary>
    Committed = 2,

    /// <summary>Execution failed with confirmed transaction rollback or clean pre-flight rejection.</summary>
    Failed = 3,

    /// <summary>Pipe disconnected, timed out, or process terminated while InFlight; outcome ambiguous.</summary>
    OutcomeUnknown = 4
}

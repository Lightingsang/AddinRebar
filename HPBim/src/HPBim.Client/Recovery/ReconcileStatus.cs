namespace HPBim.Client.Recovery;

/// <summary>
/// Status of an auto-reconciliation attempt against Revit Extensible Storage.
/// </summary>
public enum ReconcileStatus
{
    /// <summary>Receipt confirmed in Revit DB; transaction was committed and ledger updated.</summary>
    Committed = 0,

    /// <summary>Receipt absent in Revit DB; transaction was confirmed rolled back or never started.</summary>
    RolledBack = 1,

    /// <summary>Could not query Revit Extensible Storage (pipe disconnected, timeout, or doc mismatch).</summary>
    StillUnknown = 2,

    /// <summary>No unresolved or ambiguous intents found to reconcile.</summary>
    NoOp = 3
}

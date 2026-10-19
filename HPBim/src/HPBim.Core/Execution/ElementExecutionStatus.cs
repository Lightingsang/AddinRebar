namespace HPBim.Core.Execution;

/// <summary>
/// Granular execution outcome classification for an individual BIM element.
/// </summary>
public enum ElementExecutionStatus
{
    /// <summary>
    /// Element was newly instantiated in the host Revit document.
    /// </summary>
    Created = 1,

    /// <summary>
    /// An existing element was updated (parameters, geometry, or position synchronized).
    /// </summary>
    Updated = 2,

    /// <summary>
    /// Element already exists and is strictly identical to design specification; no write performed.
    /// </summary>
    NoOp = 3,

    /// <summary>
    /// Intentionally bypassed (e.g., belongs to a frozen zone, disabled stage, or prerequisite failed upstream).
    /// </summary>
    Skipped = 4,

    /// <summary>
    /// Element creation failed due to missing resources, invalid geometry, or host API failure.
    /// </summary>
    Failed = 5,

    /// <summary>
    /// Outcome could not be definitively determined due to truncated response, record deficit, or unconfirmed commit.
    /// </summary>
    OutcomeUnknown = 6
}
